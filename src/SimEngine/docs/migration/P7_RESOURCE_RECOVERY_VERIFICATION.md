# P7-C3b2b2 protected removal and resource recovery — focused verification

## Current M78: recovery geometry observation and classified packing — 2026-10-22

Native **2.724.0/save161/replay2**, master HEAD
`cb61483ab81404c11944931aedb4703ba694edb5` plus preserved dirty/untracked work.
The whole goal remains active: original221/210 source-recorded checked/11open; no original
requirement closes. This extends the original ConstructionService, not a new recovery subsystem.

### Confirmed defect and correction

Recovery called selected geometry directly during placement capture/checks and container identity
verification. A callback could mutate the source/clock or throw and still leave a directly invoked
recovery/preview without a terminal fault. The first exact negative run records36/16. Fourteen
contexts injected the intended source/clock/throw behavior; the other two attempted to find a newly
created container through the settled enumeration and did not perform their intended Touch. Those
two instrumentation errors are retained, not described as valid mutation reproductions. The final
probe captures the actual entity supplied to ConfigurePlacement before touching it during capture.

ConstructionService.ReadRecoveryGeometry delegates to the existing EntityObservation.ReadProvider
with the actual source/container codec and selected space. Every runtime recovery capture, placement
check and placement-identity callback verifies before accepting its result, including refusing
preflight, the second actual source capture and each newly created container. CheckEvacuation and
CheckYield require a healthy session before returning an empty/otherwise successful result. Actual
packing failures latch through World and rethrow the original exception; first-fault ownership is
unchanged. ConfigurePlacement remains a mutating spawn-lifecycle callback, not a purported pure read.

The partial-checkpoint hypothesis was **not** confirmed. The initial16/2 operation result failed to
reach its between-container probe for the same pending-enumeration reason. Corrected instrumentation
passes16/0 on the old2.723 engine: EntityStore's existing lifecycle admission already blocks partial
save/hash. No new operation scope, coordinator, persistent flag, ledger or clock was added.

Existing stock still uses CommitMovement/StockTransfer; authored structural yield still uses
CommitInventory/Produced with its original cause. Packing capacities, physical hauling, authored
timing and material quantities are unchanged. Only ConstructionRecovery.cs and EngineInfo.Build
change in the450 native sources. Strict build identity changes to2.724; no old-save header rewrite,
compatibility path or codec change.

### Exact checks and evidence

Artifacts: [M78](../../artifacts/optional-module-durability/94742d8250b64825b807f0f13bc81a3e/),
[attempts](../../artifacts/optional-module-durability/94742d8250b64825b807f0f13bc81a3e/attempts.json),
[tested build inputs](../../artifacts/optional-module-durability/94742d8250b64825b807f0f13bc81a3e/build-3-inputs.json),
[typed caller audit](../../artifacts/optional-module-durability/94742d8250b64825b807f0f13bc81a3e/compiled-caller-audit.log),
[original gates](../../artifacts/optional-module-durability/94742d8250b64825b807f0f13bc81a3e/original-gates-current.json),
[final frozen inputs](../../artifacts/optional-module-durability/94742d8250b64825b807f0f13bc81a3e/final-inputs.json),
[structural checks](../../artifacts/optional-module-durability/94742d8250b64825b807f0f13bc81a3e/structural-checks.json).

Three targeted Release builds succeeded with0warnings/0errors:12.37/8.24/8.63seconds. Build1/2 are
diagnostic2.723 images; build3 is the corrected2.724 tested image. Build2 binaries were not separately
captured; do not fabricate their hashes. All three actual build logs and diagnostic failures remain.

