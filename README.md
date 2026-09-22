# Just Start

"You configure the game. Just Start deals you the hand."

A controlled-random-start framework for RimWorld 1.6. The player configures scenario,
storyteller, difficulty, and world generation as normal; Just Start automates the
optimisation-heavy choices that follow (starting tile hunting, pawn rerolling) while staying
inside the constraints the active scenario declares.

This document tracks implementation status against `JUST_START_DESIGN.md` (the spec this repo
was built from). It is a first-pass framework, not a finished, playtested mod.

## Status

Implements design-doc sequence steps 1-12 as a first pass:

- Public XML extension framework (`JustStartScenarioExtension`, a `DefModExtension` attached to
  `ScenarioDef`) — Section 5.
- Composable tile-constraint rule objects (`TileConstraint_Biome`, `TileConstraint_Hilliness`),
  selected via the same `Class="..."` XML-list idiom vanilla uses for ScenParts/PatchOperations
  — Sections 6-8.
- Candidate-based tile selection pipeline that reuses `TileFinder.IsValidTileForNewSettlement`
  for vanilla validity, applies scenario constraints, and fails loudly (no silent fallback) when
  no candidate remains — Section 7.
- Pawn roles (`PawnRole`) supporting player colonists, player prisoners, and hostile on-map
  pawns, with per-role fixed equipment and xenotype rules — Sections 9-10.
- Xenotype rule modes: Any, AllowedPool, Required, Fixed, Excluded, SameForAll — Section 10.
- Ideology modes Inactive/Fluid/Fixed, delegated to RimWorld's own `IdeoGenerator` rather than
  reimplementing meme/precept compatibility — Section 11.
- Starting animals with pack-animal filtering and carried inventory assigned into the animal's
  own inventory (not spawned loose) — Section 12.
- Pre-flight scenario validation (`ScenarioValidator`) surfacing Def-reference and rule-coherence
  problems, plus DLC-gating warnings — Section 14/15.
- Four of the five confirmed showcase scenarios (Lost Colony, The Prisoner, I Got This, Mountain
  Dwellers), all consuming the same public XML path — no scenario-name branches in C#.

## Explicitly not done (per design doc Section 18-19)

- **Scenario five** — not designed, not invented.
- **Curated vanilla-restriction profiles** — the opt-in setting and the gating mechanism
  (`JustStartScenarioExtension.curatedRestrictionOnly`) exist, but no profiles are authored for
  Crashlanded/Naked Brutality/Lost Tribe/etc. Default is OFF either way.
- **Per-meme/precept ideology XML** (require/prefer/exclude a specific meme or precept from
  scenario XML) — only the global Inactive/Fluid/Fixed mode is implemented. Noted as a documented
  future extension point in `IdeologyRuleSet`.
- **Pawn quality/playability safeguards** — left as TBD per spec; Just Start does not add
  unrequested quality guarantees.
- **Crashlanded/vanilla-scenario automation** — the framework supports attaching a (possibly
  empty) `JustStartScenarioExtension` to any `ScenarioDef`, including vanilla ones, which is the
  intended mechanism (Section 3-4). No patch to Core's `Scenarios.xml` is included in this pass;
  doing so is a data change (add `<modExtensions>` to the vanilla `ScenarioDef`s), not a code
  change, and should be reviewed before shipping since it touches vanilla content.

## Known verification gaps — read before building

**This environment has no local RimWorld installation**, so nothing here has been compiled
against the real `Assembly-CSharp.dll`, run in-game, or verified against actual 1.6 Def names.
Per the design doc's own instruction ("verify RimWorld 1.6 APIs and Def names against the actual
game assemblies/XML before coding"), treat every API/Def reference below as needing a check pass
with ILSpy/dnSpy or a local build before this mod is trusted:

- `Source/JustStart/Patches/PageFlowPatches.cs` is the highest-risk file: it assumes
  `Page_CreateWorldParams`, `Page_SelectStartingSite`, `PageUtility.InitGameStart()`, and
  `Page.DoNext()`/`CanDoNext()` exist with those names/signatures. This is the exact insertion
  point the design doc calls out as needing investigation (Section 2, Section 16 step 2) — it was
  not investigated against real game code here.
- `RimWorld.Planet.PlanetTile` is assumed to be the 1.6 tile-addressing struct (introduced
  alongside Odyssey's multi-surface support) with a `(int, Surface)` constructor and a
  `world.grid[tile]` indexer exposing `PrimaryBiome`/`hilliness`. Verify against the installed
  RimWorld.Planet assembly.
- `TileFinder.IsValidTileForNewSettlement(PlanetTile)` is assumed to be a public static method
  usable outside the vanilla landing-site page. If it isn't public, this needs a Harmony
  reverse-patch or an access transpiler instead.
- All `ThingDef`/`PawnKindDef`/`BiomeDef` defNames used in the bundled scenario XML (`Gun_Revolver`,
  `Gun_PumpShotgun`, `Apparel_FlakJacket`, `Apparel_FlakVest`, `Pirate`, `SeaIce`,
  `MealSurvivalPack`) are recalled from general RimWorld modding knowledge, not confirmed against
  1.6's actual Defs. Cross-check with `Data/Core` (and `Data/Odyssey` for biome coverage per
  Section 8) before relying on them.
- Ideology `IdeoGenerationParms` constructor signature in `IdeologyGenerator.cs` is a best guess
  at the 1.6 API surface.

Build locally with `RimWorldInstallDir` pointed at a real install (see `JustStart.csproj`), fix
whatever has drifted, and playtest each bundled scenario before treating any acceptance criterion
in Section 17 of the design doc as met.

## Repository layout

```
About/                      ModMetaData, LoadFolders
1.6/Defs/ScenarioDefs/       Bundled showcase scenarios (public-XML consumers)
1.6/Assemblies/              Build output (JustStart.dll) - not checked in
Languages/English/Keyed/     Translation keys
Source/JustStart/
  Core/                      Mod entry point, settings, the ScenarioExtension Def, the
                             orchestrating flow, validation
  Rules/                     Composable rule classes (tile constraints, xenotype rules,
                             pawn roles, animal roles, ideology mode) - the public schema
  Generation/                Tile/pawn/animal/ideology generation pipelines
  Patches/                   Harmony hooks into the new-game page flow
```

## Writing a third-party scenario

Attach a `JustStart.JustStartScenarioExtension` to your `ScenarioDef`'s `<modExtensions>`. Every
field is optional; an empty extension still gets automatic tile selection and pawn generation
with vanilla defaults. See `1.6/Defs/ScenarioDefs/*.xml` for worked examples of tile constraints,
per-role xenotype rules, fixed equipment, and pack-animal carried inventory.
