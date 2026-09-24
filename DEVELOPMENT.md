# Just Start — Development Notes

This document tracks implementation status against `JUST_START_DESIGN.md` (the spec this repo was
built from), and covers the repo layout and how to write a third-party scenario. It is a first-pass
framework, not a finished, playtested mod.

## Status

Implements design-doc sequence steps 1-12 as a first pass:

- Public XML extension framework (`JustStartScenarioExtension`, a `DefModExtension` attached to
  `ScenarioDef`) — Section 5.
- Composable tile-constraint rule objects (`TileConstraint_Biome`, `TileConstraint_Hilliness`,
  `TileConstraint_Temperature`),
  selected via the same `Class="..."` XML-list idiom vanilla uses for ScenParts/PatchOperations
  — Sections 6-8.
- Candidate-based tile selection pipeline that reuses `TileFinder.IsValidTileForNewSettlement`
  for vanilla validity, applies scenario constraints, and fails loudly (no silent fallback) when
  no candidate remains — Section 7.
- Pawn roles (`PawnRole`) supporting player colonists, player prisoners, and hostile on-map
  pawns, with per-role fixed equipment, xenotype rules, and gender/age/violence constraints —
  Sections 9-10.
- Xenotype rule modes: Any, AllowedPool, Required, Fixed, Excluded, SameForAll — Section 10.
- Ideology modes Inactive/Fluid/Fixed/Classic plus forced/disallowed memes and disallowed
  precepts, delegated to RimWorld's own `IdeoGenerator` rather than reimplementing meme/precept
  compatibility — Section 11.
- Any failure (invalid scenario, no matching tile, no ideoligion meeting the rules, no starting
  colonists) explains why and returns the player to the world settings page.
- Starting animals with pack-animal filtering and carried inventory assigned into the animal's
  own inventory (not spawned loose) — Section 12.
- Pre-flight scenario validation (`ScenarioValidator`) surfacing Def-reference and rule-coherence
  problems, plus DLC-gating warnings — Section 14/15.
- Five showcase scenarios (The Kindred, The Prisoner, I Got This, Mountain Dwellers, Randy's Choice),
  all consuming the same public XML path — no scenario-name branches in C#.
- A "Just Start" button placed directly above Page_CreateWorldParams's own "Generate" button, so
  world generation still works normally and Just Start is an explicit opt-in per game.

## Explicitly not done (per design doc Section 18-19)

- **Curated vanilla-restriction profiles** — the opt-in setting and the gating mechanism
  (`JustStartScenarioExtension.curatedRestrictionOnly`) exist, but no profiles are authored for
  Crashlanded/Naked Brutality/Lost Tribe/etc. Default is OFF either way.
- **Preferred memes/precepts** (weighting rather than forcing or excluding) — not implemented.
- **Pawn quality/playability safeguards** — left as TBD per spec; Just Start does not add
  unrequested quality guarantees.
- **Crashlanded/vanilla-scenario automation** — the framework supports attaching a (possibly
  empty) `JustStartScenarioExtension` to any `ScenarioDef`, including vanilla ones, which is the
  intended mechanism (Section 3-4). No patch to Core's `Scenarios.xml` is included in this pass;
  doing so is a data change (add `<modExtensions>` to the vanilla `ScenarioDef`s), not a code
  change, and should be reviewed before shipping since it touches vanilla content.

## Verification status

Compiles clean against the installed RimWorld 1.6 `Assembly-CSharp.dll` (`dotnet build`, 0
warnings/errors). API surface and Def references have been checked against the real assembly and
`Data/Core` XML, not recalled from general modding knowledge. Fixes made along the way:

- `IdeologyGenerator.cs`: `IdeoGenerationParms` has no `classicMode` parameter. Replaced with
  `forNewFluidIdeo`, which is what vanilla's own ideo-preset page (`Page_ChooseIdeoPreset`) uses to
  widen the meme pool for a freshly generated fluid ideoligion — matches this mod's
  `IdeologyMode.Fluid` semantics.
- `TileSelector.cs`: dereferenced `scenarioDef.scenario`/`.LabelCap` without a null check, even
  though the only caller (`JustStartFlow.Run`) passes `null` when no matching `ScenarioDef` is
  found. Now falls back to `Find.Scenario`.
- `XenotypeSelector.cs`: `XenotypeDef.doNotGenerateNaturally` doesn't exist in 1.6 — no such flag
  is defined on the type at all. Vanilla's own manual xenotype picker
  (`ScenPart_ConfigPage_ConfigureStartingPawns_Xenotypes`) lists every `XenotypeDef` unfiltered, so
  `AllEligible()` now does the same instead of filtering on an invented field.
- None of the five bundled scenarios originally declared a `ScenPart_GameStartDialog`, so
  third-party mods that hook that ScenPart's `PostGameStart` (e.g. Immersive Opening) had nothing
  to trigger on. All five now include one.
- The Just Start button originally relabelled and hijacked Page_CreateWorldParams's own "Generate"
  button. It's now a separate button placed above Generate, invoking the same
  `Page_CreateWorldParams.CanDoNext()` world-gen path Generate uses.

Not yet done: in-game playtesting of each bundled scenario, and confirming the relocated Just
Start button renders in the right place and doesn't visually clash with the world-settings screen
(the rect math has been checked against the vanilla `Page`/`Page_CreateWorldParams` layout, but
that's not a substitute for actually looking at it).

## Repository layout

```
LoadFolders.xml              Loads / then 1.6
About/                       ModMetaData
1.6/Defs/ScenarioDefs/       Bundled showcase scenarios, JustStart_<Scenario>.xml
1.6/Assemblies/              Release build output (JustStart.dll) - not checked in
Languages/English/Keyed/     Translation keys
Templates/                   Reference-only XML (not loaded by RimWorld - outside 1.6/Defs)
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
with vanilla defaults.

`Templates/ExampleScenario.xml` documents every field with inline comments and is a good starting
point to copy from. `1.6/Defs/ScenarioDefs/*.xml` are the five bundled scenarios, each a complete
working example of a different subset of the framework (see each file's header comment for which).
