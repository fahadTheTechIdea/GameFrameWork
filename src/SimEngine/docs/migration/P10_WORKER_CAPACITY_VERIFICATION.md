# P10-B9b — Worker capacity verification

Current M160/engine2.777/save161/replay2: independent fleet JSON metadata uses the
original WorkLoadSignal runtime and WorkBoard capacity query. Physical72/0/two final-
live-hash replay verifications and native contracts117/0 are scoped witnesses.
[All attempts, corrected physical authoring, images and self-review](P10_WORKLOAD_SIGNALS_VERIFICATION.md#current-m160-contract-authored-workload-and-fleet-signals--2026-10-25).
Focused-only acceptance; quality/lot deferred; no full suite run.

## Historical B9b evidence

2026-10-06; [stage](../../plans/enhancements/P10_WORKER_CAPACITY_DESIGN.md),
[sole tracker](../../plans/enhancements/MASTER_TRACKER.md).

## Identity and sole owners

Build `beep-simengine-2.169.0`, save95, work view52, workload signal schema2. Existing gameplay
system/service/component payloads remain unchanged; workload component codec1 stays stateless.
WorkBoard/IWorkLoad owns both workload and capacity queries. No roster, cached worker counts,
assignment state, promise ledger, scheduler, codec, current-time counter or platform layer is added.

IWorkerDispatch.CheckIdleWorker reuses WorkSystem's actual worker readiness core. The existing
WorkPolicyRules owns the extracted role/enabled/emergency/standby predicate, used by actual assignment
and capacity. Existing company reserve admission remains final for ordinary acquisition. Company
reserve readiness uses the same emergency eligibility check, without recursively checking ordinary
floors. Actual assignment retains target, area, route, dock, requirements, self-service and incumbent
continuation checks; a capacity observation cannot authorize any of those.

Company or owned individual queries explicitly select one declared role or None for all authored
roles. Each worker counts once. Busy takes precedence over held; idle means unclaimed and unheld,
not necessarily stationary. Ordinary/emergency readiness are separate current individual counts,
not simultaneous team acquisition or target-specific incident preparedness. Private immutable views
and the existing JSON workload signal component call this same authority. Fleet/fleet_worker scopes
require role:null-or-ID; incompatible job/fleet metric scopes return typed unavailability.

Exactly one unchanged GameClock per session. Source SHA256:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
No wall-clock reads or independently advancing timer was introduced.

## Exact final gate

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Worker capacity: role filters, current eligibility and private fleet signals"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Worker capacity: physical dispatch, deferred controls and saved continuation"
```

- Build: zero warnings/errors; MSBuild elapsed 5.73 seconds.
- Role filters/current eligibility/private signals:117 passed, zero failed,416ms.
- Physical dispatch/deferred controls/saved continuation:68 passed, zero failed,657ms.
- Total:185 assertions in two exact suites; zero final failures or aborted suites.
- Only these two names ran, including same-name repairs. No family/full regression ran.
- 79 older build identity files mechanically migrate and compile only. Earlier workload metadata
  expectations and its master-listed signal JSON directly migrate to schema2; that suite was not rerun.
- Earlier entity/content lifecycle, dispatch, preparedness, network and protocol gates were not rerun.

## Behavioral proof

The explicitly master-listed/embedded worker-capacity JSON owns its actual entities, policies, typed
signals and rule profiles. Native multi-role workers and an unrelated public inspect-only worker
prove distinct counts and exact role filtering. Native and independent public policy providers share
the same controls/readiness path. Empty owned fleets return known zero; missing roles/entities,
non-workers, foreign ownership, unseated companies/viewers and malformed role/schema/unit content
refuse without usable partial results. Unsupported metric scopes never fabricate zero capacity.
Repeated observations preserve hashes; retained snapshots do not follow later state changes.

Normal player policy and execution commands exercise standby, emergency ineligibility, disabled and
zero-rate workers. Held-state proof is explicit kernel permission instrumentation through the actual
worker contract, not a pretend point-platform movement command or synthetic job. Scoped impure policy
reads are rejected. Company floors protect ordinary eligibility while leaving prospective emergency
eligibility separate. With a floor of three, all four idle workers are individually candidates;
actual two-node dispatch acquires only one, rechecks the floor and keeps the other three protected.

Ordinary authored parallel inspections create actual worker/job claims, physically travel to service
slots, earn work and depart before returning idle. Busy/idle/ready signals observe those same records.
The authored spare-capacity rule defers ordinary execution controls to the next game update: it does
not change the spare worker during dispatch or during the observation update. Saves retain pending
decisions and active physical jobs through existing owners. Sole-clock pause stops both; saved twins
resume hash-identically through control commit, physical workflow completion and restart. Separate
sessions remain unchanged. Cancellation releases the actual worker claims, not a capacity ledger.

## Repairs, review and remaining scope

Initial runs reached the native cases but aborted on an unselected public policy registration. The
proof now explicitly selects a public-only registration module that reuses the existing independent
policy provider; no engine fallback, behavior wrapper or duplicated policy implementation was added.
Its component identity is explicit and belongs to that selected proof module. Same-name repair reruns
preceded the final gate. No unrelated runtime suite was added to make the gate green.

Responsibility review: existing board owns queries, dispatcher owns eligibility/acquisition, policy
rules own readiness, company policy owns floors, catalog/master JSON owns content, automation owns
deferred decisions, and worker/job/resource owners keep their state. The proof registration module
contains no gameplay. All added runtime decisions are inside SimEngine.

The [source manifest](P10_WORKER_CAPACITY_SOURCE_MANIFEST.json) identifies95 scoped current sources;
all95 SHA256 fingerprints match, including the unchanged GameClock. All7 relative links in this
stage/evidence pair resolve. Scoped tracked `git diff --check` passes; Git reports only existing
LF-to-CRLF normalization warnings. Source ownership checks find one WorkBoard capacity implementation,
one dispatcher readiness implementation and one GameClock construction in World. World.Tick and host
views project that clock's StepIndex; they do not store or independently advance time. No older
2.168.0 build literal remains in the inspected engine/test C# trees. These are scoped source checks,
not a full-repository cleanliness, whole-provider purity or runtime-regression claim.
Global provider mutation/rollback, large-fleet performance/allocation, target-specific multi-resource
teams, future trip/fuel/traffic guarantees, arbitrary workload filters, replay/fuzz/soak and full
P9/P10/framework acceptance remain open. No Oil/Tycoon/host/demo/Unity/Godot/Draw changes or full test run.
