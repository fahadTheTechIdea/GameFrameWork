# P9-R1 — Recovery policy contracts and interrupted network cargo

Date2026-10-14. Engine beep-simengine-2.590.0/save157/replay1. Existing persistent state layouts are unchanged. Work remains entirely in Beep.SimEngine; no Oil/Tycoon/Godot/Unity/Draw/host gameplay implementation or Git writes.

## Implementation

Deleted `LogisticsRecoveryProfileDef` and `RecoveryDestinationPolicyDef`. Native JSON registrations and external public-only content definitions implement the existing interfaces directly on `Def`. Resolution and quantized destination decisions remain in the original declarations. No-demand/quantum-gap strings are unchanged and now belong to the declaration that emits them, not a concrete content registration. Mechanical test catalog lookups use contracts, and the independently authored build expectation advances to2.590.0.

The existing original LogisticsService, WorkBoard, inventory, receipt, transporter, pool topology and clock owners are unchanged. There is no additional state model, recovery loop, compatibility alias, synthetic cargo or fallback supplier.

## Physical proof

One explicitly master-listed JSON document disables evacuation and enables demand-only cargo return. Two exact cases each run native/direct policy metadata; together they exercise four real provider/metadata combinations. A native rate-limited port fills a six-unit pipe from finite authored feed. Ordinary player hauling loads three units through its connected resource port. Holding the carrier interrupts the original job but retains physically loaded cargo. Releasing it without demand produces the authored no-demand blocker, not invented delivery. An ordinary receiving stock-policy command permits the engine to choose and execute one residual-return job. It moves, unloads one unit per authored minute and completes through its existing itinerary.

Every observed Update checks total18-unit conservation. Pending loading, retained-cargo wait, paused clock, acquired return and partial unloading saves continue through actual original codecs. Foreign recovery queues are withheld. Interleaved session and command replay identities are checked. Positive drivers only submit commands, drive Update/clock and read detached facts. Internal access in the new driver only asserts the selected immutable policy alias; it does not perform gameplay.

## All attempts

1. Targeted test/content compile exit0, zero warnings/errors,7.01s.
2. Independent geometry exact new case FINAL288/0,4236ms; bounded PID26600 exit0/4408ms.
3. Native geometry exact new case FINAL288/0,14292ms; bounded PID32460 exit0/14474ms. Session35013 awaited to terminal; no restarted process.
4. Existing recovery content-contract exact case initially97/3,831ms; PID11140 exit1/1015ms. Failures were independently authored old build2.589.0, obsolete hard-coded save154 and obsolete private-entity NotOwned expectation. Actual current save157 and concealment NotFound were already engine behavior. No runtime failure or physical stock discrepancy occurred.
5. Correct those existing assertions and advance authored protocol build; add an explicit zero foreign-cargo-facts assertion. No production or new physical driver edits after their successful runs.
6. Repair targeted compile exit0, zero warnings/errors,4.48s.
7. Same existing exact case FINAL101/0,954ms; PID39624 exit0/1136ms.

Final successful distinct check total677 (288+288+101). The two physical cases were not rerun after the test-expectation-only repair; their production/source/content path was unchanged. No hidden full/profile/family runs. All processes terminal. This is scoped evidence, not a full regression claim.

## Commands

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Tile cargo recovery contracts: independent geometry and authored residual return" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 6500
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Tile cargo recovery contracts: native geometry and authored residual return" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 6500
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Recovery content contracts: independent JSON return and evacuation preserve physical cargo" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 6500
```

## Review and remaining scope

Single responsibility: declarations author decisions, native definitions register JSON and the existing coordinator executes them. Contracts admit independent implementations without native-family inheritance. No duplicate scheduler, clock, cargo/claim authority or platform dependency. Search over current production/test C# finds no references to the deleted bases or old concrete blocker owners.

Mixed resources, actual destination destruction during delivery, dock congestion/egress, arbitrary hostile callbacks, wider replacement/replanning and global allocation/lockstep/fuzz/soak gates remain open. No original broad checkbox/count is changed; frozen A47 audit remains historical. Whole plan/goal and final publication are incomplete.
