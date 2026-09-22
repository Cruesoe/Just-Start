using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using Verse;

namespace JustStart
{
    /// <summary>
    /// Context passed to tile constraints while evaluating candidates. Kept minimal and
    /// extensible so future constraint types (coast, rivers, faction proximity, ...) can
    /// read whatever world/tile state they need without changing the constraint contract.
    /// </summary>
    public class TileSelectionContext
    {
        public World World;
        public Scenario Scenario;
    }

    /// <summary>
    /// Base class for a single, composable starting-tile rule. Third-party XML declares
    /// constraints via the Class="..." XML list idiom (same pattern vanilla uses for
    /// ScenParts/PatchOperations), so new constraint types added later - by this mod or by
    /// other mods - slot in without touching the selection pipeline.
    /// </summary>
    public abstract class TileConstraint
    {
        /// <summary>Return false to reject the tile. Do not throw for "just doesn't match".</summary>
        public abstract bool IsSatisfiedBy(PlanetTile tile, TileSelectionContext context);

        /// <summary>Human-readable description used in "no valid tile" diagnostics.</summary>
        public abstract string Describe();

        /// <summary>
        /// Structural validation independent of any generated world (e.g. "allowedBiomes is
        /// non-empty and every BiomeDef reference resolved"). Called by ScenarioValidator
        /// before the tile-selection pipeline ever runs.
        /// </summary>
        public virtual IEnumerable<string> ValidateReferences() => Enumerable.Empty<string>();
    }

    /// <summary>Restricts starting biome to an allowed pool and/or excludes a pool.</summary>
    public class TileConstraint_Biome : TileConstraint
    {
        public List<BiomeDef> allowedBiomes;
        public List<BiomeDef> excludedBiomes;

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
                return "JustStart.Constraint.Biome.Allowed".Translate(allowedBiomes.Select(b => b.label).ToCommaList());
            if (excludedBiomes != null && excludedBiomes.Count > 0)
                return "JustStart.Constraint.Biome.Excluded".Translate(excludedBiomes.Select(b => b.label).ToCommaList());
            return GetType().Name;
        }

        public override IEnumerable<string> ValidateReferences()
        {
            if ((allowedBiomes == null || allowedBiomes.Count == 0) && (excludedBiomes == null || excludedBiomes.Count == 0))
                yield return "TileConstraint_Biome declared with neither allowedBiomes nor excludedBiomes.";
        }
    }

    /// <summary>Restricts starting terrain by Hilliness (e.g. requiring Mountainous terrain).</summary>
    public class TileConstraint_Hilliness : TileConstraint
    {
        public List<Hilliness> allowed;

        public override bool IsSatisfiedBy(PlanetTile tile, TileSelectionContext context)
        {
            if (allowed == null || allowed.Count == 0) return true;
            return allowed.Contains(context.World.grid[tile].hilliness);
        }

        public override string Describe() =>
            "JustStart.Constraint.Hilliness.Allowed".Translate(allowed.Select(h => h.ToString()).ToCommaList());

        public override IEnumerable<string> ValidateReferences()
        {
            if (allowed == null || allowed.Count == 0)
                yield return "TileConstraint_Hilliness declared with an empty allowed list.";
        }
    }
}
