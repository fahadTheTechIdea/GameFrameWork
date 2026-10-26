# P4-G28 reusable worker bindings — scoped evidence

2026-10-09, core2.364.0/save154/replay1. Full P1-P12/active goal OPEN. No publication.

## Implemented

IStatefulWorkerDefinition and IStatefulWorker pair the reusable authored three-mode worker.
WorkerDeclaration owns the original JSON roles/rate/state reader/resolver. WorkerDef and the
independent proof definition delegate the same declaration. Original Worker now consumes the
metadata contract, not Component<WorkerDef>, without changing its four-field codec1.

Original WorkerExecutionContracts validates actual contract pairing and state/role catalog aliases,
including all loaded unused definitions, and reads through the original actual-provider codec guard.
No duplicate Worker runtime, work scheduler, assignment/home/role/rate store, resource owner, clock,
state resolver or host gameplay rules. Historical native accessor call sites migrated for compilation
only; no compatibility methods retained. Original recipe authoring helper accepts a worldEdit callback,
rather than adding a second content loader.

## Compilation and exact runtime evidence

Command: dotnet build Beep.SimEngine.slnx --no-restore.
Four builds succeeded: 7.09s,4.23s,4.04s,4.55s; zero warnings/errors.

Only this NEW exact bounded case was run:
Worker bindings: independent metadata drives native physical work and pure observations.
Process deadline30000ms, termination deadline2000ms, captured output limit24000chars.

| Attempt | Passed | Failed | Aborted | Case ms | PID/exit | Observed ms |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 15 | 1 | 1 | 968 | 37364/1 | 1127 |
| 2 | 15 | 1 | 1 | 1013 | 19408/1 | 1195 |
| 3 | 270 | 1 | 0 | 2734 | 14668/1 | 2877 |
| Final | 271 | 0 | 0 | 2656 | 40364/0 | 2802 |

First proof queried a live order after normal retirement. Second queried earned history using
unqualified inspect instead of canonical activity:inspect. Both bounded waits reported
step493/state point.ready/no live orders; no process hung. Third proof diagnostic composition
dereferenced intentionally null state metadata before original validation. Diagnostics now tolerate
that invalid metadata so the engine's original pre-Initialize validator refuses it correctly.
These were proof corrections, not engine gameplay fallbacks. Final process exited0 with no output
truncation/observer error. No older/full/family/Oil/Tycoon/platform runtime was executed.

## Proven scope

Both native and independent declarations use exact authored work_rate1.5, inspect/haul roles and
the SAME native worker. Ordinary commands enable policy and post inspection; machine travels
physically, reaches authored productive state and needs actual elapsed updates to finish rated work.
The existing activity history reports completion with canonical identity; no test-side completion logic.

The same machine then physically picks up and delivers input3/tool1 for normal manufacturing.
Authored loading state is observed; original stock views report source16/cargo0/factory1 after
consumption and reusable tool return. Original processor creates exactly its ordinary result.
Private worker views are detached/read-only and denied to the other seat. Saved in-travel twins,
sole-clock pause, saved supply twins, current save identity, ordinary command replay and an untouched
interleaved session verify scoped continuation and isolation. Retained travel/rating facts stay stable.

21 malformed JSON/contract declarations refuse before Initialize.
30 read/Update callback fault combinations: three metadata getters times five faults times two paths.
Exception, hidden held-arrived codec mutation without Touch, version Touch, clock pause and identity
allocation are caught before worker facts/time publish; first observation.provider fault is retained.
Faulted sessions cannot resume normal Update or emit healthy hashes/saves. No rollback is claimed.

## Frozen inputs and open acceptance

97 selected executable inputs frozen 2026-10-09 10:28:19 UTC before the final exact run.
No executable edits after that successful run. Whole-source anchors in the completion audit are
drift checks, not correctness certificates; this is not a full solution-input snapshot.

All-role/genre/state/codec/team/dock/home/held/reservation/stress/performance and broader acceptance
remain open. Scoped provider observation is not a whole-world/CLR sandbox and does not prove arbitrary
cross-provider hidden mutation. No historical regression claim is carried forward to2.364.0.

No GameClock change, Tycoon change, money/default business rules, Draw/platform/domain implementation,
staging, commit or push. Full goal/plan and excluded catalog authority remain OPEN.
