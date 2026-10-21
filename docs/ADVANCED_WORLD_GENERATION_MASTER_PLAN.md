# ADVANCED WORLD GENERATION — MASTER ENGINEERING PLAN
Date: 2026-10-21 | Status: research-backed implementation plan, NOT completed implementation
Target: GameFrameWork in C#/.NET, engine-independent core, Godot 4.6 adapter.
Git rule: work directly on `main`; do not create extra branches or PRs. Maintain a master progress tracker with evidence.

## 0. TWO-PART PRODUCT AND MANDATORY GAMEARTS INTEGRATION (supersedes optional-adapter language below)

GameFrameWork is **one coordinated product with two deliverables**:
1. **World Generation Agent Framework:** Claude Code and Codex installable orchestrator, specialist skills, configuration, research, world planning, approved mockup/production-art lifecycle, controlled execution, reviews, quality gates and Godot deployment skills.
2. **World Generation Engine:** working C#/.NET libraries, domain rules and planners, asset manifest contracts, generators, CLI, validators, Godot 4 adapters, sample worlds and tests.

Neither part may be merely documentation for the other. The agent framework drives the real C# engine through typed CLI/API calls and reads machine-verifiable output; the engine works without an agent when invoked directly. Both share one canonical world specification, versioned schemas, asset IDs, progress states and diagnostics.

**GameArtsSkills is a first-class, mandatory production dependency**, not an optional post-generation integration. Preserve GameArtsSkills as the authoritative source of artwork policies, mockup workflow, animation rules, masked originals and measured sheet atlases; GameFrameWork must integrate it end-to-end without copying divergent versions of its standards. Use a pinned, version-reported provider/manifest contract and a supported local install/sync adapter, with clear compatibility errors when unavailable.

### Required end-to-end generation transaction
User specification -> agent derives validated WorldBlueprint and plans semantic terrain/streets/buildings/objects -> generates/locates scene mockup -> **explicit mockup approval** -> decomposes artwork into static and animated component requirements -> finds approved reusable assets or requests GameArts production -> **explicit production bundle approval** -> validates each PNG/atlas, anchors, scales and directional/component coverage -> resolves all required asset IDs -> deterministic C# spatial generation and rendering compilation -> Godot TileMapLayers/PackedScenes/navigation/physics/lighting -> headless import, in-engine state checks and visual review -> publish validated output. Planning/provisional layout can proceed while art is pending, but a map requiring missing production artwork cannot be labeled visually complete or game-ready. Missing capabilities and rejected/awaiting approvals are explicit typed states, never silently bypassed.

### Shared contracts and ownership
- `WorldBlueprint`: geographic and settlement constraints, semantic layouts, visual style, version/seed, rules, required asset IDs, approval requirements.
- `AssetRequirement`: semantic type, intended placement/function, dimensions (meters), terrain/asset perspective, directions, moving component inventory and action coverage.
- `ApprovedArtManifest`: immutable GameArts origin/revision, approval proof/status, hash, files, mask policy, measured atlases, world-space scale and licence/provenance.
- `ComponentAssembly`: static master, typed child IDs, parent/child named attachment anchors, per-direction/frame local pivots, rest/animated transforms, occlusion and collision/nav state.
- `WorldBuildManifest`: generation dependencies, source hashes, validation reports, Godot artifacts, incremental rebuild mapping.
- Shared stable versioned schema definitions define the engine/agent boundary. Skills cannot invent asset geometry or claim artifacts exist without verifying actual outputs.

