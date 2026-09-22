# Just Start — Implementation Design Plan

Document purpose
This document is an implementation specification for an AI coding agent building the RimWorld mod "Just Start". Treat confirmed requirements as authoritative. Items explicitly marked TBD, proposed, or implementation-dependent must not be invented or silently resolved. Verify RimWorld 1.6 APIs and Def names against the actual game assemblies/XML before coding.

## Target
- Game: RimWorld 1.6.
- DLC-aware: Core first, with optional support for Ideology, Biotech, Odyssey, and other installed DLC.
- Primary implementation strategy: C# supplies reusable machinery; XML defines scenario-specific data and constraints wherever practical.
- Compatibility goal: use Def references and generic systems rather than hard-coding only vanilla content.

## 1. Product Definition

Just Start is a controlled-random-start framework.

Guiding rule: "You configure the game. Just Start deals you the hand."

The mod must remove optimisation-heavy pre-game choices while preserving meaningful configuration. It is not a "randomise everything" mod. The player chooses the game parameters and scenario; Just Start chooses valid outcomes inside the applicable rules.

Core framework rule: "Scenario defines constraints. Just Start randomly selects within those constraints."

Do not add inferred restrictions because they seem sensible. Only vanilla validity rules, explicit Just Start rules, and player-enabled curated vanilla restrictions may constrain an outcome.

## 2. Required Player Flow

Preserve the normal player-controlled choices for starting scenario; storyteller and difficulty; world-generation configuration, including normal vanilla/modded world settings. The player must still see and configure world generation. Do not bypass world generation.

After world configuration, provide a Just Start action. Once activated, Just Start takes responsibility for the optimisation-prone choices that follow: selecting the starting settlement tile; generating/accepting starting pawns without player reroll optimisation; handling ideology according to the configured Just Start ideology mode when Ideology is active.

Exact UI insertion point and exact button wording/placement are implementation questions. Inspect RimWorld 1.6 new-game flow before choosing patches/hooks.

## 3. Vanilla Scenario Behaviour

Vanilla scenarios must remain vanilla by default. Use Crashlanded as the implementation reference case: preserve its normal pawn count, equipment/resources, and ScenarioParts; do not impose a Just Start biome restriction by default; automatically choose a valid settlement tile; generate/accept starting pawns without exposing the normal optimisation/reroll loop. Apply the same principle to other vanilla scenarios: automate choices without rewriting the scenario. Do not hard-code "sensible" biome/xenotype combinations.

## 4. Optional Curated Restrictions for Vanilla Scenarios

Add a player setting that opts vanilla scenarios into Just Start-authored location restrictions. Default: OFF. OFF: vanilla scenarios use any otherwise-valid location. ON: supported vanilla scenarios use curated Just Start location profiles, each with its own allowed/required location rules. The actual curated profiles are TBD; do not invent final lists. Make these profiles data-driven where practical.

## 5. Public XML Extension Framework

Core requirement. Just Start must expose as much scenario configuration through XML as practical so third-party mod authors can create compatible scenarios without compiling a DLL. C# implements parsing/validation/generation/selection/integration/error-handling/reusable rule evaluators; XML declares scenario-specific constraints and content. Third-party XML should reference vanilla or modded Defs by defName where appropriate. Do not finalise the schema prematurely — model capabilities first, then build a coherent extensible schema. Optional fields degrade to vanilla/default behaviour; invalid references produce diagnostics; DLC-specific rules fail gracefully when the DLC is inactive; avoid hard-coding checks for only the five bundled scenarios; prefer composable rule objects over scenario-name conditionals; leave room for modded Defs.

## 6. Controlled Randomness Types

Capable of representing (where relevant): fixed values; allowed pools; exclusions; required minimums/counts; weighted choices; unrestricted random selection; cross-property rules only when explicitly authored. Build reusable primitives where they naturally fit; don't implement every theoretical type everywhere.

## 7. Starting-Tile Selection

