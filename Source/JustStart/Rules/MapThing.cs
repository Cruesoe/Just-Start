using System.Collections.Generic;
using Verse;

namespace JustStart
{
    /// <summary>
    /// An item left on the starting map for the player to reach: one of <see cref="options"/>,
    /// chosen at random, placed <see cref="distance"/> cells from the player's start spot. Each
    /// option is a normal item entry, so it can set count, stuff, quality and chance.
    /// </summary>
    public class MapThing
    {
        public List<ThingDefCountClass>? options;
        public IntRange distance = new IntRange(8, 15);

        public IEnumerable<string> ValidateReferences()
        {
            if (options.NullOrEmpty())
                yield return "MapThing declared with no options.";
            if (distance.min < 0 || distance.max < distance.min)
                yield return $"MapThing has an invalid distance ({distance}).";
        }
    }
}
