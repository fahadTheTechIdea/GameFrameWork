# P9-B7 motion operating inputs — scoped verification

Date: 2026-10-06. Framework only. Build **beep-simengine-2.105.0**, save **64**.
[Stage](../../plans/enhancements/P9_MOTION_OPERATING_INPUTS_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Ownership

IMotionOperatingDefinition/IMotionOperatingInputs admit native and independent public named providers.
MotionOperatingInputs.Sync alone owns elapsed active-attempt exposure and last accepted tick, not a fuel
balance, remainder, route, job, enabled-state copy or resource ledger. Required id/inputs/states.disabled
are authored through the master-listed entity document and existing component registration.
ResourceRateSpec remains the sole strict resource/time parser. ResourceRates/QuantizedGameRates remain
the sole cumulative native-quantum calculators. Each elapsed interval is ceiling(after)-ceiling(before);
zero-draw intervals use already prepaid native quantum credit, never free invented resources.

MotionOperatingContracts uses the shared named/pure gate protocol and actual catalog/inventory capacity
metadata. All providers' next draws merge into one ResourceInventoryIntent before admission. Existing
IResourceTransaction checks whole real unreserved stock, consumes exact native quantities and publishes
the existing ResourceFlowFact accounting. Promised cargo cannot be burned. There is no second resource
store, reservation book or automatic supply scheduler.

The existing MovementSystem advances funded ready Moving providers once per movement phase, after
whole resource/exposure commitment. IMotion alone retains route/position/goal/distance. Rates describe
active movement attempts, independent of speed: a route failure/replan attempt can consume an interval
without earning distance. Idle, disabled, zero-rate, readiness and shortage waits consume nothing.
Native MotionView now includes detached company-private operating budgets; existing travel readiness
uses those same blockers. Entity.Update remains the only resolved entity-state committer.

New motion-input component1/motion view2/movement system4/work system5; existing native owner payloads
unchanged. Whole decoding stages both exposure fields. Current session chronology validates diagnostics,
normal updates, checkpoint and post-restore. Invalid/mutating reads, non-exact commits and stock/promise
mutation during exposure commitment fault explicitly. No global rollback of hostile provider mutations
is claimed. Unrepresentable next cumulative draws have a typed motion.operating-range blocker.

## Final focused commands and results

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Motion operating inputs: authored contracts and whole resource admission"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Motion operating inputs: timed expenditure, physical resupply and saved continuation"
```

- Build succeeded: zero warnings, zero errors, elapsed2.19 seconds.
- Authored contracts and whole resource admission: **63 passed,0 failed**,316ms.
- Timed expenditure, physical resupply and saved continuation: **56 passed,0 failed**,358ms.
- Total **119 checks,0 failures,0 aborted suites**, exactly two full named suites.
- Same-name repair reruns only. Initial contract proof incorrectly expected composition to observe
  runtime exposure; repaired to test the actual pure reader. A mixed-capacity fixture omitted explicit
  resource measures required by its limits; repaired authored mass measures, not engine fallback.
- 39 preceding identity assertion files migrate2.104.0→2.105.0; old motion/worker schema assertions
  migrate to movement4/work5. These earlier suites compile only and were not run.
- Scoped tracked-file whitespace check passed (ordinary LF/CRLF notices). No old2.104.0 identity,
  SpeedModifier or WorkerExecutionSnapshot remains in source/test C#.

## Proven scope

Required JSON fields, actual resource metadata/rational/discrete quantum/filter/motion/inventory bindings,
private immutable pure projections, stable multiple native named owners and independent public provider
composition, whole overlapping draws, retained outbound cargo receipts, five hostile provider modes,
staged malformed/truncated codec, rejected future checkpoints/default contexts and typed overflow pass.

Real point motion spends inputs over game time. Idle/disabled/zero/shortage waits do not spend stock or
grant exposure. Saved prepayment boundaries retain exact fractional credit. Shortages retain reached
position and original goal. An ordinary separate courier loads real stock, travels to the stranded
consumer, unloads and resupplies the same inlet; player shutdown keeps the consumer still during
unloading, then the ordinary operation command resumes its retained goal. No free refill, teleport,
host decision scheduler or input refund. Existing flow accounting records actual expenditure. Real
route-attempt semantics, mixed discrete/liquid rates and interrupted/interleaved session twins pass.
Every physical stage has a200-tick failure timeout with engine diagnostics.

## Remaining work

This is not a fuel-tank/cargo-selection or autonomous fleet-refueling policy. A motion-only consumer
and a separate ordinary courier prove physical resupply. Existing shared inventory/cargo behavior is
not silently filtered. Automatic refuel/service/reserves, cargo/tank selection, distance/idle/work
consumption profiles, base-authoring audit, dynamic ETA/signal-rate formulas, broad provider/geometry/
work-policy/fuelled-carrier matrices, replay/lockstep/fuzz/stress/allocation/full P9/P1–P12 remain open.
No OilFieldDays, Tycoon, host/demo, Unity/Godot or Draw code changed. Full goal remains active/partial.

## Exact source fingerprints

These fingerprints identify this scoped proof; they are not a full repository or full-regression claim.

| Source | SHA-256 |
|---|---|
| `src/Beep.SimEngine/Standard/MotionOperatingInputs.cs` | `aa4c85fe94dcb89dbb048dedd01dd7abd1971a93f7a3ed656d97f9bfb0617ac4` |
| `src/Beep.SimEngine/Standard/MotionExecution.cs` | `dfeadf3d99344f4a3979e4709709ca451663aa9a111230b75000174be2d3c0d1` |
| `src/Beep.SimEngine/Standard/Movement.cs` | `1a5fb48bf2fa002e5643c9917222d2683adf0e4975950b0e196b00244283da8a` |
| `src/Beep.SimEngine/Standard/MotionModule.cs` | `adf9a0ae5fd630f0ec89472e4cfec093d4acdf487bba95a2e724e93b553d1835` |
| `src/Beep.SimEngine/Standard/Work.cs` | `ce69c0371c83c0f08d9f2dad58d7abd54d781fe8313d8aa371e3011d341718f1` |
| `src/Beep.SimEngine/Kernel/SaveGame.cs` | `97160f4b3df94b04994684c233c5db528cd7f8e02ffa1def39156c0185e5fe8a` |
| `src/Beep.SimEngine/Core/ResourceRates.cs` | `7691c717993be8ecfe0449e8ea83fee4424b3f26c6557bf042ae0f3a055465b3` |
| `src/Beep.SimEngine/Content/ResourceRateSpec.cs` | `26aac823b4e3e6a9b9294ad809ef92c58a1bde8354459df8bf2c84b9d06b5137` |
| `tests/ExtensionProof/MotionOperatingProof.cs` | `12ee165e55e329fccf829d834e20cb38c1358b08b2e993fb7f3b3c01660404df` |
| `tests/ExtensionProof/PointSpaceProof.cs` | `e6ef8567dc933a61ac22c7882915e37d474790ca051b74d4190e2c639db5cb49` |
| `tests/ExtensionProof/ExtensionProof.csproj` | `4bc552847e5a09fd0dd70395942c1499fed456f0cb59f9f02da2ab82761397db` |
| `tests/ExtensionProof/content/motion-operating-inputs.json` | `d6c671b41f0d4838d7f0d37d378789008cf012da5724023b3ce9f4104708219b` |
| `tests/Beep.SimEngine.Tests/MotionOperatingTests.cs` | `0e28f309d598b69f03886de9b1a369cd539f3a8196a00b67404b9b6c10e6c6ac` |
| `tests/Beep.SimEngine.Tests/Program.cs` | `e89c8236ee5fefe57704878c913eec57e5bff912e3479302a5ceb9dc3615f8dd` |

