# P3-PC1 — Physical proximity is not route reachability

Implemented2026-10-16; current2.642.0/save160/replay1. Four focused framework
finals462/0; canonical two saved days now pass, but a real loaded-vehicle congestion
blocker remains. [Every attempt, current native save and limits](../../docs/migration/P3_SPATIAL_PROXIMITY_CONTRACT_VERIFICATION.md).

## 0. Problem and specification

Canonical harbour continuation repeats full A* searches from WorkSystem.CheckAssignmentCore
even though that caller only tests WithinReach. A live2.640 stack confirms Mover.CanReach
under that query. Another sample confirms full approach checks in dock admission; those
actually require routing and remain in scope for later cost measurement, not bypassing.

## 1. Goals

1. Add explicit contract-owned physical proximity, distinct from prospective routing.
2. Migrate proximity-only work checks without relaxing route/area/dock admission.
3. Guard actual provider, both entities, selected space and clock; no cached decisions.
4. Verify native/independent tile and non-tile providers, malformed/failing callbacks,
   ordinary physical movement/work and saved continuation through focused cases only.

## 2. Design

Extend ISpatialInteraction with mandatory CheckProximity and typed WithinReach,
OutsideReach, Unsupported results. SpatialInteraction remains the sole dispatch owner.
Its proximity observation uses existing EntityObservation and full actual entity/space
codecs. TileInteractionRules owns the shared tile geometry; TileMap and independent
SensorTileSpace delegate there. PointSpace owns point distance. Full Check/Approach
continue to own route admission; a positive proximity result never promises a route.

WorkSystem.CheckAssignmentCore, FixedActivityExecution.CheckWorker and undocked physical
delivery occupancy use proximity only. Complete route checks, approach and job route/area
gates stay unchanged. PreviewDock no longer performs an unused current-origin reachability
query for supplied future origins; actual future-leg PlanRoute remains required.
No parallel spatial store, route cache or scheduler.

## 3. API surface

Mandatory CheckProximity on the existing spatial interaction contract; no default/legacy
method. Engine2.641 introduces proximity;2.642 removes the unused future-leg query.
Existing save160/replay1 state layouts unchanged. Old native
checkpoints stay build-bound/incompatible, never header-rewritten. If validating updated
canonical work, explicitly reproduce the same seed/geometry/intents, not a fallback world.

## 4. Files

Kernel/SpatialInteractions.cs, Map/TileInteractionRules.cs and TileMap.cs, original work
callers, two existing independent spatial providers, focused tests and current protocol.
Keep all gameplay in these engine providers; proof hooks are negative instrumentation.

The affected optional-space proof also exposed a pre-existing null supply call in
WorkSystem.RefreshSupply: material-free plans are valid without IWorkSupplyRequests,
but empty cleanup dereferenced that absent service. Preserve that explicit optional
composition; withdraw nothing if no inputs/service, and refuse actual inputs without
their owner. No substitute supplier, free materials or required logistics module.

## 5. Errors

Invalid/absent geometry is Unsupported, not OutsideReach. Invalid enum, mutation or
callback failure latches the original fault; no healthy result or resumable partial state.
Faulted reads reject before invalid/empty early returns.

## 6. Compatibility

Development only. No shim/default implementation or duplicate spatial authority.

## 7. Verification

Before changes, repeat only the known2.640 saved-day literal under original180s cap,
sample its confirmed-live process for call-site evidence. Then targeted Release compile,
new proximity literal and affected existing physical itinerary/nonspatial literals.
Canonical new-build preparation/harbour checks retain their original limits.
No full/profile/family run and no publication until the whole goal/plan finish.

## 8. Risks

Dock candidate admission still legitimately plans journeys; this change alone may not
resolve its cost. Do not confuse physical proximity with reachability or change refusals.

## 9. Out of scope

Weakening route/hash/mutation guards, altered timing/resources/maps, business, platforms,
Draw, full suites, staging/commit/push.

## 10. Execution order

1. [x] Reproduce and sample actual saved-day route checks (bounded failure retained).
2. [x] Extend existing contract/providers and migrate proximity-only callers.
3. [x] Verify focused contract/physical cases and canonical continuation (partial, not full harbour).
4. [x] Record all results/limits in master/P3/P5/AGENTS/README.
5. [ ] Resolve actual vehicle access at grid site and complete original harbour work.
