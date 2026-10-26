# P4-WP1 — Work policy authority and public group verification

Date2026-10-13. Engine2.512.0/save154/replay1/work view101; physical payload schemas and sole clock unchanged.

## Original owner and public contract
WorkPolicyRules.CheckConfigure now requires player authority and checks healthy/live/actual/owned/current sight before settings or provider callbacks. Single handlers and typed previews forward to it. PreviewGroup canonicalizes target identity and shares that evaluator; hidden/foreign/missing entries have no policy settings. WorkView.TryReadPolicy uses the same current-sight authority and the existing policy owner’s observed read, which guards actual provider state and latches failures through the original observation owner.

ConfigureWorkPolicyGroupCommand and its handler live in the existing command file; the existing work module registers their ownership. Its sole Sync path snapshots/canonicalizes target IDs and immutable settings. WorkView.PreviewPolicyGroup forwards to the original selection rule. Original ConfigureGroup preflights all members, then commits through original providers; no named-fleet dependency, second policy store, duplicate scheduler, rollback, instant movement or compatibility entry point is added.

Two older foreign assertions migrate mechanically in WorkPolicyTests and FleetWorkPolicyTests. Their registered cases were not executed. Work view100-to101 and engine2.511-to2.512 explicitly identify the breaking behavior; save154 and physical codecs remain unchanged.

## Actual build and runtime
One Release solution build:

```text
dotnet build Beep.SimEngine.slnx --no-restore -c Release -v minimal
```

Clean13.53s/0warnings/errors. The solution maps its unused quickstart module to Debug; core/testing/proof/tests compile Release.

New exact runtime:

```text
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Work policy access: grouped private admission preserves physical cargo pause and saved continuation" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 24000
```

FIRST:286passed/0failed/0aborted in1876ms, PID10152/process2095ms/Exited0, untruncated/no observer error. Before final freezing, the new driver added an explicit actual hauling-job identity assertion during disabled-policy movement.

One focused assertion-coverage build:

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -c Release -v minimal
```

Clean7.31s/0warnings/errors. FINAL same-new-case:288passed/0failed/0aborted in2363ms, PID38492/process2563ms/Exited0, both streams untruncated/no observer error. No executable edits after FINAL. No older/family/full runtime ran.

## Physical proof
Both authored minute and half-minute flows use the existing point/resource/policy JSON/master composition. The carrier and a separate authored stationary worker consume the same native policy profile; one existing engine-side sight module hides only the carrier. No live entity cloning or driver gameplay/state writes occur.

Ordinary public group input changes both actual policy owners without selecting named fleets. Valid/invalid/foreign/missing/non-policy single and group previews agree with ordinary admission. During actual finite-stock hauling, a hidden carrier’s settings are withheld while the visible stationary worker still has detached settings. Real single/group inputs from both seats are saved in the pending inbox and executed after load. They refuse NotFound and change neither the visible prefix nor hidden policy.

Reveal restores ordinary group control. Disabling the loaded worker stops actual position change while game time advances; stock, assigned job identity and saved twin hashes are retained. Re-enable resumes actual delivery from source20 to source12/receiver8/empty carrier. Retained settings and roles remain immutable. Per-Update physical conservation, final saves, public input replay and interleaved-session isolation pass. Each physical wait has a512Update observer bound and typed diagnostic facts on failure.

The same selected factories also run the existing public resource supply/manufacture proof, verifying the policy addition does not replace work or production rules. These are helper-invoked public workflows inside one new exact case, not executions of older registered cases.

## Scope
Original221 gates remain193checked/28open; additional244. No compound original gate closes from this slice. Other work controls, other policy families, complete P4.014/P3.008/P11.033/provider/fault/performance/global/full requirements remain open.

Metadata-only profile info:712registered/included,0excluded/long markers,0game suites executed. Command: `dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --profile-info tests/Beep.SimEngine.Tests/testdata/framework-validation-profile.json`. This is not a full regression result.

All12Tycoon files and sole clock remain unchanged. No Oil/platform/Draw or business-rule implementation edits, compatibility shim, staging, commit or push. The authorized ONE final full regression remains deferred until implementation finishes; the whole goal is open.

## Post-pass audit

1849 source anchors,1037 selected executable inputs and13 quickstart anchors validate with zero mismatch or post-pass executable drift. Inventory3024paths/1095Markdown/8790local links/0broken;18master rows/0malformed; diff check exits0 with34existing LF/CRLF warning lines;3012dirty paths/0staged. All221 original gate literals/line anchors/checkboxes are unchanged (193checked/28open); additional244.

Six pre-existing production files changed in original owners; no new production file or policy/scheduler/state/clock owner. Three new files were authored. Three pre-existing changed files and one existing consumed policy JSON are newly source-anchored/selected-input-frozen, plus the new driver. All12Tycoon andsole-clock hashes match the prior checkpoint. The driver audit finds no raw World/resource/progress writes, TestKit or RunTicks. No full regression, staging, commit or push occurred.


