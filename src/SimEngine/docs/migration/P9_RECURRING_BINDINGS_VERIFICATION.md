# P9-B13 reusable recurring activity bindings — scoped verification

Date: 2026-10-09. [Stage](../../plans/enhancements/P9_RECURRING_BINDINGS_DESIGN.md).
Core `beep-simengine-2.385.0`, save154/replay1. Full goal and original P1–P12 plan remain OPEN.

## Implementation boundary

The original RecurringActivitySource now selects IRecurringActivitySourceDefinition from its actual
named content binding. Native and independently authored JSON definitions share the sole strict
RecurringActivityDeclaration and original settings/binding/condition readers. No concrete metadata
inheritance, duplicate source runtime, copied plan/execution/history store or compatibility path.
Original schedule/template codec8, WorkBoard, physical DAG/work/resource/motion and GameClock owners
remain authoritative. Existing commands/previews/private immutable views remain the player's API.

Existing WorkBoard composition validates every reusable binding, including unused definitions, with
actual immutable owner/runtime/codec aliases, conditions and original plan/template/policy/target rules.
Callbacks are observed; recursive composition refuses through a transient guard. Live schedule and
native request/trigger metadata use existing scoped provider/whole-entity codec and clock observations.
Scoped faults stop the session; no healthy view, checkpoint/hash, fallback or compensation is fabricated.
The proof module adds content and negative observation probes; its installer Update has no gameplay.

## Actual build and runtime attempts

Three `dotnet build Beep.SimEngine.slnx --no-restore` invocations:

| Build | Exit | Seconds | Warnings | Errors |
|---|---:|---:|---:|---:|
| 1 | 1 | 7.44 | 0 | 2 |
| 2 | 0 | 8.14 | 0 | 0 |
| FINAL | 0 | 4.89 | 0 | 0 |

First build: the negative probe installer omitted abstract Update and StateSchemaVersion members.
Those proof-only declarations were added; engine compilation had succeeded.

Only this ONE NEW exact case ran:

`Recurring bindings: direct JSON schedules share physical work, stock gates and saved history`

| Attempt | Passed | Failed | Aborted | Case ms | PID | Exit | Observed ms |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1 | 81 | 3 | 1 | 1072 | 27372 | 1 | 1232 |
| FINAL | 142 | 0 | 0 | 1145 | 36052 | 0 | 1292 |

First attempt: two assertions expected the source's completion acknowledgment before the existing
coordinator's next pass. The test now waits for that normal pass; earned plan completion/due time
were already correct. A separate blocked-gate proof raised minimum30 but left maximum20, which the
strict parser correctly rejected. Authoring now supplies a valid explicit30–40 band. Engine behavior,
clock, parser and condition semantics were unchanged between runtime attempts.

Both children were observed Exited. Limits:30,000ms execution,5,000ms owned termination and30,000
output characters. No timeout, truncation or observer error. No historical recurring/incident/
propagation/environment/family/full/Oil/Tycoon/platform runtime ran. Old cases compiled only.

## What this case proves

Native and direct metadata earn equal actual plan chronology through the original runtime. Disabled
intent creates no plan or stock change; ordinary commands enable the source and one real machine.
That machine travels4→25 before consuming the facility's actual eight supply units. A paused original
plan retains earned progress and never reposts; the sole clock pause freezes work and schedule/hash.
Saved twins resume the same plan, earn completion, then repeat six authored game minutes after that
completion—not six minutes after submission. Two physical cycles consume stock20→12→4; the actual
stock signal then blocks a third plan, with no automatic supply, free resource or invented completion.

Disable affects future repeats only. Cancellation during real travel consumes no material and earns
no completed-service acknowledgment. Saved disabled/cancelled state waits; re-enable uses the existing
retry/history owner, physically completes a new cycle and spends eight once. An authored unmet stock
band blocks initial posting and stays blocked through saved continuation. Foreign views publish no
source intent/settings. Retained schedule facts are detached; replay, settled saves and interleaved
fresh-session ownership are covered.

Nineteen unused malformed authoring/runtime/metadata variants reject before Initialize, including
throw/pause/recursive validation. Four live conditions-getter faults cover throw, pause, Touch and
private codec mutation without Touch. Original observation/provider or recurring metadata first-fault
phases are checked; live views/save/hash refuse after cutoff. No healthy post-fault state is claimed.

## Evidence limitations and inventory

Selected314 executable source/proof/test/JSON inputs were hashed AFTER final green at
2026-10-09 17:08:51 UTC and recorded in the completion audit. This is a post-run snapshot, not
a pre-run freeze or complete transitive source closure. No executable edits followed.
Hash/link/table checks are metadata integrity, not gameplay regression.

Not proven: full recurring templates/library revisions/facility targeting/maintenance policies/native
tile breadth, all source getters/providers/private CLR mutations, global rollback, allocation/throughput,
stress, lockstep, fuzz/soak or full acceptance. Original221 compound gate text/checkbox states remain
unchanged. G19 Tycoon type-reference authority is unanswered. No Tycoon/Oil/host/platform/GameClock
changes or staging/commit/push before full verified finish.
