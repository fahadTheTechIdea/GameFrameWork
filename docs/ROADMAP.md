# World generator roadmap

## Current vertical slice
- Deterministic C# desert grid, elevations, simple cross roads, structures and gate state metadata.
- World JSON and Godot 4 TileMapLayer scene skeleton; no TileSet resource, painted map, nav polygon, animated component or atlas export yet.

## Next milestones
1. World-stage dependency graph, independent random streams, terrain continuity, chunk generation and cancellation/memory tests.
2. Height/slope topology, multi-biome distributions and constrained island/coast/forest generation.
3. Road connectivity graph, settlement planning, accessibility and semantic buildings.
4. Approved GameArts asset provider, measured sheet atlases, color-key ingestion and traceable provenance.
5. Godot 4 TileSet and TileMapLayer painting, collision polygons and navigation surfaces, asset layers and occlusion; verify in-engine.
6. Stateful animated gates, parent-child attachment metadata and navigation invalidation.
7. Claude/Codex installer with preview and non-destructive integration, automated CI and end-to-end Godot samples.

Reference: GameArts master v7.7 orthogonal square tiles. Store logical geometry as world coordinates; do not conflate camera projection and grid topology.

## SDLC governance and current engineering debt (2026-10-21)

**Mandatory authority:** [agentic-sdlc-framework](https://github.com/fahadTheTechIdea/agentic-sdlc-framework) canonical `master`; repository routing in `AGENTS.md` and `CLAUDE.md`. GameArtsSkills remains the art lifecycle authority. Work directly on `main` only.

| Requirement | Status | Exit evidence |
| --- | --- | --- |
| Inspect canonical SDLC constitution, engineering, quality, evidence and operational policies | Reviewed | GitHub canonical source read 2026-10-21 |
| Project instruction routing for Codex/Claude | Committed | `AGENTS.md`, `CLAUDE.md` |
| Adopt the canonical SDLC installer and skills non-destructively | Not done | Preview and reviewed install diff; no overwrites |
| Reproducible .NET solution build/test and CI | Not verified | Recorded SDK, command, logs, commit SHA |
| Shared log, audit and notification services for executable operations | Not implemented | Working services, coverage matrix and failure cases |
| SDLC task/decision traceability and architecture review | Partial | Acceptance criteria/ADR/verification evidence linked to changes |
| Constraints replacing random-looking prototype placement | Not implemented | Validity/provenance and deterministic graph tests |
| GameArts approval + real PNG/atlas checksum validation | Structural gate only | Hashes and measured files checked and asset lifecycle integration |
| Godot rendered/navigation/collision/runtime validation | Not implemented | Actual Godot engine test logs and visual review |
| Claude/Codex live evaluations | Not run | Host-specific outcome evidence |

Never mark a gate complete on source inspection alone. Scope service/assurance controls proportionately; reuse what is already available.
