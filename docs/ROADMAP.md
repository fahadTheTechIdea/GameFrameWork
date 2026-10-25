# World generator roadmap

Current status: 2026-10-25. Work directly on `main`. The [master plan](ADVANCED_WORLD_GENERATION_MASTER_PLAN.md) owns requirements; this is the active tracker. Actual commands and evidence are in [SDLC_VERIFICATION.md](SDLC_VERIFICATION.md).

## Catalog packs: current delivery and remaining work

**Implemented:** shared current contracts, separate Modern assembly, explicit capability flags, registered vocabulary, static/dynamic providers and affected CLI/current-fixture replacement. Other production packs and explicit composition remain open. [Research and design](SETTLEMENT_IDENTITY_AND_BUILDING_RULES.md#settlement-catalog-packs--research-and-replacement-design) classifies the existing inventory as Modern and proposes independently registered Historical, Fantasy, Space and custom catalogs. Each can own fixed C# policies/algorithms and data. Explicit flags enable Modern's current services; shared engine code dispatches contracts rather than setting-specific switches. Era, culture, environment and gameplay are independent context profiles.

- C1 implemented and locally verified: replaced contracts/vocabulary, extracted Modern 197/585, updated consumers and regenerated all three repository fixtures.
- C2 partially implemented: compiled provider/registry, flags, dependency validation, deterministic registration and shared reporting. Explicit composition and data-file manifests remain open.
- C3: implement scoped Historical, original Fantasy and Space packs with their own meaningful inventories, size variants, surroundings, dependencies and fixed code where needed.
- C4 current CLI/API/export/fixture/removal changes implemented: old catalog APIs, implicit fallback and geometry-only schema paths are removed. Future multi-catalog composition and setup integration remain open.
- C5 local current-contract builds, full engine/CLI harness and independent dynamic DLL checks pass on SDK 10.0.401. Pinned SDK 10.0.402, remote CI, actual GameArts visuals and Godot/export/AOT targets remain unverified.

Do not maintain compatibility adapters or numbered replacement APIs. Hashes preserve input identity, not multiple runtime contract versions. Current runtime/source evidence is in [curved roads and city generation](evidence/2026-10-25-curved-roads-modern-city.txt). The Modern catalog log records the earlier extraction snapshot. Earlier research evidence remains historical.

## Current engine behavior

Current world creation: **Modern city — generated through the capture pipeline**. The [city brief](../.gameframework/worlds/modern-city/city-design.json), [capture](../.gameframework/worlds/modern-city/mockup-capture.json) and [ordinary engine JSON request](../.gameframework/worlds/modern-city/world-request.json) define a 43-building demonstration with stadium, heavy factory, warehouse, school, offices, housing and greens. The actual C# engine generates 32 road paths, 44 source-anchored park trees and 174 accepted resource allocations, preserving the captured diagonals/curves. Authored ground calibration and 49 at-grade junction records resolve all 51 crossings. The [original concept](../.gamearts/outputs/mockups/generated/modern_city_mockup.png) remains generated/unapproved. Review the generated geometry against the source before production art; no saved project-wide art profile, production bundle approval or Godot runtime is claimed.

- The current `MockupWorldDesign`, explicit catalog/capabilities and complete contextual siting are mandatory for generation. Terrain masks and authored height gradients replace analytic desert noise; roads follow named mockup paths and profiles.
- `SettlementPlanner` derives frontage parcels and envelopes from visual regions. `BuildingProgramPlanner`/`BuildingSizePlanner` resolve physical dimensions and service capacity from the existing extensible catalog and settlement context. Buildings retain their dimensions rather than filling every envelope or shrinking to fit.
- `BuildingSitePlanner` derives entrance walks and contextual service yards, courts, parking, gardens and green buffers. Site-zone maximum depth prevents a small house's garden from automatically consuming an oversized parcel.
- Public props follow explicit path patterns and offsets. Green zones and species-specific planting use existing geometry/vegetation gates. Physical conflicts and raster alias conflicts are rejected, with an explicit shared entrance/forecourt connection exception for the owning building.
- Source region, mockup revision/style, grammar, precise position, building footprint, selected size, height/capacity and site/planting geometry survive into the exported world data.
- `generate-world` CLI and `IWorldGenerator` use the same engine. Unsupported CLI commands and the distance-only public-realm API refuse prototype placement. `dummy-village` remains a separate simulation fixture.
- GameArts bundle/file gates, coordinated crossing modules, production dependency manifests and `verify-world` auditing remain available. The new raster design is not automatically a GameArts-approved production package.

## Active slice: contextual mockup-driven algorithms

| Requirement | Implementation/evidence | Status |
| --- | --- | --- |
| No vacant-cell or arbitrary terrain/warehouse fallback | Authored masks/paths, typed region grammars and missing-design diagnostics | PASS locally |
| Physically appropriate building scales | 197 catalog families / 585 variants in 15 categories; custom-type support and capacity-based selection | PASS locally |
| Different stadium scales for town/city demand | Local, town, regional and major-city variants; demand planner selects variant before counts | PASS locally |
| Multiple factory/warehouse sizes and suitable access | Size families, role restrictions and minimum carriageway width | PASS locally |
| Contextual surroundings | Required/bounded site bands and connected entrance walks | PASS locally |
| Intentional green distribution | Separate parks and building-owned buffers; explicit species dimensions and reservation checks | PASS locally |
| Stable response to mockup edits | Region/frontage edits move dependent buildings without changing unrelated positions/IDs | PASS locally |
| Malformed/unsupported/conflicting designs and cancellation | Typed diagnostics, bounded pattern counts and cancellation propagation | PASS locally for recorded cases |
| CLI/schema roundtrip and rejected-output preservation | Shared JSON options, real child-process generation/audit tests | PASS locally |

Local evidence uses Windows/.NET SDK 10.0.401 and synthetic designs/art authorities. Pinned 10.0.402 is unavailable; remote CI and Godot runtime remain unverified. CI runs the expanded harness and explicit synthetic `generate-world` input.

## Catalog expansion

The existing catalog now covers housing, commerce, hospitality, offices, education/research, health, civic/safety, worship/culture, sport, transport, industrial, agricultural, utility and public-space families. `list-buildings <catalog-id> [category]` provides a machine-readable inventory; capacity units, placement domains, site purposes and demand policy are inspectable. Specialized facilities are explicit program choices rather than automatic additions to every town.

Local checks validate every definition and variant, size/site-plan all 555 frontage variants, and generate 14 representative facility worlds through the actual engine. Ten linear/water families now use dedicated infrastructure rules; their 30 variants are covered by complete generated worlds, with maximum-span and rejected-connection cases. They remain deliberately rejected by the frontage resolver. Catalog profiles are not artwork, universal dimensions or detailed internal-facility generators. Full category counts and remaining domain limits are in [the building design record](SETTLEMENT_IDENTITY_AND_BUILDING_RULES.md).

## Infrastructure placement

The existing pipeline now plans road/rail/pedestrian bridges, road tunnels, dams, canal locks, breakwaters, quays, ferry landings and jetties. Capacity/context, maximum span, actual width, full approach profiles and shoreline/bank conditions determine feasibility. Safety/service reservations exclude incompatible buildings, planting and roads. The world retains underlying water/rock and typed passage metadata. Shared generation logging/auditing reports rejection or successful export. See [infrastructure contracts and limits](SETTLEMENT_IDENTITY_AND_BUILDING_RULES.md#bridge-tunnel-and-waterfront-design) and [current evidence](SDLC_VERIFICATION.md).

## Completed earlier slice: production integrity

Wrong-asset native bundles, malformed dependency records, revoked/incomplete manifests and modified asset bytes are rejected. `verify-world` returns JSON, appropriate exit status and correlated log/audit/notification events. These regressions continue to run with the new engine suite; earlier commands/source hashes are retained as historical evidence.

## Next delivery gates

### Implemented slice: structure siting and district compatibility

The [structure-specific siting research](SETTLEMENT_IDENTITY_AND_BUILDING_RULES.md#structure-specific-siting-research-2026-10-25) records 26 primary-source entries, 45 proposed functional profiles and a classification audit for all 197 catalog IDs (92 explicit family refinements). It distinguishes cooling technology, industrial/logistics land use, source-water protection, safe service access, airports, agriculture, public-space distribution and sensitive neighbors. Conditional nuclear technology now refines `power-station`; full nuclear campuses and landfill cells still need dedicated supporting assets and process profiles.

Status: **C# baseline implemented and locally verified**. Existing catalog/design/planner contracts now enforce authored district compatibility, technology-dependent supplies, typed connected access, real provider dependencies, shared resource/link capacity, campus/site reservations, explicit separation policies and exclusion/green interfaces. World JSON retains rule evidence and allocations; missing catalog/siting context is rejected. All 197 profiles and 216 technology resolutions validate; 34 complete contextual worlds and negative/CLI checks pass with the full regression harness. Numeric regulatory thresholds, detailed correctional/cultural/mining/biological profiles, demand allocation and local source qualification remain open. This is functional plausibility for the selected game profile; physical simulation remains separately scoped.

Current runtime evidence is in [siting execution evidence](evidence/2026-10-25-siting-algorithms.txt) and [verification](SDLC_VERIFICATION.md). Earlier [research evidence](evidence/2026-10-25-structure-siting-research.txt) remains the documentation-only historical snapshot. SDK 10.0.402, remote CI, actual GameArts artwork and Godot runtime acceptance remain unverified.

### Existing engineering and runtime gates

1. Run clean pinned-SDK 10.0.402 builds and inspect Windows/Linux CI.
2. Extend rectangular north/south frontage to other authored orientations and shapes; road compilation now supports diagonal/curved polylines and richer district/road-composition grammars; validate representative multi-use mockups.
3. Expand configurable building/site catalogs from real game requirements. Current examples are neither exhaustive nor legal engineering standards. Add richer compound footprints, loading/parking/circulation behavior and regional/biome profiles without bypassing artwork composition.
4. Verify approved GameArts mockup fidelity and real asset scale/animation/lifecycle integration. No synthetic authority supplies real approval.
5. Compile actual Godot TileSets/TileMapLayers, collisions/navigation, component scenes and animations; run headless/in-engine and visual acceptance.
6. Add advanced hydrology, climate/irrigation/ecology, streaming, incremental rebuilds and performance qualification as required by the game. These are not proven by simple terrain gradients or zoned planting.
7. Run live Claude/Codex wizard/install evaluations independently of C# checks.

## Operational limits

The park now has actual compiled planting/paving/sport surfaces and six shared 1m surface-clearance rules. Original observed contours are preserved alongside authored setback masks, with fixed tree anchors and protected road/sport/building boundaries. Surface materials persist in cells, placement envelopes no longer repaint the park, and configured gaps receive paving. The current rule/default is a game-specific visual choice; other packs and regions configure their own spacing. Subcell raster and artwork/runtime limits still apply.

Generation and auditing reuse existing log/audit/notification services. Sink retention, redaction, channel outages and multiprocess coordination remain separate work. No UI or hosted service is introduced. Portable integrity audits do not establish complete geometry, lifecycle, decoded-pixel or symlink containment acceptance. No new runtime dependencies were added. The current capture slice updates both world-generator skill copies; live agent behavior remains unverified.


## Mockup source capture and image/atlas workflow (2026-10-25)

The owner rejected the abstract schematic. It is superseded by a capture overlay on the original source PNG, not treated as a rejected GameArts asset. The original concept's lifecycle remains pending review. The [integration contract](GAMEARTS_INTEGRATION_CONTRACT.md) now owns the researched source-pixel capture, road topology, review/calibration, existing-engine handoff, Codex prompts and canonical atlas workflow.

Implemented: strict shared capture JSON/schema; source hash/header/path verification; road/feature geometry and explicit junction auditing; source-image SVG/interactive review; blocked crop/prompt packets; calibrated geometry draft and `ApplyToDesign` with retained capture lineage and stale-geometry rejection. Current city capture has 875 annotations and a separate 99-feature compilation scope, including 18 original roads and 14 authored accesses. Authored ground calibration and 49 explicit at-grade junction records resolve all 51 road-pair crossings. Full visual inventory remains partial. Canonical two-frame synthetic atlas builder/validator evidence and C# import tests cover measured packing only.

Implemented road/city extension: continuous graph checks, bevel joins, diagonal four-connected raster traversal, dense curve samples, explicit shared utility corridors and source-to-engine CLI generation. The current Modern city passes contextual gates with 43 buildings, 32 roads, 44 source-anchored park trees and 174 accepted resource allocations. Remaining: full visual inventory/lane details, approved art profile, artwork production/visual approval, nonrectangular/oriented building frontage, layered road navigation, Godot import/runtime/fidelity and live Codex/Claude skill evaluations. The current templates do not grant approval or claim autonomous vision accuracy. See [verification](SDLC_VERIFICATION.md) for execution status.


Detailed extraction revision: 875 source/design annotations, including 432 tree/palm positions and 48 landscape surface/traces; eight region inspections explicitly retain missing fine detail. The shared anchored vegetation pattern preserves exact source positions. All 432 plants have an engine-planned draft; 44 park trees are integrated into the current generated city at those positions, replacing the prototype grids. Full city-wide vegetation/building/ground reconciliation and component separation remain open. See the current integration contract and detail-extraction verification evidence.

Ground-contour continuation: refined southern garden/fork boundaries and added 59 source-reviewed interior/exterior constraints to the shared capture contract. Export rejects a contour contradicting those samples. Current builds/full regression, source/city CLI exports and browser sample review pass; exact edge masks and wider scene coverage remain open.