Candidate-based pipeline: obtain the generated world; enumerate valid candidate tiles; apply applicable scenario constraints; randomly select among what remains; continue into game start. Never silently violate a required constraint. If zero candidates remain: stop the automatic start, tell the player the world has no valid location for the scenario, let them return to/change/regenerate world settings, and do not silently fall back to an unrestricted tile. Constraint framework must be broader than biome-only. Required/known: biome, hilliness/mountain requirement. Future/extensible: coast, temperature, roads, rivers, faction proximity, pollution, other tile properties.

## 8. Biome Support

Expected ordinary planetary starting biomes (Core + DLC) are listed for design purposes only — do not treat as the authoritative runtime source; use Def-driven lookup and verify against actual 1.6 Defs. Special/pocket maps, ocean/lake, orbit, and other nonstandard biomes are not ordinary Just Start choices unless normal validity and explicit scenario rules make them appropriate.

## 9. Pawn Generation

Prevent the normal optimisation loop as far as practical while still producing a valid start. Respect the scenario's required pawn count and normal generation rules unless Just Start XML explicitly overrides them. Do not add unrequested quality guarantees. Use RimWorld generation systems rather than reimplementing. Minimum playability safeguards are TBD. XML must support scenario-specific pawn roles/content for the bundled examples, including player and non-player/hostile pawns.

## 10. Xenotype Rules

Biotech-only. Scenario XML must be able to constrain xenotypes: Any, Allowed pool, Required (count/minimum), Fixed (slot/role), Excluded, Same xenotype (one random eligible xenotype applied to all applicable colonists). Do not infer biome suitability from xenotype unless an explicit scenario rule creates that relationship. Use Def-driven references, support modded XenotypeDefs.

## 11. Ideology Modes

Ideology-only. Configurable modes: Inactive (no Just Start ideology generation); Fluid (random valid fluid ideology); Fixed (random but coherent fixed ideology, generated in layers: core/memes → compatible additional memes → meme-required/forbidden precepts → randomised remaining compatible optional precepts, preferring ordinary over extreme positions unless memes justify it → compatible rituals/roles/buildings/relics/cosmetic identity → validate → repair/regenerate invalid combinations). Reuse RimWorld's own compatibility/validation systems wherever possible; verify available 1.6 APIs. Future scenario XML may require/prefer/allow/exclude ideology features; not yet fully specified.

## 12. Starting Animals and Carried Inventory

Needed for Mountain Dwellers: starting animal count; fixed species and/or eligible pool; pack-animal requirement or equivalent validator; starting inventory assigned to the animal (not merely spawned on the ground). Prefer existing RimWorld inventory/caravan/animal systems. Exact XML syntax is TBD.

## 13. Bundled Showcase Scenarios

