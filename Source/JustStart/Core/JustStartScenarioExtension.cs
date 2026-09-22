using System.Collections.Generic;
using Verse;

namespace JustStart
{
    /// <summary>
    /// Public XML extension point. Third-party scenario authors attach this as a
    /// &lt;modExtensions&gt; entry on a &lt;ScenarioDef&gt; to opt that scenario into the Just
    /// Start framework and declare its constraints. Every field is optional and defaults to
    /// vanilla/unrestricted behaviour - an extension with no fields set still lets Just Start
    /// auto-pick a valid tile and auto-generate pawns per the scenario's own normal rules.
    /// </summary>
    public class JustStartScenarioExtension : DefModExtension
    {
        /// <summary>
        /// If true, this extension's tileConstraints only apply when the player has enabled
        /// "use curated vanilla restrictions" in mod settings. Used for optional curated
        /// profiles layered onto vanilla scenarios (Section 4). Custom scenarios that ship
        /// their own ScenarioDef should leave this false so their constraints always apply.
        /// </summary>
        public bool curatedRestrictionOnly = false;

        public List<TileConstraint> tileConstraints;

        public List<PawnRole> pawnRoles;

        /// <summary>Scenario-wide xenotype rule applied to PlayerColonist roles that don't declare their own.</summary>
        public XenotypeRuleSet xenotypeRules;

        public IdeologyRuleSet ideologyRules;

        public List<AnimalRole> animalRoles;

        public IEnumerable<string> ValidateReferences(ScenarioDef scenarioDef)
        {
            if (tileConstraints != null)
                foreach (var c in tileConstraints)
                    foreach (var msg in c.ValidateReferences())
                        yield return $"[{scenarioDef.defName}] {msg}";

            int totalColonistCount = 0;
            if (pawnRoles != null)
            {
                foreach (var role in pawnRoles)
                {
                    foreach (var msg in role.ValidateReferences())
                        yield return $"[{scenarioDef.defName}] {msg}";
                    if (role.faction == PawnRoleFaction.PlayerColonist)
                        totalColonistCount += role.count;
                }
            }

            if (xenotypeRules != null)
                foreach (var msg in xenotypeRules.ValidateReferences(totalColonistCount))
                    yield return $"[{scenarioDef.defName}] {msg}";

            if (animalRoles != null)
                foreach (var role in animalRoles)
                    foreach (var msg in role.ValidateReferences())
                        yield return $"[{scenarioDef.defName}] {msg}";

            if (ideologyRules?.mode == IdeologyMode.Fixed || ideologyRules?.mode == IdeologyMode.Fluid)
            {
                if (!ModsConfig.IdeologyActive)
                    yield return $"[{scenarioDef.defName}] IdeologyRuleSet mode {ideologyRules.mode} declared but Ideology DLC is not active; will be ignored.";
            }
        }
    }
}
