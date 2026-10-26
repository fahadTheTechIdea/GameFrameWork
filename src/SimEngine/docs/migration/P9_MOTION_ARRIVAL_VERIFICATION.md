# P9-B9 motion arrival diagnostics — scoped verification

Date: 2026-10-06. Current workspace source, not a clean-HEAD or full-regression claim.
Build **beep-simengine-2.110.0**, save **65**, motion view **3**.
[Stage](../../plans/enhancements/P9_MOTION_ARRIVAL_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Ownership and implemented behavior

Optional public IMotionEstimate extends the actual IMotion owner. MotionTravelEstimate is detached
remaining ticks/unknown reason, not another goal/position/route/time authority. Stateless
MotionArrivalContracts supplies one pure actual-owner reader and wide raw-budget duration calculator.
MotionView.ReadArrival publishes private immutable entity/version/observed tick/status/remaining/
arrival/blocker facts. Missing/foreign entities return no object; explicit observer access is deliberate.
Non-motion refuses. Providers absent from the optional estimate capability report Unsupported; there
is no guessed straight-line duration, coordinate conversion or compatibility fallback.

Actual Moving estimates first apply existing MotionExecutionContracts readiness/rate/fuel blockers,
preserving their original typed cause. Actual Arrived reports zero/current tick without estimation.
Idle and Unreachable report typed unknown. Provider results must contain either positive Moving ticks
with no blocker or nullable unknown with an explicit blocker. Actual motion alias, entity version/
owner/tick/allocator and unchanged status are guarded around pure callbacks. Six public probes test
touching/status-changing/empty/negative/zero/both-result failures. Query violations refuse usable facts;
this does not claim global rollback, all-coordinate/service purity detection or session faulting for
every externally invoked read. Arbitrary callback exceptions remain explicit, not substituted values.

Native Mover reads only its actual current path index/position/goal and current NavGrid.CostFor entry/
corner costs. Unplanned or newly blocked routes return typed unknown; reads never FindPath, replan,
move or advance telemetry. TrySegment/TickBudget are shared by Advance and EstimateRemaining, rather
than copied physical formulas. Sum actual per-segment weighted Q32 costs in a wide intermediate and
ceiling once against the same representable physical tick budget. Zero budget and duration/arrival
overflow return typed unknown, never wrapped/saturated times or an int-sized world limit.

Independent public PointMotion reads its own actual goal/point position/access and shares its real
tick-budget helper between movement and estimation. It uses the same generic raw-budget calculator,
not a point-specific engine branch. Public UnestimatedMotion demonstrates a real optional-motion
provider without coordinates/forecast capability; it owns its own existing proof duration/state/Sync.
It is not used as a runtime fallback or another implementation of point movement.

Unused MovementSystem.HoursFor and NavGrid.PathCost had no source/test callers beyond each other;
their approximate static tile-duration path is deleted without an alias. Existing actual path search,
native movement/control/resource/job/map owners and codecs remain authoritative. No persistent ETA,
deadline, timer, route, fuel reserve, scheduler or view-state owner was added. Motion view3 identifies
the added public diagnostics; existing native motion/work/map/system payloads/schemas remain unchanged.

Current-route forecasts are conditional on current costs/rate/readiness/access. They do not reserve
future fuel, guarantee changing road access, replace actual movement, or promise exact arbitrary
diagonal fixed-point/congestion timing. Future-trip planning/formulas remain separate unfinished work.

## Final focused commands and results

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Motion arrival: contract estimates, private admission and provider guards"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Motion arrival: physical route timing, gates and saved continuation"
```

- Final build succeeded: zero warnings/errors, elapsed3.65 seconds.
- Contract estimates, private admission and provider guards: **47 passed,0 failed**,292ms.
- Physical route timing, gates and saved continuation: **56 passed,0 failed**,447ms.
- Total **103 checks,0 failures,0 aborted suites**, exactly two distinct full named suites.
- Only same-name expansion/final reruns. Earlier89-check results were superseded. No preceding,
  third, family or full suite ran; previous tests compile only, not a broader green claim.
- Test fixture initially attempted read-only layer index assignment, then the wrong coordinate
  overload. Corrected to the existing Set(TileCoord,byte) API. Each failed build was repaired and
  successfully rebuilt before any no-build test run; no stale executable gate was used.
- Final review shared native segment/budget math with physical execution and removed both unused
  approximate helpers. Rebuilt and reran only these same two complete suite names.

## Evidence covered

- Private missing/foreign/non-motion/observer admission; pure frozen facts, actual entity version/
  observed tick, idle unknown and actual moving point remaining/arrival ticks.
- Independent optional capability absence and six invalid/mutating estimate callbacks explicitly
  refusing usable facts; no provider mutation rollback or every query-fault save prohibition claim.
- Raw ceiling, zero budget, negative inputs,2^40 duration, unrepresentable raw duration and arrival
  addition near long.MaxValue. Actual master-authored point rate estimates beyond int.MaxValue and
  reconstructs/continues from sole physical provider state without billions of simulated ticks.
- Actual ordinary SetHome/ConfigureOperation commands recalculate current forecast after rate,
  enabled, zero multiplier, worker readiness and target edits. Disabled/readiness waits earn no
  physical progress. Saved changed controls/current point route continue identically.
- Point current-access loss reads unknown without changing actual status; normal spatial access
  command also produces real Unreachable status. Shared JSON leaves independent sessions idle.
- Real ordinary reserve policy and operating-input burn deplete actual motion fuel. The forecast
  then carries the original MotionExecution blocker, no remaining/arrival time or distance grant.
  Saved blocked route/fuel owners reconstruct the same reason and deterministic continuation.
- Native pending actual path remains unplanned/hash-identical after reading. Normal MoveVehicle
  performs real planning/movement. Current native route reads preserve path/position/age/hash.
- Actual native layer mutation blocks a remaining segment; the reader reports RouteChanged without
  replanning. Reopening it reconstructs forecasts exactly from the sole saved current route.
- Real point and straight native tile arrival match their current budget forecast; saved twins
  preserve physical hashes. One actual native diagonal case arrives within one tick of its current
  weighted continuous-route estimate, proving that scoped case rather than all rounding trajectories.
- Every staged physical wait is bounded150 ticks with tick/status/blocker/arrival diagnostics.

Fixtures author existing master-listed proof content and submit ordinary commands. Framework-only
inputs alter an actual spatial access flag/layer, set the actual Held flag to preserve a direct-motion
case, seed a boundary tick or request a native unplanned goal for a read-only probe. No host/test
implements route planning, movement, fuel consumption, scheduling, control rules or save state.

43 preceding build assertion files migrate2.109.0→2.110.0 and compile only, including WorkerHomeTests.
The new MotionArrivalTests build assertion is part of this gate, not a preceding migration. No older
schema assertion matched the changed motion view version; the composed registration is3 in source.

## Still open

Explicit base worker/movement authoring, dynamic signal-to-rate formulas, prospective route planning,
service/preferred routes/areas/congestion, future fuel budgets, arbitrary diagonal precision bounds,
full provider side effects/purity/rollback/replay/lockstep/fuzz/soak/allocations and full P9/P1–P12
acceptance remain open. No Oil/Tycoon/host/demo/platform/Draw work, size restriction or full test run.
The full framework goal remains active/partial; this closes only the focused P9-B9 slice.

## Source fingerprint at final gate (SHA-256)

| File | SHA-256 |
|---|---|
| `src/Beep.SimEngine/Kernel/MotionEstimate.cs` | `e52f5e80cfd667fd48c582603940502f97539bd57f67b91e6153ef1a8ac1e1e7` |
| `src/Beep.SimEngine/Standard/MotionArrival.cs` | `4b2a5e50d1f3bc66275e6e7ab9c2e1e1eb6970319656f387ee95f74d41d7496d` |
| `src/Beep.SimEngine/Standard/Movement.cs` | `6e18d621ffa63ba56da78028579bd03ae849e717fef8412a84a021f9dee7460a` |
| `src/Beep.SimEngine/Standard/Navigation.cs` | `9e847cc4dd561a95e6eca032213bed1eab378adbe2baa8dd3a5edfcac222fa24` |
| `src/Beep.SimEngine/Views/MotionView.cs` | `4f38e14d69fd1fa842a8251dabda33cc54949c88396b098b477328ceded57c9c` |
| `src/Beep.SimEngine/Standard/MotionModule.cs` | `25487715c3bb60d69ab6dff46c7ab8728ab41cdea472f2456142f80c366bf703` |
| `src/Beep.SimEngine/Kernel/SaveGame.cs` | `79fc58573bb334bdb16f452ab0f8e5414145c0804b440d627317b05927c66fc1` |
| `tests/ExtensionProof/PointSpaceProof.cs` | `f6867e6803769cce8b1c7e6b7c3b64790009947a2e2d746b532c50d29873b75f` |
| `tests/Beep.SimEngine.Tests/MotionArrivalTests.cs` | `0dc71765461561891959c1ae563210bdfeb29e0e2f2381d28f86240e4f082efb` |
| `tests/Beep.SimEngine.Tests/Program.cs` | `fe11fe166211b822d9978da8307dc8b2599a4e2829cd29769334ac7fd2c90a7d` |
