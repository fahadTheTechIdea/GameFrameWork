# P8-EY1 execution-preview verification

Status: verified preview acceptance; not full P8 or full engine completion. Date:2026-10-12.

## Actual implementation

Engine `beep-simengine-2.501.0`, save154/replay1, transformation view22. Existing `TransformationExecutionPreview` now copies the authored nullable `EntityOutput` content ID inside the existing guarded, authorized projection. It denotes one successful batch yield, never a live definition, a created entity, guaranteed placement or delivered throughput. `Current.EntityOutput` remains the actual head's state, not an alias for requested-recipe metadata. No new result, queue, work, resource, permission or clock authority.

## Commands and terminal evidence

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Complete execution previews: entity yields physical estimates disclosure and atomic observations" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 24000
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --profile-info tests/Beep.SimEngine.Tests/testdata/framework-validation-profile.json
```

ONE solution build10.43s, exit0/zero warnings/errors. FIRST exact1176 passed/12 failed/0aborted in13295ms (PID44668, Exited1, process13645ms): fixture expected an execution-yield preview to invoke unrelated physical InputPorts callbacks. Actual engine correctly did not inspect those bindings. Corrected fixture asserts no callback/no fault/no state change, then exercises dedicated ReadInputPorts refusal and complete preview's subsequent first-fault cutoff. No production rule changed for the fixture repair. Focused repair build4.94s clean. SAME NEW exact intermediate1200 passed/0failed/0aborted in11703ms (PID18744, Exited0/process11887ms). Before closing the gate, added explicit batch resource-yield quantities and all continuous rate coefficients/periods, not historical evidence; focused build8.46s clean. SAME NEW exact FINAL1223 passed/0failed/0aborted in13465ms (PID7200, Exited0/process13642ms). All watchdog outputs untruncated/no observer error. No executable edits after the third, FINAL attempt.

Profile-info exit0:700 registered/included,0excluded/0long markers/0runtime suites. Full profile executor not run; no full green inferred.

## Covered acceptance

- Current authored metadata checks: batch input2/resource yield1/tool wear1 per one-minute cycle; continuous input2/output1/tool wear1 per minute with exact numerator/denominator/unit/resource identity. Operating input1 per minute in both modes. Bills/rate lists are immutable; resource-only recipes disclose no invented entity result or continuous batch cycle. Save round trips add no preview payload.

- Two minute/half physically supplied batch flows: authored entity yield before any queue, exact input3/tool1, nominal period, blocked supply, earned work, disabled state, changed target1.5/fractional progress, conditional remaining work/completion, paused sole clock, saved twin and one actual entity at predicted completion. Inputs consumed once; reusable tool retained; output metadata is not fabricated resource stock.
- Eight existing public-only discovery/manufacturing helper flows reused, not old registered cases: native/independent recipes crossed with hidden resource/quantity unit/capacity unit/result. New checks prove yield withheld with whole recipe before physical discovery and disclosed as a detached ID afterward. Foreign refusal, pure observations, stock/tool accounting, saved twins, replay and idle session isolation run at current source.
- Two public continuous helper flows reused: exact authored throughput rates, no fabricated batch ETA, pause/disabled/changed-target/finite-feed wait/stop and retained facts. Four tile/point minute/half return helper flows reused: original cancellation causes rather than disabled production, no productive ETA, physical original-endpoint restoration, saved twins and retained receipts.
- Twelve observation sites crossed with exception/pause-clock/identity-allocation probes. Eleven sites guard complete preview callbacks; the physical-port site proves preview non-invocation and dedicated port-view fault protection. No failed observation publishes an output identity or metadata prefix; same first fault, no further gameplay/time, no save and prior detached facts retained.
- Three independent processor RNG faults: draw, draw-and-restore and same-state writes all rejected by the original observation epoch. No prefix publication, save or clock advancement; unrelated session healthy.
- Public driver contains no World/TestKit/RunTicks/resource or progress mutation. Existing helpers remain player input/composition authoring, not execution or domain rules.

## Gate decision and limits

Original `P8.acceptance.015` alone is closed: immutable previews report inputs/yields/throughput/ETA/blockers without changing RNG/state. Original221 gates now190 checked/31 open; additional232. Literal gate requirement and line1106 are preserved except the checkbox. No closure of cancel/save at every phase, broader provider accounting, P8.013/P8.017, global/private-view breadth, all-platform acceptance or full regression. This test proves the listed current flows and fault boundaries, not every conceivable mod callback.

No Oil files, all12 Tycoon sources, sole clock, platform/Draw implementations, business rules or size limits changed. No staging/commit/push. One authorized final full regression is deferred until implementation finishes.

Metadata-only validation:1800 source anchors,1009 executable inputs and13 quickstart anchors; no source/quick/input drift. All221 original gate records match literal requirements/line anchors, only P8.015 checkbox changed.2984 inventoried files/1071 Markdown/8763 local links, zero broken;18 master rows/zero malformed. Driver boundary0, git diff-check0,2972 dirty/0staged. No full game suite executed by profile-info.
