# P4-WC1 — Worker control authority verification

Date2026-10-13. Core2.513.0/save154/replay1/work view102. Physical codecs and sole clock unchanged.

## Original control and physical owners
WorkSystem.CheckHeld now gates healthy/live/owned/current-sight admission before worker/assignment facts. Existing SetHeld still owns genuine interruption, held state and motion stopping; no parallel control state is added. WorkerHomeContracts.CheckHome gates worker and requested home authority. Its original physical binding validation is the internal CheckBinding in the same owner; the existing recurring-home caller uses it, not player sight. Invisible workers or home targets do not disable engine-owned eligibility.

ReadCurrent in the same home owner withholds derived current facts when the worker or bound home is unseen. WorkView.ReadHome and grouped preview use it. Allowed clearing of an own home intent can therefore have no Current projection; ConfigureGroup compares the original private ReadHome intent rather than dereferencing a nullable public preview. This separates authority, physical eligibility and public disclosure without a duplicate home, route or state owner.

WorkView assignment/operating-input/execution reads gate actual current worker sight before service/provider facts or invalid arguments. Typed hold/home diagnosis uses the actual owner for an observer; it cannot grant command privilege. The new SetWorkerHomeGroupCommand/handler in the existing worker-control file and PreviewHomeGroup forward original complete-set admission/commit. Named fleets remain optional.

Mechanical older assertions migrate in FleetHomeTests and WorkerHomeTests (player private refusals) and WorkerStandbyTests (prior policy boundary consistency). The physical existing-home foreign-owner blocker remains NotOwned; it is not remapped into a compatibility code. Those registered older cases were not run.

## Actual builds and retained attempts
One Release solution:

```text
dotnet build Beep.SimEngine.slnx --no-restore -c Release -v minimal
```

Clean10.25s/0warnings/errors; its unused quickstart maps to Debug, core/testing/proof/tests compile Release.

Exact runtime:

```text
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Worker control access: private hold home and assignments preserve autonomous loaded travel" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 24000
```

FIRST:542passed/1failed in1344ms, PID46672/process1496ms/Exited1, untruncated/no observer error. The512Update loaded-travel wait stalled because the authored held secondary vehicle occupied the one-dimensional delivery route atposition2. Moving its authored startup toposition20 retains the same delivery distance, time quantum, speed, quantities, physics and collision rules. No live reposition, crossing or teleport fallback was introduced. Wait failure diagnostics now include actual positions/stocks/jobs.

One same-case fixture repair build:

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -c Release -v minimal
```

Clean9.05s/0warnings/errors. FINAL:500passed/0failed/0aborted in1999ms, PID44980/process2188ms/Exited0, both streams untruncated/no observer error. Assertion counts differ because the first run consumed a failed512Update wait; the final run completes the real workflow instead. No executable edits after FINAL.

## Executed physical proof
Four native point flows combine minute/half-minute timing with hiding either the carrier or its home target. A second vehicle prototype is authored as JSON before initialization, not cloned live, and its held placement avoids the active route. Its lower actual identity establishes a visible sorted prefix before the hidden carrier.

Ordinary hold/release and grouped home inputs establish actual native state without named fleets. Malformed, foreign and hidden previews agree with command admission. Hidden assignment, execution, operating inputs and home views return no live objects/facts. Mixed grouped previews expose only the actual authorized entry.

Real refused hold/home/group requests from both seats are saved in the pending inbox, loaded and executed normally. They preserve actual acquired job identity, cargo and home intent, including the earlier visible group member. Reveal permits home changes without commandeering the acquired haul.

When the home target stays unseen, physical delivery finishes and the idle worker returns normally to that actual home through the original engine loop. Player ReadHome still withholds derived readiness. Clearing the known own home intent is admitted without publishing the hidden old target; group commit uses the real private intent, so nullable preview state never becomes an execution dependency.

Finite source20 ends12/receiver8/empty carrier, with per-Update conservation. Retained worker facts remain old immutable observations. Saved hidden inbox/travel, final saves, twin hashes, input replay and separate sessions pass. Each physical wait is bounded512normal Updates; outer process90000ms/termination5000ms/output24000characters. No older/family/full executor ran.

## Scope and limits
Original221 gates remain193checked/28open; additional245. No compound gate closes. Other recurring/facility/aggregate work projections, all-provider/fault/performance/full P4.014/P3.008/P11.033 and whole goal remain open. The recurring caller's physical split is source/compile verified; this case specifically proves native unseen-home return, not a complete recurring-source provider matrix.

Profile metadata only:713registered/included,0excluded/long markers,0game suites executed. `dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --profile-info tests/Beep.SimEngine.Tests/testdata/framework-validation-profile.json`. No full-profile runtime pass is inferred.

All12Tycoon and sole-clock sources unchanged. No Oil/platform/Draw or business-rule implementation edits, new home/state owner, compatibility shim, staging, commit or push. Authorized ONE final full regression remains deferred until implementation finishes.

## Post-pass audit

1857 source anchors,1044 selected executable inputs and13 quickstart anchors validate without mismatch or post-pass executable drift. Inventory3027paths/1097Markdown/8793local links/0broken;18master rows/0malformed; diff check0 with34existing LF/CRLF warning lines;3015dirty paths/0staged. All221 original gate lines/literals/checkboxes remain unchanged (193checked/28open), additional245.

Eight existing production files revise only original owners/callers and command/view/module/identity boundaries; no new production file or worker/home/assignment/state/clock owner. Three new files were authored. Five existing changed source files and six existing changed executable inputs are newly explicitly anchored/frozen, plus the new driver. All12Tycoon andsole-clock hashes match the prior checkpoint. The driver boundary audit finds no raw World/resource/progress writes, TestKit or RunTicks. No full regression, staging, commit or push occurred.


