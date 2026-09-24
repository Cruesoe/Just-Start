using System.Collections.Generic;
using RimWorld;
using Verse;

namespace JustStart
{
    public enum PawnRoleFaction
    {
        /// <summary>A normal starting colonist under the player's faction.</summary>
        PlayerColonist,

        /// <summary>A prisoner held by the player's colony, generated under another faction.</summary>
        PlayerPrisoner,

        /// <summary>Spawns hostile on the starting map, not part of the player's colony (e.g. The Prisoner's guard).</summary>
        HostileOnMap,
    }

    /// <summary>One named group of starting pawns: how many, what kind, their relationship to the player, and their gear and xenotype rules.</summary>
    public class PawnRole
    {
        /// <summary>Identifies this role in logs/errors. Not shown to the player.</summary>
        public string id = string.Empty;

        /// <summary>Pawn kind to generate. Optional for PlayerColonist/PlayerPrisoner (defaults to the faction's normal kind); required for HostileOnMap.</summary>
        public PawnKindDef? kindDef;

        /// <summary>How many pawns this role generates. Ignored if countRange is set.</summary>
        public int count = 1;

        /// <summary>If set, replaces count with a random number in this range (inclusive), rolled at game start.</summary>
        public IntRange? countRange;

        public PawnRoleFaction faction = PawnRoleFaction.PlayerColonist;

        /// <summary>
        /// HostileOnMap and PlayerPrisoner only: the faction to generate under. Defaults to the kind's own faction if it suits,
        /// else a random hostile (HostileOnMap) or non-player (PlayerPrisoner) humanlike faction.
        /// </summary>
        public FactionDef? factionDef;

        public XenotypeRuleSet? xenotypeRules;

        /// <summary>Items given to each pawn: worn or equipped where the slot is free, otherwise carried.</summary>
        public List<ThingDefCountClass>? fixedEquipment;

        /// <summary>False strips vanilla-generated gear and starting possessions, leaving only fixedEquipment; true adds fixedEquipment on top.</summary>
        public bool useDefaultVanillaGear = true;

        /// <summary>One of these is picked at random and replaces any weapon, after fixedEquipment.</summary>
        public List<ThingDefCountClass>? weaponOptions;

        /// <summary>
        /// Non-colonist roles: distance in cells from the player's start spot. Defaults to 30~45 for
        /// HostileOnMap and 0~10 otherwise. Colonists are placed by the scenario's arrival method.
        /// </summary>
        public IntRange? spawnDistance;

        /// <summary>HostileOnMap only: never gives up or leaves the map.</summary>
        public bool fightToTheDeath;

        /// <summary>HostileOnMap only: may carry off a downed colonist.</summary>
        public bool canKidnap;

        /// <summary>HostileOnMap only: may grab valuables and leave.</summary>
        public bool canSteal;

        /// <summary>HostileOnMap only: may pick up weapons lying on the map.</summary>
        public bool canPickUpWeapons;

        public bool mustBeCapableOfViolence;

        public Gender? gender;

        public FloatRange? biologicalAgeRange;

        public int MinCount => countRange?.min ?? count;

        public IntRange SpawnDistance =>
            spawnDistance ?? (faction == PawnRoleFaction.HostileOnMap ? new IntRange(30, 45) : new IntRange(0, 10));

        public int ResolveCount() => countRange?.RandomInRange ?? count;

        public void ApplyConstraints(ref PawnGenerationRequest request)
        {
            if (mustBeCapableOfViolence)
                request.MustBeCapableOfViolence = true;
            if (gender.HasValue)
                request.FixedGender = gender;
            if (biologicalAgeRange.HasValue)
            {
                request.BiologicalAgeRange = biologicalAgeRange;
                request.ExcludeBiologicalAgeRange = null;
            }
        }

        public IEnumerable<string> ValidateReferences()
        {
            if (countRange is IntRange range)
            {
                if (range.min <= 0 || range.max < range.min)
                    yield return $"PawnRole '{id}' has an invalid countRange ({range}).";
            }
            else if (count <= 0)
                yield return $"PawnRole '{id}' has non-positive count ({count}).";

            if (spawnDistance is IntRange distance && (distance.min < 0 || distance.max < distance.min))
                yield return $"PawnRole '{id}' has an invalid spawnDistance ({distance}).";

            if (biologicalAgeRange is FloatRange age && (age.min < 0f || age.max < age.min))
                yield return $"PawnRole '{id}' has an invalid biologicalAgeRange ({age}).";

            if (faction == PawnRoleFaction.HostileOnMap && kindDef == null)
                yield return $"PawnRole '{id}' is HostileOnMap but declares no kindDef; a PawnKindDef is required to generate a hostile pawn.";

            if (factionDef != null && faction == PawnRoleFaction.PlayerColonist)
                yield return $"PawnRole '{id}' declares factionDef, which only applies to HostileOnMap and PlayerPrisoner roles.";

            if (weaponOptions != null && weaponOptions.Exists(w => w.thingDef == null || !w.thingDef.IsWeapon))
                yield return $"PawnRole '{id}' has a weaponOptions entry that is not a weapon.";

            if (xenotypeRules != null)
                foreach (var msg in xenotypeRules.ValidateReferences(MinCount))
                    yield return $"PawnRole '{id}': {msg}";
        }
    }
}
