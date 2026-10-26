# P3-S34 — Shared contract-based tile line geometry

## 0. Problem

TileLineGeometry still resolves TileMap/ByteLayer/IntLayer services for corridor placement, routing, stamping, ownership, removal and graph enumeration. TileLinePlacementDeclaration and InfrastructureView endpoint routing also cast concrete maps. A mod-owned tile world therefore cannot reuse the engine's existing infrastructure gameplay.

## 1. Goals

1. Share one tile line geometry implementation across selected providers through geometry, placement, sample, writable and sparse-enumeration capabilities.
2. Preserve original line construction, persistent plans, material hauling, work, network graph/pool/flow and clock owners.
3. Prove a material-supplied underlay activates a persistent owned corridor plan, with saved physical work, private network facts and deterministic continuation/replay.

## 2. Design

ITileSparseSampleLayer publishes canonical nonzero cells without a dense scan. Native sparse layers and the independent proof expose their existing stores through it. TileLineGeometry resolves actual registered ITileWritableSampleLayer and validates typed values/revisions. Its original router, cursor, stamping, ownership and graph rules remain in one class, no native fallback.

TileLineGeometry.Compose owns the existing service/command registrations; native and independent providers delegate through ILineGeometryComposition. Tile line placement and InfrastructureView consume actual ITileSpatialSpace. Endpoint rings use clipped long-intermediate geometry rather than concrete-map inflation.

Completed temporary work markers are retired by the existing line owner. WorkBoard.CanDisclosePlan now retains the owning company's terminal plan result when that target no longer exists, while retaining current content, live-target, reference and company visibility checks. No history or entity snapshot owner is added.

The existing proof composition selects native Infrastructure through an explicit master-listed JSON document: an unowned underlay, an owned corridor, authored terrain policy, six-minute material-consuming segments, original work markers and an empty private network. Normal drivers queue the corridor, construct the prerequisite through commands and observe original engine plan/work/network views. No state/rules are written in the driver.

## 3. API/state

One optional sparse tile-sample capability, shared composition and existing immutable route/pool views. Build2.585.0/save157/replay1; original payloads unchanged. No extra grid, graph, stock or scheduler.

## 4. Files

Map/TileSpatialContracts.cs, TileMap.cs, TileLineGeometry.cs; Standard/LinePlacementPolicies.cs; Views/InfrastructureView.cs; independent provider/work composition and explicit line JSON; new focused line tests and engine/protocol/content/test registrations.

## 5. Errors

Missing/incompatible layers, invalid values/revisions, noncanonical sparse cells, wrong geometry, broken paths, placement blockers and foreign ownership refuse through the original rules and observation boundaries. No skipped prerequisite, instant corridor or fabricated network contents.

## 6. Compatibility

Concrete dependency removal in place; no old path or compatibility wrapper.

## 7. Verification

New native/independent physical corridor cases319/0 each, admission12/0 and existing plan-disclosure65/0:715 distinct focused checks. Targeted builds clean. The older topology split/merge case remains unverified because its fixture first required an unselected business supplier and then referenced the wrong map; no geometry execution occurred. [All attempts](../../docs/migration/P3_TILE_LINE_GEOMETRY_VERIFICATION.md). No full/profile/family tests.

## 8. Risks

Atomic multi-layer write/fault recovery, all stocked split/merge/removal topologies, utility/connectivity/visibility/hostile provider breadth and global durability/stress/allocation remain open. Byte corridor levels and integer ownership retain their existing authored semantics.

## 9. Exclusions

Oil/Tycoon/platform/Draw/host rules, duplicate infrastructure models, dense size policy, full regression and Git publication.

## 10. Status

Shared contract migration and scoped physical/visibility proof verified. Repair the old topology fixture's explicit module/map composition before claiming its split/merge/removal gate. Broader line/network/utility/fault gates and full goal remain open; no original broad checkbox closes.
