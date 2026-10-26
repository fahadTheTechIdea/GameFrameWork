# P9-B9 — Provider-owned travel estimates and engine arrival diagnostics

Status: focused implementation verified,2026-10-06. Full P9/P1–P12 remains partial.
Clean build;103 checks in exactly two named suites, zero failures/aborts.
[Evidence](../../docs/migration/P9_MOTION_ARRIVAL_VERIFICATION.md).

## 0. Problem

MotionView.ReadExecution exposes readiness/rates/fuel, but no current arrival estimate. IMotion owns
actual goals/positions; Mover owns its real route and point motion owns independent geometry. A host
must not infer routes, remaining distance, effective speed, waits or arrival times from rendered facts.
Unused MovementSystem.HoursFor/NavGrid.PathCost estimate static approximate tile costs instead of
actual current physical motion. Remove those parallel estimate helpers, not preserve them as fallback.

## 1. Goals

1. Optional IMotionEstimate on the actual IMotion owner supplies a pure current-route duration.
2. MotionView.ReadArrival returns private detached remaining/arrival ticks or an explicit typed blocker.
3. Actual readiness/fuel gates precede estimation; unplanned/changed routes are unknown, never guessed.
4. Native tile and independent point providers reuse one checked wide raw-budget duration calculator.
5. Demonstrate physical timing, edits/waits, purity/faults, save/load and isolated sessions in two suites.

## 2. Design

- Add Kernel/MotionEstimate.cs for the narrow IMotionEstimate contract and immutable estimate result.
  This capability must be the actual motion owner, not another route/position/progress store.
- Add stateless Standard/MotionArrival.cs: actual provider alias/pure/status/result validation, gate
  composition, wide duration math and immutable arrival facts. No scheduler, timer, codec or route copy.
- Mover reads only its current remaining path, current position and actual NavGrid.CostFor entry/corner
  costs. Pending route planning or a now-blocked segment returns a typed wait; read never plans/replans.
  Sum native per-segment weighted raw costs with a wide intermediate, divide by the same actual tick
  budget used by Advance, ceiling once. Zero representable travel budget and overflow are explicit.
  TrySegment/TickBudget are shared with Advance, so no second segment-cost/speed formula survives.
  Delete unused HoursFor/PathCost rather than retain a legacy static approximate-duration path.
- PointMotion implements the same public contract using its actual point goal/position/budget and
  current access. Public-only proof supplies fault instrumentation, not a second implementation.
- Current Moving estimates require positive remaining ticks; Arrived gives0/current tick without a
  provider callback. Idle and Unreachable have explicit unknown reasons. Provider absence is unsupported,
  never a straight-line fallback. Readiness/fuel blockers retain their existing original typed cause.
- Checked arrival addition returns an overflow blocker, not a wrapped/saturated deadline. Forecasts
  are estimates for the current route/rate/geometry only, not promises against future congestion,
  policy/terrain/fuel changes or continuous fixed-point rounding across arbitrary routes.
- Runtime data remains provider-owned/saved once. Optional capability is diagnostic, not compatibility.

Build2.110.0/save65; motion view3. Existing native motion/work/map layouts and system schemas unchanged.

## 3. API surface

IMotionEstimate.EstimateRemaining(World,Fix) returns MotionTravelEstimate(RemainingTicks,Blocker).
MotionView.ReadArrival(EntityId,out MotionArrivalView) publishes entity/version/observed tick/status/
remaining ticks/arrival tick/blocker, never World, route handles or providers. Hosts render these facts.

## 4. Files

| Action | File | Scope / risk |
|---|---|---|
| New | Kernel/MotionEstimate.cs,Standard/MotionArrival.cs | Narrow contract/shared stateless reader; medium |
| Modify | Standard/Movement.cs | Native current-route estimator only; medium |
| Modify | Standard/Navigation.cs | Delete unused approximate PathCost; low |
| Modify | Views/MotionView.cs,Standard/MotionModule.cs,Kernel/SaveGame.cs | Projection/schema/identity; low |
| Modify | ExtensionProof/PointSpaceProof.cs | Independent public estimator/fault probes; medium |
| New | Tests/MotionArrivalTests.cs | Two exact gates; medium |
| Modify | Tests/Program.cs, preceding build/motion-view assertions | Registration/compile-only migration |
| New | docs/migration/P9_MOTION_ARRIVAL_VERIFICATION.md | Actual evidence |
| Modify | master/P9/README/AGENTS/help | Honest source/evidence boundaries |

## 5. Error handling

Foreign/missing entities disclose nothing; non-motion refuses. Pending/blocked/unsupported/zero budget/
overflow estimates expose nullable unknown facts. Invalid provider results or mutating callbacks throw
instead of returning usable false forecasts. Scoped purity guards are not global rollback or all-field
provider sandboxing. Nothing is spent or reserved by a read.

## 6. Compatibility

Development-only clean revision. No old view shim, fallback coordinate model or legacy save reader.

## 7. Verification

Build standalone solution; run exactly these two complete suite names:

- `Motion arrival: contract estimates, private admission and provider guards`
- `Motion arrival: physical route timing, gates and saved continuation`

Cover idle/arrived/private/observer/frozen/pure facts, unsupported/faults, wide arithmetic/arrival
overflow/zero budget, native path planning and dynamic route/rate/access waits, physical point/tile
arrival and saved twins. Prior suites compile only. No third/family/full gate.

## 8. Risks

Native route scans are proportional to the actual remaining path; not a total CPU budget or map cap.
Forecasts do not reserve future fuel, promise future access or replace actual movement. Arbitrary
diagonal fixed-point trajectories may differ from a current weighted-distance estimate by rounding.

## 9. Out of scope

Future-trip route planning, dynamic signal-to-rate formulas, service/preferred
routes/areas/congestion simulation, full provider/replay/stress/allocations and full P9/framework gates.
No Oil/Tycoon/host/demo/platform/Draw or world-size restriction.

## 10. Execution order

- [x] Add contracts/shared private diagnostics/native and public estimators; delete unused static helpers.
- [x] Add authored/public/physical tests; migrate43 preceding build assertions only.
- [x] Build and execute only the two exact gates; repair within them.
- [x] Record evidence and update master/parent/guidance; keep overall goal active.
