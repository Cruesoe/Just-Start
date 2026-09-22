using System.Collections.Generic;
using System.Linq;
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
    }

    /// <summary>
    /// Biotech-only. Declares how starting-pawn xenotypes are chosen for a scenario (or a
    /// specific pawn role - see <see cref="PawnRoleDef"/>). Never infers a biome<->xenotype
    /// relationship; that only happens if a scenario author explicitly writes both a tile
    /// constraint and a xenotype rule that happen to agree.
    /// </summary>
    public class XenotypeRuleSet
    {
        public XenotypeRuleMode mode = XenotypeRuleMode.Any;
        public List<XenotypeDef> xenotypes;
        public int requiredCount = 1;

        public IEnumerable<string> ValidateReferences(int pawnCountForScope)
        {
            if (!ModsConfig.BiotechActive)
            {
                yield return "XenotypeRuleSet declared but Biotech is not active; rule will be ignored.";
                yield break;
            }

            bool needsPool = mode is XenotypeRuleMode.AllowedPool or XenotypeRuleMode.Required
                or XenotypeRuleMode.Fixed or XenotypeRuleMode.Excluded or XenotypeRuleMode.SameForAll;

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
