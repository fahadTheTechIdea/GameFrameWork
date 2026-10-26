# P12-D8 — Company minimum-ready worker reserves

Status: focused verified, 2026-10-06; broader P9/P12 remain partial. Parents: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md),
[P12](P12_INCIDENTS_ENVIRONMENT_DESIGN.md), [D7](P12_WORKER_STANDBY_DESIGN.md).

## 0. Problem

D7 reserves explicitly selected machines. Existing CompanyAlertPolicies owns company controls but
cannot protect a minimum ready count per authored worker role across actual workers. Ordinary
dispatch can occupy every enabled responder. Extend that settings owner and existing shared admission.

## 1. Goals

1. Explicit JSON/current immutable settings declare canonical per-role minimum-ready counts.
2. Ordinary new work/hauling cannot lower any protected role below its floor; actual emergencies can.
3. Readiness derives from actual idle/unheld workers, declared roles, emergency-eligible enabled work
   policies and positive current execution gates/rate. No cached fleet membership or virtual machines.
4. Actual incumbent jobs finish; cancel/preempt/retry requires fresh admission, using D7's same relation check.
5. Existing company command/private preview/codec/version carries all settings; diagnostics are detached.
6. Native/public workers and policies obey the same rules; role references and dishonest callbacks fail.
7. One WorkSystem/WorkBoard/GameClock; no separate quota scheduler, resource reserve or timer.

## 2. Design

CompanyAlertSettings gains a required explicit list of WorkerReserveSetting(role, minimumReady),
canonical sorting/copy/equality/shared strict Read/Sync. Scenario company rows require worker_reserves,
including []. Existing ConfigureCompanyAlertCommand edits that value atomically through its sole owner.
CompanyAlertPolicies implements narrow ICompanyWorkerReservePolicy; both service contracts alias it.
Its settings schema replaces the old protocol; no constructor overload/default reader.

CompanyWorkerReserveRules reads ready roles from actual IWorker/IWorkerDefinition/IWorkPolicy/
IWorkerExecution contracts with existing scoped purity guards. Native company service derives counts
from current entities every admission, then refuses removing a ready candidate if any role's floor
would be violated. Multi-role workers count toward each compatible role; floors are independent,
not a guarantee of distinct actors for simultaneous incidents. No target route/material readiness is
invented: actual emergency candidate admission still checks role, area, route, fuel/materials and bays.

WorkSystem.CheckAssignment uses this optional capability only for new ordinary assignments. Extract
the existing live job/worker/company/role/remaining-stop relation into WorkPolicyRules.IsContinuation
and reuse it for individual standby and company floor admission. Existing emergency classification
and sequential fresh acquisition remain authoritative; no alert level or priority bypasses the floor.
Already assigned ordinary jobs and loaded deliveries never get retrospectively unassigned by edits.

CompanyAlertView exposes immutable role/count/minimum/shortfall diagnostics from the same contract,
subject to existing private company authority. Missing module deliberately means no company policy;
missing role/invalid settings/foreign company refuses, never an inferred reserve.

## 3. API

WorkerReserveSetting; CompanyAlertSettings(profile, level, workerReserves); required worker_reserves
JSON rows with role/minimum_ready. ICompanyWorkerReservePolicy.CheckOrdinaryAssignment/ReadReserve;
CompanyWorkerReserveSnapshot; CompanyAlertView.TryReadReserve. Existing command remains authoritative.

## 4. Files

CompanyAlerts/CompanyAlertModule/new stateless CompanyWorkerReserves, WorkPolicies/Work shared
admission, CompanyAlertView, SaveGame; explicit constructor/scenario/raw fixture migration; authored
reserve fixture/two focused test suites; stage/master/parents/README/help/evidence.

## 5. Errors

Missing/duplicate/negative/unknown role authoring rejects. Invalid current semantic role refuses
matching command/preview. work.company-minimum-ready is a queued wait, not job failure. Saved
noncanonical/truncated/unknown role settings reject. Scoped impure worker/policy reads fault, no rollback.

## 6. Compatibility

Replace protocol directly; no old constructor, implicit empty list, reader or copied company state.

## 7. Verification

Clean build and only these exact suites; same-name repair reruns allowed:

- Company reserves: authored floors, private controls and fresh admission
- Company reserves: physical dispatch, emergency use and saved continuation

Strict/canonical/private/immutable/codec/pending/unknown roles/pure reads; real multiple workers,
multi-role protection across haul/inspect, disabled/held/busy/zero-rate exclusion, actual emergency
response/supply, incumbent ordinary/loaded hauling, cancellation/reacquisition, saved twins/session/
sole-clock pause. Bounded waits with diagnostics. Older suites compile only; no family/full run.

Final build2.160.0/save87/board27 unchanged/work22/work view50 unchanged/company policy2/company
view2. Clean build;214 contracts +182 physical =396 checks, zero failures/aborts. Native/public worker
and individual policy combinations use the same native company settings owner. Public readiness
helpers and validated public snapshot constructor support independent implementations; a replacement
company service/provider matrix is not claimed. [Evidence](../../docs/migration/P12_COMPANY_WORKER_RESERVES_VERIFICATION.md).

## 8. Risks

Readiness is local dispatch capacity, not target-specific readiness or stock/fuel promises. An incident
may legitimately consume reserve below its floor; this does not conjure replacement workers. Queries
scan current workers, no performance claim. Independent floors can share multi-role machines; distinct
simultaneous-role allocation is a separate future explicit policy, not silently inferred behavior.

## 9. Out of scope

Reserve stock/maintenance exemption policies, target-specific preparedness, distinct multi-role quota
allocation, fleet creation, incident science/contamination, fuel/traffic/congestion, global provider
rollback/replay/fuzz/soak/stress/performance/full framework. No Oil/Tycoon/platform/Draw or extra clock.

## 10. Tasks

- [x] Inspect current company/work/clock authority and define shared count admission.
- [x] Extend sole settings, alias reserve contract, strict protocol and shared fresh admission.
- [x] Authored/native-public bounded contracts and physical scenarios.
- [x] Clean build/two exact gates, ownership review and tracker/evidence.
