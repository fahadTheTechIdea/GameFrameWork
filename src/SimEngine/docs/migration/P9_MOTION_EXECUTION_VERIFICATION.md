# P9-B6 motion execution — scoped verification

Date: 2026-10-06. Framework only. Build **beep-simengine-2.104.0**, save **64**.
[Stage](../../plans/enhancements/P9_MOTION_EXECUTION_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Ownership and source changes

MovementSystem.SpeedModifier is deleted, not deprecated. Authored IMotionExecutionDefinition/
IMotionExecution providers return immutable ExecutionRateSnapshot observations. ExecutionRateContracts
alone handles stable named metadata, nonnegative multiplier composition, applicability and shared gate/
purity validation for work and motion. Work composition starts with actual IWorker.WorkRate, preserving
its previous fixed-point arithmetic order; motion composes dimensionless multipliers from identity.
No provider means no additional constraint, not an invented vehicle/base rate. Unrepresentable
composition faults explicitly; no clamp/default/compatibility callback is introduced.

ControlledExecutionDef<T>/ControlledExecution<TDef> alone implement native explicit settings/conditions/
state bindings, ordinary operation controls and staged enabled/multiplier Sync. WorkerExecution and
MotionExecution are independent capability specializations using this one implementation. Old
WorkerExecutionSnapshot is removed rather than aliased. Native speed uses scalar unit.speed-multiplier;
worker controls retain scalar unit.work-rate. Both require explicit controls/conditions/states.disabled.
No progress, position, route, condition, fuel or stock mirror is added.

MotionModule registers motion_execution codec1 and private MotionView schema1. Work view becomes33,
movement system3, work system4. Worker codec1/WorkBoard22/existing payloads are unchanged. Movement
validates actual metadata at composition and live observations at update/checkpoint/full restore.
Actual moving IMotion advances once per movement phase only when motion and worker gates are ready
and speed is positive. Unassigned waits preserve goals. MotionExecutionView is detached/private and
reports actual status, rate observations, original motion/worker causes and motion.zero-rate.

Work assignment consults motion readiness only when actual remaining destinations require travel.
Stationary work is not incorrectly stopped by locomotion control. Existing retained hauling translates
unavailability into its normal retry profile without a second queue/carrier/receipt owner. Actual
coordinate-specific motion implementations still own routes, physical distance, goals and active ages.

Native authoring fragments are compiled into the existing explicit master-listed points.json input;
the condition signal catalog is independently master-listed. Tile prototype authoring is explicitly
listed as motion-tile.json. Public-only ProofMotionExecution is an independent engine-side provider,
not native implementation inheritance or host gameplay; no friend access is added. Tests only author
fixtures, submit ordinary player/provider execution inputs and observe authoritative engine behavior.

## Final commands and results

```
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Motion execution: authored contracts, controls and private readiness"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Motion execution: timed travel, retained cargo and saved continuation"
```

- Final build: **0 warnings, 0 errors**, 3.78 s.
- Contracts/controls/readiness: **67 passed, 0 failed**, 302 ms, one exact suite.
- Timed travel/cargo/continuation: **53 passed, 0 failed**, 423 ms, one exact suite.
- **120 checks, 0 failures, 0 aborted suites**. Only these same two full names ran during repairs.
- Initial runs found a fixture declaring one seat while requesting two. Explicit scenario authoring was
  corrected; no engine fallback or seat-limit gameplay change was introduced.
- 38 prior identity assertion files migrate/compile only. Shared snapshot type migration and the earlier
  worker-suite coordinator identity assertion compile only. No previous/third/family/full gate ran.
- Earlier P9-B5 evidence is historical and was not rerun under the shared implementation.
- Scoped tracked-file whitespace and explicit new-file trailing-whitespace checks passed; Git reported
  LF/CRLF notices only.13 source fingerprints match. Searches found no SpeedModifier, old snapshot name
  or old2.103 test identity in current C# source. Unrelated existing checkout changes were preserved.

Contract checks cover strict required controls/conditions/state/minimum/unit/actual motion binding,
private immutable facts, command/preview bounds, independent work/speed controls and worker gates,
stable public/native ordering, mathematical identity and actual non-worker movement. Five public
provider faults cover mutation, wrong identity, negative multiplier, null gate and non-applicable
nonidentity multiplier; current checkpoints reject each. Composition overflow is observed/checkpoint
rejected rather than clamped. Native shared malformed/truncated decode preserves prior controls;
pending ordinary commands restore and apply with identical hashes. Real typed condition signals block
motion independently of worker readiness and restore from the actual condition owner. Condition changes
are existing public engine-contract fixture inputs, not a free repair command or a timed service proof.

Physical checks prove point-space travel earns exact half/full distances from authored public/native
composition. Zero rate and worker gates freeze position and active movement age while keeping actual
unassigned goals. Save/load resumes to the same destination. New travel-required haul acquisition
refuses zero-speed carriers without promises/IDs. Loaded hauls preserve carrier/age/cargo/destination
receipts through disabled waits, resume physical travel, unload once and conserve all native quantities.
An interleaved session receives no cargo. Stationary work completes with motion disabled and no movement.
Native tile routes use the same coordinator, freeze actual positions, retain paths/goals and complete
with identical saved coordinates/hashes. Each physical stage is bounded to200 ticks.

## Source fingerprints (SHA-256, relative to framework root)

```
3d742eaf5119885d8cef360e14bfa376e64562bb914287b54bf335bbc42e6638 src/Beep.SimEngine/Standard/ExecutionRates.cs
e65cc1634ebc334176b07ab4fa7f9b742e22caa4ef836863e367d50b4c75c26b src/Beep.SimEngine/Standard/WorkerExecution.cs
621f36a810d580f983789173e4d448c76c53c6b45abbc80409cc10697c7ac0e9 src/Beep.SimEngine/Standard/MotionExecution.cs
05099ae6142675989049eea1fae8a7eb58c8250756258200fa986c5dfe40aa4e src/Beep.SimEngine/Standard/Movement.cs
0fae2f7a1480bcf565c626081abe17d28ff6b2b3b9d7614cdf1c9c9c12ea467f src/Beep.SimEngine/Standard/MotionModule.cs
50cee1a41db0d14414798ee2932ec2c85dea67711e3a530895d66693d018a03e src/Beep.SimEngine/Standard/Work.cs
1e9bdf7dedf5c1f2e0278fcced348e18f1e0473b9d7e4cec394c87966fa2dc32 src/Beep.SimEngine/Standard/WorkModule.cs
03327747ca109eab75bf6fbc5b1429fd5633b8ee6fc94b1d6c358fcc7349b880 src/Beep.SimEngine/Views/MotionView.cs
af39ed9baf493c876b6cb250e6686e56ad7f6ae775d454c578337f8e70ef5266 src/Beep.SimEngine/Kernel/SaveGame.cs
680a34ce4fdd02ab469b7fd5285e55c2a2518a2ef77b00b67ebd514513802806 tests/Beep.SimEngine.Tests/MotionExecutionTests.cs
4b845282f184cc8d4dd4d6538c85881cd9dac89ba842506cdcc11cbceb4b7f38 tests/ExtensionProof/MotionExecutionProof.cs
9272299f8ee81c485e125972716d7a0893f8629d4a579d8e7200c7b028e28d43 tests/ExtensionProof/WorkerExecutionProof.cs
c0f85d305246030e57568c3ed21ab35570b998da8f26d7d43264f3c3a1214bcb tests/ExtensionProof/content/motion-execution.json
```

## Remaining scope

Dynamic ETA/travel cost and signal-to-rate formulas are not implemented here; actual spatial providers
retain existing cost/base-speed semantics. Per-provider base-rate multiplication overflow remains its
explicit arithmetic guard, not a portable rate/ETA proof. Combined composition overflow faults at
observation/update/checkpoint; normal controls do not yet have a typed combined-rate overflow refusal.
Native scalar multiplier units are conventions, not general catalog-unit conversions. Scoped purity
guards are not arbitrary cross-service mutation rollback. Base worker/movement authoring defaults,
physical fuel/condition wear/service/fleet/load/wait/home/routes/areas/reserves/service intervals,
preparedness, broader provider/replay/lockstep/stress/allocation gates and full P9/P1–P12 remain open.
No Oil/Tycoon/host/demo/platform/Draw implementation or full-regression/acceptance claim.
