# P3-S30 — Independent tile construction and physical supply

## 0. Problem

Native tile construction filters still required concrete TileMap. Native map placement preflight also owned footprint/constraint/occupancy logic that the independent provider did not share, so independent construction could omit native component constraints or require a copied implementation.

## 1. Goals

1. Keep one tile placement and target/site geometry preflight across native and independent providers.
2. Make authored tile construction filters consume selected geometry contracts.
3. Prove ordinary construction commands, physical material loading/delivery, productive work, interrupted saves and actual native completion without business or a concrete map.

## 2. Design

TilePlacementRules owns the original positive geometry, actual session/content, replacing entity, component constraint and occupied-footprint preflight. Native TileMap delegates to it; its duplicate body is removed. Constraints execute through original EntityObservation provider/replacing-state/selected-space guards. Native occupancy and TilePresence still own their original state. Construction target/site parity uses the same shared rule.

The independent provider declares existing ISpatialConstructionPlacement and delegates placement/target-site validation to that engine implementation. TileConstructionRules requires ITileSpatialSpace, not TileMap/service aliases. No construction coordinator, site state, graph, inventory, promise, router or clock is copied.

The existing independent work factory selects optional native Construction and Logistics through explicit master JSON. A new master-listed construction document authors materials/capacity, hauling role/handling profile, four-minute staffed build DAG, empty site, finished prototype and a12-unit depot. The depot explicitly permits automatic supply discovery. The original work/logistics system finds supply, handles actual cargo over time and consumes the eight-unit requirement before native site replacement. Drivers only submit commands/update/read views; no stock or progress writes.

## 3. API/state

Shared TilePlacementRules pure methods; existing contracts unchanged. Build2.581.0/save157/replay1, native payloads unchanged. Native and independent construction use original physical owners. No compatibility path.

## 4. Files

Map/TilePlacementRules.cs and TileMap.cs; Standard/TileConstructionRules.cs; current engine/protocol identity. Existing independent provider/factory adopts the construction capability and authored module selection. New independent-tile-construction.json and IndependentTileConstructionTests.cs are explicitly registered/embedded. Existing work source is loaded once; no parallel file-source branch remains.

## 5. Errors

Wrong geometry/session/content/replacing entity/bounds or occupied footprint retains typed refusal. Constraint mutation faults through original observation guards. Unavailable authored sources leave the site waiting with work.resource.stock and NoSupply diagnostics; no material preloading or instant build fallback. Temporary site replacement remains original native completion.

## 6. Compatibility

Development-only concrete dependency removal. Original map preflight bodies are replaced by delegation to one owner, not retained behind compatibility checks. Business and rendering remain optional/outside the simulation rules here.

## 7. Verification

Focused independent physical construction121/0, independent docked work58/0 and native construction18/0. Positive construction proves empty site admission, no instant finished structure, actual loaded cargo, interrupted-haul/productive saves, twin continuation, exact material conservation, one finished facility and retired site. [Evidence](../../docs/migration/P3_TILE_CONSTRUCTION_CONTRACTS_VERIFICATION.md).

## 8. Remaining risks

Full independent construction refusal/template/cancellation/salvage/recovery/provider-fault matrices, competing carrier dispatch/docks/utilities, all ownership/lifecycle/replay/lockstep/stress/allocation and original compound gates remain open. This single supplied project is not general engine acceptance.

## 9. Exclusions

Instant materials/builds, new gameplay/state owners, altered baseline amounts/timing, business/Oil/platform/Draw, full/profile/family execution and Git publication.

## 10. Status

Migration and scoped physical verification complete; full goal active. No original broad checkbox closes.
