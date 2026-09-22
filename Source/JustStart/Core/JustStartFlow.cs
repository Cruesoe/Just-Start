using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace JustStart
{
    /// <summary>
    /// Orchestrates the "Just Start" action once the player has finished configuring scenario,
    /// storyteller, difficulty, and world generation. Everything below this point (tile choice,
    /// pawn/ideology/animal generation) is automated; nothing above it is touched or bypassed.
    ///
    /// This class is intentionally UI-agnostic: it is invoked by a Harmony patch (see
    /// Patches/PageFlowPatches.cs) that adds the "Just Start" button to the appropriate vanilla
    /// page and wires its click to Run(). The exact page/button hookup is the part of this mod
    /// most likely to need adjustment against the real Assembly-CSharp.dll - see
    /// Patches/PageFlowPatches.cs for the verification note.
    /// </summary>
    public static class JustStartFlow
    {
        public static void Run(Action onFailure)
        {
            ScenarioDef scenarioDef = Find.Scenario.GetType() != null
                ? DefDatabase<ScenarioDef>.AllDefsListForReading.FirstOrDefault(d => d.scenario == Find.Scenario)
                : null;

            JustStartScenarioExtension ext = scenarioDef?.GetModExtension<JustStartScenarioExtension>();

            if (scenarioDef != null)
            {
                var validation = ScenarioValidator.Validate(scenarioDef, ext);
                foreach (var w in validation.Warnings) Log.Warning($"[JustStart] {w}");
                if (!validation.IsValid)
                {
                    Log.Error("[JustStart] Scenario validation failed:\n" + string.Join("\n", validation.Errors));
                    Messages.Message("JustStart.Error.ValidationFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                    onFailure?.Invoke();
                    return;
                }
            }

            if (!TileSelector.TryFindTile(scenarioDef, ext, out PlanetTile tile, out TileSelectionFailure failure))
            {
                string detail = failure.UnsatisfiedConstraintDescriptions.Any()
                    ? "\n" + string.Join("\n", failure.UnsatisfiedConstraintDescriptions)
                    : string.Empty;
                Messages.Message(failure.Reason + detail, MessageTypeDefOf.RejectInput, false);
                onFailure?.Invoke();
                return;
            }

            Find.GameInitData.startingTile = tile;

            var rng = new Random();

            if (ModsConfig.IdeologyActive)
            {
                IdeologyMode mode = ext?.ideologyRules?.mode
                    ?? JustStartMod.Settings.defaultIdeologyMode;
                IdeologyGenerator.ApplyMode(mode);
            }

            var colonists = StartingPawnGenerator.GenerateColonists(scenarioDef, ext, rng);
            if (colonists.Count > 0)
            {
                Find.GameInitData.startingAndOptionalPawns.Clear();
                Find.GameInitData.startingAndOptionalPawns.AddRange(colonists);
                Find.GameInitData.startingPawnCount = colonists.Count;
            }
            // If no explicit pawnRoles were declared, GameInitData already holds whatever the
            // scenario's own vanilla config-page pipeline produced; Just Start only skipped
            // showing that page to the player (see Patches/PageFlowPatches.cs).

            var nonColonistPawns = StartingPawnGenerator.GenerateNonColonistRoles(ext, rng);
            // Hostile/prisoner pawns are spawned onto the map once map generation begins;
            // see Patches/PageFlowPatches.cs MapGenerated postfix for where these are placed.
            JustStartMapSpawnQueue.Enqueue(nonColonistPawns);

            var animals = AnimalGenerator.Generate(ext, rng);
            foreach (var animal in animals)
                Find.GameInitData.startingAndOptionalPawns.Add(animal);

            PageUtility.InitGameStart();
        }
    }
}
