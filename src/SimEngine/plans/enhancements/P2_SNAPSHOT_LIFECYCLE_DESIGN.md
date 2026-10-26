# P2-F4a — Snapshot lifecycle and actual update results

Implemented/focused-verified on2.289.0/save141/replay1. ONE clean build12.57s0/0;
ONE exact NEW bounded case FIRST233 passed0 failed/aborted2044ms. No repair/rerun.
Executable inputs frozen; full P2/P3/P6/P1-P12 remain open.
[Evidence](../../docs/migration/P2_SNAPSHOT_LIFECYCLE_VERIFICATION.md).

## 0. Problem statement

F4's atomic snapshot owner correctly retains safe private projections on a fault.
Its PublishSnapshot still calls Clock.Halt(Clock.TicksThisFrame), however, so a
late capture failure after RunTicks reports an earlier paced frame's count.
Healthy RunTicks also leaves that count stale. Initialize publishes callbacks
outside the existing lifecycle guard and marks Initialized before capturing;
an initial callback could attempt another lifecycle phase while initialization
is still in progress. These are actual game-loop responsibilities, not host rules.

## 1. Goals

1. Initialize and Update share the existing lifecycle guard; no nested phase.
2. The original clock reports actual completed steps for every published Update,
   including diagnostic batches, zero-step waits and terminal capture faults.
3. Fault publication retains each viewer's prior facts and cause without retries.
4. Initial/new/load snapshot failures publish empty private facts, not observer data.
5. Healthy batch/paced/mixed saved gameplay and physical work use the same loop.

## 2. Design

Rename SimGame's original _updating flag to _lifecycleActive; Initialize acquires
it around its existing publication/Time settlement in try/finally. RequireIdle
precedes idempotence, so even an idempotent Initialize cannot enter another phase.
No additional phase flag, state machine, loop or world construction authority.

PublishSnapshot takes completedSteps. Initialize passes0; UpdatePhase passes its
actual ran value. The original GameClock.Halt method becomes FinishUpdate(int,
bool): it always reports completed steps, and only terminal fault/outcome clears
unapplied pacing and slow status. Publication calls it before capture and again
only when a new capture fault occurs. Healthy batches preserve earned backlog and
interpolation; terminal batches clear it while retaining the actual completed count.
This changes the sole GameClock implementation, not its identity or saved time.

F4 remains the only projection owner: prepare all viewer facts, commit all together,
or retain each viewer's own last safe epoch with fault metadata and empty deltas.
Initialization advances no game time and never emits Updated. Retrying Initialize
outside a phase remains idempotent, including after an initial projection fault.

Extend the original external ViewObservationProof module with a transient visibility
configuration hook. Extract its pure definition/content helpers for an engine-side
pre-initialize lifecycle proof; do not duplicate module composition or gameplay.
Normal proof players still use GameFactory, scoped commands/views and game lifecycle.
Reuse the existing two explicit JSON proof documents unchanged; no inferred defaults.

## 3. API surface

No public gameplay or clock API change. Private publication requires actual steps;
one internal clock finish operation replaces Halt, with no retained overload.
Build2.289.0/save141/replay1; original persistent layouts/module schemas unchanged.
Earlier explicit build identities migrate compile-only. No compatibility reader.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| Modify | src/Beep.SimEngine/Hosting/SimGame.cs | 25 | medium |
| Modify | src/Beep.SimEngine/Hosting/GameClock.cs | 8 | medium |
| Modify | src/Beep.SimEngine/Kernel/SaveGame.cs and older identity fixtures | mechanical | low |
| Modify | tests/ExtensionProof/ViewObservationProof.cs | 15 | low |
| New | tests/Beep.SimEngine.Tests/SnapshotLifecycleTests.cs | 220 | medium |
| Modify | existing Program registration | 1 | low |
| New/Modify | stage/evidence/tracker/parent guides | scoped | low |

## 5. Error handling matrix

| Operation | Failure | Result |
|---|---|---|
| Nested lifecycle | Initialize/Update/RunTicks during a phase | explicit rejection before work/time/publication |
| Initial capture | visibility mutation/exception | first terminal fault; all viewers empty private projections |
| Late capture | paced/batch/zero-step read failure | actual steps reported; pacing cleared; retained safe facts |
| Fault continuation | repeated lifecycle update |0 steps, no callback retry, first cause preserved |
| Healthy batch | earned pacing already exists | reports current batch count without destroying backlog |

## 6. Backward compatibility

Development-only; old internal Halt/publication shapes are removed, no legacy path.

## 7. Verification

ONE new exact bounded case:
`Snapshot lifecycle: initialization guards, actual batches and private fault epochs`.
Cover healthy mixed pace/batch counts/backlog, initialized and restored idempotence,
callback reentry, initial new/load failures, first-cause/private retention, late batch
and zero-step faults, saved twins/replay and real physically timed player construction.
Bound physical phases by2000 Updates and preserve stage/time/job/fault diagnostics.

Build Beep.SimEngine.slnx after complete edits. Only observed failures authorize repair
and same-case rerun. Record every build/run. No old/family/full/platform/stale runtime;
no broad regression without user authorization. Freeze all source/proof/tests/JSON after
final green, then docs/read-only inventory. Full goal/plan remains active; no Git publication.

## 8. Risks

Completion diagnostics are pacing, not another game clock or durable epoch. Healthy
batch handling must not discard prior elapsed-time budget. Callback faults can detect,
not roll back, mutations. Provider purity/performance beyond the F4 scoped guard remains
open. Engine-side pre-initialize instrumentation must not become a host constructor API.

## 9. Out of scope

Tycoon/business/Oil/sibling/hosts/demos/platform/Draw work, arbitrary provider sandbox,
full regression, lockstep/fuzz/soak/allocation and full P1-P12 completion are not proven.

## 10. Sub-task execution order

1. Migrate the original lifecycle guard/publication/sole clock result method in place.
2. Extend the existing public proof fixture and author one new exact case; migrate identities.
3. Build/run only that case; repair observed failures only; freeze executable inputs.
4. Record evidence and update parent/tracker/source/hash/link/diff handoff.
