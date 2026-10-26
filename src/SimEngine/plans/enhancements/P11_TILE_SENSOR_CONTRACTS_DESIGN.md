# P11-D21/D22 — Tile sensor environment contracts

> Current continuation P3-S26/build2.577.0 replaces the proof's custom presence/startup with native TilePresence and JSON WorldSetup. Sensor contracts remain unchanged. Current selected cases73/0, native64/0 and composition81/0; independent routing/full gates remain open. [Current stage](P3_TILE_PRESENCE_SPACE_CONTRACTS_DESIGN.md), [evidence](../../docs/migration/P3_TILE_PRESENCE_SPACE_CONTRACTS_VERIFICATION.md). The D21/D22 sections below describe their historical checkpoint, not the current proof's presence implementation.

## 0. Problem

TileSensorOcclusion resolved concrete TileMap/MapLayer instances. Independent tile-world providers could implement existing spatial contracts but could not supply the native sensor's geometry/sample inputs.

## 1. Goals

Read actual selected geometry and layers through contracts; retain original storage, line-of-sight rules, revisions and clock; do not impose a renderer/world-size limit or create another obstacle buffer.

## 2. Design

ITileSpatialSpace exposes existing width/height and spatial ownership. ITileSampleLayer exposes existing typed scalar samples and write revision. TileMap and MapLayer implement those contracts directly, with no new state, wrapper or codec. TileSensor resolves the selected environment contract and actual layer-provider registration, then applies the original supercover algorithm to original samples.

Layer selection rejects missing/duplicate registrations, wrong space/value type, private occluders and negative revisions. Active occlusion queries/key reads use the original selected-space codec and entity observation guard; callback mutation cannot be accepted as a healthy observation. The unoccluded path adds no spatial codec hashing overhead. Native layer revisions remain the same invalidation authority.

## 3. API/state

New public geometry/sample contracts in Map/TileSpatialContracts.cs. Existing implementations migrate in place; no interface fallback or compatibility shim. D22 also makes the existing detached TileSpatialSnapshot and TileWorldSnapshot constructors public: external providers must publish these same fact shapes without friend access or duplicate snapshots. Build2.576.0/save157/replay1; no saved payload or schema changes. Coordinates and geometry belong to the selected tile provider, not a game-platform SDK. The engine's TileMap is not Godot.TileMap; Godot rendering APIs remain outside this work.

## 4. Files

TileSpatialContracts.cs; TileMap.cs interface declarations; TileSensorOcclusion.cs contract reads and guarded callbacks; TilePresence.cs detached snapshot constructors; current build/protocol; focused SensorOcclusionTests and IndependentTileSensorTests. The external ExtensionProof assembly implements a sparse ITileSpatialSpace/ITileSampleLayer and stationary ITilePresence, authored through three explicit master-listed JSON documents. It does not reference native TileMap/MapLayer, implement optical rules, or require internal access.

## 5. Errors

Malformed/missing runtime bindings throw through the original first-fault boundary. No default sample, assumed geometry, unregistered layer or replacement provider is substituted.

## 6. Compatibility

Development-only contract change, existing native implementation retained as the sole storage owner. No parallel sensor algorithm.

## 7. Verification

Current focused independent-provider case45/0 and native authored-screen regression64/0 (109 checks, two exact bounded cases). Independent proof covers selected sparse space and sample identity, shared detached geometry, native optical sampling and threshold/revision behavior, foreign entity disclosure, operation preview/command/shutdown, save/load continuation, interleaved isolation, and state/change-restore/clock/throw callback faults. Native regression covers physical movement, scoped layer/entity sight, saved continuation, controls, paused clock and invalid JSON. Source search confirms no TileMap/MapLayer reference remains in TileSensorOcclusion.

## 8. Risks

The independent stationary byte-layer proof does not prove independent navigation/workflow integration, every malformed provider/primitive/type/privacy combination, replay/lockstep, large-world throughput/allocation budgets, or broad spatial/privacy acceptance. Those remain open. Active occlusion verifies the original selected space codec; no arbitrary-private-CLR sandbox claim is made.

## 9. Exclusions

New spatial storage, rendering, platform adapters, coordinate conversion, global world-size policy, business logic, full regression and Git publication.

## 10. Status

Implemented; scoped native and independent-provider verification complete. No original broad acceptance gate closes. [Evidence](../../docs/migration/P11_TILE_SENSOR_CONTRACTS_VERIFICATION.md).
