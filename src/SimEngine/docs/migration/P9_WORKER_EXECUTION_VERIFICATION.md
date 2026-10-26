# P9-B5 worker execution — scoped verification

Date: 2026-10-06. Framework only. Build **beep-simengine-2.103.0**, save **64**.
[Stage](../../plans/enhancements/P9_WORKER_EXECUTION_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Ownership and source changes

WorkSystem's mutable CanWork and WorkRate callbacks are deleted, not deprecated. No compatibility
facade or implicit callback fallback remains. IWorkerExecutionDefinition/IWorkerExecution declare named
metadata and immutable gate/multiplier observations. WorkerExecutionContracts reads stable named
providers through the existing shared gate reader, verifies actual identity/metadata/purity and composes
nonnegative multipliers with actual IWorker.WorkRate. Non-applicable observations contribute identity.
Unrepresentable multiplication throws explicitly; no clamping or manufactured rate is introduced.

Native worker_execution owns enabled/multiplier once, using existing IControllableOperation settings,
ordinary commands/previews and staged Sync. JSON requires explicit operation controls, conditions[] and
states.disabled. Typed conditions read actual named signal providers. State resolution belongs solely
to entity Update. WorkSystem remains the sole dispatcher/progress/retry/job owner; existing hauling
continues to own physical receipts/cargo/transfer. MovementSystem consults the same readiness before
advancing a real motion provider, without a path/coordinate/goal mirror. Work multiplier affects work
and handling only, not travel speed. Zero work rate does not invent a fuel or locomotion policy.

Current identities: native execution codec1, work view32, work system3, movement system2. WorkBoard22
and existing owner payloads remain unchanged. Work checkpoints/current restore validate live execution
providers. WorkView.ReadWorkerExecution is private, immutable and contains no provider handles.

Source callers in WorkerCandidate/ContinuousWork/ResourceRequirement/EmergencyPreemption/EmergencySupply/
HaulRetry migrate to authored ordinary controls or explicit public test providers. WorkRetryProofModule
removes its callback composition and redundant retry-operation registration. The independent execution
proof is an engine-side public provider compiled without friend access, not host/harness gameplay.
Fixture helpers only author JSON and submit ordinary inputs; they never implement a dispatcher or rate
calculation. The fixture execution fragment is explicitly compiled into the existing master-listed
points.json; the condition signal catalog is independently master-listed as execution-condition.json.

## Final commands and results

```
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Worker execution: authored contracts, controls and immutable admission"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Worker execution: physical work, cargo waits and saved continuation"
```

- Final build: **0 warnings, 0 errors**, 3.38 s.
- Authored contracts/controls/admission: **75 passed, 0 failed**, 296 ms, one exact suite.
- Physical work/cargo/continuation: **59 passed, 0 failed**, 343 ms, one exact suite.
- **134 checks, 0 failures, 0 aborted suites**. Only the same two full names ran during repairs.
- Initial runs found missing fixture seats and an incorrect assertion that an exhausted pickup receipt
  should survive full loading. Tests now distinguish retained remaining stock from consumed receipts.
  An additional catalog check rejected an invented native condition-unit alias; the fixture now authors
  the actual unit.condition schema, not a fallback. No runtime repair/compatibility path was added.
- 37 preceding engine-identity assertion files migrate/compile only. Existing callback caller suites
  also compile only; they were not run. No previous/third/family/full test run is claimed.
- Scoped tracked-file whitespace check and explicit new-file trailing-whitespace scan passed; Git
  reported LF/CRLF notices only. Source searches found no old2.102 test identity or removed callback
  assignment. Unrelated existing checkout changes were preserved.

Contract checks cover required controls/conditions/state/bands/unit convention, invalid binding and
non-worker refusal; private/frozen/detached views; ordinary command/preview control bounds; deterministic
multiple-provider composition; mathematical no-provider identity; five public-provider faults (mutation,
wrong capability, negative multiplier, null gate and non-applicable nonidentity multiplier). Current
checkpoints reject these providers and unrepresentable effective rates. Native malformed/truncated decode
stages all fields and leaves controls untouched. Pending ordinary commands restore and apply identically.
Actual condition signals refuse unsafe haul acquisition without allocating promises; saved damaged and
repaired observations use the real condition owner. Condition changes are public engine-contract fixture
inputs: this slice does not claim a new timed physical maintenance or preparedness scenario.

Physical checks submit ordinary activity/control commands and public engine haul execution. Real work
earns zero/rate-two/disabled increments correctly; ordinary reopening preserves earned node progress.
Partial loading, disabled retained waits, full loaded travel, zero-rate unloading and eventual delivery
keep native stock conserved, actual carrier/age/destination/cargo receipts intact and release exhausted
pickup receipts correctly. No teleport, second carrier or duplicate delivery occurs. Unassigned movement
keeps its real goal through readiness waits. Save/load/interleaved continuation hashes match; an isolated
session receives no cargo/controls. Every stage has a160-tick bound and current job diagnostics.

## Source fingerprints (SHA-256, relative to framework root)

```
f79601d3d3ac2691adcaf9eccefe78d525ba094c19e58f892ec3a7807f957922 src/Beep.SimEngine/Standard/WorkerExecution.cs
1925b61ec72ab404f085705e3fe258f104f348b5ec98a57a3ce4ceb9f4c2acab src/Beep.SimEngine/Standard/Work.cs
d3721d98a2cd561037f5053f8c05c21629257cc0cbca2bd8203b9d76c4f5e1da src/Beep.SimEngine/Standard/WorkScheduling.cs
8a37ea4ae793bea44405dc3d474fbf4498f76ce510405da65ae170ca49d310a7 src/Beep.SimEngine/Standard/WorkModule.cs
51fcc9dad0ed566e84c805ea73fb02e46f6b79a1315a147aa310875164a55fcc src/Beep.SimEngine/Standard/ExecutionGates.cs
8890a3bcd876dd615f4214ce73d5bea484e61b99477d8c859ee44a5f3ecfe90b src/Beep.SimEngine/Standard/Movement.cs
4608f4230e2cbe804ef3a097974f1e506218400cbc6f410956141df168187585 src/Beep.SimEngine/Views/WorkView.cs
7c7c66d5330fc0c6a827ec856b47391925320237f340f1ea06739fc6d89892e6 src/Beep.SimEngine/Kernel/SaveGame.cs
cdddbc26faeba0454d89e077dece3de9dcbbf40a1f8ba11565308caf1de5bfa4 tests/Beep.SimEngine.Tests/WorkerExecutionTests.cs
8ce41c5ff3ac39ab29b36e66d89556fd0396c583c0de097650d8c815080a2f83 tests/Beep.SimEngine.Tests/WorkerExecutionTestContent.cs
a28e9d1d1687c07134d2781f4ec78f267040974a8e24b0ea94154fcd7fbb1135 tests/ExtensionProof/WorkerExecutionProof.cs
c680e4343ca9ec075bbd63870e74636a9ae3194c5d7027ea26dde9df7c1bfc3c tests/ExtensionProof/WorkRetryProof.cs
163f401166fa966d08a50e8f07b573e169288245b0d7be54e730f358e187ac12 tests/ExtensionProof/content/worker-execution.json
8a9ad4c36d5ccad0d19817b9e06b0babd5856f42edb288a9fd329f90dbd5eb73 tests/ExtensionProof/content/worker-execution-condition.json
```

## Remaining scope

MovementSystem.SpeedModifier remains a separate authored-contract migration. WorkerDef's existing
base-rate default remains an explicit-authoring audit item. Native scalar unit.work-rate is not catalog
rate-unit conversion proof. Combined multiplier overflow faults at observation/checkpoint/update;
the ordinary operation preview does not yet provide a typed combined-rate overflow refusal. Purity
guards cover declared participants, not arbitrary cross-service mutations or transactional rollback.
Fuel burn, physical service dispatch, fleet minimum load/maximum wait/home/routes/areas/reserves/service
intervals, preparedness, broader provider/replay/lockstep/stress/allocation gates and full P9/P1–P12 are
unfinished. No Oil/Tycoon/host/demo/platform/Draw implementation or global acceptance claim.
