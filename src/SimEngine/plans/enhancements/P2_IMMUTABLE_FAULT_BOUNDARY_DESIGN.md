# P2-F1: Immutable first-failure boundary

## 0. Problem statement

`Core/Fault.cs` exposed writable fields through `SimGame.Fault`. `World.Step` also replaced a fault
already recorded by entity initialization or replacement with the outer phase. A provider swallowing
an initialization exception could continue commands and complete game time despite the terminal latch.

## 1. Goals

1. The existing fault record is immutable and engine-created, with no duplicate DTO or compatibility API.
2. World alone latches the first failing owner, cause, stack and sole-clock coordinate.
3. A caught/wrapped exception cannot turn a partial operation into a completed update or command result.
4. Fault publication precedes Updated; retained observations and unrelated sessions remain unchanged.

## 2. Design

`WorldFault` has an internal constructor and four get-only properties. World's private `LatchFault`
is shared by initialization, replacement and update catches. `RequireHealthyUpdate` checks callback
boundaries and command checks/applies before results, later phases or clock completion. It aborts the
existing update path; it adds no scheduler, state store, timer, serializer or recovery mechanism.
One callback cannot be forcibly preempted; engine boundaries stop subsequent work once it returns.
Developer exception details are not player-localized messages. Faults remain terminal and unsavable.

## 3. API surface

Existing `SimGame.Fault` / `World.Fault` keep their type. Tick/Phase/Message/Detail become read-only.
Tick is the existing game-update coordinate, not an independent clock. No Draw or platform integration.

## 4. Files

- Core/Fault.cs; Kernel/World.cs; existing build identity and current assertion literals.
- LifecycleTests.cs and nested FaultBoundaryTests.cs: explicit JSON probe, registered component/command.
- Master, P2/P3 parent plans, README, AGENTS, offline help and scoped verification report.

## 5. Errors

Escaping, wrapped and swallowed initialization failures all retain `entity.initialize` and the original
cause. Later failure attempts cannot replace it. Already completed earlier commands retain results;
failed/later commands have no success result. No rollback or fabricated healthy checkpoint.

## 6. Compatibility

Development-only breaking API/build identity; no writable-field aliases or earlier-build reader.
Save layout unchanged because faults are not resumable state.

## 7. Verification

Clean solution build, then only the exact existing lifecycle fault suite and timed replacement suite.
Exercise real submitted commands, partial initialization, backlog cutoff, first-failure identity,
immutable API, snapshots/events, sole-clock timing, save refusal and isolated sessions. Other suites
compile only. No third, family or full run; full P2/P3/roadmap acceptance remains open.

## 8. Risks

This does not preempt callback code or validate all shared content/protocol arguments. Diagnostic
strings may contain developer-sensitive exception text; consumers must not show them to players.

## 9. Out of scope

Oil/Tycoon, hosts/demos, renderers, adapters, changing GameClock, recoverable incidents, full regression.

## 10. Execution

- [x] Replace mutable record and consolidate existing fault latching.
- [x] Guard update/command callback boundaries and add nested actual-command fault probes.
- [x] Build and run the two exact gates; record scoped evidence and reconcile current tracker.

Focused result: build2.146.0/save76; clean build,190 checks/two exact suites (78 lifecycle;112 replacement),
zero final failures/aborts. See [evidence](../../docs/migration/P2_IMMUTABLE_FAULT_BOUNDARY_VERIFICATION.md).
This completes F1 only, not full P2/P3 or the framework roadmap.

Follow-on [F2 event-listener cutoff](P2_EVENT_FAULT_CUTOFF_DESIGN.md) binds the existing dispatcher to
that same World authority and guards individual callbacks. F1's preceding190 gate is not rerun there.
