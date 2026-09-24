using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace JustStart
{
    /// <summary>
    /// Generates the scenario's pawn roles once each, with no reroll screen. Without a PlayerColonist role the
    /// scenario's own starting pawns are kept.
    /// </summary>
    public static class StartingPawnGenerator
    {
        private static readonly Func<PawnGenerationRequest> DefaultStartingPawnRequest =
            AccessTools.MethodDelegate<Func<PawnGenerationRequest>>(
                AccessTools.PropertyGetter(typeof(StartingPawnUtility), "DefaultStartingPawnRequest"));

        public static List<Pawn> GenerateColonists(JustStartScenarioExtension? ext)
        {
            var colonists = new List<Pawn>();
            var roles = ext?.pawnRoles?.Where(r => r.faction == PawnRoleFaction.PlayerColonist).ToList();
            if (ext == null || roles == null || roles.Count == 0)
                return colonists;

            // Discards the scenario's own pawns from Scenario.PostIdeoChosen, with their families and possessions.
            StartingPawnUtility.ClearAllStartingPawns();

            var counts = roles.Select(r => r.ResolveCount()).ToList();
            var xenotypes = AssignColonistXenotypes(roles, counts, ext.xenotypeRules);

            for (int r = 0; r < roles.Count; r++)
            {
                PawnRole role = roles[r];
                for (int i = 0; i < counts[r]; i++)
                {
                    // Vanilla's request carries the player faction's own pawn kind and any mod patches to it.
                    PawnGenerationRequest request = DefaultStartingPawnRequest();
                    if (role.kindDef != null)
                        request.KindDef = role.kindDef;
                    Pawn pawn = Generate(request, role, xenotypes[r][i]);

                    // Mirrors StartingPawnUtility.NewGeneratedStartingPawn; map gen reads startingPossessions[pawn] for every starting pawn.
                    pawn.relations.everSeenByPlayer = true;
                    PawnComponentsUtility.AddComponentsForSpawn(pawn);
                    StartingPawnUtility.GeneratePossessions(pawn);
                    if (!role.useDefaultVanillaGear)
                        Find.GameInitData.startingPossessions[pawn].Clear();

                    ApplyGear(pawn, role);
                    colonists.Add(pawn);
                }
            }

            return colonists;
        }

        public static List<(Pawn pawn, PawnRole role)> GenerateNonColonistRoles(JustStartScenarioExtension? ext)
        {
            var results = new List<(Pawn, PawnRole)>();
            if (ext?.pawnRoles == null) return results;

            foreach (var role in ext.pawnRoles.Where(r => r.faction != PawnRoleFaction.PlayerColonist))
            {
                Faction? faction = FactionFor(role);
                if (faction == null)
                {
                    Log.Warning($"[JustStart] PawnRole '{role.id}' has no suitable faction in this world; skipping it.");
                    continue;
                }

                int count = role.ResolveCount();
                var xenotypes = XenotypeSelector.Assign(role.xenotypeRules, count);
                for (int i = 0; i < count; i++)
                {
                    var request = new PawnGenerationRequest(
                        kind: role.kindDef ?? faction.def.basicMemberKind,
                        faction: faction,
                        context: PawnGenerationContext.NonPlayer,
                        forceGenerateNewPawn: true);
                    Pawn pawn = Generate(request, role, xenotypes[i]);
                    ApplyGear(pawn, role);
                    results.Add((pawn, role));
                }
            }

            return results;
        }

        // Roles with their own rule resolve it alone; the rest share one assignment from the scenario-wide rule.
        private static List<List<XenotypeDef?>> AssignColonistXenotypes(List<PawnRole> roles, List<int> counts, XenotypeRuleSet? scenarioRule)
        {
            int sharedCount = Enumerable.Range(0, roles.Count).Where(r => roles[r].xenotypeRules == null).Sum(r => counts[r]);
            List<XenotypeDef?> shared = XenotypeSelector.Assign(scenarioRule, sharedCount);

            var result = new List<List<XenotypeDef?>>(roles.Count);
            int next = 0;
            for (int r = 0; r < roles.Count; r++)
            {
                if (roles[r].xenotypeRules != null)
                    result.Add(XenotypeSelector.Assign(roles[r].xenotypeRules, counts[r]));
                else
                {
                    result.Add(shared.GetRange(next, counts[r]));
                    next += counts[r];
                }
            }
            return result;
        }

        private static Pawn Generate(PawnGenerationRequest request, PawnRole role, XenotypeDef? xenotype)
        {
            role.ApplyConstraints(ref request);
            if (xenotype != null)
                request.ForcedXenotype = xenotype;
            return PawnGenerator.GeneratePawn(request);
        }

        // The role's factionDef, else the kind's own faction, if it suits; otherwise a random hostile (HostileOnMap) or non-player (PlayerPrisoner) humanlike faction.
        private static Faction? FactionFor(PawnRole role)
        {
            bool hostile = role.faction == PawnRoleFaction.HostileOnMap;
            bool Suits(Faction f) => !f.IsPlayer && !f.defeated && f.def.humanlikeFaction && (!hostile || f.HostileTo(Faction.OfPlayer));

            FactionDef? def = role.factionDef ?? role.kindDef?.defaultFactionDef;
            Faction? own = def != null ? Find.FactionManager.FirstFactionOfDef(def) : null;
            if (own != null && Suits(own))
                return own;
            return Find.FactionManager.AllFactionsVisible.Where(Suits).RandomElementWithFallback();
        }

        // Without default gear the pawn keeps only fixedEquipment; with it, fixedEquipment is added on top. A weaponOptions pick then replaces any weapon.
        private static void ApplyGear(Pawn pawn, PawnRole role)
        {
            if (!role.useDefaultVanillaGear)
            {
                pawn.equipment?.DestroyAllEquipment();
                pawn.apparel?.DestroyAll();
                pawn.inventory?.DestroyAll();
            }

            if (role.fixedEquipment != null)
                foreach (var entry in role.fixedEquipment)
                    foreach (Thing thing in ThingFactory.Make(entry))
                        Give(pawn, thing);

            if (!role.weaponOptions.NullOrEmpty() && pawn.equipment != null)
            {
                var things = ThingFactory.Make(role.weaponOptions!.RandomElement());
                if (things.Count > 0)
                {
                    pawn.equipment.DestroyAllEquipment();
                    foreach (Thing thing in things)
                        Give(pawn, thing);
                }
            }
        }

        // Worn or equipped when the slot is free, otherwise carried in inventory.
        private static void Give(Pawn pawn, Thing thing)
        {
            if (thing is Apparel apparel && pawn.apparel != null && pawn.apparel.CanWearWithoutDroppingAnything(apparel.def))
                pawn.apparel.Wear(apparel, dropReplacedApparel: false);
            else if (thing.def.IsWeapon && thing is ThingWithComps weapon && pawn.equipment != null && pawn.equipment.Primary == null)
                pawn.equipment.AddEquipment(weapon);
            else
                pawn.inventory?.innerContainer.TryAdd(thing);
        }
    }
}
