using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace JustStart
{
    /// <summary>
    /// Picks the starting tile the way vanilla's "Select random site" button does (TileFinder.RandomStartingTile): weighted by
    /// the biome's settlementSelectionWeight and the player faction's temperature curve, skipping biomes vanilla never picks at
    /// random. The starting site settings filter the candidates.
    /// </summary>
    public static class TileSelector
    {
        public const int MaxGrowingDays = 60;

        public const int MaxHostileDistance = 30;

        public const float MinTemperature = -50f;

        public const float MaxTemperature = 50f;

        private const int DaysPerTwelfth = 5;

        // Plant.MinOptimalGrowthTemperature and MaxOptimalGrowthTemperature, as the world tab's growing period uses.
        private const float MinGrowingTemperature = 6f;
        private const float MaxGrowingTemperature = 42f;

        public static bool TryFindTile(out PlanetTile chosen)
        {
            JustStartSettings settings = JustStartMod.Settings;
            WorldGrid grid = Find.WorldGrid;
            PlanetLayer surface = grid.Surface;
            SimpleCurve? temperatureCurve = Faction.OfPlayer.def.minSettlementTemperatureChanceCurve;

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
                if (!PassesQuickFilters(data, biome, settings))
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

            List<PlanetTile> hostileSettlements = HostileSettlementTiles(surface, settings.minHostileDistance);

            // Weighted draws; vanilla's settlement check and the slower filters run only on the drawn tile.
            while (tiles.Count > 0)
            {
                int index = WeightedIndex(weights, total);
                PlanetTile tile = tiles[index];
                if (TileFinder.IsValidTileForNewSettlement(tile) && PassesSlowFilters(tile, settings, hostileSettlements))
                {
                    chosen = tile;
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

        /// <summary>False for the full slider range, which allows any temperature.</summary>
        public static bool LimitsTemperature(FloatRange range) => range.min > MinTemperature || range.max < MaxTemperature;

        /// <summary>Biomes the game warns about when settling there (BiomeDef.settleWarning).</summary>
        public static bool IsExtreme(BiomeDef biome) => !biome.settleWarning.NullOrEmpty();

        private static bool PassesQuickFilters(Tile data, BiomeDef biome, JustStartSettings settings)
        {
            if (settings.excludeExtremeBiomes && IsExtreme(biome))
                return false;
            if (!HillinessAllowed(data.hilliness, settings))
                return false;
            if (LimitsTemperature(settings.temperatureRange) && !settings.temperatureRange.Includes(data.temperature))
                return false;
            if (settings.requireRiver && (data as SurfaceTile)?.Rivers.NullOrEmpty() != false)
                return false;
            if (settings.requireRoad && (data as SurfaceTile)?.Roads.NullOrEmpty() != false)
                return false;
            return true;
        }

        private static bool PassesSlowFilters(PlanetTile tile, JustStartSettings settings, List<PlanetTile> hostileSettlements)
        {
            if (settings.requireCoast && Find.World.CoastDirectionAt(tile) == Rot4.Invalid)
                return false;
            if (settings.minGrowingDays > 0 && GrowingDays(tile) < settings.minGrowingDays)
                return false;
            foreach (PlanetTile settlement in hostileSettlements)
            {
                if (Find.WorldGrid.ApproxDistanceInTiles(tile, settlement) < settings.minHostileDistance)
                    return false;
            }
            return true;
        }

        private static bool HillinessAllowed(Hilliness hilliness, JustStartSettings settings)
        {
            switch (hilliness)
            {
                case Hilliness.Flat:
                    return settings.allowFlat;
                case Hilliness.SmallHills:
                    return settings.allowSmallHills;
                case Hilliness.LargeHills:
                    return settings.allowLargeHills;
                case Hilliness.Mountainous:
                    return settings.allowMountainous;
                default:
                    return true;
            }
        }

        /// <summary>Days a year whose average temperature suits outdoor growing, as the world tab's growing period counts them.</summary>
        private static int GrowingDays(PlanetTile tile) =>
            GenTemperature.TwelfthsInAverageTemperatureRange(tile, MinGrowingTemperature, MaxGrowingTemperature).Count * DaysPerTwelfth;

        private static List<PlanetTile> HostileSettlementTiles(PlanetLayer surface, int minDistance)
        {
            var result = new List<PlanetTile>();
            if (minDistance <= 0)
                return result;
            foreach (Settlement settlement in Find.WorldObjects.Settlements)
            {
                if (settlement.Faction != null && settlement.Faction != Faction.OfPlayer && settlement.Faction.HostileTo(Faction.OfPlayer)
                    && settlement.Tile.Layer == surface)
                    result.Add(settlement.Tile);
            }
            return result;
        }

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
