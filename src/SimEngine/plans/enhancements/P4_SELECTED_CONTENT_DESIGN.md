# P4-C3a — Scenario discovery without resolving unused gameplay

Status: focused-verified under the approved P4 composition stage, build 2.23.0 / save 11;
128 checks / two exact suites, zero failures. See
[verification](../../docs/migration/P4_SELECTED_CONTENT_VERIFICATION.md). Whole P4 remains partial.

## Problem and goals

GameFactory currently resolves every registered module to discover scenarios, their clocks and input
maps, then resolves the chosen modules again. An invalid unused entity/provider can therefore prevent
starting a valid scenario. Discovery also invokes unrelated def generators/Resolve callbacks.

ContentSet remains the sole source. Discovery projects the explicitly listed raw engine-owned scenario
and input declarations using their existing strict Read implementations. It does not resolve a world,
run an extension callback, create a second catalog, or retain a second set of typed definitions.
Only the chosen scenario and requested modules enter ResolvedContent's existing cache.

## Changes and ownership

- ResolvedContent owns one shared authored-definition read routine. Actual resolution and transient
  engine declaration projections use it; no duplicated scenario field parser.
- ContentSet freezes loaded JSON. Internal discovery can read only a captured engine-owned section
  with its exact declared type. It never accepts an extension section as a discovery provider.
- Find scans IDs without reading unrelated declaration bodies, rejects duplicate matching IDs, then
  strictly reads the requested declaration. Listing reads the scenario declarations only. Bad menu
  metadata can fail listing, but an unrelated bad scenario body cannot prevent selecting a valid ID.
- ResolveScenario selects the declared modules plus explicit requested additions through the captured
  dependency graph, checks that the declaring owner is present, then returns the canonical ScenarioDef
  from the sole selected ResolvedContent. Missing time/space/provider references fail on selection.
- GameFactory.NewGame/ScenarioTime use this route. Scenarios returns detached immutable existing
  ScenarioInfo projections. InputMap reads its core declaration only; the existing clock/quit default
  is unchanged, not a fabricated simulation policy.
- AllModuleIds and all-module pre-resolution disappear. No compatibility route survives.

## Verification and limitations

Two exact suites prove that malformed unused gameplay and unrelated scenario bodies do not block a
valid session, no unrelated Read/Resolve/generator/Compose/Initialize callback runs, metadata is
immutable, selected dependencies are canonical, collisions/refusal happen before initialization,
and interleaved saved sessions remain deterministic. Build identity changes; save layout does not.

Manifest syntax/version/ownership and JSON parsing still validate all explicitly listed files at Load.
Inactive broken JSON syntax is not ignored. Listing returns authored intent, not a promise that every
listed scenario has fully resolved gameplay. Selecting it performs full normal validation.
Malformed discovery IDs or section array shapes also fail explicitly because identity cannot be safely
selected from them. Unrelated well-identified scenario bodies are not parsed on single-ID selection.

C1 optional modules, remaining C2 imports/root work, P4 DAG and JSON-authored construction remain
open. C3a is independent of C1; it does not claim composition or whole-P4 completion.
