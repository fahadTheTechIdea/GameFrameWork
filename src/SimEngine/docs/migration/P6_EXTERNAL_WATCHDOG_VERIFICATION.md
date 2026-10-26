# P6 external bounded acceptance watchdog — scoped verification

## Outcome and authority

41 parent checks passed,0 parent failures/aborts in7303ms; ONE distinct NEW exact case, ONE attempt.
Expected failing/hanging child probes are deliberate proof inputs, not a claim that all engine suites pass.
Full goal/P1–P12 remain ACTIVE. No staging, commit or push before full goal and plan finish/verification.
Only test infrastructure and an independent authored deliberate engine-fault module changed.
Core2.303.0/save144/replay1/sole GameClock and prior authored game JSON are unchanged.
No Oil/Tycoon/Unity/Godot/Draw or older behavior/family/full regression execution.
The lifecycle skill kept stage, implementation and focused evidence separate from full completion.

## Ownership and behavior

BoundedTestProcess is the one process owner in Beep.SimEngine.Testing. It launches the current entry
assembly/apphost with strict --worker-exact; never shells an interpolated command or attaches to a PID.
An ArgumentList carries exact names/input without shell interpretation.
It concurrently drains fixed-size chunks into bounded stdout/stderr tails, preventing unbounded
newline buffering. Output cannot reset the wall-time deadline. Timeout kills only the started child
tree and observes its exit within explicit grace. Output drain is also bounded. Start/termination/
observer errors cannot report Success; these negative observer branches remain untested here.
Detached result retains actual PID, exit code, elapsed wall time, status, truncation and observer failure.

TestRunner owns unique ordinal exact registration validation before executing a case.
Unknown/partial names match0; deliberately duplicate fixture matches2; neither starts a scenario.
Its bounded CLI parent path is implemented/compiled but NOT executed in this slice.
The process API and worker path are the observed evidence; CLI wiring is source-only.

The independent UpdateWatchdogProbeModule selects a master-listed JSON fault profile, adding one
stateless normal engine system. At an authored sole-clock step it either throws or never returns.
A noisy variant writes long lines/continuous output and then remains blocked.
These are explicit deliberately invalid provider probes, not game rules or a second clock.

The child driver submits one normal extraction command and calls Game.Update. Before each update it
emits actual public scenario/seed/build/save/content digest/clock/hash/state/module/order/node/job/
recent-command facts. Native engine Fault phase/message are emitted after a thrown provider.
No hash/save is fabricated while an update is incomplete/faulted. The last complete snapshot is not
the current internal state of a blocked provider. Healthy public work roundtrips native save/load and
leaves an untouched session's exact hash unchanged. No live World or resource mutation in the driver.

## Files and authored data

- Testing/BoundedTestProcess.cs and existing Testing/TestRunner.cs.
- ExtensionProof/UpdateWatchdogProof.cs: public extension contracts, no core changes.
- Master-listed update-watchdog-master.json and update-watchdog.json reuse original Planet X content.
- WatchdogTests.cs: SAME registered case with explicit verification-only child input.
- testdata/watchdog-profile.json: timeout2000ms, termination grace3000ms, per-stream tail4096chars.
- Existing Program and project owners register/embed the new proof, not parallel runners/catalogs.

## Exact commands and attempts

Build: `dotnet build Beep.SimEngine.slnx --no-restore`.

Runtime: `dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "P6 watchdog: exact isolated acceptance, bounded output and blocked engine update diagnostics"`.

| Build | Result | Observation |
| --- | --- | --- |
| 1 | Failed;0 warnings,3 errors;8.55s | New driver used nonexistent JsonValue.AsInt; use the existing JsonReader.Int contract |
| 2 | Clean;0 warnings/errors;5.78s | Final executable input |

ONE runtime attempt:41/0/0 in7303ms.
Executable source/proof/tests/JSON frozen at2026-10-08 15:23:37 UTC.
No executable changes or reruns after green. Other compiled cases were NOT behaviorally revalidated.

## Actual nested process outcomes

| Input | Status / exit | Elapsed ms | Retained stdout / stderr chars |
| --- | --- | --- | --- |
| healthy | Exited /0 | 684 | 2962 /0 |
| assertion | Exited /1 | 588 | 3001 /0 |
| driver-throw | Exited /1 | 631 | 1865 /0 |
| engine-throw | Exited /1 | 654 | 1533 /60 |
| block | TimedOut /-1 | 2059 | 1265 /60 |
| noisy-block | TimedOut /-1 | 2046 | 4096 /4096; both truncated |
| missing exact name | Exited /2 | 159 | 0 /49 |
| partial exact name | Exited /2 | 141 | 0 /49 |
| duplicate registration | Exited /2 | 302 | 0 /49 |

Every started child returned an observed terminal exit. Both blocked probes retained the entered
provider marker; quiet block retained its last public snapshot in the output tail.
Noisy block deliberately truncates earlier facts, retaining actual latest still-blocked output.
No observer failure in these nine processes. Neither timeout is converted into game completion.

Scoped source audit:1869 self-excluded paths,612 Markdown,3941 local links. All359 prior core
source paths and341 prior authored game JSON unchanged; two new authored fault-probe documents.
Sole GameClock SHA unchanged. Actual git diff --check exit0 (33 line-ending messages);
18 five-column phase rows valid. New player driver live-World/mutation scan has no matches.
Final manifest/hash/link audit is metadata only, not broad behavioral acceptance.

## Limits and next work

Verified on this Windows/.NET8 host, not other OS/process hosts.
No malicious-process sandbox/escaped descendants/startup kernel hang/failed termination guarantee.
Negative start/stream/drain/termination observer branches and CLI-parent execution remain open.
All desired diagnostic facets (reservation tokens/routes/dock queues/WIP/rules/incidents) are not
present in this extraction-only probe. Capture richer facts for later acceptance slices.
Existing TestKit.RunUntil still returns after an unmet predicate; next slice must make that fail
explicitly with public diagnostics while retaining its thin-player-driver boundary.
Broader provider/corruption/lockstep/fairness/accounting/stress/soak/allocation/full industrial/Oil/
P1–P12 gates remain open. This proof does not authorize full-suite execution.

[Stage](../../plans/enhancements/P6_EXTERNAL_WATCHDOG_DESIGN.md) ·
[Master tracker](../../plans/enhancements/MASTER_TRACKER.md) ·
[Source manifest](P6_EXTERNAL_WATCHDOG_SOURCE_MANIFEST.json)

