using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace JustStart
{
    /// <summary>
    /// Runs Just Start once world generation finishes: picks the tile, sets up the ideoligion, accepts the scenario's
    /// starting pawns and starts the game. Any failure explains why and calls onFailure.
    /// </summary>
    public static class JustStartFlow
    {
        public static void Run(Action onFailure)
        {
            // Forced-map scenarios already chose their tile and map generator during world generation.
            if (!Find.Scenario.AllParts.OfType<ScenPart_ForcedMap>().Any())
            {
                if (!TileSelector.TryFindTile(out PlanetTile tile))
                {
                    Fail("JustStart_ErrorNoValidTile".Translate(), onFailure);
                    return;
                }
                Find.GameInitData.startingTile = tile;
            }

            if (!Find.GameInitData.startingTile.Valid)
            {
                Fail("JustStart_ErrorNoValidTile".Translate(), onFailure);
                return;
            }

            if (ModsConfig.IdeologyActive)
            {
                IdeologyGenerator.ApplyMode(JustStartMod.Settings.defaultIdeologyMode);
                // Sets startingPawnCount and generates the scenario's starting pawns; without Ideology, world generation already did this.
                Find.Scenario.PostIdeoChosen();
            }

            if (Find.GameInitData.startingPawnCount < 1)
            {
                Fail("JustStart_ErrorNoStartingPawns".Translate(), onFailure);
                return;
            }

            PageUtility.InitGameStart();
        }

        private static void Fail(string message, Action onFailure)
        {
            onFailure();
            Find.WindowStack.Add(new Dialog_MessageBox(message + "\n\n" + "JustStart_ErrorAdjustWorld".Translate()));
        }
    }
}
