using HarmonyLib;
using RimWorld;

namespace JustStart
{
    /// <summary>Clears the hidden page reference once that page closes.</summary>
    [HarmonyPatch(typeof(Page_SelectStartingSite), "PostClose")]
    public static class Patch_Page_SelectStartingSite_PostClose
    {
        public static void Postfix(Page_SelectStartingSite __instance)
        {
            if (JustStartGameStartHook.AutoStartPage == __instance)
                JustStartGameStartHook.AutoStartPage = null;
        }
    }
}
