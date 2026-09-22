using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace JustStart
{
    /// <summary>
    /// Produces starting pawns without exposing the normal reroll/optimisation UI. For plain
    /// vanilla scenarios (no JustStartScenarioExtension, or one with no pawnRoles declared)
    /// this simply calls the scenario's own normal starting-pawn generation once per required
    /// pawn and accepts the result, the same generation vanilla uses - Just Start only skips
    /// showing the player the picking/rerolling screen.
    /// </summary>
    public static class StartingPawnGenerator
    {
        public static List<Pawn> GenerateColonists(ScenarioDef scenarioDef, JustStartScenarioExtension ext, Random rng)
        {
            var colonists = new List<Pawn>();

            var explicitRoles = ext?.pawnRoles?.Where(r => r.faction == PawnRoleFaction.PlayerColonist).ToList();
            if (explicitRoles == null || explicitRoles.Count == 0)
            {
                // No explicit roles declared: defer entirely to vanilla scenario-driven starting
                // pawn generation (ScenPart_ConfigPage_ConfigureStartingPawns etc.), just without
                // presenting the picker/reroll page. StartingPawnUtility already drives this for
                // the normal new-game flow; Just Start's page patch (see Patches/) calls the same
                // generation entry points and skips straight past the UI.
                return colonists; // populated by the page-patch calling vanilla generation directly
            }

            foreach (var role in explicitRoles)
            {
                var xenoAssignment = ModsConfig.BiotechActive
                    ? XenotypeSelector.AssignForRole(role.xenotypeRules ?? ext.xenotypeRules, role.count, rng)
                    : Enumerable.Repeat<XenotypeDef>(null, role.count).ToList();

                for (int i = 0; i < role.count; i++)
                {
                    var request = new PawnGenerationRequest(
                        kind: role.kindDef ?? PawnKindDefOf.Colonist,
                        faction: Faction.OfPlayer,
                        context: PawnGenerationContext.PlayerStarter,
                        forceGenerateNewPawn: true,
                        fixedGender: null);

                    if (xenoAssignment[i] != null)
                        request.ForcedXenotype = xenoAssignment[i];

                    var pawn = PawnGenerator.GeneratePawn(request);

                    if (!role.useDefaultVanillaGear || role.fixedEquipment != null)
                        ApplyFixedEquipment(pawn, role.fixedEquipment);

                    colonists.Add(pawn);
                }
            }

            return colonists;
        }

        public static List<(Pawn pawn, PawnRole role)> GenerateNonColonistRoles(JustStartScenarioExtension ext, Random rng)
        {
            var results = new List<(Pawn, PawnRole)>();
            if (ext?.pawnRoles == null) return results;

            foreach (var role in ext.pawnRoles.Where(r => r.faction != PawnRoleFaction.PlayerColonist))
            {
                var xenoAssignment = ModsConfig.BiotechActive
                    ? XenotypeSelector.AssignForRole(role.xenotypeRules, role.count, rng)
                    : Enumerable.Repeat<XenotypeDef>(null, role.count).ToList();

                Faction faction = role.faction == PawnRoleFaction.HostileOnMap
                    ? Find.FactionManager.RandomEnemyFaction()
                    : Faction.OfPlayer;

                for (int i = 0; i < role.count; i++)
                {
                    var request = new PawnGenerationRequest(
                        kind: role.kindDef,
                        faction: faction,
                        context: PawnGenerationContext.NonPlayer,
                        forceGenerateNewPawn: true);

                    if (xenoAssignment[i] != null)
                        request.ForcedXenotype = xenoAssignment[i];

                    var pawn = PawnGenerator.GeneratePawn(request);

                    if (role.faction == PawnRoleFaction.PlayerPrisoner)
                        pawn.guest?.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);

                    if (!role.useDefaultVanillaGear || role.fixedEquipment != null)
                        ApplyFixedEquipment(pawn, role.fixedEquipment);

                    results.Add((pawn, role));
                }
            }

            return results;
        }

        private static void ApplyFixedEquipment(Pawn pawn, List<ThingDefCountClass> equipment)
        {
            pawn.equipment?.DestroyAllEquipment();
            pawn.apparel?.DestroyAll();
            pawn.inventory?.DestroyAll();

            if (equipment == null) return;

            foreach (var entry in equipment)
            {
                var thing = ThingMaker.MakeThing(entry.thingDef, GenStuff.DefaultStuffFor(entry.thingDef));
                thing.stackCount = entry.count;

                if (thing is Apparel apparel)
                    pawn.apparel.Wear(apparel, false);
                else if (thing.def.IsWeapon && pawn.equipment != null)
                    pawn.equipment.AddEquipment((ThingWithComps)thing);
                else
                    pawn.inventory.innerContainer.TryAdd(thing);
            }
        }
    }
}
