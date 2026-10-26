# P8-RP1 actual return execution preview verification

## Engine change

Core2.483.0/save154/replay1/transformation-view20. ProductiveEstimateStatus now includes Returning. The existing ProjectExecution identifies an actual returning head before production mode/control/gate status, leaving productive remaining time and completion step absent. CurrentBlocker uses the actual snapshot return blocker before disabled production or production-gate substitution.

Detached Gates remain production-gate facts and ClockPaused remains the actual sole clock fact; no cleanup duration is invented. Nominal authored cycle remains distinct from productive completion. Ordinary working conditional ETA, disabled-production blocker and not-head semantics remain. Native Transformer.Phase/Blocker and existing WIP/tool/return execution remain the sole authority; no new preview state, queue, clock or scheduler.

Transformation view19 advances to20 for changed public status/blocker semantics. Save154/replay1 payloads unchanged.

## Exact build/runtime

One solution compile attempted: dotnet build Beep.SimEngine.slnx --no-restore,3.47s,0warnings/1error. Windows reported insufficient system resources reading .git/info/exclude before compilation. Read-only metadata/process diagnostics confirmed the file exists; no repository repair, cleanup or process termination was performed.

One focused reduced-concurrency compile completed cleanly in75.33s,0warnings/errors:

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -m:1 -nr:false -p:UseSharedCompilation=false
```

No solution rerun. ONE NEW exact FIRST/FINAL346/0/0 in11537ms; PID9720 Exited0, process12255ms, zero aborted suites, stream truncation or observer error. Clean compile precedes runtime; no executable edit after FINAL. No old/family/full runtime.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Return execution preview: original cancellation blocker and no productive ETA while disabled" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 24000
```

Outer60000ms/termination5000ms/output24000chars. Each physical stage bounded1000Updates with actual clock/phase/blocker/progress diagnostic.

## Public-player proof

Four native/point minute/half workflows use the existing actual master-listed manufacturing/restoration files and ordinary seat commands/views/results, Game.Update, saves/replay and single-clock controls. No new authored fixture, World/TestKit/direct stock/progress/job/geometry writes or host gameplay.

Unstarted recipe has NotHead/no current blocker or fabricated ETA. Physical haul supplies eight real units. Actual timed working state reports a conditional completion step and remaining productive time from current earned work and the selected authored quantum. Disabling normal production retains its existing Disabled cause/no ETA.

Normal raw-port selection removes original return access; ordinary cancellation enters actual Returning. The engine preview now reports Returning, exact ResourcePortCodes.OriginalUnavailable and absent productive estimates even though production remains disabled. Original reusable tool identity remains. Repeated/private reads preserve hash; foreign queries publish no facts. Saved twins and paused clock preserve actual return work/stock/accounting without advancing duration or repeating operating costs.

Restoring original input access returns WIP once and releases the tool. Settled preview becomes NotHead without stale blocker/ETA. Actual raw equals eight minus previously consumed operating inputs, cancelled output stays zero, and retained return snapshot remains detached after release. Final saves/replay and untouched interleaved session match.

## Qualification

Original221/186checked/35open unchanged; additional210. No original checkbox/text/line changes or gate closure. This proves affected native/point batch return semantics, not every continuous-return transient, independent provider, production gate/descriptor fault, optional monetary cost, all phases/resources/tools, RNG implementation beyond actual hash checks, performance/stress or full preview/P8/P1-P12 regression. Earlier cases compile only, not rerun.

Canonical SimEngine only; protected sole-clock implementation/G19/Tycoon unchanged. No Oil, Draw/platform/adapters, staging, commit or push. Full goal AND plan remain incomplete.

## Metadata closure

Fresh read-only metadata closure:1715 source anchors,954 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2903 paths/1028 Markdown files/8669 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors, unchanged186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2891/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional210. Six existing selected executable inputs changed; one newly created executable driver/three new source files, four existing production sources changed/zero new. No previously existing input newly admitted. Transformation view20 records return status/blocker semantics. An initially slow read-only hash command outlasted its observer; authoritative process inspection found no live hash command, then the inventory collector was updated to resume yielded command handles and the full fresh freeze/closure completed. No runtime rerun for documentation validation.
