# P4-N3 — Sole native point startup ownership

Historical P4-N3 runtime evidence; not rerun on2.330.0. P4-N4 now validates selected startup declarations before Initialize; actual geometry/occupancy remains during generation. [Current evidence](../../docs/migration/P4_STARTUP_COMPOSITION_VERIFICATION.md).

Status: scoped migration implemented; ONE NEW exact case44/0/0 verified. Full P1–P12 open. [Evidence](../../docs/migration/P4_NATIVE_POINT_STARTUP_VERIFICATION.md).

## Responsibility and contracts

WorldSetup is the single startup owner for geometry-backed and nonspatial entities. The point provider is an external contract proof, not a separate engine. Delete its PointStartDef/PointStart classes, point_starts registration and world-generation stage; do not keep a renamed executor, legacy reader, automatic spawn scan or compatibility loader. PointSpace retains its actual ISpatialPlacementIntent implementation, presence, movement and dock contracts. EntitySetupDef and the existing WorldSetupStage resolve/validate/execute native entries; IWorldSetupProfileDefinition and the existing DependencyGraph compiler own profile includes and dependency ordering.

Every entity startup entry explicitly authors stable id, after[], entity, company, placement(object or null), and home(id or null). Spatial entries supply actual provider-specific placement even when it happens to equal a prototype's authored creation position. Nonspatial entries explicitly use null. Homes reference actual entry IDs and their dependency edges, not prototype IDs. No startup placement is inferred from a prototype at runtime.

Each native profile explicitly declares entries[] and includes[]. The scenario explicitly selects sim.world-setup and its root profile setting. Loading a document alone cannot select a profile or spawn its entries. Shared aliases execute once through the same native graph. Master module/document ownership stays explicit; no file discovery.

## Migration stages

1. Inventory all obsolete point sections and authoring calls. Preserve existing unrelated dirty work.
2. Replace35 authored JSON sections/82 entries in place. Copy each old entry's authored company and its authored opening placement once during source migration; preserve every unrelated JSON value. Four supplemental documents reference existing base point prototypes; their placements are now explicitly authored too. Add35 native profiles, each with required arrays. Update the two master module lists with the existing WorldSetup module.
3. Update PointGame composition to register native WorldSetup. Callers explicitly include supplemental profile IDs. Remove the provider's duplicate startup code. Keep prototype creation placement distinct from actual startup placement.
4. Rewrite fixture authoring in native JSON terms. Added entries carry an explicitly supplied placement; replaced/filtered entry sets update selected-profile membership together. Fixtures intentionally changing opening coordinates now author the native entry's placement, not only a prototype default. Supplemental files are explicitly selected at their fixture entry points.
5. Keep fixture authoring thin: StartupContentAuthoring writes input JSON only; it never reads a live World, spawns, validates gameplay, grants resources, advances work, handles saves or drives a second clock. WorldSetup remains the only gameplay authority. No reader accepts obsolete owner/point-start grammar.
6. Add one normal-master point proof with an explicitly listed supplemental JSON file. Verify public commands/views, actual placement/ownership/home, selection/no implicit provider spawning, physical work and hauling, saved twins/no regrant, replay and strict schema/reference/cycle refusals.
7. Update the authoritative master, evidence and machine-readable audit without changing original acceptance checkbox text/counts. Do not publish before full goal and plan completion.

## Verification and known boundaries

Three clean compilation checks; one new bounded case only, repaired/rerun solely that case. The first run reached19 checks then rejected a geometry-only fixture that still loaded content owned by inactive WorldSetup. Remove those explicit startup sections/documents from that geometry-only input; preserve the native owner-enrollment rejection. The final44 checks pass.

Core2.329.0/save154/replay1 and native/provider state codecs remain unchanged: this migration deletes duplicate external startup behavior and uses the existing native implementation. No engine saved field, scheduler, clock, resource owner or topology algorithm was added. Proof startup/native content fingerprints change naturally. Existing migrated fixtures, including optional-money fixtures, are source/compile checked only, not runtime revalidated. No Tycoon rules changed or Tycoon test run.

Full regression, deep composition/scale, fault injection, mod/provider breadth, whole-generation transactional guarantees, general host/public-boundary cleanup and full P1–P12 acceptance remain open. Spatial/company admission retains its actual generation lifecycle after Initialize; only schema/reference/graph errors tested here are before-Initialize refusals. No Oil consumer, business expansion, Draw, platform or adapter implementation.

The user's commit/push condition is not yet satisfied. No staging/commit/push.
