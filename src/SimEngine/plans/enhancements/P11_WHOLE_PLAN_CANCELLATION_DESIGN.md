# P11-C7 — Whole native plan cancellation and grouped recovery

Status: native whole pending-plan slice implemented and focused verified,2026-10-08.
Build2.293.0/save143/playbook5/controller-view6/work-view83. One clean build12.88s0 warnings/errors;
ONE exact NEW bounded case FIRST100 passed0 failed0 aborted2495ms. Executable inputs frozen.
[Every build/run and scoped evidence](../../docs/migration/P11_WHOLE_PLAN_CANCELLATION_VERIFICATION.md).
Full goal/plan remains active; broader gates below are not complete.

## 0. Problem

C6 Standard/PlanPlaybooks.cs cancels accepted template members one command at a time.
A later member can become active or player-paused after the first cancellation, leaving a
partly cancelled cohort. The engine needs whole admission over actual IDs, not group-name inference.

## 1. Goals

1. One immutable canonical selection describes exact owned plan IDs and explicit pending-only mode.
2. Original WorkBoard previews every selected member before any cancellation; ordinary refusal
   preserves all selected plans, projects, reservations and progress.
3. Pending-only mode refuses active/terminal/paused members after stale previews; all current
   unstarted members cancel together in one normal command.
4. Existing single/user/group cancellation routes share that same owner, without legacy overloads.
5. Playbook submits one exact accepted template range and waits for its whole real answer before
   reselecting. A refusal preserves the original range and resumes ordinary actual-state diagnostics.
6. No per-member cancellation cursor/state survives; original controller/plan codecs remain sole.

## 2. Design

PlanCancellationSelection uses existing PlanIntent canonical ID validation/Sync: positive unique
nonempty identities, explicit PendingOnly. Existing CancelPlanCommand contains Selection only;
handler returns first sorted ID and exact count. IPlanBoard/WorkView PreviewPlanCancel accepts
the selection and returns existing immutable PlanGroupCheck per-entry blockers.
CancelPlan rechecks the entire set, then uses original native cancellation settlement for each.
Group APIs derive actual live members and delegate to the same selection owner.
Ordinary active cancellation remains physical and may fault on a provider violation; no rollback
or claim that arbitrary external mutation is transactional. Atomicity here means whole preflight
for ordinary refusal and complete native unstarted cancellation without gameplay callbacks.

Playbook derives the exact range from current First/Count, not mutable groups. Its existing Pending
ticket and Recovering flag discriminate placement vs whole cancellation. Remove PendingPlan entirely.
Initiate attempts only when the whole normal command previews ready; accepted answers must match
range/count and actual Cancelled native state before resetting the current link. Refused commands
clear cancellation-in-flight, retain First, and resume current native plan/limit diagnostics.
Attempts still count actual submitted cancellations. Paused/active readiness refuses before submission.
Immutable diagnostics expose current exact CancellationPlans only while the real command is pending.

## 3. API and versions

Clean cancellation command/API revision, no old Plan/PendingOnly fields/overloads/readers.
Build2.293.0/save143/replay1/playbook5/controller-view6/work-view83.
Existing JSON policies/site/template authoring unchanged; one new explicit grouped proof document.

## 4. Files

Standard/PlanContracts.cs, WorkPlans.cs, WorkPlanGroups.cs, PlanPlaybooks.cs, PlaybookDiagnostics.cs,
PlayerControllersModule.cs, WorkModule.cs, Commands/PlanCommands.cs, Views/WorkView.cs,
Kernel/SaveGame.cs; migrate older source identities/cancellation callers compile-only.
One new public player proof case, explicit grouped JSON, embedding; docs/tracker/evidence.

## 5. Errors

Null/empty/duplicate/invalid IDs refuse construction/admission. Foreign/missing IDs retain native
permission/not-found semantics per entry. Pending-only active/terminal gives NotPending;
paused gives Paused. No prefix cancellation on any ordinary refusal. Malformed command answers/
saved in-flight links or interrupted provider mutation cannot yield a valid checkpoint.

## 6. Compatibility

Development-only breaking redesign, no per-member fallback, old reader or parallel cancellation.

## 7. Verification

ONE new exact bounded case:
Plans: whole cancellation, grouped rival recovery and saved physical execution

Prove shuffled immutable selections, mixed-owned/missing/terminal/active/paused whole refusal,
stale queued command refusal without prefix, real native grouped recovery and ordinary budget1,
re-grouping stability, pending whole-cancel saves, accepted-answer replacement, physical material
consumption and replay twins. Bounded physical waits <=2000 updates with diagnostics.
Record every build/run; only repair observed same-case failures. No old/family/full/platform run.
Freeze executable inputs after final green, then docs/read-only audit.

## 8. Risks

General active-project cancellation/provider atomicity, larger interruption/corruption/lockstep
matrices, optional-money/profile breadth and full gates remain open. No rollback fiction.

## 9. Out of scope

Tycoon, Oil/client/platform changes, instant relocation/refunds/grants, new scheduler/clock/store.

## 10. Execution

- [x] Inspect existing native cancellation/recovery/codec/selection owners.
- [x] Revise command/board/group/view cancellation to one whole-set path.
- [x] Remove per-member recovery state and migrate callers/identities.
- [x] Author new grouped physical proof, build/run and freeze.
- [x] Update tracker/evidence/current guides and source manifest.

Open decisions: none for native whole pending cancellation.