| Final literal on build3 | Passed/failed | Suite/observer ms | PID/exit |
| --- | --- | --- | --- |
| Recovery observation: actual geometry capture placement and container checks reject mutation and preserve first faults | 116/0 | 334/473 | 25684/0 |
| Recovery operation: whole stock packing and authored yield reject partial checkpoints and retain classified quantities | 16/0 | 330/466 | 28368/0 |
| Resource recovery: guarded removal, live stock and cancelled-site continuation | 89/0 | 582/723 | 10876/0 |
| Resource recovery: timed construction, native salvage and saved physical delivery | 89/0 | 2129/2269 | 15328/0 |

Final **310/0 in four exact cases**, not a full regression. The two new cases deliberately call
lower-level contracts and inject hostile callbacks: protocol proof, not public-player acceptance.
Sixteen observation contexts span fresh/restored sessions and eight injection positions, with original
exception identity, first-fault cutoff, save/hash refusal, no time advance and saved-session isolation.
The packing probe verifies actual between-container checkpoint refusal and final saved identity for
existing20 stock versus eight authored additional units. The older physical case earns native/point
construction, demolition and real delivery over game time, but also contains explicitly injected
leftover stock/third-party promises; it is not relabelled wholly public-only. No replay or allocation
measurement was run here; earlier results retain their actual images.

