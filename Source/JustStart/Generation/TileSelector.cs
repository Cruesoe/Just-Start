using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace JustStart
{
    public class TileSelectionFailure
    {
        public string Reason;
        public List<string> UnsatisfiedConstraintDescriptions = new List<string>();
    }

    /// <summary>
    /// Candidate-based starting-tile selection (Section 7): enumerate tiles the game itself
    /// considers valid for a new settlement, narrow by the scenario's Just Start constraints,
    /// then pick randomly among what remains. Never falls back to an unconstrained tile if a
    /// required constraint can't be met - see Section 7 "zero valid candidates".
    /// </summary>
    public static class TileSelector
    {
        public static bool TryFindTile(ScenarioDef scenarioDef, JustStartScenarioExtension ext,
            out PlanetTile chosenTile, out TileSelectionFailure failure)
        {
            var world = Find.World;
            var context = new TileSelectionContext { World = world, Scenario = scenarioDef.scenario };

            List<TileConstraint> activeConstraints = new List<TileConstraint>();
            if (ext?.tileConstraints != null)
            {
                bool curatedGateOpen = !ext.curatedRestrictionOnly || JustStartMod.Settings.useCuratedVanillaRestrictions;
                if (curatedGateOpen)
                    activeConstraints.AddRange(ext.tileConstraints);
            }

            var candidates = new List<PlanetTile>();
            int tilesCount = world.grid.TilesCount;
            for (int i = 0; i < tilesCount; i++)
            {
                var tile = new PlanetTile(i, world.grid.Surface);

                // Vanilla settlement-site validity (not ocean/lake, not impassable, not already
                // occupied, etc.) - reuse the game's own check rather than reimplementing it.
                if (!TileFinder.IsValidTileForNewSettlement(tile))
                    continue;

                if (activeConstraints.Count > 0 && !activeConstraints.All(c => c.IsSatisfiedBy(tile, context)))
                    continue;

                candidates.Add(tile);
            }

            if (candidates.Count == 0)
            {
                chosenTile = PlanetTile.Invalid;
                failure = new TileSelectionFailure
                {
                    Reason = "JustStart.Error.NoValidTile".Translate(scenarioDef.LabelCap),
                    UnsatisfiedConstraintDescriptions = activeConstraints.Select(c => c.Describe()).ToList(),
                };
                return false;
            }

            chosenTile = candidates.RandomElement();
            failure = null;
            return true;
        }
    }
}
