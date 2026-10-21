# GameArts integration v0.1 — implementation boundary

GameFrameWork is one product with two parts: Claude/Codex skill framework and executable .NET engine. GameArtsSkills remains the canonical authority for art production, approvals, animated sprite sheets and atlas measurement.

Current C# implementation: `WorldBlueprint`, `AssetRequirement`, `ComponentRequirement`, `ApprovedArtManifest`, `IApprovedArtRegistry`, file-system manifest registry, `ApprovedArtValidator` and `WorldBuildGate`.

Lifecycle: derive semantic world and component inventory -> GameArts mockup approval -> GameArts production bundle approval -> sync immutable approved artifact manifests -> validate -> world art compilation -> Godot import/runtime tests. Asset registry does not fabricate approval.

The validator currently performs **schema-level and structural checks**, not image, file SHA-256 content verification, atlas frame measurement or actual GameArts provenance verification. A valid result here is insufficient for game-ready artwork. No Godot runtime, art generation, or moving-part sprite-sheet generation is implemented by these contracts.

Manifest JSON uses `schemaVersion:1`, `assetId`, `gameArtsRevision`, `standardVersion`, `approvalState:4` for Approved (numeric enum until serializer policy is pinned), `sheets`, `attachments`. Every sheet requires PNG/atlas paths and SHA-256 fields. Every frame-animated moving component must have sheet coverage for every requested direction; every moving component needs per-direction named attachment anchors.

Follow [GameArtsSkills v7.7](https://github.com/fahadTheTechIdea/GameArtsSkills/blob/main/GAME_ARTS_MASTER_STANDARD_v7.7.md); do not create an incompatible parallel art convention. The next slice must map GameArts' real schema and approval records to this manifest, verify actual file hashes/atlases/images, and enforce orientation/pivot, nesting and native scale. Keep original opaque #FF00FF source masking.

## 2026-10-21 integration review: GameArts v7.7 additions

GameArtsSkills canonical latest master document: [v7.7](https://github.com/fahadTheTechIdea/GameArtsSkills/blob/main/GAME_ARTS_MASTER_STANDARD_v7.7.md). Do not keep v7.5 pinned in current instructions without a user-requested legacy compatibility profile. Recheck the actual revision when installing or building.

**Now executable in GameArtsSkills**:
- `scripts/build_sheet_atlas.py` produces opaque chroma-pink source sprite sheet and measured `*.atlas.json` from artist-approved frame images, provided pivots and anchors. `scripts/validate_sheet_atlas.py` checks a subset of invariants.
- `scripts/build_component_bundle.py` produces distinct component-by-direction PNG/atlas pairs and a `*.bundle.json` containing hierarchy, actions, nested/time-varying attachment transforms and sampled composed poses; `scripts/validate_component_bundle.py` validates paths/directions/pixel metadata.
- `godot/addons/gamearts/game_arts_component.gd`, `game_arts_component.tscn`, `chroma_key.gdshader` and `scripts/generate_godot_scene.py` provide an initial Godot composition/playback route using normalized animation phases.
- Source unused pixels remain **opaque exact #FF00FF**; default runtime handling is the supplied shader, not creating unapproved transparent replacement sources.

### Required adapter updates
1. Add an explicit versioned GameArts-native `*.atlas.json`/`*.bundle.json` reader and adapter instead of assuming the old GameFrameWork `ApprovedArtManifest` is already the authoritative on-disk GameArts schema. Keep approval provenance separate and validate it using GameArts lifecycle records.
2. Read real PNG bytes, compare actual SHA-256 with approval records, parse measured cell/frame rects, duration, pivots, anchors, direction/action metadata and verify each against actual PNG dimensions; use GameArts native validator as complementary evidence, not a substitute for world-side checks.
3. Preserve child hierarchy and per-facing sheets, parent mount points, normalized action time and composition matrix order: parent_world × parent_frame_mount_offset × animated_mount × child_rest × child_motion. Validate before compiling a Godot scene.
4. When targeting Godot, reuse the shipped component addon/shader instead of creating a second incompatible animation playback model. Instantiate resources with real bundle paths and report unverified Godot playback.
5. Keep C# world-placement semantics separate from sprite pixel anchors, and make any world-meter → visual projection explicit. Do not infer mounts from a mockup image or pretend those measurements are already available.
6. Agent controls GameArts mockup and sprite approvals; C# world generation uses approved style and design grammars and does not arbitrarily place content.

### Current implementation gaps
`GameArtsFileVerifier` only checks hashes, PNG IHDR and JSON syntax; it does not parse GameArts native atlas/bundle fields, verify #FF00FF pixels, check pivots/rectangles, or execute Godot. `WorldArtAssembler` structurally binds object IDs to a simplified GameFrameWork manifest, not a real approved component bundle. None of these gaps are resolved merely by this documentation update.

**New acceptance evidence**: approved mockup → approved source component frames → real GameArts atlas/bundle builder and validator → C# native bundle import/verification → Godot scene generated with supplied addon/shader → actual Godot headless and visual motion checks.
