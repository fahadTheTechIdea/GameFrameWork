# P3-S12 — Contract-owned spatial content

## 0. Problem

Content/SpatialSpaceDef.cs, Kernel/Modules.cs and native tile consumers select the concrete
SpatialSpaceDef/TileSpaceDef hierarchy. Layers, regions, initial placement, sensors, docks, resources,
incidents, construction and routing reject independently authored spaces even when they use the same
tile runtime. This blocks the framework's existing file/contract extension model before movement.

## 1. Goals

1. Scenario/layer/region/recovery references select actual ISpatialSpaceDefinition aliases.
2. Tile consumers require ITileSpaceDefinition geometry, not native authoring inheritance.
3. Native/independent JSON tile metadata composes the original TileMap through one declaration.
4. All loaded space contracts validate actual ownership and positive authored tile dimensions before
   Initialize, including unused metadata; duplicate broad identities refuse, not first-match.
5. Physical routing/work, private multilayer readings, saves/replays and isolated sessions retain
   original state owners. No platform-size limit, map copy, new world or compatibility hierarchy.

## 2. Design

Replace abstract SpatialSpaceDef with ISpatialSpaceDefinition : IContentDefinition, exposing Compose.
ITileSpaceDefinition extends it with Width/Height. Existing resolved-content indexes alias actual
providers once; no second registry/loader. Native TileSpaceDef directly implements the tile contract.
TileSpaceDeclaration owns original width/height reading and original map/layer/view composition.
Independent Def providers delegate the same declaration; they need not inherit native space classes.

SpatialSpaceContracts validates actual root/tile alias identity and positive dimensions during the
existing definition-resolution lifecycle. All native references, type tests and method parameters
consume the contracts in place. Point proof metadata becomes Def + ISpatialSpaceDefinition; its
unchanged point-space runtime remains engine-owned. No old abstract base survives.

Tile dimensions remain explicit game-content geometry. They are not renderer/platform limits.
Original sparse chunks/layers, occupancy, navigation, entity position/state, visibility/knowledge,
resource endpoints and one clock remain authoritative. Arbitrary non-tile providers remain possible.

## 3. API

ISpatialSpaceDefinition.Compose(WorldComposer); ITileSpaceDefinition.Width/Height;
TileSpaceDeclaration.Read(JsonReader), Compose(WorldComposer, ITileSpaceDefinition).
ScenarioDef.Space, SpatialLayerDef.Space and SpatialRegionDef.SpaceDefinition expose actual contracts.
Native tile-condition/region/navigation/dock/sensor/resource/setup declarations reference tile contracts.
The removed SpatialSpaceDef base has no compatibility replacement class or copied catalog.

## 4. Files

Content/SpatialSpaceDef.cs, ResolvedContent.cs, SpatialRegionDef.cs; Kernel/Modules.cs;
Map/TilePresence.cs and TileRegions.cs; existing Standard tile/infrastructure/movement/resource/
sensor/incident/setup/recovery consumers. Mechanical point proof metadata binding changes compile
historical tests only. One new independent-space engine proof and one thin bounded player case.

## 5. Errors

Missing/wrong/ambiguous root or tile contract, invalid positive geometry and unused malformed metadata:
ContentException before Initialize; unknown selected module retains original InvalidOperationException.
Native map composition verifies actual catalog/geometry metadata
through the existing observation guard before map registration. No fallback native space or duplicate
ID selection. Existing route/visibility/placement refusals remain typed and physical.

## 6. Compatibility

Development only: remove old abstract base and migrate callers in place. No old JSON/save reader,
wrapper or parallel world. Build identity changes; native map/movement/save/replay layouts unchanged.

## 7. Verification

ONE NEW exact bounded case: Space content contracts: independent JSON worlds retain physical layers and travel.
Native/independent JSON space, original tile runtime and actual root/tile aliases; master-listed
layers/regions/conditions/startup; normal travel and work, private reads, saved mid-travel twins,
single-clock pause, replay, interleaved isolation, large sparse geometry and early metadata refusals.
Compilation for older fixtures only; no historical/family/full/Oil/Tycoon/platform runtime.

## 8. Risks

One proof does not validate every migrated consumer or all mod/provider callbacks. Full construction,
docks/network/incidents/resource extraction/operating-area matrices, provider composition faults,
cross-provider/CLR sandbox, lockstep/concurrency/fuzz/soak/allocation/performance and global acceptance
remain open. Mover's native metadata binding is a separate subsequent migration, not implicitly done.

## 9. Scope

Engine only; excluded Tycoon/catalog authority remains pending. No renderer/Draw/platform SDK,
business/defaults, second clock or staging/commit/push before full verified goal/plan completion.

## 10. Order

Review actual references → root/tile contracts and sole native composition → migrate every active
space caller and remove old base → actual alias validation → independent JSON engine proof →
focused compilation and ONE new bounded case → actual evidence/master/audit. Full P1-P12 remain OPEN.

## 11. Current scoped result

Implemented root/tile contracts, sole native declaration and in-place consumer migration.
Core2.368.0/save154/replay1; two clean builds7.75s/4.84s.
ONE NEW exact case final147/0/0 in751ms (first146/1/0: test exception expectation corrected).
Actual aliases/private multilayers/physical travel/material-paid work/twins/pause/replay/isolation,
100000 x100000 sparse geometry and27 pre-Initialize refusals narrowly verified.
157 selected inputs frozen11:30:28UTC; no subsequent executable edit.
[Scoped evidence](../../docs/migration/P3_SPACE_CONTENT_CONTRACTS_VERIFICATION.md).
Remaining Mover/component metadata, native condition hierarchy and full/global gates remain OPEN.