### Modular animation rule
For **all** applicable building, structure, vehicle and other visual assets: detect moving/interactive/state-changing parts and default to separate static master plus independently reusable or asset-specific animated components. A door, window shutter, gate, fan, vehicle tire/wheel, warning lamp etc. should have separate sprite sheet plus measured JSON atlas **when frame-based motion is the appropriate fidelity method**; support transformed/static art, shader and procedural alternatives only with documented choice and validated matching appearance. Do not bake whole buildings/vehicles into each moving-part frame. Wheels must use physically sized reusable families and compatible mount geometry: diameter, width, rim/tread, attachment position, facing, steering and suspension; do not stretch one tire sheet for incompatible vehicles. Preserve current GameArts master rules for opaque chroma-mask source PNGs, approved revision lifecycle, directional coverage and nested attachment motion. Every emitted runtime scene must preserve animated-component separation and respond to semantic state changes (e.g. opening a door changes collision/traversal when configured).

### Packaging, install and readiness
- One clear user-facing world-generation installer for Claude or Codex, preview/diff first, preserve existing AGENTS.md/CLAUDE.md and installed GameArts/SDLC integrations.
- Agent skill orchestration defaults to lazy skill loading; world generation invokes GameArts only when an art requirement/approval is involved, but art compliance is always mandatory.
- C# engine exposes pure/testable APIs, deterministic CLI, structured diagnostics and well-defined Godot export; no mandatory Claude/Codex runtime for consumers of compiled libraries.
- Integration/e2e tests MUST exercise an agent-produced spec, a real approved GameArts fixture, a C# world build, art+atlas validation, Godot import and component animation. Simulated/synthetic fixtures are labeled as such.
- Track separate versions of WorldFrameWork/engine, skill pack, GameArtsSkills standard, Godot target and asset profile. Detect incompatibilities before writes.
- Avoid circular dependency: GameArtsSkills owns artwork generation standards, GameFrameWork owns world semantics, placement and compilation, SDLC framework owns engineering process.

## 1. Product objective
Generate complete, coherent and editable 2D worlds including cities, towns, villages, wilderness, forests, deserts, islands, coasts, mountains, industrial facilities and mixtures of these. All objects have justified positions; every rule is world/region/settlement/district/parcel/object scoped. Outputs include inspectable semantic world records, visual mockups (approved via GameArtsSkills), source graphics and measured companion atlases, static TileMapLayers and dynamic scenes, route/navigation/physics/height/occlusion/lighting and validation evidence. No random scattering, and no false game-ready claims.

