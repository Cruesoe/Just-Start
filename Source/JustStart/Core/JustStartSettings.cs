using Verse;

namespace JustStart
{
    public class JustStartSettings : ModSettings
    {
        /// <summary>When on, biomes with a settle warning are never picked. Off by default.</summary>
        public bool excludeExtremeBiomes = false;

        /// <summary>Ideoligion mode for the player; only used with Ideology active.</summary>
        public IdeologyMode defaultIdeologyMode = IdeologyMode.Fixed;

        /// <summary>How starting colonists' xenotypes are set; only used with Biotech active.</summary>
        public StartingXenotypeMode xenotypeMode = StartingXenotypeMode.Xenotype;

        /// <summary>defName of the xenotype for StartingXenotypeMode.Xenotype, and the fallback for a missing custom xenotype.</summary>
        public string startingXenotype = "Baseliner";

        /// <summary>File name of the custom xenotype for StartingXenotypeMode.Custom.</summary>
        public string startingCustomXenotype = string.Empty;

        /// <summary>When on, mixed also picks from the player's custom xenotypes.</summary>
        public bool mixedIncludesCustom = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref excludeExtremeBiomes, "excludeExtremeBiomes", false);
            Scribe_Values.Look(ref defaultIdeologyMode, "defaultIdeologyMode", IdeologyMode.Fixed);
            Scribe_Values.Look(ref xenotypeMode, "xenotypeMode", StartingXenotypeMode.Xenotype);
            Scribe_Values.Look(ref startingXenotype, "startingXenotype", "Baseliner");
            Scribe_Values.Look(ref startingCustomXenotype, "startingCustomXenotype", string.Empty);
            Scribe_Values.Look(ref mixedIncludesCustom, "mixedIncludesCustom", false);
            base.ExposeData();
        }
    }
}
