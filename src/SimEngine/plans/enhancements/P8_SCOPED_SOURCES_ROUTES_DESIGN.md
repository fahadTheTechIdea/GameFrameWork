# P8-D5c — Scoped source discovery and standing routes

## 0. Problem statement

D5a supports explicit named physical hauling and D5b supports automatic named destinations.
Standard/Logistics.cs PreviewSource still scans only IOutletPort entity inventories; Route stores
only From/To entity IDs. Public scoped stores cannot supply automatically and standing routes
cannot address named stores. Complete those existing owners, not a second port registry/matcher.

Current focused result: core2.318.0/save152/logistics18/WorkBoard47/logistics-view32/work-view90.
ONE NEW exact case107/1,108/0,FINAL108/0/0 in1981ms. FIVE builds (one new-view compile failure),
final clean5.11s. Executable freeze21:32:32 UTC. Prior runtimes historical/not rerun; full goal OPEN.
[Recorded evidence](../../docs/migration/P8_SCOPED_SOURCES_ROUTES_VERIFICATION.md).

## 1. Goals

1. Discover actual owned public stock through IResourcePort/IResourcePortDefinition as well as
   ordinary entity outlets. No local inventory is required for a named-only source.
2. Deduplicate aliases by physical site/actual endpoint and resolve stable names. Keep actual
   export priority, travel cost and stable identity arbitration, original native capacity and claims.
3. Extend the original Route/AddRouteCommand/preview/view/save/hash/replay with source/destination
   names. Future trips follow current named selection; already acquired jobs retain original receipts.
4. Route commands use ILogisticsRoutes bound to the same LogisticsService. Admission rejects
   missing/incompatible/private-company/actual-self endpoints before intent creation.
5. Master-listed JSON owns all source exposure, content, stocks, recipes, startup and worker rules.
   Tests remain player commands/immutable facts/Update only. No legacy reader or old alternate path.

## 2. Design

PreviewSource enumerates each actual site once, explicit local outlet plus sorted authored named
resource-port definitions. Resolve through ResourcePortContracts; only actual Public stores enter
automatic supply. Skip transporters and requester sites; deduplicate actual endpoint aliases per
site. One existing comparison chooses owned supply before optional supplier acquisition. Preserve
priority, interruption avoidance, selected route cost, entity identity and ordinal port tie-breaking.
Explicit routes may use private owned endpoints; public exposure is for automatic discovery only.

Preview/Acquire carry exact selected source endpoint as well as destination endpoint. Rebuild
current offers before acquisition, resolve the offered original stores exactly, then use D5a whole
source/cargo/destination claims. Live Job receipts remain the acquired-reservation authority. Job additionally retains the admitted
source/destination identities as execution facts after those receipts are spent, so saved views do
not lose pickup provenance during delivery. Explicit existing recovery replanning commits its new
admitted destination binding together with the replacement receipt; ordinary selection never does.

Route stores SourcePort/DestinationPort, not stock or an extra reservation. Different actual stores
at one physical site are valid; actual endpoint self-transfer refuses. A later selection can alias
both ends: future trips block, but saved intent and settings edits remain valid. Creation requires
distinct endpoints; one shared admission algorithm validates later stored intent without that
runtime readiness condition. AddRoute/CheckRoute
accept those names, validate actual current endpoint packing and distinct identities. ConfigureRoute
changes only original settings/age, preserves declared names. RemoveRoute retires future intent;
native already acquired trips continue to their original stores. Save codecs/command stream are
breaking development migrations; no compatibility adapter/overload/old-format reader.

ILogisticsRoutes declares existing admission/add/settings/remove operations; all three handlers
depend on this alias, not concrete service. Existing RouteView projects named intent and offer facts
include actual source/destination names/endpoint identity. No mutable job/port handles are exposed.

Proof EndpointPoolDef gains required authored public exposure, propagated into its existing
ResourceInventoryState; migrate old pool JSON/literals explicitly false. No invented starting
stock: new public scoped sources are funded by ordinary physical player hauling from JSON stock.

## 3. API surface

- ILogisticsRoutes: CheckRoute/AddRoute (...settings,sourcePort,destinationPort), existing settings
  check/configure and explicit CheckRemoveRoute/RemoveRoute.
- AddRouteCommand adds SourcePort/DestinationPort; command Sync includes legitimate Code.None.
- RouteView and LogisticsDispatchOffer expose detached named intent/bindings.
- Existing manual hauling API remains unchanged. No host gameplay callbacks.

## 4. Files

Modify Standard/Logistics.cs, LogisticsScheduling.cs, LogisticsModule.cs, StandardCommands.cs;
Commands/StandardCommands.cs and HaulResourcesCommand.cs; Views/LogisticsView.cs and WorkView.cs;
Standard/Work.cs, WorkModule.cs and LogisticsRecovery.cs; Kernel/SaveGame.cs; current protocol JSON.
Reuse HaulResourcePorts/ResourcePortContracts. Migrate EndpointProof authored exposure and pool
documents/literals. Extend ScopedHaulGame composition for one master-listed scenario; add one
new player-only exact bounded case, stage evidence and master/audit checkpoints.

## 5. Errors

Missing names/storage/foreign ownership/actual self-transfer/incompatible quantum refuse intent
before mutation. Public=false excludes only automatic discovery, never fabricates stock. Native
outlet reserve/destination policies and hauling blockers still wait physically. Provider purity/
identity violations fault; no compensation/default replacement. Removed sites lapse existing intent.

## 6. Compatibility

Development-only breaking codec/API migration. No legacy path or compatibility readers.

## 7. Verification

Build for compilation. ONE NEW exact bounded case: physical source funding, automatic scoped
pickup/production, stable alias and original receipt behavior; named standing pool-to-pool routes,
changed selections, future-route pause/remove, private/pure refusal views, saved twins/accounting/
replay where exercised. Stage Update bounds plus observed child exit; repair/rerun only this case.
No prior/family/full/Oil/platform runtime. Full original acceptance remains open until actually proven.

## 8. Risks

Automatic aliases must not duplicate source capacity; choices must not depend on authored array
order. Current selected stores and original acquired stores are distinct concepts. Route preview
must use the same admission/packing logic as commands, not another local-inventory shortcut.

## 9. Remaining scope

Scoped stock bands/per-port outlet policies, generic scoped DAG supply/recovery/diagnostics,
dynamic removal/fuel/road/area/dock/emergency/provider fault/stress/allocation matrices, purchase
breadth and full P8/P1-P12 remain open. No platforms, rendering or business requirement added.

## 10. Order

1. Migrate original route contract/state/commands and exact source acquisition binding.
2. Generalize original discovery through sorted named contracts with alias deduplication.
3. Migrate normal authored exposure; add JSON scenario and single bounded player gate.
4. Record actual attempts, review focused source and update tracker/audit without inflating gates.
   Publication still requires FULL goal AND plan completion and verification.
