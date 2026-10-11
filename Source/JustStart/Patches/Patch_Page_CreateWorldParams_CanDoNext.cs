using HarmonyLib;
using RimWorld;

namespace JustStart
{
    /// <summary>The vanilla Generate button disarms a Just Start left pending by a world generation that never finished.</summary>
    [HarmonyPatch(typeof(Page_CreateWorldParams), "CanDoNext")]
    public static class Patch_Page_CreateWorldParams_CanDoNext_ClearPendingAutoStart
    {
        public static void Prefix()
        {
            if (!JustStartGameStartHook.InvokingFromButton)
                JustStartGameStartHook.PendingAutoStart = false;
        }
    }
}
