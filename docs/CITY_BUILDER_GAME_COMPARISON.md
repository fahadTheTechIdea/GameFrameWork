# City-building game comparative research — 2026-10-21

This is an evidence-led requirements study, not proof of proprietary internal implementations. All conclusions are engineering inferences unless expressly documented by developers.

## Games and transferable mechanics

| Game | Documented mechanics | Required GameFrameWork subsystem |
|---|---|---|
| Cities: Skylines II | Flexible curved/parallel/grid street tools, roundabouts, grading, road upgrades; zoning adjacent to roads; district services; traffic path costs for time, comfort, money and behavior | Street graph + cross-section assembler, route graph, lots/zoning, contextual street furniture, vehicle path-cost profiles |
| Manor Lords | Flexible road-adjacent burgage plots; sufficiently large backyards unlock gardens/orchards and workshops | Road frontage-aware variable parcels, building footprints, yards, parcel extension system |
| Foundation | Gridless modular buildings, organic town growth | Organic graph planner and composable footprint/architecture rules |
| Workers & Resources: Soviet Republic | Explicit building road, footpath, power, water and industrial connectors; connectivity affects reachability | Typed building ports and distinct connectivity graphs, validation of service access |
| Townscaper | Geometry-neighbor-sensitive generation, contextually shaped architecture | Corner/adjacency rules and replaceable procedural visual detail components |
| Tiny Glade | Gridless construction with procedural details | Building detail grammars, local topology triggers and deterministic edit regeneration |
| Timberborn | Height/stacking and water/levee/floodgate interactions | Height-aware structure foundation and explicit dynamic hydraulic component contracts; high fidelity 3D fluids excluded from phase 1 |
| Farthest Frontier | Grid building placement, terrain slope/flattening, building tiers, decorative services | Foundation suitability, footprint rotation and development-state progression |

## Mandatory urban production stages

1. Research/gather configured urban style, climate, street-design standard, locale and simulation depth. Never hardcode one locale's road widths/traffic signs.
2. Terrain, watershed, coast, slopes and restricted zones.
3. Demand centers, districts, densities, major public facilities and anchors.
4. Street topology and intersection graph (hierarchical arterial/collector/local service); curves, slopes, dead ends, loops, crossings, roundabouts.
5. Geometric road corridor assembly: lanes, medians, curbs, curb ramps, sidewalks, access buffers, roadside parking, drainage, markings. Profiles vary by street class and neighborhood. Driveway breaks and corner joins must be measured.
6. Derive blocks from corridors and split into parcels with valid accessible frontage; accommodate variable polygon footprints, setbacks, corner plots and courtyards.
7. Place buildings using parcel and entrance rules; preserve road/path/utility ports; derive fenced yards, gardens, landscaping and backyard extensions.
8. Place contextual public objects: traffic and street-name signs, signal assemblies, lights, benches, bus stops, fire hydrants, power poles, drains, trash bins, trees, planters and other street furniture from constraints, line-of-sight and route rules; prevent crowding a clear sidewalk.
9. Compile roads, sidewalks, accessible ramps, vehicle lane connectors, footpaths and service connectors as separate navigation networks with routing costs. Treat intersections and closing gates as dynamic state changes.
10. Generate static atlas/TileMapLayer resources, dynamic Godot scenes, spatial collision, lighting, occlusion, navigation and gameplay events; test in Godot before claiming ready.

## Key modeling contracts

StreetProfile: lane count/width; design speed; one-way; bike/transit/parking lanes; median; curb geometry; sidewalk pedestrian clear zone/furnishing zone/buffer; trees, lights, sign slots and spacing; crossings, drainage and terrain grade limits.
Intersection: incoming edges, turn restrictions, crossing phase timing, corner geometry, turn radii, curbs, sidewalk corner connectivity, crosswalk anchors and signal locations.
Parcel: polygon boundary, road frontage, setback, buildable polygon, permitted uses, building access ports, yards, gardens, driveway and tree zones.
BuildingDefinition: modular footprint/height, entry ports, collision masks, physical environment needs, animation component tree and asset references.
RoadGraph & PedestrianGraph & UtilityGraph: typed independently; connected with explicit adapters and transitions; support agent-specific routing.
DetailRule: trigger (feature or neighborhood topology) + geometry constraints + art slot + collision/light/nav behavior + deterministic placement stream.
WorldEdit: detect influence-region dependencies and replan only affected blocks, intersections, roadside objects and resource chunks.

## Scenarios to acceptance-test
- Curved road into T-intersection generates visually consistent curb corner, sidewalks and accessible crosswalk, no overlap or blocked exits.
- Widening one road updates adjacent lots, curbside plantings, signs and road markings without rebuilding distant neighborhoods.
- Houses have valid street/footpath access; backyard plot sizes govern gardens and workshops.
- Road lane network stays routable through valid junction movements; traffic signs point in the correct direction; light spacing obeys configured profile.
- A gate opens/closes with synchronized animation, collision and traversability transitions and legal route re-evaluation.
- Desert climate uses climate-appropriate landscaping; no unintentional tree/grass biome bleed.
- End-to-end Godot TileSet/TileMapLayer exports are only marked playable after measured art assets, actual collisions/nav and engine runtime verification.

## Public references
- Cities: Skylines II road tools: https://www.paradoxinteractive.com/games/cities-skylines-ii/features/road-tools
- Cities: Skylines II zones: https://www.paradoxinteractive.com/games/cities-skylines-ii/features/zones-signature-buildings
- Cities: Skylines II traffic AI: https://colossalorder.fi/news/development-diary-2-traffic-ai/
- Cities: Skylines II road details/parks: https://www.paradoxinteractive.com/games/cities-skylines-ii/news/detailers-patch-2
- Manor Lords burgage plots: https://wiki.hoodedhorse.com/Manor_Lords/Burgage_plot/en
- Foundation: https://wiki.polymorph.games/foundation/Foundation
- Workers & Resources connection graph: https://wiki.hoodedhorse.com/Workers_Resources_Soviet_Republic/Questions_and_answers
- Townscaper: https://www.gamedeveloper.com/game-platforms/how-townscaper-works-a-story-four-games-in-the-making
- Tiny Glade: https://store.steampowered.com/app/2198150/Tiny_Glade/
- Timberborn update 6: https://timberborn.wiki.gg/wiki/Update_6
- Farthest Frontier building guide: https://www.farthestfrontier.com/guide/gameplay/buildings/

## Implementation approach
Do not expand existing simplistic cross-shaped-road demo into a universal city generator. First introduce continuous world-space geometry, typed graph/parcel/corridor models and deterministic step orchestrator, then implement validated road profiles + intersections + frontage + parcels. Keep graphics and Godot exporter as downstream projections, with asset approval handled by GameArtsSkills. Introduce no new Git branches: update main.
