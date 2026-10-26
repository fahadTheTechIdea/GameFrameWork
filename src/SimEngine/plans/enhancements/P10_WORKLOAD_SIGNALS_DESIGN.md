# P10-B9 — Authoritative workload and physical dock-queue signals

## Current M160 — contract-authored workload source (2026-10-25)

Implemented on engine2.777/save161/replay2. IWorkLoadSignalDefinition declares scope,
fleet selection and role filter; WorkLoadSignalDeclaration is the sole strict JSON reader
and metric declaration. Native and independent registered metadata use that declaration
and the original stateless WorkLoadSignal runtime, no concrete-definition runtime base.
WorkLoadSignalBindingContracts admits all loaded runtime/metadata pairs and validates
actual frozen owner/codec, signal schema2, named worker scope and actual role selection.
Live reads capture those settings under original provider/entity observation and invoke
the same IWorkLoad authority. WorkBoard still owns every job, queue, assignment and
worker-capacity calculation; no cached counts, extra scheduler, material store or clock.

Focused acceptance: independent workload physical queues/controls65, independent fleet
physical controls72, native workload contracts49 and native fleet contracts117:303/0.
Three actual final-live-hash replays (one workload, two fleet policy contexts). These
are selected affected cases, not full P10 qualification. Existing physical fixtures
needed feasible one-dimensional parking/waiting/exit geometry and explicit startup
placements; ordinary player pause/resume forms a queue without changing handling rates,
movement rules or 400-update stage bounds. No gameplay decision moved to a driver.
[All attempts/current images/self-review](../../docs/migration/P10_WORKLOAD_SIGNALS_VERIFICATION.md#current-m160-contract-authored-workload-and-fleet-signals--2026-10-25).

Only this concrete P10.022 metadata coupling is completed. The broader finite control/
provider matrix remains open; quality/lot stays deferred and acceptance is focused-only.
No Oil/host/demo/platform/business edit, full/profile/family/soak run or publication.

## Historical B9 implementation

Status: focused verified, 2026-10-06; build2.153.0/save80/work view44/new signal codec1/schema1.
Existing WorkBoard24/WorkSystem15/clock and all other owner payloads unchanged.
**107 checks/two exact suites** (47 contracts;60 physical), zero failures/aborts, clean build.
[Evidence](../../docs/migration/P10_WORKLOAD_SIGNALS_VERIFICATION.md). Full P9/P10/framework remain partial.
Parent: [P10](P10_NETWORKS_AUTOMATION_DESIGN.md);
physical ownership: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).

Follow-on [P10-B9b worker capacity](P10_WORKER_CAPACITY_DESIGN.md) extends this same query/signal
owner with explicit fleet/fleet_worker scopes and declared-role filters. Current build2.169.0/save95,
work view52 and signal semantic schema2 supersede the historical identities above; no old-schema
reader or parallel source remains. B9b passes185 assertions/two exact suites; this B9 gate was not rerun.

## 0. Problem statement

WorkBoard owns real jobs and WorkDocks owns their actual slot claims, but authored automation cannot
observe open job backlog or physical waiting queues. Hosts must not calculate those rules themselves.
WorkView currently exposes individual jobs; a second counting/scheduling store would be incorrect.

## 1. Goals

1. WorkBoard implements one read-only IWorkLoad contract, deriving immutable facts from its live jobs.
2. Explicit company, endpoint and assigned-worker scopes return truthful live/work/haul/open/assigned/
   paused counts, waiting claims and physically arrived waiting counts. Terminal jobs count nowhere.
3. Endpoint queue counts refer only to the current physical dock, not a future haul destination.
4. Master-listed JSON selects a named stateless source/scope and typed count signals; existing rules
   consume them and defer ordinary actions to the next game update. No clock, queue or scheduler added.
5. WorkView and signal providers use the same query, with company-private snapshots and exact bindings.

## 2. Design

