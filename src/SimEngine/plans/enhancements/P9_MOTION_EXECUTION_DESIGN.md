# P9-B6 — Authored coordinate-neutral motion execution

Status: historical focused evidence, not rerun during B7. Current resource-backed motion work is
[P9-B7](P9_MOTION_OPERATING_INPUTS_DESIGN.md).
Previously verified 2026-10-06. Build2.104.0/save64/motion execution1/motion view1/work view33/
movement system3/work system4. Worker execution1/WorkBoard22/existing payloads unchanged. **120 checks/
two exact suites** (67 contracts;53 physical), zero failures/aborts, clean build.
[Evidence](../../docs/migration/P9_MOTION_EXECUTION_VERIFICATION.md). P9/P1–P12 remain partial.

## 0. Problem and specification

Before this slice MovementSystem exposed SpeedModifier, a mutable Func with implicit unset behavior. Replace it with
named authored contracts. IMotion alone owns route/goal/coordinates/distance. No compatibility API.

## 1. Goals

Delete the callback; independently control motion and work; share rate/gate/native control/codec
implementation; prove private diagnostics and saved physical travel/hauls in two exact suites.
No compulsory worker, map, platform, genre, fuel formula, resource grant or Draw implementation.

## 2. Design and API

ExecutionRateSnapshot/Evaluation/Contracts share stable multiplier/gate composition and actual named
metadata/purity/applicability/signals/negative/overflow checks. Worker composition starts with actual
IWorker.WorkRate to preserve arithmetic ordering; motion requests dimensionless composition from identity.
ControlledExecutionDef<T>/ControlledExecution<TDef> alone implement explicit native settings, conditions,
disabled state and enabled/multiplier codec. WorkerExecution and MotionExecution are independent
specializations, not copied state owners. No old snapshot-name alias survives.

MotionExecutionContracts.Read requires actual live IMotion. Detached MotionExecutionView reports
entity/status/composed multiplier/original gates/providers/effective blocker. Zero multiplier yields
motion.zero-rate; worker or motion gate blocks freeze real movement. MotionModule registers native
motion_execution and private MotionView. Existing MovementSystem validates metadata before tick one
and live facts at update/checkpoint/restore, then advances ready moving providers once per movement pass.
Unassigned waits preserve actual goals. Work assignment checks motion only when remaining stops require
travel; stationary work remains eligible. Hauls use existing unavailable retry/cargo/claims, no new queue.

Native explicit operation controls use nonnegative scalar unit.speed-multiplier, separate from work.
Multiplier scales actual provider distance, not a coordinate-neutral base-rate/ETA/fuel formula.

## 3. Files and errors

ExecutionRates.cs/MotionExecution.cs/MotionView.cs; refactor WorkerExecution.cs; integrate Movement/
MotionModule/Work/current build, authored fixtures and public proof. Strict missing metadata/controls/
conditions/states/unit/motion/signals fail explicitly. Negative/mutating/malformed/non-applicable
nonidentity observations or multiplication overflow fault; no clamp or old reader. Decode stages fields.

## 4. Verification

Build and only these two exact suites; same-name repair reruns allowed:

- Motion execution: authored contracts, controls and private readiness
- Motion execution: timed travel, retained cargo and saved continuation

Prior suites compile/migrate only; no previous/third/family/full gate.

## 5. Risks and remaining scope

Scoped purity is not global rollback. Dynamic ETA/signal-to-rate formulas, broad provider/geometry/
route/automation matrices, physical fuel/wear/service/fleet policies, base authoring defaults,
combined-rate command refusal, replay/lockstep/stress/allocation/full P9/P1–P12 remain incomplete.
Public proof compiles without friend access; tests only author/submit/observe engine behavior.

## 6. Sequence

- [x] B6.1 shared contract/control refactor and native motion specialization
- [x] B6.2 movement/work/view/checkpoint integration and callback deletion
- [x] B6.3 two exact gates, public provider and saved physical proof
- [x] B6.4 current evidence/master/parent/README/AGENTS/help

## 7. Scoped results

Strict JSON/owner/signal binding, private frozen facts, normal controls/preview, zero/disabled/worker
readiness, stable public/native composition, five hostile providers, overflow, staged malformed/truncated
decode and pending command save pass. Non-worker motion advances normally. Physical point and native
tile providers retain actual positions/routes/goals through waits and saved continuation; speed changes
scale earned distance. Loaded hauls use existing retries and preserve native cargo/destination claims/
carrier/age, deliver once and conserve quantities. Stationary work completes with locomotion disabled.
Stages are bounded to200 ticks.38 prior identity assertion files, shared snapshot type and source caller
migrations compile only; prior suites not rerun. Condition damage/repair are public engine-contract
fixture inputs, not a new free gameplay repair command. No compatibility path or platform logic.
