# Just Start — Development Notes

Just Start adds a button to the world settings page that generates the world and then starts the game
without the starting-site, ideoligion and pawn pages. It deliberately has no scenario framework: an
earlier version with scenario rules and bundled scenarios is in git history (commit `deda781`).

## How it works

- `Patches/PageFlowPatches.cs` adds the button above Generate (right-click opens the mod settings).
  Clicking it runs the same world generation as Generate, then the flow runs on the hidden
  `Page_SelectStartingSite` instead of showing it.
- `Generation/TileSelector.cs` picks the tile like `TileFinder.RandomStartingTile` (the "Select random
  site" button): weighted by `BiomeDef.settlementSelectionWeight` and the player faction's
  `minSettlementTemperatureChanceCurve`, skipping `canAutoChoose=false` biomes. With "Exclude extreme
  biomes" on, it also skips every biome with a `settleWarning`.
- `Generation/IdeologyGenerator.cs` mirrors `Page_ChooseIdeoPreset` for the chosen ideoligion mode, then
  `Scenario.PostIdeoChosen` generates the scenario's pawns as vanilla does.
- Starting season and map size stay under the world settings page's Advanced settings.

## Repository layout

```
LoadFolders.xml              Loads / then 1.6
About/                       ModMetaData, Preview
1.6/Assemblies/              Release build output (JustStart.dll) - not checked in
Languages/English/Keyed/     Translation keys
Source/JustStart/
  Core/                      Mod entry point, settings, ideoligion mode, the flow
  Generation/                Tile selection and ideoligion generation
  Patches/                   Harmony hooks into the new-game page flow
```
