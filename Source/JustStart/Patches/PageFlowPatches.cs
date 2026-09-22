using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace JustStart
{
    /// <summary>
    /// VERIFICATION NOTE: this file could not be compiled or run against the real game in the
    /// authoring environment (no local RimWorld install). Page_CreateWorldParams / PageUtility
    /// member names/signatures below are based on documented RimWorld 1.6 modding knowledge and
    /// must be re-checked with ILSpy/dnSpy against the installed Assembly-CSharp.dll before this
    /// mod ships. If a member name has drifted, this patch will simply fail to apply (Harmony
    /// logs an error) rather than corrupting a save - it only ever runs at the pre-game screens.
    ///
    /// Insertion point chosen per Section 2: after the player finishes Page_CreateWorldParams
    /// (world settings configured, world not yet generated) we add a "Just Start" button next to
    /// "Start" / "Next". Clicking it lets the normal world-generation LongEventHandler run, then
    /// invokes JustStartFlow.Run() instead of advancing to Page_SelectStartingSite /
    /// Page_ConfigureStartingPawns.
    /// </summary>
    [HarmonyPatch(typeof(Page_CreateWorldParams), "DoWindowContents")]
    public static class Patch_Page_CreateWorldParams_AddJustStartButton
    {
        public static void Postfix(Page_CreateWorldParams __instance, Rect rect)
        {
            var buttonRect = new Rect(rect.x, rect.yMax - 38f - 45f, 160f, 38f);
            if (Widgets.ButtonText(buttonRect, "JustStart.Button.JustStart".Translate()))
            {
                var canDoNext = (bool)AccessTools.Method(typeof(Page_CreateWorldParams), "CanDoNext")
                    ?.Invoke(__instance, null);
                if (canDoNext != false)
                {
                    JustStartGameStartHook.PendingAutoStart = true;
                    AccessTools.Method(typeof(Page), "DoNext")?.Invoke(__instance, null);
                }
            }
        }
    }

    /// <summary>
    /// After world generation completes and the game would normally show
    /// Page_SelectStartingSite, intercept and run the automated flow instead. Implemented as a
    /// prefix on the page's constructor/PreOpen that, if JustStartGameStartHook.PendingAutoStart
    /// is set, runs JustStartFlow.Run() and closes the window stack rather than displaying it.
    /// </summary>
    [HarmonyPatch(typeof(Page_SelectStartingSite), "PreOpen")]
    public static class Patch_Page_SelectStartingSite_SkipIfAutoStart
    {
        public static bool Prefix(Page_SelectStartingSite __instance)
        {
            if (!JustStartGameStartHook.PendingAutoStart)
                return true;

            JustStartGameStartHook.PendingAutoStart = false;
            JustStartFlow.Run(onFailure: () => Find.WindowStack.Add(__instance));
            return false;
        }
    }

    /// <summary>Spawns queued non-colonist pawns (The Prisoner's guard, etc.) once the starting map exists.</summary>
    [HarmonyPatch(typeof(Map), "FinalizeInit")]
    public static class Patch_Map_FinalizeInit_SpawnJustStartPawns
    {
        public static void Postfix(Map __instance)
        {
            if (__instance.IsPlayerHome)
                JustStartMapSpawnQueue.SpawnAllOn(__instance);
        }
    }

    public static class JustStartGameStartHook
    {
        public static bool PendingAutoStart;
    }
}
