using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace JustStart
{
    /// <summary>
    /// Runs Just Start once world generation finishes: picks the tile, season, arrival method, ideoligion,
    /// pawns, animals and map items, then starts the game. Any failure explains why and calls onFailure.
    /// </summary>
    public static class JustStartFlow
    {
        private static readonly AccessTools.FieldRef<ScenPart_PlayerPawnsArriveMethod, PlayerPawnsArriveMethod> ArriveMethodField =
            AccessTools.FieldRefAccess<ScenPart_PlayerPawnsArriveMethod, PlayerPawnsArriveMethod>("method");

        public static void Run(Action onFailure)
        {
            JustStartMapSpawnQueue.Begin();

            ScenarioDef? scenarioDef = ScenarioLookup.DefFor(Find.Scenario);
            JustStartScenarioExtension? ext = scenarioDef?.GetModExtension<JustStartScenarioExtension>();

            if (scenarioDef != null)
            {
                var validation = ScenarioValidator.Validate(scenarioDef, ext);
                foreach (var w in validation.Warnings) Log.Warning($"[JustStart] {w}");
                if (!validation.IsValid)
                {
                    Log.Error("[JustStart] Scenario validation failed:\n" + string.Join("\n", validation.Errors));
                    Fail("JustStart_ErrorValidationFailed".Translate(), onFailure);
                    return;
                }
            }

            if (!TileSelector.TryFindTile(scenarioDef, ext, out PlanetTile tile, out TileSelectionFailure? failure))
            {
                string detail = failure!.UnsatisfiedConstraintDescriptions.Any()
                    ? "\n\n" + string.Join("\n", failure.UnsatisfiedConstraintDescriptions.Select(d => "  - " + d))
                    : string.Empty;
                Fail(failure.Reason + detail, onFailure);
                return;
            }

            Find.GameInitData.startingTile = tile;

            if (ext != null && !ext.startingSeasons.NullOrEmpty())
                Find.GameInitData.startingSeason = ext.startingSeasons!.RandomElement();

            if (ext != null && !ext.arrivalMethods.NullOrEmpty())
                SetArrivalMethod(ext.arrivalMethods!.RandomElement());

            if (ModsConfig.IdeologyActive)
            {
                IdeologyMode mode = ext?.ideologyRules?.ResolveMode() ?? JustStartMod.Settings.defaultIdeologyMode;
                if (!IdeologyGenerator.ApplyMode(mode, ext?.ideologyRules))
                {
                    Fail("JustStart_ErrorNoValidIdeo".Translate(), onFailure);
                    return;
                }
                // Sets startingPawnCount and generates the scenario's starting pawns; without Ideology, world generation already did this.
                Find.Scenario.PostIdeoChosen();
            }

            var colonists = StartingPawnGenerator.GenerateColonists(ext);
            if (colonists.Count > 0)
            {
                Find.GameInitData.startingAndOptionalPawns.AddRange(colonists);
                Find.GameInitData.startingPawnCount = colonists.Count;
            }

            if (Find.GameInitData.startingPawnCount < 1)
            {
                Fail("JustStart_ErrorNoStartingPawns".Translate(), onFailure);
                return;
            }

            // Non-colonist pawns, animals and map items are spawned once the map exists, by Patch_Map_FinalizeInit_SpawnJustStartPawns.
            JustStartMapSpawnQueue.Enqueue(StartingPawnGenerator.GenerateNonColonistRoles(ext));
            JustStartMapSpawnQueue.EnqueueAnimals(AnimalGenerator.Generate(ext));
            JustStartMapSpawnQueue.EnqueueMapThings(ext?.mapThings);

            PageUtility.InitGameStart();
        }

        // Swaps in a copy of the scenario first, so the roll leaves the ScenarioDef's own scenario unchanged for later games.
        private static void SetArrivalMethod(PlayerPawnsArriveMethod method)
        {
            Current.Game.Scenario = Find.Scenario.CopyForEditing();
            foreach (var part in Find.Scenario.AllParts.OfType<ScenPart_PlayerPawnsArriveMethod>())
                ArriveMethodField(part) = method;
        }

        private static void Fail(string message, Action onFailure)
        {
            onFailure();
            Find.WindowStack.Add(new Dialog_MessageBox(message + "\n\n" + "JustStart_ErrorAdjustWorld".Translate()));
        }
    }
}
