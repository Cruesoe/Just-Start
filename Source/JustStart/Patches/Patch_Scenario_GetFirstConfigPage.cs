using HarmonyLib;
using RimWorld;

namespace JustStart
{
    // Forced-map scenarios omit site selection; add a hidden hook without changing their normal page flow.
    [HarmonyPatch(typeof(Scenario), nameof(Scenario.GetFirstConfigPage))]
    public static class Patch_Scenario_GetFirstConfigPage_AddForcedMapHook
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
