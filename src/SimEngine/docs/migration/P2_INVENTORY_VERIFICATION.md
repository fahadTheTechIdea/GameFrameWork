# P2 inventory execution and prototype-contract slice — 2026-10-01

This is incremental P2 implementation, not completion of the framework separation/generalization.
The stage design and master tracker remain the sole requirements/status authorities.
This report identifies the inventory source before subsequent module-registration hardening.
See [module metadata evidence](P2_MODULE_METADATA_VERIFICATION.md) for newer current-source gates.

## Implemented owners

- The existing EntityDef component list resolves definition contracts. IComponentDefinition exposes
  the runtime component type; specific inherited definition contracts cannot have competing owners.
  The ambiguous common definition root refuses queries. No second prototype registry was added.
- IInletPortDefinition exposes JSON-authored receiving compatibility before an entity exists. StorageDef
  and the independent SingleItemPortDef implement it. WorkView.StoreAccepts uses the contract, not StorageDef.
- ProducerDef validates IInventory rather than StorageDef. ConstructionService.Demolish reads IInventory.Stock
  into the existing salvage mechanism. Generated site/pile content still explicitly uses standard Storage;
  this is a selected implementation, not a concrete-only consumer query or a second store.
- ProductionSystem validates every output's compatibility and total unpromised FreeSpace before any item
  or vehicle delivery. Inbound promises cannot be stolen by production. One rejected item blocks the whole
  delivery with blocker.output-incompatible; full/promised room uses the existing blocker.output-full.
- WorkView.TryGetProduction returns a detached readonly ProductionView with phase, progress, required work,
  queue count, recipe and blocker. A rival cannot query another owner's private production. No client
  predicts progress/availability or mutates operating state.
- The non-friend SingleItemPort proof now exposes IInventory over its same single-item buffer and Sync;
  no Storage, proxy or extra stock/reservation ledger exists on those entities. Test-only setup composes
  JSON-authored factories using this provider; standard production/work/logistics code executes them.

## Executed gates

Framework root:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- Capabilities -v
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- --all
pwsh -NoProfile -File docs/migration/VerifyExtraction.ps1
```

Build: zero warnings/errors. Focused capabilities: **83 passed, zero failed, eight suites**, 0.2 seconds.
Full framework: **375 passed, zero failed, 43 suites**, none skipped, 0.9 seconds. New coverage adds
**39 checks**: 21 production/reservation/view checks, nine incompatible-output checks, seven demolition
and physical recovery checks, plus two invalid-content checks. Existing assertions were preserved.

Tests cover whole-output item/vehicle atomicity, once-only input/output, authored elapsed work, inbound
space promises, private/detached snapshots, blocked cancellation, mid-production save/load/hash twins,
mid-demolition save/load/hash twins and timed machine recovery of stored inventory plus construction salvage.
Ambiguous definition owners and producers without inventory refuse before play.

Oil active-v2 root:

```powershell
dotnet build SimEngine.slnx --no-restore
dotnet run --project OilCo.Tests --no-build -- --all
```

Build: zero warnings/errors. Full regression finished: **186 passed, zero failed, 22 suites**, none
skipped, **548.5 seconds**. Includes Frontier, programmed-player demo, performance reporting, offshore,
storyteller incidents, visibility/presentation purity, lockstep and thin-client gates. Combined inventory
baseline: **561 passed / 65 suites**, zero failures. This proves the inventory source identified below,
not module-registration edits performed while its already-built process was running.

Ownership verification passes all **121** original dispositions/recovery hashes, **52** unchanged
namespace-normalized C# transfers (declared later P2 edits excluded), standalone project dependencies,
and absence of the original runtime. Current executable identity: **139 files**, SHA-256
`569E749C77D9BA6B3A273D16650E1BFD85B33E69E0649A25145EA36348705456`.
Framework runtime DLL SHA-256:
`491DA5625A2F236FA9A6960522D570B2C4A94ED28EFA3709FD2D55B11B8E77D9`.

## Remaining scope

Production rollout still selects navigation through MoverDef; replace it with authored mobility-definition
contracts and an independent mobility production proof. Supplier abstraction, typed roles/requirements,
operation/signal/response providers, immutable module metadata snapshots and full event/state/view schema
ownership remain unfinished P2 work. P3–P6 remain planned. This slice neither removes tile-map bounds nor
implements optional space/time, DAG planning, emergency policies, automation or final replay acceptance.

No Tycoon, old sibling engine, renderer/platform, oil-client gameplay, repository initialization or
package-publication changes were made. All behavioral edits are framework/engine-side proof code.
