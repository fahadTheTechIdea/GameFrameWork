# P6-FD1 — Bounded normal-lifecycle failure diagnostics verification

Date2026-10-12. Canonical SimEngine only. Core2.510.0/save154/replay1 and all runtime modules unchanged.

## Single wait owner, no gameplay in tooling

The existing TestKit wait loop now owns both its explicit administrative RunUntil path and a normal UpdateUntil path. The latter calls SimGame.Update only. SimulationWaitException records the required budget unit, actual scenario/seed/build/modules, clock age, retained snapshot, fault and hash availability. Lifecycle-call budgets are observer limits, not a second game clock; paused calls never fast-forward physical work. Testing drives commands and reads authoritative views. No stock/progress/state injection, completion formula or replacement scheduler was added.

Limits are authored once in the embedded physical-failure-profile.json:16 Update calls,4000ms per owned child,3000ms termination grace,12000characters per output stream. The existing BoundedTestProcess deadline cannot be renewed by output and terminates only its owned process tree. The new exact case launches only itself with seven explicit inputs, then runs the existing public physical supply/manufacture completion helper.

## Commands and retained attempts

Solution build:

```text
dotnet build Beep.SimEngine.slnx --no-restore -v minimal
```

Clean7.49s,0warnings/errors. Two same-case repair builds used:

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
```

Both clean,6.20s and5.28s,0warnings/errors.

Exact runtime:

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Failure diagnostics: normal update starvation paused corruption and blocked process boundaries" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 24000
```

- First outer:55passed/9failed in4455ms; PID27708/process4638ms/Exited1. A reporting path read a default absent transformation snapshot; this prevented watchdog children from entering the actual provider. The starvation assertion also guessed the wrong engine phase.
- Same-case engine-fault diagnostic (append `--case-input engine-fault`):0passed/1failed in363ms; PID16492/process562ms/Exited1. Located the absent-queue formatter failure before Update.
- Second outer:62passed/1failed in11853ms; PID45764/process12008ms/Exited1. Actual blocked child PID32592 timed out/exit-1 in4047ms; noisy child PID10816 timed out/exit-1 in4034ms. Remaining mismatch was a guessed Blocked phase rather than actual Supplying.
- Same-case starvation diagnostic (append `--case-input starved`):7passed/1failed in549ms; PID46616/process729ms/Exited1. The deliberate unmet wait reported LifecycleUpdates16/16,clock17,queue1,Supplying,blocker.transformation-tools. Its negative exit is expected for the outer protocol.
- FINAL outer:63passed/0failed/0aborted in11238ms; PID18756/process11392ms/Exited0, both output streams untruncated, no observer error. No executable changes after FINAL.

Reporting repairs guard actual TryGet results and state absence explicitly as unavailable. They do not fabricate empty queues, change production phases or force physical completion. Chatty-child assertions inspect retained bounded tails rather than require a discarded initial snapshot.

## Final child outcomes

| Input | Owned PID | Terminal status/exit | Milliseconds | Actual evidence |
| --- | --- | --- | --- | --- |
| healthy | 35880 | Exited/0 | 547 | Ordinary Update reaches a public clock observation; not claimed as manufactured batch completion |
| starved | 33804 | Exited/1 | 563 | Accepted physical batch waits on missing supplied tools/material; budget failure includes actual queue/blocker/hash/snapshot; stalled queue saves unchanged |
| paused | 43996 | Exited/1 | 520 |16 lifecycle calls exhaust observation budget while sole clock stays1; saved stalled queue and other session remain intact |
| corrupt | 42896 | Exited/0 | 406 | Real damaged save bytes refuse through normal loader; healthy and interleaved games unchanged |
| engine-fault | 38284 | Exited/1 | 504 | Existing declared provider throws its original intentional fault; latched failure reports retained publication and unavailable live hash |
| blocked | 2720 | TimedOut/-1 | 4029 | Provider actually enters synchronous Update and blocks; owned process terminates at deadline |
| noisy-block | 35612 | TimedOut/-1 | 4038 | Both output tails stay bounded/truncated; emitted output cannot extend deadline; provider entry remains diagnostic |

Every child has a terminal observed exit and no process-observer error. Deliberately failing child outcomes are asserted as such; they are not reported as successful gameplay. The final positive resource workflow separately completes physical hauling/manufacture through public commands and immutable facts.

## Gate scope and limitations

Only original P6.acceptance.012 closes: “Failure diagnostics and bounded scenarios work when deliberately stalled/corrupted.” Its original text and line1720 stay unchanged. Original221 gates become193checked/28open; additional242. Other P6 durability, all-profile, external-mod, ownership/thin-client and full P1–P12 requirements remain open.

Profile metadata only:

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --profile-info tests/Beep.SimEngine.Tests/testdata/framework-validation-profile.json
```

710registered/included,0excluded/long markers,0game suites executed. An initial invalid `--profile-info framework` invocation refused the missing explicit file (exit1); it ran no games, and the explicit JSON invocation replaces no runtime evidence. No older/family/full regression ran. Hostile arbitrary CLR sandboxing and every possible domain workflow are not claimed.

All production, Tycoon and sole-clock files remain unchanged from P4-QP1. No Oil/platform/Draw edits, compatibility shim, new engine state owner, staging, commit or push. The authorized ONE final full regression remains deferred until implementation is finished. Whole goal is still open.

## Post-pass inventory and audit

Current freeze contains1837 source anchors,1028 executable inputs and13 quickstart anchors. Four new files are authored; one previously existing testing source is newly anchored, and two previously existing testing files are explicitly added to the selected executable freeze. Actual source hashes show zero production changes and all12Tycoon/sole-clock files unchanged. All221 original gate lines and literal requirements validate; only P6.012 changes checkbox, with no line shifts.

Inventory3018paths/1091Markdown/8784local links/0broken;18master rows/0malformed; diff check exits0 with34existing LF/CRLF warnings;3006dirty paths/0staged. The driver audit permits only the normal TestKit.UpdateUntil API, not administrative TestKit paths, RunTicks or raw world/resource/progress writes. An initial metadata audit lacked PCRE2 for that explicit negative-lookahead rule and emitted a regex error; its empty boundary result was not accepted as proof. The corrected audit uses PCRE2 and rejects search errors.

The stale root permission record is reconciled with the user's explicit mechanical-only Tycoon authorization and the already-landed current contract bindings. No Tycoon code changes are made here. Full regression, staging, commit and push remain unperformed.


