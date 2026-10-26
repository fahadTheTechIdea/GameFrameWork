# P6 — External bounded acceptance execution

## 0. Problem statement

The public Planet X drivers bound repeated Update calls, but one synchronous stuck Update prevents
their stopwatch from running again. Existing TestRunner executes a selected Action<Check> inline.
An engine provider fault or hang must not leave a gate running indefinitely.

## 1. Goals

Scoped API/strict-worker proof complete:41 checks,0 parent failures/aborts in7303ms.
Bounded CLI parent entry is compiled/source-reviewed but NOT executed; its end-to-end gate stays open.

Execute exactly one named case in its own process. Keep its exit/refusal distinct from timeout.
Drain stdout/stderr concurrently into bounded tails; retain emitted engine diagnostics on termination.
Kill only the process started by this execution and its descendants; report unconfirmed termination.
Prove success, assertion/driver failure, engine fault and synchronous blocked/noisy Update behavior.
No simulation rule, clock, stock, workflow completion, renderer or platform adapter enters the runner.

## 2. Design

One BoundedTestProcess in Beep.SimEngine.Testing owns child lifecycle and bounded output capture.
TestRunner gains an explicit bounded-exact command and a strict exact-worker dispatch path.
Existing substring/default suite behavior is not changed or executed by this slice.
Child input is verification-only probe selection, never game configuration or live state mutation.
A master-listed independent engine-side UpdateWatchdogProbeModule selects an authored fault profile.
Its deliberately broken system blocks or throws during normal Game.Update. Native World faults still
own fault latch/phase; the watcher never invents a save/hash after an unfinished/faulted update.
A new testdata JSON explicitly supplies test timeout, termination grace and output capacity.

## 3. API surface

BoundedTestProcess.Run(exactCase, timeoutMilliseconds, terminationTimeoutMilliseconds,
outputCharacters, caseInput = null) returns detached lifecycle/output diagnostics.
TestRunner: --bounded-exact NAME --timeout-ms N --termination-timeout-ms N --output-chars N
with optional --case-input INPUT. It forwards only the exact worker protocol, never a broad filter.
The internal worker spelling requires exactly one matching registered case and rejects unknown options.

## 4. Files

New Testing/BoundedTestProcess.cs; modify Testing/TestRunner.cs.
New independent UpdateWatchdogProof.cs and two explicit master/content JSON documents.
New WatchdogTests.cs and testdata/watchdog-profile.json; register/embed in existing project owners.
Plans and evidence remain in the existing enhancement tracker and migration evidence folder.

## 5. Error handling

Unknown/duplicate exact case: nonzero, no case execution. Invalid bounds/options: explicit refusal.
Start failure: typed result, no fabricated PID. Child failed: actual exit code retained.
Timeout: diagnostic tails retained and own process tree terminated.
Termination not observed within declared grace: distinct failure, never successful cleanup.
Normal engine fault: native Fault facts emitted; no guessed progress or resumed checkpoint.

## 6. Compatibility

Development-only direct enhancement. No engine compatibility shim, copied authority or legacy reader.
No core protocol/save-layout change; existing test behavior remains unrelated to this narrow gate.

## 7. Verification

One distinct NEW exact case:
P6 watchdog: exact isolated acceptance, bounded output and blocked engine update diagnostics.
Build once when ready, then only this case. Repair/rerun only its observed failures.
Nested workers invoke the SAME new case with explicit probe input; no old/family/full/Oil runtime.
All actual builds, attempts and probe outcomes are retained in scoped evidence.

## 8. Risks

Process startup/OS termination cannot be made a simulation guarantee; failures are reported.
The last completed pre-update snapshot is not a stalled provider's current internal state.
Bounded tails intentionally truncate older output. Arbitrary detached/escaped processes are not a sandbox.
No whole-game CPU, provider purity or full-regression claim follows from this watchdog proof.

## 9. Out of scope

Oil, Tycoon, Unity/Godot, Draw, game policies, fabricated repair/progress, broad/full regression.
Commit/push wait for the full objective and plan, not this scoped verification slice.

## 10. Execution order

Executable inputs frozen2026-10-08 15:23:37 UTC after the first runtime attempt passed.
TWO builds: first failed(new JsonValue.AsInt misuse), final clean5.78s0 warnings/errors.
No older behavior/family/full/Oil/Tycoon/Unity/Godot runtime was run. Subsequent edits are docs/read-only.
[Focused evidence](../../docs/migration/P6_EXTERNAL_WATCHDOG_VERIFICATION.md).

Implement strict process boundary; add authored deliberate provider faults and public driver diagnostics;
run the one new exact case; freeze executable files after final green; update tracker/evidence/hash audit.

