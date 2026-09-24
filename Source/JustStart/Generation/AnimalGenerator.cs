using System.Collections.Generic;
using RimWorld;
using Verse;

namespace JustStart
{
    /// <summary>Generates starting animals, putting carriedInventory in each animal's own inventory rather than on the ground.</summary>
    public static class AnimalGenerator
    {
        public static List<(Pawn pawn, AnimalRole role)> Generate(JustStartScenarioExtension? ext)
        {
            var pawns = new List<(Pawn, AnimalRole)>();
            if (ext?.animalRoles == null) return pawns;

            foreach (var role in ext.animalRoles)
            {
                PawnKindDef? species = role.ResolveSpecies();
                if (species == null)
                {
                    Log.Error($"[JustStart] AnimalRole '{role.id}' could not resolve a species; skipping.");
                    continue;
                }

                for (int i = 0; i < role.count; i++)
                {
                    var request = new PawnGenerationRequest(
                        kind: species,
                        faction: Faction.OfPlayer,
                        context: PawnGenerationContext.PlayerStarter,
                        forceGenerateNewPawn: true);

                    Pawn animal = PawnGenerator.GeneratePawn(request);

                    if (role.carriedInventory != null)
                        foreach (var entry in role.carriedInventory)
                            foreach (Thing thing in ThingFactory.Make(entry))
                                animal.inventory.innerContainer.TryAdd(thing);

                    pawns.Add((animal, role));
                }
            }

            return pawns;
        }
    }
}
