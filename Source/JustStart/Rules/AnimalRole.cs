using System.Collections.Generic;
using System.Linq;
using Verse;

namespace JustStart
{
    /// <summary>Declares starting animals for a scenario; carriedInventory goes into each animal's own inventory.</summary>
    public class AnimalRole
    {
        /// <summary>Identifies this role in logs/errors. Not shown to the player.</summary>
        public string id = string.Empty;

        /// <summary>
        /// Species pool to pick from at random. Ignored if fixedSpecies is set. Left empty, it is every pack animal
        /// when requirePackAnimal is set, else every animal vanilla would pick as a random pet (petness above 0).
        /// </summary>
        public List<PawnKindDef>? allowedSpecies;

        /// <summary>If set, always this species instead of picking from allowedSpecies.</summary>
        public PawnKindDef? fixedSpecies;

        /// <summary>How many animals this role generates.</summary>
        public int count = 1;

        /// <summary>Restricts the species pool to pack animals (RaceProps.packAnimal).</summary>
        public bool requirePackAnimal = false;

        /// <summary>Items placed in the animal's own inventory (e.g. supplies a pack animal carries), not spawned loose.</summary>
        public List<ThingDefCountClass>? carriedInventory;

        /// <summary>Distance in cells from the player's start spot. Defaults to 0~10.</summary>
        public IntRange? spawnDistance;

        public IntRange SpawnDistance => spawnDistance ?? new IntRange(0, 10);

        private static List<PawnKindDef>? allAnimals;

        private static List<PawnKindDef> AllAnimals =>
            allAnimals ??= DefDatabase<PawnKindDef>.AllDefsListForReading.Where(k => k.RaceProps?.Animal == true).ToList();

        public PawnKindDef? ResolveSpecies()
        {
            if (fixedSpecies != null) return fixedSpecies;

            IEnumerable<PawnKindDef> pool;
            if (!allowedSpecies.NullOrEmpty())
                pool = requirePackAnimal ? allowedSpecies!.Where(k => k.RaceProps?.packAnimal == true) : allowedSpecies!;
            else
                pool = requirePackAnimal
                    ? AllAnimals.Where(k => k.RaceProps.packAnimal)
                    : AllAnimals.Where(k => k.RaceProps.petness > 0f);

            return pool.RandomElementWithFallback();
        }

        public IEnumerable<string> ValidateReferences()
        {
            if (count <= 0)
                yield return $"AnimalRole '{id}' has non-positive count ({count}).";

            if (spawnDistance is IntRange distance && (distance.min < 0 || distance.max < distance.min))
                yield return $"AnimalRole '{id}' has an invalid spawnDistance ({distance}).";

            if (fixedSpecies == null && requirePackAnimal
                && (allowedSpecies.NullOrEmpty() || !allowedSpecies!.Any(k => k.RaceProps?.packAnimal == true))
                && !AllAnimals.Any(k => k.RaceProps.packAnimal))
                yield return $"AnimalRole '{id}' requires a pack animal but no PawnKindDef with RaceProps.packAnimal is loaded.";

            if (fixedSpecies != null && requirePackAnimal && fixedSpecies.RaceProps?.packAnimal != true)
                yield return $"AnimalRole '{id}' has fixedSpecies '{fixedSpecies.defName}' which is not a pack animal, but requirePackAnimal is true.";
        }
    }
}
