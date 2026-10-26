# P7-D4 creation lifecycle first-fault cutoff verification

## Current M63: removal lifecycle

2026-10-22; **native2.718.0/save161/replay2**, master HEAD
`cb61483ab81404c11944931aedb4703ba694edb5` plus preserved existing work. The
[same lifecycle design](../../plans/enhancements/P7_CREATION_LIFECYCLE_CUTOFF_DESIGN.md) now covers
retirement through original EntityStore/World owners. Native changes are ONLY Kernel/Entities.cs,
Kernel/World.cs and Kernel/SaveGame.cs build identity; current protocol fixture matches. Production
codecs/public signatures remain unchanged. No compatibility path or header rewrite.

### Reproduced defect and original-owner repair

Old2.717 direct removal threw from actual PointSpace.OnDespawn without latching World.Fault
(baseline6/2). The separate unfinished-removal admission baseline0/2 shows a save attempt escaped
the lifecycle boundary. Both tests use explicit engine-side negative instrumentation, not host rules.

EntityStore.Destroy now brackets its actual callback with a finally-unwound transient depth; existing
RequireCompletedLifecycle refuses checkpoint/decoding/clock work while any removal remains active.
World.Despawn retains pure guard refusal outside mutation, catches/rethrows unexpected removal failures
through original LatchFault("entity.remove",error), and checks existing health after callbacks so a
swallowed nested failure cannot return healthy. No rollback, resource refund or success prefix is
invented. Nested ordinary removal/creation remains valid; first-fault precedence remains unchanged.

Two new methods in the SAME CreationCleanupTests partial cover four direct/nested failure contexts
and108 nested lifecycle admission contexts in new/restored sessions. Save, hash, breakdown, direct
encoding/decoding, World.Step, ordinary/elapsed Update and fast-forward refuse before payload/clock
changes, across outer-before/nested/outer-after callbacks. Handled refusals let complete nested
removal and creation finish/save/continue identically. Propagated failures stop subsequent work and
cannot checkpoint. Normal real-stock guard refusal remains healthy and unchanged. Isolated sessions
actually advance. Updated's established zero-step fault notification is not productive game progress.

### Affected public physical retirement

Existing PermanentRemovalTests performs actual player policy/planning, timed physical removal,
partial loading/unloading interruption, real packing, retained cargo recovery, paused/saved twins,
privacy and session isolation. Its initial76/2 and diagnostic76/2 failures are retained: actor bay32
was valid but the old assertion demanded31; a truck paused at2 physically blocked the crew's path
from3 to the only free inner bay1. Diagnostic entity/job snapshots prove the situation before
retirement, not a removal-depth fault.

Only test content authoring/oracles changed: original slot filters restrict endpoint inner bay1 to
resource hauling and outer bay2 to builder work, so both actual roles have reachable geometry.
The actor still has its two authored service bays, either of which the engine can choose. Routing,
collision, stock/claim guards, fixed512-update limit and engine gameplay remain unchanged. Final
six minute/half-minute worker/pickup-endpoint/delivery-endpoint workflows physically complete and replay.

### Every attempt and scope

~~~powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact '<literal below>' --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
~~~

| Literal | Actual final result / image |
| --- | --- |
| Removal lifecycle: direct geometry failures and swallowed nested faults cannot return healthy |60/0,284ms,PID8476/exit0,observer425ms; build4 |
| Removal lifecycle admission: nested callbacks cannot checkpoint or advance the sole clock |984/0,780ms,PID28988/exit0,observer919ms; build4 |
| Creation allocation: local reentrancy and checkpoints refuse while initialized children retain distinct identities |612/0,477ms,PID10192/exit0,observer624ms; build4 |
| Permanent removal: authored physical worker loss and partial-haul endpoint retirement preserve state |311/0,8708ms,PID300/exit0,observer8889ms; build6, six actual replays |

Selected successful checks1967/0 span two test images; no claim that all ran against final build6 tests
DLL. Native/proof inputs did not change after build3. Build4 tests hash was not captured before later
test-only edits; do not invent one. Reproduction/native and final identities are explicitly recorded.
No old green literal was rerun after those edits. No full/default/profile/family/soak, consumer/
solution-wide execution, new performance budget or timeout increase.

Six targeted builds: first failed12.31s because the new test omitted StateReader offset/length; clean
build2 9.33s after correcting the actual API. Native repair build3 11.96s clean. Its first fault test56/4
had an overly strict new Updated oracle; correcting it changed no notification rule. Clean build4
10.07s, diagnostic build5 6.47s and test-authoring build6 6.72s. Nine bounded runtime invocations,
all terminal; five expected/diagnostic failures and four scoped successes. Live build observers67873,
58903,75861,34806 were polled to actual terminal state, never restarted because of an observation yield.

