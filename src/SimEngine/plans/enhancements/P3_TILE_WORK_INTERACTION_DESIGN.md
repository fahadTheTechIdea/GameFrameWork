# P3-S29 — Native work and docks on independent tile providers

## 0. Problem

TileWorkDock placement/live admission required concrete TileMap. TileMap also owned a private implementation of tile approach policy, so an independent provider had to reproduce it before native workers could approach ordinary targets. Movement alone did not prove native docked work.

## 1. Goals

1. Native tile docks admit selected navigation/geometry contracts rather than concrete map services.
2. Keep one engine-owned tile approach/overlap implementation shared by native and independent providers.
3. Prove ordinary player-posted native work, physical service/departure, earned progress, saved continuation and private views on an independent world.

## 2. Design

TileInteractionRules is the stateless owner of the original tile estimate/check/approach policy. TileMap delegates its existing ISpatialInteraction implementation to it; the private duplicate methods are removed. The independent provider uses that same implementation. Native motion remains the only route/time owner. Shared TileSpatialGeometry.CheckOverlap preserves original positive-footprint and long-coordinate comparisons; both providers delegate to it rather than duplicate tile overlap rules.

TileWorkDockDef.CheckTilePlacement and TileWorkDock.Live use ITileNavigationSpace. Existing slots, role/resource checks, queue/claims, vacancy, approach, checkpoint checks and authored offsets remain unchanged. No dock queue, worker claim, coordinate owner or scheduler is added. Missing overlap capability still fails original composition; no fallback geometry is introduced.

The independent fixture adds native worker/policy components, an authored six-minute inspection DAG and ordinary WorldSetup target placement. Its native tile_work_dock has service/waiting/departure slots. Public commands, clock controls, detached entity/node views and completion history drive the positive scenario. Provider overlap and approach use shared engine rules, not test-authored gameplay.

## 3. API/state

One shared tile interaction implementation and shared overlap function; existing contracts unchanged. Build2.580.0/save157/replay1, native payloads unchanged. No compatibility shim or parallel execution model.

## 4. Files

Map/TileInteractionRules.cs; TileMap.cs; TileSpatialContracts.cs; Standard/TileWorkDocks.cs; current engine/protocol identity. Existing independent provider declares interaction/overlap capabilities. Master-listed independent-tile-work.json and IndependentTileWorkTests provide native-worker proof. DockGeometryClaimTests updates stale waiting-slot expectations against actual provider travel costs while preserving claim/refusal/checkpoint checks.

## 5. Errors

Missing overlap refuses composition before initialization. Invalid/nonlive/foreign approach returns the original unsupported result; faulted interaction/overlap refuses before shortcuts. Bounds/blocked docks retain existing typed admission. Completed orders use original stable completion keys/history, not live-node lookup after retirement. Departure is physical and not treated as continued productive service.

## 6. Compatibility

No legacy branch or copied approach/overlap algorithm. Native and independent implementations delegate one engine policy; original jobs/state/time remain authoritative.

## 7. Verification

Current exact focused cases: independent docked work58/0, independent movement82/0, native/point cross-facility claim admission43/0. Independent worker reaches actual service bay, earns six game minutes, restores a productive-stage save and physically uses departure before completion. Pause/private progress/fresh-session isolation pass. Claim proof verifies current cost-first waiting selection and exclusive service geometry. [Evidence](../../docs/migration/P3_TILE_WORK_INTERACTION_VERIFICATION.md).

## 8. Remaining risks

Independent material hauling/construction/recovery/utilities, competing dock queues, dynamic dock lifecycle/areas/provider faults, broader replay/lockstep/stress/allocation and full original gates remain open. A role-only docked inspection is not proof of every industrial workflow.

## 9. Exclusions

New physical rules, copied providers/state/clock, instant fallback, business/Oil/platform/Draw, full/profile/family testing and Git publication.

## 10. Status

Migration and scoped verification complete. Full goal active; no original broad checkbox closes.
