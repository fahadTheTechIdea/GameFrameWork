# P2-F2: Terminal event-listener cutoff

## 0. Problem statement

`EventBus.Queue<T>.DispatchNext` invoked all listeners without checking World's terminal fault latch.
P2-F1 checked only after the entire event pass. A listener swallowing actual entity initialization
failure could still run later same-payload, queued and nested listeners. Direct event dispatch could
also be retried after a callback exception as if the session were healthy.

## 1. Goals

1. Only World creates its existing EventBus; each dispatcher references that same fault authority.
2. Each listener checks terminal state before and after execution; no later listener runs after fault.
3. Escaping listener exceptions identify their stable subscription; nested faults keep their first owner.
4. All touched queues clear on failure; subsequent publication/dispatch cannot restart the session.
5. Healthy publication order, nested draining, metadata and queue reuse remain unchanged.

## 2. Design

EventBus has one internal World-bound constructor, not a detached public/default dispatcher.
Its existing typed queues receive that World during DispatchNext. Existing World.LatchFault and
RequireHealthyUpdate become internal engine methods reused by the dispatcher; no public mutation API.
Listener exceptions latch `events.<subscription-id>` before rethrowing. Nested initialization faults
remain first. Guards also protect Publish and Dispatch, whose existing finally clears every touched
queue. World.Step catches the original failure and never completes the sole game clock.

## 3. API surface

Modules retain composition Register/Require/Subscribe and runtime Publish on World.Events.
EventBus construction is engine-only. No compatibility constructor, second dispatcher/model/clock.
Fault Phase is a developer diagnostic, now subscription-specific for direct listener exceptions.

## 4. Files

- Kernel/Events.cs, Kernel/World.cs, existing build identity/assertions.
- EventOwnershipTests.cs and nested EventFaultBoundaryTests.cs; explicit master-listed JSON probe.
- This stage, parent P2/P3, tracker, evidence, AGENTS, README and help.

## 5. Errors

Escaping direct listener failure becomes events.probe.first. Escaping, wrapped and swallowed spawn
initialization failure remains entity.initialize. Subsequent publications reject and queues are
empty. The original failure remains terminal; no retry/rollback/healthy checkpoint is fabricated.

## 6. Compatibility

Development-only breaking build/API; no old dispatcher constructor or fault-reset alias. Save layout
and event declarations/subscription manifest remain unchanged; faulted sessions are not resumable.

## 7. Verification

Clean solution build, then exactly:

- Event ownership: handler failure clears every pending queue
- Event ownership: nested same-type publications drain in order

Nested probes use an actual GameSystem publishing events during normal Update. Assert listener fanout,
nested/queued cleanup, callback completion cutoff, first cause, immutable publication, one clock,
backlog halt, save/hash refusal and no resumed dispatch. Other suites compile only; no broad/full gate.

## 8. Risks

Callbacks cannot be forcibly preempted; code inside a listener before it returns remains its provider
responsibility. Host Updated subscribers and attached player input drivers are outside this slice.

## 9. Out of scope

Oil/Tycoon/host/demo/platform/Draw; changing GameClock; full P2/P3 lifecycle or roadmap acceptance.

## 10. Execution

- [x] Bind existing EventBus to World and reuse its first-failure/health authority.
- [x] Guard each listener/publication/dispatch and replace retry-after-fault assumptions.
- [x] Build, run only the two exact gates, record scoped evidence and update tracker.

Build2.147.0/save76; clean build,111 checks/two exact suites (84 failures/cleanup;27 healthy nesting/
saved continuation), zero failures/aborts. [Evidence](../../docs/migration/P2_EVENT_FAULT_CUTOFF_VERIFICATION.md).
This closes F2 only; full P2/P3/P1–P12 acceptance remains open.
