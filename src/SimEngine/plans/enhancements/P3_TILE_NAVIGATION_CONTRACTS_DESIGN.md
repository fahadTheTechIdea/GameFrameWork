# P3-S27 — Contract-based tile routing and layer setup

> Current continuation P3-S28/build2.579.0 completes the previously timed-out native work proof and all preserved construction/predicate stages within30s selected bounds. [Current stage](P3_OBSERVATION_HASH_COST_DESIGN.md), [current evidence](../../docs/migration/P3_OBSERVATION_HASH_COST_VERIFICATION.md). The incomplete results below remain the historical S27 checkpoint, not current work completion.

## 0. Problem

Native tile presence no longer required a concrete map, but Navigation.cs, TileNavigationModule, move-command admission, movement approach geometry and terrain predicates still required TileMap. Ordinary native movement was therefore unavailable on the independent provider. TileLayerSetupDef also required MapLayer, preventing reuse of ordinary JSON terrain initialization.

## 1. Goals

1. Native A*, mover, movement classes and ordinary move commands consume selected geometry/occupancy contracts.
2. Native layer predicates and startup writes consume actual registered sample contracts, preserving one sample binding validator.
3. Independent providers reuse native routing, movement, states, JSON setup, views, save pipeline and clock; no alternate pathfinder or movement loop.
4. Verify ordinary physical movement and callback safety with focused bounded cases; keep incomplete work and runtime evidence explicit.

## 2. Design

ITileNavigationSpace extends the existing relocation/geometry capability with actual OccupantAt and InteractionGoal. TileMap implements the contract on its original occupancy and approach logic. NavGrid, NavService, PathSearch, native move admission and mover route checks resolve actual selected contracts. The original sparse octile A*, frontier budget, heuristic, corner rules and deterministic tie-breaking remain unchanged.

TileSpatialGeometry owns shared bounds, adjacency and the existing immutable neighbour order. TileMap.Neighbours is removed, and its routing/network/test callers migrate to the one shared definition. No compatibility alias is retained.

ITileConditionDefinition takes ITileSpatialSpace rather than TileMap. Native layer bands resolve ITileSampleLayer through TileSpatialGeometry.RequireSampleLayer, also used by optical sensors. That resolver rejects absent/null/duplicate registrations, wrong space/type/access and invalid revisions. Public predicates/sensors require public layers; startup explicitly permits the declared private layer access. Groups, footprint conditions and external predicates migrate in place.

ITileWritableSampleLayer extends scalar sampling with the existing CheckSample/SetSample operations. Native MapLayer implements those methods directly. TileLayerSetupDef uses those contracts and the existing layer-owner assignment for private setup, retaining its ordinary authored checks and write sequence. It does not create another layer store or setup coordinator.

Navigation occupancy and terrain callbacks execute within one original rule/actor/selected-space observation scope per entry-cost query, not two overlapping scopes. Returned occupants must be actual live blocking presences covering the requested tile. Faulted navigation refuses before out-of-bounds early returns. No hostile-CLR sandbox or all-entity mutation guarantee is claimed.

## 3. API and state

ITileNavigationSpace and ITileWritableSampleLayer are added. Predicate signatures change in place; independent declarations reuse their original implementations. Native map/router/mover/sample/clock state remains authoritative. Build2.578.0/save157/replay1; native codecs unchanged. No shim or old signature overload.

## 4. Files

Map/TileSpatialContracts.cs, TileMap.cs and TileLineGeometry.cs; Standard/Navigation.cs, TileNavigationModule.cs, TileNavigationRules.cs, LayeredTileNavigation.cs, Movement.cs, MotionNavigation.cs, StandardCommands.cs, TileLayerConditions.cs, TileConditionGroups.cs, TileSensorOcclusion.cs and WorldSetup.cs. Existing external predicate signatures and immutable-neighbour assertions migrate mechanically. IndependentTileSensorSpace implements the provider contracts without native map delegation. Four master-listed navigation/startup/condition documents and IndependentTileNavigationTests provide ordinary-player proof.

## 5. Errors

Missing navigation geometry refuses composition. Invalid bounds and blocked terrain retain normal command refusal. Unsafe occupancy callbacks produce no healthy route preview, latch observation.provider and cannot update/save. Layer setup uses the original complete preflight; no default sample provider or fabricated terrain is substituted.

## 6. Compatibility

Development-only replacement of concrete dependencies. Old TileMap.Neighbours and concrete predicate signatures are removed rather than wrapped. Engine TileMap remains independent simulation data, unrelated to Godot's deprecated node.

## 7. Verification and current status

Implemented; verification partial. Current focused independent ordinary-command movement82/0, independent sensor/presence protocols73/0 and native optical travel64/0. Independent movement proves JSON-selected native mover/routing, preview parity, finite-time travel, blocked-strip detour, moving/arrival states, partial saves, pause, visibility and session isolation. Occupancy clock/layer/throw probes prove first-fault handling.

The longer native layered-work case and independent-predicate colony-work case exceeded30s and are not passing evidence. The latter also exposed a stale save154 expectation, now mechanically updated to the independently authored current protocol expectation; it has not been rerun after that fix. Native layered diagnostics prove progression through clock32, not successful work completion. [All attempts and current limitations](../../docs/migration/P3_TILE_NAVIGATION_CONTRACTS_VERIFICATION.md).

## 8. Next required work

Diagnose native layered-work runtime with bounded stage diagnostics; do not mask it by loosening deadlines. Complete affected physical work and predicate acceptance, then cover provider/lifecycle/ownership/fault/replay/lockstep/performance breadth. Current focused success does not close original P3/P11 compound gates or the full goal.

## 9. Exclusions

Oil/Tycoon/business, Draw/platform adapters, another clock/router/coordinate/store, world-size restrictions, full/profile/family runs and Git publication.

## 10. Tracker

Contract migration implemented; independent movement and selected protocols verified. Native work/runtime validation remains open. No original acceptance checkbox changed.
