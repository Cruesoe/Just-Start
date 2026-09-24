using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace JustStart
{
    public class TileSelectionFailure
    {
        public string Reason = string.Empty;
        public List<string> UnsatisfiedConstraintDescriptions = new List<string>();
    }

    /// <summary>
    /// Picks a random starting tile that vanilla accepts for a new settlement and that meets the scenario's
    /// tile constraints. Never falls back to an unconstrained tile.
    /// </summary>
    public static class TileSelector
    {
        public static bool TryFindTile(ScenarioDef? scenarioDef, JustStartScenarioExtension? ext,
            out PlanetTile chosenTile, out TileSelectionFailure? failure)
        {
            World world = Find.World;
            var context = new TileSelectionContext { World = world, Scenario = scenarioDef?.scenario ?? Find.Scenario };

            bool constraintsApply = ext?.tileConstraints != null
                && (!ext.curatedRestrictionOnly || JustStartMod.Settings.useCuratedVanillaRestrictions);
            List<TileConstraint> constraints = constraintsApply ? ext!.tileConstraints! : new List<TileConstraint>();

            // Vanilla's random start skips canAutoChoose=false biomes (sea ice, glacial plain); allowAnyBiome or a biome allow list opts in.
            bool allowAnyBiome = ext?.allowAnyBiome == true;
            var listedBiomes = new HashSet<BiomeDef>(constraints.OfType<TileConstraint_Biome>()
                .Where(c => c.allowedBiomes != null)
                .SelectMany(c => c.allowedBiomes!));

            // Lazy Fisher-Yates: tiles are tried in uniformly random order and the first that passes is a uniform pick among all valid tiles.
            PlanetLayer surface = world.grid.Surface;
            int tilesCount = world.grid.TilesCount;
            int[] order = new int[tilesCount];
            for (int i = 0; i < tilesCount; i++)
                order[i] = i;

            for (int i = 0; i < tilesCount; i++)
            {
                int j = Rand.RangeInclusive(i, tilesCount - 1);
                (order[i], order[j]) = (order[j], order[i]);
                var tile = new PlanetTile(order[i], surface);

                // Cheap biome and constraint checks run before vanilla's settlement check, which looks up world objects.
                BiomeDef biome = world.grid[tile].PrimaryBiome;
                if (!biome.canAutoChoose && !allowAnyBiome && !listedBiomes.Contains(biome))
                    continue;
                if (!constraints.All(c => c.IsSatisfiedBy(tile, context)))
                    continue;
                if (!TileFinder.IsValidTileForNewSettlement(tile))
                    continue;

                chosenTile = tile;
                failure = null;
                return true;
            }

            chosenTile = PlanetTile.Invalid;
            failure = new TileSelectionFailure
            {
                Reason = "JustStart_ErrorNoValidTile".Translate(scenarioDef?.LabelCap ?? Find.Scenario.name),
                UnsatisfiedConstraintDescriptions = constraints.Select(c => c.Describe()).ToList(),
            };
            return false;
        }
    }
}
