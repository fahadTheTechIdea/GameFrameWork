# P9-C8h — Fair physical queue admission across shared facilities

Current M71 adds selected public repeated-dock contention/allocation evidence on
unchanged native2.723/save161/replay2: four carriers, one bay per endpoint, three
rounds, native/independent buffers at minute/half-minute quanta;22,475/0/two replays.
[Current exact scope/metrics and remaining gaps](../../docs/migration/P7_CAPACITY_LOGISTICS_STRESS_VERIFICATION.md#current-m71-repeated-finite-dock-contention--2026-10-22).
No new dock/queue owner or engine fix. Initial C8h records below remain historical,
not current all-provider or cross-company stress qualification.

Status: focused implementation verified, 2026-10-06. Parent P9; prerequisite C8g.
Build2.123.0; clean build,87 checks/two exact suites (48 admission;39 physical), zero failures/aborts.
[Evidence](../../docs/migration/P9_SHARED_DOCK_QUEUE_VERIFICATION.md).

## 0. Problem

WorkBoard.ArrivedBefore compares endpoint identity and blocks every service bay if any older local
waiter can use a bay. It neither protects an arrived waiter against new admission at another facility
sharing that physical bay, nor distinguishes unrelated bay geometry. Queue ownership must stay on
existing jobs, not another global scheduler, arrival timestamp table or reservation index.

## 1. Goals and rules

Extend the existing declared dock FIFO rule (original job age, then stable job ID) across provider-owned
overlapping service geometry. Only actual arrived, assigned, currently eligible waiters compete.
Paused plans, unavailable workers, forbidden routes/policies, outstanding retry deadlines and remote
waiters do not block promotion. An existing service claim remains exclusive, including paused service.
Ordinary work-vs-haul dispatch priorities remain with WorkSystem; this does not introduce emergency
preemption of an acquired bay or claim unconditional fairness under endless ineligible work.

New/alternative service admission must yield the relevant physical bay to an earlier arrived waiter;
it may select another feasible service bay or actual waiting place. Retained service/wait claims do not
change merely for queue order. Waiting promotion uses this exact same pure candidate-specific rule.
Different company names or endpoint IDs neither partition nor reveal the physical arbitration.

## 2. Existing owners and contract surface

Factor C8g's guarded geometry comparison inside WorkBoard; actual ISpatialOverlap alone interprets
detached shapes. QueueBlocker reads live jobs, current waiting reach, existing WorkSystem.CheckJob,
request compatibility and ServiceAvailable/DockJourney. It compares only usable service geometry to
the candidate bay; unsupported comparison refuses, never guesses disjointness. No native geometry
branch, new persisted order/clock, copied eligibility formula or test/host queue logic.

Use QueueBlocker in SelectDock for replacements/new service claims and in ApproachDock promotion.
Retained claims skip queue arbitration. Existing receipt/work/motion/Sync/view owners stay unchanged.
Bump build identity only; exact current saves, no legacy reader or compatibility path.

## 3. Locked gate

Build, then only these exact two complete suites; same-name repairs permitted:

- Shared dock queues: fair admission, eligible waiters and geometry isolation
- Shared dock queues: physical promotion and saved continuation

Authored native/public geometry and normal player commands exercise newly vacant bay checkpoints,
cross-facility/company admission, multiple distinct service bays, paused/ineligible and away waiters,
pure previews, actual timed service/stock/progress/exit, private facts and saved interleaved twins.
Physical loops are bounded with diagnostics. Preceding suites compile only, never broad/full.

## 4. Boundaries and tasks

No hull/trajectory or future traffic reservations, moving facilities, alternate authored queue
disciplines, global provider rollback, fleet/deadlock recovery, replay/stress/full acceptance,
Oil/Tycoon/host/demo/platform/Draw changes. Full P9/framework remains partial.

- [x] Factor sole guarded overlap check and candidate-specific arrived arbitration.
- [x] Integrate pure admission and actual promotion without changing retained claim authority.
- [x] Build and verify the two named suites.
- [x] Align master, parent/guidance and final source-specific evidence.
