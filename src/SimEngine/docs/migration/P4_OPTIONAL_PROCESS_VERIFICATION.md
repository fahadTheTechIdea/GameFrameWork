# P4-C1a optional-process verification

Date: 2026-10-04. Working-tree build `beep-simengine-2.24.0`; save format 11 unchanged.

## Implemented single owners

- StandardModule and old sim module identity removed. InventoryModule (`sim.inventory`),
  OperationsModule (`sim.operations`), ContentAccessModule (`sim.access`) and ProductionModule
  (`sim.production`) own transferred declarations, events, commands and views. PhysicalWorkModule
  (`sim.work`) retains the remaining physical group pending C1b; there is no compatibility facade.
- ProductionSystem/Producer remains the one existing timed process loop and queue. No map, movement,
  work, construction, logistics or economy is accessed by authored local, zero-priced processing.
- Producer schema 2 requires automatic_supply. True requires IProductionSupplyRequests before
  Initialize; the actual LogisticsService implements it using its existing demand identities/store,
  bound as an alias to that same instance. False waits on actual inventory without fabricated supply.
  Positive recipe cash requires IEconomy before Initialize; zero-price processing does not resolve it.
- Construction no longer owns access checks. Exactly one selected ContentAccessDefinition produces
  IContentAccess through the existing JSON catalog. Open policy is explicitly authored, never implicit.
  Ordinary previews and commands use the same policy. ExtensionProof authors a distinct denying
  definition/provider through public contracts without changing production/construction systems.
- Queue/progress/rollout DTOs and queries moved to ProductionManagementView (schema 1).
  WorkView schema 3 has no producer compatibility methods. State codecs remain with their original
  queue/inventory/coordinator owners; no new persisted queue or secondary catalog was introduced.
- Standalone proof registrations, manifest dependencies, selected modules, producer JSON and view
  consumers migrated in place. Shared open-access proof content has one explicit embedded JSON source.

## Exact final commands and results

Run from `C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine`:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Optional process: mapless timed inventory processing and saved sessions"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Optional process: explicit provider refusals and physical colony composition"
```

Final build: zero warnings/errors. Mapless: **35 passed / 0 failed**, 87 ms.
Provider/physical: **27 passed / 0 failed**, 199 ms. Combined **62 checks / two exact named suites**.
Only these two suites ran/re-ran in this slice; older suites compiled but were not executed.
No broad/full run, oil integration, platform verification or full stage closure is claimed.

## Established behavior

- Explicit process-only manifest composes/initializes without registering PhysicalWorkModule.
  Ordinary public player commands consume actual local stock, execute six game ticks per batch,
  publish item and non-spatial entity outputs once, and preserve sequential queues.
- Active and starved processes save/load; split update batches and continued sessions retain identical
  hashes. Local shortages remain typed blockers; cancellation requires no logistics provider.
- Missing automatic-supply/economy provider, absent/competing access policy and obsolete producer
  content refuse before initialization. Authored denying policy gives identical preview/result refusals.
- Ordinary physical colony still hauls production/construction materials, moves its construction
  machine, manufactures a third truck and completes a tower within a 4,320-tick bound. Saved physical
  twins continue identically with the request alias and sole logistics coordinator.
- Live route cost excludes other entities' occupied tiles, permits the actor's own tile, rejects
  occupied destinations, finds clear alternatives and leaves the session hash unchanged.

## Fault found and corrected

The first physical acceptance run faulted before its save checkpoint: TileMap.Relocate correctly
rejected `movement is blocked`, while NavGrid had planned using terrain costs alone. The test did not
relax collision or skip construction. The existing NavGrid search now evaluates live occupancy,
excluding the actor's own start occupant. Mover rechecks entry and diagonal corner costs on cached
paths, discarding changed/blocked paths for replanning from its current physical position next tick.
TileMap's strict relocation/occupancy guard remains authoritative. Final exact-suite results above
include the added occupancy checks and normal physical completion; broader traffic/deadlock stress
and P9 dock/congestion policies are not established by this small proof.

## Remaining work

C1b must split physical navigation/work/construction/logistics/networks/response selections and remove
their blanket economy requirement. Generic construction, authored salvage/site capacity, resource
atomic transactions, TransformerManager, arbitrary activity DAG, typed progression, policy/automation
and full stage gates remain open. Older owner/schema assertions may need further migration when their
directly affected gates are authorized; compiling them is not a claim that they pass.

Only Beep.SimEngine source/proofs/docs changed; no OilFieldDays, Tycoon, host/demo gameplay, renderer,
Unity/Godot or adapter changes. The lifecycle skill guided staged ownership and evidence tracking;
repository/user exact-suite rules override its generic full-regression recommendation.
