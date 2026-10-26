# P3-S26 — Native tile presence on independent spatial providers

## 0. Problem

Map/TilePresence.cs required a concrete TileMap service both during TilePresenceDeclaration.ValidateComposition and during TilePresence.Move. The independent sensor proof consequently implemented another presence runtime and startup stage. That prevented reuse of the native coordinate/codec owner and the ordinary JSON setup pipeline.

## 1. Goals

1. Admit native tile presence against actual ITileSpatialSpace geometry, including exact authored dimensions; reject missing/mismatched geometry before initialization.
2. Delegate occupancy relocation through a capability contract while native TilePresence alone publishes coordinates, heading, interpolation history and entity version.
3. Reuse native presence and WorldSetup in the independent engine module, removing custom presence/runtime/startup paths without compatibility code.
4. Verify native movement/disclosure and composition with bounded focused cases; do not equate this with the complete plan or independent pathfinding integration.

## 2. Design

Map/TileSpatialContracts.cs declares ITileRelocationSpace : ITileSpatialSpace with Relocate(World, ITilePresence, TileCoord). Stationary native presence requires geometry only; mobile presence additionally requires relocation. TilePresenceDeclaration compares the selected provider dimensions to SpatialSpaceContracts.ReadGeometry for the actual scenario. Existing kernel presence admission/observation remains responsible for all-loaded validation and callback safety before initialization.

TilePresence.Move resolves the actual selected provider, validates live session/bounds/health, preflights entity version overflow, and calls relocation before publishing position/heading/tile and Entity.Touch. The original EntityObservation provider guard verifies the source codec, entity/clock/identity/random facts after a successful relocation callback. Spatial occupancy changes are legitimate here; source/clock mutations are not. Normal blocked/out-of-bounds relocation still refuses without publishing coordinates. Provider exceptions retain their original semantics; this is not a hostile CLR sandbox or a whole-world rollback mechanism.

TileMap implements the new contract directly on its original Relocate method and existing occupancy index. No additional native occupancy store, coordinate owner, update loop, state machine or clock is created. Public relocation checks actual live mobile presence and selected-space ownership before occupancy writes.

ExtensionProof/IndependentTileSensorSpace.cs now implements geometry, sample and placement/relocation contracts only. Its existing sparse optical values remain provider-owned; occupancy derives from actual entities. SensorTilePresenceDef, SensorTilePresence and the custom Starts WorldGenStage are deleted. Native tile_presence JSON and WorldSetupModule create and restore real native TilePresence components. The ordinary startup profile is explicitly listed in the existing master JSON; no hidden initialization or host gameplay is added.

## 3. API and state

New ITileRelocationSpace capability; existing TileMap relocation method becomes its public implementation. TilePresence consumes contracts rather than concrete map/service types. Build2.577.0/save157/replay1. Native saved coordinate and layer payloads unchanged. No compatibility alias or wrapper.

## 4. Files

- Map/TileSpatialContracts.cs: relocation contract.
- Map/TilePresence.cs: composition, actual provider selection and guarded coordinate publication.
- Map/TileMap.cs: original occupancy implementation and public admission.
- Kernel/SaveGame.cs and testdata/current-engine-protocol.json: current engine identity.
- ExtensionProof/IndependentTileSensorSpace.cs and its master/world/startup JSON: native runtime/setup reuse.
- IndependentTileSensorTests.cs: relocation protocol, saved geometry/history and invalid callback checks.

## 5. Refusal behavior

Missing tile geometry, mismatched authored dimensions or missing mobile relocation capability refuse composition. Nonlive/foreign/out-of-bounds movement refuses before coordinate publication. Occupied destinations retain original coordinates/state. A successful callback that mutates the source or clock faults through the existing observation.provider owner; faulted sessions neither update nor save. Sensor sample fault behavior remains unchanged.

## 6. Compatibility

Development-only replacement of concrete dependencies. Custom proof presence/startup code is removed, not retained as a second path. Existing native geometry/storage/navigation remain normal implementations, not legacy wrappers. The engine TileMap is unrelated to Godot.TileMap; platform rendering is excluded.

## 7. Verification

Three exact bounded focused cases: independent contracts73/0, native physical occlusion64/0, selected/unused presence composition81/0. The independent movement assertions exercise coordinate/occupancy protocols directly, not a player teleport command or timed pathfinding acceptance. They verify blocked/foreign/bounds refusal, native coordinate publication, saved interpolation/history, ordinary Update settlement/public snapshot publication and sole clock progression. Native regression retains actual player-command travel. [Evidence](../../docs/migration/P3_TILE_PRESENCE_SPACE_CONTRACTS_VERIFICATION.md).

## 8. Open risks

Independent routing/native mover integration, work/utility dispatch, all geometry/provider/lifecycle matrices, arbitrary callback exceptions/private-field effects, replay/lockstep, performance/soak and broad original acceptance gates remain open. Focused proofs do not establish full engine or Oil readiness.

## 9. Exclusions

Draw/rendering, Godot/Unity/adapters, business/Tycoon/Oil changes, world-size policy, second state owners, full/profile/family testing and Git publication.

## 10. Status

Contract migration and scoped verification complete. Full goal remains active; no original compound acceptance gate closes.
