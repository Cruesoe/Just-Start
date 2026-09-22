using System.Collections.Generic;
using System.Linq;
using Verse;

namespace JustStart
{
    public class ValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
    }

    /// <summary>
    /// Runs before Just Start commits to a generated start. Checks everything that can be
    /// checked without a generated world (Def references, internal rule coherence); the
    /// "zero valid tiles in this generated world" case is a separate, later, recoverable
    /// failure surfaced by TileSelector against the actual World.
    /// </summary>
    public static class ScenarioValidator
    {
        public static ValidationResult Validate(ScenarioDef scenarioDef, JustStartScenarioExtension ext)
        {
            var result = new ValidationResult();
            if (ext == null) return result;

            foreach (var msg in ext.ValidateReferences(scenarioDef))
                result.Errors.Add(msg);

            if (ext.ideologyRules?.mode is IdeologyMode.Fixed or IdeologyMode.Fluid && !ModsConfig.IdeologyActive)
                result.Warnings.Add($"[{scenarioDef.defName}] Ideology rule will be skipped: Ideology DLC inactive.");

            bool anyXenotypeRule = ext.xenotypeRules != null
                || (ext.pawnRoles?.Any(r => r.xenotypeRules != null) ?? false);
            if (anyXenotypeRule && !ModsConfig.BiotechActive)
                result.Warnings.Add($"[{scenarioDef.defName}] Xenotype rules will be skipped: Biotech DLC inactive.");

            return result;
        }
    }
}