[All attempts](../../artifacts/optional-module-durability/7316136d332c41afb5aa85242fd4557f/attempts.json),
[logs](../../artifacts/optional-module-durability/7316136d332c41afb5aa85242fd4557f/),
[baseline identities](../../artifacts/optional-module-durability/7316136d332c41afb5aa85242fd4557f/baseline-binaries.json),
[old-engine reproduction inputs](../../artifacts/optional-module-durability/7316136d332c41afb5aa85242fd4557f/reproduction-inputs.json),
[internal removal callers](../../artifacts/optional-module-durability/7316136d332c41afb5aa85242fd4557f/removal-callers.json),
[frozen inputs](../../artifacts/optional-module-durability/7316136d332c41afb5aa85242fd4557f/final-inputs.json),
[original221 map](../../artifacts/optional-module-durability/7316136d332c41afb5aa85242fd4557f/original-gates-current.json),
[structural checks](../../artifacts/optional-module-durability/7316136d332c41afb5aa85242fd4557f/structural-checks.json).
Final native1CD0D6B27A1199072671A93542E297B229BF45B77EC9EB8F98E4AF756D59035A /
proof98BB9D222132F228ACB00CC100931F7BE164F420707C76B44BAA2B87BD1302FE /
tests0EFAD953C58AB2165680E1F85D2727A6545E5777A7FBDD29845EA0D6C39FD504.
Proof source/content was not edited; its DLL was rebuilt against the new native image.

### Caller audit, self-review and remaining work

