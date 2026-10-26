# P4-G32 reusable tile movement — scoped evidence

2026-10-09; core2.369.0/save154/replay1. Original Mover codec4/map6/state/view layouts unchanged.
Full P1-P12 and active goal remain OPEN. Nothing staged/committed/pushed.

## Original ownership

ITileMoverDefinition composes original mobility/navigation metadata with actual tile-space/state
bindings. ITileMover composes original physical capability contracts. Mover becomes Component bound
to that actual definition, not Component<MoverDef>. MoverDeclaration is the sole native/independent
JSON class-list/initial-selection/speed/state reader and resolver/local geometry validator.
No compatibility class, duplicate route/current navigation/position/clock/state store or loader.

Original route, travel budget, occupancy, replanning, motion selection, work/stock and one clock
remain authoritative. Native moving/stopped selectors are replaced by actual authored States modes;
the affected historical assertion compiles, not rerun. Shared space authoring helper admits extra
registered modules and the original space proof queries ITileMoverDefinition, without a new fixture
world or any historical test execution.

MotionNavigationContracts validates all loaded reusable metadata/runtime pairs, positive speed,
mobile presence, actual owned tile space and exact owned moving/stopped state modes before Initialize.
NavService validates selected classes through that existing owner BEFORE enumerating external lists.
Route/area/arrival/status and navigation-selection reads now observe the actual provider codec through
EntityObservation.ReadProvider. This replaces weaker entity-only reads; no second observation system.

## Commands actually executed

Compilation: dotnet build Beep.SimEngine.slnx --no-restore.
Initial build failed2 errors/0 warnings in2.89s: missing Maps namespace import for new tile-contract
validation. Fixed import before any runtime. Clean builds5.70s/5.56s, both0 warnings/errors.

ONE NEW exact bounded case, executed twice, only that SAME case:
Mover bindings: authored contracts drive physical tile travel and work.
First268 passed/1 failed/0 aborted in1080ms; PID14920/exit1/observed1245ms.
Actual independent null navigation classes reached NavService enumeration before typed validation.
Fixed ORIGINAL NavService to call original declaration validator under existing observation guard
before enumeration. Not a test-assertion workaround or substitute list. No gameplay fallback.
Final269 passed/0 failed/0 aborted in959ms; PID15872/exit0/observed1110ms.
Bounds60000ms runtime/5000ms termination/50000chars output. No truncation/observer error.
No historical/family/full/Oil/Tycoon/platform runtime; historical fixtures/quickstart compiled only.

## Proven scope

Native/independent JSON component metadata binds the SAME Mover in an independently authored JSON
space. Actual declared type/state/space aliases precede Initialize. Ordinary player hold/move commands
earn partial physical distance and authored moving state. Readonly/private navigation lists and
selection refusals use existing engine previews. Ordinary selection of an authored costlier route
changes original route intent and extends ETA; no teleport or host movement decision.

In-travel saves restore exact original route/current selection/state. Sole-clock pause preserves
position/hash/time, saved twins continue identically, and actual arrival matches the engine's projected
remaining duration. Ordinary release/post-order commands physically approach the site and perform
half-hour survey work, consuming exactly8 of20 authored materials through original transactions.
Completed checkpoint, player command/clock replay and untouched interleaved session identity pass.

36 malformed loaded declarations/contract bindings refuse before Initialize:15 native+15 independent
missing/blank/unlisted/duplicate class, speed, states, unknown/extra state, unused malformed metadata,
unexpected host field, missing/mobile presence cases; six actual independent class/classes/speed/
space/states/undeclared reusable contract cases.

10 fault combinations: actual speed observation via arrival AND prospective route views, five modes
(exception, hidden original TicksMoving codec mutation without Touch, Touch, clock pause and identity
allocation). Actual provider codec guard withholds both outputs and latches first observation.provider;
faulted sessions cannot advance, produce healthy hash or save. Hidden age uses only original codec4,
not a second state model. Thin driver has no World/direct stock/progress mutation.

## Freeze, SOLID and remaining scope

165 selected executable inputs frozen2026-10-09 11:46:13 UTC BEFORE final successful runtime;
first164-input freeze11:45:11UTC retained separately. No executable edits follow final pass.
Current source/doc anchors and original gate line offsets refreshed separately, without modifying
original gate IDs, requirement text or checkboxes. Build success is not full regression evidence.

Scoped SOLID review: declaration owns authoring; actual provider owns physical state/Sync; focused
contracts replace concrete authoring dependency. Existing selection/observation owners are reused.
Independent native mover needs no scheduler change. No renderer/Draw/platform SDK, business/default,
Oil domain, GameClock edit or excluded Tycoon change. Pending resource catalog authority unchanged.

This proof does not validate every malicious metadata callback during codec serialization, every
component/provider, arbitrary CLR/cross-provider/whole-world sandbox, non-tile motion, all coordinate/
route/fuel/areas/docks/congestion/removal/replanning matrices, full accounting, provider commits,
lockstep/concurrency/fuzz/soak/allocation/performance or full P1-P12. These remain OPEN.
TileConditionReferences still selects its native condition hierarchy; that separate migration is OPEN.

[Stage](../../plans/enhancements/P4_MOVER_BINDINGS_DESIGN.md);
[master](../../plans/enhancements/MASTER_TRACKER.md);
[audit](../../plans/enhancements/COMPLETION_AUDIT.json).
