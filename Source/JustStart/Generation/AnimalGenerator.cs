using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace JustStart
{
    /// <summary>
    /// Generates starting animals and assigns carried inventory into the animal's own
    /// ThingOwner (Pawn.inventory), matching Section 12's requirement that supplies are
    /// carried by the pack animal rather than merely spawned on the ground.
    /// </summary>
    public static class AnimalGenerator
    {
        public static List<Pawn> Generate(JustStartScenarioExtension ext, Random rng)
        {
            var pawns = new List<Pawn>();
            if (ext?.animalRoles == null) return pawns;

            foreach (var role in ext.animalRoles)
            {
                var species = role.ResolveSpecies(rng);
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

                    var animal = PawnGenerator.GeneratePawn(request);

                    if (role.carriedInventory != null)
                    {
                        foreach (var entry in role.carriedInventory)
                        {
                            var thing = ThingMaker.MakeThing(entry.thingDef, GenStuff.DefaultStuffFor(entry.thingDef));
                            thing.stackCount = entry.count;
                            animal.inventory.innerContainer.TryAdd(thing);
                        }
                    }

                    pawns.Add(animal);
                }
            }

            return pawns;
        }
    }
}
