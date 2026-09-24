using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace JustStart
{
    /// <summary>
    /// Public XML extension point: a &lt;modExtensions&gt; entry on a &lt;ScenarioDef&gt; that opts it into Just Start and
    /// declares its rules. Every field is optional; an empty extension still auto-picks a tile and generates pawns.
    /// </summary>
    public class JustStartScenarioExtension : DefModExtension
    {
        /// <summary>If true, tileConstraints only apply with the "use curated vanilla restrictions" setting on.</summary>
        public bool curatedRestrictionOnly = false;

        /// <summary>Rules the starting tile must satisfy. Unset means any vanilla-valid tile.</summary>
        public List<TileConstraint>? tileConstraints;

        /// <summary>
        /// Player FactionDef defNames, most preferred first; the first one loaded replaces the scenario's player faction at startup.
        /// Strings, so naming a faction from an absent mod is not an error.
        /// </summary>
        public List<string>? preferredPlayerFactions;

        /// <summary>Also allows biomes vanilla never picks at random (canAutoChoose=false, e.g. sea ice, glacial plain).</summary>
        public bool allowAnyBiome = false;

        /// <summary>If set, one is picked at random and replaces the scenario's own arrival method.</summary>
        public List<PlayerPawnsArriveMethod>? arrivalMethods;

        /// <summary>If set, one is picked at random; otherwise the season stays vanilla's latitude-based default.</summary>
        public List<Season>? startingSeasons;

        /// <summary>Starting pawns by role. With no PlayerColonist role, the scenario's own parts generate the colonists.</summary>
        public List<PawnRole>? pawnRoles;

        /// <summary>Xenotype rule shared by every PlayerColonist role without its own, resolved across all of them together.</summary>
        public XenotypeRuleSet? xenotypeRules;

        /// <summary>Ideoligion mode and meme/precept rules. Unset falls back to the default ideoligion mode setting.</summary>
        public IdeologyRuleSet? ideologyRules;

        /// <summary>Starting animals (species, count, carried inventory).</summary>
        public List<AnimalRole>? animalRoles;

        /// <summary>Items left on the map for the player to find, each a random pick from its own options.</summary>
        public List<MapThing>? mapThings;

        public IEnumerable<string> ValidateReferences(ScenarioDef scenarioDef)
        {
            int sharedRuleColonists = pawnRoles?
                .Where(r => r.faction == PawnRoleFaction.PlayerColonist && r.xenotypeRules == null)
                .Sum(r => r.MinCount) ?? 0;

            return All(tileConstraints, c => c.ValidateReferences())
                .Concat(All(pawnRoles, r => r.ValidateReferences()))
                .Concat(xenotypeRules?.ValidateReferences(sharedRuleColonists) ?? Enumerable.Empty<string>())
                .Concat(All(animalRoles, r => r.ValidateReferences()))
                .Concat(All(mapThings, t => t.ValidateReferences()))
                .Concat(ideologyRules?.ValidateReferences() ?? Enumerable.Empty<string>())
                .Select(msg => $"[{scenarioDef.defName}] {msg}");
        }

        private static IEnumerable<string> All<T>(List<T>? items, Func<T, IEnumerable<string>> validate) =>
            items?.SelectMany(validate) ?? Enumerable.Empty<string>();
    }
}
