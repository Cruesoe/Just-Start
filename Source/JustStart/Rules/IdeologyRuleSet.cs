using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace JustStart
{
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

    /// <summary>
    /// Ideology-only. A scenario may pin a mode or a pool of modes; otherwise the default ideoligion mode setting is used.
    /// The meme/precept lists apply to Fixed and Fluid and are passed to vanilla's IdeoGenerationParms.
    /// </summary>
    public class IdeologyRuleSet
    {
        public IdeologyMode? mode;

        /// <summary>If set, one of these is picked at random and replaces mode.</summary>
        public List<IdeologyMode>? modes;

        /// <summary>How many ideoligions to generate while looking for one that meets the meme/precept rules.</summary>
        public int generationAttempts = 50;

        /// <summary>Vanilla builds the ideo from exactly these plus a structure meme, with no further random memes.</summary>
        public List<MemeDef>? forcedMemes;

        public List<MemeDef>? disallowedMemes;

        public List<PreceptDef>? disallowedPrecepts;

        /// <summary>The mode to use this game, or null to fall back to the mod setting.</summary>
        public IdeologyMode? ResolveMode() => modes.NullOrEmpty() ? mode : modes!.RandomElement();

        public IEnumerable<string> ValidateReferences()
        {
            if (forcedMemes != null && forcedMemes.Count(m => m.category == MemeCategory.Structure) > 1)
                yield return "IdeologyRuleSet forces more than one structure meme.";

            if (forcedMemes != null && disallowedMemes != null)
                foreach (var meme in forcedMemes.Intersect(disallowedMemes))
                    yield return $"IdeologyRuleSet both forces and disallows meme '{meme.defName}'.";

            if (generationAttempts < 1)
                yield return $"IdeologyRuleSet has generationAttempts below 1 ({generationAttempts}).";

            bool hasMemeRules = !forcedMemes.NullOrEmpty() || !disallowedMemes.NullOrEmpty() || !disallowedPrecepts.NullOrEmpty();
            bool anyRuledMode = modes.NullOrEmpty()
                ? !(mode is IdeologyMode.Classic or IdeologyMode.Inactive)
                : modes!.Exists(m => m == IdeologyMode.Fixed || m == IdeologyMode.Fluid);
            if (hasMemeRules && !anyRuledMode)
                yield return "IdeologyRuleSet declares meme/precept rules, but none of its modes use them.";
        }
    }
}