Standard/WorkLoad.cs adds WorkLoadScope, immutable WorkLoadSnapshot and IWorkLoad.ReadWorkLoad.
WorkBoard computes facts directly from its own jobs. Company must be a real seat; endpoint/worker must
be live and owned by that company. A haul touches each participating endpoint once; waiting claims
and arrived counts use the current loading/delivery endpoint only. Worker scope excludes open jobs
that have no assigned worker. Empty workload is a real zero, not unavailable.

WorkDocks extends its sole waiting-slot query for actual arrival checks through existing provider
geometry, rather than reproducing slot/reach logic. Waiting claims may include approaching or paused
vehicles; arrived waiting is physical presence, not eligibility/fairness or a guaranteed service ETA.
WorkModule aliases IWorkLoad to its actual board. WorkView exposes a private read through this alias.

Optional WorkSignalsModule depends on sim.work and sim.operations, registers work_load_signal and
adds no system, view or stateful service. WorkLoadSignalDef strictly reads id/scope; native source
observes the board through IWorkLoad and emits typed exact integer counts in unit.count. Metadata-only
components retain the ordinary empty component codec. Existing signal validation/discovery, private
views, gates, rule hysteresis/cooldown and deferred normal commands remain authoritative.

## 3. API surface

- IWorkLoad.ReadWorkLoad(PlayerId company, WorkLoadScope scope, EntityId subject, out WorkLoadSnapshot).
- WorkView.ReadWorkLoad with the same explicit query and private ownership guard.
- WorkBoard.ReadDockWaiting(Job, out waiting, out arrived) uses its existing waiting/reach owner.
- work_load_signal JSON: explicit id and scope company/endpoint/worker; no inferred scope.
- Number metrics: work.live-jobs, work.activity-jobs, work.haul-jobs, work.open-jobs,
  work.assigned-jobs, work.paused-jobs, work.dock-waiting, work.dock-arrived-waiting.

## 4. Files

Add Standard/WorkLoad.cs, Standard/WorkSignals.cs, WorkSignalsModule.cs;
revise WorkDocks.cs, WorkModule.cs, WorkView.cs, SaveGame.cs;
add WorkLoadSignalTests and explicit master-listed test JSON; register only two new exact suites.
Existing public mod workers/docks and normal controls prove provider-neutral observation/execution.

## 5. Errors

Unknown scope/implicit subject, missing/foreign entities, unseated company and non-worker worker
scope refuse without partial snapshots. Malformed JSON/units/metrics/schema fail composition.
Signal sources must belong to the actual world/entity/content; query-service alias loss faults,
not fabricated zero. Missing waiting participants and unsupported arrival geometry return typed
refusals, never fabricated empty counts. Existing geometry pure checks guard physical observations.

## 6. Compatibility

No aliases, older reader or parallel cache. Current build identity migrates; existing owner payloads
and the sole GameClock remain unchanged. No legacy support.

## 7. Verification

Clean build then exactly two complete named suites, same-name repairs allowed:

- Workload signals: authored scopes, private facts and query ownership
- Workload signals: physical queues, deferred controls and saved continuation

Prove explicit authoring/units/schemas, native/public jobs, company/endpoint/worker scopes, pure frozen
private views, current-leg queue accounting, actual arrival/service/exit, cancellation/pause, zero after
completion, no same-update feedback, pending-decision saves, isolated saved twins and sole-clock pause.
Physical waits have a bounded diagnostic timeout. No third/family/full test run.

## 8. Risks

Waiting reservations are not arrived FIFO eligibility; expose both honestly. Missing participants
may exist before normal cleanup, so a missing subject refuses rather than silently swapping identity.
No physical work is earned by signal reads or rule previews.

## 9. Out of scope

Idle fleet capacity/travel forecasts, service subjects/cadence orchestration, traffic/congestion,
arbitrary workload filters, whole-session callback purity, broad replay/stress/fuzz/full P9/P10/framework.
No Oil/Tycoon/host/demo/platform/Draw logic.

## 10. Execution order

- [x] Add sole query, immutable scopes/facts and reuse actual dock arrival owner.
- [x] Add optional JSON signal bridge and private engine view.
- [x] Build and run only the two named workload gates.
- [x] Record evidence, update tracker/guidance, keep broad acceptance open.
