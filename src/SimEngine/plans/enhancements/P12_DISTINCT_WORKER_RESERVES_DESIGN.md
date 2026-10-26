# P12-D12 — Distinct-worker reserve coverage

Status: focused implementation verified, 2026-10-06. Engine-owned preparedness; P9/P12 remain partial.
Build2.164.0/save91/work25/company policy3/view3; other owners unchanged. One unchanged GameClock.
Clean build;494 assertions/two exact suites (366 contracts;128 physical), zero final failures/aborts.
[Evidence](../../docs/migration/P12_DISTINCT_WORKER_RESERVES_VERIFICATION.md). Full objective remains open.

## 0. Problem

Before this stage, CompanyWorkerReserveRules.ReadyRoles reported each worker's compatible roles, but
independent ReadyCount floors double-counted a multi-role actor. Two machines with haul/inspect and one floor
per role admit ordinary work because both raw counts are two, leaving one machine for two protected
roles. The engine needs deterministic maximum coverage by different actual idle machines.

## 1. Goals

1. Existing JSON worker_reserves/settings remain the sole policy; no virtual machine/roster/claim state.
2. One stateless computation captures actual ready roles and matches one worker to at most one floor
   slot, with deterministic role/worker ordering and augmenting paths (not greedy assignment).
3. Aggregate and per-role immutable views report compatible count, distinct coverage and shortfall.
4. Ordinary acquisition preserves existing role floors and cannot worsen distinct coverage; genuine
   emergency/self-service and incumbent continuation retain their existing engine-derived exemptions.
5. Very large authored counts produce a wide shortage without allocating one node per requested slot.
6. Public worker/policy providers participate through current contracts. Reads/save/hash do not create
   a scheduler or consume/reserve machines, resources or time.

## 2. Design

Extend CompanyWorkerReserveSnapshot with CoveredWorkers; Shortfall uses distinct coverage, while
ReadyWorkers and Headroom explicitly remain raw role compatibility facts, not assignment promises.
Add detached CompanyWorkerReserveCoverage and WorkerReserveAssignment projections through the existing
ICompanyWorkerReservePolicy/CompanyAlertView. Existing CompanyAlertPolicies aliases both contracts.

CompanyWorkerReserveRules captures idle/unheld, enabled/emergency-eligible, positive-rate/gate workers
using current pure capability reads. Sort actual identities and canonical authored roles. Iterative
augmenting paths maximize covered slots while preserving earlier ordinal roles; role capacity is the
authored floor and worker capacity is one. Requests scale with actual fleet/roles, not billion-sized
floor counts. Required/shortfall totals are checked Int64. Returned assignments are illustrative
coverage facts, not live jobs/reservations or target-specific readiness.

Replace ReadyCount and ordinary admission loops with this same transient computation. Removing a
candidate must not reduce total maximum coverage or worsen any raw protected role shortage. Every
fresh assignment rechecks current facts; policy edits never claw back acquired work or cargo.

## 3. API

Required CoveredWorkers snapshot constructor argument; ICompanyWorkerReservePolicy.ReadCoverage;
CompanyAlertView.TryReadReserveCoverage. Existing company policy commands/JSON remain unchanged.
No compatibility overload or second policy store; views are detached and immutable.

## 4. Files

CompanyWorkerReserves, CompanyAlerts metadata, CompanyAlertModule view registration, CompanyAlertView,
Work semantics metadata, SaveGame and exact migrated callers. New focused tests/authored fragment,
stage/evidence/current docs/master. No Oil/Tycoon/platform/Draw/host changes.

## 5. Errors

Unknown company/role remains typed refusal. Invalid coverage/snapshot/duplicate assignments reject.
Dishonest provider reads use existing scoped guards. Real unmet coverage remains shortage, never a
fake second actor. No integer narrowing of aggregate requested capacity.

## 6. Compatibility

Development-only breaking projection/semantic replacement; no alias, legacy constructor or reader.

## 7. Verification

Clean build and exactly:

- `Distinct reserves: maximum coverage, private contracts and deterministic matching`
- `Distinct reserves: physical dispatch, emergencies and saved continuation`

Multi-role overlap, augmenting reassignment, shortages/large floors, non-ready workers, native/public
providers, frozen/private/pure views, current controls, ordinary rejection/extra compatible actors,
actual emergency use, policy edits/physical work/hauling, saved twins/session/pause. Bounded workflows.
Older suites compile only; no third/family/full runtime gate.

## 8. Risks

Coverage is simultaneous role capacity, not target stock/route/bay/fuel readiness or a reservation.
Stable ordinal priority explains which floor receives scarce workers. Repeated capture cost and large
fleet allocation budgets require later profiling; this stage does not claim performance acceptance.

## 9. Non-goals

Target-specific preparedness, material/supply exemptions, guaranteed multi-stage incident teams,
general fleet/traffic/science/global purity/replay/stress/full acceptance. One unchanged GameClock.

## 10. Tasks

- [x] D12.1 — Extend current immutable coverage contracts and implement deterministic matching.
- [x] D12.2 — Replace existing ordinary floor admission with shared fresh coverage checks.
- [x] D12.3 — Prove two exact suites after a clean build.
- [x] D12.4 — Record evidence/current tracker and preserve remaining full objective.
