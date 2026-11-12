using HarmonyLib;
using RimWorld;

namespace JustStart
{
    /// <summary>Skips the page's PostOpen setup for the hidden page and the forced-map hook page.</summary>
    [HarmonyPatch(typeof(Page_SelectStartingSite), "PostOpen")]
    public static class Patch_Page_SelectStartingSite_PostOpen
    {
        public static bool Prefix(Page_SelectStartingSite __instance) =>
            JustStartGameStartHook.AutoStartPage != __instance && !JustStartGameStartHook.ForcedMap;
    }
}
