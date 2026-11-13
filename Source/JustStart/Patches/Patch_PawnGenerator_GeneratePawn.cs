using HarmonyLib;
using RimWorld;
using Verse;

namespace JustStart
{
    /// <summary>Applies the starting xenotype, age range and capability settings to starting colonists generated during a Just Start run.</summary>
    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), typeof(PawnGenerationRequest))]
    public static class Patch_PawnGenerator_GeneratePawn
    {
        public static void Prefix(ref PawnGenerationRequest request)
        {
            if (request.Context != PawnGenerationContext.PlayerStarter)
                return;
            // Without Ideology the scenario's pawns are generated during world generation; with it, inside JustStartFlow.
            if (!JustStartGameStartHook.PendingAutoStart && !JustStartFlow.Running)
                return;

            if (ModsConfig.BiotechActive)
                StartingXenotype.ApplyTo(ref request);
            StartingPawnFilters.ApplyTo(ref request);
        }
    }
}
