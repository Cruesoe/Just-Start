using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace JustStart
{
    /// <summary>
    /// The starting xenotype setting: one xenotype or custom xenotype for every colonist, or a random one each (mixed). Needs Biotech.
    /// </summary>
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
        // Built on the main thread when Just Start is clicked; world generation (which may generate the pawns) runs asynchronously.
        private static List<Choice> runChoices = new List<Choice>();

        private static JustStartSettings Settings => JustStartMod.Settings;

        private static Texture2D? MixedIcon => mixedIcon ??= ContentFinder<Texture2D>.Get("UI/Structures/Random", reportFailure: false);

        /// <summary>Every loaded xenotype, in the order the vanilla character editor lists them.</summary>
        public static IEnumerable<XenotypeDef> AllXenotypes =>
            DefDatabase<XenotypeDef>.AllDefs.OrderBy(x => -x.displayPriority);

        /// <summary>The player's saved custom xenotypes, newest first; loaded on first use.</summary>
        public static List<CustomXenotype> CustomXenotypes => customXenotypes ??= LoadCustomXenotypes();

        private static List<CustomXenotype> LoadCustomXenotypes()
        {
            var list = new List<CustomXenotype>();
            foreach (var file in GenFilePaths.AllCustomXenotypeFiles)
            {
                if (GameDataSaveLoader.TryLoadXenotype(file.FullName, out CustomXenotype xenotype))
                    list.Add(xenotype);
            }
            return list;
        }

        private static CustomXenotype? SelectedCustom =>
            Settings.startingCustomXenotype.NullOrEmpty() ? null : CustomXenotypes.FirstOrDefault(x => x.fileName == Settings.startingCustomXenotype);

        /// <summary>The chosen xenotype def, or Baseliner if the saved one is no longer loaded.</summary>
        private static XenotypeDef SelectedDef =>
            DefDatabase<XenotypeDef>.GetNamedSilentFail(Settings.startingXenotype) ?? XenotypeDefOf.Baseliner;

        /// <summary>The fixed choice; a custom xenotype whose file is gone falls back to the def setting.</summary>
        private static Choice Selected => SelectedCustom is CustomXenotype custom ? new Choice(custom) : new Choice(SelectedDef);

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
            if (!ModsConfig.BiotechActive)
                return;
            customXenotypes = null;
            runChoices =Settings.mixedXenotypes ? MixedPool.ToList() : new List<Choice> { Selected };
        }

        /// <summary>Forces the next starting colonist's xenotype from the choices fixed by PrepareRun.</summary>
        public static void ApplyTo(ref PawnGenerationRequest request)
        {
            Choice choice = runChoices.Count > 0 ? runChoices.RandomElement() : new Choice(XenotypeDefOf.Baseliner);
            request.ForcedXenotype = choice.Def;
            request.ForcedCustomXenotype = choice.Custom;
            request.AllowedXenotypes = null;
            request.ForceBaselinerChance = 0f;
        }

        private static string Label(Choice choice) =>
            choice.Custom != null
                ? choice.Custom.name.CapitalizeFirst() + " (" + "Custom".Translate() + ")"
                : choice.Def!.LabelCap.Resolve();

        private static Texture2D? Icon(Choice choice) => choice.Custom != null ? choice.Custom.IconDef.Icon : choice.Def!.Icon;

        private static string CurrentLabel => Settings.mixedXenotypes ? "JustStart_XenotypeMixed".Translate().Resolve() : Label(Selected);

        private static Texture2D? CurrentIcon => Settings.mixedXenotypes ? MixedIcon : Icon(Selected);

        public static void OpenMenu()
        {
            customXenotypes = null;
            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption("JustStart_XenotypeMixed".Translate(), () => Settings.mixedXenotypes = true,
                    MixedIcon, XenotypeDef.IconColor, mouseoverGuiAction: r => TooltipHandler.TipRegion(r, "JustStart_XenotypeMixedTooltip".Translate()))
            };
            foreach (XenotypeDef xenotype in AllXenotypes)
            {
                XenotypeDef local = xenotype;
                options.Add(new FloatMenuOption(local.LabelCap, () =>
                    {
                        Settings.mixedXenotypes = false;
                        Settings.startingXenotype = local.defName;
                        Settings.startingCustomXenotype = string.Empty;
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
                        Settings.mixedXenotypes = false;
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