Build command: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`.
Each runtime uses the actual test DLL, `--bounded-exact` and the literal above, with
`--timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 16000`.
The read-only compiled audit matches450 sources/PDB checksums,2936types,12923 IL methods,
344935instructions,137 typed calls,14 resource primitive writes/eight spatial-layer primitive writes,
and zero unknown/unmapped/indirect sites. This is compiled structural coverage, not a whole-resource
accounting certificate or arbitrary CLR sandbox. Tested native SHA256:
`4464979510172B15F170A434D10CAAD5DD04A2637DFCED7737BDAA14B0BB1126`.

### Self-review, operational boundary and remaining work

Self-review only. World.LatchFault and EntityObservation remain the diagnostic/first-fault owners;
existing typed refusal results, resource-flow facts, journal and notifications retain their roles.
No logger, financial rule or notification sink was introduced. Original exceptions are rethrown,
not swallowed; checkpoint-denial catches exist only in explicit negative test instrumentation.
Shared operational assurance remains with
[the existing matrix](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage).
Protected retention/correlation/channel-outage guarantees remain broader unqualified work; UI is
not applicable to this headless change. No dependencies, adapters, Draw, consumer source/run or
business-rule changes. Existing supplier protocol coverage is not financial qualification.

Next finite semantic review: spatial opening quantities versus resource-free metadata/terrain, and
the separately scoped optional financial resource callers. Broader C3/P8 fault/accounting/stress,
conditional lot/quality and original P6/full-plan gates remain open. No full/default/profile/family/
soak was run or authorized, including final validation. No staging, commit or push before full goal.

Earlier continuation: [M66 compound logistics admission](P9_ATOMIC_HAUL_BASELINE_VERIFICATION.md#current-m66-compound-logistics-operation-admission--2026-10-22)
on2.721/save161/replay2 qualifies the direct supplier claim/release group identified below, through
the same logistics owner and a shared transient scope primitive. No business-rule change. The M65
recovery results below retain their actual2.720 image and are not rerun/relabelled current.

<a id="current-m65-whole-movement-packing-and-compiled-caller-census--2026-10-22"></a>

## Earlier M65: whole-movement packing and compiled caller census — 2026-10-22

Native **2.720.0/save161/replay2**, `master` HEAD
`cb61483ab81404c11944931aedb4703ba694edb5` plus preserved existing work. Full goal remains active;
original221 requirements remain210 source-recorded checked/11open, not current full-regression
certification. [Existing C3 design/remaining obligations](../../plans/enhancements/P7_OWNED_RESERVATIONS_DESIGN.md#current-c3-audit-m62)
and the master tracker remain authoritative. No additional plan/tracker or resource owner.

### Implementation and responsibility

The compiled census found ConstructionService.Pack manually acquiring two endpoint receipts before
the single-transfer entry. Its actual-stock branch now calls existing `IResourceTransaction.CommitMovement`
with the original endpoints, requester, company and exact quantized bill. That coordinator owns
validation, acquisition, verified transfer and unfinished-operation admission; no receipt pair is
acquired outside it. Structural yield still uses CommitInventory/Produced and its demolition cause.
Placement, source-local containers, splitting, balances/books, hauling and game time are unchanged.
Only ConstructionRecovery.cs and EngineInfo.Build changed in450 native sources; codecs161/replay2
are unchanged. This is source-supported consolidation, not an invented old-engine gameplay RED.

The older recovery fixtures were incomplete: unused supplier without its optional module; crane
without hauling capability; no selected automatic-recovery policy. The shared PhysicalRecoveryTestContent
owner now accepts selected world/flow documents and explicit automatic-recovery selection. New
master-listed startup JSON reuses existing depot/crane/truck/factory prototypes—no duplicate vehicle
or hidden hauling ability. Physical recovery stays resource-only; only the supplier-promise protocol
case selects the existing optional Tycoon capability. Independent point physical cases select the
existing recovery module/profile through their existing composition helper. No business rules changed.
The invalid discrete-salvage probe edits parsed JSON instead of a whitespace-sensitive string.
Failure diagnostics include current jobs; original1200/720-update and60000ms process bounds remain.
Original deliberate inventory effects/foreign promises remain **protocol probes**, not public acceptance.
There is no engine fallback, instant movement, new balance or test-owned gameplay algorithm.

### Compiled call-site matrix

[AuditResourceMutationCalls.ps1](AuditResourceMutationCalls.ps1) traverses native managed method bodies
including generated code, resolves typed calls/references and primitive field writes, and maps their
PDB source locations. It requires explicit build inputs, matching tested DLL hash, exact source
set/hashes, PE/PDB identity and SHA256 source checksums. Unknown public contract methods, unresolved
members/invalid IL and unmapped calls fail. It reports indirect/reflection invocation separately.
It composes no game and runs no tests. Final `audit-4.log`: **450sources/450PDB checksums,2923types,
12885methods with IL,344004instructions,111calls,14field writes,0unknown/0unmapped/0indirect**.
All14 writes remain inside their primitive owners: ResourceInventoryState11/ResourceReservationBook3.
This is typed compiled-call coverage, **not** every semantic resource mutation, arbitrary CLR safety,
every provider runtime or a whole-session accounting certificate. Spatial layer sample writes and
non-resource-contract callers require their own existing-owner review. A method reference is recorded
as a reference, not counted as an executed transfer.

| Reviewed group | Standard files (call counts) | Disposition |
| --- | --- | --- |
| Recovery2 | ConstructionRecovery2 | Whole movement for stock; attributed inventory yield for structural salvage. |
| Inventory28 | Inventory15, ResourceInventoryState13 | Facade delegates sole backing state/book; initialization/codec explicitly classified. |
| Coordinator24 | ResourceTransactions13, ResourceTransferBatches5, ResourceConversions6 | Existing verified mutations/facts; M64 admission evidence retains its actual2.719 image. |
| Logistics13 | Logistics10, HaulResourcePorts1, LogisticsRecovery2 | Physical claims/handling use coordinator; purchase-only ReserveSupply/two ReserveIn and EndHaul ReleaseSupply remain direct. |
| Work/inputs7 | InventoryWorkEffects2, OperatingInputs1, WorkResources2, WorkStart2 | Existing requirement/effect owner delegates; earlier M56–M58 evidence not rerun/relabelled. |
| Spatial12 | SpatialResources12 | Original cell endpoint owns book; sample/layer accounting review remains open. |
| Networks6 | Networks5, NetworkFlow1 | Original shared pool/transfers/codec; M60–M61 retain earlier images. |
| Transformations12 | Transformations1, TransformationsTools1, TransformationsWip6, TransformerFlow2, TransformerOperatingInputs2 | Original WIP/tools/ports; full accounting/provider gates remain open. |
| Optional Tycoon6 | Tycoon/ResourceSalesSystem1, Tycoon/Suppliers5 | Read-only census of original sale transaction/supply book; no business edit/runtime claim. |
| Upgrade1 | Upgrades1 | Original movement owner; typed reference-owner matrix remains open. |

`caller-dispositions.json` assigns all111 calls to exactly one group with path/line/caller/target/IL
offset. Earlier `audit-3.log` is2.719/113calls/344021instructions; final2.720 removes two net endpoint
calls. Two invalid-manifest probes refuse wrong DLL/source SHA without altering source or binaries.

### Verification, images and retained failures

Artifacts: [M65](../../artifacts/optional-module-durability/9e4e7b2f7baa4be9af9820285ba96fac/),
[attempts/images](../../artifacts/optional-module-durability/9e4e7b2f7baa4be9af9820285ba96fac/attempts.json),
[current census](../../artifacts/optional-module-durability/9e4e7b2f7baa4be9af9820285ba96fac/audit-4.log),
[caller dispositions](../../artifacts/optional-module-durability/9e4e7b2f7baa4be9af9820285ba96fac/caller-dispositions.json),
[frozen inputs](../../artifacts/optional-module-durability/9e4e7b2f7baa4be9af9820285ba96fac/final-inputs.json),
[original gates](../../artifacts/optional-module-durability/9e4e7b2f7baa4be9af9820285ba96fac/original-gates-current.json),
[structural checks](../../artifacts/optional-module-durability/9e4e7b2f7baa4be9af9820285ba96fac/structural-checks.json).

Five targeted Release builds succeeded0warnings/0errors:13.75/15.09/8.27/8.03/5.49seconds.
Nine literal runtimes retain five failures: unused supplier0/1; missing carrier1/1;
invalid-salvage authoring88/1; native missing recovery policy21/1; point missing recovery policy47/1.
These are fixture/configuration failures, not demonstrated native accounting regressions. No bounds,
expected totals or engine capability guards were weakened. Removal also has an intermediate89/0.

| Latest literal | Passed/failed | Suite/observer ms | PID/exit | Image/scope |
| --- | --- | --- | --- | --- |
| Permanent removal: authored physical worker loss and partial-haul endpoint retirement preserve state | 311/0 | 7703/7864 | 25776/0 | build1; six actual public replays |
| Resource recovery: guarded removal, live stock and cancelled-site continuation | 89/0 | 619/762 | 13328/0 | build4; protocol probes/point admission |
| Resource recovery: timed construction, native salvage and saved physical delivery | 89/0 | 2239/2378 | 25340/0 | build5; native/point saved physical continuation |

Latest scopes **489/0/six actual replays**, across captured images—not a final-image full regression.
Public311/0 was not rerun after recovery-fixture-only edits; removal was not rerun after final point-
physical-only edits. Native DLL unchanged across these builds:
`A1D108EDA5EA142A207E5DFD790513541D49404F8A79B27DC6A92D55227AA24A`.
Final proof `D726DB349EE9609A33613EFDEF185E7ADE77593D646E2BA79F8AA196834725F1`;
tests `48AB6DC1ACDE2849EC391BB8E24367672D8FE84F8FDBDCDD3DC25F0F41B718DB`.
Build1/3/4/5 captures retained; build2 test/proof hashes were not captured before edits—not fabricated.
The erroneous first build3 capture guessed a wrong proof name/null hash; corrected capture is separate.
Audit1 failed identity (redirected log empty; no fabricated error output); audit2 failed18 initially
unclassified reviewed helper sites; audit3 succeeds2.719/audit4 succeeds2.720. Reviewed pure static
helpers and initial-stock initialization are now explicitly classified; unfamiliar public methods
still fail. Additional wrong-DLL/wrong-source manifest checks verify those actual refusal guards.

Build: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`.
Runtimes use the actual tests DLL, `--bounded-exact` plus each literal above and
`--timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`.
Audit: `docs/migration/AuditResourceMutationCalls.ps1 -BuildInputs artifacts/optional-module-durability/9e4e7b2f7baa4be9af9820285ba96fac/build-inputs.json`.

