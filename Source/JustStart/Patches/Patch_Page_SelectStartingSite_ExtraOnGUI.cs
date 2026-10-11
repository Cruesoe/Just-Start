using HarmonyLib;
using RimWorld;
using Verse;

namespace JustStart
{
    /// <summary>Runs JustStartFlow behind the hidden page, or passes straight through the forced-map hook page.</summary>
    [HarmonyPatch(typeof(Page_SelectStartingSite), "ExtraOnGUI")]
    public static class Patch_Page_SelectStartingSite_ExtraOnGUI_RunFlow
    {
        public static bool Prefix(Page_SelectStartingSite __instance)
        {
            if (JustStartGameStartHook.AutoStartPage != __instance)
            {
                if (JustStartGameStartHook.ForcedMap)
                {
                    __instance.Close(doCloseSound: false);
                    if (__instance.next != null)
                    {
                        __instance.next.prev = __instance.prev;
                        Find.WindowStack.Add(__instance.next);
                    }
                    else
                        __instance.nextAct?.Invoke();
                    return false;
                }
                return true;
            }

            if (!JustStartGameStartHook.FlowRan)
            {
                JustStartGameStartHook.FlowRan = true;
                __instance.Close(doCloseSound: false);
                JustStartFlow.Run(onFailure: () => Find.WindowStack.Add(__instance.prev));
            }
            return false;
        }
    }
}
