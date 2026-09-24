using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace JustStart
{
    /// <summary>World and scenario state passed to tile constraints while evaluating candidates.</summary>
    public class TileSelectionContext
    {
        public World World = null!;
        public Scenario Scenario = null!;
    }

    /// <summary>A single starting-tile rule, declared in XML with Class="..." so other mods can add their own subclasses.</summary>
    public abstract class TileConstraint
    {
        /// <summary>Return false to reject the tile. Do not throw for "just doesn't match".</summary>
        public abstract bool IsSatisfiedBy(PlanetTile tile, TileSelectionContext context);

        /// <summary>Human-readable description used in "no valid tile" diagnostics.</summary>
        public abstract string Describe();

        /// <summary>Checks the rule itself, without a world; called by ScenarioValidator before tile selection.</summary>
        public virtual IEnumerable<string> ValidateReferences() => Enumerable.Empty<string>();
    }

    /// <summary>Restricts starting biome to an allowed pool and/or excludes a pool.</summary>
    public class TileConstraint_Biome : TileConstraint
    {
        public List<BiomeDef>? allowedBiomes;
        public List<BiomeDef>? excludedBiomes;

        public override bool IsSatisfiedBy(PlanetTile tile, TileSelectionContext context)
        {
            BiomeDef biome = context.World.grid[tile].PrimaryBiome;
            if (allowedBiomes != null && allowedBiomes.Count > 0 && !allowedBiomes.Contains(biome))
                return false;
            if (excludedBiomes != null && excludedBiomes.Contains(biome))
                return false;
            return true;
        }

        public override string Describe()
        {
            if (allowedBiomes != null && allowedBiomes.Count > 0)
                return "JustStart_ConstraintBiomeAllowed".Translate(allowedBiomes.Select(b => b.label).ToCommaList());
            if (excludedBiomes != null && excludedBiomes.Count > 0)
                return "JustStart_ConstraintBiomeExcluded".Translate(excludedBiomes.Select(b => b.label).ToCommaList());
            return GetType().Name;
        }

        public override IEnumerable<string> ValidateReferences()
        {
            if ((allowedBiomes == null || allowedBiomes.Count == 0) && (excludedBiomes == null || excludedBiomes.Count == 0))
                yield return "TileConstraint_Biome declared with neither allowedBiomes nor excludedBiomes.";
        }
    }

    /// <summary>Restricts starting tile by its average yearly temperature (Tile.temperature, in Celsius).</summary>
    public class TileConstraint_Temperature : TileConstraint
    {
        public FloatRange averageTemperature = new FloatRange(-999f, 999f);

        public override bool IsSatisfiedBy(PlanetTile tile, TileSelectionContext context) =>
            averageTemperature.Includes(context.World.grid[tile].temperature);

        public override string Describe() =>
            "JustStart_ConstraintTemperature".Translate(
                averageTemperature.min.ToStringTemperature("F0"),
                averageTemperature.max.ToStringTemperature("F0"));

        public override IEnumerable<string> ValidateReferences()
        {
            if (averageTemperature.max < averageTemperature.min)
                yield return $"TileConstraint_Temperature has max below min ({averageTemperature}).";
        }
    }

    /// <summary>Restricts starting terrain by Hilliness (e.g. requiring Mountainous terrain).</summary>
    public class TileConstraint_Hilliness : TileConstraint
    {
        public List<Hilliness>? allowed;

        public override bool IsSatisfiedBy(PlanetTile tile, TileSelectionContext context)
        {
            if (allowed == null || allowed.Count == 0) return true;
            return allowed.Contains(context.World.grid[tile].hilliness);
        }

        public override string Describe() =>
            "JustStart_ConstraintHillinessAllowed".Translate((allowed ?? new List<Hilliness>()).Select(h => h.GetLabel()).ToCommaList(useAnd: false));

        public override IEnumerable<string> ValidateReferences()
        {
            if (allowed == null || allowed.Count == 0)
                yield return "TileConstraint_Hilliness declared with an empty allowed list.";
        }
    }
}
