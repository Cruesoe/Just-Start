using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace JustStart
{
    public enum XenotypeRuleMode
    {
        /// <summary>Normal/unrestricted eligible behaviour - vanilla pawn generation decides.</summary>
        Any,

        /// <summary>Choose only from the listed xenotypes (per pawn, independently).</summary>
        AllowedPool,

        /// <summary>Guarantee at least <see cref="XenotypeRuleSet.requiredCount"/> pawns use <see cref="XenotypeRuleSet.xenotypes"/>.</summary>
        Required,

        /// <summary>A specific pawn slot/role always uses <see cref="XenotypeRuleSet.xenotypes"/>[0].</summary>
        Fixed,

        /// <summary>Any xenotype except the listed ones.</summary>
        Excluded,

        /// <summary>Pick one eligible xenotype at random and apply it to every applicable starting colonist.</summary>
        SameForAll,

        /// <summary>Each pawn gets a uniformly random xenotype from every XenotypeDef.</summary>
        Random,
    }

    /// <summary>Biotech-only. How starting-pawn xenotypes are chosen, scenario-wide or for one <see cref="PawnRole"/>.</summary>
    public class XenotypeRuleSet
    {
        public XenotypeRuleMode mode = XenotypeRuleMode.Any;
        public List<XenotypeDef>? xenotypes;
        public int requiredCount = 1;

        public IEnumerable<string> ValidateReferences(int pawnCountForScope)
        {
            // Without Biotech the rule is skipped; ScenarioValidator warns rather than failing the scenario.
            if (!ModsConfig.BiotechActive)
                yield break;

            // SameForAll with no list picks from every xenotype.
            bool needsPool = mode is XenotypeRuleMode.AllowedPool or XenotypeRuleMode.Required
                or XenotypeRuleMode.Fixed or XenotypeRuleMode.Excluded;

            if (needsPool && (xenotypes == null || xenotypes.Count == 0))
                yield return $"XenotypeRuleSet mode {mode} requires at least one XenotypeDef in <xenotypes>.";

            if (mode == XenotypeRuleMode.Required && requiredCount > pawnCountForScope)
                yield return $"XenotypeRuleSet requires {requiredCount} pawns but only {pawnCountForScope} pawns are in scope.";

            if (xenotypes != null)
            {
                foreach (var x in xenotypes)
                    if (x == null)
                        yield return "XenotypeRuleSet references an unresolved XenotypeDef.";
            }
        }

        /// <summary>Resolve the xenotype pool a single pawn may be generated from, given this rule.</summary>
        public List<XenotypeDef> EligiblePoolFor(List<XenotypeDef> allEligible)
        {
            switch (mode)
            {
                case XenotypeRuleMode.Any:
                case XenotypeRuleMode.Random:
                    return allEligible;
                case XenotypeRuleMode.AllowedPool:
                case XenotypeRuleMode.Required:
                case XenotypeRuleMode.Fixed:
                case XenotypeRuleMode.SameForAll:
                    return xenotypes != null && xenotypes.Count > 0 ? xenotypes : allEligible;
                case XenotypeRuleMode.Excluded:
                    return allEligible.Where(x => xenotypes == null || !xenotypes.Contains(x)).ToList();
                default:
                    return allEligible;
            }
        }
    }
}
