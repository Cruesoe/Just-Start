using HarmonyLib;
using UnityEngine;
using Verse;

namespace JustStart
{
    /// <summary>Mod entry point: Harmony init + settings window.</summary>
    public class JustStartMod : Mod
    {
        public static JustStartMod Instance = null!;

        public static JustStartSettings Settings = null!;

        public JustStartMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<JustStartSettings>();

            var harmony = new Harmony("cruesoe.juststart");
            harmony.PatchAll();
        }

        public override string SettingsCategory() => "Just Start";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            SettingsPage.Draw(inRect);
            base.DoSettingsWindowContents(inRect);
        }
    }
}
