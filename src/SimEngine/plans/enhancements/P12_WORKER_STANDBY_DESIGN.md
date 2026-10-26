# P12-D7 — Emergency-only worker standby

Status: focused verified, 2026-10-06; broader P9/P12 remain partial. Prerequisites: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md),
[P12](P12_INCIDENTS_ENVIRONMENT_DESIGN.md), [D6d](P12_EDITABLE_FACILITY_POLICIES_DESIGN.md).

## 0. Problem statement

Standard/WorkPolicies.cs has enabled/roles/area/emergency eligibility but no way to keep an enabled
real machine available for emergency work. Disabling it blocks emergency response too. WorkSystem and
LogisticsService already share CheckAssignment and actual bound emergency classification; extend them.

## 1. Goals

1. One WorkPolicySettings owner carries explicit All/EmergencyOnly assignment mode.
2. Ordinary new work/hauling refuse standby before claims/spending; actual emergency response/supply can use it.
3. Already assigned ordinary work/loaded hauling finish with their actual job/worker relations intact.
4. Disabled/role/area/route/fuel/emergency eligibility checks still apply; priority/alerts grant no emergency identity.
5. Existing command/private preview/codec/state source save/hash the same mode; no copied fleet roster.
6. Public policy/worker providers use existing contracts, exact setting acknowledgement and shared refresh.
7. Exactly one GameClock, WorkBoard and WorkSystem; no new reservation store or scheduler.

## 2. Design

Add WorkAssignmentMode All/EmergencyOnly to WorkPolicySettings's required constructor/strict JSON
assignment_mode/shared Sync/Matches. Remove the old constructor; migrate explicit authored profiles,
callers and state bindings. New WorkPolicyCodes.Standby is a wait, not job failure.

WorkPolicyRules.CheckAssignment accepts the existing ownedJob coordinate and recognizes continuation
only from the actual live WorkBoard job plus both Job.Worker/IWorker.Job links, owner and role. New
ordinary admission on an EmergencyOnly machine refuses. Existing bound response/demand classification
supplies emergency authority; neither numeric priority nor company alert fabricates it. All safety checks
remain before/after this gate. Existing pause/retry/cancellation remains authoritative.

Add RefreshPolicy to existing IWorkerDispatch; WorkSystem already implements it. Native/public policy
owners request normal refresh through that contract. Shared CheckConfigure/Configure wrap existing
provider admission/commit with scoped purity and exact immutable settings acknowledgement, stable owner,
clock and actual job relation. Commands/private previews share them; ignored standby edits fault.

Author required states.standby. WorkPolicyRules resolves it only for enabled EmergencyOnly idle/unheld
workers; native/public state providers reuse that same function. Entity.Update commits state through the
existing state resolver after normal gameplay. No entity-private time/state machine duplicates are added.
Public ProofWorkPolicy owns its own settings/Sync and implements contracts without wrapping native policy.

## 3. API surface

WorkAssignmentMode; WorkPolicySettings(..., emergencyEligible, assignmentMode); shared policy control/
state helpers; IWorkerDispatch.RefreshPolicy. Existing ConfigureWorkPolicyCommand/private policy views.
Required JSON assignment_mode:all/emergency_only and states.standby. No new instant response command.

## 4. Files

WorkPolicies/WorkContracts/Work/WorkModule/WorkPolicyCommands/WorkView/SaveGame. Strict content/caller
migration; public proof registration/source; authored standby fixture; two named WorkerStandbyTests.
Stage/master/parents/README/help/evidence align.

## 5. Errors

Missing/unknown mode or standby state fails content. Invalid enum fails construction/decode.
Ordinary standby admission returns work.policy-standby. Eligibility/role/area/current route blockers
retain existing reasons. Unknown/non-live/one-sided job relations never exempt a new assignment.
Dishonest settings/check callbacks fault; no silent field substitution or partial checkpoint.

## 6. Compatibility

Clean constructor/content/state protocol replacement; no old overload/reader/default mode shim.

## 7. Verification

Clean build; only these two exact suites, same-name repair reruns allowed:

- Worker standby: authored settings, private controls and shared admission
- Worker standby: physical response, emergency supply and retained work

Native/public policies and workers, strict authoring, mode codec/private/pure/frozen facts, exact control/
pending/corrupt saves, emergency role/area/routes, ordinary max priority refusal, real response/supply
travel/stock/tools/work/restart, active ordinary/loaded haul continuation, cancellation/release, saved
twins/session/sole-clock pause and entity state settlement. Bounded physical waits with diagnostics.
Other suites compile only; no third/family/full runtime gate.

Final build2.159.0/save86/WorkBoard27 unchanged/WorkSystem21/WorkView50/native work-policy codec3/
public proof codec1. Zero build warnings/errors; 168 contracts + 326 physical = 494 checks, zero failures/
aborts. Four native/public policy × worker combinations prove dry response, ordinary continuation,
loaded hauling, cancellation and emergency preemption/reacquisition. Supplied response uses native
multi-role workers with both policy providers. Unknown/truncated settings and dishonest read/check/
mode/link controls reject; saved pending edits/twins/private facts/sole-clock pause pass.
[Current evidence and source fingerprints](../../docs/migration/P12_WORKER_STANDBY_VERIFICATION.md).

## 8. Risks

Standby preserves availability, not a claim: emergency demand still competes deterministically for
actual workers and materials. Incumbent continuation is not permission to reassign an arbitrary job.
Cancelled/preempted ordinary work must reacquire and then respects current standby. EmergencyOnly does
not suppress manual movement or home return, and an already-bound ordinary job keeps normal safety gates.

## 9. Out of scope

Automatic company-wide minimum-ready counts/role quotas, maintenance exemptions, automatic spare stock
policies, fuel/traffic/congestion budgets, incident science, broader providers/global callback rollback,
replay/fuzz/soak/stress/performance/full framework. No Oil/Tycoon/host/demo/platform/Draw/extra clock.

## 10. Execution order

- [x] Inspect actual policy/dispatch/emergency/clock owners and define one standby authority.
- [x] Implement settings/admission/control/state contracts and explicit migration.
- [x] Public proof and authored bounded contracts/physical scenarios.
- [x] Clean build/two exact gates, ownership review and current tracker/evidence.
