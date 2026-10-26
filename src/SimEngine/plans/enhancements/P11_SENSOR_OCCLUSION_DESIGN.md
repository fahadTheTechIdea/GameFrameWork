# P11-D11: Engine-owned sensor occlusion

Status: scoped implementation verified,2026-10-08. Build2.257.0/save139; persistent payloads unchanged.
TWO clean builds12.45s/5.11s,0 warnings/errors. TWO runs of ONE exact NEW bounded case: initial51 passed/
9 assertion failures410ms,0 aborts; observed fixture assumptions repaired only. Final62 checks0 failures/
aborts342ms.121 older build identity lines/files and three explicit-empty native sensor JSON instances
compile-only; no older/family/full/platform/stale gates. Source/tests/content frozen after green.
[Evidence](../../docs/migration/P11_SENSOR_OCCLUSION_VERIFICATION.md). Full P1–P12 goal remains active.

## Problem and ownership

Current sensors have range geometry but cannot reject a sample behind an optical obstruction.
Extend the SAME sensor contract/provider and selected visibility policy. Do not add a fog map,
obstacle store, raycast service, game loop, clock, renderer or host gameplay.

## Contracts and authored content

ISpatialSensor owns a pure CanObserve(World,SpatialSnapshot) query. SensorCoverage remains detached
geometry/scope metadata; it is not retained authority to query a changed world. The shared
SensorVisibilityDef delegates current sample admission to the actual provider for both layer and
entity targets. External sensors can supply different geometry/occlusion without changing policy.

TileSensorDef requires an explicit occlusion array, including [] for unobstructed sensors. Each
entry names one actual spatial layer and a positive native blocking threshold. Resolve and validate
missing/duplicate/wrong-space/private references and unrepresentable native thresholds before use.
This native slice selects only public tile layers: a private foreign layer must not secretly influence
another company's visibility or page keys. No invented layer, threshold or compatibility default.

TileSensor reads actual native MapLayer samples. Detached coverage keys include selected native layer
versions, alongside actual source footprint/range. Existing map end-Update/restore scope refresh
invalidates pages after an obstruction changes even when the sensor remains stationary. No saved
visibility or copied obstacle values. Native layer persistence and operation controls remain owners.

## Native optical semantics

Range remains the existing whole-footprint square. Sight originates at the actual source footprint's
top-left tile center. Every target footprint cell must be observable. Cells in the source footprint
and the destination cell do not obstruct that ray: the obstruction itself is visible, but cells
beyond it are hidden. Intermediate cells at or above any selected threshold block sight.

Traverse center-to-center supercover lines iteratively with exact integer arithmetic; at corner ties
inspect both adjacent cells, preventing diagonal cracks. Long arithmetic covers native tile coordinate
differences and boundary products without floating-point, recursion, a ray budget or world-size cap.
Only crossed cells are queried; no full world scan or copied grid. Optical obstruction does not
implicitly add movement/terrain rules: each authored navigation provider retains its own authority.

## Verification and stages

1. [x] Extend the existing contracts/native authoring/provider; migrate all three native sensor instances
   to explicit empty occlusion. No old API overload or fallback reader.
2. [x] Add one master-selected native optical fixture and ONE distinct bounded case: real player controls,
   blocked/unblocked map and entity sight, endpoint/corner/whole-footprint semantics, layer changes,
   immutable scope keys, privacy, saved physical travel, sole-clock pause and session isolation.
3. [x] Build after the complete slice; run ONLY that exact new case. Repair/rerun only observed
   failures. Older strict build identities compile-only; no older/family/full/platform suites.
4. [x] After final green freeze source/tests/content; record every invocation, then docs/tracker/inventory.

## Open gates

Non-tile/provider fault/global purity, multi-layer/typed threshold breadth, large-footprint/long-ray
performance, structural occlusion, weather attenuation, persistent exploration/sharing/forgetting,
rival strategy, replay/stress/full P11/P1–P12 remain open. No Oil/Tycoon/host/demo/platform changes.
Commit/push only after the FULL goal and plan are finished and verified.
