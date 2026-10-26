# P3-V1: Visibility-scoped entity snapshot deltas — Design Document

Status: focused implementation verified,2026-10-08. Full P1-P12 goal remains active.
Provider layer invalidation is subsequently exposed through the shared scoped regional contract in
[P3-V2](P3_SPATIAL_REGION_CHANGES_DESIGN.md). Entity snapshot owner unchanged; not a second history.
[Evidence](../../docs/migration/P3_ENTITY_SNAPSHOT_DELTAS_VERIFICATION.md).

## 0. Problem statement

Hosting/GameSnapshot.cs originally reports only Spawned/Removed identities. A host must re-diff detached entity
projections to learn changes, and version alone cannot represent PreviousPosition settlement on BeginTick.
SimGame.World is ALREADY internal; do not wrap or duplicate it.

## 1. Goals

1. Original GameSnapshot capture owns canonical Added/Changed/Removed classification and baseline revision.
2. Compare all projected entity facts and contract-authored spatial snapshots, including interpolation history.
3. Existing scoped snapshots alone are prior state; delete obsolete observer/seat known-ID lists.
4. Hidden entities never leak into any seat delta, even when only foreign sources change.
5. Prove native/public spatial movement, no-change/pause, physical work and restored projections
   through public player lifecycle/views without mutable World access.

## 2. Design

SpatialSnapshot.Matches(SpatialSnapshot) is required pure detached semantic comparison; native
TileSpatialSnapshot and independent public PointSnapshot implement every own field and space/type.
No tile/point switch in Hosting. EntityView.Matches compares every existing entity projection field
and delegates spatial matching, not provider or object reference equality.

GameSnapshot gains PreviousRevision, Viewer, Changed (readonly EntityView values). Capture accepts
previous GameSnapshot, sorts current visible entities by stable IDs and merge-compares previous
Entities. New arrivals only Spawned, retained differing facts only Changed, missing only Removed.
All lists sorted/disjoint; initial/load publication base0/full arrivals. Full Entities always available
when a consumer missed its same-session/same-viewer baseline. No replay/logical/game rules in host.

PlayerSeat.CaptureScoped and SimGame.PublishSnapshot pass their original retained snapshot. Delete
_known/_observed lists; no extra history cache, codec, scheduler, time or map invalidation owner.
Observer remains deliberate all-seeing stream, seats remain visibility-scoped. Spatial layer deltas
stay on original provider; no rendering, platform or Draw implementation.

## 3. API surface

SpatialSnapshot abstract bool Matches(SpatialSnapshot other).
EntityView internal bool Matches(EntityView other).
GameSnapshot PreviousRevision/Viewer/Changed added to existing facts.

## 4. Files to change

Kernel/SpatialContracts, Map/TilePresence, existing public PointSpaceProof snapshot;
Hosting/PlayerView/GameSnapshot/SimGame; new bounded test/registration/build identity.
Existing EntitySensorTests.Factory/OptionalProcessTests.Factory assembly-only visibility made internal for reuse.
No JSON definitions duplicated or gameplay/content changed. Older identity fixtures compile-only.
New stage/evidence/manifest and original tracker/docs finalized after green.

## 5. Error handling matrix

Different spatial space/type → false (changed), null spatial matches only null.
Foreign viewer baseline → engine invariant refusal. New/load baseline → complete initial arrivals.
Unknown host baseline → consume full snapshot, never infer absent entities from a skipped delta.
No identity/revision/time defaults grant gameplay.

## 6. Backward compatibility

Development only; direct contract migration, no legacy/shim or fallback concrete geometry matching.

## 7. Verification

ONE distinct exact NEW bounded case. Native visible movement, direct detached interpolation comparison,
readonly retained values, updated metadata, hidden foreign mutation/sight enter-exit, explicit pause,
positive native work, public point movement/completion, sequential saved twins/session isolation,
initial/load arrivals and actual supplied timed mapless manufacturing birth. Public player commands/views only
in new driver, no World/provider mutation. Max1000 Update per physical stage with public diagnostics.
Completed-slice build, record every build/run; repair/rerun only observed same-case failure.
No old/family/full/platform/stale runtime. Freeze code/proof/tests/JSON after final green.
Three clean builds11.15s/5.25s/2.94s; three SAME-case runs31/1/one throw567ms,
66/2/one throw655ms, final72/0/0 539ms. Test-only corrections detailed in evidence.
Actual same-version interpolation settlement was NOT observed and is not claimed.
Historical default colony composition refuses undeclared work_cost_requirements; not repaired
or rerun as an older suite. Existing resource-only process fixture supplies the birth proof.

## 8. Risks

Mod Matches must be pure, symmetric/reflexive and cover every immutable field; no global hostile-mod
sandbox claim. Revision is local publication sequencing, not game time or saved authority. Stable
shared-content metadata relies on original authoring freeze. Entity delta is not spatial-layer delta.

## 9. Out of scope

Arbitrary provider/fault/global purity, actual destructive removal/corruption/full privacy matrix,
snapshot transport/serialization/compression/history retention, spatial query expansion, 3D/hydrology,
rendering/platform/Draw, replay/lockstep/CPU/allocation/stress/full P3/P1-P12 acceptance.
No Oil/Tycoon business source/host/demo edits. Commit/push after FULL goal/plan verification only.

## 10. Sub-task execution order

1. Required detached spatial matching native/public and original entity projection comparison.
2. Original snapshot merge delta/current baseline, eliminate redundant known-ID state.
3. One bounded public-player native/mod physical/visibility/save/birth proof, existing authored JSON.
4. Build/exact gate/freeze, evidence/tracker/current-source inventory.
