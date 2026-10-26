# P3-GT1 — Goal-directed deterministic A* ties

## 0. Problem and specification

Current2.665 canonical continuation1453->2053 exceeds the unchanged120000ms
stage bound at122774ms. Native state is healthy and physically progressing:
shipyard erection507/3600, depot clearing complete, grid clearing complete,
loaded trucks actually moving. Inspection18/0 and one profiled Update24/0
retain2054/hash0748168D86EE6F2C. Actual Update stack samples full spatial hashing
inside NavGrid.CanReach -> TileWorkDock.Check -> retained dock rebinding.

PathHeap currently compares estimated total cost, then row/column only. On a
uniform-cost shortest-path plateau this expands many equal-f tiles away from
the goal before admitting the equally optimal goal-directed frontier. Change
tie order, not path costs, callbacks, budgets, physics or observation guards.

## 1. Goals

1. Keep estimated total cost the primary priority. Within ties prefer greater
   travelled cost (equivalently smaller remaining heuristic), then row/column.
2. Keep optimal rounded edge costs, obstacle/corner/area/occupancy callbacks,
   sparse query scratch, nested safety, expansion bounds and typed refusals.
3. Prove symmetric authored plateaus finish with80 expansions and at most600
   unique cost callback reads instead of broad equal-cost exploration.
   This is a fixture budget, never an engine size or global route limit.
4. Verify weighted shortest-path oracle, real native physical saved work and
   exact-goal provider faults separately; no full/profile/family test run.

## 2. Design

Sole PathHeap.Compare in Standard/Navigation.cs compares Priority ascending,
Distance descending, Tile.Y ascending, Tile.X ascending. Equal-f queue order
does not change the cost function/admissible heuristic or closed-set rules.
No alternate search, straight-line shortcut, route cache, extra grid, trusted
provider exemption or changed expansion cap. All expanded entries still use
original guarded costs and permitted-step checks.

This intentionally changes which equally optimal route is selected; stable
replay/peer identity therefore advances to2.666. No compatibility promise or
old native hash expectation. Save160/replay1/component schemas unchanged.

## 3. API surface

No new public contract, JSON gameplay field, state owner or scheduler. Update
route documentation to name actual priority/distance/row/column ordering.

## 4. Files

Navigation.cs, SaveGame.cs, current-engine-protocol.json. Extend existing
CapacityLogisticsStressTests via partial GoalTies.cs, master-listed embedded
testdata/navigation-goal-ties.json and its existing Register method. Existing
oracle and physical cases unchanged. Stage/report/master/AGENTS/README updated.

## 5. Errors

Zero/insufficient expansion budgets still refuse; bounded refusal exposes no
old path. Actual provider throw/mutation retains fail-stop guards. Blocked or
weighted routes may need more expansions; no universal80-expansion assertion.

## 6. Compatibility

Development only. Preserve2.665 checkpoint748334a63b1f41aeafad46128b6c1922 as
historical once code changes; never rewrite its header or synthesize progress.

## 7. Verification

New exact plateau/heap case on pre-edit build, same exact on implementation.
Separate existing rounded-edge oracle, exact-goal negative/provider case and
native layered physical saved/replay case. Bounded exact child processes only.
One target compile per baseline/final, no automatic repeats of passing cases.
Native continuation is a separate acceptance scope, not inferred from heap tests.

## 8. Risks

Equal-cost routes can alter actual traffic interaction and game-state hashes;
scoped physical proof cannot certify universal deadlock freedom. A different
tie cannot cure all low heuristic/minimum mismatches or full spatial hashing
cost. No end-to-end speedup claim until actual current workload is measured.

## 9. Out of scope

Guard/hash bypasses, altered content/stock/work speed, larger engine map or
route/time limits, Oil/business/host/platform/Draw changes, broad regression,
full-plan closure or Git publication.

## 10. Execution order

1. [x] Reproduce broad equal-cost frontier with authored symmetric queries (actual38/8).
2. [x] Change sole heap comparator and actual identity, preserving search.
3. [x] Run narrow oracle/guard/physical checks; record all attempts and limits.

[Every attempt, exact identities and current native continuation](../../docs/migration/P3_GOAL_DIRECTED_ROUTE_TIES_VERIFICATION.md).
Final four separate exact framework cases479/0; plateau reads1926/326/326/1884
become326 in every direction at identical cost960/80 physical steps. Bare search
measurement is not a native provider-guard proof; separate native tests retain
that boundary. New-build native preparation/day/single-update70/0; day82469ms,
active Update468ms. Harbour/full performance/full goal remain OPEN.
