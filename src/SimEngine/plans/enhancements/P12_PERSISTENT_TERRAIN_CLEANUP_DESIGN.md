# P12-E1: Physical cleanup of persistent terrain state

## 0. Problem

The completion audit leaves persistent contamination/cleanup unproven. Existing incidents already own response phases and machine work; selected spatial providers already own durable typed layers and atomic sample batches. There is no authored physical work effect connecting earned cleanup to those layer values.

## 1. Goals

1. Author a contract-based tile-terrain work effect in an explicitly master-listed JSON module.
2. Apply an authored exact value to a fixed worked target footprint only after positive machine work, through existing physical completion and selected atomic sample contracts.
3. Reject invalid metadata, foreign private layers and resource-quantity layers; cleanup must not delete reserved material or fabricate resource disposal.
4. Add cleanup as a dependent response node after repair, using the existing DAG/incident/stock/movement/clock owners.
5. Prove finite physical supply, partial cleanup saves/pause, no early terrain changes, final persistent clean state and replay on native and independent tile providers.

## 2. Design

One `ITileTerrainWorkEffectDefinition` binds a resolved layer and exact target value. One declaration authors metadata; one shared contract algorithm validates selected runtime capability and builds target-footprint writes. Native registration implements contracts directly on Def. A content-only optional module validates every loaded effect before Initialize; it adds no system, clock, terrain store or work queue.

Existing PhysicalWorkEffectContracts verifies earned work/actual order/subject. Existing ITileSampleMutationSpace performs whole-footprint publication. Resource-quantity channels are explicitly excluded: their changes require existing resource-transfer/receipt paths, not terrain resets. Fixed target geometry and private-layer ownership are mandatory. This is a generic terrain-work primitive, not a contamination ledger or scientific transport model.

The authored proof uses a persistent contamination channel initialized by scenario content. A file-authored response graph uses repair -> four-minute cleanup, each consuming one finite steel unit; source and target select that authored workflow. The cleanup node writes zero to its worked footprint. One mask/episode/work owner remains; no driver changes terrain, incident state, progress or resources. Metadata and graph authoring live in the explicit JSON add-on, not C# scheduler code.

## 3. API/state

New optional sim.tile-terrain-work content module; tile_terrain_work_effects JSON section; metadata contract/declaration and normal registration. Existing spatial/work contracts unchanged. Engine2.593.0/save157/replay1; actual layer and DAG codecs already save all state.

## 4. Files

New Standard/TileTerrainWorkEffects.cs; protocol identity/expectation; existing independent work composition; one authored cleanup document/embedding; focused physical cleanup tests; tracker/evidence.

## 5. Errors

Missing/wrong selected layer or atomic capability fails composition. Invalid native value/resource-quantity channel refuses before Initialize. Missing/foreign/movable target or private-layer owner mismatch returns existing typed refusal. No partial publication or instant resource destruction.

## 6. Compatibility

Current direct contracts only; no legacy facade, alternate state or fallback map.

## 7. Verification

Two clean targeted compiles6.71s/7.24s. Final exact native/independent cleanup cases183/0 each:366 distinct successful checks. Both initial runs were green before final workflow metadata was moved wholly into the authored file; the same exact cases were then rerun. [All attempts and scoped evidence](../../docs/migration/P12_PERSISTENT_TERRAIN_CLEANUP_VERIFICATION.md). Bounded stage waits/watchdogs; no full/profile/family run.

## 8. Risks

Automatic spill deposition/contamination propagation/weather chemistry, clean-material capture/disposal, private multi-owner scenarios, hostile callbacks and global allocation/lockstep/soak remain broader gates. Current generic mutation batching has its existing collection/allocation constraints, not a game-world size policy.

## 9. Exclusions

Oil/Tycoon/platform/Draw/host logic, full pollution science, a second contamination ledger/scheduler, terrain mutation without earned work, full regression and Git publication.

## 10. Status

Implemented and scoped verified on native and independent tile providers. Terrain cleanup requires actual supplied material and positive-time work, preserves partial/pause/disabled-machine state and publishes the complete footprint through original atomic storage. Response DAG/layer saves and replay remain authoritative. Spill deposition, scientific transport, disposal/capture and broader incident/private/provider/global gates stay open; whole goal active.
