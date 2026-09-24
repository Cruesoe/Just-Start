using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace JustStart
{
    /// <summary>Mod entry point: Harmony init + settings window.</summary>
    public class JustStartMod : Mod
    {
        public static JustStartMod Instance = null!;

        public static JustStartSettings Settings = null!;

        public JustStartMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<JustStartSettings>();

            var harmony = new Harmony("cruesoe.juststart");
            harmony.PatchAll();
        }

        public override string SettingsCategory() => "Just Start";

        private static string ModeLabel(IdeologyMode mode) => ("JustStart_IdeologyMode" + mode).Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled(
                "JustStart_SettingsCuratedRestrictionsLabel".Translate(),
                ref Settings.useCuratedVanillaRestrictions,
                "JustStart_SettingsCuratedRestrictionsTooltip".Translate());

            listing.Gap();
            listing.Label("JustStart_SettingsCuratedRestrictionsNote".Translate());

            listing.GapLine();
            Rect row = listing.GetRect(30f);
            Widgets.Label(row.LeftHalf(), "JustStart_SettingsDefaultIdeologyLabel".Translate());
            if (ModsConfig.IdeologyActive)
            {
                TooltipHandler.TipRegion(row, "JustStart_SettingsDefaultIdeologyTooltip".Translate());
                if (Widgets.ButtonText(row.RightHalf(), ModeLabel(Settings.defaultIdeologyMode)))
                {
                    var options = new List<FloatMenuOption>();
                    foreach (IdeologyMode mode in Enum.GetValues(typeof(IdeologyMode)))
                        options.Add(new FloatMenuOption(ModeLabel(mode), () => Settings.defaultIdeologyMode = mode));
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }
            else
            {
                TooltipHandler.TipRegion(row, "JustStart_SettingsDefaultIdeologyRequiresIdeology".Translate());
                Widgets.ButtonText(row.RightHalf(), ModeLabel(Settings.defaultIdeologyMode), active: false);
            }

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            ScenarioRulesSummary.ClearCache();
        }
    }

    public class JustStartSettings : ModSettings
    {
        /// <summary>When on, extensions marked curatedRestrictionOnly apply their tile constraints. Off by default.</summary>
        public bool useCuratedVanillaRestrictions = false;

        /// <summary>Ideoligion mode for scenarios that set none; only used with Ideology active.</summary>
        public IdeologyMode defaultIdeologyMode = IdeologyMode.Fixed;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref useCuratedVanillaRestrictions, "useCuratedVanillaRestrictions", false);
            Scribe_Values.Look(ref defaultIdeologyMode, "defaultIdeologyMode", IdeologyMode.Fixed);
            base.ExposeData();
        }
    }
}
