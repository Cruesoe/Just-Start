using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace JustStart
{
    /// <summary>
    /// The starting xenotype setting: one xenotype or custom xenotype for every colonist, a random one each (mixed), or the
    /// scenario's own. Needs Biotech.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class StartingXenotype
    {
        /// <summary>One choice: a xenotype def, or a custom xenotype from the xenotype editor.</summary>
        private readonly struct Choice
        {
            public readonly XenotypeDef? Def;
            public readonly CustomXenotype? Custom;

            public Choice(XenotypeDef def) { Def = def; Custom = null; }
            public Choice(CustomXenotype custom) { Def = null; Custom = custom; }
        }

        private static Texture2D? mixedIcon;
        private static List<CustomXenotype>? customXenotypes;
        // The language the caches above were built under; a language change reloads every Def and texture.
        private static LoadedLanguage? cacheLanguage;
        // Built on the main thread when Just Start is clicked; world generation (which may generate the pawns) runs asynchronously.
        // Empty leaves the scenario's xenotypes alone.
        private static List<Choice> runChoices = new List<Choice>();
        // Custom xenotypes given to colonists this run, added to the new game's custom xenotype database before it starts.
        private static readonly HashSet<CustomXenotype> appliedCustom = new HashSet<CustomXenotype>();

        private static JustStartSettings Settings => JustStartMod.Settings;

        private static StartingXenotypeMode Mode => Settings.xenotypeMode;

        private static void ClearCachesIfReloaded()
        {
            if (cacheLanguage == LanguageDatabase.activeLanguage)
                return;
            cacheLanguage = LanguageDatabase.activeLanguage;
            mixedIcon = null;
            customXenotypes = null;
        }

        private static Texture2D MixedIcon
        {
            get
            {
                ClearCachesIfReloaded();
                return mixedIcon ??= ContentFinder<Texture2D>.Get("UI/Structures/Random", reportFailure: false)
                    ?? ContentFinder<Texture2D>.Get("UI/Icons/QuestionMark");
            }
        }

        /// <summary>Every loaded xenotype, in the order the vanilla character editor lists them.</summary>
        public static IEnumerable<XenotypeDef> AllXenotypes =>
            DefDatabase<XenotypeDef>.AllDefs.OrderBy(x => -x.displayPriority);

        /// <summary>The player's saved custom xenotypes, newest first; loaded on first use, and never without Biotech.</summary>
        private static List<CustomXenotype> CustomXenotypes
        {
            get
            {
                ClearCachesIfReloaded();
                return customXenotypes ??= LoadCustomXenotypes();
            }
        }

        private static List<CustomXenotype> LoadCustomXenotypes()
        {
            var list = new List<CustomXenotype>();
            if (!ModsConfig.BiotechActive)
                return list;
            foreach (var file in GenFilePaths.AllCustomXenotypeFiles)
            {
                if (GameDataSaveLoader.TryLoadXenotype(file.FullName, out CustomXenotype xenotype))
                    list.Add(xenotype);
            }
            return list;
        }

        private static CustomXenotype? SelectedCustom =>
            Settings.startingCustomXenotype.NullOrEmpty() ? null : CustomXenotypes.FirstOrDefault(x => x.fileName == Settings.startingCustomXenotype);

        /// <summary>The chosen xenotype def, or Baseliner if the saved one is no longer loaded; null without Biotech.</summary>
        private static XenotypeDef? SelectedDef =>
            DefDatabase<XenotypeDef>.GetNamedSilentFail(Settings.startingXenotype) ?? XenotypeDefOf.Baseliner;

        /// <summary>The fixed choice; a custom xenotype whose file is gone falls back to the def setting.</summary>
        private static Choice? Selected
        {
            get
            {
                if (Mode == StartingXenotypeMode.Custom && SelectedCustom is CustomXenotype custom)
                    return new Choice(custom);
                return SelectedDef is XenotypeDef def ? new Choice(def) : (Choice?)null;
            }
        }

        private static IEnumerable<Choice> MixedPool
        {
            get
            {
                foreach (XenotypeDef def in AllXenotypes)
                    yield return new Choice(def);
                if (Settings.mixedIncludesCustom)
                {
                    foreach (CustomXenotype custom in CustomXenotypes)
                        yield return new Choice(custom);
                }
            }
        }

        /// <summary>Reloads custom xenotypes and fixes the choices for the Just Start run about to begin.</summary>
        public static void PrepareRun()
        {
            runChoices = new List<Choice>();
            appliedCustom.Clear();
            if (!ModsConfig.BiotechActive)
                return;
            customXenotypes = null;
            if (Mode == StartingXenotypeMode.Mixed)
                runChoices.AddRange(MixedPool);
            else if (Mode != StartingXenotypeMode.Scenario && Selected is Choice selected)
                runChoices.Add(selected);
        }

        /// <summary>Forces the next starting colonist's xenotype from the choices fixed by PrepareRun.</summary>
        public static void ApplyTo(ref PawnGenerationRequest request)
        {
            if (runChoices.Count == 0)
                return;
            Choice choice = runChoices.RandomElement();
            request.ForcedXenotype = choice.Def;
            request.ForcedCustomXenotype = choice.Custom;
            request.AllowedXenotypes = null;
            request.ForceBaselinerChance = 0f;
            if (choice.Custom != null)
                appliedCustom.Add(choice.Custom);
        }

        /// <summary>
        /// Adds the custom xenotypes given this run to the game's database, as vanilla does for the starting-pawn page's
        /// choices; without it the colonists count as baseliners.
        /// </summary>
        public static void RegisterAppliedCustomXenotypes()
        {
            if (!ModsConfig.BiotechActive)
                return;
            List<CustomXenotype> known = Current.Game.customXenotypeDatabase.customXenotypes;
            foreach (CustomXenotype custom in appliedCustom)
            {
                if (!known.Contains(custom))
                    known.Add(custom);
            }
        }

        private static string Label(Choice choice) =>
            choice.Custom != null
                ? choice.Custom.name.CapitalizeFirst() + " (" + "Custom".Translate() + ")"
                : choice.Def!.LabelCap.Resolve();

        private static Texture2D Icon(Choice choice) => choice.Custom != null ? choice.Custom.IconDef.Icon : choice.Def!.Icon;

        private static string CurrentLabel
        {
            get
            {
                switch (Mode)
                {
                    case StartingXenotypeMode.Mixed:
                        return "JustStart_XenotypeMixed".Translate();
                    case StartingXenotypeMode.Scenario:
                        return "JustStart_XenotypeScenario".Translate();
                    default:
                        return Selected is Choice choice ? Label(choice) : "JustStart_XenotypeBaseliner".Translate().Resolve();
                }
            }
        }

        private static Texture2D? CurrentIcon
        {
            get
            {
                switch (Mode)
                {
                    case StartingXenotypeMode.Mixed:
                        return MixedIcon;
                    case StartingXenotypeMode.Scenario:
                        return null;
                    default:
                        return Selected is Choice choice ? Icon(choice) : null;
                }
            }
        }

        public static void OpenMenu()
        {
            customXenotypes = null;
            var options = new List<FloatMenuOption>
            {
                // ClearTex keeps the label aligned with the icon entries below.
                new FloatMenuOption("JustStart_XenotypeScenario".Translate(), () => Settings.xenotypeMode = StartingXenotypeMode.Scenario,
                    BaseContent.ClearTex, Color.white,
                    mouseoverGuiAction: r => TooltipHandler.TipRegion(r, "JustStart_XenotypeScenarioTooltip".Translate())),
                new FloatMenuOption("JustStart_XenotypeMixed".Translate(), () => Settings.xenotypeMode = StartingXenotypeMode.Mixed,
                    MixedIcon, XenotypeDef.IconColor,
                    mouseoverGuiAction: r => TooltipHandler.TipRegion(r, "JustStart_XenotypeMixedTooltip".Translate()))
            };
            foreach (XenotypeDef xenotype in AllXenotypes)
            {
                XenotypeDef local = xenotype;
                options.Add(new FloatMenuOption(local.LabelCap, () =>
                    {
                        Settings.xenotypeMode = StartingXenotypeMode.Xenotype;
                        Settings.startingXenotype = local.defName;
                    },
                    local.Icon, XenotypeDef.IconColor,
                    mouseoverGuiAction: r => TooltipHandler.TipRegion(r, local.descriptionShort ?? local.description),
                    extraPartWidth: 24f,
                    extraPartOnGUI: r => Widgets.InfoCardButton(r.x, r.y + 3f, local),
                    extraPartRightJustified: true));
            }
            foreach (CustomXenotype custom in CustomXenotypes)
            {
                CustomXenotype local = custom;
                options.Add(new FloatMenuOption(Label(new Choice(local)), () =>
                    {
                        Settings.xenotypeMode = StartingXenotypeMode.Custom;
                        Settings.startingCustomXenotype = local.fileName;
                    },
                    local.IconDef.Icon, XenotypeDef.IconColor));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        /// <summary>A button showing the current choice's icon to the left of its name.</summary>
        public static bool DrawButton(Rect rect, bool active)
        {
            bool clicked = Widgets.ButtonText(rect, string.Empty, active: active);
            Rect iconRect = new Rect(rect.x + 6f, rect.y + (rect.height - 24f) / 2f, 24f, 24f);
            Texture2D? icon = CurrentIcon;
            Color oldColor = GUI.color;
            if (icon != null)
            {
                GUI.color = active ? XenotypeDef.IconColor : XenotypeDef.IconColor * Color.gray;
                GUI.DrawTexture(iconRect, icon);
            }
            GUI.color = active ? Color.white : Color.gray;
            Rect labelRect = new Rect(iconRect.xMax + 6f, rect.y, rect.xMax - iconRect.xMax - 12f, rect.height);
            TextAnchor oldAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, CurrentLabel.Truncate(labelRect.width));
            Text.Anchor = oldAnchor;
            GUI.color = oldColor;
            return clicked && active;
        }
    }
}
