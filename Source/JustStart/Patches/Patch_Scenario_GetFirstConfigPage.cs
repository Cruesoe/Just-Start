using HarmonyLib;
using RimWorld;

namespace JustStart
{
    /// <summary>Forced-map scenarios omit site selection; adds a hidden hook page without changing their normal page flow.</summary>
    [HarmonyPatch(typeof(Scenario), nameof(Scenario.GetFirstConfigPage))]
    public static class Patch_Scenario_GetFirstConfigPage
    {
        public static void Postfix(Scenario __instance, Page __result)
        {
            if (!JustStartGameStartHook.HasForcedMap(__instance))
                return;

            for (Page page = __result; page != null; page = page.next)
            {
                if (!(page is Page_CreateWorldParams))
                    continue;
                if (page.next is Page_SelectStartingSite)
                    break;

                var hook = new Page_SelectStartingSite
                {
                    prev = page,
                    next = page.next,
                    nextAct = page.nextAct
                };
                if (hook.next != null)
                    hook.next.prev = hook;
                page.next = hook;
                page.nextAct = null;
                break;
            }
        }
    }
}
