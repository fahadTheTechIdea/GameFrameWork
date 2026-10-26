# P10-B9b — Current worker capacity and fleet signals

Current M160/engine2.777: fleet/fleet_worker JSON now shares IWorkLoadSignalDefinition,
WorkLoadSignalDeclaration and the original signal runtime with independent metadata.
WorkBoard retains actual capacity/eligibility/reserve semantics; no fleet counter state
or dispatch logic was added to proofs. Independent physical72/0 (two policy contexts,
two actual final-live-hash replay checks) and native contracts117/0 pass. The broader
workload slice has its own65/0 and49/0 checks, not an every-module pass.
[Current design](P10_WORKLOAD_SIGNALS_DESIGN.md#current-m160--contract-authored-workload-source-2026-10-25),
[scope/attempts/self-review](../../docs/migration/P10_WORKLOAD_SIGNALS_VERIFICATION.md#current-m160-contract-authored-workload-and-fleet-signals--2026-10-25).
Focused-only acceptance; quality/lot deferred; broader compound gates remain open.

## Historical B9b implementation

Status: focused-verified, 2026-10-06; broader P9/P10/framework remains partial.
Parent [P10](P10_NETWORKS_AUTOMATION_DESIGN.md), [workload](P10_WORKLOAD_SIGNALS_DESIGN.md),
[physical dispatch](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
[Evidence](../../docs/migration/P10_WORKER_CAPACITY_VERIFICATION.md).

Follow-on [P9-B10 operating inputs](P9_WORKER_OPERATING_INPUTS_DESIGN.md) uses2.170.0/save95/work
view53 and leaves this workload schema2/capacity protocol unchanged. This185-assertion gate is
preceding evidence, not rerun in B10; B10's279 assertions prove its own phase-input scope only.

## Problem and goals

WorkBoard exposes live backlog and physical queues through IWorkLoad, but hosts would have to
invent machine-capacity calculations for fleet automation. The same worker can have multiple roles;
independent role sums overcount it. Idle is not readiness: held control, zero execution rate,
disabled policies, standby and company reserve floors affect current ordinary availability.

1. Extend the existing WorkBoard/IWorkLoad query owner with detached company/individual capacity,
   optionally filtered by one explicitly declared role. Count each actual worker once.
2. Partition actual workers into busy, held and idle; separately report idle ordinary/emergency
   eligibility using the same dispatch readiness/policy/reserve rules, not another scheduler.
3. Extend the existing workload signal component with explicit fleet/fleet_worker scopes and
   required role:null-or-ID. Master JSON binds typed count signals and normal deferred controls.
4. Private views, authoring errors, preview purity, native/public workers, actual physical work,
   pause/cancellation, saved continuation and session isolation agree with the same engine facts.

## Design and API

IWorkerDispatch.CheckIdleWorker(World, Entity, PlayerId, WorkerRoleId, bool emergency) is a pure
target-independent eligibility projection. It shares WorkSystem's actual CheckWorker core, the
extracted WorkPolicyRules readiness check and the existing company reserve admission. It does not
authorize work or claim target/route/dock/material/fuel feasibility. Existing assignment retains
continuation/self-service and destination checks. Company reserve readiness uses this same check.

IWorkLoad.ReadWorkerCapacity(PlayerId, WorkerRoleId, EntityId, out WorkerCapacitySnapshot) remains
implemented by the sole WorkBoard. None subject means whole company; otherwise actual owned worker.
None role means all authored roles, never summing role counts. Busy takes precedence over held,
matching real admission. No roster/cache, assignments, claims, stock, progress, Sync or clock added.
WorkView provides the same private immutable query. Retained snapshots never follow later changes.

WorkLoadSignalDef retains existing job scopes and adds fleet/fleet_worker with mandatory role.
All signal metadata migrates directly to schema2: existing eight job metrics plus total/busy/held/
idle/ordinary-ready/emergency-ready worker metrics. Unsupported scope/metric combinations return
typed unavailability, not fabricated zero. Existing component empty codec remains unchanged.

## Files, errors and compatibility

Standard/WorkContracts, Work, WorkPolicies, CompanyWorkerReserves, WorkLoad, WorkSignals;
Views/WorkView, WorkModule view declaration, SaveGame build identity; master-listed proof JSON,
two focused suites and runner registration; stage/evidence/master/parent/README/help/AGENTS.
Unknown role, foreign/missing/non-worker subject or unseated company refuse before partial results.
Missing/malformed role and wrong signal schema/unit fail composition. Scoped impure reads fault.
No compatibility alias/old schema loader. Gameplay payloads and sole GameClock remain unchanged.

## Verification

Clean solution build, then only these two exact names (same-name repair reruns permitted):

- Worker capacity: role filters, current eligibility and private fleet signals
- Worker capacity: physical dispatch, deferred controls and saved continuation

Include native/public workers, multi-role identity counts, held/disabled/zero-rate/standby/reserve
rules, unchanged observations, invalid bindings, real busy/idle transitions, timed work and physical
dock departure, deferred ordinary controls, pause and saved twins. Bounded waits with fault diagnostics.
Older identities/schema expectations compile only; no third/family/full gate.

## Risks and non-goals

Ready counts describe individual present eligibility, not simultaneous floor-preserving acquisition,
response-frontier matching or a future-trip guarantee. Target-specific preparedness keeps its existing
owner. Large-fleet allocation/performance, global provider purity/replay/stress/full acceptance remain
open. No host/demo/Oil/Tycoon/platform/Draw, idle fuel formula or independent time source.

## Execution

- [x] Share existing worker readiness and reserve checks without changing actual dispatch authority.
- [x] Extend sole workload query/private view/JSON signals and author explicit proof content.
- [x] Build and run exactly two declared suites:117 contracts +68 physical assertions, zero failures.
- [x] Record scoped evidence and update sole tracker; broader roadmap remains incomplete.

Build2.169.0/save95/work view52/workload signal schema2; component codec1/gameplay payloads unchanged.
Clean build;185 assertions/two exact suites, zero final failures/aborts. Native/public workers and
policy providers, private role-filtered distinct counts, real dispatch/floor rechecks, held instrumentation,
unavailability/standby, pending deferred controls/save/pause/twins/isolation/cancellation pass.
79 older build identity files and preceding workload schema expectations compile only, not rerun.
