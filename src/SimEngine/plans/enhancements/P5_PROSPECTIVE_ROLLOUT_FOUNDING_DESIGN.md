# P5-A13: Prospective rollout and complete founding admission — Design Document

## 0. Problem Statement

Oil FoundCompanyHandler uses retired Width/Height/World.Map/ProductionSystem/spawn helpers. Its per-vehicle empty-world check can select the same spot repeatedly; direct per-resource Add can silently discard a starting-kit remainder. Native rollout requires a live source, so an unbuilt HQ cannot be previewed without inventing an entity. Extend original owners rather than restore retired helpers.

## 1. Goals

1. Allow original spatial rollout to accept a planned producer and the whole earlier placement prefix without spawning a temporary entity.
2. Keep one provider-owned rollout interpreter; live and prospective callers use the same native or independent geometry.
3. Admit starting stock against actual initial quantities, resource filters, discrete units and whole capacity before mutation.
4. Use one Oil Plan function for command preview/execution and original creation/resource protocols for committing.
5. Verify focused tile/non-tile geometry and stock queries; record full Oil runtime as unverified until remaining composition/content migrations finish.

## 2. Design

SpatialPlacement.cs defines ephemeral PlannedEntityPlacement and EntityRolloutRequest. Neither is a saved placement/reservation/entity owner. ISpatialRollout now takes the request and supplies CheckRolloutPlacement; no old provider signature or compatibility reader remains.

EntityRollout.Find keeps actual live-source validation/observation and has a planned-source overload. It captures live placement through the original provider or accepts explicit planned intent. It validates actual catalog references/spaces, planned-root placement and prefix geometry, then checks the chosen result through the same provider. Existing producers do not rerun founding/construction placement policies; normal operation gates govern them.

TileMap retains one deterministic ring scan. Complete candidate footprints must avoid blocking live occupants, the planned producer and every earlier planned footprint, even for non-blocking outputs. PointSpace proof retains its existing authored offset, skipping unavailable points within its authored extent. Both implement the updated interface directly.

IResourceTransaction.EvaluateInitialInventory(EntityDef, Bill) delegates to original resource transaction/capacity/unit/filter contracts. Its addition accounts for definition.InitialStock, not empty invented capacity. The result is detached pure admission, not permission to retain a stale preview.

Oil FoundCompanyHandler.Plan checks company state, native placement/ground/reach, complete identity room, starting-stock admission and sequentially planned distinct vehicle placements. Apply recomputes admission before the first mutation, commits actual HQ creation and whole initialized stock, then actual vehicle creations and worker home links. Companies remains the once-only founding/state owner. Unexpected callback/commit inconsistency throws; this is fail-stop, not rollback.

Existing Oil scenario JSON continues to author HQ/stock/vehicle/radius choices. No stock/cost data moved into a harness or proof module. Full strict settings/content migration remains open.

## 3. API surface

- ISpatialRollout.FindRollout(World, EntityRolloutRequest, out SpatialPlacement).
- ISpatialRollout.CheckRolloutPlacement(World, EntityRolloutRequest, SpatialPlacement).
- EntityRollout.Find(World, EntityDef, SpatialPlacement, EntityDef, IReadOnlyList<PlannedEntityPlacement>, out SpatialPlacement).
- IResourceTransaction.EvaluateInitialInventory(EntityDef, Bill).
- Oil typed refusals oil.no-room-for-start-stock and oil.invalid-start-stock.
- No host, platform, Draw, business or clock API added.

## 4. Files to change

| Action | File | Risk |
|--------|------|------|
| Modify | src/Beep.SimEngine/Kernel/SpatialPlacement.cs | medium |
| Modify | src/Beep.SimEngine/Map/TileMap.cs | medium |
| Modify | src/Beep.SimEngine/Core/ResourceTransactions.cs; Standard/ResourceTransactions.cs | medium |
| Modify | src/Beep.SimEngine/Kernel/SaveGame.cs build marker only | low |
| Modify | Oil extension OilCo.Game/Commands.cs | medium |
| Modify | tests/ExtensionProof/PointSpaceProof.cs; ExtensionProof.csproj; tests/Beep.SimEngine.Tests/Program.cs | medium |
| New | tests/ExtensionProof/ProspectiveRolloutProof.cs; content/prospective-rollout-master.json, prospective-rollout-tile.json, prospective-rollout-point.json | low |
| New | tests/Beep.SimEngine.Tests/ProspectiveRolloutTests.cs | low |

## 5. Error handling matrix

| Condition | Result |
|-----------|--------|
| Missing/foreign definition, origin or prefix | Existing typed refusal; no result |
| Prefix outside actual selected geometry | OutOfBounds |
| All valid output positions unavailable | Blocked; no retained reservation |
| Complete initial stock exceeds capacity | NoCapacity; Oil typed stock-room blocker |
| Resource filter/quantum/unit incompatibility | Typed inventory admission failure before creation |
| Native commit changes after complete admission | Throw through existing world fault boundary; no recoverable partial-success fiction |

## 6. Backward compatibility

Development only. Updated provider signature replaces the old one; no legacy service/helper/shim. Existing live public Find remains a current use case, not a duplicate interpreter.

## 7. Verification

Compile only affected test/Oil projects. Run one new bounded exact prospective rollout case, rerun only for same-slice review changes. Retain all compile and case attempts. No old/family/full regression. Read-only metadata validation is not gameplay acceptance.

## 8. Risks

Prospective query proof is not physical manufacturing/hauling or actual Oil founding/uniqueness/stock commitment. Custom provider callback fault breadth, complete Oil JSON, identity exhaustion and complex initialization callbacks need later focused integration. Fail-stop is not transactional rollback. Historical checked criteria and earlier runtime evidence are not newly proven by this case.

## 9. Out of scope

Remaining FreeEconomy/LineService composition, business rules, network/mover/worker/producer content, complete Oil runtime/privacy/rivals/physical saves, platform hosts and full regression.

## 10. Sub-task execution order

1. Refactor original rollout request/provider boundary and both existing geometry implementations.
2. Extend original transaction interface for complete prospective inventory admission.
3. Replace Oil retired founding APIs with one pure Plan and native commits.
4. Add JSON/master-backed non-friend read-only proof; compile and run exact case.
5. Review prefix validation and live-source semantics; rerun only this case.
6. Update stage/evidence/master/audit while all broad gates remain open.
