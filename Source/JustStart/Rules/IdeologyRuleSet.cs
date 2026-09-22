namespace JustStart
{
    public enum IdeologyMode
    {
        /// <summary>Just Start does not generate or touch a player ideology through its own system.</summary>
        Inactive,

        /// <summary>Generate a random valid fluid ideology (RimWorld's "no fixed ideoligion" mode).</summary>
        Fluid,

        /// <summary>Generate a random but internally coherent fixed ideology.</summary>
        Fixed,
    }

    /// <summary>
    /// Global (Ideology-only) ideology handling. A scenario may pin a specific mode; when it
    /// doesn't, the player's mod-settings default mode is used. Per-meme/precept require/prefer
    /// rules are a documented future extension point (see README "Ideology extension - future"),
    /// not implemented in this pass.
    /// </summary>
    public class IdeologyRuleSet
    {
        public IdeologyMode? mode;
    }
}
