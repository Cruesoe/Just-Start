using HarmonyLib;
using RimWorld;
using Verse;

namespace JustStart
{
    /// <summary>Applies each scenario's preferredPlayerFactions once all Defs are loaded.</summary>
    [StaticConstructorOnStartup]
    public static class PreferredPlayerFactions
    {
        private static readonly AccessTools.FieldRef<Scenario, ScenPart_PlayerFaction> PlayerFactionPart =
            AccessTools.FieldRefAccess<Scenario, ScenPart_PlayerFaction>("playerFaction");

        private static readonly AccessTools.FieldRef<ScenPart_PlayerFaction, FactionDef> FactionDefField =
            AccessTools.FieldRefAccess<ScenPart_PlayerFaction, FactionDef>("factionDef");

        static PreferredPlayerFactions()
        {
            foreach (ScenarioDef scenarioDef in DefDatabase<ScenarioDef>.AllDefsListForReading)
            {
                var preferred = scenarioDef.GetModExtension<JustStartScenarioExtension>()?.preferredPlayerFactions;
                if (preferred.NullOrEmpty())
                    continue;

                ScenPart_PlayerFaction? part = scenarioDef.scenario != null ? PlayerFactionPart(scenarioDef.scenario) : null;
                if (part == null)
                    continue;

                foreach (string defName in preferred!)
                {
                    FactionDef faction = DefDatabase<FactionDef>.GetNamedSilentFail(defName);
                    if (faction != null && faction.isPlayer)
                    {
                        FactionDefField(part) = faction;
                        break;
                    }
                }
            }
        }
    }
}
