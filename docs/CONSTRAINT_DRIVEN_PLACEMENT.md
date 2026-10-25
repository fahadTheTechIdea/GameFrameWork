# Deterministic, constraint-driven placement policy

**Status: enforced by the current mockup-driven terrain/settlement/site/planting slice; broader simulation and arbitrary-shape generation remain open.** See [verification](SDLC_VERIFICATION.md).

No world component may be placed merely by choosing a random tile, coordinate, or point. A deterministic pseudorandom seed does not make an arbitrary placement acceptable.

For each proposed world feature, determine:
1. **Purpose**: why it exists and which entities it serves.
2. **Inputs**: source geometry, biome, infrastructure, topography, policy, demand, simulation state and available approved assets.
3. **Hard constraints**: visual-region containment, appropriate physical footprint/capacity, configured land use, collision-free geometry, clearances, connected access and neighboring uses. Detailed slope/hydrology/safety simulation is opt-in; it is not a substitute for game composition.
4. **Soft objectives**: visual coherence, readability, contextual service capacity, walkability and configured ecological/simulation goals.
5. **Design derivation**: use mockup polygons, frontage parcels, graph paths and attachment anchors; never sample a uniform map position or scan for an unoccupied fallback.
6. **Size resolution**: select an authored building variant satisfying context/capacity and its envelope, retaining its physical dimensions. Failure revises the design rather than shrinking a stadium or moving a factory to an unrelated empty site.
7. **Dependent generation**: derive roads, curbs, markings, driveways, lights, gardens, attachments and resource connections from parent features.
8. **Verification**: validate geometry, adjacency, overlap exceptions, route connectivity, runtime state transitions and configured policies after each accepted placement.
9. **Failure**: return unfulfilled constraints and alternatives, not a fallback arbitrary position.

## Randomness
The current generator uses no random placement or noise-based terrain fallback. Terrain comes from declared masks/gradients; buildings come from frontage grammars; public props and planting come from declared patterns. A retained request seed does not authorize new geography. Future cosmetic variation must preserve the approved composition and all constraints.

## Placement data contract
Each placed entity should carry stable ID, creator/generation stage, parent/derived relationship, source/constraint IDs, footprint, orientation, anchor, reason, evaluated objectives, rejection reasons for alternative candidates when useful, deterministic seed/version provenance and dependency region.

## Tests
- For every emitted object, a placement decision and relevant constraints must be inspectable.
- Reject candidates violating hard clearance, footprint, slope, biome or connectivity constraints.
- Road signs correspond to traffic control and face relevant approaches. Streetlights satisfy coverage configuration. Curb ramps connect actual walkways.
- Houses have road/footpath entry; garden placement respects parcel boundaries and usable yard.
- River corridors descend according to terrain/drainage model or declare explicit managed structures.
- Re-running identical inputs yields identical decisions; changing a local constraint invalidates and replans affected entities only.
- No feasible placement must return a failure/diagnostic result rather than generating a wrong object.

This policy governs cities, forests, islands, deserts, industrial complexes and every future world profile.

Related design docs: CITY_GENERATION_RESEARCH.md and CITY_BUILDER_GAME_COMPARISON.md.

## Contextual siting in the active compiler

The current mockup contract requires catalog selection/capabilities, authored districts, technology choices, typed capacity-bearing supplies and explicit dependency connections. The engine resolves compatible profiles before parcels, then checks actual sized structures, required surroundings, resource allocations, provider cycles, neighbor policies and exclusion overlays. Utility/interface corridors are reserved before planting. Rejections request a design revision; no free-space search is introduced. Inputs without contextual data are rejected. Capacities are editable game units and source qualities are authored facts; physical simulation, service-demand allocation and regulatory certification are separate requirements. See the [implemented siting contract](SETTLEMENT_IDENTITY_AND_BUILDING_RULES.md#implemented-siting-contract) and [verification](SDLC_VERIFICATION.md).

## Infrastructure reservations in the active mockup compiler

Bridges, tunnels and water structures use `InfrastructureDesignRule` rather than frontage parcels. The planner resolves size/capacity and validates the alignment, named obstacle, banks/portals/shore, transport modes, exact connection interfaces and compatible approach profiles before compilation. Every selected body and derived abutment/portal/service-edge reservation is protected against incompatible buildings, site uses, parks, plants, public props, unrelated profiled roads and other infrastructure. Intersections are rejected instead of relocating either object. The raster retains underlying water/rock in `InfrastructureLayers`; only a matching validated crossing alignment may create travel over that obstacle. See the [infrastructure design contract](SETTLEMENT_IDENTITY_AND_BUILDING_RULES.md#bridge-tunnel-and-waterfront-design) for supported geometries and runtime limits.
