# P7-D2 / P3-S11 — authored provider-neutral resource recovery

## Current M78: runtime geometry reads

The existing ConstructionService now observes every recovery geometry capture, placement check
and identity read through the original EntityObservation boundary, using the actual source/container
codec and selected space. Healthy admission is required before a successful recovery answer. Actual
packing errors preserve World first-fault and rethrow the original cause. No extra recovery owner,
ledger, scheduler or clock. Stock packing remains movement; structural yield remains authored
production. Existing EntityStore lifecycle admission already prevents partial-container checkpoints;
the corrected old-engine probe confirmed this, so no redundant operation boundary was introduced.
Native2.724/save161/replay2; [four exact final cases310/0 and all retained diagnostic attempts](../../docs/migration/P7_RESOURCE_RECOVERY_VERIFICATION.md#current-m78-recovery-geometry-observation-and-classified-packing--2026-10-22).
This is scoped source/physical/protocol evidence, not full C3/P8 accounting or future-provider safety.
The original problem/design below records the earlier provider-neutral migration, not remaining
TileMap coupling in current recovery code. No Godot API or renderer is involved.

P10-A3 uses this existing recovery owner before an empty construction-intent cursor can move.
Surplus stays in the selected ordinary container at its actual old placement; outstanding receipts
prevent handoff. Physical failure/cancellation preserve stock and earned geometry. No new recovery
store/packing logic.2.125.0/save68/line_work2;386/two exact P10 suites,
[evidence](../../docs/migration/P10_SEGMENT_CONSTRUCTION_VERIFICATION.md). Earlier recovery suites
compile only, not rerun; broad provider/tool/retry matrices remain open.

## Problem and goals

ConstructionService.CheckEvacuation/CheckPacking/Pack/CheckDemolition still require ITilePresence,
TileMap and a generated sim.salvage-pile prototype. This contradicts provider-neutral construction
and master-file authoring. Replace the path, not add a point-specific recovery service or shim.

1. ResourceRecoveryDef/IResourceRecoveryDefinition selects an actual space/container prototype in
   the existing content catalog. Scenario settings.sim.construction.recovery is explicitly a profile
   ID or null; no inferred tile pile, capacity, automatic file scan or old construction_settings.
2. Recovery containers are ordinary master-listed JSON prototypes with empty creation stock and
   native inventory capacity/filter/quantum. Selected ISpatialRecoveryPlacement validates definition
   geometry; its existing capture/check/configure methods own actual placement, not the stock owner.
3. The existing ConstructionService/IResourceRecovery remains the single packing/disposition owner.
   Live balances move through exact owned native receipts, authored salvage through lifecycle yields.
   No second ledger, pending stock, scheduler, cleanup list, geometry conversion or compensation.
4. Demolition and leftover/cancelled-site recovery use the same provider path. Pure admission protects
   claims/motion/removal guards and current geometry/capacity. Missing/invalid recovery refuses and
   retains actual stock/order; empty removal does not require invented recovery content.
5. Immutable private construction views expose the selected container identity and actual recovery
   blocker. Saved interrupted/cancelled/build/demolition and later real hauling preserve accounting.

## Design and execution order

1. Add the definition/provider contracts to current capability/placement owners. Replace generated
   salvage authoring and tile-specific LeavePile API directly. Declare resource_recovery_profiles;
   explicit nullable scenario selection aliases the same content definition. No new state codec.
2. Migrate ConstructionService packing to selected capture/check/configure placement. Separate its
   packing code into a partial file if needed, still one class/owner. Validate exact catalog aliases,
   empty stock, geometry and native compatibility before initialization; preflight every request.
3. Migrate existing tile fixtures/callers and author an independent point container/profile document
   through the master. Tests simulate player commands; executable geometry remains module contracts.
4. Prove original tile recovery/accounting plus point surplus/cancel/demolition and physical hauling,
   private/pure views, typed missing/invalid content, bounds/claims and saved twins. Bounded waits only.
5. Update source/save/view identity, master/P3/P7/P9/README/AGENTS and scoped evidence together.

## Failure/timing and verification

Previews never spawn containers, allocate claims or advance jobs. Actual recovery occurs only through
existing physical work effects/retained cleanup; recovered material stays at the source until actual
hauling. A provider violation during committed packing faults Update and cannot save/hash as success.
No silent stock loss or substitute geometry. Native promises forbid normal removal and disposition.

Exactly two affected names (retain and extend their tile coverage), no family/full run:

- `Resource recovery: guarded removal, live stock and cancelled-site continuation`
- `Resource recovery: timed construction, native salvage and saved physical delivery`

Build: `dotnet build Beep.SimEngine.slnx --no-restore`. Focused proof does not close all provider
fault matrices, multi-leg transactional batches, templates, fleet/areas, replay/stress/performance,
P8/P10/P11/P12 or full framework acceptance. No compatibility reader, Oil/Tycoon/host/platform/Draw.

## Status

Focused-verified **178 checks / two exact suites** (89 admission/removal; 89 physical), zero
failures/aborts, clean build. Identity: build2.71.0/save43/work20/work-view25/logistics4/logistics-view9/
construction-view3. [Evidence](../../docs/migration/P7_PROVIDER_RECOVERY_VERIFICATION.md).
Native codecs unchanged. Tile cases retained; point authored profiles/local containers, genuine
tool surplus, foreign-promise cancellation, timed demolition and later real hauling/saved twins pass.
A stale tile test now waits for actual progress, not a travelling assigned node's Working status.
Other suites compiled only. This slice is complete; broader completion-admission retry policy,
provider fault matrices/batch/fleet/templates/replay/stress/full gates remain required. Full goal
stays active/partial. Lifecycle skill guides staged ownership and focused evidence; repository/user
verification restrictions override its full-regression advice.

## Current content and public boundary

The master explicitly lists documents in the owning module (point proof lists points.json,
point-construction.json and point-recovery.json). No file scan or generated recovery prototype.

```json
{"resource_recovery_profiles":[{"id":"field.recovery","space":"field.points","container":"field.container"}],
 "entities":[{"id":"field.container","kind":"entity","components":{
   "point_presence":{"space":"field.points","position":0},
   "storage":{"capacity_policy":"fixture.count.4","public":true,"evacuate":true,"initial_stock":{}}}}]}
```

This excerpt depends on the already authored space/resource/capacity/provider declarations. Scenario
selection is `"settings":{"sim.construction":{"recovery":"field.recovery"}}`; explicit null
selects no recovery capability content and causes stocked requests to refuse MissingRecovery.
Empty removals still succeed. Tile containers instead author nonblocking/nonmoving tile_presence;
other spaces own their compatibility through ISpatialRecoveryPlacement. The container ID/category/
visual/kind are content choices, not hard-coded engine names or mandatory marker/structure kinds.

IResourceRecovery exposes immutable Definition, CheckEvacuation/Evacuate and CheckYield/ReleaseYield
over provider SpatialPlacement. Actual work effects/cleanup call these engine hooks; they are not
instant player/admin commands. ConstructionView.RecoveryContainer returns only a content ID, and
CheckRecovery enforces company privacy before native diagnostics. No executable provider escapes.
ConstructionRecovery.cs is a partial file of the one ConstructionService, with no persistent state.