Ship five custom scenarios, both playable and examples of the public framework, each demonstrating a meaningfully different capability, consuming the same framework as third-party authors (no scenario-name-specific C#).

- **Lost Colony** — starting colony shares one randomly selected eligible xenotype; player doesn't choose it. Demonstrates xenotype randomisation/constraints. Pawn count/supplies/biome restrictions/narrative TBD unless already supplied by normal scenario data.
- **The Prisoner** (exact title) — one prisoner, one guard; guard is hostile on the starting map and better equipped than the prisoner. Demonstrates distinct pawn roles, non-colonist pawn spawning, faction/hostility state, immediate hostile map entities. Exact equipment/generation constraints TBD.
- **I Got This** — ultimate challenge: one starting pawn, one pistol, required Sea Ice biome. Demonstrates exact biome restriction plus minimal/fixed loadout. Same scenario as the "Sea Ice ultimate challenge" — do not create a separate one.
- **Mountain Dwellers** — Dwarf Fortress-inspired embark: exactly seven starting colonists (intentional, keep unless explicitly changed), mountainous starting tile required, pack animal carrying expedition supplies. Demonstrates non-biome tile constraint, high fixed pawn count, starting animal, animal-carried inventory. Exact pack-animal species/pool and supply manifest TBD.
- **Scenario 5 — TBD.** Do not invent or implement yet. Must be meaningfully different from the first four and showcase an important XML capability not already demonstrated (candidates: ideology constraints, relationships/family structures, faction relations, multi-group/arrival rules, or another capability identified during framework design).

## 14. Scenario Validation

Validate before committing to a generated start, at minimum: referenced Defs exist and are available with the current DLC/mod set; required tile constraints can be evaluated; xenotype rules are internally satisfiable for the requested pawn count; required pawn roles/counts are coherent; starting animal/load rules can be resolved; generated ideology is valid when used. Prefer actionable diagnostics identifying the failing scenario/rule. A particular generated world having zero valid tiles is a recoverable user-facing world-generation problem, not permission to ignore constraints.

## 15. DLC and Mod Compatibility

Base mod must not assume all DLC are active. Gate Biotech-specific xenotype functionality, Ideology-specific ideology functionality, Odyssey-specific content/Defs. Do not reference DLC-only classes/Defs in ways that break loading without that DLC. Prefer Def existence/feature checks and isolated integration code. Support modded biome/xenotype/animal Defs where the underlying type is compatible. Third-party Just Start XML should be able to depend on its own mod/DLC normally.

## 16. Implementation Strategy

Build the framework before polishing the bundled scenarios. Recommended sequence: inspect RimWorld 1.6 new-game/world-gen/settlement-tile/scenario/pawn-generation code paths; identify the least invasive hooks/patch points for a Just Start action; implement a generic scenario-extension Def/XML model; implement rule validation and diagnostics; implement automatic valid tile selection with biome and hilliness constraints; prove the pipeline using vanilla Crashlanded with no Just Start restrictions; add pawn automation; add Biotech/xenotype rule support; add hostile/special pawn role support required by The Prisoner; add starting animal and carried-inventory support required by Mountain Dwellers; add ideology modes using vanilla validation/generation where possible; implement the four confirmed showcase scenarios entirely through the public data/framework path; add curated vanilla restrictions only after their profiles are designed; design scenario five after reviewing which public XML capability still needs a showcase; document the XML API with examples drawn from the bundled scenarios. This sequence is guidance, not permission to invent unresolved gameplay rules.

## 17. Acceptance Criteria

- A player can choose a normal scenario, difficulty/storyteller, and world-generation settings, then invoke Just Start.
- Crashlanded can start without manually choosing a tile or rerolling/selecting pawns, while otherwise preserving vanilla Crashlanded behaviour.
- Vanilla scenarios receive no Just Start biome restrictions unless the optional curated-restrictions setting is enabled.
- A custom scenario can constrain the start to specified biome Defs through the public framework.
- A custom scenario can require mountainous terrain.
- Impossible world-location constraints produce a clear recoverable error rather than a silent fallback.
- Biotech-enabled scenarios can express the required xenotype rule types.
- The Prisoner can create one player prisoner and one hostile guard using generic framework features.
- I Got This can force Sea Ice and a one-pawn/one-pistol start.
- Mountain Dwellers can force a mountainous tile, create seven starting colonists, and create a pack animal carrying supplies.
- The four confirmed bundled scenarios do not rely on hard-coded scenario-name branches.
- DLC-specific functionality does not break the mod when the DLC is absent.
- XML errors and impossible constraints produce useful diagnostics.
- Third-party authors can use the same public XML mechanisms as the bundled scenarios.

## 18. Explicit Non-Goals / Do Not Assume

Until separately designed, do not: invent the fifth showcase scenario; invent curated biome lists for vanilla scenarios; remove the world-generation screen; turn Just Start into a total-random-start mod; automatically match xenotypes to biomes; silently relax impossible constraints; add player pawn rerolling as part of the Just Start flow; hard-code the bundled scenario behaviour by scenario name; assume RimWorld 1.7 APIs; treat the biome list in this document as a substitute for inspecting RimWorld 1.6 Defs; finalise a public XML schema before checking how the required systems map cleanly onto RimWorld's existing Def architecture.

## 19. Open Decisions

Do not resolve without further design input unless required for a minimal technical prototype: exact Just Start UI/button location; exact curated restrictions for vanilla scenarios; final scenario five; final Lost Colony pawn count/loadout/location rules; final Prisoner equipment and generation constraints; final Mountain Dwellers pack animal and supply manifest; pawn quality/playability safeguards; full list of tile constraint types for v1; public XML element/class names and final schema; detailed ideology-constraint XML beyond the global Inactive/Fluid/Fixed modes; whether and how scenario selection itself can be randomized from an eligible pool.
