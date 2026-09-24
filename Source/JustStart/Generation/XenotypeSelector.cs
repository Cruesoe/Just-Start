using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace JustStart
{
    /// <summary>Resolves a XenotypeRuleSet into one xenotype per pawn; a null entry leaves the choice to vanilla generation.</summary>
    public static class XenotypeSelector
    {
        /// <summary>All null without Biotech, without a rule, or in Any mode.</summary>
        public static List<XenotypeDef?> Assign(XenotypeRuleSet? rule, int count)
        {
            var result = new List<XenotypeDef?>(count);
            if (!ModsConfig.BiotechActive || rule == null || rule.mode == XenotypeRuleMode.Any)
            {
                result.AddRange(Enumerable.Repeat<XenotypeDef?>(null, count));
                return result;
            }

            List<XenotypeDef> all = DefDatabase<XenotypeDef>.AllDefsListForReading;
            switch (rule.mode)
            {
                case XenotypeRuleMode.SameForAll:
                    result.AddRange(Enumerable.Repeat<XenotypeDef?>(rule.EligiblePoolFor(all).RandomElementWithFallback(), count));
                    break;
                case XenotypeRuleMode.Fixed:
                    result.AddRange(Enumerable.Repeat<XenotypeDef?>(rule.xenotypes?.FirstOrDefault(), count));
                    break;
                case XenotypeRuleMode.Required:
                    // requiredCount pawns, at random positions, get a listed xenotype; the rest are left to vanilla.
                    List<XenotypeDef> required = rule.xenotypes.NullOrEmpty() ? all : rule.xenotypes!;
                    for (int i = 0; i < count; i++)
                        result.Add(i < rule.requiredCount ? required.RandomElement() : null);
                    result.Shuffle();
                    break;
                default:
                    // AllowedPool, Excluded and Random pick independently per pawn.
                    List<XenotypeDef> pool = rule.EligiblePoolFor(all);
                    for (int i = 0; i < count; i++)
                        result.Add(pool.RandomElementWithFallback());
                    break;
            }
            return result;
        }
    }
}
