using HarmonyLib;
using RimWorld;

namespace JustStart
{
    [HarmonyPatch(typeof(Page_SelectStartingSite), "PostClose")]
    public static class Patch_Page_SelectStartingSite_PostClose_ReleaseAutoStartPage
    {
        public static void Postfix(Page_SelectStartingSite __instance)
        {
            if (JustStartGameStartHook.AutoStartPage == __instance)
                JustStartGameStartHook.AutoStartPage = null;
        }
    }
}
