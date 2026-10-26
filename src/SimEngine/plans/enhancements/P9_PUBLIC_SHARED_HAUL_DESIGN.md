# P9-C9 — Public shared-haul admission and timed handling

## 0. Problem statement

Original P9.acceptance.048 requires shared supplies/docks/carriers not to double-book and handling duration to scale with actual amount. Existing SharedDockQueueTests reads mutable internal jobs and manually acquires hauling; it is historical evidence, not current public-player acceptance. Current HaulResourcesCommand/InventoryView/WorkView and original WorkBoard/resource transaction/dock owners supply the necessary engine boundary.

## 1. Goals

1. Simultaneously submitted player commands acquire distinct carriers and dock/wait geometry with exact source/outgoing and destination/incoming claims.
2. A fourth player request refuses while bounded dock geometry is occupied, with no successful stock/job prefix.
3. Actual four/eight-unit loads report four/eight loading and eight/sixteen unloading steps through original authored handling, travel physically and conserve all24 delivered units.
4. Pending, queued, partial load/unload and terminal saves, paused work/clock, detached/private views and replay preserve original ownership.

## 2. Design

Add shared-haul-master.json, explicitly referencing existing process-only world/work/recipes/access/retry plus shared-haul.json. The new document adds queue-only source/destination/four transporter definitions, bounded dock geometry, a24-unit receive policy, WorldSetup profile and scenario. Only that profile initializes the queue actors; previous process entities are definitions, not live instances. Use existing TileNavigationModule/TileSpace/TileWorkDock/Mover and native Storage/Worker/Transporter. PointModule loads the referenced point declarations only; it is not the selected geometry. First point-space attempt showed bidirectional single-lane traffic blocking, not a stock/claim defect. Two-dimensional authored circulation preserves real occupancy rather than allowing crossing or teleportation. No new queue, allocator, handling algorithm, clock or stock owner.

Thin SharedHaulAdmissionTests observes immutable views and issues ordinary HaulResourcesCommand and work policy commands. It never assigns jobs, places/spawns actors or computes resource handling eligibility. Check public snapshots for distinct mobile positions at service/wait/departure sites, reservations for aggregate actual promised24 or less, and original job fields for exact selected phases/amounts. If this reveals an actual owner defect, repair that owner rather than adding a driver workaround.

## 3. API surface

Existing initialization, seat previews/submission/results, InventoryView/WorkView/LogisticsView, game Update/Clock, snapshots/saves/replay. No new public API planned.

## 4. Files to change

New shared-haul master/content JSON and SharedHaulAdmissionTests; embed content and register one new exact case. Existing engine sources only if a demonstrated defect requires correction. Stage/evidence, parent P9, tracker and audit updated with actual results.

## 5. Error handling

Refused dock/carrier/stock admission remains an engine Code; preview leaves hashes/claims unchanged. Queued workers earn no handling. Paused service retains claims; no preemption/instant fallback. Missing capability remains a composition error, no alternate route/stock substitution.

## 6. Compatibility

Development only, no legacy reader or compatibility shim. No protocol change unless actual repair needs one.

## 7. Verification

Build completed slice; ONE NEW exact bounded case only, same-case repair/reruns. Stage loops256Updates, child30000ms and termination5000ms. No historical/family/full/Oil/Tycoon/platform runtimes. Close original P9.048 only if all named semantics are genuinely covered; otherwise leave compound gate open and report exact gap.

## 8. Risks

Keep destination capacity and dock constraints distinct; don't mislabel finite scenario-authored geometry as a framework limit. Avoid sampling only terminal totals: inspect every advanced simulation step. Tool/resource/timing formulas remain engine-owned. A test may compare expected authored values but cannot execute a second scheduling rule.

## 9. Out of scope

Continuous-process/entity-tool breadth, breakdown/fuel/topology recovery, global provider rollback, arbitrary geometry/provider/stress/soak matrices, protected G19 files, Oil/Tycoon/platform/rendering, publication.

## 10. Execution order

Inspect original admission/claims/handling/dock projections; author explicit scenario; exercise public concurrent inputs and saved physical continuation; fix only evidenced native owner defects; record final-source evidence and original gate scope without aggregate closure.

Outcome: FINAL46/0/0 in5587ms; original P9.048 verified. Existing 2D native navigation/owners require no production repair. All build/runtime attempts retained; full gates OPEN. [Evidence](../../docs/migration/P9_PUBLIC_SHARED_HAUL_VERIFICATION.md).
