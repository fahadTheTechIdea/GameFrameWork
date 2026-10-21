# Deterministic, constraint-driven placement policy

**Status: mandatory architectural rule, not yet implemented.**

No world component may be placed merely by choosing a random tile, coordinate, or point. A deterministic pseudorandom seed does not make an arbitrary placement acceptable.

For each proposed world feature, determine:
1. **Purpose**: why it exists and which entities it serves.
2. **Inputs**: source geometry, biome, infrastructure, topography, policy, demand, simulation state and available approved assets.
3. **Hard constraints**: valid footprint, slope, land use, hydrology, collision-free geometry, clearances, connectivity, access, navigation, structural support, safety restrictions and neighboring interactions.
4. **Soft objectives**: coverage, walkability, network efficiency, visual coherence, ecology, historical style and resource cost.
5. **Candidate generation**: use valid polygons, graph nodes and attachment anchors; never sample a uniform map position as a final placement decision.
6. **Optimization**: score and select from feasible placements with explainable reasons and objective scores.
7. **Dependent generation**: derive roads, curbs, markings, driveways, lights, gardens, attachments and resource connections from parent features.
8. **Verification**: validate geometry, adjacency, overlap exceptions, route connectivity, runtime state transitions and configured policies after each accepted placement.
9. **Failure**: return unfulfilled constraints and alternatives, not a fallback arbitrary position.

## Randomness
Seeded randomness can be used for controlled variation **inside a feasible set** or as a tie-breaker of comparable scored alternatives. Use stable independent per-subsystem streams. Do not use a random stream to bypass hard rules or to scatter props arbitrarily.

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
