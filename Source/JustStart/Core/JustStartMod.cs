using HarmonyLib;
using UnityEngine;
using Verse;

namespace JustStart
{
    /// <summary>Mod entry point: Harmony init + settings window.</summary>
    public class JustStartMod : Mod
    {
        public static JustStartSettings Settings;

        public JustStartMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<JustStartSettings>();

            var harmony = new Harmony("justart.juststart");
            harmony.PatchAll();
        }

        public override string SettingsCategory() => "Just Start";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled(
                "JustStart.Settings.CuratedRestrictions.Label".Translate(),
                ref Settings.useCuratedVanillaRestrictions,
                "JustStart.Settings.CuratedRestrictions.Tooltip".Translate());

            listing.Gap();
            listing.Label("JustStart.Settings.CuratedRestrictions.Note".Translate());

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }
    }

    public class JustStartSettings : ModSettings
    {
        /// <summary>
        /// OFF by default. When ON, vanilla scenarios that have a curated JustStartScenarioExtension
        /// profile use it to constrain their starting location. When OFF, vanilla scenarios use any
        /// otherwise-valid location. Curated profiles themselves are authored as data (DLC/ScenarioDefs
        /// patches), not hard-coded here - see README "Curated restrictions".
        /// </summary>
        public bool useCuratedVanillaRestrictions = false;

        /// <summary>
        /// Used when a scenario doesn't pin its own IdeologyRuleSet.mode. Only consulted when
        /// Ideology is active.
        /// </summary>
        public IdeologyMode defaultIdeologyMode = IdeologyMode.Inactive;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref useCuratedVanillaRestrictions, "useCuratedVanillaRestrictions", false);
            Scribe_Values.Look(ref defaultIdeologyMode, "defaultIdeologyMode", IdeologyMode.Inactive);
            base.ExposeData();
        }
    }
}
