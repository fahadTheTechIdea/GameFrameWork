# P9-DC1: deterministic travel-cost dock selection — Design Document

## 0. Problem Statement

WorkBoard.SelectDock returns the first eligible canonical slot rather than comparing actual provider costs. IWorkDock.TryEstimateCost has no prospective origin, so later journey admission can price destination slots from the carrier's old physical position. This violates travel-cost-before-stable-ID selection.

## 1. Goals

1. One original selector evaluates all eligible slots of the preferred kind, choosing minimum actual provider cost and stable ID on ties.
2. Service precedes waiting; existing valid claims remain retained, not displaced by cheaper alternatives.
3. Cost receives explicit actual/prospective spatial origin; native tile and independent point providers validate it, without coordinate fallback.
4. Full route/vacancy/area/queue/departure gates remain authoritative; no movement, stock or claims occur in previews.
5. Physical saved hauls prove closest slots, prospective origin, equal-cost identity, array-order independence and retained claims.

## 2. Design

Replace IWorkDock.TryEstimateCost signature with World/carrier/slot/SpatialSnapshot origin/out cost. Null explicitly means current carrier position; foreign/unsupported snapshots refuse. Update original TileWorkDock, PointDock and negative admission provider directly; no overload/shim.

WorkBoard.DockCost uses original EntityObservation and carrier-version validation. SelectDock retains existing claims first; fresh candidate selection keeps kind priority, then provider cost, then Before stable identity. Every selected candidate must still pass actual queue/vacancy/area/complete journey gates. Selected arrival/estimate come from the same candidate. No new coordinator, route, slot book, policy store or clock.

## 3. API surface

Changed IWorkDock.TryEstimateCost(World,Entity,Code,SpatialSnapshot,out long). Actual authored slot geometry/roles/resources/areas remain JSON; native cost arithmetic is generic provider behavior, never host logic. Save payload/slot identities unchanged.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| Modify | Standard/DockContracts.cs, WorkDocks.cs, TileWorkDocks.cs | 40 | high |
| Modify | PointDockProof/DockSpatialAdmissionProof signature | 8 | medium |
| New | actual JSON and public-player dock selection proof | scoped | medium |
| Modify | engine/protocol identity, stage/evidence/master | scoped | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Eligible service/wait candidates | Minimum provider cost within preferred kind; stable ID tie |
| Valid retained slot | Same original claim retained |
| Foreign/mismatched origin | Explicit unsupported/refusal, never current-position substitute |
| Mutating/negative provider cost | Original first fault, no acquisition prefix |
| Occupied/full/area/route/departure gate | Existing physical refusal/wait |

## 6. Backward compatibility

Development only. Replace signatures and first-match path, no legacy overload/default origin shim.

## 7. Verification

One solution compile and necessary focused repairs only. One NEW bounded public-player case, JSON/master/Update/views/saves/replay/private/isolation. No historical/family/full runtime.

## 8. Risks

Physical one-dimensional crossing is not solved by cheapest slot; actual occupancy remains. Retained claims/fairness cannot be traded away. Provider cost is authoritative and deterministic, not a promised ETA or currency.

## 9. Out of scope

All native tile/arbitrary cost/queue/congestion/retarget/stress/full matrices, Oil/platform/Draw/Tycoon/protected G19/clock source edits or publication.

## 10. Sub-task execution order

1. Migrate actual/prospective origin cost contract and its existing providers.
2. Replace first-eligible fresh allocation with minimum-cost original selector.
3. Prove public physical slot choice/durability and update evidence/master without shrinking original gates.

## 11. Scoped result

Core2.475.0/save154/replay1, original selector/provider contract migrated with actual prospective origin and shared minimum-cost/stable-ID ranking for fresh service and departure choices. Valid claims retained. ONE NEW bounded FINAL241/0/0 in1596ms, twelve physical minute/half and ordered/reversed flows plus two observed cost-provider faults. One solution failure/three clean focused builds and all three runtime attempts retained; no executable edit after FINAL or broad runtime. Original221/186checked/35open unchanged; additional202. Full native/arbitrary-provider/fairness/congestion/fault/stress/P1-P12 and publication remain open. [Evidence](../../docs/migration/P9_DOCK_TRAVEL_SELECTION_VERIFICATION.md).

Fresh read-only metadata closure:1684 source anchors,935 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2872 paths/1012 Markdown files/8645 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors, unchanged186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2860/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional202. Ten existing selected executable inputs changed, two new executable inputs/four new source files, four existing production sources changed/zero new. No runtime rerun for documentation checks.

