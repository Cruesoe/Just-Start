using System.Collections.Generic;
using Verse;

namespace JustStart
{
    public class JustStartSettings : ModSettings
    {
        // Starting site

        /// <summary>defNames of the biomes the site may be in; empty allows any.</summary>
        public List<string> allowedBiomes = new List<string>();

        /// <summary>When on, biomes with a settle warning are never picked. Off by default.</summary>
        public bool excludeExtremeBiomes = false;

        public bool allowFlat = true;
        public bool allowSmallHills = true;
        public bool allowLargeHills = true;
        public bool allowMountainous = true;

        public bool requireRiver = false;
        public bool requireCoast = false;
        public bool requireRoad = false;

        /// <summary>Minimum outdoor growing period in days (multiples of 5); 0 allows any.</summary>
        public int minGrowingDays = 0;

        /// <summary>The tile's average temperature (Celsius) must fall within this; the full slider range allows any.</summary>
        public FloatRange temperatureRange = new FloatRange(TileSelector.MinTemperature, TileSelector.MaxTemperature);

        /// <summary>Minimum distance in tiles from any hostile settlement; 0 allows any.</summary>
        public int minHostileDistance = 0;

        // Ideoligion

        /// <summary>Ideoligion mode for the player; only used with Ideology active.</summary>
        public IdeologyMode defaultIdeologyMode = IdeologyMode.Fixed;

        /// <summary>defNames of normal memes a random ideoligion never gets.</summary>
        public List<string> excludedMemes = new List<string>();

        // Colonists

        /// <summary>How starting colonists' xenotypes are set; only used with Biotech active.</summary>
        public StartingXenotypeMode xenotypeMode = StartingXenotypeMode.Xenotype;

        /// <summary>defName of the xenotype for StartingXenotypeMode.Xenotype, and the fallback for a missing custom xenotype.</summary>
        public string startingXenotype = "Baseliner";

        /// <summary>File name of the custom xenotype for StartingXenotypeMode.Custom.</summary>
        public string startingCustomXenotype = string.Empty;

        /// <summary>When on, mixed also picks from the player's custom xenotypes.</summary>
        public bool mixedIncludesCustom = false;

        /// <summary>Starting colonists' biological ages fall within this; the full slider range allows any.</summary>
        public IntRange startingAgeRange = new IntRange(StartingPawnFilters.MinAge, StartingPawnFilters.MaxAge);

        public GenderMix genderMix = GenderMix.Any;

        public int minPassions = 0;
        public int minMajorPassions = 0;

        /// <summary>Trait keys ("defName:degree") starting colonists never have.</summary>
        public List<string> excludedTraits = new List<string>();

        /// <summary>When on, starting colonists are generated with no disabled work.</summary>
        public bool requireCapableColonists = false;
        public bool requireViolenceCapable = false;

        public bool noAddictions = false;
        public bool noPregnancies = false;
        public bool noRelations = false;

        public void ResetToDefaults()
        {
            var defaults = new JustStartSettings();
            allowedBiomes = defaults.allowedBiomes;
            excludeExtremeBiomes = defaults.excludeExtremeBiomes;
            allowFlat = defaults.allowFlat;
            allowSmallHills = defaults.allowSmallHills;
            allowLargeHills = defaults.allowLargeHills;
            allowMountainous = defaults.allowMountainous;
            requireRiver = defaults.requireRiver;
            requireCoast = defaults.requireCoast;
            requireRoad = defaults.requireRoad;
            minGrowingDays = defaults.minGrowingDays;
            temperatureRange = defaults.temperatureRange;
            minHostileDistance = defaults.minHostileDistance;
            defaultIdeologyMode = defaults.defaultIdeologyMode;
            excludedMemes = defaults.excludedMemes;
            xenotypeMode = defaults.xenotypeMode;
            startingXenotype = defaults.startingXenotype;
            startingCustomXenotype = defaults.startingCustomXenotype;
            mixedIncludesCustom = defaults.mixedIncludesCustom;
            startingAgeRange = defaults.startingAgeRange;
            genderMix = defaults.genderMix;
            minPassions = defaults.minPassions;
            minMajorPassions = defaults.minMajorPassions;
            excludedTraits = defaults.excludedTraits;
            requireCapableColonists = defaults.requireCapableColonists;
            requireViolenceCapable = defaults.requireViolenceCapable;
            noAddictions = defaults.noAddictions;
            noPregnancies = defaults.noPregnancies;
            noRelations = defaults.noRelations;
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref allowedBiomes, "allowedBiomes", LookMode.Value);
            Scribe_Values.Look(ref excludeExtremeBiomes, "excludeExtremeBiomes", false);
            Scribe_Values.Look(ref allowFlat, "allowFlat", true);
            Scribe_Values.Look(ref allowSmallHills, "allowSmallHills", true);
            Scribe_Values.Look(ref allowLargeHills, "allowLargeHills", true);
            Scribe_Values.Look(ref allowMountainous, "allowMountainous", true);
            Scribe_Values.Look(ref requireRiver, "requireRiver", false);
            Scribe_Values.Look(ref requireCoast, "requireCoast", false);
            Scribe_Values.Look(ref requireRoad, "requireRoad", false);
            Scribe_Values.Look(ref minGrowingDays, "minGrowingDays", 0);
            Scribe_Values.Look(ref temperatureRange, "temperatureRange", new FloatRange(TileSelector.MinTemperature, TileSelector.MaxTemperature));
            Scribe_Values.Look(ref minHostileDistance, "minHostileDistance", 0);
            Scribe_Values.Look(ref defaultIdeologyMode, "defaultIdeologyMode", IdeologyMode.Fixed);
            Scribe_Collections.Look(ref excludedMemes, "excludedMemes", LookMode.Value);
            Scribe_Values.Look(ref xenotypeMode, "xenotypeMode", StartingXenotypeMode.Xenotype);
            Scribe_Values.Look(ref startingXenotype, "startingXenotype", "Baseliner");
            Scribe_Values.Look(ref startingCustomXenotype, "startingCustomXenotype", string.Empty);
            Scribe_Values.Look(ref mixedIncludesCustom, "mixedIncludesCustom", false);
            Scribe_Values.Look(ref startingAgeRange, "startingAgeRange", new IntRange(StartingPawnFilters.MinAge, StartingPawnFilters.MaxAge));
            Scribe_Values.Look(ref genderMix, "genderMix", GenderMix.Any);
            Scribe_Values.Look(ref minPassions, "minPassions", 0);
            Scribe_Values.Look(ref minMajorPassions, "minMajorPassions", 0);
            Scribe_Collections.Look(ref excludedTraits, "excludedTraits", LookMode.Value);
            Scribe_Values.Look(ref requireCapableColonists, "requireCapableColonists", false);
            Scribe_Values.Look(ref requireViolenceCapable, "requireViolenceCapable", false);
            Scribe_Values.Look(ref noAddictions, "noAddictions", false);
            Scribe_Values.Look(ref noPregnancies, "noPregnancies", false);
            Scribe_Values.Look(ref noRelations, "noRelations", false);
            // An empty or missing list loads as null.
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                allowedBiomes ??= new List<string>();
                excludedMemes ??= new List<string>();
                excludedTraits ??= new List<string>();
            }
            base.ExposeData();
        }
    }
}
