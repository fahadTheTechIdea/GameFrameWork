# P9-B10 worker specifications — scoped verification

Date: 2026-10-06. Current dirty-workspace source, not a clean-HEAD/full-regression claim.
Build **beep-simengine-2.111.0**, save **65**, work view **35**, work system **8**.
Worker1, WorkBoard22, mover/motion/map and existing native payload layouts are unchanged.
[Stage](../../plans/enhancements/P9_WORKER_SPECIFICATIONS_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## One authored authority

IWorkerDefinition extends the existing component-definition contract and owns positive authored
WorkRate and immutable declared Roles. Native WorkerDef and independently implemented public
ContractWorkerDef implement it. IWorker.WorkRate and native/public runtime getters are removed,
without alias, alternate loader or compatibility facade. Runtime IWorker continues to own actual
job/home/held facts; existing execution providers own modifiers/readiness, never another base ledger.

Native worker.work_rate is required and positive. Native mover.class is required/nonblank and must
resolve through the actual selected navigation service. Native mover.speed remains required/positive.
Native WorkerDef/MoverDef and public proof ContractWorkerDef reject repeated Read/Resolve, preventing
post-load re-authoring of these actual specifications. This is not proof of all external-definition
immutability or an arbitrary plugin sandbox.

WorkerExecutionContracts validates matching definition/runtime types, actual live/session/entity
alias, positive base rating and nonempty unique stable catalog roles. Before Initialize, composition
refuses missing metadata, metadata on a non-worker and invalid declared ratings/roles. Live reads
check actual roles in declaration order, Can(None)==false and Can(declared)==true under existing
entity/tick/owner/allocator purity guards. Canonical Can additionally checks the requested role
against the authored set and pure actual provider answer. Assignment, existing assigned-machine
restore checks, candidate dispatch and private idle counts use it; no second worker selector exists.
The guard checks requested eligibility, not every possible unknown provider input in advance.

Read composes existing execution providers with that same base rating. Private detached
WorkerExecutionView exposes BaseRate and read-only copied Roles alongside actual effective rate,
gates and existing provider facts. The public worker-rating requirement reads canonical BaseRate,
not a runtime mirror or temporary multiplier. Normal operation commands change only effective
execution controls. These facts are derived, never another codec/state/model/settings store.

Nine JSON fixtures and two C# authored JSON documents were revised in place: eight explicit
work_rate1 fields and fifteen explicit class land fields. Existing explicit rates/classes remain
untouched. No runtime/test helper silently fills missing base rates or movement classes. Prototype
fingerprints change through normal manifest content. Only the selected fixtures have physical proof
in this gate; other migrated fixture workflows are static/compile-only, not a regression claim.

## Final commands and results

~~~powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Worker specifications: explicit JSON, definition contracts and private facts"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Worker specifications: authored physical rates, role guards and saved continuation"
~~~

- Final build succeeded: zero warnings/errors, elapsed3.18 seconds.
- Explicit JSON, definition contracts and private facts: **144 passed,0 failed**,433ms.
- Authored physical rates, role guards and saved continuation: **147 passed,0 failed**,461ms.
- Total **291 checks,0 failures,0 aborted suites**, exactly two distinct complete suite names.
- Same-name repair/expansion/final reruns only. Earlier249/269-check totals are superseded.
  No preceding, third, family or full suite ran.
- Initial test compilation used nonexistent node/progress member names; repaired to actual Id/
  AdvanceUnbounded. A tile fixture then referenced an unspawned warehouse; repaired to an explicit
  initial tower fixture. Each no-build run followed a successful build. The physical suite's early
  abort was repaired and superseded, not hidden as a passing full run.
- 44 preceding build assertion files migrate2.110.0→2.111.0 and four overlapping WorkSystem
  assertions migrate7→8; compile only. No preceding behavioral suite was rerun.

## Covered behavior

- Native/public missing/null/string/bool/zero/negative/unrepresentable/sub-quantum rates refuse.
  Exact0.5/1.5/maximum-positive ratings use actual definitions, pure reads and manifest saves.
- Native class missing/null/blank/nonstring/undeclared and invalid speed refuse without land fallback.
  Repeated native/public Read/Resolve refuses and leaves the actual authored values unchanged.
- Five public metadata faults refuse before Initialize. Declaration-only probes reuse actual worker
  type or metadata-on-non-worker; they add no alternate worker runtime/scheduler/rate owner.
- Native null/string/empty/duplicate/unknown role sets refuse. Foreign private/non-worker/cross-session
  handles refuse. Actual metadata and view role collections are read-only, retained facts frozen.
- Ordinary controls scale effective rate while authored base remains unchanged. Native/public
  candidate requirements report base rating even when the effective multiplier is0.5; reads allocate
  no job/progress. Existing sole manifest/control payload restores that distinction.
- Native and independent public point workers at0.5/1.5/2 travel physically, earn no remote work,
  advance exact whole/fractional work per Update, pause at zero effective rate, retain saved progress
  and resume through ordinary controls. Interleaved untouched sessions receive no jobs/activities.
- Native stationary worker needs no invented spatial/motion capabilities and performs timed self work.
  Native tile worker with explicit land/1.5 rating physically arrives, works and continues from save.
- Five public runtime role/Can/purity faults refuse facts and fault normal Update; those faulted sessions
  cannot checkpoint. False acceptance of a different declared catalog role refuses canonical query/
  eligibility and faults normal dispatch before assignment.
- All physical stages are bounded220 ticks with tick/node/progress/blocker diagnostics on timeout.

Fixtures author master-listed JSON and submit ordinary PostOrder/ConfigureOperation commands.
Initial test scenario entities are created through the actual engine setup API; tests implement no
rate composition, movement, work scheduling, reservations, control rules or saved gameplay state.

## Limits and remaining roadmap

Query callback mutation is not rolled back globally. Purity guards detect the existing actual entity/
tick/owner/allocator boundaries, not all hidden service/coordinate/global side effects. Arbitrary
provider exceptions and post-composition changing external metadata remain wider contract gates.
Specification projection allocates detached lists; stress/allocation budgets are not verified here.

Arbitrary base-rate units/formulas, combined effective-rate command overflow admission, future trip
budgets, service/preferred routes/areas/congestion, named holds, full provider purity/rollback/replay/
lockstep/fuzz/soak/allocation and full P9/P1–P12 acceptance remain open. Explicit native authoring and
the matching worker specification contract are closed only at this slice's scope.

No OilFieldDays/Tycoon/host/demo/platform/Draw change, artificial size cap, compatibility reader,
duplicate worker store or broad test run. Full framework goal remains active/partial.
The lifecycle skill kept implementation, stage, master and scoped verification aligned.

## Source fingerprint at final gate (SHA-256)

| File | SHA-256 |
|---|---|
| src/Beep.SimEngine/Standard/Capabilities.cs | 8A283F4C19B3AE8F4A5CC16ACF05F0DDF1AF515CC3719826D3AD39690852E329 |
| src/Beep.SimEngine/Standard/Work.cs | 2E125A008FCF680F5D5806E6B5558975F277354B3F9EDFF1119E7B23A617FC4B |
| src/Beep.SimEngine/Standard/WorkerExecution.cs | 93AF4D56953255155D8B1BD57C45890740601991574E7F08F4DF6D2FFB62B587 |
| src/Beep.SimEngine/Standard/Movement.cs | 907992E48D7A56D40D58546AFF3EA1490AD43684F84239A244AAD174467B5F2C |
| src/Beep.SimEngine/Standard/WorkModule.cs | B611CF117EB0F13AC6F8C640FD49B0CDC74B9FCD91979DE8AFBF03F10F7FE701 |
| src/Beep.SimEngine/Views/WorkView.cs | 122DE338C7C1E68EB2AB5A9809C606298FF1CA2B5475BD08D58B3D76C5461B21 |
| src/Beep.SimEngine/Kernel/SaveGame.cs | 60169F79474CEDE1FDF8695BBA2BEFEA1A12EA6202CE05ABCEAE7751FDAE1DE8 |
| tests/ExtensionProof/ContractCapabilities.cs | F117E278EDFBCA388122B5E4227288D80209F7FA3E1F0E61732896001E6AEEA5 |
| tests/ExtensionProof/ContractRequirements.cs | 6F8895ECB0CA78F9570B509A3E03C2BDF46542A3A5A85F4C4E6C88C03D20539A |
| tests/ExtensionProof/PointSpaceProof.cs | 237BC7D30704E145D1DFA12B1F2FC0A4C764B634B13AB4AAEF0EB957CF63BC22 |
| tests/Beep.SimEngine.Tests/WorkerSpecificationTests.cs | 4ACDEB262D93ECE606B155731787BF5319181436DB5DCDED3ED3D3E56060B19A |
| tests/Beep.SimEngine.Tests/Program.cs | B36A1B07225C199E18A85FDB44116F389B772C425EE6AA5975A74BB4714FDA15 |

