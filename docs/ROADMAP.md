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

Reference: GameArts master v7.5 orthogonal square tiles. Store logical geometry as world coordinates; do not conflate camera projection and grid topology.
