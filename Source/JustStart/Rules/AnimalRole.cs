using System.Collections.Generic;
using System.Linq;
using Verse;

namespace JustStart
{
    /// <summary>
    /// Declares starting animals for a scenario (Mountain Dwellers' pack animal). Prefers vanilla
    /// animal/inventory systems: carriedInventory is assigned into the generated Pawn's inventory
    /// (ThingOwner), not spawned loose on the map.
    /// </summary>
    public class AnimalRole
    {
        public string id;
        public List<PawnKindDef> allowedSpecies;
        public PawnKindDef fixedSpecies;
        public int count = 1;
        public bool requirePackAnimal = false;
        public List<ThingDefCountClass> carriedInventory;

        public PawnKindDef ResolveSpecies(System.Random rng)
        {
            if (fixedSpecies != null) return fixedSpecies;

            IEnumerable<PawnKindDef> pool = allowedSpecies != null && allowedSpecies.Count > 0
                ? allowedSpecies
                : DefDatabase<PawnKindDef>.AllDefsListForReading.Where(k => k.RaceProps?.Animal == true);

            if (requirePackAnimal)
                pool = pool.Where(k => k.RaceProps != null && k.RaceProps.packAnimal);

            var list = pool.ToList();
            return list.Count == 0 ? null : list[rng.Next(list.Count)];
        }

        public IEnumerable<string> ValidateReferences()
        {
            if (count <= 0)
                yield return $"AnimalRole '{id}' has non-positive count ({count}).";

            if (fixedSpecies == null && requirePackAnimal
                && (allowedSpecies == null || allowedSpecies.Count == 0 || !allowedSpecies.Any(k => k.RaceProps != null && k.RaceProps.packAnimal)))
            {
                bool anyPackAnimalExists = DefDatabase<PawnKindDef>.AllDefsListForReading
                    .Any(k => k.RaceProps != null && k.RaceProps.Animal && k.RaceProps.packAnimal);
                if (!anyPackAnimalExists)
                    yield return $"AnimalRole '{id}' requires a pack animal but no PawnKindDef with RaceProps.packAnimal is loaded.";
            }

            if (fixedSpecies != null && requirePackAnimal && fixedSpecies.RaceProps?.packAnimal != true)
                yield return $"AnimalRole '{id}' has fixedSpecies '{fixedSpecies.defName}' which is not a pack animal, but requirePackAnimal is true.";
        }
    }
}
