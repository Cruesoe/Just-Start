using System.Linq;
using RimWorld;

namespace JustStart
{
    /// <summary>State shared by the new-game page patches.</summary>
    public static class JustStartGameStartHook
    {
        /// <summary>Set by the Just Start button; the next Page_SelectStartingSite runs the flow instead of opening.</summary>
        public static bool PendingAutoStart;

        /// <summary>True while the Just Start button is calling Page_CreateWorldParams.CanDoNext.</summary>
        public static bool InvokingFromButton;

        /// <summary>The hidden Page_SelectStartingSite the flow is running behind; cleared when it closes.</summary>
        public static Page_SelectStartingSite? AutoStartPage;

        public static bool FlowRan;

        /// <summary>Whether the scenario forces its map, read when the starting-site page opens.</summary>
        public static bool ForcedMap;

        public static bool HasForcedMap(Scenario scenario) => scenario.AllParts.OfType<ScenPart_ForcedMap>().Any();
    }
}
