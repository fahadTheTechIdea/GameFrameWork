# P3-S33 — Tile incident propagation on independent geometry

## 0. Problem

TileIncidentPropagationDeclaration still requires concrete TileMap despite its footprint predicates already consuming tile geometry/sample contracts. This prevents a mod-owned tile world from using the engine's ordinary incident/exposure/response rules.

## 1. Goals

1. Admit and observe actual selected ITileSpatialSpace, preserving authored identity, geometry, interval, radius, delay and footprint semantics.
2. Keep original incidents, exposure records, work, material transactions, machine movement and clock authoritative.
3. Prove delayed propagation and materially supplied physical response on independent geometry, including interrupted saves, privacy, replay and isolation.

## 2. Design

TileIncidentPropagationDeclaration replaces concrete map checks with ITileSpatialSpace. Existing TileFootprintConditionDeclaration/TileConditionContracts continue to validate and read actual registered layers under EntityObservation. The old independent terrain condition proof's callback now accepts the same geometry contract, not a concrete map.

The existing independent work composition adds optional native incident/propagation modules and one explicit master-listed JSON document. It authors a two-tile source, two-tile target, public mask, source-all/target-any predicates, four-minute exposure delay and six-minute staffed repair consuming one material. Only one target tile is masked, proving all/any behavior without manufacturing eligibility in a driver. Existing depot, worker, transport and handling remain authoritative. An explicitly authored emergency-eligible response policy selects the original worker, and the target dock explicitly accepts steel delivery. Source/target response receive real material through ordinary hauling commands. Ordinary inspection policy and unrelated scenarios remain unchanged.

## 3. API/state

No new gameplay contract or state owner; existing geometry and condition contracts only. Build2.584.0/save157/replay1; saved payloads unchanged.

## 4. Files

Standard/TileIncidentPropagation.cs; existing independent terrain proof and work composition; independent-tile-incidents.json; new IndependentTileIncidentTests.cs; engine/protocol identity and test/content registrations.

## 5. Errors

Wrong selected geometry, incompatible/missing layers and illegal content still refuse before initialization. Original inactive/distance/terrain/privacy blockers, delay and actual material/work waits remain unchanged; no fabricated incident, supply, response or instant repair fallback.

## 6. Compatibility

Concrete dependency replaced directly. No alternate propagation coordinator, duplicate state or legacy shim.

## 7. Verification

Four exact focused cases pass570 checks: new independent physical response140, native physical response140, admission/all-any selection18 and affected existing terrain reconstruction/callback-fault case272. Targeted builds clean after fixture repairs; the existing save metadata assertion now uses the actual current protocol. [Evidence and all attempts](../../docs/migration/P3_TILE_INCIDENT_GEOMETRY_VERIFICATION.md). No full/profile/family run.

## 8. Risks

Cross-company/climate/network isolation, dynamic terrain reconstruction, hostile callbacks, all incident phases/response types and global durability/stress/allocation remain wider open gates. The separate concrete line-placement condition is identified but not changed in this incident slice.

## 9. Exclusions

Oil/Tycoon/platform/Draw/host logic, instant supply, new business or combat, full regression and Git publication.

## 10. Status

Migration and scoped physical/selection/fault verification complete. The separate line-placement dependency is the next spatial consumer to migrate; no original broad acceptance checkbox closes and full goal remains active.
