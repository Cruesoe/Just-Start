using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace JustStart
{
    /// <summary>
    /// Resolves which XenotypeDef each PlayerColonist pawn generation request should use,
    /// given a scenario-wide and/or per-role XenotypeRuleSet. Biotech-only; callers must check
    /// ModsConfig.BiotechActive before using this.
    /// </summary>
    public static class XenotypeSelector
    {
        public static List<XenotypeDef> AllEligible() =>
            DefDatabase<XenotypeDef>.AllDefsListForReading.Where(x => !x.doNotGenerateNaturally).ToList();

        /// <summary>
        /// Builds a per-pawn xenotype assignment for a role. SameForAll is resolved once and
        /// applied to every slot; Required guarantees at least requiredCount slots get one of
        /// the specified xenotypes, remaining slots fall back to Any.
        /// </summary>
        public static List<XenotypeDef> AssignForRole(XenotypeRuleSet rule, int count, Random rng)
        {
            var all = AllEligible();
            var result = new List<XenotypeDef>(count);

            if (rule == null || rule.mode == XenotypeRuleMode.Any)
            {
                for (int i = 0; i < count; i++) result.Add(null); // null => let vanilla PawnGenerator decide
                return result;
            }

            if (rule.mode == XenotypeRuleMode.SameForAll)
            {
                var pool = rule.EligiblePoolFor(all);
                var chosen = pool.Count > 0 ? pool[rng.Next(pool.Count)] : null;
                for (int i = 0; i < count; i++) result.Add(chosen);
                return result;
            }

            if (rule.mode == XenotypeRuleMode.Fixed)
            {
                var fixedXeno = rule.xenotypes?.FirstOrDefault();
                for (int i = 0; i < count; i++) result.Add(fixedXeno);
                return result;
            }

            if (rule.mode == XenotypeRuleMode.Required)
            {
                var pool = rule.xenotypes ?? all;
                for (int i = 0; i < count; i++)
                    result.Add(i < rule.requiredCount ? pool[rng.Next(pool.Count)] : null);
                return result;
            }

            // AllowedPool / Excluded: independently pick per pawn from the resolved pool.
            var eligiblePool = rule.EligiblePoolFor(all);
            for (int i = 0; i < count; i++)
                result.Add(eligiblePool.Count > 0 ? eligiblePool[rng.Next(eligiblePool.Count)] : null);
            return result;
        }
    }
}
