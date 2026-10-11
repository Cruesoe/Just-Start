using HarmonyLib;
using RimWorld;

namespace JustStart
{
    [HarmonyPatch(typeof(Page_SelectStartingSite), "PostOpen")]
    public static class Patch_Page_SelectStartingSite_PostOpen_SkipForcedMapHook
    {
        public static bool Prefix(Page_SelectStartingSite __instance) =>
            JustStartGameStartHook.AutoStartPage != __instance && !JustStartGameStartHook.ForcedMap;
    }
}
