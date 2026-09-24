using System.Linq;
using RimWorld;
using Verse;

namespace JustStart
{
    /// <summary>Ideology-only. Generates the player's ideoligion through vanilla's IdeoGenerator for the chosen mode.</summary>
    public static class IdeologyGenerator
    {
        /// <summary>Mirrors Page_ChooseIdeoPreset.PostOpen, DoClassic and AssignIdeoToPlayer, which are skipped along with the page.</summary>
        public static void ApplyMode(IdeologyMode mode)
        {
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
                return;
            }

            Find.IdeoManager.classicMode = false;
            GenerateMissingFactionIdeos();

            Ideo ideo = mode == IdeologyMode.Inactive
                ? GenerateClassicIdeo(playerDef)
                : GenerateRandomIdeo(playerDef, mode);
            AssignToPlayer(ideo);
            Find.IdeoManager.RemoveUnusedStartingIdeos();
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

        private static Ideo GenerateRandomIdeo(FactionDef playerDef, IdeologyMode mode) =>
            IdeoGenerator.GenerateIdeo(new IdeoGenerationParms(playerDef, forNewFluidIdeo: mode == IdeologyMode.Fluid));

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
