# P11-D8a / P1-G1 — Engine-authored native startup and physical surveys

Current authored colony follow-up [P4-I7](P4_AUTHORED_COLONY_RULES_DESIGN.md),2026-10-08:
compiled routes/placement deleted; default sample selects native layered navigation/optional construction
rules and shared public native band evaluator. Startup lane is explicit13th entry, no compiled row9
restriction. ONE clean build11.82s0/0 on2.276.0/save140;ONE NEW bounded case53/0/0 1626ms proves
edited actual row11 travel and saved physical construction; original survey/sensor gates NOT rerun.
Other custom startup/policies/modes/lifecycle seal/mod/global purity/full goal remain OPEN.
[Evidence](../../docs/migration/P4_AUTHORED_COLONY_RULES_VERIFICATION.md).

Historical startup contract migration [P4-I6](P4_AUTHORED_COLONY_SETUP_DESIGN.md),2026-10-08:
TileLayerSetupDef requires explicit width/height, validates wide map bounds, preflights every native
sample and generates row-major rectangle through the original owner. Native survey/sensor/layered
navigation/occlusion setup documents directly author1x1 for22 existing entries, no legacy fallback.
ONE NEW public-player case81/0/0 1512ms also covers byte/int/private fixed2x2 samples/strict values/
initial save. No original native survey/sensor/etc runtime rerun. Build2.275.0/save140/codecs/clock
unchanged; ONE clean build8.66s0/0. Colony now uses same master-listed startup pipeline. No new
registry/global rollback/atomic-provider guarantee. Source frozen, full framework still active.
[Evidence](../../docs/migration/P4_AUTHORED_COLONY_SETUP_VERIFICATION.md).

2026-10-07. Scoped implementation verified:58 checks/ONE distinct NEW bounded case,0 final failures/
aborts365ms. TWO clean builds11.86s/6.38s; first case2 checks/1 aborted163ms from public-layer ownership
API misuse; repair preserves native guard. Full framework goal remains active.
[Evidence](../../docs/migration/P11_NATIVE_SURVEY_STARTUP_VERIFICATION.md).

## 0. Problem

D8 native sample capture is protocol-tested, not timed native play. Existing ColonyGame startup and
navigation hard-code a fixture's terrain/starts and bind a test economy. A new survey must not rely on
that module, host-created workers, test-written stock/progress, or another survey executor.

## 1. Goals

1. Optional engine WorldSetupModule selects explicit master JSON startup graph, no host gameplay.
2. Startup stages are contract/content extensions using existing DependencyGraph/world-generation
   pipeline; current placement/value/creation owners commit actual state. No saved setup registry.
3. Existing NavService accepts explicit authored uniform tile rules through TileNavigationModule.
4. Native worker travels and consumes materials/time before existing knowledge effect captures.
5. Partial/cancelled/refresh/private/save/load behavior is proved with normal commands/views.

## 2. Design

WorldSetupDef/IWorldSetupDefinition declare after dependencies, pure current Check and Generate.
WorldSetupProfileDef compiles selected entries with existing DependencyGraph; missing/cyclic refs
fail content validation. SAME world-generation pass checks then executes each stage in stable order.
Transient context maps authored entry IDs to actual created IDs only during startup; actual entity
store, worker.Home, map values and their existing Sync remain authoritative after startup.
EntitySetupDef delegates opaque JSON placement to selected ISpatialPlacementIntent and World.Spawn;
explicit company/placement/home, required home dependency/actual same-company worker relation.
TileLayerSetupDef delegates explicit typed sample/owner writes to SAME MapLayer/TileMap owner.
Failure refuses unpublished NewGame, not fabricated rollback or a resumable partial startup.

MapLayer GetSample/CheckSample/SetSample reuse existing QuantityValue/StoredValue/Set conversion and
resource callbacks. Native TileReadingDef uses GetSample; no second type interpreter/value grid.
UniformTileNavigationDef binds explicit positive cost/space through existing NavService.Define.
Contract-based rule subclasses may extend authoring; no new navigation service/search/cache.

Actual embedded master/world/work/knowledge/navigation/setup JSON uses only native modules. No
Tycoon/economy, mod gameplay module or raw test spawning/stock/knowledge/progress for this workflow.

## 3. API

Optional sim.world-setup plus world_setup_profiles/entity_setups/tile_layer_setups sections;
required profile scenario setting. Typed setup context/results are startup intent links, not live
entity registry. uniform_tile_navigation belongs to SAME sim.tile-navigation. Existing lifecycle,
normal work/policy commands and immutable knowledge views remain public player boundary.
Build2.232.0/save136 direct semantic migration; existing state/view codecs unchanged.

## 4. Files

New Standard/WorldSetup.cs, WorldSetupModule.cs, TileNavigationRules.cs; existing TileNavigationModule,
Map/TileMap, Standard/SpatialReadings; actual native JSON assets and one bounded proof; identities
compile-only migration; docs/tracker/evidence.

## 5. Errors

Missing/cyclic setup/home refs, bad profiles/companies/placement/value/nav cost refuse before usable
game is returned. Current placement collisions and protected values refuse through existing owners.
Pure startup checks cannot allocate entities/advance the clock. No defaults or instant player commands.

## 6. Compatibility

Direct development migration, no old startup/config reader or compatibility mode.

## 7. Verification

ONE exact NEW bounded native case: authored startup, travel, spent materials, delayed capture,
refresh/cancel/private reads, partial saves/twins and invalid startup declarations. Build then case;
repair/rerun only observed failure. Older cases compile only. Freeze source/JSON/tests after green.
No older/family/full/platform/OilFieldDays test gate.

## 8. Risks

Startup is initial authored creation, never a normal free-build/spawn gameplay fallback. Context must
not survive startup or become a second entity store. Pure checks and transient links are not global
provider mutation sandbox. Navigation costs remain deterministic; world sizes are content intent.

## 9. Exclusions

Dynamic sensors/exploration/sharing/expiry, broad startup/provider/privacy/fault/replay/stress/full
gates, OilFieldDays/hosts/demo/Draw/platform/business gameplay, commit/push.

## 10. Sequence

1. Extend existing native value/navigation owners and add generic authored startup stage contracts.
2. Master JSON native scenario/work/reading/start profile, normal driver and one bounded proof.
3. Build/exact gate; freeze then evidence/tracker/source inventory.
