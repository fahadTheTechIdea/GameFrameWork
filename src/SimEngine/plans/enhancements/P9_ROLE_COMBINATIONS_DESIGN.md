# P9-B4h/P12-D14 follow-on — Authored worker-role combinations

## 0. Problem

Standard/Work.CopyCandidatesCore selects one supported/allowed role before evaluating native
requirements and dock admission. A multi-role actor can be blocked by that first role while another
authored role is feasible. WorkScheduling.FitsAssignmentSet and WorkIncidentPreparedness also pick
the first row for a matched worker, hiding jointly feasible material/dock alternatives.

## 1. Goals

1. Expose one deterministic pure candidate per supported (actual worker, authored role) pair.
2. Keep live assigned workers pinned to their actual Job.Role; never silently relabel live execution.
3. Keep combined worker matching distinct by actual worker identity, never count roles as machines.
4. Joint role/material/dock/optional-cost admission shares original WorkBoard.CheckWorkSet.
5. Emergency preparedness uses that same admission and exposes its illustrative covered role.
6. Prove JSON-authored role-aware inputs, pending-first/ready-second roles, scarce/abundant parallel
   teams, role restrictions/cancellation, actual emergency response, physical saved continuation,
   private detached facts, isolation and canonical replay through ordinary commands/views.

## 2. Design

WorkSystem.CopyCandidatesCore evaluates every supported authored role through existing policy,
executor/dock/route/requirement/preemption checks. Original candidate ranking is unchanged.
For an actual live assigned worker retain Job.Role only. Native dispatch-edge projection deduplicates
worker IDs; public provider duplicate worker offers remain invalid.

One overload of original WorkBoard.CheckWorkSet receives aligned candidate alternatives. It searches
canonical role combinations iteratively, delegates EVERY resource/dock/cost decision to its original
complete-set overload, and returns chosen actual candidate/result/dock facts. No new resource selector,
receipt ledger, worker registry, coordinator, state model or persistent search. Failed combinations
claim nothing; first preferred refusal is retained when none fits. Existing native acquisition
freshly rechecks exact role/source/dock choices before physical execution.

Original WorkScheduling and WorkIncidentPreparedness pass all satisfied role options for each
distinct matched actor to that same owner. Preparedness distinct edges deduplicate worker identity;
live incumbent role stays pinned. CoveredRole is detached illustrative readiness, not authority.

## 3. Contracts

IWorkerDispatch.CopyCandidates documents worker/role pair semantics. No method signature change.
IncidentPreparednessNode adds immutable CoveredRole. Work-view85/incidents-view8 declare the changed
projections. Build2.301.0/save143/replay1; persistent layouts unchanged.
One independent normal JSON requirement provider maps explicitly authored roles to actual target or
worker resource endpoints. It extends existing ResourceWorkRequirementDef/IWorkerWorkRequirement,
never introduces balances or scheduler branches.

## 4. Files

Standard/Work.cs, WorkScheduling.cs, WorkStart.cs, WorkIncidentPreparedness.cs, WorkContracts.cs,
WorkModule.cs, IncidentResponseModule.cs, Kernel/SaveGame.cs. Existing proof module registration/
embedding and NativeDispatchPrefixTests driver; new role requirement provider and authored JSON.
Older identities/consumers compile-only.

## 5. Errors

Missing/duplicate/unknown role mappings or invalid endpoint selections refuse before Initialize.
Actual whole-set acquisition refusal cannot become partial member mutation. Provider purity faults
remain fail-stop through original observation/transaction owners. No fabricated defaults or grants.

## 6. Compatibility

Development-only direct change. No legacy one-role selector or saved-reader shim.

## 7. Verification

ONE distinct NEW bounded case: Authored role combinations: joint inputs, readiness and saved physical response.
Record every build/run; only same-new-case observed repair/reruns. Bounded1200 normal updates with
clock/job/blocker diagnostics. Freeze executable source/proof/tests/JSON immediately after green,
then docs/read-only. No older/family/full/Oil/Tycoon/platform/stale runtime.

## 8. Risks

Role combinations can multiply native feasible searches; no arbitrary worker/world-size limit.
Full CPU/fairness/provider/hostile/purity/locking/corruption/accounting/stress gates remain open.
This is native role/source/dock admission, not global all-provider optimality or complete acceptance.

## 9. Exclusions

No Oil/Tycoon/business/platform/Draw/host gameplay, extra clock or parallel resource/worker authority.
Publication waits for full goal AND plan completion/verification.

## 10. Execution

Read actual candidate/admission/preview owners → migrate roles and shared whole-set admission →
author public requirement/JSON and thin physical driver → build/ONE exact case → freeze →
update tracker/evidence/source manifest.

## 11. Focused result

Implemented through original WorkSystem/WorkBoard/WorkIncidentPreparedness owners. Native role
alternatives and detached covered-role facts share whole-set resource/dock/cost admission.
Independent role-aware public requirement and master-listed JSON; no mod-specific scheduler branch.
Build2.301.0/save143/work-view85/incidents-view8. ONE clean build11.40s; ONE new case156/0/0
1057ms first attempt.167 older identity paths compile-only. Frozen 2026-10-08 13:28:14 UTC.
No repair/rerun/older/full runtime. Physical work/policy/cancel/emergency/private saved twins/replay
are scoped proof. Facility preference/interruption/dock/provider/CPU/full gates OPEN.
[Evidence](../../docs/migration/P9_ROLE_COMBINATIONS_VERIFICATION.md).