### Self-review and next action

Local self-review, not independent review. Existing World first-fault, typed results, flow facts,
private views and notifications remain the affected operational owners; [wider assurance gaps](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
are not certified. Original requester/provenance, discrete/fractional quantities, foreign promises,
cancellation, saved twins, optional-module selection and source-local packing checked narrowly.
No new logger/journal, package, provider adapter or host logic.

Next inspect LogisticsService.Acquire's purchase-only supplier/cargo/destination claim group and
EndHaul release boundary through existing physical contracts. Establish actual unfinished-operation/
fault admission before changing it; do not invent supplier inventory/inlet aliases, duplicate
coordinators or change business prices/charges. Then continue spatial sample/accounting, typed
reference-owner and relevant bounded stage-stress matrices. Neither P7.017 nor P7.019 closes from
this census/selected cases. Full/profile/family/soak, consumer/Oil/marine, Tycoon business changes,
platforms/Draw and staging/commit/push were not performed. Full goal remains active.

## Historical 2.36 evidence — not current signatures or a current runtime claim

2026-10-04. Working-tree identity **beep-simengine-2.36.0 / save 16**. Stock/order and
event/journal/view layouts are unchanged; flow schemas remain 2. Exact build identity and declared
system manifest reject a previous simulation build; there is no compatibility reader. Framework,
external proof and directly affected tests only. Oil/Tycoon, hosts, demos, adapters and platforms
were neither changed nor tested. The full framework roadmap and P7 remain partial.

## One owner per responsibility

Kernel IEntityRemovalGuard provides pure typed removal checks. World.CheckDespawn requires the actual
live entity in this session, visits component guards and verifies unchanged entity versions. Despawn
checks again and refuses before deleting unresolved authoritative state. IInletPort (and thus every
IOutletPort/IInventory) and ISupplier require this contract. Built-in and external providers protect
their actual stock and owned promises. ResourceTransactions.CheckInventoryRemoval owns the common
inventory rule. Initialization/removal interfaces are multi-part lifecycle hooks, not singular
IEntityCapability aliases; more than one part may own lifecycle checks without capability-slot conflicts.

The existing ConstructionService instance also implements optional IResourceRecovery. CheckEvacuation
preflights actual stock, promises, other removal guards, placement, receiving policy and exact common
quantum. Evacuate uses the existing ResourceTransactions coordinator and real owned source/destination
receipts to move stock into authored-capacity piles at the source location. Stock remains solely at
its actual providers. The original endpoint must be empty before removal. Recovery is a local material
disposition; subsequent travel/loading/delivery remains ordinary physical hauling over game time.

Completion and cancelled-site cleanup use this same path. No copy-after-despawn, ignored partial Add,
unrelated promise clearing or remote-depot teleport survives in those paths. One stateless construction
cleanup system (order 405, after work 400) retries inactive sites when claims permit. Infrastructure
line cleanup uses the optional recovery contract, not a second pile implementation. A stocked marker
without that selected provider stays live with a typed missing-recovery blocker; no fallback destruction
or fabricated replacement is supplied. That missing-provider branch is source-implemented/compiled,
**not separately executed** by the present two suites.

IWorkOrderRetention allows the actual Site/LineWork target to retain its existing ended order and
blocker while disposition is unresolved. WorkView projects that same record. EndOrder releases and
clears its job references. WorkSystem.CompleteUpdate prunes resolved records at the final declared
system boundary after gameplay, events and entity updates, before World.Step publishes the completed
tick. This is engine bookkeeping inside the sole Update loop, not a host callback, new job store or
second simulation loop. It prevents a save containing an ended line order whose generated plan target
was removed later in that same tick.

Demolition commands and execution share CheckDemolition and one SalvageBill rule. Already-known
promises or incompatible disposition refuse before posting work; execution rechecks current state.
Structural salvage is the authored construction bill times a validated fraction in [0,1]. Native
discrete quantities floor to whole pieces; continuous fluids retain fractional native quantity.
This is explicitly attributed Produced lifecycle yield with a demolition requester/plan cause,
**not** a fictitious transfer from absent stock. Real stored leftovers transfer separately. Both paths
use one packing loop and the existing verified flow journal. This does not establish an invested/WIP
material ledger, provenance/quality-sensitive salvage, whole-session balances or topology accounting.

## Exact verification

Build command:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
```

Result: exit 0, **zero warnings/errors**, 1.30 s reported build elapsed time.
Only the following exact full suite names were invoked; each selected one suite:

```powershell
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Resource recovery: guarded removal, live stock and cancelled-site continuation"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Resource recovery: timed construction, native salvage and saved physical delivery"
```

| Suite | Checks passed | Failed | Elapsed |
| --- | --- | --- | --- |
| Guarded removal, live stock and cancelled-site continuation | 60 | 0 | 211 ms |
| Timed construction, native salvage and saved physical delivery | 28 | 0 | 217 ms |
| Total | **88** | **0** | Two suites; no aborted suite |

The first suite proves pure refusal for real stock and owned inbound/outbound/supply promises, explicit
accounted loss before removal, external-provider live recovery, capacity-split receipts/source identity,
no recreated input, cancelled-site claim retention, typed immutable blocker visibility, saved twin
continuation after release, fractional fluid conservation, normal line cancellation and healthy
end-of-tick checkpoints. Invalid authored salvage fractions reject content before play.

The second suite plays ordinary construction/demolition commands through physical material delivery,
site preparation, machine work and return hauling. It verifies consumed inputs exactly once, unused
stock transferred before site destruction, current demolition preview/command refusal parity, no order
on refusal, timed structural yield split by authored capacity, explicit causes, fractional fluid recovery,
save/load during work/demolition and deterministic twin continuation. All workflow waits are bounded
to 1,200 updates per stage and report tick/fault on failure. Test-only stock fixtures use the engine's
verified lifecycle contract; they do not implement scheduling or game rules in the harness. Resource,
capacity, workflow, timing and journal policy fixtures are master-listed JSON through the existing factory.

Early scoped runs caught an invalid mixed-capacity proof definition, premature ended-order pruning,
and an orphan generated line plan at a same-tick checkpoint. The authored capacity definition and
single WorkBoard retention/finalization owner were corrected. An expected receipt count was corrected
to reflect shared capacity across steel and water (three exact receipts, not two). The final commands
above are the post-repair evidence; failed intermediate runs are not included in the pass count.

## Explicitly unverified / remaining

- Existing LiveCapacity LeavePile caller was source-migrated and compiled, not rerun. Earlier initial,
  lifecycle, flow, ordinary Standard/Framework/Lifecycle families were not rerun and are historical evidence.
- General entity lifecycle/CompleteUpdate extension behavior beyond these recovery workflows is not a
  full kernel regression proof. No broad filter/full suite or stage gate was run.
- Missing-recovery-provider line execution, other recovery-incompatibility configurations, hostile
  recovery providers and incident destruction need their own focused future proofs.
- Late commitments after demolition has already begun return existing typed effect refusal; automatic
  policy-driven rescheduling of failed demolition is not added by this slice.
- Remaining raw/destructive callers, network pools/topology, full relocation/loss/accounting, generic
  spatial construction, DAG/tools/docks, transforms, stress and authorized stage gates remain open.
- Development-lifecycle skill guided the execution contract, existing-owner migration, scoped evidence
  and master-tracker update; it did not authorize a full test run or close the stage.
