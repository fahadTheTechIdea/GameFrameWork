# P3-G1: Scenario-owned tile geometry — Design Document

## 0. Problem Statement

ITileSpaceDefinition exposed one Width/Height pair, forcing shared prototypes into one map size. Native movement, docks and content validators also assumed those fixed dimensions. Oil's six authored sizes could not bind to its stable oil.tiles identity.

## 1. Goals

1. One immutable space definition declares fixed or explicit per-scenario geometries.
2. The engine selects exactly the current scenario's bounds, without a missing-variant fallback.
3. Shared prototypes/layers remain shared metadata; each session owns its existing TileMap.
4. Context-free native checkpoint validation uses the entity's immutable session identity.
5. Existing spatial consumers use declarations for shape admission and selected bounds for execution.
6. Preserve Oil's six sizes and keep complete Oil/runtime/broad gates open.

## 2. Design

ITileSpaceDefinition now exposes Geometries and ReadGeometry(string scenario). Old fixed Width/Height accessors are deleted. TileSpaceGeometry is an immutable value with explicit scenario identity, dimensions, overflow-safe containment, equality and deterministic hash.

SpatialSpaceContracts is the existing admission owner, extended to validate canonical unique positive geometries, select a required scenario entry, verify provider output against actual declared metadata and validate real scenario/space aliases. TileSpaceDeclaration remains the sole native JSON author/composer. Fixed width/height is deliberate universal authoring; scenario arrays are an exclusive authored mode, not a compatibility fallback.

Native composition creates the existing TileMap from the selected geometry and registers the same actual layer aliases. No mutable selection is retained on shared content. Sparse chunk ceiling arithmetic was inspected and already handles int.MaxValue without adding an engine/platform clamp.

EntityStore receives the actual immutable scenario identity at World construction. Spawn, replacement preparation and restore give each Entity that same internal identity. This is a session ownership coordinate, not a second world, clock, map, size state or public mutable escape. Native movers and dock checkpoints resolve through the same space contract even before the first update or during load.

Regions, readings, resource cells and layer setup structurally fit a declared geometry. Painted regions must fit wholly within one variant, not a union of incompatible bounds. Actual resource/read bindings, placement, navigation, docks and execution use the selected session. Native origin constraints now receive World; pure dock helpers take the selected immutable geometry. Independent providers and affected proof callers migrate directly, with no old API aliases.

## 3. API surface

- ITileSpaceDefinition.Geometries; ReadGeometry(string scenario).
- TileSpaceGeometry: Scenario, Width, Height, Contains, equality.
- SpatialSpaceContracts.ReadGeometry/ContainsAny.
- ITileNavigationOriginConstraint.CheckNavigationOrigin(World, TileCoord).
- Dock TryTile/CheckOrigin take TileSpaceGeometry.
- JSON tile_spaces: explicit fixed width/height OR nonempty scenarios entries with scenario/width/height.

## 4. Files to change

Sole spatial declaration/contracts; original entity/world, movement/navigation/dock, region, reading/resource/setup consumers; affected independent/proof APIs. One new thin case, one independent registration and two authored fixtures. Oil spatial-world.json, scenario space bindings and master. Tracker/evidence/audit.

## 5. Error handling matrix

Missing/empty/duplicate/unknown scenario, mixed authoring, absent identity or nonpositive dimensions: content refusal before initialization. Provider-substituted dimensions: actual contract refusal. Geometry outside every variant: declaration refusal. Outside selected execution bounds: existing typed runtime refusal. No minimum-size intersection, default variant, duplicate prototype catalog or instant/compatibility path.

## 6. Backward compatibility

Development only. Old fixed-size contract accessors removed and callers revised. Deliberate fixed-size authoring remains a native feature through the same new contract. Engine2.533.0/save154/replay1; prior evidence historical.

## 7. Verification

One NEW bounded exact case exercises native and independent declarations with16x16,1048576x131072 and int.MaxValue x64 sparse sessions, shared prototypes/layers, interleaved updates, saves/twin continuation and malformed/provider-substitution refusal. Scoped compilation repairs retain their failed observations. No old/family/full runtime.

The new case does not physically exercise movement/dock travel, every region/resource variant, lockstep, replay, provider fault purity or complete Oil composition. Those broader requirements remain open.

## 8. Risks

A metadata shape can be meaningful only in a larger variant; actual consuming owners must still refuse unavailable selected bounds. Full physical movement/dock and variant content acceptance remain required. Oil still fails85 unrelated body/API integrations and has not loaded current content.

## 9. Out of scope

Platform/Draw/adapters, Tycoon rules/clock changes, complete industrial acceptance, final full regression and Git writes.

## 10. Sub-task execution order

1. Revise the existing geometry authoring/contract/composition.
2. Migrate native structural and selected-session consumers, including checkpoints.
3. Migrate independent providers; add one file-authored focused case.
4. Bind Oil's six preserved dimensions through one space definition.
5. Record scoped evidence and continue complete spatial/resource/activity/operation/module integration.

## Outcome

New exact case207/0/0; complete framework test-project build succeeded. Oil binds six explicit geometry entries in source but still fails85 body/API errors. No original criterion closed. See [evidence](../../docs/migration/P3_SCENARIO_GEOMETRY_VERIFICATION.md).
