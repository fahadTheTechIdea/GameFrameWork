# P12-D6 — Explicit machine bindings in ordinary activity plans

Current authoring supersedes this historical worker-only payload: B4i uses the SAME selection
owner with explicit role:null or a declared role in every selected worker entry. Save144/board44;
no worker-only legacy reader. RequestedRole and actual SelectedRole are separate immutable facts.
[Role-aware facility intent](P9_ROLE_FACILITY_INTENT_DESIGN.md). Prior tests below are historical,
not rerun or broad current-build proof.

Status: focused verified, 2026-10-06; build2.155.0/save82/board26/work17/view46.
163 checks/two exact suites (65 contracts;98 physical), zero failures/aborts, clean build.
[Evidence](../../docs/migration/P12_SELECTED_SERVICE_WORKERS_VERIFICATION.md).
Parent: [P12](P12_INCIDENTS_ENVIRONMENT_DESIGN.md),
[P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md); prerequisite: [D5](P12_WORKER_SERVICE_EFFECTS_DESIGN.md).

## 0. Problem statement

ActivityDef.ReadIntentTarget accepts only target. WorkSystem.CopyCandidates chooses any compatible
worker; repairing an arbitrary damaged finisher is not enough when a player needs a specific machine
serviced at a chosen facility. WorkBoard already owns intent, nodes, acquisition and completion.
Extend these owners; do not introduce a fleet scheduler or a separate service model.

## 1. Goals

1. Ordinary activity plan JSON can name node/worker pairs alongside its target; omitted bindings
   mean the existing deliberate any-compatible-worker policy, never a substitute for a bound machine.
2. One immutable IActivityWorkerSelection contract exposes canonical node bindings; one implementation
   owns parsing, graph validation, identity, admission and embedded order Sync.
3. Bound workers must be actual owned compatible machines on submission. Busy/held/policy-disabled
   machines may have persistent intent; existing eligibility determines when execution can begin.
4. Candidate offers, acquisition, live job checks, effects and restore enforce exact binding. Missing,
   foreign, incompatible or busy selected machines never cause another machine to execute the node.
5. Bindings are selection intent, not acquired reservations. Job.Worker and IWorker.Job remain the only
   acquisition authority; CompletedBy remains the actual earned completion receipt.
6. Private immutable node views distinguish requested, assigned and completing machines. Save/load,
   pending commands, cancellation, resource accounting, parallel nodes and native/public machines
   retain the same intent and physical timing through bounded focused gates.

## 2. Design

New Standard/ActivityWorkerSelection.cs defines immutable ActivityWorkerBinding(node,worker),
IActivityWorkerSelection, and ActivityWorkerSelection. Canonical ordinal node order makes selection
identity and its embedded codec independent of binding array order. The existing plan retains original
frozen argument arrays; equivalent reordered commands need not have identical whole-session hashes.
Activity arguments accept target plus optional nonempty workers:

```json
{"target":1,"workers":[{"node":"service","worker":4}]}
```

Unknown fields, duplicate nodes, nonpositive identities, missing/timer/zero-time nodes reject. Unbound
nodes deliberately retain automatic selection. No authored default worker, affinity or fallback exists.
ActivityDef reuses the parser for identity/admission/activation and stores one immutable selection on
Order through WorkBoard.Post. The order codec embeds bindings; PlanEntry.Arguments retains original
immutable player intent as before, with execution identity parity checked by the existing plan board.

WorkSystem.CopyCandidates restricts actual eligible machines before role/policy/route/resource scoring.
Unavailable bound identities yield typed diagnostics with no foreign operational details. Board.Assign
checks the exact node constraint again before acquiring resources or the worker. CheckJob enforces
the same constraint. Existing physical departure/worker effects remain untouched; checkpoint validation
rejects assignments/completion receipts that differ from a requested worker. Selected entities may
disappear after admission: retained intent waits/cancels through normal owners, never substitutes.

## 3. API surface

IActivityWorkerSelection.Bindings/WorkerFor(node); ActivityWorkerBinding.Node/Worker;
Order.WorkerSelection; ActivityNodeView.RequestedWorker. Existing SubmitPlanCommand, private PreviewPlan,
node/candidate views and plan controls remain the player API. Direct system orders remain deliberately
unbound unless an explicit selection is supplied. No new player repair command.

## 4. Files

New Standard/ActivityWorkerSelection.cs; modify Standard/Work.cs, WorkModule.cs, Views/WorkView.cs,
Kernel/SaveGame.cs; focused SelectedWorkerTests, explicit embedded activity JSON and Program registration.
Stage/master/parents/AGENTS/README/help and migration evidence align with final source identities.

## 5. Errors

Malformed binding arguments return InvalidArgument through existing plan preview/command handling.
Unknown/foreign/nonworker/wrong-role selections return typed admission refusal. Busy/held/disabled/
unreachable workers use ordinary waits; there is no remote repair or replacement executor. Forged
selection graph/identity, assignment or earned completion mismatch rejects checkpoints or acquisition.

## 6. Compatibility

Clean order codec/build/save replacement, no previous reader, argument translator or compatibility
facade. The unbound policy and explicit selection are two valid current policies, not legacy paths.

## 7. Verification

Clean build, then only these two complete exact names; same-name repairs permitted:

- Selected workers: intent contracts, admission and checkpoint ownership
- Selected workers: physical service, parallel nodes and saved continuation

Prove strict/canonical parsing, private preview/command parity, pending save, exact worker with nearer
alternatives, unavailable/removed/foreign worker no fallback, parallel distinct/same worker arbitration,
bay work/materials/tools/departure/receipt, pause/cancel and interleaved twins/sole-clock pause.
Other suites compile only. No full/family/third gate without explicit user authorization.

## 8. Risks

Selection is not a reservation: multiple intents may select one machine, but only the existing actual
job claim may execute. Same-worker parallel nodes must serialize without deadlock. Removed selected
IDs remain stable historical intent; future IDs and actual wrong assignments cannot checkpoint.

## 9. Out of scope

Automatic service subject/facility search, editable fleet cadence orchestration and provenance,
repair teams/contributor accounting, traffic/fuel/preparedness/science, arbitrary global purity,
replay/stress/full P9/P12/framework. These remain required later work, not completion claims.
No OilFieldDays/Tycoon/host/demo/platform/Draw or second clock.

## 10. Execution order

- [x] Add sole immutable selection parser/contract/codec and activity intent integration.
- [x] Enforce selection in existing candidates/acquisition/live checks/restore/private views.
- [x] Add explicit JSON plus native/public physical service and DAG proof.
- [x] Clean build, two exact gates, evidence/docs/tracker; keep full orchestration open.
