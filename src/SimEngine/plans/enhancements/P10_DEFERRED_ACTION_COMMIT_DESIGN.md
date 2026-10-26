# P10-AC1 — Deferred action commit admission and cutoff

Status: implemented2.628.0/save159; baseline106/22; four selected current-core
checks502/0. [Every attempt/limits](../../docs/migration/P10_DEFERRED_ACTION_COMMIT_VERIFICATION.md).
Original P10.022/full goal open.

## 0. Problem Statement

AutomationSystem writes applied/latch/timing state immediately after normal
handler.Apply without checking whether that handler swallowed an already-latched
engine fault. It also reads provider cooldown metadata after physical mutation
and after beginning the record update. These can publish a false success prefix.

## 1. Goals

1. Resolve pure cooldown metadata and checked deadline before applying a command.
2. Refuse malformed/mutating metadata through the existing participant observer.
3. Check original world health immediately after Apply before rule-state success.
4. Preserve real partial physical mutation and original first fault, never fabricate
   rollback; later due actions/evaluators must not run after failure.
5. Preserve normal deferred actions, pending saves and deterministic continuation.

## 2. Design

In the original due loop, capture the pending decision, derive cooldown deadline
through existing ReadParticipants over the actual rule and bound entities, then
invoke the normal handler. RequireHealthyUpdate before assigning applied/latch/
cooldown fields. No new executor, transaction owner, queue, clock or codec.

## 3. API surface

No public API change. Build identity changes; save/replay/schema unchanged.

## 4. Files to change

| Action | File | Risk |
|--------|------|------|
| Modify | AutomationSystem.cs | Medium: authoritative commit boundary |
| Modify | ExtensionProof/AutomationProof.cs | Low: explicit handler/cooldown fault hooks |
| New | DeferredActionCommitTests.cs | Low: targeted protocol evidence |
| Modify | AutomationTests.cs, SaveGame.cs/current-engine-protocol.json | Low: registration/build identity |
| New | verification report | Low: exact evidence |
| Modify | tracker, P10 design, README, AGENTS | Low: current status |

## 5. Error handling matrix

| Outcome | Required result |
|---------|-----------------|
| Handler succeeds | Ordinary rule applied state after physical action |
| Handler throws or catches a latched native fault | Original first fault; no applied-state prefix |
| Pure cooldown metadata mutates or throws | Fault before handler/state mutation |
| Real first leg already changed target | Preserve faulted facts, no rollback/save/hash |

## 6. Backward compatibility

Development-only; no legacy path or compatibility shim.

## 7. Verification

One exact case with fresh/restored pending rules, two same-step due targets,
handled native creation fault/propagated native fault/direct throw and cooldown
metadata silent-state/throw/clock-throw cases. Compare original rule codec, actual
target state/version, sole clock, retained snapshots, first fault and independent
saved-session continuation. Run only new reproduction/final plus existing directly
affected action/rule provider literals as needed. No full/family/profile run.

## 8. Risks

This is explicit normal-handler protocol fault instrumentation, not a public-player
failure scenario or CLR sandbox. Already executed physical mutation is retained.
Provider observation is transient; no global state copying or rollback introduced.

## 9. Out of scope

Oil/Tycoon/hosts/platform/Draw/adapters, arbitrary-handler scope enforcement, broad
stress/performance/P10 closure, Git staging/commit/push.

## 10. Sub-task execution order

1. Add targeted proof and reproduce current native-owner defect.
2. Repair original due action ordering and health cutoff.
3. Build and run exact affected checks.
4. Record every attempt, update tracker and preserve broad open requirements.
