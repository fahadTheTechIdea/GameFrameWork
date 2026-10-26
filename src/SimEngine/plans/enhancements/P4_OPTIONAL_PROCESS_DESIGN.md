# P4-C1a — Independently selected inventory, operations, access and production

Status: C1a focused-verified, 62 checks / two exact suites; build 2.24.0 / save 11.
Full optional module composition remains incomplete. See
[source-specific evidence](../../docs/migration/P4_OPTIONAL_PROCESS_VERIFICATION.md).

## Problem and acceptance

The former StandardModule bound work/navigation/construction/logistics/production and required an
economy even for mapless, zero-priced process content. Production's permission check is owned by
ConstructionService, and its management API is mixed into WorkView. Removing only registrations
would leave these hidden dependencies, so this slice migrates their owners in place.

Goals: a JSON-authored process-only game runs without space/nav/work/construction/logistics/economy;
automatic supply and priced recipes require actual selected contracts before initialization; production
views/commands still share engine rules; normal physical colony content continues through the same
systems, with no second scheduler/store/process algorithm.

## Single owners

- InventoryModule (`sim.inventory`): StorageDef declaration only; inventories retain their codecs.
- OperationsModule (`sim.operations`): signal declarations, control command and operations view.
- ContentAccessModule (`sim.access`): one selected ContentAccessDefinition from the existing catalog;
  open_content_access JSON explicitly selects unrestricted rules, while a game can author a definition
  implementing IContentAccess. No implicit open/free provider. Existing access rules move out of
  ConstructionService into ContentAccessPolicy; all checks use IContentAccess.
- ProductionModule (`sim.production`): ProducerDef/RecipeDef, existing ProductionSystem/events/
  queue commands and ProductionManagementView. Requires inventory/operations/access, not physical work.
- PhysicalWorkModule (`sim.work`): remaining current work/logistics/nav/construction/network/response
  registrations. This is an explicitly named remaining work group, not a StandardModule facade.
  Its later decomposition remains C1b; C1a does not claim all mechanisms are independently selectable.

The list above describes the C1a source. C1b1 subsequently transferred motion and tile navigation to
their own owners; see [independent motion](P4_OPTIONAL_MOTION_DESIGN.md). The remaining group and its
mandatory economy constraint have not yet been fully decomposed.

Delete StandardModule and the old sim module identity; no wrapper or alias. Physical code is transferred,
not duplicated. Shared system order constants move to the corresponding owner. Production DTOs and
methods move out of WorkView; no delegating compatibility methods survive.

The physical acceptance uncovered a pre-existing route/occupancy inconsistency. NavGrid's existing
search now uses current blocking occupants, excluding the actor's own start occupant. Mover checks
entry and diagonal corner availability on cached paths each tick and discards invalid routes to replan.
TileMap retains its strict movement/occupancy authority; collision is not bypassed or caught as success.

Producer JSON requires automatic_supply true/false. True uses IProductionSupplyRequests implemented
by the sole LogisticsService. Requests/withdrawal reuse its existing demand identity/store. False uses
actual local inventory and reports shortages; it never invents delivery. A positive authored recipe price
requires IEconomy before tick one; zero-priced production never resolves an economy service.

## Migration and verification

Update all standalone proof registrations, exact manifest dependencies, scenario selections and view
calls. The shared explicit open-policy proof document has one authored source; manifests list it.
Bump engine build identity; state layout changes only if new authoritative persisted fields require it.
Authored supply selection is definition state, not another persisted queue.

Two exact suites prove pure timed processing/saved continuation and strict optional-provider validation
plus physical colony parity. Build all standalone projects; do not execute older/broad/full suites.

Remaining: separate physical work/logistics/nav/construction/networks/incident composition in C1b;
physical work's existing mandatory economy constraint; generic construction authoring, resource atomic
transactions, TransformerManager, arbitrary activity DAG, typed progression/conditions and full gates.
