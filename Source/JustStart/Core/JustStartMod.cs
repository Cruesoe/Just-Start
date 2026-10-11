using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
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
            listing.ColumnWidth = Mathf.Min(inRect.width, SettingsColumnWidth);

            Heading(listing, "JustStart_SettingsSectionSite".Translate());
            listing.CheckboxLabeled(
                "JustStart_SettingsExcludeExtremeLabel".Translate(),
                ref Settings.excludeExtremeBiomes,
                "JustStart_SettingsExcludeExtremeTooltip".Translate(ExtremeBiomeList));

            Heading(listing, "JustStart_SettingsSectionIdeoligion".Translate());
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

            Heading(listing, "JustStart_SettingsSectionColonists".Translate());
            bool biotech = ModsConfig.BiotechActive;
            Rect xenotypeRow = listing.GetRect(30f);
            Widgets.Label(xenotypeRow.LeftHalf(), "JustStart_SettingsXenotypeLabel".Translate());
            TooltipHandler.TipRegion(xenotypeRow, biotech
                ? "JustStart_SettingsXenotypeTooltip".Translate()
                : "JustStart_SettingsXenotypeRequiresBiotech".Translate());
            if (StartingXenotype.DrawButton(xenotypeRow.RightHalf(), biotech))
                StartingXenotype.OpenMenu();

            listing.Gap(4f);
            Rect customRow = listing.GetRect(Text.LineHeight);
            customRow.xMin += IndentWidth;
            bool customActive = biotech && Settings.xenotypeMode == StartingXenotypeMode.Mixed;
            Widgets.CheckboxLabeled(customRow, "JustStart_SettingsMixedCustomLabel".Translate(), ref Settings.mixedIncludesCustom, disabled: !customActive);
            TooltipHandler.TipRegion(customRow, !biotech
                ? "JustStart_SettingsXenotypeRequiresBiotech".Translate()
                : customActive
                    ? "JustStart_SettingsMixedCustomTooltip".Translate()
                    : "JustStart_SettingsMixedCustomRequiresMixed".Translate());

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        private const float SettingsColumnWidth = 640f;

        private const float IndentWidth = 24f;

        private static void Heading(Listing_Standard listing, string label)
        {
            if (listing.CurHeight > 0f)
                listing.Gap(18f);
            Text.Font = GameFont.Medium;
            listing.Label(label);
            Text.Font = GameFont.Small;
            listing.GapLine(6f);
        }

        // Every loaded biome the game warns about when settling there, including other mods' biomes; built once, as Defs don't change after load.
        private static string? extremeBiomeList;

        private static string ExtremeBiomeList =>
            extremeBiomeList ??= DefDatabase<BiomeDef>.AllDefs.Where(TileSelector.IsExtreme).Select(b => b.LabelCap.Resolve()).ToLineList("  - ");
    }
}
