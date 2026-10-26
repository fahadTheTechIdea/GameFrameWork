# P9-C16: Named fleet recurring-service controls — Design Document

Status: implemented and focused-verified,2026-10-08; not complete-engine acceptance.
Full P1–P12 goal remains active. Source/public proof/tests/JSON frozen after final green.
Final119 checks0 failures/aborts1827ms, THREE clean builds/THREE same-case runs including two
bounded-stage aborts and new fixture-only repairs. Build2.271.0/save140/FleetView6/fleet1/recurring5.
[Evidence](../../docs/migration/P9_FLEET_SERVICE_CONTROLS_VERIFICATION.md) records every attempt,
actual coverage and remaining gates. No service spares/condition restoration/global sandbox claim.
Scoped737 canonical selected paths/152 Markdown/2196 local links/0 broken/0 duplicate
selected paths after slash normalization. Actual `git diff --check` exit0,31 existing LF/CRLF warnings.
Final readonly SHA checks and18 phase rows/five-column formatting recorded in source manifest.

## 0. Problem statement

RecurringPlanContracts and IRecurringPlanSource already own enabled/priority/interval/facility
settings and last submitted/completed cycle references. Named fleets cannot configure an explicit
source across current members without a host reproducing admission and risking partial mutation.

## 1. Goals

1. Original RecurringPlanContracts owns whole canonical explicit-source admission and exact commits.
2. Read actual provider codecs; preserve posting/completion/template and worker state; same value no-op.
3. Same current fleet membership delegates through normal command/private immutable FleetView.
4. Original WorkBoard/recurring sources own physical service, history and derived trip windows.
5. Prove actual native/public service with travel/work and multi-source earned deadlines, saved twins,
   edited intervals without age reset and loaded hauling without cancelling acquired cargo.

## 2. Design

RecurringPlanSelection.cs extends the original partial contract with canonical copied target IDs,
explicit named Code, detached nullable schedule per owned entry, whole preview and stable commits.
ReadSchedule guards existing codec in finally. Configure freshly preflights, no-ops exact settings,
verifies exact settings/increasing version/actual live identity/owner/clock/allocator, posting and
completion references, template settings and existing worker job/home/held state. No timer reset.
Ordinary whole refusal changes no prefix; violated commit faults session, not rollback.

IMachineFleets.PreviewRecurringPlan/ConfigureRecurringPlan use ControlledFleet and CURRENT IDs.
ConfigureFleetRecurringPlanCommand carries explicit source and original RecurringPlanSettings.Sync.
FleetView6 masks foreign settings and typed private refusal. Fleet service1/save140/physical layouts
unchanged; build advances. It is generic recurring intent control, not a hardcoded service scheduler.

Strict master JSON assembles actual native RecurringActivitySource and existing independent public
ProofRecurringSource, alternating two named sources on actual machines. Public module declares
the existing provider only, no second implementation. Service activities remain original authored
physical machine nodes, recurring work targets actual worker home/dock. HaulTripPolicy reads original
source completion deadlines. Tests submit commands/advance/inspect engine, not complete service.

## 3. API surface

RecurringPlanSelectionEntry/Preview; RecurringPlanContracts.PreviewGroup/ConfigureGroup;
IMachineFleets.PreviewRecurringPlan/ConfigureRecurringPlan; ConfigureFleetRecurringPlanCommand;
FleetView.PreviewRecurringPlan. Existing single-source control remains the sole settings protocol.

## 4. Files to change

RecurringPlans/new partial selection; MachineFleets/module/FleetCommands/FleetView/build identity.
Existing public provider declaration/strict JSON/startup docs/optional factory assembly/new exact
bounded test/registration. Older identity/view fixtures compile-only. Stage/evidence/manifest and
AGENTS/README/master/P9 finalized after green freeze.

## 5. Error handling matrix

Null/empty/duplicate/invalid IDs or absent source identity → InvalidArgument; invalid seat → NotSeated;
missing → NotFound; foreign commands → NotOwned/private → NotFound; missing provider → not-recurring-source;
invalid interval/facility/priority → original typed refusal. No fictitious schedule/default provider.
Actual mutation/inexact commit faults session; no healthy partial checkpoint.

## 6. Backward compatibility

Development only; revise sole owners directly. No legacy reader/shim/duplicate cadence store.

## 7. Verification

Completed slice build; ONE distinct exact NEW bounded case. Record every attempt, repair/rerun only
observed failures in that case. Canonical whole refusal/private pure frozen facts/idempotence/current
membership/all fields/pending saves/actual positive machine travel/work/service completion/multi-source
deadlines/interval edit history/loaded cargo/saved twins/pause/isolation/strict JSON/public fault.
1500 Update bound per physical stage with clock/sources/jobs/positions diagnostics. No old/family/full/
platform/stale runtime. Freeze gameplay/public proof/tests/JSON after final green; docs only.

## 8. Risks

An interval is earned-completion-based only when actual definition declares it; submission sources
retain their declared semantics. Editing enabled/priority does not cancel outstanding physical service.
Whole preflight is not a global malicious-mod rollback sandbox. Trip windows concern prospective
fresh dispatch, not a copied arrival guarantee or a cancellation policy for acquired cargo.

## 9. Out of scope

Named outlet reserve amounts, general fleet provider/fault/global purity/corruption matrices,
facility-policy combinations/templates, docked homes/formations/corridors/emergency/incident/non-tile/
no-work/replay/lockstep/CPU/stress/full gates. No Oil/Tycoon business source/host/demo/Draw/platform work.
Commit/push only after FULL goal AND plan finish and verification.

## 10. Sub-task execution order

1. Original recurring owner whole pure schedule admission/exact idempotent state-preserving commits.
2. Delegate current fleet membership/explicit-source command/private view.
3. Strict authored native/public physical multi-source proof and one new bounded case.
4. Build/exact gate/freeze; record every attempt and update tracker with remaining full scope.
