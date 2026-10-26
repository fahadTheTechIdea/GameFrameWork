# P12-D13 / P4-G2 — Joint productive dock feasibility

## Specification

Current response preparedness must not count one service bay twice, or count waiting bays as
simultaneous productive capacity. Check the canonical covered workers against actual dock geometry,
live job intentions, occupancy, policies, routes and departures. Undocked work retains ordinary
interaction admission; timed/completing nodes need no invented berth. Missing criteria or coverage
leaves the joint observation unevaluated. This is current feasibility, never acquired team authority.

## Single ownership

Extend SAME WorkBoard.PreviewDock/SelectDock/ClaimBlocker with transient proposed service slots and
excluded candidate IDs. Existing ordinary single-job paths keep their behavior. Proposed geometry
passes through SAME conflict check as live job slots, including overlap across different endpoint
and slot IDs. Iterative prefix search revisits earlier slots if later covered workers cannot fit;
no recursion, arbitrary graph limit, second selector, live job, claim ledger or provider clone.
Only actual jobs own dock intentions. Service-only mode excludes waiting/departure from productive
selection but uses existing departure-route checks. Views copy node/worker/endpoint/slot identities,
not provider handles or retained geometry. Existing jobs still acquire/recheck normally over time.

## API and sequencing

IncidentPreparednessSnapshot adds nullable FrontierDockBlocker and immutable FrontierDocks.
Null means unevaluated; Code.None means current complete set feasible. Failed checks expose no
misleading partial assignment. ReadyForFrontier additionally requires feasible joint docks.
Reuse actual selected worker/role/execution contexts from the existing material frontier capture.
Build identity advances; incident view schema advances; persistent save layout stays unchanged.

ONE exact NEW bounded case checks independent point docks, single bay plus waiting, disjoint bays,
live intentions, pure/private/frozen observations, physical waiting/work/departure and saved twins.
Build once; run this case only; repair/rerun only observed failures. Older identity assertions are
compile/source only. Freeze source/proof/tests/JSON after final green, then docs-only ledger/tracker.

Canonical engine only. No Oil, host, demo, Draw, platforms, business, alternate clock or compatibility
path. Full P1-P12 goal remains active; commit/push only after full verified completion. Alternative
worker/recipe teams, joint money, atomic acquisition, provider faults, replay/stress/CPU/full gates
remain open. This check does not guarantee future response or jointly timed departure capacity.

## Verified scoped checkpoint,2026-10-08

Implemented2.251.0/save139/incident view6/persistent payloads unchanged. TWO builds: first1 missing
test Register error5.47s/0 warnings; observed repair only; clean3.89s/0 warnings/errors. ONE exact NEW
bounded case55 checks0 failures/aborts659ms FIRST runtime attempt.117 older identity assertion
files/lines compile-only. Source/public proof/tests/JSON frozen after green; docs-only afterward.
No older/family/full/platform/stale gate. Existing master-authored workflow and dock JSON reused.
Independent point provider/shared and disjoint bays/area backtracking/provider overlap/live waiting/
service claims/pure-private-frozen facts/physical waiting and parallel work/departure/restart/eight
actual consumed units/sole-clock pause/saved twins proved. Wider cross-endpoint/fault/CPU/global
purity/atomic teams/money/future exits/replay/stress/full gates open.
[Report](../../docs/migration/P12_FRONTIER_DOCK_FEASIBILITY_VERIFICATION.md).
