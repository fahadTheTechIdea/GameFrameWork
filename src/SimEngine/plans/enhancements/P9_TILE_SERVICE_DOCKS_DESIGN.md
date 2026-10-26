# P9-C8b — Native tile service facilities

Status: focused implementation verified,2026-10-06. Parent: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Clean build;130 checks/two exact suites, zero failures/aborts.
[Evidence](../../docs/migration/P9_TILE_SERVICE_DOCKS_VERIFICATION.md).
Prerequisite: verified [shared docks C8a](P9_SERVICE_DOCKS_DESIGN.md). Full goal remains partial.

## 0. Problem

The shared IWorkDock coordinator has no native tile geometry provider. Only the independently
registered point provider supplies physical facilities. Native colony machines must use the same
jobs, claims and movement owner rather than a tile-specific queue or host vacancy logic.

## 1. Goals

1. Master-listed entity JSON declares tile_work_dock supported roles/resources and relative slots.
2. A native TileWorkDock implements the existing IWorkDock contract using actual ITilePresence and
   IMobility; existing WorkBoard owns admission, claims, FIFO, timing and departure unchanged.
3. Actual tile placement checks the whole authored facility geometry, including negative/outlying
   offsets and blocked static slot positions, before allocating an entity. No hardcoded world cap.
4. Actual native travel, shared haul/activity contention, pauses, departure and saved continuation
   execute through Game.Update. Queries and provider geometry introduce no new state/queue/clock.

## 2. Design

- Standard/TileWorkDocks.cs: TileWorkDockDef/TileWorkDock. Required explicit work_roles/resources/slots;
  immutable unique slot IDs, kinds and x/y integer offsets, required service/departure, catalog
  resolution, immutable Read/Resolve. Definition requires stationary tile presence. Resource service
  requires an inlet; work-only service does not. Slots cannot occupy the facility's blocking footprint.
- WorkModule declares tile_work_dock codec1. No new selected module or mandatory tile-world service;
  a point/nonspatial scenario using sim.work remains valid. Provider validation references the actual
  selected authored tile space through the presence definition.
- Maps.ITilePlacementConstraint is an optional multi-part definition hook, not a singular capability slot. TileMap.CheckPlacement invokes
  such constraints in authored component order through its existing preflight, shared by native
  spawning/construction/replacement/rollout. It guards existing map/tick/entity-sequence/replacement
  facts around callbacks. This is an extension of the selected geometry owner, not a second placement
  service/registry. TileWorkDockDef checks exact offset bounds with wide arithmetic and existing
  static occupied geometry. Mobile occupants are not permanent placement reservations; live dock
  vacancy still governs their physical availability.
- Native request validates actual session/entity/presence/mobility aliases and explicit supported
  roles/resources. Slot vacancy reads actual blocking footprints and all worker/transporter presence,
  including nonblocking machines. Slot reach/cost/approach use the existing IMobility goal/CanReach/
  GoTo and tile metric, never marker entities or another path. In-flight identical goals are preserved.
- IEntityCheckpointValidation is a pure multi-part lifecycle contract invoked by Entity.Sync after
  every component payload is decoded/written, avoiding JSON component-order assumptions for cross-
  capability checks. TileWorkDock implements it plus existing IEntityInitialization; no payload is
  added. It rejects invalid actual origin/space/placed state against the same immutable geometry.
  Presence owns saved coordinates; WorkBoard owns saved claims. Existing entity version/owner/state
  facts guard pure callbacks; this is not a global side-effect/rollback sandbox. This is an entity checkpoint phase,
  not another gameplay update loop or state authority.

Build2.113.0/save65/WorkBoard23/work system9/work view36/tile dock1. Existing owner payloads unchanged.

## 3. API

tile_work_dock component with work_roles/resources arrays and slots[{id,kind,x,y}]. Existing IWorkDock,
WorkDockRequest, private WorkJobView and activity graph facts remain the only gameplay boundary.
ITilePlacementConstraint.CheckTilePlacement(World,EntityDef,TilePlacement,Entity replacing) is pure.
IEntityCheckpointValidation.ValidateCheckpoint() validates settled component state only.

## 4. Files

Add Standard/TileWorkDocks.cs; extend Map/TilePresence.cs and TileMap.CheckPlacement, WorkModule and
Kernel/Entities.cs and SaveGame.cs. Add explicit test JSON, TileServiceDockTests and runner registration. Mechanically
migrate preceding build assertions only. Update this stage/master/P9/README/AGENTS/help/evidence.

## 5. Errors

Missing/null/wrong types, duplicate IDs/positions/roles/resources, absent service/departure, moving or
wrong presence, invalid catalog references or blocking self geometry refuse content. Invalid live
request/session/geometry refuses without creating jobs. Out-of-bounds/blocked placement returns typed
reasons before entity IDs/stock initialization. Provider misuse cannot checkpoint invalid placement.
No fabricated slot/default resource/teleport/compensating state mutation is permitted.

## 6. Compatibility

Development-only clean extension; no legacy or alternate coordinator. Existing point provider stays
the independent contract proof, not a fallback when native geometry refuses.

## 7. Verification

Build then only these two complete exact suite names; same-name repair/final reruns only:

- Tile service docks: authored geometry, placement and shared admission
- Tile service docks: native physical queues and saved continuation

Prove strict JSON, work-only/actual port semantics, pure/private placement/candidates, real bounds/
static occupancy, actual shared claims, native timed travel/queue/service/departure, paused exclusive
bay, conservation, immutable facts and saved twin/session isolation. Bound every physical wait.

## 8. Risks

Native blocking footprint must not occupy authored slots. Slot coordinates may extend beyond the
building footprint and need full placement preflight. Nonblocking workers still physically occupy
slots. Completed actors can occupy finite exits until subsequent real home/work movement clears them.

## 9. Remaining scope

Vehicle maintenance subject/cadence/repair effects, rotated/3D facilities, cross-facility overlapping
claims, route collision/congestion, full slot/route operating-area containment, preferred routes/trip
budgets, global provider purity/rollback/replay/stress/allocations/full P9/framework stay open.
No Oil/Tycoon/host/demo/platform/Draw change or full regression run.

## 10. Execution

- [x] Implement native provider, canonical tile placement constraint and post-decode checkpoint boundary.
- [x] Author native facility/machines and two physical contract proof suites.
- [x] Build/run only the two names; record scoped source evidence.
- [x] Align active tracker, parent and developer guidance; leave broad requirements open.
