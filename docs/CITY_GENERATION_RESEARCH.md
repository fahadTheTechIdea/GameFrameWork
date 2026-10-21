# Research-backed city generator architecture (2026-10-21)

Status: architectural design and research; not implemented. This document is normative guidance for future work, not evidence of working City generation.

## Scope and integration
- Core spatial planning, urban topology, terrain, geometry, objects and gameplay semantics are independent of Godot.
- GameArtsSkills supplies approved graphics, mockup workflow, animations and measured atlases; never replace or bypass asset approval.
- Agentic SDLC framework supplies orchestrator discipline, ADRs, tests, evidence and quality controls.
- World geometry uses current GameArtsSkills v7.5 **orthogonal square** logical grid (16 x 16 m cells / initially 256 x 256 px ground artwork), 1 m construction snap as configured. Street geometry is continuous meters and is rasterized to world cells only at export. Do not mistake 2.5D art projection for terrain grid shape.

## Critical dependency model
1. World terrain/height, rivers, cliffs, protected areas, coast.
2. City demand/growth anchors, district land-use/density, service centers, gateways.
3. Connected, typed street graph over topography: arterials, collectors, local roads, alleys, paths. Constraints: flood/slope, crossings, dead ends, grade, block sizes, connectivity.
4. Co-generate road corridors, blocks and frontage-based parcels; snap junctions, resolve planar intersections, reject invalid parcel geometries and disconnected access.
5. Road assembly from parameterized cross sections including carriageways/lane geometry, parking, medians, curbs, curb ramps, drainage, pedestrian through zone, furnishing/tree zone, frontage and setbacks. Use orientation-aware intersection templates, not arbitrary sprite adjacency.
6. Zoning-based buildings with parcel frontage, entrance connectivity, foundation elevation, roof/render heights, accessible driveways and component attachment rules.
7. Public space, parks, gardens, plazas, footpaths, green spaces, landscaping and terrain-appropriate species profiles.
8. Placement rules for furniture, streetlights, signs, markings, signals, poles, manholes, hydrants, drains, barriers; respect clearance, crossings, access, tree roots, sightlines and lighting coverage.
9. Independent vehicle, pedestrian, service/emergency navigation; record graph and tile/polygon-derived traversal data, actor sizes, one-way segments and state-dependent gate access.
10. Resolve approved art asset IDs into Godot resources, rasterization/atlases, collision, navigation, occlusion, sorted layers, interactive scenes, validation and exports.

## Core spatial entities
WorldRegion; City; District; LandUseZone; StreetNode; StreetEdge; StreetCorridor; StreetProfile; Lane; Intersection; StreetCorner; SidewalkZone; CurbSegment; Crossing; Block; Parcel; BuildingFootprint; BuildingEntrance; Park; Garden; PlantingArea; StreetFurniture; Sign; Luminaire; UtilityFeature; TraversalLink; NavigationGraph; TerrainHeightField; DynamicComponent; AssetReference.
Every feature has stable unique ID, bounds/geometry, real-world units, owner/district ID, seed/provenance and optional rendering/interaction references.

## Street rules and semantic layers
- Four sidewalk roles: building frontage, clear pedestrian travel, furnishing/trees, and optional curbside buffer. Widths and local rules belong to street profile; do not encode one city's measurements universally.
- Curbs are sampled along continuous road borders; corners need specialized geometry, ADA-style ramps for suitable profiles and explicit crosswalk connections.
- Signs derive from street name, intersection restrictions, direction and route type; lights derive from street profile/coverage, spacing and obstruction checks. Signals are stateful timelines with conflict-free phases.
- Gardens belong to parcels, parks or medians; irrigation, plant type, sunlight and biome constraints determine vegetation choices. Adjacent gardens should share compatible borders but retain natural variation.
- The generated city must pass connected-network, legal-geometry, parcel frontage, entrance reachability, pedestrian walkability, drainage and visual-layer checks.

## Algorithms and determinism
- Compare grid, organic, radial, hybrid graph planning; avoid hardcoding one road topology.
- Use seeded stream-per-subsystem and stable IDs to localize regeneration; global System.Random consumption order should not define stability across unrelated stages.
- Use graph search/MST for major connection scaffolds, cost-guided routing respecting slope and obstacles, planarity/intersection splitting, constrained polygon partitioning for blocks/parcels, and rule-based furniture placement. These are candidate algorithms requiring profiling and tests rather than already-implemented guarantees.
- Store both high-fidelity vector semantics and chunked render raster data, with chunk-border seams and runtime-edit handling.

## Godot adapter
- Target Godot 4.6 docs (pin/test target project version). TileMapLayer plus TileSetAtlasSource for repeated surfaces and graphics, TileSetScenesCollectionSource or PackedScenes for suitable small/static scene items, standalone scenes for interactive buildings/gates.
- TileSet supports physics, navigation, occlusion and custom-data layers, but simply naming layers does not create those resources.
- Keep pedestrian/road routing graphs as independent data; export simplified NavigationPolygon regions and navigation links, avoiding excessive per-tile region polygons.
- Gate state transitions update physics, nav graph and runtime links atomically in game model; schedule Godot NavigationServer synchronization before querying updated routes.
- Preserve original opaque chroma pink GameArts PNGs; runtime import produces explicitly tracked transparent derivatives only where asset policy and approval allow—resolve policy conflict with GameArts maintainer before automatically generating them.

## Acceptance tests before marking city playable
- Same seed+profile+version yields identical canonical output. No silent invalid geometry. All parcels with required frontage can reach a usable street/sidewalk; all structure entrances reachable for intended actors.
- Non-overlapping curbs/lanes/intersections/props unless explicitly allowed. Crosswalk endpoints connect sidewalk graphs. Signs face correct traffic direction. Furniture preserves clear walkways and intersection sightlines.
- Each atlas references a real measured PNG with verified cells, pivots, padding, scale and masking. Every dynamic component has valid attachment transforms and complete directional coverage when required.
- Imported Godot world renders correct tile IDs and layers; collisions match physical obstacles; nav paths work for every agent type; closing gates blocks routes; no seam or art alignment errors.
- Benchmarks at growing city/chunk sizes; reproducible build and test logs. The foundation exporter currently writes only a scene skeleton, not a playable scene.

## References examined
- https://onlinelibrary.wiley.com/doi/full/10.1111/cgf.15053 (Chen et al., 2024, parcels/streets co-generation)
- https://link.springer.com/article/10.1007/s43762-026-00263-8 (2026, comparative procedural morphology)
- https://nacto.org/publication/urban-street-design-guide/ (street hierarchy/intersections)
- https://nacto.org/publication/urban-street-design-guide/street-design-elements/sidewalks/sidewalk-zones/ (cross-sectional zones)
- https://docs.godotengine.org/en/4.6/classes/class_tileset.html (tile resource types/layers)
- https://docs.godotengine.org/en/4.6/tutorials/navigation/navigation_using_navigationmeshes.html (navigation design/performance)
- https://docs.godotengine.org/en/4.6/tutorials/navigation/navigation_using_navigationservers.html (synchronization)
