namespace JustStart
{
    /// <summary>How Just Start sets up the player's ideoligion when Ideology is active.</summary>
    public enum IdeologyMode
    {
        /// <summary>The player gets a classic ideoligion of a random allowed culture; other factions keep their own.</summary>
        Inactive,

        /// <summary>Generate a random valid fluid ideology (RimWorld's "no fixed ideoligion" mode).</summary>
        Fluid,

        /// <summary>Generate a random but internally coherent fixed ideology.</summary>
        Fixed,

        /// <summary>Vanilla's Classic preset: every faction shares one classic ideoligion.</summary>
        Classic,
    }
}
