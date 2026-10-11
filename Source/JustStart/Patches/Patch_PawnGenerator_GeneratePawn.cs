using HarmonyLib;
using RimWorld;
using Verse;

namespace JustStart
{
    /// <summary>Forces the starting xenotype setting onto starting colonists generated during a Just Start run.</summary>
    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), typeof(PawnGenerationRequest))]
    public static class Patch_PawnGenerator_GeneratePawn
    {
        public static void Prefix(ref PawnGenerationRequest request)
        {
            if (!ModsConfig.BiotechActive || request.Context != PawnGenerationContext.PlayerStarter)
                return;
            // Without Ideology the scenario's pawns are generated during world generation; with it, inside JustStartFlow.
            if (!JustStartGameStartHook.PendingAutoStart && !JustStartFlow.Running)
                return;

            StartingXenotype.ApplyTo(ref request);
        }
    }
}
