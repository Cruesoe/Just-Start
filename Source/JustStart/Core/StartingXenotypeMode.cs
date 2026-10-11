namespace JustStart
{
    /// <summary>How Just Start sets starting colonists' xenotypes when Biotech is active.</summary>
    public enum StartingXenotypeMode
    {
        /// <summary>Every colonist gets the xenotype def in startingXenotype.</summary>
        Xenotype,

        /// <summary>Every colonist gets the custom xenotype in startingCustomXenotype.</summary>
        Custom,

        /// <summary>Each colonist gets a random xenotype.</summary>
        Mixed,

        /// <summary>The scenario's own xenotypes are kept.</summary>
        Scenario,
    }
}
