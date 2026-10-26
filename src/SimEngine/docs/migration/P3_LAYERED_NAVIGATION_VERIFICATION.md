# P3-S12 JSON-authored layered navigation — verification

Date2026-10-08. Canonical engine only. Full goal and P1–P12 plan ACTIVE; commit/push only after full
verified completion. Scoped evidence, not full-engine acceptance.

## Ownership and identities

Build2.258.0/save139; all persistent payloads unchanged. No new codec, service, command or loop.
[LayeredTileNavigationDef](../../src/Beep.SimEngine/Standard/LayeredTileNavigation.cs) implements the
existing TileNavigationRuleDef/ITileNavigationRule contract. The existing
[TileNavigationModule](../../src/Beep.SimEngine/Standard/TileNavigationModule.cs) composition loop
binds it to the original [NavService/PathSearch](../../src/Beep.SimEngine/Standard/Navigation.cs).
The same [Mover](../../src/Beep.SimEngine/Standard/Movement.cs) owns actual route/position/travel/
replanning/Sync. Immutable condition/rule metadata holds content intent, not map values or paths.

Public native layers only; no hidden foreign/private geology driving observable movement. Native
value bands are lower-inclusive/upper-exclusive, with explicit nullable bounds and at least one
bound. All conditions in a rule must match. Highest priority wins; same-priority stable ordinal ID
breaks ties independently of JSON order. No match uses explicit base_cost. Zero blocks entry;
every provider needs at least one positive authored outcome, from which admissible MinCost derives.
Fixed bands support fractional depth; native byte/int bands require integral bounds.

Actual cost reads use original MapLayer storage, no full-map scan/cache/copy. Existing native search,
occupancy and diagonal-entry rules remain authoritative. Current weights affect path admission and
physical speed. Upcoming impassable-cell changes reopen the same mover path. Still-passable
cheaper-road edits do NOT force replanning; runtime fleet corridors/class switches remain open.
No hydrology, water creation, renderer, platform API or Tycoon dependency is added.

## Every invocation

Canonical working directory:

```
dotnet build Beep.SimEngine.slnx
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Layered navigation: authored roads, depth gates and physical saved work"
```

ONE build: exit0,12.35s,0 warnings/errors.
ONE exact NEW bounded case: exit0,48 checks,0 failures/0 aborted suites1049ms,FIRST attempt.
122 older strict build identity assertion files/lines migrated and compiled only. No older/family/
full/platform/stale suites. Prior sensor/logistics/clock gates compile-only, not rerun at2.258.0.

## Proven behavior

[New case](../../tests/Beep.SimEngine.Tests/LayeredNavigationTests.cs) extends existing native
master/startup/work/operation/time composition with explicitly master-selected
[movement rules](../../tests/ExtensionProof/content/native-layered-navigation.json) and
[surface/depth/startup](../../tests/ExtensionProof/content/native-layered-navigation-world.json).
No test-owned cost callback, pathfinder, motion provider or gameplay module. Normal player commands
hold/release a real machine and post the original physical survey activity.

- Actual JSON contract/origin and admissible positive MinCost; explicit ground/dry-vessel blocking.
- Same-priority road-rule ID tie, including reversed authored ordering; pure current cost/path reads.
- Existing pathfinder prefers a cheaper, longer road over direct expensive ground.
- Actual fixed half-depth inclusive threshold blocks land but permits the authored vessel class;
  quarter-depth and exclusive upper band admit road but not vessel. Vessel is a cost-provider protocol
  check, NOT a physically travelling ship. Signed native terrain blocks or uses explicit base.
- Actual machine initially selects that road. Travel grants no work progress or material spending.
- Changed actual depth makes one upcoming cell impassable. Same physical mover travels on road,
  avoids flooded cell, reaches real work, consumes exactly eight materials, and earns knowledge only
  after completing productive work. No teleport/instant bypass/duplicate route owner.
- Moving/partial-work save-load twin hashes, sole-clock pause, independent actual layer sessions.
- Eleven invalid initialization cases: negative/all-zero costs, duplicate IDs, empty conditions,
  absent bound, empty band, duplicate layer, fractional-int band, unknown/private/foreign-space layer.
- Bounded500-update stages with step/position/motion/order diagnostics on timeout.

Direct native layer Set calls are provider protocol/environment-change checks, NOT a new player
editing, road construction or weather workflow. Optical sensor source is unchanged apart from old
strict build metadata; no prior sensor runtime gate was run.

## SOLID and boundary review

Existing cost provider owns cost selection only. Rule/condition types are immutable authoring values;
NavService/Mover and MapLayer retain search/travel/value responsibilities. Shared composition depends
on its existing navigation contract, not a new provider branch. Distinct uniform and conditional
providers have distinct authored behaviors, not duplicated search or a compatibility route. Costs
read actual current state without persistent derived data; unknown/private metadata refuses explicitly.
No kernel geometry branch, game-world size cap, new time/resource/stock owner or host game rules.

## Freeze and scoped inspection

Source/tests/content frozen immediately after green; following edits documentation-only.
Scoped617 paths/125 Markdown documents/2013 local links/0 broken; actual git diff --check exit0
with existing LF/CRLF warnings. All manifest hashes verified after final documentation edits.
Own [manifest](P3_LAYERED_NAVIGATION_SOURCE_MANIFEST.json) excluded from its own hashes. Historical
reports/manifests preserved. Accumulated scoped inventory is not whole-repository/runtime acceptance.
Sole GameClock unchanged SHA256:
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.

## Open gates

Byte bands/multiple water regions/physical vessels/multicell hulls, far coordinates and long-path CPU/
allocation budgets, provider fault/global purity, hydrology/weather/3D/non-tile geometry, runtime
movement-class/fleet corridor policies, optimal rerouting for still-valid cheaper roads, player road
construction/removal, congestion/service, replay/fuzz/stress/full P3/P9/P1–P12 acceptance. No Oil/
Tycoon/business/host/demo/Draw/platform changes. Full goal active; publication remains deferred.
