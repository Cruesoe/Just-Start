using System.Linq;
using RimWorld;
using Verse;

namespace JustStart
{
    /// <summary>Ideology-only. Generates the player's ideoligion through vanilla's IdeoGenerator for the chosen mode and meme/precept rules.</summary>
    public static class IdeologyGenerator
    {
        /// <summary>
        /// Mirrors Page_ChooseIdeoPreset.PostOpen, DoClassic and AssignIdeoToPlayer, which are skipped along with the page.
        /// Returns false if no ideoligion meeting the scenario's meme/precept rules could be generated.
        /// </summary>
        public static bool ApplyMode(IdeologyMode mode, IdeologyRuleSet? rules)
        {
            if (!ModsConfig.IdeologyActive)
                return true;

            FactionDef playerDef = Find.FactionManager.OfPlayer.def;

            if (mode == IdeologyMode.Classic)
            {
                Ideo classic = GenerateClassicIdeo(playerDef);
                AssignToPlayer(classic);
                foreach (Faction faction in Find.FactionManager.AllFactions)
                {
                    if (faction.ideos == null)
                        continue;
                    faction.ideos.RemoveAll();
                    faction.ideos.SetPrimary(classic);
                }
                Find.IdeoManager.classicMode = true;
                Find.IdeoManager.RemoveUnusedStartingIdeos();
                return true;
            }

            Find.IdeoManager.classicMode = false;
            GenerateMissingFactionIdeos();

            Ideo? ideo = mode == IdeologyMode.Inactive
                ? GenerateClassicIdeo(playerDef)
                : GenerateRuledIdeo(playerDef, mode, rules);
            if (ideo == null)
                return false;

            AssignToPlayer(ideo);
            Find.IdeoManager.RemoveUnusedStartingIdeos();
            return true;
        }

        private static void GenerateMissingFactionIdeos()
        {
            foreach (Faction faction in Find.FactionManager.AllFactions)
            {
                if (faction == Faction.OfPlayer || faction.ideos == null || !faction.ideos.PrimaryIdeo.memes.NullOrEmpty())
                    continue;

                FactionDef def = faction.def;
                if (def.fixedIdeo)
                    faction.ideos.ChooseOrGenerateIdeo(new IdeoGenerationParms(def, forceNoExpansionIdeo: false, null, null, name: def.ideoName, styles: def.styles, deities: def.deityPresets, hidden: def.hiddenIdeo, description: def.ideoDescription, forcedMemes: def.forcedMemes, classicExtra: false, forceNoWeaponPreference: false, forNewFluidIdeo: false, fixedIdeo: true, requiredPreceptsOnly: def.requiredPreceptsOnly));
                else
                    faction.ideos.ChooseOrGenerateIdeo(new IdeoGenerationParms(def));
            }
        }

        // Vanilla's preset page starts the player on a classic ideo of a random allowed culture.
        private static Ideo GenerateClassicIdeo(FactionDef playerDef)
        {
            if (!DefDatabase<CultureDef>.AllDefs.Where(c => playerDef.allowedCultures.Contains(c)).TryRandomElement(out CultureDef culture))
                culture = DefDatabase<CultureDef>.AllDefs.RandomElement();
            return IdeoGenerator.GenerateClassicIdeo(culture, new IdeoGenerationParms(playerDef), noExpansionIdeo: false);
        }

        // disallowedMemes only filters vanilla's random normal memes, so every rule is re-checked here and retried.
        private static Ideo? GenerateRuledIdeo(FactionDef playerDef, IdeologyMode mode, IdeologyRuleSet? rules)
        {
            var parms = new IdeoGenerationParms(
                playerDef,
                forceNoExpansionIdeo: false,
                disallowedPrecepts: rules?.disallowedPrecepts,
                disallowedMemes: rules?.disallowedMemes,
                forcedMemes: rules != null && !rules.forcedMemes.NullOrEmpty() ? rules.forcedMemes : null,
                forceNoWeaponPreference: false,
                forNewFluidIdeo: mode == IdeologyMode.Fluid);

            for (int i = 0; i < (rules?.generationAttempts ?? 50); i++)
            {
                Ideo ideo = IdeoGenerator.GenerateIdeo(parms);
                if (MeetsRules(ideo, rules))
                    return ideo;
            }
            return null;
        }

        private static bool MeetsRules(Ideo ideo, IdeologyRuleSet? rules)
        {
            if (rules == null)
                return true;
            if (rules.forcedMemes != null && !rules.forcedMemes.All(ideo.memes.Contains))
                return false;
            if (rules.disallowedMemes != null && ideo.memes.Any(rules.disallowedMemes.Contains))
                return false;
            if (rules.disallowedPrecepts != null && ideo.PreceptsListForReading.Any(p => rules.disallowedPrecepts.Contains(p.def)))
                return false;
            return true;
        }

        private static void AssignToPlayer(Ideo ideo)
        {
            Faction.OfPlayer.ideos.SetPrimary(ideo);
            foreach (Ideo other in Find.IdeoManager.IdeosListForReading)
                other.initialPlayerIdeo = false;
            ideo.initialPlayerIdeo = true;
            Find.IdeoManager.Add(ideo);
        }
    }
}
