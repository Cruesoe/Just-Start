using HarmonyLib;
using RimWorld;

namespace JustStart
{
    /// <summary>
    /// Stops the hidden page writing the world selection over Just Start's chosen tile, and keeps the forced-map hook page
    /// from drawing.
    /// </summary>
    [HarmonyPatch(typeof(Page_SelectStartingSite), "DoWindowContents")]
    public static class Patch_Page_SelectStartingSite_DoWindowContents
    {
        public static bool Prefix(Page_SelectStartingSite __instance) =>
            JustStartGameStartHook.AutoStartPage != __instance && !JustStartGameStartHook.ForcedMap;
    }
}
