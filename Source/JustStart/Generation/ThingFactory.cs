using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace JustStart
{
    /// <summary>
    /// Makes items from XML ThingDefCountClass entries, honouring count, stuff (else the def's default), quality and chance.
    /// Written either as &lt;Gun_Revolver&gt;1&lt;/Gun_Revolver&gt; or with child elements, e.g.
    /// &lt;Gun_Revolver&gt;&lt;stuff&gt;Steel&lt;/stuff&gt;&lt;quality&gt;Good&lt;/quality&gt;&lt;/Gun_Revolver&gt;.
    /// </summary>
    public static class ThingFactory
    {
        /// <summary>Empty when the chance roll fails; counts above the stack limit (1 for apparel and weapons) become several things.</summary>
        public static List<Thing> Make(ThingDefCountClass entry)
        {
            var things = new List<Thing>();
            if (entry.chance.HasValue && !Rand.Chance(entry.chance.Value))
                return things;

            ThingDef stuff = entry.stuff ?? GenStuff.DefaultStuffFor(entry.thingDef);
            for (int remaining = entry.count; remaining > 0; remaining -= things[things.Count - 1].stackCount)
            {
                Thing thing = ThingMaker.MakeThing(entry.thingDef, stuff);
                thing.stackCount = Math.Min(remaining, entry.thingDef.stackLimit);
                thing.TryGetComp<CompQuality>()?.SetQuality(entry.quality, ArtGenerationContext.Outsider);
                things.Add(thing);
            }
            return things;
        }
    }
}
