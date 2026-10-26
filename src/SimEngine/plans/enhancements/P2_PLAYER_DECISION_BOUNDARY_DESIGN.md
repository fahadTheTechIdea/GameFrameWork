# P2-F5 / P11-C5 — Unified player decision boundary

Status: scoped implementation verified,2026-10-08. Build2.290.0/save141/replay1.
ONE clean build13.47s0/0; ONE exact NEW bounded case FIRST175/0/0 2619ms.
Executable inputs frozen. Continues P2-F4a and P11-C1-C4; full P1-P12 remains open.
[Evidence](../../docs/migration/P2_PLAYER_DECISION_BOUNDARY_VERIFICATION.md).

## 0. Problem statement

Hosting/SimGame.cs PlayerSeat.RunControllers iterates the mutable attachment list and
never expires attached decision contexts. TickCore allows callback exceptions to escape
before Time/snapshot publication. Standard/PlayerControllers.cs expires authored contexts
separately. App/Standard.cs ScriptedController retains its initial context inside a coroutine,
so enforcing expiration without revising that API would break deferred player inputs.
World.Submit/SubmitStamped/Check and network input currently admit activity after a fault.

## 1. Goals

1. Both controller kinds use one invocation/expiration owner, preserving the first fault.
2. Every decision context operation expires after success or failure; immutable Id remains readable.
3. Capture all seats' attachments before dispatch; changes take effect next pass, with no arbitrary cap.
4. A decision failure stops command application and later decisions, then publishes actual time and
   private retained fault snapshots through the original loop.
5. Scripts yield actions using fresh contexts, with no retained-context compatibility path.
6. Faulted sessions reject new commands/previews before callbacks or sequence allocation.
7. Prove healthy public physical work, saved continuation and replay alongside bounded fault cases.

## 2. Design

PlayerDecisionContext owns internal Invoke(IPlayerController): healthy precheck, Decide,
healthy postcheck, first-cause player.decide.<company> fault latch, and finally expiration.
RequireOpen rejects expired or already-faulted contexts. View/time/date/answers/TryGetAnswer
join existing guarded commands/observations. Provider strategy state remains intentionally
mutable; this is not global provider purity or rollback.

PlayerSeat.Attach/Detach use reference identity; duplicate attachment refuses. CaptureControllers
returns a transient input set. TickCore captures every seat before any decision, dispatches
those arrays, and stops on a latched failure before network sealing or World.Step.
Native PlayerControllersSystem calls the same Invoke; its existing saved cadence/state/answers
remain authoritative and only settle after a successful callback.

ScriptedController takes Func<IEnumerable<Wait>>, not an initial decision context.
Wait.Do(Action<PlayerDecisionContext>) executes player input with the current context.
Existing delays/conditions/answers use that same current context; no second scheduler.
Migrate the two existing source callers compile-only. No compatibility constructor.

World checks terminal health before ordinary/stamped inbox admission and preview callbacks;
SimGame checks before network buffering. Previously queued inputs are retained unapplied,
not refunded, deleted or falsely answered. First cause and partial-state save/hash rejection
remain existing World responsibilities. Attached scripts are transient external player input;
authored controllers continue to persist their actual module-owned state.

## 3. API surface

Breaking ScriptedController constructor; new Wait.Do action, existing IPlayerController unchanged.
Internal Invoke replaces Close. Build2.290.0/save141/replay1; no persistent layout or new JSON.
No legacy identity reader. Earlier build fixtures migrate compile-only.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| Modify | src/Beep.SimEngine/Hosting/PlayerDecisionContext.cs | 35 | medium |
| Modify | src/Beep.SimEngine/Hosting/SimGame.cs | 35 | medium |
| Modify | src/Beep.SimEngine/Standard/PlayerControllers.cs | 3 | medium |
| Modify | src/Beep.SimEngine/Kernel/World.cs | 3 | medium |
| Modify | src/Beep.SimEngine/App/Standard.cs | 15 | medium |
| Modify | old scripted callers/build identities/Program | mechanical | low |
| New | tests/Beep.SimEngine.Tests/PlayerDecisionBoundaryTests.cs | bounded case | medium |
| New/Modify | stage/evidence/master/parent guides | scoped | low |

## 5. Error handling matrix

| Operation | Error | Result |
|---|---|---|
| Decide | exception or swallowed latched observation fault | first terminal cause; context expires |
| Retained context | any operational access | explicit expiration, no input |
| Dispatch mutation | attach/detach on any seat | affects next pass only |
| Duplicate attachment | same reference | explicit refusal before list changes |
| Faulted input/preview | local/network/stamped | rejects before sequence/callback |
| Script delay | resumes later | action receives current context, real reply only |

## 6. Backward compatibility

Development-only breaking correction; old constructor and separate Close owner removed.
No retained-context bridge, legacy execution branch, duplicate coordinator or default profile.

## 7. Verification

ONE new exact bounded case:
`Player decision boundary: expired contexts, stable dispatch and terminal command admission`.
One complete-slice build, then this case only. Record every build/run; only observed failures
justify repair and same-case rerun. No old/family/full/platform runtime. Physical waits <=2000
updates with actual work/time/fault diagnostics. Source/proof/tests/JSON freeze after final green.
Full authorized P6 regression, Oil and all broader plan gates stay open.

## 8. Risks

Retained PlayerView references remain scoped live read models, not a security sandbox.
Trusted controller arbitrary CPU/world mutation, cross-provider purity, concurrency and distributed
fault recovery remain open. Captured detached controllers deliberately finish their scheduled pass.
Callbacks must not block; this stage does not add a worker/process timeout.

## 9. Out of scope

Rendering/adapters/Tycoon changes, new gameplay rules/modules, CPU isolation, broad stress,
legacy compatibility, full completion claim and premature Git publication.

## 10. Execution order

1. [x] Inspect real loop/context/native/script/admission owners and record this design.
2. [x] Unify invocation, expiration, stable dispatch and terminal input checks.
3. [x] Replace retained-context script API and migrate actual callers.
4. [x] Author/build/run the one bounded case; no build/runtime repairs required.
5. [x] Freeze executable inputs; update evidence/tracker/current guides and inventory.

