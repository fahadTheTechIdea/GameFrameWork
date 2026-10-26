# P11-D22 verification — 2026-10-14

Current build2.576.0/save157/replay1. Existing TileSpatialSnapshot and TileWorldSnapshot now expose public detached-value constructors. A separate non-friend ExtensionProof module implements sparse tile geometry/samples and stationary tile presence directly, reusing original immutable metadata, native TileSensor/operation controls, disclosure, clock and save owners. Three explicit master-listed JSON documents select its content. No duplicate snapshot type, native map wrapper, optical algorithm or compatibility path remains in the proof.

Only the targeted test project was built. Two initial compile failures identified the inaccessible entity snapshot constructor (4.97s and1.61s); no stale binaries were tested. The first clean compile15.59s preceded an initial44/0 independent case (368ms, PID28476/exit0/observer702ms). Removing its duplicate world snapshot and adding the shared-shape assertion preceded the final clean8.07s compile, zero warnings/errors.

Final exact bounded cases, each with30s timeout/5s termination budget, no truncation or observer error:

- `Tile sensor contracts: independent sparse space, native control and guarded samples`:45/0 in181ms, PID26640/exit0/observer342ms.
- `Sensor occlusion: authored screens, physical sight and saved continuation`:64/0 in3051ms, PID28924/exit0/observer3201ms.

Independent proof verifies actual contract sampling, unchanged authoritative hash/clock on reads, density thresholds and revisions, scoped foreign visibility, ordinary shutdown preview/command, saved disabled state and density, deterministic continuation and session isolation. Sample callbacks that change saved values, restore values with changed revision, mutate the clock, or throw all fail through observation.provider; faulted sessions neither retry callbacks nor update/save. Native regression verifies the existing physical movement and occlusion workflow. No executable edits followed the final cases.

Independent navigation, work/utility integration, all primitive and malformed provider matrices, replay/lockstep, allocation/soak and full acceptance remain open. No original acceptance checkbox changed; the large A47 audit is historical, not regenerated as current proof. No full/profile/family test run, Oil/Tycoon/platform/host change, Git staging/commit/push, or goal-completion claim.

## Historical D21 evidence (not current independent-provider proof)

Build2.575.0/save157/replay1; codecs unchanged. Native TileMap/MapLayer directly implement ITileSpatialSpace/ITileSampleLayer. TileSensorOcclusion uses those contracts and actual layer registration; original supercover algorithm, storage, revisions and single clock remain. Active occlusion callbacks are guarded with the original selected-space codec/entity observations; no new fault owner or obstacle state.

Targeted builds clean8.92s and9.31s, zero warnings/errors. Only exact case `Sensor occlusion: authored screens, physical sight and saved continuation` ran:64/0 in3180ms, PID19944/exit0/observer3339ms,30s limit/no truncation. Existing authored optical screens, native physical movement, private layer/entity sight, saved continuation, controls, paused clock and invalid JSON pass. New assertions bind the existing native map/layer to contract identities/revision, not copies.

Search confirms no TileMap/MapLayer dependency in TileSensorOcclusion; diff check passed and staged files0. Independent tile provider, arbitrary callback/primitive/type/privacy matrices, large-world performance and full acceptance remain unverified. No original checkbox changed. Historical A47 consolidated hashes/counts are not current verification. No Oil/Tycoon/platform/host/clock or Git staging/commit/push changes; no full/profile/family test run.
