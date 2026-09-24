using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace JustStart
{
    /// <summary>
    /// Picks the starting tile the way vanilla's "Select random site" button does (TileFinder.RandomStartingTile): weighted by
    /// the biome's settlementSelectionWeight and the player faction's temperature curve, skipping biomes vanilla never picks at random.
    /// </summary>
    public static class TileSelector
    {
        public static bool TryFindTile(out PlanetTile chosen)
        {
            WorldGrid grid = Find.WorldGrid;
            PlanetLayer surface = grid.Surface;
            SimpleCurve? temperatureCurve = Faction.OfPlayer.def.minSettlementTemperatureChanceCurve;
            bool excludeExtreme = JustStartMod.Settings.excludeExtremeBiomes;

            var tiles = new List<PlanetTile>();
            var weights = new List<float>();
            float total = 0f;
            for (int i = 0; i < grid.TilesCount; i++)
            {
                var tile = new PlanetTile(i, surface);
                Tile data = grid[tile];
                BiomeDef biome = data.PrimaryBiome;
                if (!biome.canBuildBase || !biome.implemented || !biome.canAutoChoose || data.hilliness == Hilliness.Impassable)
                    continue;
                if (excludeExtreme && IsExtreme(biome))
                    continue;

                float weight = biome.settlementSelectionWeight;
                if (temperatureCurve != null)
                    weight *= temperatureCurve.Evaluate(GenTemperature.MinTemperatureAtTile(tile));
                if (weight <= 0f)
                    continue;

                tiles.Add(tile);
                weights.Add(weight);
                total += weight;
            }

            // Weighted draws; vanilla's settlement check (occupied or next to a settlement) runs only on the drawn tile.
            while (tiles.Count > 0)
            {
                int index = WeightedIndex(weights, total);
                if (TileFinder.IsValidTileForNewSettlement(tiles[index]))
                {
                    chosen = tiles[index];
                    return true;
                }
                total -= weights[index];
                int last = tiles.Count - 1;
                tiles[index] = tiles[last];
                weights[index] = weights[last];
                tiles.RemoveAt(last);
                weights.RemoveAt(last);
            }

            chosen = PlanetTile.Invalid;
            return false;
        }

        /// <summary>Biomes the game warns about when settling there (BiomeDef.settleWarning).</summary>
        public static bool IsExtreme(BiomeDef biome) => !biome.settleWarning.NullOrEmpty();

        private static int WeightedIndex(List<float> weights, float total)
        {
            float roll = Rand.Value * total;
            for (int i = 0; i < weights.Count; i++)
            {
                roll -= weights[i];
                if (roll < 0f)
                    return i;
            }
            return weights.Count - 1;
        }
    }
}