## 2. Research conclusions and architectural decisions
- CityEngine/urban morphology research supports street centerline graphs -> corridors/intersections -> blocks/parcels -> rule-based building envelopes and details; retain editability across incremental changes.
- Cities: Skylines II: typed/upgradeable roads, intersections and land-use zoning; roads can carry utilities and influence movement, services and frontage. Its public features are reference mechanics, NOT proprietary source.
- Manor Lords: settlements, plots, gardens and extensions are shaped by landscape, road frontage, functional demand and service/resource constraints.
- Wave Function Collapse handles local compatibility but does not guarantee global accessibility, hydrology or coherent settlement economy. Use WFC/constraint propagation *only as a local detail compositor* after global structural constraints are satisfied.
- Godot 4.6 TileMapLayer/TileSet/AtlasSource have per-tile physics/navigation/occlusion/custom data; stand-alone scenes are appropriate for dynamic multi-part components. Large continuous walkable areas may benefit from simplified navigation meshes over expensive tile-by-tile navigation regions.
- Separate procedural *world meaning* from *graphics* and *Godot export*. No game-specific hardcoded geometry in a terrain graphics painter.
- GameArtsSkills v7.5: orthogonal square logical world geometry, 16×16 m cells, initially 256×256 px terrain, and approved chroma pink (#FF00FF) source sprite sheet+measured atlas workflow. Runtime transparency transformation must be an explicitly approved/traceable integration step, not accidental alpha output.

## 3. Core architectural layers
A. Contracts: units/meters, planar geometry (points, polylines, polygons, holes), elevation/hydrology, typed graphs, entities, stable IDs, provenance, constraints, diagnostics, versioned schemas.
B. World orchestration: typed DAG stages, deterministic versioned random streams scoped by world/region/stage/entity, caching, invalidation dependencies, cancellation and progress/audit.
C. Environment: tectonic-scale landform inputs (configurable), multi-resolution elevation, rivers/drainage and lakes, coastlines, climate, soil, climate zones, resources, hazards, seasonal states.
D. Rule profiles: inheritance + composition global -> biome/region -> settlement -> district -> block -> parcel -> component, climate, culture, era, economic role, population and growth.
E. Spatial planning: centerline transport graphs, corridor profiles, intersection geometry, urban parcels/building plans; ecological planting communities, watershed and habitat polygons, functional industry layouts.
F. Simulation semantics: movement, water/vegetation growth, building demand, service reachability, utilities, gates, signals, moving components, environmental dynamics; configure depth per game.
G. Asset composer: approved GameArts mocks -> linked static masters -> individual moving parts and directional sprite sheets -> per-sheet measured JSON atlases -> attachment/animation manifests. Reusable compatible tire/wheel sets by physical dimensions, vehicle type, perspective and mounting standard. No one fixed wheel sprite size.
H. Engine adapters: Godot 4.6 maps to TileSetAtlasSource, TileMapLayer, PackedScene/AnimatedSprite2D/AnimationPlayer, collisions, NavigationRegion2D/NavigationLink2D, occluders, lights, sorting and runtime state syncing.
I. Agents/tools: C# SDK, CLI, optional MCP server, installable Codex/Claude skills with preview-before-write installer and configuration validation.

## 4. World design and object-placement contract
For EVERY generated object record: feature ID, parent feature and role, evaluated constraints, geometry, elevation, orientation, anchor, purpose, inputs, score and rejection diagnostics, world seed/stage/version, dependencies, asset refs, spatial bounds, state and nav/collision outputs.
Mandatory process: need/purpose -> feasible polygon/graph/attachments -> hard constraint filtering -> scored functional optimization -> dependent feature generation -> validation. Controlled seeded variety may only choose within feasible candidates. No feasible placement => explicit diagnostic/unmet demand, not fallback scattering.

## 5. Environment detail
Terrain: deterministic multiscale height fields + plateaus/ridges/basins + slope/aspect; apply prioritized hydrologic correction and consistent drainage; river and lakes graph depends on depression handling, flow accumulation and rainfall.
Climate: seasonal rainfall, temperature, evaporation, latitude/elevation, prevailing wind and exposure; soil/moisture/ground permeability; coast salinity, inundation and erosion bands.
Biomes: derive communities from environmental envelopes and transitions, no abrupt arbitrary square boundary. Forest stand simulation with canopy competition and understory suitability; shrub/grass influenced by shade, soil and disturbance. Urban gardens are managed/irrigated planting rather than wild biome output.
Islands/coasts: shoreline topology, beach/dune/mangrove or salt marsh compatibility, harbor accessibility, flood buffers. Mountain: steep-slope construction exclusions and traversable pass placement.
Streaming: hierarchical regional world blueprint, deterministic 16×16m logical grids + smaller raster/render tiles, chunk halos and dependency graphs, world-space coordinates independent of raster scale, seam tests across chunk ordering.

## 6. Urban and settlement detail
City role/demand/capacity -> district demand -> primary road access and freight -> street graph(s) -> road geometry (lanes/medians/curbs/bikeways/walkways/drainage) -> intersections/ramps/crosswalks -> blocks -> frontage/parcel split -> building program and utility/service connections -> footprints/envelopes -> façades/details/animated parts -> roadside furniture.
Neighborhoods: dense downtown, residential, industrial, commercial, civic, suburban, historic, rural, university, port and mixed use. Settlement class (village/town/metropolis) is distinct from economic function, culture, technology period, landscape and densities.
Street signs/lights/trees: derive from actual approaches, line-of-sight, utility corridors, walkway clearances and coverage. Gardens, parks, yards and hedges have intentional design programs.
Building planner: generate types/counts/capacities by demand & purpose, prerequisite services, land-use legality, transport/loading/footpath/utility ports, footprint/slope/setback; explicit unmet capacity diagnostics.
City evolution: building phases, renovation/upgrades, zoning edits and incremental dependency recompute. No global destruction of unaffected custom edits.

## 7. Component art and animations
Discover every moving, interactive or visibly state-changing component (doors, gates, shutters, windows, fans, vehicle wheels of different sizes, lights, sign displays, elevators, machinery). Default separated static parent + child component; image/transform/shader hybrid chosen per motion complexity.
Make separate spritesheets for actual frame-based moving parts, exactly measured JSON atlas per sheet. Maintain named anchors in parent-local meters AND measured pixel coordinates per direction/frame; stable pivots, nested transforms, render/occlusion and state/facing synchronization. For 8-direction parents, every visible moving part must have compatible coverage for all 8 views or validated direction-independent procedural rendering.
Wheel family library: diameter, width, rim style, mount interface, perspective projection, torque/roll direction, suspension/steering, spin pose/frame timing and tire variants. Reuse only after size/anchor/art consistency validation; do not arbitrarily upscale.
Gates: logical state and transition progress drive both animation and navigability/physics. Synchronize Godot server updates before path queries.
Approval: mockup approval != production asset approval. Do not silently generate all production sprites before resolving missing approved mocks. Source masked PNGs never replaced by unapproved derivations.

## 8. C# solution/modules
GameFrameWork.Core; .Geometry; .Graphs; .Rules; .Generation; .Terrain; .Hydrology; .Climate; .Biomes; .Ecology; .Settlements; .Roads; .Parcels; .Buildings; .Infrastructure; .Assets; .Animation; .Navigation; .Physics; .Godot; .CLI; .AgentTools; integration & property-based tests. Create modules incrementally, avoid premature dozens of empty projects.
Canonical WorldDocument with spatial entity stream, graph stores, raster grids, asset registry and simulation events; versioned serialization and semantic validation. Godot dependency ONLY in adapter.

## 9. Delivery stages & exit gates
P0 — Foundation hardening: compile existing code, inspect baseline and GameArts schemas, build solution, unit tests, CI, profile version pin, fix prototype arbitrary placement and annotate limitations. **Gate:** clean reproducible build, tests and architecture ADR.
P1 — Geometry/rules/DAG: continuous meter geometry, immutable typed features, graph abstraction, policy resolver with priorities, constraint solver, diagnostics, stable object IDs/stage RNG/invalidation. **Gate:** invalid placements rejected with explainable diagnostics, deterministic fixtures.
P2 — Advanced terrain/hydrology: multi-scale terrain, watershed, cliffs, rivers, lakes, climate/soil and biome masks; chunk seams. **Gate:** river flow/downhill validation, reproducibility and chunk-order invariance, biome plausibility fixtures.
P3 — Settlements and roads: role/demand hierarchy, network routing optimized for slope/connectivity, continuous street profiles, intersections, curbs, sidewalks, markings. **Gate:** connected road and walkway graph, valid junctions/offsets and no illegal geometry.
P4 — Parcels/buildings/infrastructure: block faces, frontage subdivision, building programs and feasible envelopes, services, gardens, street furniture, landscaping. **Gate:** service demand coverage, accessible entrances, all props justified/clearances.
P5 — Ecology & biomes: vegetation communities, competition, maintained/natural planting, island/coastal/forest/desert rules. **Gate:** habitat viability, canopy & shade consistency and no misplaced biome assets.
P6 — Asset system/animations: GameArts approved artifact registry, modular static+moving part sprite sheets and atlases, component library/wheel families, state machine. **Gate:** frame/attachment/pivot/scale/direction validated for *actual* artwork.
P7 — Godot full export/runtime: TileSet/TileMapLayer compilation with physics, navigation, occlusion, lights, height/layer projection, PackedScene component assembly, streaming and dynamic updates. **Gate:** actual Godot headless import + scene startup + interaction tests, visual verification.
P8 — AI/dev tooling: natural-language spec -> validated plan -> CLI/skills install, preview/diff, mockup/asset approval handoffs; optional MCP. **Gate:** Claude and Codex smoke-test tasks, installation idempotence.
P9 — World-scale hardening: LOD/culling, streaming, incremental updates, profiling, visual scoring, golden seeds, stress tests, performance baseline, packages and docs. **Gate:** agreed desktop/mobile target budgets and sample worlds.

## 10. Cross-cutting tests
- Property-based graph/geometry valid topology, no self-intersections.
- World-to-render coordinate roundtrips; layer and occlusion integrity.
- Same seed+version+profile canonical byte-equality; changed order of chunk generation same output.
- Graph path validity for pedestrians, cars, trucks, emergency vehicles and utility ports.
- No arbitrary placeable object without a PlacementDecision.
- Animation samples cover directions/components; gates/wheels never detach.
- Atlas schema validated against real PNG bytes, opaque #FF00FF unused pixels, correct native scale, individual PNG + JSON deliverables; no ZIP-only packages.
- Running Godot engine project tests, not merely parsing tscn.
- Review evidence with failed and skipped checks explicitly exposed.

## 11. Representative reference worlds (golden fixtures)
1. Desert industrial town with residential district, service/utility roads, depot, functional well-site and animated gate.
2. Agricultural village with land fertility, irrigation/drainage, crop plots, orchards, hedges and local market.
3. Coastal tourism town with drainage/flood exclusions, port roads, beach-to-inland biomes, promenades.
4. Modern grid downtown with traffic controls, zoning, curbs, walkways, lamps/signs and service access.
5. Organic hillside settlement with adapted road grades, steps and constrained parcels.
6. Forest region with mature stand/shrub/groundcover layers, trails, wetlands and river corridors.
7. Vehicle test world with reusable wheel family sizes, rotation/steering, mounted pivot alignment and action synchronization.

## 12. Tracked decisions/risks
- Confirm target Godot 4.x pinned minor version before writing import formats; Godot 4.6 docs were researched, but target project may differ.
- Formalize world-meter to sprite pixel calibration and world coordinate precision.
- Licensed/approved asset import and policy-compliant conversion from source chroma mask to runtime resources.
- Distinguish dynamic simulation accuracy from visual appearance; prioritize functional world consistency.
- City scale may require multi-level pathfinding and off-screen simplified simulation.
- Define performance budgets from sample scenes instead of inventing FPS/memory targets.

## Research links
https://link.springer.com/article/10.1007/s43762-026-00263-8
https://link.springer.com/chapter/10.1007/978-981-15-8983-6_35
https://onlinelibrary.wiley.com/doi/10.1111/j.1467-8659.2012.03047.x
https://doc.arcgis.com/en/cityengine/latest/get-started/get-started-about-cityengine.htm
https://www.paradoxinteractive.com/games/cities-skylines-ii/features/road-tools
https://www.paradoxinteractive.com/games/cities-skylines-ii/features/zones-signature-buildings
https://www.paradoxinteractive.com/games/cities-skylines-ii/features/city-services-districts-policies
https://wiki.hoodedhorse.com/Manor_Lords/Burgage_plot/en
https://www.sciencedirect.com/science/article/pii/S1618866724004242
https://arxiv.org/abs/2605.13570
https://marian42.de/article/infinite-wfc/
https://docs.godotengine.org/en/4.6/classes/class_tileset.html
https://docs.godotengine.org/en/4.6/classes/class_tilesetatlassource.html
https://docs.godotengine.org/en/4.6/tutorials/navigation/navigation_introduction_2d.html
https://github.com/fahadTheTechIdea/GameArtsSkills
https://github.com/fahadTheTechIdea/agentic-sdlc-framework
