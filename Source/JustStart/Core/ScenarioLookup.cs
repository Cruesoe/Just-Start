using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace JustStart
{
    /// <summary>Maps a Scenario back to the ScenarioDef that owns it, built once after Defs load.</summary>
    public static class ScenarioLookup
    {
        private static Dictionary<Scenario, ScenarioDef>? defsByScenario;

        /// <summary>Null for custom and copied scenarios, which belong to no ScenarioDef.</summary>
        public static ScenarioDef? DefFor(Scenario scenario)
        {
            defsByScenario ??= DefDatabase<ScenarioDef>.AllDefsListForReading
                .Where(d => d.scenario != null)
                .ToDictionary(d => d.scenario);
            return defsByScenario.TryGetValue(scenario, out ScenarioDef def) ? def : null;
        }

        public static JustStartScenarioExtension? ExtensionFor(Scenario scenario) =>
            DefFor(scenario)?.GetModExtension<JustStartScenarioExtension>();
    }
}
