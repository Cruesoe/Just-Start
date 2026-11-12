using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace JustStart
{
    /// <summary>
    /// The Just Start settings page: starting site and ideoligion on the left, colonists on the right. Every option is a
    /// labelled row with its control in the right part of the column; options that need a DLC or another setting are greyed out.
    /// </summary>
    public static class SettingsPage
    {
        private const float ColumnGap = 34f;
        private const float LabelPct = 0.5f;
        private const float RowHeight = 30f;
        private const float SliderRowHeight = 32f;
        private const float GroupGap = 10f;
        private const float IndentWidth = 24f;

        private const int TemperatureSliderId = 0x4A53_5450;
        private const int AgeSliderId = 0x4A53_4147;

        private static JustStartSettings Settings => JustStartMod.Settings;

        private static readonly Listing_Standard LeftListing = new Listing_Standard();
        private static readonly Listing_Standard RightListing = new Listing_Standard();

        private static Vector2 scroll;
        private static float contentHeight;

        public static void Draw(Rect inRect)
        {
            RefreshTextIfLanguageChanged();

            Rect viewRect = new Rect(0f, 0f, inRect.width, Mathf.Max(contentHeight, inRect.height));
            bool scrolls = contentHeight > inRect.height;
            if (scrolls)
                viewRect.width -= 16f;
            Widgets.BeginScrollView(inRect, ref scroll, viewRect, showScrollbars: scrolls);

            float columnWidth = (viewRect.width - ColumnGap) / 2f;
            float left = DrawLeftColumn(new Rect(0f, 0f, columnWidth, 99999f));
            float right = DrawRightColumn(new Rect(columnWidth + ColumnGap, 0f, columnWidth, 99999f));
            contentHeight = Mathf.Max(left, right);

            Widgets.EndScrollView();
        }

        private static float DrawLeftColumn(Rect rect)
        {
            Listing_Standard listing = LeftListing;
            listing.Begin(rect);

            Heading(listing, Tr("JustStart_SettingsSectionSite"));
            CheckboxWithTip(listing, "JustStart_SettingsExcludeExtremeLabel", ref Settings.excludeExtremeBiomes, ExtremeBiomeTooltip);

            listing.Gap(GroupGap);
            SubLabel(listing, "JustStart_SettingsTerrainLabel", "JustStart_SettingsTerrainTooltip");
            CheckboxPair(listing,
                "JustStart_SettingsTerrainFlat", ref Settings.allowFlat,
                "JustStart_SettingsTerrainSmallHills", ref Settings.allowSmallHills);
            CheckboxPair(listing,
                "JustStart_SettingsTerrainLargeHills", ref Settings.allowLargeHills,
                "JustStart_SettingsTerrainMountainous", ref Settings.allowMountainous);

            listing.Gap(GroupGap);
            SubLabel(listing, "JustStart_SettingsFeaturesLabel", "JustStart_SettingsFeaturesTooltip");
            CheckboxTriple(listing,
                "JustStart_SettingsFeatureRiver", ref Settings.requireRiver,
                "JustStart_SettingsFeatureCoast", ref Settings.requireCoast,
                "JustStart_SettingsFeatureRoad", ref Settings.requireRoad);

            listing.Gap(GroupGap);
            Settings.minGrowingDays = SteppedSlider(listing, "JustStart_SettingsGrowingLabel", "JustStart_SettingsGrowingTooltip",
                Settings.minGrowingDays, 0, TileSelector.MaxGrowingDays, 5, GrowingValue);
            TemperatureRow(listing);
            Settings.minHostileDistance = SteppedSlider(listing, "JustStart_SettingsHostileLabel", "JustStart_SettingsHostileTooltip",
                Settings.minHostileDistance, 0, TileSelector.MaxHostileDistance, 1, HostileValue);

            Heading(listing, Tr("JustStart_SettingsSectionIdeoligion"));
            bool ideology = ModsConfig.IdeologyActive;
            if (ButtonRow(listing, "JustStart_SettingsDefaultIdeologyLabel", ModeLabel(Settings.defaultIdeologyMode),
                ideology ? "JustStart_SettingsDefaultIdeologyTooltip" : "JustStart_RequiresIdeology", ideology))
                OpenIdeologyModeMenu();

            bool randomIdeo = Settings.defaultIdeologyMode == IdeologyMode.Fixed || Settings.defaultIdeologyMode == IdeologyMode.Fluid;
            string memeTip = !ideology ? "JustStart_RequiresIdeology"
                : randomIdeo ? "JustStart_SettingsMemesTooltip" : "JustStart_SettingsMemesRequiresRandom";
            if (ButtonRow(listing, "JustStart_SettingsMemesLabel", MemeCount(Settings.excludedMemes.Count), memeTip, ideology && randomIdeo))
                OpenMemeDialog();

            listing.Gap(24f);
            Rect resetRect = listing.GetRect(RowHeight);
            resetRect.width = Mathf.Min(resetRect.width, 200f);
            if (Widgets.ButtonText(resetRect, Tr("JustStart_SettingsReset")))
                Settings.ResetToDefaults();
            TooltipHandler.TipRegion(resetRect, Tr("JustStart_SettingsResetTooltip"));

            float height = listing.CurHeight;
            listing.End();
            return height;
        }

        private static float DrawRightColumn(Rect rect)
        {
            Listing_Standard listing = RightListing;
            listing.Begin(rect);

            Heading(listing, Tr("JustStart_SettingsSectionColonists"));
            bool biotech = ModsConfig.BiotechActive;
            Rect xenotypeRow = listing.GetRect(RowHeight);
            RowLabel(xenotypeRow.LeftPart(LabelPct), "JustStart_SettingsXenotypeLabel");
            TooltipHandler.TipRegion(xenotypeRow, Tr(biotech ? "JustStart_SettingsXenotypeTooltip" : "JustStart_RequiresBiotech"));
            if (StartingXenotype.DrawButton(xenotypeRow.RightPart(1f - LabelPct), biotech))
                StartingXenotype.OpenMenu();

            bool customActive = biotech && Settings.xenotypeMode == StartingXenotypeMode.Mixed;
            Checkbox(listing, "JustStart_SettingsMixedCustomLabel", ref Settings.mixedIncludesCustom,
                !biotech ? "JustStart_RequiresBiotech" : customActive ? "JustStart_SettingsMixedCustomTooltip" : "JustStart_SettingsMixedCustomRequiresMixed",
                disabled: !customActive, indent: true);

            listing.Gap(GroupGap);
            AgeRow(listing);
            if (ButtonRow(listing, "JustStart_SettingsGenderLabel", GenderLabel(Settings.genderMix), "JustStart_SettingsGenderTooltip", true))
                OpenGenderMenu();

            listing.Gap(GroupGap);
            Settings.minPassions = SteppedSlider(listing, "JustStart_SettingsPassionsLabel", "JustStart_SettingsPassionsTooltip",
                Settings.minPassions, 0, StartingPawnFilters.MaxPassions, 1, CountValue);
            Settings.minMajorPassions = SteppedSlider(listing, "JustStart_SettingsMajorPassionsLabel", "JustStart_SettingsMajorPassionsTooltip",
                Settings.minMajorPassions, 0, StartingPawnFilters.MaxMajorPassions, 1, CountValue);
            if (ButtonRow(listing, "JustStart_SettingsTraitsLabel", TraitCount(Settings.excludedTraits.Count), "JustStart_SettingsTraitsTooltip", true))
                OpenTraitDialog();

            listing.Gap(GroupGap);
            Checkbox(listing, "JustStart_SettingsCapableLabel", ref Settings.requireCapableColonists, "JustStart_SettingsCapableTooltip");
            if (Settings.requireCapableColonists)
            {
                // Capable of all work includes violence; show it ticked and inert.
                bool included = true;
                Checkbox(listing, "JustStart_SettingsViolenceLabel", ref included, "JustStart_SettingsViolenceIncluded", disabled: true);
            }
            else
                Checkbox(listing, "JustStart_SettingsViolenceLabel", ref Settings.requireViolenceCapable, "JustStart_SettingsViolenceTooltip");

            listing.Gap(GroupGap);
            Checkbox(listing, "JustStart_SettingsNoAddictionsLabel", ref Settings.noAddictions, "JustStart_SettingsNoAddictionsTooltip");
            Checkbox(listing, "JustStart_SettingsNoPregnanciesLabel", ref Settings.noPregnancies,
                biotech ? "JustStart_SettingsNoPregnanciesTooltip" : "JustStart_RequiresBiotech", disabled: !biotech);
            Checkbox(listing, "JustStart_SettingsNoRelationsLabel", ref Settings.noRelations, "JustStart_SettingsNoRelationsTooltip");

            float height = listing.CurHeight;
            listing.End();
            return height;
        }

        // Rows

        private static void Heading(Listing_Standard listing, string label)
        {
            if (listing.CurHeight > 0f)
                listing.Gap(18f);
            Verse.Text.Font = GameFont.Medium;
            listing.Label(label);
            Verse.Text.Font = GameFont.Small;
            listing.GapLine(6f);
        }

        private static void RowLabel(Rect rect, string labelKey)
        {
            TextAnchor anchor = Verse.Text.Anchor;
            Verse.Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(rect, Tr(labelKey));
            Verse.Text.Anchor = anchor;
        }

        private static void SubLabel(Listing_Standard listing, string labelKey, string tipKey)
        {
            Rect row = listing.GetRect(Verse.Text.LineHeight);
            Widgets.Label(row, Tr(labelKey));
            TooltipHandler.TipRegion(row, Tr(tipKey));
            listing.Gap(2f);
        }

        private static void Checkbox(Listing_Standard listing, string labelKey, ref bool value, string tipKey, bool disabled = false, bool indent = false) =>
            CheckboxWithTip(listing, labelKey, ref value, Tr(tipKey), disabled, indent);

        private static void CheckboxWithTip(Listing_Standard listing, string labelKey, ref bool value, string tip, bool disabled = false, bool indent = false)
        {
            Rect row = listing.GetRect(Verse.Text.LineHeight);
            if (indent)
                row.xMin += IndentWidth;
            Widgets.CheckboxLabeled(row, Tr(labelKey), ref value, disabled);
            TooltipHandler.TipRegion(row, tip);
            listing.Gap(2f);
        }

        private static void CheckboxPair(Listing_Standard listing, string keyA, ref bool a, string keyB, ref bool b)
        {
            Rect row = listing.GetRect(Verse.Text.LineHeight);
            row.xMin += IndentWidth;
            float width = (row.width - IndentWidth) / 2f;
            Widgets.CheckboxLabeled(new Rect(row.x, row.y, width, row.height), Tr(keyA), ref a);
            Widgets.CheckboxLabeled(new Rect(row.x + width + IndentWidth, row.y, width, row.height), Tr(keyB), ref b);
            listing.Gap(2f);
        }

        private static void CheckboxTriple(Listing_Standard listing, string keyA, ref bool a, string keyB, ref bool b, string keyC, ref bool c)
        {
            Rect row = listing.GetRect(Verse.Text.LineHeight);
            row.xMin += IndentWidth;
            float width = (row.width - 2f * IndentWidth) / 3f;
            float step = width + IndentWidth;
            Widgets.CheckboxLabeled(new Rect(row.x, row.y, width, row.height), Tr(keyA), ref a);
            Widgets.CheckboxLabeled(new Rect(row.x + step, row.y, width, row.height), Tr(keyB), ref b);
            Widgets.CheckboxLabeled(new Rect(row.x + 2f * step, row.y, width, row.height), Tr(keyC), ref c);
            listing.Gap(2f);
        }

        private static bool ButtonRow(Listing_Standard listing, string labelKey, string buttonLabel, string tipKey, bool active)
        {
            Rect row = listing.GetRect(RowHeight);
            RowLabel(row.LeftPart(LabelPct), labelKey);
            TooltipHandler.TipRegion(row, Tr(tipKey));
            bool clicked = Widgets.ButtonText(row.RightPart(1f - LabelPct), buttonLabel, active: active);
            listing.Gap(2f);
            return clicked && active;
        }

        private static Rect LabelledControlRow(Listing_Standard listing, string labelKey, string tipKey)
        {
            Rect row = listing.GetRect(SliderRowHeight);
            RowLabel(row.LeftPart(LabelPct), labelKey);
            TooltipHandler.TipRegion(row, Tr(tipKey));
            listing.Gap(2f);
            return row.RightPart(1f - LabelPct);
        }

        private static int SteppedSlider(Listing_Standard listing, string labelKey, string tipKey, int value, int min, int max, int step, Func<int, string> valueLabel)
        {
            Rect control = LabelledControlRow(listing, labelKey, tipKey);
            float raw = Widgets.HorizontalSlider(control, value, min, max, middleAlignment: true, label: valueLabel(value), roundTo: step);
            return Mathf.Clamp(Mathf.RoundToInt(raw / step) * step, min, max);
        }

        private static void TemperatureRow(Listing_Standard listing)
        {
            Rect control = LabelledControlRow(listing, "JustStart_SettingsTemperatureLabel", "JustStart_SettingsTemperatureTooltip");
            FloatRange range = Settings.temperatureRange;
            Widgets.FloatRange(control, TemperatureSliderId, ref range, TileSelector.MinTemperature, TileSelector.MaxTemperature,
                valueStyle: ToStringStyle.Temperature, roundTo: 1f);
            Settings.temperatureRange = range;
        }

        private static void AgeRow(Listing_Standard listing)
        {
            Rect control = LabelledControlRow(listing, "JustStart_SettingsAgeLabel", "JustStart_SettingsAgeTooltip");
            IntRange range = StartingPawnFilters.ClampedRange(Settings.startingAgeRange);
            Widgets.IntRange(control, AgeSliderId, ref range, StartingPawnFilters.MinAge, StartingPawnFilters.MaxAge);
            Settings.startingAgeRange = range;
        }

        // Menus and dialogs

        private static void OpenIdeologyModeMenu()
        {
            var options = new List<FloatMenuOption>();
            foreach (IdeologyMode mode in Enum.GetValues(typeof(IdeologyMode)))
            {
                IdeologyMode local = mode;
                options.Add(new FloatMenuOption(ModeLabel(local), () => Settings.defaultIdeologyMode = local));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void OpenGenderMenu()
        {
            var options = new List<FloatMenuOption>();
            foreach (GenderMix mix in Enum.GetValues(typeof(GenderMix)))
            {
                GenderMix local = mix;
                options.Add(new FloatMenuOption(GenderLabel(local), () => Settings.genderMix = local));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void OpenTraitDialog()
        {
            var entries = new List<Dialog_ExclusionList.Entry>();
            foreach (TraitDef def in DefDatabase<TraitDef>.AllDefs)
            {
                // Only traits random generation can give.
                if (def.GetGenderSpecificCommonality(Gender.Male) <= 0f && def.GetGenderSpecificCommonality(Gender.Female) <= 0f)
                    continue;
                foreach (TraitDegreeData degree in def.degreeDatas)
                {
                    if (degree.commonality > 0f && !degree.label.NullOrEmpty())
                        entries.Add(new Dialog_ExclusionList.Entry(StartingPawnFilters.TraitKey(def, degree.degree), degree.LabelCap, null));
                }
            }
            entries.SortBy(e => e.Label);
            Find.WindowStack.Add(new Dialog_ExclusionList(Tr("JustStart_SettingsTraitsLabel"), entries, Settings.excludedTraits));
        }

        private static void OpenMemeDialog()
        {
            var entries = DefDatabase<MemeDef>.AllDefs
                .Where(m => m.category == MemeCategory.Normal && !m.hiddenInChooseMemes)
                .OrderBy(m => m.label)
                .Select(m => new Dialog_ExclusionList.Entry(m.defName, m.LabelCap.Resolve(), m.description, m.Icon))
                .ToList();
            Find.WindowStack.Add(new Dialog_ExclusionList(Tr("JustStart_SettingsMemesLabel"), entries, Settings.excludedMemes));
        }

        // Text, cached per language; value labels are cached per value.

        private static LoadedLanguage? textLanguage;
        private static readonly Dictionary<string, string> texts = new Dictionary<string, string>();
        private static readonly Dictionary<int, string> growingValues = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> hostileValues = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> countValues = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> memeCounts = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> traitCounts = new Dictionary<int, string>();
        private static readonly Dictionary<IdeologyMode, string> modeLabels = new Dictionary<IdeologyMode, string>();
        private static readonly Dictionary<GenderMix, string> genderLabels = new Dictionary<GenderMix, string>();
        private static string? extremeBiomeTooltip;

        private static readonly Func<int, string> GrowingValue = days =>
            Cached(growingValues, days, d => d == 0 ? Tr("JustStart_ValueAny") : "JustStart_ValueDays".Translate(d).Resolve());

        private static readonly Func<int, string> HostileValue = tiles =>
            Cached(hostileValues, tiles, t => t == 0 ? Tr("JustStart_ValueAny") : "JustStart_ValueTiles".Translate(t).Resolve());

        private static readonly Func<int, string> CountValue = count =>
            Cached(countValues, count, c => c == 0 ? Tr("JustStart_ValueAny") : c.ToString());

        private static string MemeCount(int count) =>
            Cached(memeCounts, count, c => c == 0 ? Tr("JustStart_ValueNone") : "JustStart_ValueMemes".Translate(c).Resolve());

        private static string TraitCount(int count) =>
            Cached(traitCounts, count, c => c == 0 ? Tr("JustStart_ValueNone") : "JustStart_ValueTraits".Translate(c).Resolve());

        private static string ModeLabel(IdeologyMode mode) =>
            Cached(modeLabels, mode, m => ("JustStart_IdeologyMode" + m).Translate().Resolve());

        private static string GenderLabel(GenderMix mix) =>
            Cached(genderLabels, mix, m => ("JustStart_Gender" + m).Translate().Resolve());

        // Every loaded biome the game warns about when settling there, including other mods' biomes.
        private static string ExtremeBiomeTooltip =>
            extremeBiomeTooltip ??= "JustStart_SettingsExcludeExtremeTooltip".Translate(
                DefDatabase<BiomeDef>.AllDefs.Where(TileSelector.IsExtreme).Select(b => b.LabelCap.Resolve()).ToLineList("  - ")).Resolve();

        private static string Tr(string key)
        {
            if (texts.TryGetValue(key, out string? text) && text != null)
                return text;
            text = key.Translate().Resolve();
            texts[key] = text;
            return text;
        }

        private static string Cached<TKey>(Dictionary<TKey, string> cache, TKey key, Func<TKey, string> make) where TKey : notnull
        {
            if (cache.TryGetValue(key, out string? text) && text != null)
                return text;
            text = make(key);
            cache[key] = text;
            return text;
        }

        /// <summary>A language change reloads every Def and translation, so all cached text is rebuilt.</summary>
        private static void RefreshTextIfLanguageChanged()
        {
            if (textLanguage == LanguageDatabase.activeLanguage)
                return;
            textLanguage = LanguageDatabase.activeLanguage;
            texts.Clear();
            growingValues.Clear();
            hostileValues.Clear();
            countValues.Clear();
            memeCounts.Clear();
            traitCounts.Clear();
            modeLabels.Clear();
            genderLabels.Clear();
            extremeBiomeTooltip = null;
        }
    }
}
