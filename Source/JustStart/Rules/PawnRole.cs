using System.Collections.Generic;
using Verse;

namespace JustStart
{
    public enum PawnRoleFaction
    {
        /// <summary>A normal starting colonist under the player's faction.</summary>
        PlayerColonist,

        /// <summary>A prisoner belonging to the player's faction (e.g. The Prisoner).</summary>
        PlayerPrisoner,

        /// <summary>Spawns hostile on the starting map, not part of the player's colony (e.g. The Prisoner's guard).</summary>
        HostileOnMap,
    }

    /// <summary>
    /// One named group of starting pawns within a scenario: how many, what kind, what faction
    /// relationship to the player, and optional per-role xenotype/equipment overrides. A scenario
    /// with multiple roles (The Prisoner: one PlayerPrisoner + one HostileOnMap) declares a list
    /// of these instead of Just Start special-casing that scenario by name.
    /// </summary>
    public class PawnRole
    {
        public string id;
        public PawnKindDef kindDef;
        public int count = 1;
        public PawnRoleFaction faction = PawnRoleFaction.PlayerColonist;
        public XenotypeRuleSet xenotypeRules;

        /// <summary>
        /// If set, replaces normal scenario-part/pawnkind starting gear for this role with exactly
        /// this list. Used by e.g. I Got This ("one pistol") and The Prisoner's guard loadout.
        /// </summary>
        public List<ThingDefCountClass> fixedEquipment;

        /// <summary>
        /// Relative equipment quality for this role when fixedEquipment is not set, expressed the
        /// same way vanilla scenario parts do (e.g. ScenPart_StartingThing equivalents upstream);
        /// left null to use normal vanilla-default generation for the role's kind.
        /// </summary>
        public bool useDefaultVanillaGear = true;

        public IEnumerable<string> ValidateReferences()
        {
            if (count <= 0)
                yield return $"PawnRole '{id}' has non-positive count ({count}).";

            if (faction == PawnRoleFaction.HostileOnMap && kindDef == null)
                yield return $"PawnRole '{id}' is HostileOnMap but declares no kindDef; a PawnKindDef is required to generate a hostile pawn.";

            if (xenotypeRules != null)
                foreach (var msg in xenotypeRules.ValidateReferences(count))
                    yield return $"PawnRole '{id}': {msg}";
        }
    }
}
