using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace JustStart
{
    /// <summary>
    /// Adds a "Just Start" button to the left of Page_CreateWorldParams's own "Generate" button. It runs the same world
    /// generation as Generate (CanDoNext), then the Page_SelectStartingSite patches run JustStartFlow instead of showing that page.
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
            // Keep both start actions in the bottom row, separated by the standard 17f gap.
            Rect buttonRect = new Rect(rect.xMax - 2f * bottomButSize.x - 17f, generateButtonY, bottomButSize.x, bottomButSize.y);

            if (Widgets.ButtonText(buttonRect, "JustStart_ButtonJustStart".Translate()))
            {
                // Right-click opens the mod settings instead.
                if (Event.current.button == 1)
                    Find.WindowStack.Add(new Dialog_ModSettings(JustStartMod.Instance));
                else
                {
                    StartingXenotype.PrepareRun();
                    JustStartGameStartHook.InvokingFromButton = true;
                    JustStartGameStartHook.PendingAutoStart = true;
                    try
                    {
                        CanDoNextMethod.Invoke(__instance, null);
                    }
                    finally
                    {
                        JustStartGameStartHook.InvokingFromButton = false;
                    }
                    // CanDoNext returns false either way; only a queued world generation keeps the flag armed.
                    if (!LongEventHandler.AnyEventNowOrWaiting)
                        JustStartGameStartHook.PendingAutoStart = false;
                }
            }
            TooltipHandler.TipRegion(buttonRect, "JustStart_ButtonJustStartTooltip".Translate());
        }
    }
}
