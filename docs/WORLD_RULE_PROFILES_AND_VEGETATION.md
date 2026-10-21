# World-Specific Rule Profiles and Vegetation Communities
Date: 2026-10-21
Status: researched design specification; implementation not yet complete.

## Policy
Do not use one placement algorithm or density constant for every city/town/biome. Never scatter vegetation or objects randomly. A seeded sampler may propose candidate points only inside habitat/land-use-feasible regions, with mandatory environmental viability, spacing, access, purpose, and provenance checks.

## Independent profile dimensions
WorldProfile = SettlementMorphology (dense city, medium city, town, village, rural, industrial, none) + Biome (desert, temperate forest, tropical forest, coastline, mountain, etc.) + Climate + LandUse/District + TimePeriod + CulturalDesign + Maintenance/Irrigation + Season + GameplayOverrides.
Hierarchy: global constraints -> world -> region/biome -> city/settlement -> district/land-use -> parcel/park/street corridor -> object/species. Merge via explicit precedence; hard constraints cannot be overridden by aesthetic preferences. Mixed worlds may contain multiple settlements and regions with distinct profiles.

## Observed research, not universal density thresholds
- Urban forest studies indicate land use strongly affects canopy. Parks generally contain larger contiguous canopies than residential lots, while higher-density neighborhoods tend to have less cover and smaller growing space. Local variations remain substantial.
- Street-tree standards require utilities, planting strips, root space, sight lines, pedestrian clear zone, street type, vehicle clearance and mature canopy sizing to be considered.
- In arid lands, microtopography, soil moisture and drainage influence shrub/grass patches, not simple uniform scattering.
- Forest vegetation is stratified: canopy affects understory sunlight and therefore shrubs, saplings, herbs and grass.
- Game-specific lessons: Cities: Skylines II supports landscape vegetation lifecycle and district-defined park roles; Manor Lords ties orchards/backyard gardens to road-fronting property geometry.

## Settlement subprofiles
DenseCityCore: street tree pits / planters / plazas / roof gardens / formal parks. No arbitrary grass around dense buildings. Sidewalk clearances and utilities constrain trees.
MediumCityResidential: street planting zones + district green pockets + individual parcel front/back gardens subject to lot access.
Suburban: lawns, hedges, gardens, road-aligned shade trees, parks. Planting area derived from lot footprints and impervious surfaces.
SmallTown: civic greens, gardens, orchards, retained woodland, irregular legacy trees and more permeable edge parcels; avoid applying urban tree grids everywhere.
VillageRural: fields, hedgerows, orchards, riparian strips, native unmanaged plants. Managed fields follow plots and farm practices.
Industrial: landscape screening/erosion control near boundaries, avoid operational clearances, safety and access zones.
Each profile has no universal canopy percentage; configure policy targets and derive feasible cover from buildable land and species mature crown projection.

## Biome community models
Desert: xerophytic shrub/grass communities from soil, microsites, drainage, seasonal rainfall, salinity and wind; oases only with supported hydrology and/or irrigation.
TemperateForest: tree species stands, age cohorts, competing crown/root extents, canopy gaps, understory shade tolerance, groundcover.
TropicalIsland: coastal exposure, salinity and soil separate beach, dune, mangrove (where appropriate), inland and wetland plants.
Mountain: altitude zones, aspect, slope stability, timberline, rock cover and moisture.
ManagedUrban: purposeful planting palettes by street, park, parcel, irrigation and maintenance budgets; trees, shrubs and lawn managed separately from wild vegetation.

## Domain concepts
IWorldRuleProfile; IRuleResolver; SettlementProfile; BiomeProfile; DistrictProfile; VegetationCommunity; PlantSpeciesDefinition; HabitatSuitability; PlantingZone; VegetationStand; CanopyLayer; ShrubLayer; GroundCoverLayer; GrowthStage; MaintenancePolicy; IrrigationNetwork; PlacementDecision.
Plant species carry mature radius/height, rooting zone, water/light/salinity temperature tolerances, slope limits, seasonality, fire behavior when needed, ecological competition parameters, visuals, animation and collision/nav footprint.

## Staged vegetation planner
1. Build terrain climate soil hydro and sunlight maps and pre-existing plant communities.
2. Resolve world/settlement/district/parcel profile and applicable vegetation community.
3. Delineate habitat polygons and managed planting zones; exclude impervious roads, building footprints, utility conflicts, inaccessible plots and steep/unsafe terrain.
4. Derive desired cover/functional objectives and species palette compatible with profile and environment.
5. Evaluate viability for each candidate; no valid candidate means diagnostic, not forced placement.
6. Place canopy-forming trees first; enforce mature crown/root spacing, building distance, infrastructure and light requirements.
7. Compute resulting canopy shade/competition; place shrubs and saplings using understorey requirements, hedgerow alignments or planting design.
8. Place ground cover (grass, herbs, wildflowers, leaf litter) according to shade, soil, moisture, pedestrian wear, parcel use and irrigation; do not sprinkle individual decorative grass on pavement.
9. Compute access/nav/collision/occlusion, shadows, wind swaying, growth/seasonal states, fire/rain interactions, atlas/asset references and stable provenance.
10. Validate regional density targets against feasible space, explain unmet targets and maintain chunk-safe deterministic regeneration.

## Acceptance examples
- Two settlements on same desert map: dense city uses irrigated formal landscape in legal planted zones; rural village uses native xeric patches and courtyard shade, without irrigated lawns absent water.
- In dense block, sidewalk clear walkway cannot be occupied by a mature tree trunk/root or planter; lamp and utility clearance respected.
- Residential corner parcel garden and hedge respect driveway, pedestrian entrances, property and visibility limits.
- Forest clearing has more sun-loving groundcover than closed canopy; shade-loving shrubs possible under trees.
- Desert wash has species composition/cover consistent with drainage, not the same distribution as adjacent dune crest.
- No object without a location rationale, habitat/profile match and placement diagnostics.
- Approved GameArts v7.5 graphic representations and Godot engine test are distinct downstream validation gates.

## Reference research
- https://www.sciencedirect.com/science/article/pii/S1618866724004242 (2025 urban canopy/land use 10 cities)
- https://www.sciencedirect.com/science/article/pii/S1618866712001227 (zoning and canopy)
- https://streetsillustrated.seattle.gov/design-standards/street-trees/ (street clearance)
- https://link.springer.com/article/10.1007/s00371-020-01920-7 (AutoBiomes multi-object sampling)
- https://www.sciencedirect.com/science/article/pii/S1470160X25001293 (arid microtopography)
- https://jms.imde.ac.cn/article/doi/10.1007/s11629-019-5538-0 (Kuwait wadis)
- https://www.paradoxinteractive.com/games/cities-skylines-ii/features/city-services-districts-policies (city parks/districts/trees)
- https://wiki.hoodedhorse.com/Manor_Lords/Buildings/en (plot-related buildings and roads)