Contextual review finds exactly one native EntityStore.Destroy caller, World.Despawn, and eight
explicit internal missing-participant fault injections in framework tests. Normal gameplay cannot use
that internal bypass. This census is not all raw resource mutation/conversion/reference accounting;
the broader original obligation remains open in the [active C3 matrix](../../plans/enhancements/P7_OWNED_RESERVATIONS_DESIGN.md#current-c3-audit-m62).

The installed SDLC procedure kept this repair within existing lifecycle design/evidence/tracker owners.
Self-review only, not independent review. Original first-fault diagnostics/typed blockers and zero-step
Updated fault reporting are reused; no new logger or notification channel. Existing
[operational/assurance gaps](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remain outside this scoped check. No business/Oil/consumer/platform/Draw change, staging/commit/push.
Original221/210 source-recorded checked/11open unchanged; no original closure or full-goal completion.
Next finish the actual resource transaction caller/fault matrix, then typed reference-owner evidence
and relevant bounded stage stress. Focused-only permission includes final validation.

## Historical D4 creation evidence

## Outcome

Engine **2.617.0 / save159 / replay1**. Public signatures and production state codecs unchanged. Original allocator, Spawn/EntityStore lifecycle and World first-fault owner remain authoritative. No compatibility overload, new simulation state, financial rule, host application, platform or Draw change.

Three literal finals: **218 +88 +78 =384 passed, zero failures**. Original P7.017 and the full goal remain open. Original bookkeeping stays221 requirements /200 recorded checked /21 tracked open; this is not a fresh global audit.

442 core C# path/content SHA256: `e861614d987862c66ab2182fcf96359e45791c517d9d3e3a7aa9d983b9e45f22`. Earlier checkpoints are historical unless explicitly rerun here.

## Production changes

### One World fault boundary

`World.Spawn` now invokes original `RequireHealthyUpdate` before argument/content/provider work. Healthy definition/space validation remains before the mutating lifecycle and retains its original refusal semantics.

The existing `Entities.Create` call moved inside Spawn's original try/catch. Phase starts `entity.create`, then becomes existing `entity.initialize` after registration returns. Unexpected exceptions use the same `LatchFault`, preserving any earlier fault record, and rethrow rather than produce a healthy result. Configure/OnSpawn/OnCreate failures no longer depend on an outer Update or resource transaction to fault direct callers.

Each actual `IEntityInitialization.Initialize` invocation is followed by the original health check before invoking another component or returning success. A provider that catches and swallows an earlier engine failure cannot continue the later initialization chain.

### Existing entity-store callback order

`EntityStore.Create` invokes the supplied original health check after configure, after the selected space's OnSpawn and after each component OnCreate, before registration. It still owns local candidate construction/cleanup, sole ID progression and registration/pending state. No callback dispatcher or second fault flag was introduced.

The unused optional `restore` callback was replaced by the required health Action, without a compatibility overload. Read-only call-site inspection found exactly one production caller, World.Spawn. Load still decodes through original EntityStore.Sync rather than creation callbacks; replacement preparation uses its own unchanged lifecycle.

This is fault cutoff, **not** whole-source/map purity around otherwise healthy initialization. Legitimate new target initialization and spatial registration are not frozen by a world-hash comparison.

## New proof coverage

Literal: `Creation lifecycle: direct failures and swallowed faults stop later callbacks`.

The endpoint fixture explicitly authors a result with existing creation probe followed by a diagnostic tail component, plus a child prototype whose actual initialization throws. Per-instance hooks/counters are transient negative instrumentation, not simulation state, a codec or static mutable cross-session state. No host configure hook was added to public CommitCreation.

- Four healthy new/restored × Spawn/CommitCreation paths prove exactly one OnCreate/Initialize per actual component, original saved identity/hash, and zero replayed creation/grant callbacks on load.
- Twelve direct Spawn negative contexts: new/restored × configure/OnCreate/Initialize × throw/catch-and-swallow a real nested initialization failure. Verify first fault, no successful returned entity, correct original exception/phase, one original callback, later OnCreate/Initialize cutoff, no pre-registration outer candidate, no success events, retained healthy publication, no resumable save/hash and early refusal of later invalid Spawn.
- Four CommitCreation negatives: new/restored × file-authored throw-create/throw-initialize. Verify actual first phase/exception, withheld return, no allocation for failed local creation, terminal registered prefix for initialization failure and later callback cutoff. Do not reset an ID or invent rollback for an actual prefix.
- A separately restored healthy session actually advances after each failure.

Nested creation during the outer callback can leave its own real registered/pending prefix before failing. The engine retains that terminal state for diagnostics; these tests do not assert an artificial rollback or healthy continuation.

Pre-repair proof: **166 passed /46 failed**. It demonstrated unlatched direct configure/OnCreate failures, later OnCreate/Initialize execution after swallowed inner failure, success returns after fault, and argument checks preceding existing terminal faults. Final218/0 uses the same proof; extra postfault assertions become reachable only when the repaired owner latches the failure.

## Existing focused regression evidence

- `Entity conversion: timed results, saved waits and provider fault boundaries` **88/0**: real native WIP/tools, capacity wait, repeat/cancel, sequential entity-only work, independent result, resource/initialization failures, point/tile rollout and saved placement wait remain valid.
- `Lifecycle: entity state faults latch before snapshot publication` **78/0**: includes existing `FaultBoundaryTests.Run` plus state-fault checks. Original initialization phase/exception survive wrapper and swallowed command errors, later commands/state updates stop, one clock does not advance, retained snapshots remain detached and failed state cannot save/hash/resume. Existing `entity.initialize` semantics were preserved.

These are low-level provider/protocol tests where direct World/component setup is explicit. They are not proof of complete public-player acceptance or arbitrary-mod isolation.

## Every observed attempt

Build: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`.

Each runtime is a separate `--bounded-exact` child with timeout60000 /termination5000; output6000 for baseline,4500 for finals. All returned outputs were untruncated.

| Attempt | Result | Process evidence |
|---|---|---|
| Test/probe baseline build on2.616 |Clean5.86s|zero warnings/errors,exit0; no production change yet.|
| New proof reproduction |166/46,241ms|pid10944,exit1,outer398ms; full matrix completed.|
| Production lifecycle repair/build2.617 |Clean7.72s|zero warnings/errors,exit0; save159/replay1 unchanged.|
| New lifecycle final |**218/0**,206ms|pid16628,exit0,outer355ms.|
| Timed entity conversion final |**88/0**,784ms|pid32756,exit0,outer932ms.|
| Existing lifecycle fault final |**78/0**,86ms|pid14084,exit0,outer222ms.|

No executable edits after these final results. No full/profile/family runtime or Git publication. No active process handles remain.

## Open boundaries

Secondary cleanup exceptions can still mask a primary creation exception inside original EntityStore candidate cleanup; audit separately rather than claim this stage proves that boundary. Spatial OnSpawn swallowed-fault cutoff is implemented by the same gate but not separately negatively instrumented here. Whole-source/space auxiliary purity across otherwise healthy creation commits, coupled conversion callbacks, topology/accounting breadth and stress/performance remain original P7.017 work. Other P1–P12 requirements remain tracked. No stage/commit/push before full goal and plan completion.
