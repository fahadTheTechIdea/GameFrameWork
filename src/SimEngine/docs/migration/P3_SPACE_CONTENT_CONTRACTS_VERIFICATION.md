# P3-S12 spatial content contracts — scoped evidence

2026-10-09; core2.368.0/save154/replay1. Native map6, movement and layer/view layouts unchanged.
Full P1-P12 and active goal remain OPEN. No staging/commit/push.

## Original owners

ISpatialSpaceDefinition replaces the abstract concrete space hierarchy. ITileSpaceDefinition adds
authored geometry only. Native TileSpaceDef and independent Def metadata alias the original resolved
catalog. TileSpaceDeclaration owns the single JSON geometry reader and original TileMap/layer/view
composition. No new map, world, position, occupancy, route, clock, stock, loader or compatibility path.

Scenario/layer/region/recovery references now use root contracts. Presence, tile regions/conditions,
navigation, movement, docks, sensors, incident propagation, spatial quantities/readings,
construction, infrastructure and startup consume tile geometry contracts in place. Loaded aliases
and positive geometry validate before Initialize, including unused independent metadata. Native
declaration composition verifies actual catalog/geometry before map registration.

Point proof definitions now directly implement the root contract; their existing spatial providers
are unchanged and compiled only. Mover still has native component metadata binding: this stage
removes its concrete SPACE dependency, not that separate pending migration.

## Executed commands

Two compilation builds: dotnet build Beep.SimEngine.slnx --no-restore.
7.75s/4.84s; both zero warnings/errors.

ONE NEW exact bounded runtime case, two executions of that SAME case:
Space content contracts: independent JSON worlds retain physical layers and travel.
First146 passed/1 failed/0 aborted in858ms; PID23332/exit1/observed1012ms.
The test expected ContentException for unknown module; existing composition correctly throws
InvalidOperationException. Only that assertion moved to its actual exception type; no engine
refusal behavior changed. Final147 passed/0 failed/0 aborted in751ms,
PID18728/exit0/observed896ms; no output truncation or observer error.
Bounds60000ms runtime/5000ms termination/50000chars output.
No historical/family/full/Oil/Tycoon/platform runtime. Quickstart and historical fixtures compile only.

## Proven scope

Native/independent master-listed geometry binds original TileMap with actual scenario/layer,
rectangular/painted region, tile-condition and mover-space aliases before Initialize.
Three authored private/public fixed/byte/integer layers preserve native sparse samples and ownership.
Ordinary hold/move commands cause partial physical travel and authored moving state; no instant
placement. Retained pose remains detached and physical sensor coverage invalidates old page cursors.

In-travel save/load twins continue identically; the sole clock pauses both without progress/hash
changes. Ordinary release/post-order commands approach the real site, perform half-hour survey work,
consume exactly eight of twenty actual materials and earn existing durable knowledge. Completed
layers/materials/work/knowledge checkpoint, ordinary command/clock replay and interleaved untouched
session identity are narrowly verified. No driver-side map/work/resource mutation or game rules.

100000 x100000 independently authored geometry (ten billion cells) composes/checkpoints sparsely,
with a small native page. No renderer/platform-size cap was added. This does not prove unbounded
memory, every long route or all representable coordinate/arithmetic edges.

27 invalid loaded declarations/composition references refuse before Initialize:20 native/independent
missing/zero/negative/extra/unused geometry, wrong scenario/layer/presence reference and out-of-bounds
region; six actual independent width/height/declaration mismatch/unused/duplicate-alias/missing-document
cases; one unknown registered module. Existing content errors and original composition exception
remain distinct, with no substitute native space.

## Freeze, review and remaining gates

157 selected executable inputs frozen2026-10-09 11:30:28 UTC before final pass. First attempt freeze
11:29:52 UTC retained separately in audit. No executable edit follows the final successful runtime;
read-only final hashes verify unchanged inputs. The audit separately refreshes current source/doc
anchors and original acceptance line offsets, preserving every identity/text/checkbox.

SOLID scoped review: declarations own authoring/composition; runtime providers retain state/Sync;
root/tile references depend on focused contracts. Independent native-map content needs no scheduler
change. This is not proof of all provider composition/callback safety or all feature migrations.

All native/non-tile/custom providers, composition callback faults, condition execution matrices,
construction/docks/network/incidents/extraction/operating areas, long-range routing, removed references,
full accounting, lockstep/concurrency/fuzz/soak/allocation/performance/global acceptance remain OPEN.
Tile condition references still select their native condition hierarchy, independently of space
contracts; Mover still selects native component metadata. No unsupported completion claim.
Pending excluded Tycoon authority unchanged. No business/default/domain/platform/Draw/GameClock edits.

[Stage](../../plans/enhancements/P3_SPACE_CONTENT_CONTRACTS_DESIGN.md);
[master](../../plans/enhancements/MASTER_TRACKER.md);
[audit](../../plans/enhancements/COMPLETION_AUDIT.json).
