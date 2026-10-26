# P9-DJ1 — Prune proven noncompetitive departure journeys

## 0. Problem and specification

WorkBoard.DockJourney.Finish evaluates every eligible departure route before
discarding noncompetitive results with its existing Cheaper comparator. A live
canonical construction stack shows this route/terrain/observation path. Once an
actually feasible departure is found, a worse or losing-tie departure cannot
change the selected journey. Its route and continuation need not be evaluated.

## 1. Goals

1. Keep the identical selected feasible slot, cost, route duration and stable tie order.
2. Reject a dominated candidate only after its original guarded policy and cost
   reads and after a genuinely feasible incumbent, not merely a cheap estimate.
3. Preserve attempts of cheaper alternatives, all candidates before first feasible
   result, retained claims, physical travel, callbacks actually used and refusals.
4. No cache, route bypass for a selected journey, second scheduler, business change,
   shortened work, altered timeout or claimed general traffic fix.

## 2. Design

Inside the sole existing WorkBoard.DockJourney.Finish departure loop, after
DockPolicy and DockCost, continue when found && !Cheaper(exit,exitCost,best,bestCost).
Use the original comparator; do not reimplement ranking or sort by a new metric.
Only then invoke the original PlanRoute and continuation. The main service-slot
selector and retained-slot behavior remain unchanged.

## 3. API surface

No new production contract/state/codec. Engine build identity advances to2.644;
save160/replay1 unchanged, no old-header migration. Test-only PointMotion route
faults expose invocation of a losing candidate without a second route algorithm.

## 4. Files

Standard/WorkDocks.cs, Kernel/SaveGame.cs, current protocol fixture; existing
DockTravelSelectionTests and PointSpaceProof. Stage report and current tracker.

## 5. Errors

Negative cost and actual selected-route mutations/throws retain original guards.
An unavailable cheaper exit cannot prune a feasible later one. When none succeeds,
original typed refusal/Pending precedence remains because no incumbent is found.

## 6. Compatibility

Development only; no compatibility shim, cached positive answer or migration.

## 7. Verification

One new exact query case before/after: dominated far exit, losing equal-cost tie,
and blocked cheap exit requiring later fallback, including reversed authoring and
native save/load purity. Then the existing exact physical dock-travel case for
ordinary movement/departure, saved twins/replay, chronology and original faults.
No full/profile/family test run. Canonical harbour remains separately bounded.

## 8. Risks

This only removes provably irrelevant route evaluations. Spatial hashing and
non-dominated/native traffic work can remain expensive. Never infer global speed,
deadlock freedom or complete harbour acceptance from the local proof.

## 9. Out of scope

Service selection, new policies, road yielding, geometry/data/stock/timing changes,
platform/Draw, broad regression or Git publication.

## 10. Execution order

1. [x] Reproduce unnecessary invocation through independent registered motion.
2. [x] Prune losing exits using existing comparator in the sole journey owner.
3. [x] Verify focused query and physical scopes; record limits/current tracker.
   Canonical harbour/full acceptance remains partial, not a passed broad gate.

[All attempts and scope](../../docs/migration/P9_DOMINATED_DOCK_JOURNEYS_VERIFICATION.md).
