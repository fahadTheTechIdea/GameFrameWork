# P3-NC1 — Admissible authored diagonal route costs

Status: implemented/scoped-verified2.659.0/save160/replay1. Original PathSearch
owner, not another navigator. [All attempts/identities/limits](../../docs/migration/P3_DIAGONAL_ROUTE_COST_VERIFICATION.md).

## 0. Problem and specification

Route-cost investigation on2.658 sampled actual native worker33372 at1992:
StateHasher.Bytes -> TileConditionContracts.Matches -> NavGrid.EntryCost ->
PathSearch.Find -> Mover.CheckRoute -> joint WorkBoard.DockJourney -> RunJob.
That one Update takes889ms; repeated whole-space callback checks remain intact.
The investigation does NOT establish a safe performance fix yet.

Source Navigation.cs PathSearch computes diagonal edge floor(cost*14/10), but
Heuristic computes floor((14*diagonal+10*straight)*minimum/10). Rounding once
after summation can overestimate a chain of rounded edges for minimum values
not divisible by5. This violates the stated admissible A* contract. A generated
12x12 map suggests old result20 versus optimal19; reproduce with actual engine
before repair. Actual Oil paved minimum4 is affected in principle; no Oil data
or rules change is proposed.

## 1. Goals

1. Actual existing PathSearch agrees with an independent Dijkstra oracle for
   the counterexample, varied weighted maps, directed steps and corner gates.
2. Compute a consistent lower bound from the exact original per-edge rounding.
3. Keep coordinate tie breaks, all guarded callbacks, query-local scratch,
   explicit budgets, route/travel/area owners and one clock unchanged.
4. No performance win or harbour completion claim from an algorithm proof.

## 2. Design

In original Navigation.cs Heuristic, use
diagonal*floor(minCost*14/10) + straight*minCost with original saturation.
For each legal edge its actual positive cost is at least this edge's lower
bound. Octile distance is then an admissible and consistent shortest-path
distance on an obstacle-free lower-bound graph. Constraints and blockers only
remove edges/increase cost; no reopening or new search/cache owner is needed.
Keep PathHeap priority,row,column order. No cost scale is rounded in content.

Existing CapacityLogisticsStressTests gains a separate exact algorithm literal
through a partial file; running it does NOT run the stress case. Test inputs
come from explicit embedded JSON; an independent exhaustive-selection Dijkstra
oracle exists only for assertions, never gameplay/host decisions. Reuse same
PathSearch instance to exercise scratch clearing. Check path legality and cost,
once-per-query reads, deterministic repeats, budget and propagated exceptions.
One existing guarded exact-goal case and one native layered physical saved/replay
case provide scoped integration, individually selected after completed build.

## 3. API and state

No public API, JSON gameplay, codec or clock changes. Corrected route choices
may differ; bump EngineInfo.Build/current protocol2.659.0, retain save160/replay1.
Old native saves remain historical/incompatible, never rewritten or migrated.

## 4. Files

Modify sole Navigation.cs heuristic, SaveGame.cs identity, existing stress test
registration (partial), Tests.csproj and protocol fixture. Add same-owner partial
algorithm proof and authored testdata JSON. Update this stage/master/evidence.

## 5. Errors

Keep original BudgetExceeded/Unreachable/refusal distinction, callback errors,
cleared failure path, spatial/clock faults and original saturated priority.
No fallback route or invented passability on refusal.

## 6. Compatibility

Development only; exact build matching, no legacy/shim/header rewrite.

## 7. Verification

New exact literal baseline on2.658, then same case after repair. Targeted build
must finish before tests. Individually run affected exact-goal/physical layered
cases, not full/profile/family tests. Record all attempts/source identities and
retain native1993/hash8B90E5B1A1A40652 as last2.658 checkpoint if identity changes.

## 8. Risks

Correctness can expand more nodes than the old inadmissible heuristic; do not
claim performance improvement. Independent oracle and fixed expected19 protect
against copying the same heuristic defect into the assertion. Test fixtures are
not a substitute for actual complete harbour/fleet acceptance.

## 9. Out of scope

Guard/hash shortcuts, new route cache, heap tie changes, Oil/business/platform/
Draw changes, full tests, original broad closure and Git publication.

## 10. Execution order

1. [x] Reproduce actual fixed-map failure against independent shortest-path oracle.
2. [x] Correct sole heuristic and explicit build identity.
3. [x] Await target build; run new literal and separate affected integration cases.
4. [x] Record evidence/current compatible artifacts/remaining original gates.
