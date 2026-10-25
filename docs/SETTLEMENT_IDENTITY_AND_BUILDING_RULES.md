# Settlement Identity, Functional Building Programs and World-Wide Rules
Date: 2026-10-21
Status: partial C# implementation with local synthetic regression evidence; advanced demand/economy, arbitrary geometry and real-art/runtime acceptance remain open.

## Settlement catalog packs — research and replacement design

Date: 2026-10-25. Status: **shared contracts and Modern extraction implemented; other production setting packs remain planned**. This section owns the catalog design; the master plan and roadmap track delivery. Owner constraints: classify the current structures/families as **Modern**; support other game settings through modular, contract-based, plug-and-play catalogs; each pack may contain its own fixed C# algorithms. Modern enables its electricity, potable-water networks, motor-road networks, district rules and technology options through explicit engine capability flags. Other packs select their own capabilities and rules. Replace/update the active implementation in place: no legacy modes, compatibility shims, parallel contract versions or retained obsolete catalog APIs.

### Research conclusions

1. **A catalog setting is more than an art skin.** UNESCO's accounts of [Provins](https://whc.unesco.org/en/list/873) connect trade fairs to storage, merchant buildings and defenses; [Yazd](https://whc.unesco.org/en/list/1544) connects qanats, districts, courtyards and climate adaptation. The design inference is that a pack must own functional dependencies, morphology, size/capacity and surroundings as well as its family inventory. An old-looking model with Modern utilities is insufficient.
2. **Historical requires period and cultural context.** [Moenjodaro](https://whc.unesco.org/en/list/138) has planned streets and drainage; [Jaipur](https://whc.unesco.org/en/list/1605) has deliberately planned commercial streets/squares; [Lijiang](https://whc.unesco.org/en/list/811/) relates streets, buildings and bridges to water and terrain. These examples rule out a universal historical template of winding European medieval streets, absent sanitation or identical houses. Specific claims need selected place/period sources.
3. **Space requires different service and connection concepts.** [NASA ECLSS](https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/) describes atmosphere, oxygen, ventilation, water recovery and waste systems. A [NASA lunar-habitat thermal reference design](https://ntrs.nasa.gov/api/citations/20210026557/downloads/IEEE%202022%20Paper%20SH%20TCS%20Architecture%20and%20Technical%20Challenges%20Update.pdf) considers radiators and surviving darkness. Game synthesis: sealed habitats, airlocks, pressure domains and heat rejection cannot be expressed as houses beside ordinary roads. These are reference concepts, not a universal space-colony specification or numerical defaults.
4. **Modules may contain data and code.** [Godot resources](https://docs.godotengine.org/en/stable/tutorials/scripting/resources.html) separate stored data from scene behavior, while [Epic's modular gameplay features](https://dev.epicgames.com/documentation/unreal-engine/game-features-and-modular-gameplay-in-unreal-engine) support standalone plugin features and opt-in components. [OpenRA traits](https://docs.openra.net/en/release/traits/) demonstrate capability composition, explicit prerequisites and conditional behavior. The recommendation is common engine contracts with pack-owned code/data, not a universal scripting language or a hardcoded setting switch in Core.
5. **.NET plugin identity must be shared correctly.** [Microsoft's plugin tutorial](https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support) uses shared interfaces, `AssemblyLoadContext` and `AssemblyDependencyResolver`; loading a second copy of the interface assembly breaks type identity. Dependency isolation is not a sandbox. Trusted compiled packs can implement rules; ordinary content packs need not execute code. Static registration remains necessary where a target cannot dynamically load assemblies.
6. **Fiction needs explicit game rules, not invented historical authority.** The [Wesnoth terrain definitions](https://github.com/wesnoth/wesnoth/blob/master/data/core/terrain.cfg) illustrate named terrain/settlement data and gameplay properties in a fantasy game. Mana, wards, portals, supernatural factions and invented cultures are authored game mechanics in our proposed packs. They are not facts validated by UNESCO/NASA, and no third-party game assets or lore are imported.

All sources accessed online 2026-10-25. Observations above are distinct from the proposed architecture and pack examples below. Source pages are examples/reference designs, not standards for every game.

### Catalog types and independent dimensions

The required `SettlementCatalogSelection.CatalogId` is a validated ID backed by the registered descriptor, **not a closed setting enum requiring a Core edit for each setting**. A plugin registers its ID and display name. `modern` displays **Modern** and owns the existing **197 families / 585 variants / 15 categories**, including its 10 infrastructure families. Historic structures still used today can remain in Modern; their presence does not establish a historically accurate era profile. Other catalogs define their own inventory rather than inheriting all Modern entries.

Separate setting/catalog identity from period, culture/faction, technology, environment, settlement scale/function, visual style and gameplay depth. A fantasy city can be underground; a historical town can be desert or coastal; a future city can be Earth-like or pressurized. Environment and culture are selectable profiles, not mutually exclusive catalog-type enum values. Labels such as dark fantasy, cyberpunk and solarpunk may be curated packs/modifiers rather than mandatory new engine types.

| Catalog setting | Candidate curated packs / families (proposed, not implemented) | Pack-owned rules and capabilities |
| --- | --- | --- |
| Modern | Current inventory: houses, offices, schools, stadiums, factories, logistics, public services, plants and infrastructure | Current size/site/siting policies, electricity and water networks, motor roads, district compatibility and explicit cooling technology |
| Historical: ancient | Place/period-selected courtyard housing, markets, workshops, granaries, civic/ritual buildings, wells/cisterns, defenses, baths/drainage where evidenced | Local water/materials, pedestrian/cart/boat access, craft production, storage and civic organization; no automatic electricity requirement; Moenjodaro is one researched example, not a universal model |
| Historical: medieval | Trade town, agrarian village, fortified settlement; dwellings, merchant premises, halls, mills, storage, gates and selected community/ritual buildings | Fair/market access, farm/trade dependencies, water/wind power, gate approaches, fire/service space; Provins supports a particular trade-town example |
| Historical: early modern | Selected regional commercial capitals, port/canal towns, civic courts, markets, warehouses and gardens | Period/culture-specific street and precinct grammars; Jaipur, Yazd and Lijiang demonstrate different authored contexts rather than a shared generic style |
| Historical: industrial | Mill/rail/mining towns, workshops, depots, worker housing and civic services | Steam/water/electrical technology selected by period, freight and employment relationships. [Crespi d'Adda](https://whc.unesco.org/en/list/730/) is a specific factory-village reference |
| Fantasy: grounded | Original village/market/fortress packs; dwellings, inns, crafts, granaries, guilds, shrines, walls and watch posts | Authored economy/culture, animal/cart access, fire/water needs and defense relationships; magic absent unless enabled |
| Fantasy: magical | Original arcane city, forest community, subterranean stronghold or floating settlement; sanctums, mana conduits, ward towers, portals and creature facilities | Pack-specific mana supply, ritual/ward zones, portal endpoints, species scale and environmental needs; fixed C# algorithms are allowed |
| Science fiction: near future | Automated town/research campus, robotics, advanced transit and energy | Declared automation/communication/energy systems; no mandatory pressure or oxygen infrastructure on a breathable world |
| Science fiction: planetary outpost | Habitat modules, airlocks, life support, greenhouses, radiators, power/storage, landing/service areas, extraction | Pressure domains, breathable services, water recovery, heat rejection, dust/exhaust exclusions and rover access according to selected environment |
| Science fiction: orbital | Connected habitat/lab/storage/docking modules and service trunks | Docking/interface compatibility, connected pressure/utility domains, module orientations and declared gravity assumptions; unsupported 3D compilation must fail explicitly |
| Post-apocalyptic | Survivor camps, repaired districts, salvage yards, shelters, workshops, farms and contaminated ruins | Salvage/material availability, water treatment, repaired/off-grid utilities, hazard/exposure and reuse state; an authored genre synthesis |
| Alternate technology | Steam, clockwork, diesel, magical-industrial or other original technology packs | Their own resource/production/transport rules; visual machinery does not silently enable Modern services |
| Custom | Any registered creator-defined setting | Implements the same contracts; no new engine setting enum or Modern inheritance required |

Underwater/floating settlements are environment profiles usable by multiple settings. [NOAA's historical Aquarius description](https://sanctuaries.noaa.gov/missions/2010aquarius/meet_aquarius.html) distinguishes air/power support and an ambient-pressure habitat: do not assume every underwater habitat uses the same pressure/airlock model. Full 3D, orbital dynamics and pressure simulation remain separate capabilities, not automatic consequences of selecting a catalog.

The table is a research-backed scope map, not an exhaustive inventory or a promise that a small starter pack covers every civilization, species, building or game. Pack completeness is measured against its explicit scenario and supported capabilities.

### Current contract and module boundaries

`GameFrameWork.Core` owns the shared contracts/geometry/planning. `GameFrameWork.Catalogs.Modern` is a separate assembly containing all 197 families, dimensions/site profiles, Modern vocabulary, fixed siting/demand policies and bridge/tunnel/water infrastructure strategy. Core and Worlds have no project reference or setting-code dependency on Modern. The CLI and test composition roots register it explicitly. Historical/Fantasy/Space production modules remain planned; the independent Arcane test DLL proves the extension contract, not a completed Fantasy pack.

| Boundary | Current responsibility |
| --- | --- |
| `SettlementCatalogDescriptor` | Catalog ID/name, declared capability flags, dependencies and registered vocabulary; resolved provenance includes content digest and implementation identities |
| `ISettlementCatalogProvider` | Supply the catalog's current structure definitions, vocabulary, profiles and policy registrations; same contract for built-in, statically linked and dynamically loaded packs |
| `ISettlementCatalogRegistry` | Explicit registration/discovery, dependency validation, unique IDs, deterministic resolution and immutable catalog snapshots for generation |
| `SettlementCatalogSelection` | Required catalog ID and explicitly enabled capabilities on `MockupWorldDesign.Catalog`; no implicit Modern default or fallback |
| Existing `BuildingDefinition` and related records | One shared structure contract with registered catalog-local function/category/technology/resource/district string IDs, dimensions/capacity, requirements, surroundings and visual provenance. Catalog + local ID identifies ownership; external packs can use namespaced IDs |
| `ISettlementDemandPolicy` | Pack-owned needs/count/capacity decisions replacing universal Modern population/function switches |
| `ISettlementCatalogPolicy` | Pack-owned deterministic feasibility/dependency/neighbor rules with inspectable results; may contain fixed C# algorithms rather than declarative expressions |
| `IInfrastructureDesignStrategy` | Pack-owned infrastructure planning; shared frontage/size/site geometry remains reusable. Generalized placement and site-use vocabulary extension remains future work |
| Existing validation, geometry, resource ledger and pipeline | Shared finite-input/reference checks, containment/collision/connectivity, capacity accounting, orchestration, cancellation and provenance; dispatches registered policies without catalog-name conditionals |
| Existing GameArts registry/gates and Godot adapter | Resolve approved visuals and compile supported output; no duplicate approval workflow or rendering engine inside catalog modules |

Not every class needs an interface. These boundaries are justified by substituting packs/providers/policies. Pack-specific helpers and fixed rule tables remain internal concrete code. The shared pipeline still owns validation order and cannot be bypassed by a plugin returning an arbitrary world.

The setting-specific function/category, district, technology and resource enums are replaced by registered catalog-local string IDs validated against `CatalogVocabulary`. The Modern constants live in its own assembly. Unknown vocabulary fails validation. Closed geometry/result enums remain shared; generalized site-use and placement vocabulary expansion is a later slice.


### Modern capability flags and pack-owned code

The explicit catalog selection is the engine's setting flag. Its capability set activates the corresponding pack policy/network implementations. Current `MockupWorldDesign.Catalog` value (property names match the shared JSON serializer):

```json
{
  "CatalogId": "modern",
  "EnabledCapabilities": [
    "modern.electricity-network",
    "modern.potable-water-network",
    "modern.wastewater-network",
    "modern.motor-road-network",
    "modern.district-policy",
    "modern.facility-technology"
  ]
}
```

Modern registers its current enums' replacement vocabulary, fixed technology/family refinements, demand calculations and siting rules under this contract. Historical can register fixed craft/watermill/cart-access rules; Fantasy can register fixed mana/wards/portal rules; Space can register fixed atmosphere/pressure/docking/thermal rules. **Pack-owned hardcoding is allowed; Core-owned setting branching is not the extension mechanism.** Compiled providers supply data and fixed implementations through the contracts. A standalone data-file manifest loader is not implemented in this slice. No mandatory universal rule DSL is introduced.

Capabilities do not mean every structure consumes every service. Requirements activate from the selected family, technology and context. A disabled capability cannot silently satisfy a structure that still requires it: report the missing capability/dependency. Electricity or water can be deliberately reused by another pack through explicit imports; resources and ordinary streets are not conceptually exclusive to Modern. Modern *network/policy implementations* are explicitly scoped, while basic walkways, water needs and shared geometry remain reusable.

Pack code must declare required facts, supported geometry/environment and stable algorithm IDs. It accepts cancellation, reads a generation snapshot, returns planned decisions/evaluations with rule/source provenance and does not mutate global registries during generation. It must not perform arbitrary placement, fetch runtime facts silently, bypass art approval or hide missing dependencies. The approved mockup/compositional grammar remains the visual authority in every setting.

### Composition, loading and current-contract replacement

1. Authorize/discover modules at the host boundary, validate manifests/content paths and register capabilities/policies before generation. Build an immutable, deterministically ordered registry. Duplicate IDs, absent dependencies, dependency cycles, unavailable algorithms or mismatched units/ports fail before placement. Missing packs never resolve to Modern.
2. Bind the current mockup design explicitly to one catalog; all its settlements use that selection. Heterogeneous catalogs within one world remain future composition work. Mixing families within one settlement requires an explicit composition catalog with declared imports and conflict resolution; no wildcard merging, load-order wins or automatic Modern inheritance. Shared vocabularies are imported once; pack-local IDs are namespaced. Intentionally replacing a family updates its owned definition rather than retaining aliases/obsolete copies.
3. The registry snapshots validated provider content; trusted compiled packs use the common interface assembly and can have private dependencies. One shared contract assembly is resolved from the host; do not load private duplicate copies. Dynamic assembly loading is a host capability, not a guarantee on every Godot/export/AOT target. Static registration must exercise the same provider/policy contracts. Dependency isolation does not make untrusted code safe.
4. Maintain **one current contract and one active definition per ID**. Do not introduce `CatalogV1`/`CatalogV2`, version-range negotiation, legacy adapters, deprecated APIs, schema fallbacks or old-ID aliases. Content hashes identify exact inputs/provenance, not parallel compatibility branches. An incompatible module or artifact fails with a precise diagnostic; rebuild/update the module or regenerate the artifact against the current contract.
5. The obsolete public example catalog API and geometry-only schema branch have been removed. The single current mockup schema requires catalog selection and contextual siting. CLI/API/tests and repository fixtures use the new contract; old artifacts must be regenerated. Fixed infrastructure/demand/siting code belongs to the pack; no old-ID aliases or alternate supported schema remain.
6. Shared `WorldOperationReporter` owns catalog discovery/validation/selection/generation success, failure and cancellation. Retain catalog IDs, content digests, enabled capabilities, algorithm IDs and evaluated-rule lineage in exported output/audit records. No parallel log/audit/notification stack.

The no-compatibility directive concerns active application contracts/content and supersedes the earlier additive/legacy design **for this replacement**. Git history and actual past test evidence remain historical records, not supported runtime versions. External GameArts approval identity and existing SDK pins are not silently rewritten under this catalog task.


### Host and CLI usage

Register packs at the composition root: `new SettlementCatalogRegistry([new ModernCatalog()])`, then inject that registry into `DesertWorldGenerator`, `SettlementPlanner` or `SettlementDesignPipeline`. `registry.Resolve(ModernCatalog.Selection)` returns the registered definitions, policy/demand/infrastructure implementations and provenance. Authoring may deliberately customize the structure snapshots in a design; the selected pack still validates vocabulary, required capabilities and rules. No second structure model is introduced.

`list-catalogs` lists registered descriptors. `list-buildings modern [category]` selects the Modern inventory explicitly. `generate-world <request.json> [output-dir] --catalog-plugin <trusted-assembly.dll>` loads an explicitly selected compiled provider; the request selects its catalog ID and flags. The same option is available for catalog discovery. Built-in registration and dynamically discovered providers use the same interfaces. CLI success/failure/cancellation and selected catalog/algorithm/content identities reuse the shared operation reporter.

`SettlementCatalogPlugin` uses the host Core assembly for contract type identity and an `AssemblyDependencyResolver` for private dependencies. Pack descriptors and structure/policy requirements are validated; duplicate IDs, missing/cyclic dependencies, unknown vocabulary/capabilities and missing required flags fail. Registry content is copied into read-only/frozen snapshots. Exported catalog digests cover descriptor/definitions and entry-point implementation assemblies, not an entire transitive deployment closure. Trusted code is not sandboxed. Dynamic loading is locally tested on Windows .NET; Godot/export/AOT targets require separate qualification.

### Delivery and measurable acceptance

Accountable owner: repository owner; responsible executor: implementation agent; review in this research pass: author self-review. No new approval gate is introduced. The current authorized slice implements C1, compiled provider registration/loading and affected CLI/removal checks. Composition/data manifests and other production packs remain later slices. Current evidence is in [verification](SDLC_VERIFICATION.md) and [execution evidence](evidence/2026-10-25-modern-catalog-contracts.txt).

| Slice | Required result / acceptance |
| --- | --- |
| C1 — shared contracts and Modern extraction | The 197/585/15 inventory and its setting-specific demand/siting/infrastructure behavior reside in Modern behind shared contracts; general geometry/planning stays shared. Explicit Modern selection/flags activate those services. Remove obsolete catalog entry points, vocabulary switches in Core and compatibility branches; update all callers/fixtures together. Existing intended regression coverage passes on the new contract |
| C2 — module discovery and composition | Register a provider absent from the host's compiled catalog list without editing Core. Exercise data-only, trusted dynamic and static registration through the same contracts. Reject unknown/duplicate IDs, bad dependency graphs, conflicting imports, malformed manifests, unsafe paths, contract mismatch and missing algorithms; cancellation and failure audit are observable |
| C3 — genuinely different packs | Deliver a specifically scoped Historical, original Fantasy and Space starter inventory with multiple size variants, meaningful surroundings, supplies and fixed pack-specific code as needed. Each has an accepted/rejected complete synthetic world using the actual shared pipeline; no mandatory Modern electricity/road/district assumptions leak into them. Do not claim comprehensive historical/fictional coverage from starter packs |
| C4 — integration and removal checks | CLI lists/selects catalogs and capability flags; exported lineage identifies exact pack inputs and algorithms. Test cross-settlement catalogs, explicit composition, required capability disabled, conflicting resource units, dependent provider capacity/cycles and unsupported geometry. No implicit fallback, old schema mode, old public catalog API or retained compatibility version remains |
| C5 — independent delivery gates | Build current contracts/modules/CLI/tests and run all regressions; separately qualify supported Godot targets and actual GameArts art/style/gameplay. Existing unavailable pinned SDK/Godot/remote CI gates remain visible until run |

Examples to require: historical mill fails without its declared water/wind/resource context; ordinary historical house does not require electricity; fantasy portal fails without a valid endpoint/power rule; pressurized Space habitat fails without its selected atmosphere/heat-rejection interfaces, while a breathable-world future settlement does not inherit those requirements. Modern refinery/warehouse/stadium scaling and district/utility rules must retain their current regression intent. A third-party pack adding a new resource/technology and fixed evaluator must not modify Core.

Research gaps: complete era/culture-specific inventories and measurements; fictional lore/design choices; algorithm applicability to curved, vertical, modular and 3D layouts; production plugin packaging and target restrictions. The current 2D orthogonal desert compiler cannot be relabeled as an orbital or underground implementation. Further source work accompanies each selected pack; no catalog is labelled complete from this research table alone.

## Current engine implementation (2026-10-25)

`BuildingDefinition` is the shared catalog contract. It now carries category/function, capacity units, placement domain, automatic-demand policy, size variants, per-resident scale demand, allowed settlement roles, minimum road width, entrance width and surrounding-site grammars. `BuildingProgramPlanner` chooses capacity-appropriate variants before calculating counts; a city sports requirement does not become many tiny stadiums. `BuildingSizePlanner` keeps each variant's width/depth/height and capacity intact while fitting the existing envelope. `SettlementRegionRule` can provide an explicit size/capacity scenario or a `SettlementDemand` context for automatic selection.

The catalog includes **197 families and 585 size variants**. The original 12 types remain compatible; 185 specialized families add independent dimensions, capacity metrics and context-specific site programs. Custom IDs, dimensions, capacities and site rules require no new engine enum or generator class. All dimensions/capacities remain configurable game-design profiles, not legal engineering standards or verified art dimensions.

| Category | Families | Variants | Representative facilities |
| --- | ---: | ---: | --- |
| Residential | 15 | 43 | Detached houses, villas, duplexes, courtyard homes, towers, dormitories, worker/senior/student housing |
| Retail | 15 | 43 | Convenience/grocery stores, supermarkets, hypermarkets, malls, hardware/furniture stores, dealerships, kiosks |
| Food/hospitality | 12 | 36 | Cafes, restaurants, bakeries, food courts, hotels, motels, resorts, hostels, catering kitchens |
| Office/finance | 9 | 27 | Offices/towers/campuses, banks, post offices, co-working, insurance, trade centers, exchanges |
| Education/research | 13 | 38 | Kindergartens, primary/secondary/vocational schools, colleges, universities, laboratories, libraries, observatories |
| Healthcare | 11 | 32 | Clinics, pharmacies, hospitals, emergency/maternity/rehabilitation facilities, nursing homes, veterinary clinics |
| Civic/safety | 11 | 32 | Town halls, courts, government offices, embassies, police/fire/ambulance facilities, prisons, community centers |
| Culture/worship | 14 | 42 | Mosques, churches, temples, synagogues, cathedrals, museums, theatres, cinemas, galleries, convention/exhibition centers |
| Sport/recreation | 11 | 34 | Football/multipurpose stadiums, arenas, halls, gyms, pools, tennis/basketball facilities, training grounds, tracks, skateparks |
| Transport/logistics | 16 | 48 | Bus stops/stations/depots, railway stations, freight/truck facilities, distribution centers, parking, fuel/charging, airports/hangars, ports |
| Industry/extraction | 22 | 66 | Workshops, factory/assembly/processing families, refineries, steel/cement plants, oil/gas facilities, mines/quarries, cold/grain storage, container depots |
| Agriculture | 12 | 36 | Farmhouses, barns, livestock/poultry sheds, stables, greenhouses, silos, feed mills, dairies, aquaculture, packing/equipment sheds |
| Utilities/communications | 16 | 48 | Power/substations, solar/wind sites, water/wastewater/desalination, pumps/towers, recycling/waste facilities, data/telecom centers, cell towers |
| Public-space structures | 10 | 30 | Pavilions, toilets, playgrounds, botanical gardens, squares, cemeteries, funeral/visitor facilities, watchtowers, guardhouses |
| Linear/water infrastructure | 10 | 30 | Road/rail/pedestrian bridges, tunnels, dams, locks, breakwaters, quays, ferry landings and jetties |

`ModernCatalog` is the explicit Modern provider. Its expanded inventory is in [ModernExpandedStructures.cs](../src/GameFrameWork.Catalogs.Modern/ModernExpandedStructures.cs), behind the shared provider/registry contract. `list-buildings <catalog-id> [category]` exposes the current JSON inventory with resolved catalog provenance, with family/variant counts and complete definitions. The new specialized entries set `AutomaticDemand=false`: they are available to explicit composition rules, without inventing population-only demand for every airport, refinery or cathedral. Projects can deliberately configure their demand policies.

**Current placement support:** 187 frontage families / 555 variants are validated through the size/site planners. Ten linear/water families / 30 variants use `ModernInfrastructureDesignStrategy` and `InfrastructureRules`; they continue to reject the frontage resolver with `BUILDING_PLACEMENT_KIND_UNSUPPORTED`. Campus, plant and farm profiles represent aggregate site envelopes; detailed internal buildings, equipment, panels/turbines, functional interiors and animations still need their dedicated composition and approved artwork.

`BuildingSitePlanner` computes entrance access and context-specific surroundings from the parcel and selected footprint. Site rules specify front/rear/side zones, minimum and maximum depth, inset, purpose and optional species/planting rhythm. Examples include domestic gardens, industrial service yards/buffers and venue entrance plazas/parking/green belts. A width mismatch or missing space for a required site use returns a diagnostic. Road corridors remain mockup-authored; undersized carriageways are rejected rather than widened into neighboring composition regions.

The world pipeline keeps building, road, access, service and green zones distinct, validates their physical/raster conflicts and uses the existing vegetation planner and species clearance gate. Planting requires an explicit species identity and dimensions. Park zones and building-owned buffers remain separately traceable; green space is never a default filler for unused land. Climate, irrigation, growth and complex transport simulation remain separate profile work.

Actual checks and remaining gates: [SDLC verification](SDLC_VERIFICATION.md). The researched requirements below continue to define broader scope; they are not all implemented by this slice.

## Bridge, tunnel and waterfront design

`InfrastructureDesignRule` binds an existing catalog definition to a named mockup region, straight horizontal/vertical alignment path, obstacle terrain region, settlement ID/context, purpose and connection paths. It supports explicit capacity/variant selection and minimum surrounding clearance. The planner computes the footprint and safety/service reservations; no position search or vacant-cell fallback exists.

- Scale follows the catalog's editable per-resident demand, explicit local requirement, width, allowed settlement context and supported span. The smallest sufficient variant wins. Infrastructure variant width and height stay intact; `DepthMeters` is the maximum supported alignment length, while `LengthMeters` is calculated from the authored endpoints. This is bounded infrastructure sizing, distinct from unscaled building depth. Longer spans or demand beyond a variant's capacity are rejected. Road bridge/tunnel variants require progressively wider approaches.
- Road, railway and pedestrian bridges cross their named water mask and reach dry full-width banks. Road tunnels cross named rock and reach dry portals. Both approaches must meet exact endpoints from outside the structure, continue its alignment, use the correct transport mode and match the complete cross section. The alignment and approaches use existing `Paths`/`RoadRules`; no new parallel road graph is introduced.
- Dams span water between dry banks with road maintenance connections. An explicitly profiled alignment can carry a road on the dam. Without that profile, the design must connect its service roads elsewhere in the existing network.
- Canal locks connect waterway paths at both ends and require a separate dry maintenance path ending at the footprint edge. Waterway approaches are checked across terrain-boundary intervals, so a land interruption cannot pass just because endpoints match. The vessel channel remains water, with passage metadata separate from land movement.
- Breakwaters, ferry landings and jetties begin at a dry shore connection and extend into their named water. Quays run along a shoreline with water on one long edge and dry service land on the other; their access path ends on the dry footprint edge. Quays, ferry landings and jetties have a planned pedestrian deck. Maintenance-only breakwaters remain solid in the land raster.
- Engine-derived abutment, portal, maintenance and edge reservations scale with the chosen width and clearance. Buildings, sites, parks, vegetation, public props, unrelated profiled paths and other infrastructure may not occupy those reservations. Connected approach paths may meet a structure at their intended interface; they cannot intrude into its body.

`WorldDocument.Infrastructure` retains size/capacity, source geometry, purpose, connections, passage mode and reservations. `InfrastructureLayers` preserves every underlying terrain cell and the actual passage mode at that cell. Only the matching validated crossing alignment may override its named water/rock for travel; surrounding obstacles remain blocked, and a second unnamed obstacle is rejected. Rail/pedestrian/waterway modes are semantic metadata for runtime navigation; the simple 2D land raster is not a complete multimodal simulator.

The synthetic [infrastructure request](../tests/fixtures/synthetic-infrastructure-world.json) is an executable schema example, not approved artwork. Current geometry is orthogonal with convex containment; curved spans, stacked independent crossings, structural load analysis, deck elevations/grades, vessel clearance, hydraulic flows, gate simulation and detailed 3D geometry remain outside this game-design placement slice. Custom frontage families remain data-driven; new infrastructure behaviors need an explicit algorithm binding rather than an invented geometry fallback.
## Structure-specific siting research (2026-10-25)

Status: **the functional game-profile baseline is implemented in the C# engine**, with local synthetic regression evidence described below. All 197 families expose siting profiles; technology-dependent families require an explicit selection. `power-station` now supports three nuclear cooling specializations with separate normal and shutdown heat dependencies. This does not add a new catalog family or establish a physically qualified nuclear campus. `SettlementRole.Mixed` does not waive district compatibility.

### Implemented siting contract

`MockupWorldDesign` uses one current schema with required `Catalog` selection/capabilities and a complete `WorldSitingPlan`. Districts, typed sources, capacity-bearing links, neighbor policies and exclusions remain explicit. Every settlement/infrastructure grammar supplies district, selected technology and dependency bindings. Missing context and other schemas reject generation; no geometry-only success or fallback catalog exists. See the [current synthetic request](../tests/fixtures/synthetic-siting-world.json).

`ModernSitingProfiles` supplies the Modern refinements through `ISettlementCatalogPolicy`, using the shared `BuildingDefinition` contract. Its baseline and conditional refinements reference the S01–S45 research groups and R-source register below. District compatibility and technology are resolved before parcel/site planning. `StructureSitingValidator` rejects incomplete or conflicting inputs; `StructureSitingPlanner` evaluates actual selected footprints, capacities and surroundings before vegetation or export. Custom profiles retain their authored rules. No district search, arbitrary relocation or vacant-cell fallback is introduced.

- Districts own disjoint convex regions. Each structure and its surrounding site uses must fit its compatible district. Clean/light factory, heavy processing, general/agricultural/hazardous storage and ordinary/hazardous laboratories have distinct compatibility rules.
- Dependencies check source kind, origin, units, quality/reliability and usable typed routes. Links declare the commodities they carry. Roads are checked along the whole selected route for width; connections must join source ports to the structure's arrival point or explicit service interface. Resource-area dependencies contain the full footprint. Surface-water sources require effective authored water terrain; decorative proximity grants no supply.
- The shared allocation ledger prevents consumers from exceeding source or link capacity. A provider must be a real generated structure, expose the requested output, locate its port on its footprint and stay within its aggregate output capacity. Self-supply and provider cycles fail. Named parent facilities are actual providers, not free-form labels.
- Once-through, recirculating and atmospheric nuclear options resolve different cooling origins and supplies. Normal and shutdown heat rejection are separate; intake/outfall points must differ. Protected intakes reject declared upstream contamination/discharge. PV, solar thermal, air/evaporative data centers and marine/freshwater/recirculating aquaculture likewise resolve their own dependencies.
- Existing building site grammars retain entrances, walks, loading/service yards, parking and planting. Conditional technologies add cooling/utility plant, security or biosecurity reservations. Utility and shore-interface corridors reserve actual polygons against unrelated buildings, plants and public props. Waterway links remain on effective water; `SiteInterface` explicitly represents a shore transfer.
- Pairwise neighbor policies use polygon-edge distances symmetrically. Heavy/freight/nuclear versus sensitive uses, airport versus wildlife/obstacles, and dark-sky versus lighting conflicts require an explicit applicable policy. No universal buffer is invented. Exclusions screen structure tags, public greenery and relevant public props/lighting.
- `WorldDocument.Siting` preserves evaluation status, rule/source references, measured neighbor relations, dependency quantities, consumed links and reservations. Rejected designs retain diagnostics through the shared CLI log/audit/notification boundary and cannot replace a prior successful export.

Default dependency demand is `1 + 0.1 × selected variant service capacity` in **game-service-units**; provider output defaults to one such unit per capacity unit. Both are editable profile data, not water flows, electrical ratings or calibrated throughput. Conditional 3/4/5-meter site bands are illustrative game geometry. Source qualities, reliability and upstream links are authored facts; the engine does not infer their physical truth from artwork.

Current evidence: **197 profile validations / 216 technology resolutions; 22 building, 10 infrastructure and 2 mixed/provider contextual worlds**, plus negative, JSON, cancellation and actual CLI/audit/export-preservation checks. The complete existing regression harness also passes. [Verification and limits](SDLC_VERIFICATION.md), [execution evidence](evidence/2026-10-25-siting-algorithms.txt).

The research matrix below remains a broader design reference. Preference ranking, population-based service allocation, safe crossings, timed emergency coverage, wind/dispersion, groundwater/hydraulic simulation, airport operations, internal campus equipment and jurisdiction-specific engineering qualification are not established by these typed graph and geometry checks. Existing mockup/GameArts approval and Godot runtime gates remain separate.

This research addresses the owner's request for plausible structure location and surroundings. Functional siting and district compatibility belong in that game profile; detailed physical simulation and jurisdiction-specific regulatory compliance remain separate. The sources below establish planning considerations, while the proposed rule IDs and their translation into game constraints are our design synthesis. No source establishes a universal world layout or a mandatory separation distance for every country, facility size and technology.

### Findings that change the engine design

1. **Resolve the actual technology/use before choosing a site.** A steam-cycle power station with once-through cooling needs a suitable supply/intake/outfall arrangement. Recirculating cooling has different make-up-water and land requirements. The IAEA also describes atmospheric ultimate heat sinks and some passive designs relying on air for decay-heat removal. Decay-heat removal and normal turbine-condenser cooling are distinct requirements. Therefore a nuclear site must have a complete, technology-compatible cooling/heat-rejection plan; a generic `near river` flag is insufficient, and a river/sea is not mandatory for every reactor design. [R01–R03](#siting-source-register)
2. **Separate incompatible uses, rather than segregating every business.** Heavy processing, major warehousing and freight depots generally call for industrial/logistics land with appropriate access and buffers. Small low-impact workshops, neighborhood shops and some urban delivery facilities can have a compatible mixed-use form. Classify emissions, noise, operating hours, cargo and traffic intensity before deciding. [R04–R05](#siting-source-register)
3. **Distance is only one relationship.** A nearby road may be unsuitable for trucks; a nearby river may be saline, seasonal, polluted or inaccessible. A school across a motorway may lack a safe walking route. Requirements must consider usable connections, capacity, exposure and physical barriers. [R04, R06–R07, R10, R16](#siting-source-register)
4. **Some relationships are directional or network-based.** Wastewater discharge and drinking-water abstraction depend on the water network; prevailing wind affects exposure; fire stations serve reachable demand over roads. Map north/south and straight-line radius are inadequate substitutes. [R10–R11, R23](#siting-source-register)
5. **Green space has several roles.** Neighborhood access, recreation, habitat continuity, riparian protection, shade and industrial screening require different zones and maintenance conditions. A planted strip cannot make an incompatible industrial use acceptable by itself. Airport wildlife compatibility may conflict with ponds or bird-attracting planting. [R05, R08, R17, R26](#siting-source-register)
6. **A large facility is a campus with dependencies.** Its footprint includes ancillary plant, circulation, intake/outfall or utility corridors, storage, operating clearances, security and access. These must be reserved before nearby parcels and landscaping are composed. [R01, R04, R13, R18, R20](#siting-source-register)

### Rule semantics and scope

The proposed siting profile composes rules across world → region/biome → settlement → district → parcel → structure/technology. It extends the existing catalog and mockup design rather than maintaining a second building list.

| Rule behavior | Meaning in the selected game profile | Example |
| --- | --- | --- |
| Required dependency | Fail if the named compatible source/connection and capacity are absent | Cooling supply, grid connection, vessel access, an emergency entrance |
| Exclusion/incompatibility | Reject overlapping forbidden land use or an enabled exposure/hazard zone | A school inside a heavy-industry exclusion overlay |
| Conditional rule | Activate from subtype, size, technology, operating hours or local policy | Cooling-water use; night freight; hazardous laboratory containment |
| Preference | Rank only already feasible, mockup-authorized regions | Shops near walkable customer demand; a stadium near high-capacity transit |
| Coverage objective | Allocate facilities against reachable demand and their capacity | School places, emergency response, neighborhood park access |
| Unknown evidence | A missing required fact produces an unresolved diagnostic, not a pass | Unknown cooling technology or an unspecified protected-water source |

Each rule needs an ID/revision, applicable structures and conditions, scope, severity, rationale, source reference, parameters with units and parameter origin, required environmental layers, dependency type, evaluation result and exception policy. Thresholds supplied by a game designer are labelled game assumptions; local regulatory parameters need a jurisdiction/version source. Do not invent globally valid meter buffers, water flows or response-time limits.

### Proposed structure rule matrix

The entries below are **engine proposals inferred from the cited sources**, not quotations or automatically applicable legal requirements. Baseline profiles cover every existing family; the coverage table later identifies specific additional profiles. Some profiles need further subtype research before numerical parameters can be set.

| ID | Structures / context | Proposed siting, surroundings and connection rules | Basis |
| --- | --- | --- | --- |
| S01 | All structures | Require purpose, compatible district, feasible footprint and utility/access dependencies; protect authored habitat/heritage/hazard overlays. Keep enabled hard constraints separate from visual preferences. | R05, R22; engine synthesis |
| S02 | Heavy factory, refinery, steel, cement, chemical and oil/gas processing | Industrial/process district; freight-capable access; operating and hazard reservations; assess noise, emissions and neighboring sensitive uses. Water, fuel and process dependencies vary by plant technology. | R04–R05, R24 |
| S03 | Light factory, workshop, assembly, vehicle maintenance | Distinguish clean/light use from noisy, dusty or hazardous operations. Permit compatible employment/mixed use only when its actual impacts, service yard and delivery route fit. | R04–R05 |
| S04 | Warehouse, cold/grain storage, distribution and container facilities | Freight/logistics/agricultural location according to cargo and throughput; usable truck routes, loading/queuing/turning space and customer/supply connections. Cold storage adds power/refrigeration requirements; it does not inherently require waterfront frontage. | R04; cold-chain dependency is design inference |
| S05 | Food processing, dairies, catering and packing | Connect raw-material and market flows, potable/process water, drainage, waste and deliveries. Separate clean product/customer circulation from service/waste routes; evaluate neighboring contamination sources. | R05, R10; workflow synthesis |
| S06 | Mine, quarry, well and resource processing | Bind extraction to an authored resource deposit; reserve access, operational areas and affected land. A processing plant can use a supply link instead of sitting directly on the deposit. Detailed geology/extraction profiles remain to be researched. | R05; explicit resource dependency is engine synthesis |
| S07 | Thermal `power-station`, waste-to-energy | Resolve fuel, generation and cooling technology; require compatible supply, heat rejection and grid export. Reserve cooling/plant/service areas and evaluate community exposures. Do not treat all generation as steam-cycle or water-cooled. | R03, R05, R24 |
| S08 | Nuclear power / reactor subtype (conditional power-station technology) | Resolve normal and shutdown heat-removal dependencies separately; apply cooling-specific water/air requirements, grid/site services, hazards, population/emergency-planning context and protected operational areas. No universal `must touch sea` rule and no invented exclusion radius. | R01–R03 |
| S09 | Solar farm | Solar resource/shading, usable land/topography, grid capacity, maintenance access and habitat/land-use compatibility. Distinguish PV from water-using thermal generation; allow deliberately modelled agrivoltaic/floating variants. | R15 |
| S10 | Wind farm | Wind resource, grid and maintenance access; account for wildlife, surrounding communities and aviation compatibility. Spacing/noise/shadow parameters depend on turbine/site profile. | R08, R14 |
| S11 | Electrical substation / grid assets | Join correct network and capacity; reserve equipment/maintenance access and corridors. Large transmission assets and neighborhood distribution assets use different site profiles. | R24 |
| S12 | Data center | Power capacity/reliability, communications and cooling technology are explicit dependencies. Water demand is conditional on cooling system; reserve cooling equipment, backup plant and service access. | R20; communications dependency is operational synthesis |
| S13 | Telecom exchange / cell tower | Resolve network coverage/backhaul, power, maintenance and structure type. Screen tower height against aviation overlays. A small rooftop installation and a freestanding tower require different envelopes. | R08, R25; coverage model proposed |
| S14 | Drinking-water treatment, intake, pumping, water tower | Resolve surface/ground/imported/reclaimed source and network function. Protect source/capture zones and water quality; connect abstraction, treatment, storage and demand. A water tower needs hydraulic/network context, not compulsory river adjacency. | R10–R11; hydraulic abstraction proposed |
| S15 | Wastewater treatment | Connect sewer catchment, treatment/reuse/discharge and maintenance access. Evaluate odors, exposure, flooding and sensitive neighbors; wind-aware preference needs actual wind data. Do not place discharge relative to intake using map north. | R10–R11 |
| S16 | Desalination | Resolve seawater/brackish source, intake, energy, brine handling and product-water connection. The process can connect through a corridor rather than requiring the whole campus on the water edge. Intake/outfall effects need a future process profile. | R10–R11, R13; process translation proposed |
| S17 | Recycling, waste transfer, waste-to-energy; landfill subtype missing | Distinguish waste stream and processing technology. Reserve collection/heavy-vehicle access and contain exposure/runoff. Landfill-specific groundwater/geology, flood, wetland and airport-wildlife screening must not be copied blindly to an enclosed recycling hall. | R05, R08, R12 |
| S18 | Kindergarten, schools, vocational teaching | Reachable pupil catchment, safe walking/cycling/transit, usable arrival/play space and exposure screening. Avoid solving pollution conflicts by moving schools far from the community without checking access consequences. | R06–R07 |
| S19 | University / research laboratory / science park | Campus/service access and appropriate utilities; distinguish ordinary teaching from hazardous laboratories or industrial pilot plants. Apply the latter's actual hazard profile instead of assuming all research belongs beside housing. | R05, R22, R25; subtype synthesis |
| S20 | Observatory | Professional optical observation prefers dark skies and light controls; a public teaching observatory may be intentionally urban. Radio observatories require a different interference profile, not an optical-light rule. | R21; radio subtype needs further research |
| S21 | Clinics, hospitals, maternity, nursing/rehabilitation, veterinary care | Match reachable demand and service level; protect patient/ambulance access and site resilience. Separate logistics/waste and public circulation. Specialized, residential-care and veterinary subtypes need different dependencies. | R05, R09, R25; catchment synthesis |
| S22 | Fire, ambulance, civil defense, police, emergency shelter | Evaluate service coverage over the usable road network, station exit/vehicle access and continuity under enabled hazards. A geometrically nearby site across a disconnected bridge is insufficient. | R09, R23; policing/shelter adaptation proposed |
| S23 | Town hall, courts, government offices, embassy | Public/service reachability, civic access and purpose-specific security/operating space. An embassy is not interchangeable with a neighborhood public counter. Detailed security profiles need dedicated requirements. | R05, R22; civic/security synthesis |
| S24 | Stadium, arena, convention, exhibition, concert venue | Size the precinct and access to event demand, including pedestrian dispersal, public transport, service/emergency circulation and neighboring uses. A local ground need not inherit major-tournament perimeter requirements. | R18; non-sport event adaptation proposed |
| S25 | Neighborhood sport, pools, gym, play facilities | Reachable residents/users, safe routes and suitable activity/noise/lighting context. Pools add water/energy/service dependencies; avoid imposing stadium-sized roads and parking on a local court. | R17, R26; sport subtype synthesis |
| S26 | Neighborhood shops, markets, pharmacy, cafe, restaurant | Favor walkable demand and compatible mixed-use frontage; separate deliveries/refuse from customer access. Operating intensity and hours can trigger additional neighbor controls. | R04, R22; retail synthesis |
| S27 | Mall, hypermarket, wholesale, dealership, bulky retail | Evaluate customer and delivery networks, loading/queuing and footprint scale. A wholesale freight market differs from a neighborhood shop. Large parking fields are not an automatic universal requirement. | R04, R22; retail subtype synthesis |
| S28 | Hotel, guesthouse, hostel, motel, resort | Resolve purpose: urban business, roadside, waterfront or rural tourism. Connect guests, services and waste appropriately; shore proximity is conditional on the resort concept, not required for every hotel. | R05, R22, R25; tourism adaptation proposed |
| S29 | Farmhouse, barn, equipment shed, greenhouse, grain silo | Relate buildings to agricultural parcels, crop/soil/water/climate needs and farm/market access. Controlled-environment growing can substitute infrastructure for open-field conditions. Do not force a farmhouse into a heavy-industrial district. | R05, R16; agricultural synthesis, crop-specific review pending |
| S30 | Livestock, poultry, dairy, stable, feed mill | Link animal/feed/water/manure operations and working access; evaluate neighbors, drainage and source-water protection. Intensive production and a small stable need different impact profiles. | R05, R10; species-specific review pending |
| S31 | Aquaculture | Match species and production system to water quantity/quality/salinity, land/soil, access and discharge. Pond, marine and recirculating systems differ; an indoor recirculating facility need not sit directly on the sea. | R16 |
| S32 | Airport terminal / aircraft hangar | Depend on an airport campus, airside/apron/runway connections and landside access. Screen surrounding height/noise/wildlife-compatible uses. A terminal alone does not constitute an airport. | R08, R25 |
| S33 | Railway station, bus station/stop | Join the correct passenger network; design safe walking/transfers and reachable catchment. A bus stop need not be inside a transport-only district; it must actually meet its route. | R22, R25; route/catchment synthesis |
| S34 | Freight terminal, truck/bus depot, logistics center | Freight/transport district or a deliberately compatible employment site; resolve cargo/passenger mode, working access, queuing, maintenance and neighbor effects. A bus depot is not a passenger boarding stop. | R04–R05, R25 |
| S35 | Port terminal, marina | Bind to navigable marine/fresh water and shore/berth assets plus landside links. Distinguish cargo handling and secure work areas from passenger/recreation access; avoid claiming any water cell is navigable. | R13 |
| S36 | Road/rail/pedestrian bridge, road tunnel | Bind the carried transport network, obstacle, banks/portals and usable approaches. Future navigable-water/rail clearances and structural constraints need explicit profiles; current orthogonal placement does not prove them. | Existing engine; R04, R13, R25 provide context |
| S37 | Dam, lock, quay, jetty, ferry landing, breakwater | Extend existing geometric placement with hydraulic/navigation/coastal roles and connected catchments. A ferry landing needs a viable water route/destination; an inland dry port must not receive shore-only terminal rules. | R13; hydraulic simulation remains proposed |
| S38 | Park, public square, botanical garden, pavilion, toilet, visitor center | Distribute by reachable users and intended public/habitat function; provide compatible access, shade/planting and maintenance/service dependencies. Preserve ecological links; screen airport conflicts instead of planting ponds indiscriminately. | R08, R17, R26 |
| S39 | Cemetery / funeral home | Separate burial-ground hydrogeology/source-water/flood review from funeral-home reception/service needs. Neither has a universal `always on a hill` or `always outside town` requirement. | R19; funeral-home adaptation proposed |
| S40 | Worship, museum, cultural center, theatre, gallery, memorial | Relate to community, cultural/heritage context, pedestrian access and congregation/event scale. Culture-specific orientation and ceremonies need an explicitly selected profile; do not invent universal rules across religions. | R22; local cultural rules need additional sources |
| S41 | Housing / dormitory / worker / senior / student residence | Compatible neighborhood services, access and exposure; tie student/worker accommodation to usable destinations while keeping it out of operational hazard areas. Match density and capacity to district context. | R05, R22; housing subtype synthesis |
| S42 | Office, finance, post office, co-working, trade center | Reachable employment/customer catchments and compatible center/campus setting; deliveries and site security according to actual operations. A postal sorting/logistics depot needs a different profile from a public post-office counter. | R04, R22; office subtype synthesis |
| S43 | Parking, fuel station, EV charging | Connect to intended destinations/routes and vehicle class. Fuel adds containment/fire/source-water screening; charging adds electrical capacity/dwell context. Both need safe entry/exit and pedestrian continuity. | R05, R10, R25; charging adaptation proposed |
| S44 | Guardhouse, watchtower and campus support structures | Require a purposeful parent boundary/access or surveillance role; keep visibility and circulation clear. Do not scatter standalone security structures without a served asset. | R05; parent/support semantics are engine design |
| S45 | Prison | Reserve institution-specific access, services and security; keep visiting, staff and custody functions distinct. Location must remain unresolved beyond the baseline until a correctional profile is sourced; remoteness is not an assumed universal rule. | Research gap; not a validated correctional rule |

### Proposed district composition

Industrial/heavy process, light employment, freight/logistics, utilities, airport/port campuses, residential, mixed-use centers, civic/education/health, recreation/tourism, agriculture and conservation are compatible-use profiles, not compulsory single-use districts in every settlement. Each district has a boundary, allowed/conditional uses, reserved networks, demand/capacity, interfaces to neighbors and source/design provenance. Compatibility must be evaluated on **both sides**: adding homes beside an existing noisy plant should trigger the same exposure conflict as adding the plant beside homes. [R04–R05, R22](#siting-source-register)

Primary streets connect district roles before parcel subdivision. Freight districts need feasible links to their destinations without forcing heavy movements through sensitive local access; public-facing centers need pedestrian/transit connectivity; emergency facilities need usable response routes. A rural village may combine compatible workshops and homes, whereas major process/logistics facilities may need a distinct service campus serving several settlements. Transport width, turning/loading and crossing capacity are evaluated over the required route, not just at the entrance. Exact traffic simulation is optional. [R04, R23](#siting-source-register)

Green distribution must distinguish public parks, connected habitat/riparian corridors, recreation, productive agriculture and private/industrial planting. Accessibility is measured along allowed routes and crossings. Flood-storage or ecological land is not automatically a safe playground; airport-compatible planting is a separate profile. Xeric and irrigated planting remain distinct, with climate/water demand represented at the selected gameplay level. The engine must not use greenery as a substitute for a missing district or dependency plan. [R08, R10, R17, R26](#siting-source-register)

### Integration into the existing engine — proposed delivery sequence

1. Extend `BuildingDefinition` with referenced functional/siting profiles and technology conditions, not a duplicate catalog. Explicitly model generic `power-station` technology and add a nuclear subtype only with its full supporting-site profile. Existing examples retain their identity and sizes.
2. Extend `MockupWorldDesign` with authored district boundaries, typed resource/hazard/sensitive-use layers and capacity-bearing network/resource connections. Extend `SettlementRegionRule`/`InfrastructureDesignRule` with references to the applicable district/profile. Reuse existing terrain regions, paths, geometry and source provenance.
3. Resolve profiles and facility dependencies before `SettlementPlanner` subdivides regions. Evaluate required rules first; choose among feasible **authored compositional regions** using stable purpose-based criteria. An infeasible approved mockup returns a design-conflict diagnostic for revision; it does not authorize a free-space search, random scatter or silent movement.
4. Extend `BuildingSitePlanner` and infrastructure reservations for dependent campus elements, circulation, utility links and green/public interfaces. Conserve supplied resource/network capacity across facilities; proximity alone cannot create a connection or double-spend capacity.
5. Validate the assembled design and preserve a per-structure rule result containing source IDs, resolved parameters, measured relation, dependencies, pass/fail/unresolved status and reasons. Reuse the existing CLI generation Log/Audit/Notification boundary for outcomes, rather than adding a parallel reporting service.

Geometry measures must match rule purpose: polygon-edge clearance for separation, containment/overlap for district and hazard layers, route distance/time and transport mode for access, network direction for water dependencies, and explicitly authored wind/exposure layers for directional preferences. Missing data for a required enabled rule is unresolved; optional simulation layers do not become mandatory by accident. Overrides are explicit, scoped and recorded, and cannot silently turn failed dependencies into success.

### Proposed acceptance cases

- A conventional once-through cooled plant beside a dry decorative channel fails; a compatible capacity-bearing intake/outfall/heat-rejection plan passes its selected game rules. An explicitly modelled atmospheric-cooled subtype is not rejected solely for lacking river frontage. Unknown cooling technology remains unresolved.
- Heavy factory and major warehouse in residential land fail compatibility. The same types in suitable process/logistics land still fail if their truck route or required utilities are infeasible. A low-impact workshop may pass a compatible mixed-use profile. A farm grain store may use an agricultural profile.
- New housing inside an existing plant's enabled exposure zone fails, just as the reverse placement does. A green strip alone does not waive that failure.
- A nearby school across an uncrossable trunk road fails its walking catchment. A connected safer route may satisfy it; a remote replacement is checked against actual user access.
- A hospital/emergency station with a disconnected or enabled-hazard-blocked access route fails. A large stadium cannot pass using a small ground's crowd/transport assumptions.
- A source-protected intake and a conflicting discharge/contamination pathway fail the relevant water profile. Saltwater is not accepted for a freshwater dependency without a treatment connection. Several consumers cannot each reserve the same remaining water/power capacity.
- An airport-adjacent landfill/pond/planting proposal triggers wildlife compatibility; an observatory with incompatible lighting triggers its selected darkness profile. These are not handled by a generic building-size check.
- A PV field does not inherit steam-cycle cooling, a data center resolves cooling technology, and a recirculating aquaculture site is evaluated against its system rather than compulsory coastal placement.
- Rules, source references, assumptions and unresolved gaps survive JSON serialization. Input ordering does not change results, cancellation remains observable, existing generation fixtures retain their intended profile, and diagnostics/audit records explain rejections.

These were the research acceptance scenarios. The implemented baseline and its measured synthetic coverage are recorded above; the broader physical, behavioral and service-allocation cases remain future work. Source review and classification coverage are separate from runtime evidence and actual GameArts/Godot acceptance.

### Siting source register

Retrieved 2026-10-25. These are primary authored sources. Dates below are document dates where established, not search-engine crawl/publication estimates. Older guidance is used for conceptual design; it is not asserted to be the latest applicable law. Country-specific sources remain explicitly scoped. Where a current index lists updated material, do not substitute a retired PDF or draft for that material.

- **R01 — IAEA, SSG-35 (2015), sections 3–5 and Annexes I–II:** [Site survey and site selection](https://nucleus.iaea.org/sites/committees/Policy%20Documents/Complete%20Collections%20of%20Safety%20Standards/Complete%20collection%20English/SSG-35%20Site%20Survey%20and%20Site%20Selection%20for%20Nuclear%20Installations.pdf). Cooling access, grid/transport, hazards, population and environmental considerations. Some cross-referenced safety guides have since been revised; this research does not reproduce the old reference list as current law.
- **R02 — IAEA, SSG-56, section 4.2–4.16:** [Ultimate heat sink](https://nucleus.iaea.org/sites/nss-oui/Published%20Chunks/m_4192a342-84cb-492e-be7b-d1e37adf7b26/c_e44c1a8e-7147-4cd4-ae82-20f1a3e021e3__2_0.Html). Water/atmospheric heat sinks, reliability and capacity; distinguishes residual heat removal from turbine-condenser systems.
- **R03 — U.S. EIA (2018):** [Dry cooling](https://www.eia.gov/TodayinEnergy/detail.php?id=36773). Cooling configurations have different water, energy and performance characteristics; historical U.S. statistics are not game defaults.
- **R04 — FHWA, FHWA-HOP-12-006 (April 2012), sections 2 and 4:** [Freight and Land Use Handbook, authored report distributed by NOCoE](https://transops.s3.amazonaws.com/uploaded_files/fhwahop12006-FHWA-Freight-and-Land-Use-Handbook.pdf); [FHWA publication announcement](https://ops.fhwa.dot.gov/resources/news/news_detail.asp?ID=816). Freight-compatible land use, community interfaces, truck/rail access and loading. The announcement's old publication-directory link returned a not-found page; the authored PDF was inspected.
- **R05 — IFC/World Bank Group:** [General EHS Guidelines](https://www.ifc.org/en/insights-reports/general-environmental-health-and-safety-guidelines). Site-specific environmental/community evaluation; air, noise, water, hazardous materials, traffic and emergency considerations. The index says updates are in progress; no claim of one universal industrial buffer.
- **R06 — U.S. EPA, School Siting Guidelines (2011):** [Guidelines and environmental review sections](https://www.epa.gov/schools/school-siting-guidelines). Voluntary U.S. guidance for environmental screening and site evaluation.
- **R07 — U.S. EPA:** [Smart growth and school siting](https://www.epa.gov/smartgrowth/smart-growth-and-school-siting). Community access, walking/cycling/transit and surrounding land-use consequences.
- **R08 — FAA, AC 150/5190-4B (2022), particularly land-use compatibility and wildlife sections:** [Airport land-use compatibility](https://www.faa.gov/documentLibrary/media/Advisory_Circular/150_5190_4b_Land_Use_Compatibility.pdf). U.S. guidance on surrounding uses, noise, height/airspace and wildlife attractants; thresholds are not transplanted as global rules.
- **R09 — U.S. Climate Resilience Toolkit:** [Critical facilities](https://toolkit.climate.gov/asset/infrastructure/critical-facilities). Facility resilience and continuity of access to critical public services.
- **R10 — U.S. EPA:** [Common source-water considerations](https://www.epa.gov/sourcewaterprotection/common-considerations) and [protection practices](https://www.epa.gov/sourcewaterprotection/source-water-protection-practices). Quantity/quality, catchment influences and contamination pathways.
- **R11 — IFC/World Bank Group (2007), community health sections:** [Water and Sanitation EHS](https://www.ifc.org/content/dam/ifc/doc/2000/2007-water-and-sanitation-ehs-guidelines-en.pdf). Wastewater exposure, sensitive neighbors, buffers and access control. Conceptual guidance, not a global setback table.
- **R12 — U.S. EPA:** [Municipal solid waste landfills](https://www.epa.gov/landfills/municipal-solid-waste-landfills). Facility-specific location considerations including geology, wetlands, flood areas and airport safety; local applicability requires its own profile.
- **R13 — IFC/World Bank Group (2017):** [Ports, Harbors and Terminals EHS](https://www.ifc.org/content/dam/ifc/doc/mgrt/20170201-final-ehs-guidelines-for-ports-harbors-and-terminals.pdf). Marine/freshwater facilities, environmental and community interfaces. The retired 2007 port PDF was explicitly marked no longer in use and was excluded as the controlling source.
- **R14 — U.S. DOE:** [Wind site selection](https://www.energy.gov/cmei/systems/windexchange/site-selection). Wind resource, power connections and community/wildlife considerations.
- **R15 — U.S. DOE:** [Large-scale solar siting research](https://www.energy.gov/cmei/systems/large-scale-solar-siting-research). Land, interconnection, environmental/community trade-offs and deliberately combined agricultural/floating uses.
- **R16 — FAO, aquaculture site-selection guidance:** [Technical and operational factors](https://www.fao.org/4/x5744e/x5744e02.htm). Species/system, water quality, soil, access and market factors; historical technical guidance with technology-specific applicability.
- **R17 — WHO Europe (2017):** [Urban green spaces: a brief for action](https://www.who.int/europe/publications/i/item/9789289052498). Planned access, design, community needs and monitoring; no universal tree-scatter formula.
- **R18 — FIFA Stadium Guidelines:** [Site selection](https://grassroots.fifa.com/innovation/stadium-guidelines/general-process-guidelines/initiation-and-feasibility/site-selection), [masterplan](https://inside.fifa.com/innovation/stadium-guidelines/general-process-guidelines/initiation-and-feasibility/masterplan) and [precinct/perimeter](https://ipt.fifa.com/innovation/stadium-guidelines/technical-guidelines/stadiums-guidelines/precinct-and-perimeter). Site access, neighborhood relationships, ancillary uses and size/event-dependent precincts.
- **R19 — Environment Agency, England (updated 2023):** [Cemeteries and groundwater risk](https://www.gov.uk/guidance/cemeteries-and-burials-groundwater-risk-assessments). Site-specific source/pathway/receptor and groundwater/flood considerations; not applied as a worldwide burial rule.
- **R20 — ASHRAE:** [Data-center site planning](https://www.ashrae.org/technical-resources/topics-and-initiatives/ai-data-center/site-planning). Available power, workload/expansion and cooling/resource choices; this is guidance, not an asserted adopted local building code.
- **R21 — IAU, working-group recommendations (2025):** [Protection of astronomical sites](https://www.iau.org/IAU/News/Ann2025/ann25010.aspx). Darkness and light-control considerations for professional optical observatories. The page describes recommendations and a possible future resolution; it is not treated as already enacted law.
- **R22 — UN-Habitat (2015):** [International Guidelines on Urban and Territorial Planning](https://unhabitat.org/international-guidelines-on-urban-and-territorial-planning). Integrated settlement planning, access to services, connected public spaces and land-use relationships.
- **R23 — U.S. Fire Administration:** [Fire/EMS station design, chapter 3](https://www.usfa.fema.gov/downloads/pdf/publications/design_of_fire_ems_stations.pdf). Location follows service delivery/response and access; no cited example response benchmark is adopted as universal. The indexed chapter excerpt was reviewed; full-PDF retrieval exceeded the browsing tool's size limit, so a complete document review is not claimed.
- **R24 — IFC/World Bank Group:** [Power-sector EHS index](https://www.ifc.org/en/insights-reports/general-environmental-health-and-safety-guidelines/ehs-guidelines-power). Different generation technologies and transmission/distribution scopes need different guidance.
- **R25 — IFC/World Bank Group:** [Infrastructure-sector EHS index](https://www.ifc.org/en/insights-reports/general-environmental-health-and-safety-guidelines/ehs-guidelines-infrastructure). Identifies separate healthcare, rail, fuel-network, telecom, airport and tourism scopes. The index lists 2026 healthcare/airport updates; their detailed PDFs were not comprehensively reviewed in this pass, so no detailed requirement is attributed to them.
- **R26 — WHO/UNICEF/UN-Habitat (2026):** [Child-friendly public spaces / SPACES](https://www.who.int/news/item/21-01-2026-new-guide-promotes-safe--inclusive-and-child-friendly-public-spaces). Safety, play, access, child health, equity and sustainability as public-space planning considerations.

### Research gaps and missing archetypes

The engine now has conditional nuclear technology dependencies and 2D cooling/security reservations. Full nuclear campuses, landfill cells, battery storage, individual cooling reservoirs/towers, abstraction/outfall equipment, detailed treatment/reuse processes, airport/runway complexes and port operational assets still need dedicated support definitions. Source- and groundwater/wind/resource layers also do not exist merely because the raster contains water, grass or rock.

Quantified rules remain open for local hazards, emissions/noise, protection/exclusion areas, grid/water capacity, crowd/vehicle/service throughput, hydraulic direction and regional/cultural design. Dedicated reviews are still needed for prison/correctional facilities, religious/cultural orientation, hazardous laboratories, detailed mining/processing and species/crop-specific agriculture. The matrix records those gaps instead of claiming all 197 families have fully validated real-world requirements.

The catalog coverage table below is the research classification audit used by the implemented profiles: every current ID receives a baseline and applicable refinement. It does not mean every family was independently researched or that every proposed physical rule is implemented. New catalog IDs need explicit profile coverage and must retain unresolved research gaps.

<!-- structure-siting-coverage:start -->

Coverage: **197 existing families / 15 categories**, all assigned `S01` plus functional refinements. `S08` is now a conditional nuclear specialization of `power-station`, without adding a catalog family. Profiles containing several uses remain conditional on their selected technology. This is classification coverage, not 197 independently validated siting studies.

| Existing catalog ID | Category | Proposed profiles |
| --- | --- | --- |
| `agricultural-equipment-shed` | Agriculture | S01, S29 |
| `aircraft-hangar` | TransportLogistics | S01, S32 |
| `airport-terminal` | TransportLogistics | S01, S32 |
| `ambulance-depot` | CivicSafety | S01, S22 |
| `apartment` | Residential | S01, S41 |
| `apartment-complex` | Residential | S01, S41 |
| `aquaculture-facility` | Agriculture | S01, S31 |
| `art-gallery` | CultureWorship | S01, S40 |
| `assembly-plant` | IndustryExtraction | S01, S03 |
| `athletics-track` | SportRecreation | S01, S25 |
| `bakery` | FoodHospitality | S01, S26, S05 |
| `bank` | OfficeFinance | S01, S42 |
| `barn` | Agriculture | S01, S29 |
| `basketball-court` | SportRecreation | S01, S25 |
| `botanical-garden` | PublicSpace | S01, S38 |
| `breakwater` | LinearWaterInfrastructure | S01, S37 |
| `bungalow` | Residential | S01, S41 |
| `bus-depot` | TransportLogistics | S01, S34 |
| `bus-station` | TransportLogistics | S01, S33 |
| `bus-stop` | TransportLogistics | S01, S33 |
| `cafe` | FoodHospitality | S01, S26 |
| `canal-lock` | LinearWaterInfrastructure | S01, S37 |
| `car-dealership` | Retail | S01, S27 |
| `catering-kitchen` | FoodHospitality | S01, S05 |
| `cathedral` | CultureWorship | S01, S40 |
| `cell-tower` | UtilitiesCommunications | S01, S13 |
| `cement-plant` | IndustryExtraction | S01, S02 |
| `cemetery` | PublicSpace | S01, S39 |
| `chemical-plant` | IndustryExtraction | S01, S02 |
| `church` | CultureWorship | S01, S40 |
| `cinema` | CultureWorship | S01, S40 |
| `city-retail` | Retail | S01, S26 |
| `civic-office` | CivicSafety | S01, S23 |
| `civil-defense-center` | CivicSafety | S01, S22 |
| `clinic` | Healthcare | S01, S21 |
| `co-working-center` | OfficeFinance | S01, S42 |
| `cold-storage-warehouse` | IndustryExtraction | S01, S04 |
| `college` | EducationResearch | S01, S19 |
| `community-center` | CivicSafety | S01, S23, S38 |
| `concert-hall` | CultureWorship | S01, S24 |
| `concrete-batching-plant` | IndustryExtraction | S01, S02 |
| `conference-hotel` | FoodHospitality | S01, S28 |
| `container-depot` | IndustryExtraction | S01, S04 |
| `convenience-store` | Retail | S01, S26 |
| `convention-center` | CultureWorship | S01, S24 |
| `corporate-campus` | OfficeFinance | S01, S42 |
| `courthouse` | CivicSafety | S01, S23 |
| `courtyard-house` | Residential | S01, S41 |
| `cultural-center` | CultureWorship | S01, S40 |
| `dairy-farm` | Agriculture | S01, S30 |
| `dam` | LinearWaterInfrastructure | S01, S37 |
| `data-center` | UtilitiesCommunications | S01, S12 |
| `dental-clinic` | Healthcare | S01, S21 |
| `department-store` | Retail | S01, S27 |
| `desalination-plant` | UtilitiesCommunications | S01, S16 |
| `detached-house` | Residential | S01, S41 |
| `distribution-center` | TransportLogistics | S01, S04, S34 |
| `dormitory` | Residential | S01, S41 |
| `duplex` | Residential | S01, S41 |
| `electrical-substation` | UtilitiesCommunications | S01, S11 |
| `embassy` | CivicSafety | S01, S23 |
| `emergency-department` | Healthcare | S01, S21 |
| `emergency-shelter` | UtilitiesCommunications | S01, S22 |
| `ev-charging-hub` | TransportLogistics | S01, S43 |
| `exhibition-center` | CultureWorship | S01, S24 |
| `factory` | IndustryExtraction | S01, S02 |
| `farmhouse` | Agriculture | S01, S29 |
| `fast-food-restaurant` | FoodHospitality | S01, S26 |
| `feed-mill` | Agriculture | S01, S30 |
| `ferry-landing` | LinearWaterInfrastructure | S01, S37 |
| `fire-station` | CivicSafety | S01, S22 |
| `food-court` | FoodHospitality | S01, S26 |
| `food-processing-plant` | IndustryExtraction | S01, S05 |
| `football-stadium` | SportRecreation | S01, S24 |
| `football-training-ground` | SportRecreation | S01, S25 |
| `freight-terminal` | TransportLogistics | S01, S04, S34 |
| `fuel-station` | TransportLogistics | S01, S43 |
| `funeral-home` | PublicSpace | S01, S39 |
| `furniture-store` | Retail | S01, S27 |
| `garden-center` | Retail | S01, S27 |
| `government-office` | CivicSafety | S01, S23 |
| `grain-silo` | Agriculture | S01, S29 |
| `grain-warehouse` | IndustryExtraction | S01, S04 |
| `greenhouse` | Agriculture | S01, S29 |
| `grocery-store` | Retail | S01, S26 |
| `guardhouse` | PublicSpace | S01, S44 |
| `guesthouse` | FoodHospitality | S01, S28 |
| `gym` | SportRecreation | S01, S25 |
| `hardware-store` | Retail | S01, S27 |
| `health-center` | Healthcare | S01, S21 |
| `heavy-factory` | IndustryExtraction | S01, S02 |
| `hospital` | Healthcare | S01, S21 |
| `hostel` | FoodHospitality | S01, S28 |
| `hotel` | FoodHospitality | S01, S28 |
| `hypermarket` | Retail | S01, S27 |
| `indoor-arena` | SportRecreation | S01, S24 |
| `insurance-office` | OfficeFinance | S01, S42 |
| `jetty` | LinearWaterInfrastructure | S01, S37 |
| `kindergarten` | EducationResearch | S01, S18 |
| `library` | EducationResearch | S01, S38 |
| `light-factory` | IndustryExtraction | S01, S03 |
| `livestock-shed` | Agriculture | S01, S30 |
| `local-market` | Retail | S01, S26 |
| `logistics-center` | TransportLogistics | S01, S04, S34 |
| `marina-building` | TransportLogistics | S01, S35 |
| `maternity-hospital` | Healthcare | S01, S21 |
| `medical-center` | Healthcare | S01, S21 |
| `memorial` | CultureWorship | S01, S40 |
| `mine-shaft-building` | IndustryExtraction | S01, S06 |
| `mosque` | CultureWorship | S01, S40 |
| `motel` | FoodHospitality | S01, S28 |
| `multipurpose-stadium` | SportRecreation | S01, S24 |
| `multistorey-car-park` | TransportLogistics | S01, S43 |
| `museum` | CultureWorship | S01, S40 |
| `nursing-home` | Healthcare | S01, S21 |
| `observatory` | EducationResearch | S01, S20 |
| `office-building` | OfficeFinance | S01, S42 |
| `office-tower` | OfficeFinance | S01, S42 |
| `oil-gas-processing-plant` | IndustryExtraction | S01, S02 |
| `oil-well-site` | IndustryExtraction | S01, S06 |
| `packing-house` | Agriculture | S01, S05 |
| `park-pavilion` | PublicSpace | S01, S38 |
| `parking-lot` | TransportLogistics | S01, S43 |
| `pedestrian-bridge` | LinearWaterInfrastructure | S01, S36 |
| `pharmacy` | Healthcare | S01, S26, S21 |
| `playground` | PublicSpace | S01, S38 |
| `police-station` | CivicSafety | S01, S22 |
| `port-terminal` | TransportLogistics | S01, S35 |
| `post-office` | OfficeFinance | S01, S42 |
| `poultry-house` | Agriculture | S01, S30 |
| `power-station` | UtilitiesCommunications | S01, S07 |
| `primary-school` | EducationResearch | S01, S18 |
| `prison` | CivicSafety | S01, S45 |
| `public-square` | PublicSpace | S01, S38 |
| `public-toilet` | PublicSpace | S01, S38 |
| `pumping-station` | UtilitiesCommunications | S01, S14 |
| `quarry-facility` | IndustryExtraction | S01, S06 |
| `quay` | LinearWaterInfrastructure | S01, S37 |
| `railway-bridge` | LinearWaterInfrastructure | S01, S36 |
| `railway-station` | TransportLogistics | S01, S33 |
| `recycling-center` | UtilitiesCommunications | S01, S17 |
| `refinery` | IndustryExtraction | S01, S02 |
| `rehabilitation-center` | Healthcare | S01, S21 |
| `research-laboratory` | EducationResearch | S01, S19 |
| `residential-tower` | Residential | S01, S41 |
| `resort` | FoodHospitality | S01, S28 |
| `restaurant` | FoodHospitality | S01, S26 |
| `retail-kiosk` | Retail | S01, S26 |
| `road-bridge` | LinearWaterInfrastructure | S01, S36 |
| `road-tunnel` | LinearWaterInfrastructure | S01, S36 |
| `sawmill` | IndustryExtraction | S01, S02 |
| `school` | EducationResearch | S01, S18 |
| `science-park` | EducationResearch | S01, S19 |
| `secondary-school` | EducationResearch | S01, S18 |
| `senior-housing` | Residential | S01, S41 |
| `shopping-arcade` | Retail | S01, S26 |
| `shopping-mall` | Retail | S01, S27 |
| `skatepark` | SportRecreation | S01, S25 |
| `solar-farm` | UtilitiesCommunications | S01, S09 |
| `sports-hall` | SportRecreation | S01, S25 |
| `stable` | Agriculture | S01, S30 |
| `steelworks` | IndustryExtraction | S01, S02 |
| `stock-exchange` | OfficeFinance | S01, S42 |
| `student-residence` | Residential | S01, S41 |
| `supermarket` | Retail | S01, S27 |
| `swimming-center` | SportRecreation | S01, S25 |
| `synagogue` | CultureWorship | S01, S40 |
| `telecom-exchange` | UtilitiesCommunications | S01, S13 |
| `temple` | CultureWorship | S01, S40 |
| `tennis-center` | SportRecreation | S01, S25 |
| `terraced-house` | Residential | S01, S41 |
| `textile-mill` | IndustryExtraction | S01, S02 |
| `theatre` | CultureWorship | S01, S40 |
| `town-hall` | CivicSafety | S01, S23 |
| `townhouse` | Residential | S01, S41 |
| `trade-center` | OfficeFinance | S01, S42 |
| `training-center` | EducationResearch | S01, S18 |
| `truck-depot` | TransportLogistics | S01, S34 |
| `university-building` | EducationResearch | S01, S19 |
| `university-campus` | EducationResearch | S01, S19 |
| `vehicle-maintenance-workshop` | IndustryExtraction | S01, S03 |
| `veterinary-clinic` | Healthcare | S01, S21 |
| `villa` | Residential | S01, S41 |
| `village-house` | Residential | S01, S41 |
| `visitor-center` | PublicSpace | S01, S38 |
| `vocational-school` | EducationResearch | S01, S18 |
| `warehouse` | IndustryExtraction | S01, S04 |
| `waste-to-energy-plant` | UtilitiesCommunications | S01, S07, S17 |
| `waste-transfer-station` | UtilitiesCommunications | S01, S17 |
| `wastewater-treatment-plant` | UtilitiesCommunications | S01, S15 |
| `watchtower` | PublicSpace | S01, S44 |
| `water-tower` | UtilitiesCommunications | S01, S14 |
| `water-treatment-plant` | UtilitiesCommunications | S01, S14 |
| `wholesale-market` | Retail | S01, S27 |
| `wind-farm` | UtilitiesCommunications | S01, S10 |
| `worker-housing` | Residential | S01, S41 |
| `workshop` | IndustryExtraction | S01, S03 |

<!-- structure-siting-coverage:end -->

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

## Remaining semantic contract extensions (proposed)

Extend the active `BuildingDefinition`, `SettlementDemand`, `MockupWorldDesign`, `SettlementRegionRule`, `InfrastructureDesignRule` and their planners. Add referenced district, technology, dependency and rule-evaluation records through those contracts; do not create replacement catalogs or duplicate planning interfaces just because an earlier design listed possible interface names.

District land use, technology, typed utility/resource ports, declared supply/consumption, conditional dependencies and evaluated rule evidence now exist in the shared siting contracts. Jobs/service allocation, culture/era, environmental processes and growth constraints remain broader future requirements. Generic factory, power-station, pumping and storage entries require an explicit supported technology in contextual designs.

Use explicit per-profile data and versioned rule composition; no single giant class or enum defines all future worlds. The implemented contract above identifies current capabilities; the broader acceptance goals below remain subject to the stated verification limits.

## Acceptance tests
- City, town, village using same terrain/seed produce different coherent functional building programs and networks.
- Every building explains why included; no unrelated office tower in agricultural hamlet absent explicit scenario override.
- Every built parcel has valid frontage and intended access, and each public service meets its configured coverage/capacity rules.
- Two worlds with similar population but different purposes (university vs industrial) require different facilities and distribution.
- No utilities/roads omitted when buildings require them; shortages are reported instead of bypassed.
- Changing local land use/regulation recalculates affected building program and dependent objects while preserving stable IDs elsewhere.
- Building catalog and placement remain agnostic to Godot and GameArts artwork generation.
