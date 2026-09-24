using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace JustStart
{
    /// <summary>
    /// Adds a "Just Start" button above Page_CreateWorldParams's own "Generate" button. It runs the same world
    /// generation as Generate (CanDoNext), then the Page_SelectStartingSite patches below run JustStartFlow instead of showing that page.
    /// </summary>
    [HarmonyPatch(typeof(Page_CreateWorldParams), "DoWindowContents")]
    public static class Patch_Page_CreateWorldParams_DoWindowContents_AddJustStartButton
    {
        private static readonly MethodInfo CanDoNextMethod = AccessTools.Method(typeof(Page_CreateWorldParams), "CanDoNext");
        private static readonly AccessTools.FieldRef<Vector2> BottomButSizeRef =
            AccessTools.StaticFieldRefAccess<Vector2>(AccessTools.Field(typeof(Page), "BottomButSize"));

        public static void Postfix(Page_CreateWorldParams __instance, Rect rect)
        {
            Vector2 bottomButSize = BottomButSizeRef();
            float generateButtonY = rect.y + rect.height - Page.BottomButHeight;
            // 17f matches Page.GetMainRect's own bottom margin, so this row sits flush above the content area.
            Rect buttonRect = new Rect(rect.x + rect.width - bottomButSize.x, generateButtonY - 17f - bottomButSize.y, bottomButSize.x, bottomButSize.y);

            if (Widgets.ButtonText(buttonRect, "JustStart_ButtonJustStart".Translate()))
            {
                // Right-click opens the mod settings instead.
                if (Event.current.button == 1)
                    Find.WindowStack.Add(new Dialog_ModSettings(JustStartMod.Instance));
                else
                {
                    JustStartGameStartHook.PendingAutoStart = true;
                    CanDoNextMethod.Invoke(__instance, null);
                    // CanDoNext returns false either way; only a queued world generation keeps the flag armed.
                    if (!LongEventHandler.AnyEventNowOrWaiting)
                        JustStartGameStartHook.PendingAutoStart = false;
                }
            }
            TooltipHandler.TipRegion(buttonRect, "JustStart_ButtonJustStartTooltip".Translate());
        }
    }

    /// <summary>
    /// World generation's completion callback adds Page_SelectStartingSite and then closes the world settings page,
    /// so the flow can't run inside PreOpen. PreOpen is skipped (so the world map is never shown) and the flow runs
    /// on the page's first GUI frame instead.
    /// </summary>
    [HarmonyPatch(typeof(Page_SelectStartingSite), "PreOpen")]
    public static class Patch_Page_SelectStartingSite_PreOpen_SkipIfAutoStart
    {
        public static bool Prefix(Page_SelectStartingSite __instance)
        {
            if (!JustStartGameStartHook.PendingAutoStart)
                return true;

            JustStartGameStartHook.PendingAutoStart = false;
            JustStartGameStartHook.AutoStartPage = __instance;
            JustStartGameStartHook.FlowRan = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Page_SelectStartingSite), "ExtraOnGUI")]
    public static class Patch_Page_SelectStartingSite_ExtraOnGUI_RunFlow
    {
        public static bool Prefix(Page_SelectStartingSite __instance)
        {
            if (JustStartGameStartHook.AutoStartPage != __instance)
                return true;

            if (!JustStartGameStartHook.FlowRan)
            {
                JustStartGameStartHook.FlowRan = true;
                __instance.Close(doCloseSound: false);
                JustStartFlow.Run(onFailure: () => Find.WindowStack.Add(__instance.prev));
            }
            return false;
        }
    }

    /// <summary>Stops the hidden page writing the world selection over Just Start's chosen tile.</summary>
    [HarmonyPatch(typeof(Page_SelectStartingSite), "DoWindowContents")]
    public static class Patch_Page_SelectStartingSite_DoWindowContents_SkipIfAutoStart
    {
        public static bool Prefix(Page_SelectStartingSite __instance) =>
            JustStartGameStartHook.AutoStartPage != __instance;
    }

    [HarmonyPatch(typeof(Page_SelectStartingSite), "PostClose")]
    public static class Patch_Page_SelectStartingSite_PostClose_ReleaseAutoStartPage
    {
        public static void Postfix(Page_SelectStartingSite __instance)
        {
            if (JustStartGameStartHook.AutoStartPage == __instance)
                JustStartGameStartHook.AutoStartPage = null;
        }
    }

    public static class JustStartGameStartHook
    {
        public static bool PendingAutoStart;

        /// <summary>The hidden Page_SelectStartingSite the flow is running behind; cleared when it closes.</summary>
        public static Page_SelectStartingSite? AutoStartPage;

        public static bool FlowRan;
    }
}
