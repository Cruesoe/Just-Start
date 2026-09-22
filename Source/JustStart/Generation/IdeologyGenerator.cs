using RimWorld;
using Verse;

namespace JustStart
{
    /// <summary>
    /// Ideology-only. Delegates to RimWorld's own IdeoGenerator for meme compatibility,
    /// precept requirement/forbid resolution, and validation (Section 11 steps 1-7) rather
    /// than reimplementing that logic - Just Start only decides *whether* and *how randomly*
    /// to invoke it. Per-meme/precept require/prefer/exclude authored via Just Start XML is a
    /// documented future extension (README "Ideology extension - future"), not implemented here.
    /// </summary>
    public static class IdeologyGenerator
    {
        public static void ApplyMode(IdeologyMode mode)
        {
            if (!ModsConfig.IdeologyActive || mode == IdeologyMode.Inactive)
                return;

            var parms = new IdeoGenerationParms(
                Faction.OfPlayer.def,
                forceNoExpansionIdeo: false,
                forceNoWeaponPreference: false,
                classicMode: mode == IdeologyMode.Fluid);

            Ideo ideo = IdeoGenerator.GenerateIdeo(parms);
            Faction.OfPlayer.ideos.SetPrimary(ideo);
        }
    }
}
