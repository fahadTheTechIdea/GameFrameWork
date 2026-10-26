# P12-E6 — Physical disposal and reusable inventory work effects

Current API/behavior is maintained in [D2f/M59](P7_CONVERSION_ACCOUNTING_REVALIDATION_VERIFICATION.md#current-d2f-m59-spill-cargo-interruption): required per-resource Ports, shared port-contract owner, logical endpoint identity and atomic classified claim/conversion support native/scoped stores without local-inventory fallback. Named-port incident continuation was qualified at M58; M59 separately qualifies current physical spill/disposal/treatment and saved cargo return. Acquired faults retain M57 scope. The following is retained historical E6 evidence, including its original2.598 identity/results and lifecycle API; it does not qualify those old consumer scenarios on current core or describe the current effect implementation.

Date2026-10-15. Engine beep-simengine-2.598.0/save158/replay1. Existing persistent layouts, resource journal schema7 and resource-flow/environmental views9/2 unchanged. No Oil/Tycoon/platform/Draw/host gameplay changes, full/profile/family execution or Git publication.

## Original effect responsibility, now contract-based

IInventoryWorkEffectDefinition exposes the immutable bill/kind plus physical and structural/start admission contracts. InventoryWorkEffectDeclaration is the single strict reader/resolver for consumed/destroyed/lost metadata and actual resource dependencies. InventoryWorkEffectContracts validates actual frozen content aliases, allowed kinds, nonempty exact positive native bills and IResourceTransaction capability. One intent/refusal/execution algorithm delegates EvaluateInventory/CommitInventory to the original lifecycle owner. No new stock, job, command, disposal service, ledger or clock exists.

The existing InventoryEffectDef remains a direct Def and delegates to those owners. Its duplicate parsing/intent/refusal implementation is removed. Current concrete catalog callers in CargoRecoveryTests/ResourceDispositionTests migrate to the interface; reflection assertions can still name the concrete type. The new external IndependentInventoryWorkEffectDef also inherits directly from Def, implements public contracts and delegates to the same declaration/algorithms. Its master-listed module adds metadata, not scheduler behavior or a native subclass.

CheckIntent checks actual session target/company/inventory without requiring future supply. CheckStart evaluates a pure original complete-bill lifecycle intent using a derived preview identity; it acquires no receipt or stock. Ordinary activation therefore refuses insufficient local material. Existing plan-board structural intent and activation remain distinct; this stage tests direct activation refusal, not a new pending-plan scenario. CheckSubject/CommitSubject name the actual resolved subject and ordinary work-order identity. PhysicalWorkEffectContracts remains the sole positive machine ancestry/earned completion boundary.

## File-authored disposal route

independent-tile-disposal.json authors an empty disposal facility, named material port aliasing its sole real inventory, physical service/wait/departure dock slots, targeted four-minute staffed entity activity, native Destroyed effect, disposal metric, prerequisite objective and world setup. independent-inventory-disposal.json authors the independent effect. The fixture selects the external completion ID before content loading for the independent-provider case; both definitions otherwise use identical engine contracts. Normal scenario JSON enrolls spill detected -> recovered -> disposed rather than the alternative treatment goal.

Disposal is an explicit game material sink. This is not a scientific assertion that oil disappears or a landfill/chemical process model. The immutable environmental flow reports actual Destroyed lifecycle facts with the authored effect cause and actual facility endpoint. It does not convert a transfer or visual cleanup into disposal.

## Physical verification scope

Both cases execute delayed incident spill and original supplied repair/cleanup, then earned six-minute recovery of the three spilled units. Disposal at the empty facility refuses NoStock; targeting the ordinary site refuses WrongTarget; both previews preserve the hash. A normal haul physically loads recovered material, travels, unloads through the named port and departs. Loaded material is saved/resumed.

Only then can the four-minute disposal activity activate. Partial machine work leaves all three local units intact. A direct unfinished Apply refuses NotComplete and preserves the hash. Pausing the one clock freezes disposal. Disabling the actual worker preserves progress. Cancelling partial work keeps all material and records no disposal. After re-enabling, a fresh ordinary activity earns its full completion and commits exactly one original three-unit Destroyed fact. Settled updates cannot repeat it.

The original objective system completes the disposed goal from the journal-backed metric. Two unspilled units remain at the source site; no material remains at the disposal facility; remaining raw/spill/cargo/WIP/treated plus explicitly journaled disposal accounts for the original five units. The conservation helper is a read-only test assertion over original inventories/layers/history, not a gameplay ledger. Historical provenance identifies the actual disposal facility and the selected native/external effect. Foreign-company reads withhold disposal values. Existing saved partial responses, paused/disabled/cancelled/restarted work, interleaved-session isolation and complete normal-command replay remain covered.

Invalid effect kind, empty bill and zero-duration machine ancestry reject before play. The two cases pair native tile storage/native effect and independent tile storage/direct external effect. This proves those compositions, not every storage/effect cross-product or arbitrary hostile provider. Existing environment accounting admission also passes with the refactor; broader cargo/disposition/incident families compile only here.

## All observed attempts

1. Targeted compile clean22.75s, zero warnings/errors; session1322 awaited to terminal exit0.
2. Independent disposal424/0,10135ms; PID5412 exit0/10335ms, session36751 terminal.
3. Existing environmental/journal admission86/0,1012ms; PID29632 exit0/1177ms.
4. Native disposal424/0,33048ms; PID31852 exit0/33240ms, session70347 terminal.

Three distinct final literal cases total934 successful checks, zero failures/aborts. No executable edits followed these gates. All handles reached terminal status; no truncated output, watchdog termination or restarted/duplicate live case. Only these literal cases ran; no full/profile/family test gate.

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Spill disposal: independent contracts recovery hauling cancellation and earned destruction" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 7500
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Environment accounting: authored metrics and durable journal admission" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 7500
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Spill disposal: native recovery hauling cancellation and earned destruction" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 7500
```

## Remaining requirements

Waste chemistry, environmental deposition/landfill limits, emissions/byproducts, continuous/multi-cell spills, contended reservations/material removal during productive disposal, dynamic endpoints and arbitrary private/provider/fault/reentry matrices remain open. High-cardinality cumulative history and broad allocation/soak/lockstep/fuzz/performance still need explicit scoped gates. No original broad acceptance checkbox is closed, frozen consolidated audit counts/hashes remain historical, and the entire plan/goal plus final commit/push remain incomplete.
