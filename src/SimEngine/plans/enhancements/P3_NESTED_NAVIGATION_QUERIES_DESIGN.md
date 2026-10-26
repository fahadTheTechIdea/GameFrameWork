# P3-NQ1 — Reentrant navigation query scratch ownership

Status: scoped implementation verified on2.662.0/save160/replay1.
Baseline20/10; final nested130/0, selected shortest-path360/0 and physical82/0.
[All attempts, identities and limits](../../docs/migration/P3_NESTED_NAVIGATION_QUERIES_VERIFICATION.md).
Original221/209 recorded checked/12 open; full goal remains ACTIVE.

## 0. Problem and specification

Navigation.cs gives every NavGrid in one NavService the same sole PathSearch.
PathSearch.Find keeps distance/parent/closed/entry-cost/heap scratch in shared
fields. A pure cost or route callback can synchronously query a second grid,
which clears and overwrites those fields while the outer search is still active.
The query-local entry-cost cache can then contain another profile's costs;
parent reconstruction/frontier state can likewise be corrupted. Observation
guards cannot fix transient algorithm scratch corruption.

This emerged while auditing the remaining route/logistics cost. Correctness comes
first; this stage does NOT claim to solve whole-scenario update performance.

## 1. Goals

1. Reproduce with an actual JSON-declared independent navigation provider, both
   registered ITileNavigationRule and raw persistent callback paths. Its only
   added instrumentation asks an authored uniform inner profile for a route.
2. Keep ONE PathSearch algorithm, NavService, route/clock/observation authority.
   Give each active synchronous invocation exclusive scratch; reuse it after
   return. No per-grid or full-map search table, second pathfinder or world cache.
3. Preserve byte/state layouts, minimum-cost/corner/ordering/expansion rules,
   first exceptions, pure saved queries and interleaved-session isolation.
4. Prove nested cost/step/reached callback isolation and scratch release on errors,
   plus selected existing shortest-path and physical saved-routing cases.

## 2. Design

Within the existing PathSearch, move its five scratch collections into a private
Scratch class. A per-instance depth-indexed pool borrows one Scratch per active
Find and returns the depth in finally, including early refusals/exceptions.
FindCore holds its Scratch as a local parameter; nested callbacks never change
which collections the parent reads. Entry-cost caching stays invocation-local.
Only the original algorithm accesses these collections; they are not saved game
state, immutable views, an alternate route or a second gameplay owner.

Extend existing NavigationObservationDef with an explicitly authored nested-query
mode and actual inner navigation/from/goal/expansion references. New embedded
navigation-nested-observation.json is selected by the existing module/master
mechanism. Same CapacityLogisticsStressTests owner adds a separately selected
partial proof; no stress-family run.

## 3. API and state

No public API or persisted schema change. Runtime engine identity advances to
2.662/save160/replay1 after reproduction. Never rewrite old save headers.
Separate sessions retain separate NavService/PathSearch/pools; this does not
authorize concurrent mutation of one simulation world.

## 4. Files

Original Standard/Navigation.cs, SaveGame.cs/current protocol; existing navigation
probe and test registration, same-owner partial proof/embedded JSON registration;
stage/evidence/master/README/AGENTS and existing navigation parent.

## 5. Errors

Original callback errors propagate unchanged. Every scratch borrow returns its
depth in finally. A provider fault still latches the existing World first fault;
no healthy save/update or compensating state after failure. Pure nested queries
must not fault or mutate game state merely because they use the shared router.

## 6. Compatibility

Development only; in-place ownership correction, no legacy/compatibility path.

## 7. Verification

New exact nested-query case on pre-edit engine, then same case after awaited
target build; each bounded30000/termination5000/output14000. Select existing
rounded-diagonal and independent physical layered-routing cases separately.
No full/profile/family or expensive native long-day run. Retain all failures.

## 8. Risks

Depth must unwind before a returned frame is reused. Parent/child output lists
remain caller-owned and must not alias intentionally. Mutually recursive cost
definitions/infinite arbitrary CLR callbacks and concurrent calls on one world
are not proved safe by this acyclic synchronous-query fix. Pool high-water memory
is not a general allocation/soak or platform-size-limit claim.

## 9. Out of scope

Weakening guards, caching world hashes/costs across queries, changing physical
route/claim/fuel/dock rules, business/Oil/hosts/Draw/platform edits, full acceptance,
performance victory or Git publication.

## 10. Execution

1. [x] Reproduce original shared-scratch failure through actual public profiles.
2. [x] Isolate active scratch within sole original PathSearch.
3. [x] Await build; verify same proof and two affected cases separately.
4. [x] Record evidence, identities and original unchanged completion gates.
