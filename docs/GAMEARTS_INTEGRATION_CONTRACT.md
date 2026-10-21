# GameArts integration v0.1 — implementation boundary

GameFrameWork is one product with two parts: Claude/Codex skill framework and executable .NET engine. GameArtsSkills remains the canonical authority for art production, approvals, animated sprite sheets and atlas measurement.

Current C# implementation: `WorldBlueprint`, `AssetRequirement`, `ComponentRequirement`, `ApprovedArtManifest`, `IApprovedArtRegistry`, file-system manifest registry, `ApprovedArtValidator` and `WorldBuildGate`.

Lifecycle: derive semantic world and component inventory -> GameArts mockup approval -> GameArts production bundle approval -> sync immutable approved artifact manifests -> validate -> world art compilation -> Godot import/runtime tests. Asset registry does not fabricate approval.

The validator currently performs **schema-level and structural checks**, not image, file SHA-256 content verification, atlas frame measurement or actual GameArts provenance verification. A valid result here is insufficient for game-ready artwork. No Godot runtime, art generation, or moving-part sprite-sheet generation is implemented by these contracts.

Manifest JSON uses `schemaVersion:1`, `assetId`, `gameArtsRevision`, `standardVersion`, `approvalState:4` for Approved (numeric enum until serializer policy is pinned), `sheets`, `attachments`. Every sheet requires PNG/atlas paths and SHA-256 fields. Every frame-animated moving component must have sheet coverage for every requested direction; every moving component needs per-direction named attachment anchors.

Follow [GameArtsSkills v7.5](https://github.com/fahadTheTechIdea/GameArtsSkills/blob/main/GAME_ARTS_MASTER_STANDARD_v7.5.md); do not create an incompatible parallel art convention. The next slice must map GameArts' real schema and approval records to this manifest, verify actual file hashes/atlases/images, and enforce orientation/pivot, nesting and native scale. Keep original opaque #FF00FF source masking.
