# P3-RQ1: Regional entity-query spatial observation

## 0. Problem statement

`TileMapView.CopyEntities` stages rows and observes actual presence/entity state, but its footprint predicate omits the selected spatial scope. A metadata callback can modify map layers before `PlayerView.ViewOf` captures the already-modified baseline, violating read purity even when no entity overlaps the query.

## 1. Goals

1. Observe selected map state/revision across every actual footprint predicate.
2. Overlapping and disjoint queries reject map mutation with no output prefix; restored values do not bypass revision invalidation.
3. Healthy regional queries remain deterministic, viewer-scoped detached projections with unchanged physical gameplay.

## 2. Design

Reuse existing EntityObservation.ReadProvider with the actual `_map` spatial scope in the original predicate. No new spatial registry, geometry model, authority, timer or world-size limit. Existing local row staging remains sole publication mechanism.

Extend original tile-presence proof module with opt-in authored owner visibility and actual space-write/restore/throw probes in its metadata getter. Negative callbacks are engine-side extensions, never driver state writes. Use the original factory and public commands/views/saves/replay.

Build2.505.0/save154/replay1; tile view8 declares behavior change, other view/provider schemas unchanged.

## 3. API surface

Existing TileMapView.CopyEntities and detached EntityView/TileSpatialSnapshot. No new query or provider type.

## 4. Files to change

- Existing TileMapView predicate, tile view descriptor and engine identity/protocol.
- Existing extension proof/factory helper; one new public-player exact case/profile/registration.
- Tracker/stage/evidence metadata.

## 5. Error handling

Original observation first fault is authoritative; no partial rows, successful save/hash, later gameplay/time or map publication. In-bounds empty and healthy private reads retain original codes.

## 6. Compatibility

Development only; no compatibility/legacy branch.

## 7. Verification

One solution build and one new bounded exact case. Overlap/disjoint geometry and actual map write/restore/throw callbacks; healthy owner/foreign/observer projections, immutable retained facts, original physical manufacturing/construction, saved twins and replay. No broad family or full regression.

## 8. Risks

World bounds come from authored content, not a platform constraint. A bad callback is terminal, not rolled back. This does not sandbox every CLR mod or every spatial provider.

## 9. Out of scope

No Oil/Tycoon/platform/Draw changes, second clock/spatial model, global P3.008 closure, staging or publication. Existing native geometry and state owners remain unchanged.

## 10. Execution order

1. Complete original spatial scope/descriptor.
2. Extend existing proof and add focused public query acceptance.
3. Build/run exact case, retaining failures.
4. Record scoped evidence and remaining gates.

## Delivery outcome

NEW FIRST/FINAL746/0/0 in25659ms; solution9.57s/focused3.75s accessor compile failures retained, final focused7.09s clean. One healthy owner/foreign/observer matrix, six overlap/disjoint actual space-mutation/restore/throw flows and current physical independent tile manufacturing/construction helper executed. Profile-info704 included/0excluded/0game suites; no full executor. Original221/191checked/30open unchanged,additional236; P3.008/global/full goal remains open. No executable edits after FINAL or Tycoon/clock/platform changes.

Metadata-only validation:1814 source anchors/1015 frozen executable inputs/13 quickstart anchors match; all221 original gates retain literal requirements/line anchors/checks.2998 inventoried files/1079 Markdown/8771 local links, zero broken;18 master rows/zero malformed. Driver boundary0, diff-check0,2986 dirty/0staged. All12 Tycoon sources and sole clock unchanged; goal_complete=false.
