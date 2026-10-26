# P3-MD1 detached layer metadata contracts

## 0. Problem

Kernel/SpatialContracts.cs SpatialLayerSnapshot retains original Unit, Resource and VerticalUnit definition references despite claiming detached facts. It also implements runtime ISpatialLayer and exposes CanRead, mixing retained facts with current authorization capabilities. TileMapView.Layers and external PointLayerView.Layers construct metadata after authorization but outside the observation guard. Existing UnitInfo/ResourceInfo already copy nested metadata; ResourceCapacityInfo duplicates the existing ResourceCapacityMeasure shape.

## 1. Outcomes

1. Every emitted layer unit/resource/vertical/capacity-unit fact is an existing detached value, with no IContentDefinition or original provider reference.
2. Snapshot and runtime layer contracts are distinct; retained facts cannot act as current storage/access authority.
3. One SpatialLayerAccess.Capture authorizes and captures actual metadata through existing provider/selected-space observation.
4. Resource capacity facts use one canonical measure type; trait lookup uses one algorithm on copied/authored lists.
5. Native and point providers preserve ownership, quantities, optional links, metadata, saves/replay and physical timing.

## 2. Design

UnitInfo implements pure IUnitDefinition using its existing scalar copies. ResourceInfo implements pure IResourceDefinition using copied facts, explicit QuantityUnit projection and ResourceDeclaration's one trait-list lookup. Delete ResourceCapacityInfo; copy capacity measures into the existing ResourceCapacityMeasure, boxing detached UnitInfo rather than retaining original content units. No second unit/resource/capacity model, catalog or buffer.

Introduce readonly ISpatialLayerSnapshot for metadata only, implemented by the existing SpatialLayerSnapshot. Remove its runtime ISpatialLayer/CanRead path and public constructor. Internal creation copies required/optional links through existing UnitInfo/ResourceInfo, retaining null only for explicitly absent resource/vertical units and deeply frozen cloned JSON. SpatialLayerAccess.Capture is the sole public engine capture path: require actual authorization and capture under original layer/space observation. Both TileMapView and PointLayerView use it; hosts receive authorized facts, never decide admission.

Existing negative admission fixtures now wrap actual LayerBinding, not host snapshots pretending to be providers. Four old measure immutability casts migrate mechanically to ResourceCapacityMeasure. Old invalid-viewer assertions remain on actual runtime capabilities; snapshot authority is removed, not shimmed. No original archived assertion owner occurs in the changed proof files.

## 3. API/schema

Pure UnitInfo/ResourceInfo contracts contain no factory/content identity/service writer. SpatialLayerSnapshot implements only ISpatialLayerSnapshot, no current authorization. Its unit/resource properties remain pure metadata interfaces but always hold existing value copies. ResourceInfo.CapacityMeasures uses ResourceCapacityMeasure with detached units. Native tile view schema7 and external point layer schema4 declare the changed contract. Core2.495.0/save154/replay1; saved state owners unchanged.

## 4. Files

Hosting/PlayerView.cs detached metadata contracts; Content/Resources.cs shared trait lookup; Kernel/SpatialContracts.cs facts/capture; Map/TileMapView.cs and Content/SpatialSpaceDef.cs native capture/schema. External PointLayers.cs/PointSpaceProof.cs capture/schema. Existing negative/measure proof callers migrate directly; current protocol/Program/complete validation profile/new public case and tracker/evidence update.

## 5. Errors

Missing/hidden/unowned layer: no projection, no source callback escape. Mutating/throwing capture: existing first fault before result; guard and publication retention remain original owners. Null resource/vertical metadata stays explicit absence, never substituted defaults. Caller-owned destination failures and arbitrary unrelated provider writes are not rollback/global sandbox guarantees.

## 6. Compatibility

Development only. Remove obsolete runtime snapshot authority and redundant capacity DTO; no compatibility wrapper, adapter or legacy path. Runtime layer aliases remain original content references; only public facts detach.

## 7. Verification order

1. Apply contract/capture migration and schemas in place.
2. One solution build with necessary focused repairs only.
3. One new exact bounded case: physical minute/half flows with pre-completion saved twins/replay; private owner/sample changes, detached link types/nested measures/traits/frozen JSON, no runtime snapshot capability/public constructor, capture purity, native schemas/reads, interleaved sessions.
4. Metadata-only complete profile and source/original gate audit. No old/family/full suites; preserve every attempt.

## 8. Limits

Other resource/rate/policy projections, every referenced-definition privacy rule, arbitrary payloads/foreign state/global sandbox, allocation budgets and broad P3.008/P11.026/033 remain open. All Tycoon/Oil/platform/Draw/clock implementations untouched. Full regression only after implementation finishes; no staging/commit/push.

## Status

VERIFIED-SCOPED: ONE NEW exact FIRST/FINAL485/0/0 in656ms; deep detached metadata/contract removal, two physical time quanta with pre-completion saved twins/replay/owner transfer and native/private/schema/isolation. ONE solution6.98s clean, no repair/executable edits after FINAL. Profile-info694 included/0 excluded/0 game suites, full executor not run. Original221/188checked/33open unchanged; additional226. [Evidence](../../docs/migration/P3_DETACHED_LAYER_METADATA_VERIFICATION.md).

## Source identity

Freeze2026-10-12 13:47:40 UTC:1,777 source anchors/998 selected executable inputs/13 quickstart anchors match actual dirty source with0 drift. Inventory2,962 files/1,059 Markdown documents/8,742 local links/0 broken links;18 valid master rows;221 original literal gate/line anchors validated with0 errors. Original188 checked/33 open unchanged, additional226. Seventeen previously selected executable inputs change; two added inputs are new exact driver and previously existing ResourceDeclarationTests. Six existing production sources change, no new production state/classes; three new files (driver/two docs). Native tile view7/point layer view4, other view/state codecs unchanged. Sole clock/all12 Tycoon/Oil/platform/Draw implementations unchanged. git diff --check exit0 with34 pre-existing warning lines;2,950 dirty entries/0 staged. No executable edits after FIRST/FINAL, no old/family/full suite, full profile executor or staging/commit/push.
