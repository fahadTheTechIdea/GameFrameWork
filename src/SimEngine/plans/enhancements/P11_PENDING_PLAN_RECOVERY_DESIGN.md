# P11-C6 — Authored recovery of blocked unstarted playbook plans

Historical C6 implementation evidence below. [C7 whole cancellation](P11_WHOLE_PLAN_CANCELLATION_DESIGN.md)
replaces its serial per-member cancellation with one exact selection and deletes the PendingPlan cursor.
Policy authoring/composition preparation remain the original owners; C6 runtime was not rerun on2.293.0.

Status: native pending-intent slice implemented and focused verified,2026-10-08.
Build2.292.0/save142/playbook4/controller-view5. Four builds (two corrected compile failures);
two attempts of ONE new exact bounded case, final185 passed0 failed0 aborted1709ms.
[Every attempt and scoped evidence](../../docs/migration/P11_PENDING_PLAN_RECOVERY_VERIFICATION.md).
Executable inputs frozen after final green; full goal/plan remains active.

## 0. Problem

Standard/PlanPlaybooks.cs retains accepted native plan IDs, correctly waiting for real completion.
However, an accepted pending placement blocked by a subsequently occupied site waits forever even
when another current candidate is valid. Hosts must not implement recovery or cancel physical work.

## 1. Goals

1. Author optional recovery per existing playbook step through required JSON null/profile references.
2. Resolve an open policy contract through existing content ownership, without another controller.
3. Detect eligible blockage at each authored decision cadence using sole-clock saved chronology.
4. Cancel only actual unstarted plans through normal commands with fresh pending-only admission.
5. Wait for real cancellation answers before releasing execution links and selecting another site.
6. Respect active/paused/terminal/player-controlled work, bounded attempts and decision budgets.
7. Expose immutable recovery diagnostics; save/load/replay use the same provider state.

## 2. Design

Add IPlanRecoveryPolicy/PlanRecoveryPolicyDef and native PendingPlanRecoveryDef. Pure policy
observation receives the seat view, actual detached linked plans, attempt count and first eligible
blocked timestamp. Native JSON declares positive delay, maximum recoveries and explicit blocker IDs.
All linked entries must remain Pending/unpaused before a recovery episode can start; at least one
must have a matching blocker. Active work, external cancellation and paused player plans never
become automatic recovery. Delay spans successive eligible decision observations, not a second
per-frame monitor; transient changes between slower decisions are not independently recorded.
No stock, project, route or completed-state model is copied.

Existing Playbook Execution embeds BlockedSince, RecoveryAttempts, Recovering and PendingPlan.
Decide alone updates these after actual observations/answers. Read/CopySteps remains pure.
The original Pending ticket owns either placement or cancellation, discriminated by PendingPlan.
The provider issues ordinary pending-only CancelPlanCommand calls, one actual member at a time,
within its original command budget/cadence. Refused/stale cancellations retain real links; an
active member cannot be cancelled by this policy. After all members actually Cancelled, reset
the current link and reselect through the same private site source/current activation preview.
New group identity derives from authored group plus recovery attempt, preserving old native history.
No alternative-ready site means a typed wait, never instant relocation/refund/stock grants.

Add an explicit pending-only boolean to the existing cancellation command and board/view APIs.
Preview and commit share the same actual state check; native group/user callers explicitly request
ordinary cancellation. This is a clean wire revision, not a second cancel handler or compatibility path.
An expiring decision-context observation delegates policy reads through original provider-purity capture.

## 3. Contracts and versions

JSON section pending_plan_recovery_policies belongs to sim.player-controllers.
Each playbook step requires recovery:null or a selected registered policy ID. Independent policy
definitions can extend this contract through their own section; JSON never references CLR types.
Original playbook embedded schema becomes4; controller view declaration becomes5.
Build2.292.0/save142/replay1; no old state/command reader.

## 4. Files

Standard/PlanRecoveryPolicies.cs; PlanPlaybooks.cs; PlaybookDiagnostics.cs;
PlayerControllersModule.cs; Hosting/PlayerDecisionContext.cs; PlanContracts.cs/WorkPlans.cs/
WorkPlanGroups.cs; Commands/PlanCommands.cs; Views/WorkView.cs; Kernel/SaveGame.cs.
Migrate three existing proof JSON documents and older signature/identity fixtures compile-only.
Observed runtime repair: Kernel/Services.cs gains the public ICompositionPreparation contract.
The original alias-deduplicated composition pass prepares all providers before validating any.
WorkPlanCatalog.cs moves its one catalog capture from validation into preparation; no lazy lookup.
Add one explicit recovery JSON and one new bounded player case; docs/tracker/evidence.

## 5. Errors

Missing/unknown/malformed policy references/delays/attempts/blockers fail before Initialize.
Pending-only cancellation of active/terminal work returns native NotPending without mutation.
Provider protocol mutation/throw faults original Update; no recovery compensation.
Invalid saved tickets/ranges/timestamps/counters/schema refuse normal restore/checkpoint.
Ordinary placement/cancel refusals retain actual normal semantics and never become fabricated answers.

## 6. Compatibility

Development-only breaking revision. No legacy fields/default recovery/old command reader.

## 7. Verification

ONE new exact bounded case:
Player controllers: authored pending-plan recovery, physical rivals and durable cancellation

Cover blocked-site race, actual cancellation and alternative physical construction, save during delay/
pending cancellation/replacement, twin/replay identity, attempt cap, user pause, active cancellation
race, no-ready-site wait, immutable/private diagnostics, invalid authoring and independent policy.
All physical waits <=2000 updates with native diagnostics. One build after complete changes; record
every attempt, repair observed failures in this same case only. No old/family/full/platform runtime.
Freeze executable inputs after final green, then docs/read-only audit.

## 8. Risks

Only unstarted pending-intent recovery is safe here; active project loss/material recovery remains
owned by existing physical systems. Trusted external policy CPU/global purity remain broader gates.
If a multi-entry episode becomes partly active during cancellation, retain those links and wait;
do not cancel the active prefix, retry a completed structure or silently drop actual work.

## 9. Out of scope

Tycoon, Oil/client/platform changes, active-project relocation, grants, broad stress/regression,
new time/controller/job/plan authorities and premature Git publication.

## 10. Execution

- [x] Inspect existing plan/controller/site/cancellation/persistence ownership.
- [x] Implement policy seam, pending-only command and saved native recovery.
- [x] Migrate explicit authoring/signatures and add one bounded proof.
- [x] Build/run focused gate and freeze executable inputs.
- [x] Update authoritative tracker, evidence and current guide.

Open decisions: none for pending-only authored recovery.
