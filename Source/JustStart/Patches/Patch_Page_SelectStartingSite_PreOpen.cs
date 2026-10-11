using HarmonyLib;
using RimWorld;
using Verse;

namespace JustStart
{
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
            JustStartGameStartHook.ForcedMap = JustStartGameStartHook.HasForcedMap(Find.Scenario);
            if (!JustStartGameStartHook.PendingAutoStart)
                return !JustStartGameStartHook.ForcedMap;

            JustStartGameStartHook.PendingAutoStart = false;
            JustStartGameStartHook.AutoStartPage = __instance;
            JustStartGameStartHook.FlowRan = false;
            return false;
        }
    }
}
