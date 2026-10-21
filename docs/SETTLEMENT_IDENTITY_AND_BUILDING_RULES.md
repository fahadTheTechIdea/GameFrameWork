# Settlement Identity, Functional Building Programs and World-Wide Rules
Date: 2026-10-21
Status: researched architecture and acceptance requirements, not implemented.

## Core requirement
World-specific rules apply to **everything**, not only vegetation: terrain, water, climate, settlement hierarchy, land use, buildings, architecture, street infrastructure, parks, gardens, industry, public services, utilities, signs, lighting, navigation, animation, growth and constraints. No random placement or arbitrary building-type selection.

## Sources / lessons
- UN-Habitat urban-rural continuum defines cities, towns/semi-dense and rural areas as differing settlement classes, not merely one density scalar: https://unhabitat.org/applying-the-degree-of-urbanisation-a-methodological-manual-to-define-cities-towns-and-rural-areas
- Cities Skylines II zone classes include low/medium/high density homes, mixed use, offices, commercial and industry; interdependence between citizens, jobs, shops and goods: https://www.paradoxinteractive.com/games/cities-skylines-ii/features/zones-signature-buildings
- Manor Lords building program connects flexible residential plots, backyard production extensions, water access, market provisions, warehouses, granaries, churches and upgrading/progression: https://wiki.hoodedhorse.com/Manor_Lords/Burgage_plot/en and https://wiki.hoodedhorse.com/Manor_Lords/Buildings/en
- UN-Habitat urban-rural linkages emphasizes economic, social and environmental ties between settlements: https://unhabitat.org/implementing-the-new-urban-agenda-by-strengthening-urban-rural-linkages

Do not claim access to proprietary game algorithms; these are design lessons based on published feature descriptions.

## Independent profile axes
SettlementClass: isolated facility / hamlet / village / small town / town / medium city / metropolis; project-configurable, not based on hardcoded universal population cutoffs.
SettlementFunction: agricultural, residential, commercial, administrative, university, tourism, port, mining, industrial, logistics, capital, mixed.
DensityAndForm: compact, low-rise, mid-rise, high-rise, dispersed, courtyard, row-house, campus, industrial park; can vary by district.
TimeAndCulture: historical period, local building traditions, materials, street furniture styles, sign conventions and regulatory profile.
Environment: biome, weather, hazard, water, terrain, resources, geography.
EconomyAndServices: population, jobs, service catchment, production demand, trade flows, utilities and transport connectivity.
GrowthStage: founding, expansion, mature, decline, redevelopment.
Rules are resolved hierarchically world -> region -> settlement -> district -> block -> parcel -> object with explicit hard/soft priority and provenance.

## Derived building categories and placement constraints
Residential: house, farmhouse, villa, townhouse, row house, courtyard home, low-rise and mid-rise apartments, high-rise, mixed use; choose from density, demand, infrastructure, setbacks, height limits, neighborhood form and style.
Retail/commercial: kiosk, market stall, grocery, neighborhood retail, supermarket, bazaar, restaurant, office, mall; scale with catchment and commercial accessibility, not arbitrary minimum counts.
Civic/community: primary/secondary schools, clinic, hospital, library, park, civic building, religious/community facility subject to configured culture and user-selected world rules; do not presume presence.
Employment: farms, workshops, small manufacturing, factories, warehouses, offices, industrial processing, port terminals; require workforce, supply-chain and infrastructure feasibility.
Utilities: power, water, waste, sewage, drainage, telecom, emergency access; derived from service capacities, network connections and environmental constraints.
Transport: roads, alleys, walks, bus terminals, logistics loading, parking, ports, stations; induced by movement needs and settlement role.
Decorative/context: street furniture, signs, lights, garden plants, walls, fences, gates, parking bays derived from actual parent structures/streets/plots and no-overlap constraints.

## Example profile outcomes (not compulsory templates)
- Agricultural village: houses/farms/courtyard gardens, local stores, paths and roads to fields and towns, modest services or regional service access, water supply, crop-related outbuildings.
- Small town: neighborhood retail, mixed housing, schools, local market, clinic where justified, civic center, parks, local workshops, collector roads and service connections.
- Metropolitan district: apartment and mixed-use frontages, higher-capacity employment/commercial and utilities, arterial/transit networks, public services, plazas and tightly constrained planting.
- Desert industrial town: worker housing, material yards, industrial production/warehouses, safety buffers, utility corridors, water/energy sources, emergency connections, irrigation-limited landscaping.
- Coastal tourism town: lodging, retail, access to shoreline, flood setback, promenade where suitable, service/employment buildings, salinity-adapted landscaping.
- University town: education facilities, nearby student housing, pedestrian routes, transit and local retail.

## Generation stages
1. Parse scenario, scope, simulation depth, population/employment/government profile, climate and architecture.
2. Compute expected activities and needs using configurable demand/service model; unresolved needs become diagnostics.
3. Choose settlement hierarchy and regional dependency graph: inter-settlement transport, logistics and service catchments.
4. Generate district land-use budgets and compatible zoning/geometry under terrain and hazards.
5. Generate road/pedestrian/utilities graphs and service catchments.
6. Create building program listing type, count/capacity rationale, dependency graph, required adjacency, role and allowed land use BEFORE plotting buildings.
7. Subdivide parcels, place functions/footprints against road access, setbacks, clearance, grade and utility feasibility.
8. Derive façades/styles/materials, gardens, walls/gates, streetlights, signs, parks and details from parent locations and local profiles.
9. Compile asset choices against GameArts approved masters, atlas metadata, stateful components and Godot graph/tile render layers.
10. Validate building demand coverage, access, capacity, network constraints, spatial integrity, biome and aesthetics; report shortages. Do not silently insert nonsense buildings.

## C# contracts to build
IWorldRuleResolver, ISettlementArchetype, ISettlementNeedsModel, IBuildingProgramPlanner, IBuildingCatalog, IBuildingPlacementPlanner, IRegionalDependencyPlanner, IRuleValidationService.
SettlementSpecification = classification, function, population/jobs/capacity goals, environment, time/culture, density, technology/era, phase and custom district mixes.
BuildingDefinition = semantic type, supported profiles, min/max size/height, parcel/road/utility requirements, capacity and production/consumption, connectivity ports, visual style, animation hierarchy.
BuildingPlan = semantic definition ID, service need rationale, capacity, dependency IDs, suitable candidate areas, assigned parcel/geometry, unmet constraints, placement provenance.
Use explicit per-profile data files and rule inheritance/composition with versioned schemas and validation; no single giant class or enum can define all future worlds.

## Acceptance tests
- City, town, village using same terrain/seed produce different coherent functional building programs and networks.
- Every building explains why included; no unrelated office tower in agricultural hamlet absent explicit scenario override.
- Every built parcel has valid frontage and intended access, and each public service meets its configured coverage/capacity rules.
- Two worlds with similar population but different purposes (university vs industrial) require different facilities and distribution.
- No utilities/roads omitted when buildings require them; shortages are reported instead of bypassed.
- Changing local land use/regulation recalculates affected building program and dependent objects while preserving stable IDs elsewhere.
- Building catalog and placement remain agnostic to Godot and GameArts artwork generation.
