# P12-A5 reusable incident bindings — scoped verification

Date: 2026-10-09. [Stage](../../plans/enhancements/P12_INCIDENT_BINDINGS_DESIGN.md).
Core `beep-simengine-2.383.0`, save154/replay1. Full goal and P1–P12 plan remain OPEN.

## Implementation boundary

`IIncidentEpisodeDefinition` and `IIncidentSignalDefinition` bind independent JSON component
definitions to the original `Incident` and `IncidentSignal` runtimes. The sole declarations own
strict JSON metadata parsing/resolution. Existing workflow/completion contracts, native episode
codec2, stateless signal codec1, WorkBoard, physical worker/movement/resource transactions, entity
state Update and GameClock remain their only owners. No native inheritance requirement, copied
episode/history/phase ledger, compatibility path, host gameplay, platform or business changes.

The existing response system validates every loaded binding before Initialize, including unused
definitions. It rejects incorrect runtime pairs, fabricated aliases, missing state/source/phase,
wrong registered metrics and pure callback violations. Its recursion guard is transient, not a
second state authority. Lifecycle and response metadata observations use existing scoped guards.

## Actual build and runtime attempts

Four `dotnet build Beep.SimEngine.slnx --no-restore` invocations completed with exit0, zero warnings,
zero errors:14.19s,9.25s,4.41s,4.40s. The first build's live session32225 was observed terminal.

Only this ONE NEW exact case was invoked:

`Incident bindings: native and independent JSON share physical episodes, signals and saved history`

| Attempt | Passed | Failed | Aborted | Case ms | PID | Exit | Observed ms |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1 | 43 | 2 | 1 | 726 | 40208 | 1 | 875 |
| 2 | 44 | 1 | 1 | 734 | 32856 | 1 | 888 |
| 3 | 44 | 1 | 1 | 721 | 11980 | 1 | 867 |
| FINAL | 121 | 0 | 0 | 969 | 42676 | 0 | 1104 |

First attempt: proof expected state-definition ID `blocked` rather than its authored state code
`point.blocked`; independent proof JSON also lacked its required `fault` string. Second: proof used
`JsonValue.Of(null)` (invalid string encoding). Third: explicit JSON null was not accepted by the
strict string reader. Final proof authors explicit string `none`. These were proof authoring repairs,
not engine defaults or a relaxed parser. Between the first and second attempt, code review also
added original codec/registered-metric admission checks and the unused malformed metric proof.

All four child processes were observed Exited. Limits:30,000ms execution,5,000ms owned termination,
30,000 output characters. No timeout, truncation or observer error. No earlier incident/environment,
family/full/Oil/Tycoon/platform runtime was executed. Compilation is not runtime proof for old cases.

## What this case proves

Both native and independent declarations drive identical real episode/phase timing. Timed detection
completes through the original DAG; the authored detection-phase signal reads that actual progress.
Automation earns dwell and applies the ordinary emergency policy on its deferred step. A machine
physically travels4→20, consumes eight actual supply resources and earns partial repair over time.
Cancellation resumes the same original episode/order with retained progress and no repeated material
spend. Pausing the sole clock changes neither hash nor work. Completion updates original episode
history, signals and entity state; a second episode consumes another eight. Stock20→12→4.

Raised, partial, cancelled/resumed and resolved save/load checkpoints continue with identical twin
hashes. Retained phase facts are immutable/detached, foreign lifecycle/signal reads are refused,
normal command history replays and interleaved fresh sessions retain separate state. Seventeen
malformed unused definitions fail before Initialize, including throw/pause/recursive validation.

## Evidence limitations and inventory

Selected294 executable source/proof/test/JSON inputs were hashed AFTER the final green run at
2026-10-09 16:25:38 UTC and recorded in the completion audit. This is a post-run snapshot, not a
pre-run freeze or proof of complete transitive source closure. No executable edits followed it.
Metadata hash/link/table checks are not gameplay regression proof.

This does not prove all saved phases, every provider/live metadata/private-codec mutation, rollback,
arbitrary extension safety, all spatial geometries, propagation/environmental science, throughput,
lockstep, fuzz/soak/stress or full acceptance. G19's Tycoon type-reference authority is still unanswered.
Original gates and checkbox text remain unchanged. No staging, commit or push until full verified finish.
