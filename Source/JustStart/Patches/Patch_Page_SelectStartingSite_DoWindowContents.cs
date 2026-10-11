using HarmonyLib;
using RimWorld;

namespace JustStart
{
    /// <summary>Stops the hidden page writing the world selection over Just Start's chosen tile.</summary>
    [HarmonyPatch(typeof(Page_SelectStartingSite), "DoWindowContents")]
    public static class Patch_Page_SelectStartingSite_DoWindowContents_SkipIfAutoStart
    {
        public static bool Prefix(Page_SelectStartingSite __instance) =>
            JustStartGameStartHook.AutoStartPage != __instance && !JustStartGameStartHook.ForcedMap;
    }
}
