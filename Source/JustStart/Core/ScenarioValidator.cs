using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace JustStart
{
    public class ValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
    }

    /// <summary>Checks a scenario's rules for what can be checked without a world; TileSelector reports a world with no valid tile.</summary>
    public static class ScenarioValidator
    {
        public static ValidationResult Validate(ScenarioDef scenarioDef, JustStartScenarioExtension? ext)
        {
            var result = new ValidationResult();
            if (ext == null) return result;

            result.Errors.AddRange(ext.ValidateReferences(scenarioDef));

            if (ext.ideologyRules != null && !ModsConfig.IdeologyActive)
                result.Warnings.Add($"[{scenarioDef.defName}] Ideology rule will be skipped: Ideology DLC inactive.");

            bool anyXenotypeRule = ext.xenotypeRules != null
                || (ext.pawnRoles?.Any(r => r.xenotypeRules != null) ?? false);
            if (anyXenotypeRule && !ModsConfig.BiotechActive)
                result.Warnings.Add($"[{scenarioDef.defName}] Xenotype rules will be skipped: Biotech DLC inactive.");

            return result;
        }
    }
}
