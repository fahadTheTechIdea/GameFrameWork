# P9-C7 worker home intent — scoped verification

Date: 2026-10-06. Framework only; current workspace source, not a clean-HEAD/full-regression claim.
Build **beep-simengine-2.109.0**, save **65**, work view **34**, work system **7**.
[Stage](../../plans/enhancements/P9_WORKER_HOME_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Ownership and implemented behavior

IWorker.Home remains the sole saved home identity. Native Worker replaces its public field with one
touching property backed by the same value. Its existing four-field Sync stages job/home/manual flags
and rejects negative identities before replacement; component1 and payload layout remain unchanged.
Independent public ContractWorker follows the same contract. No second home/fleet store, route, job,
stock balance, timer, component or compatibility reader was added.

Stateless WorkerHomeContracts supplies the same owned set/clear admission to normal SetHomeCommand
and WorkView.PreviewHome. Explicit None clears even a stationary worker. Self-home, negative/missing/
foreign endpoints and unsupported actual geometry refuse explicitly. A lawful future endpoint can be
recorded while operational policy/route/fuel blocks travel. Missing historical homes remain saved
intent with a typed blocker; the engine never infers another depot. WorkView.ReadHome returns private
detached intent/job/entity-version/state/blocker facts; explicit observer access remains intentional.

Pure reads guard the actual worker and selected target versions/owners/world tick/entity allocator.
Exact Home commit checks the new identity and entity touch, unchanged job/manual flags/motion status,
owner/time/allocation and selected target. These are scoped guards, not a global callback sandbox or
rollback guarantee. Public read-touch, ignored setter, untouching setter and setter-changing-job probes
exercise that boundary. Ordinary faulted Update cannot checkpoint a partially committed provider state.

Existing spatial providers alone calculate reach/routes and own actual movement goals. UpdateIdle
reissues actual approach intent so unchanged goals retain motion age, while changed/moving endpoints
retarget before the physical movement phase. Cleared/lost/foreign/unsupported/disabled/outside-area
automatic goals stop; actual motion/worker readiness and operating-input owners gate distance/fuel.
Real fuel depletion retains the actual goal without creating stock or distance. Entity.Update settles
the actual authored travel state after gameplay systems; hosts/tests implement none of these rules.

Acquired work and loaded cargo override home intent. Manual Held control cannot be commandeered by
home changes or automatic-work policy refresh; normal ReleaseVehicle ends direct motion first.
Residual cargo uses existing IHaulExecution.PostReturnCargo and real endpoint reservations/unloading
before the released carrier travels home. WorkBoard.Assign is the single acquisition boundary: it
stops prior idle motion before committing the job/worker relation, preventing one old-home movement
step in the tick before the new job's first toil. Ordinary activities, standing hauling and cargo
return share this owner; no home-specific assignment path exists.

MotionExecutionContracts.ReadStatus extracts the existing pure/enum-checked actual status read.
Stop verifies actual Idle and unchanged owner/time/entity allocation. Home, direct-control release
and assignment use it; ignored Stop faults actual engine Update instead of falsely succeeding.
Other historical Stop callers were not globally migrated. This is not a proof of all provider
coordinates, stock/cross-service effects or arbitrary faulty-provider rollback.

Worker1/WorkBoard22/movement4 and native owner payloads remain unchanged. Work system7 identifies
changed idle/assignment execution; work view34 identifies added home facts/admission. Save65 remains
the current layout. No older reader/conversion is preserved.

## Final focused commands and results

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Worker home: private intent, provider guards and canonical command admission"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Worker home: physical retargeting, protected work and saved continuation"
```

- Final build succeeded: zero warnings/errors, elapsed3.74 seconds.
- Private intent, provider guards and canonical command admission: **94 passed,0 failed**,298ms.
- Physical retargeting, protected work and saved continuation: **159 passed,0 failed**,508ms.
- Total **253 checks,0 failures,0 aborted suites**, exactly two distinct full named suites.
- Initial same-name results189 and expanded218 were superseded by these final gates. No third,
  family, preceding or full suite ran. Existing tests compile only; no broader green claim.
- One expanded test authoring build failed because JsonValue is indexed, not IEnumerable. Replaced
  fixture iteration with Count/index; rebuilt successfully before any no-build test run. No runtime
  fallback or weakened contract was introduced.
- Source review then exposed acquisition-tick stale home travel. Fixed the actual WorkBoard assignment
  owner and added normal new-job/residual-cargo/ignored-Stop proofs, rather than another home scheduler.

## Scope proven by those gates

- Native and independent public IWorker: owned command/preview parity, pure/frozen/private/observer
  facts, missing/foreign/non-worker/self/negative/unsupported refusals and explicit no-motion clear.
- Actual entity touch/exact setter, four getter/setter faults and ignored-Stop faults at home clear
  and ordinary work acquisition. Faulted Update save refusal is exercised at these boundaries.
- Staged negative/truncated worker decode retains all fields; existing policies/home and queued
  ordinary command restore/continue identically. Shared JSON does not share runtime home state.
- Real partial travel and entity travel-state settlement; same-home commands preserve provider age;
  mid-return retarget/clear, moved/removed/foreign homes, real closed/reopened route and operating area.
- Normal JSON activity acquired during home return stops its old goal on the acquisition tick;
  Home changes during acquired jobs preserve the real job and its destination. Saved jobs finish
  and only then physically return home.
- Real loaded standing haul delivers promised cargo before home; real unassigned residual stock
  acquires an ordinary return job before earning home distance. Both conserve endpoint totals and
  saved twins finish with actual empty carriers at their physical home.
- Actual retained motion fuel is consumed over physical time, not dispatched as surplus; depletion
  waits grant neither distance nor stock and saved twins retain the real blocked goal.
- Normal tile MoveVehicle/Held goal survives future-home changes and automatic-work disable/enable;
  normal ReleaseVehicle yields to current physical home rather than retaining the direct goal.
- Bounded250-step stage waits fail with tick/position/home/state/blocker/motion/job diagnostics.

The tests author JSON through existing master-listed proof documents and submit ordinary engine
commands. Framework-only fixtures move an actual provider endpoint, change an actual owner's identity
or despawn a lawful empty endpoint to exercise external world changes. They do not simulate movement,
hauling, scheduling, fuel, unloading, work or state transitions in a harness.

42 preceding build assertion files migrate2.108.0→2.109.0 and compile only. Four overlapping existing
system assertions migrate6→7 in MotionOperatingTests, MotionExecutionTests, WorkerExecutionTests and
TransporterReserveTests; they are not four additional migrated files or newly executed suites.
The new WorkerHomeTests identity check is part of this gate, not a preceding migration.

## Still open / deliberately not claimed

General home/service-bay docking, service dispatch/intervals, preferred routes, named tanks/holds,
proactive trip fuel budgets and full manufacturing/provider creation proofs remain unfinished.
Historical broad dispatch/preemption/dock/refill/provider fault matrices, global purity/transactional
rollback, replay/lockstep/fuzz/soak/allocation budgets and full P9/P1–P12 acceptance were not run here.
No OilFieldDays/Tycoon/host/demo/Draw/platform/adapters, hard spatial size limit or full test run.
The full framework goal remains active and partial; this evidence closes only P9-C7's focused slice.

## Source fingerprint at final gate (SHA-256)

| File | SHA-256 |
|---|---|
| `src/Beep.SimEngine/Standard/WorkerHomes.cs` | `01e37f294abd997a1c861b420d23475aa1b7ea183d5b32c24c14b3aaceee8a9a` |
| `src/Beep.SimEngine/Standard/Work.cs` | `1fd447c3f7404118f5d8f47946ae5a3fdea54eedb26501ed2aa0625f2d59633b` |
| `src/Beep.SimEngine/Standard/Capabilities.cs` | `32bdf2ecc4b24144b03f973546f2936ef0d1ae391c9a8dda94468b9cd2bd0ee0` |
| `src/Beep.SimEngine/Standard/StandardCommands.cs` | `beba22500ab5d66d84b2b761534df131ab31d07a436da2facd6f63781ee14d09` |
| `src/Beep.SimEngine/Views/WorkView.cs` | `9d048d33f47c3963c1f911c4415ac786118b999351960d96552c6343b29362a5` |
| `src/Beep.SimEngine/Standard/WorkModule.cs` | `72f1fca964bba142208af036c299082e801b448a060f000245cfaa21a6a5bee6` |
| `src/Beep.SimEngine/Standard/MotionExecution.cs` | `edf24b6627e6c4ba5b4ce811f49fe22369a0992b615bba9ab2b3a9f049c54647` |
| `src/Beep.SimEngine/Kernel/SaveGame.cs` | `8f62c2a9f86f91e79b097f8dba42b836b311da43836ec51cc0bbb7f1ee927666` |
| `tests/ExtensionProof/ContractCapabilities.cs` | `e6995e89bf00dc7db7525e52b1769cdd3a448d2d3ea8c678f07a129edfb40c03` |
| `tests/ExtensionProof/PointSpaceProof.cs` | `e374a6bba61322ffad776a6ea603fa443022516ad54e76e6b0d6237e443e3f7a` |
| `tests/Beep.SimEngine.Tests/WorkerHomeTests.cs` | `679e129dae8eb0b41368eae2145641b8d94943bf22337b4e972946ef26022acc` |
| `tests/Beep.SimEngine.Tests/Program.cs` | `60f44aa4225af1d1783ff26d7679052ba5cd606dd6e635de00a0dadb7a6b7b74` |
