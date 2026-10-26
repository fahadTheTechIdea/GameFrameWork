# P4-I6: Authored colony startup and rectangular layer setup — Design Document

Status: scoped implementation verified,2026-10-08.81 checks/ONE exact NEW bounded case,
FIRST runtime attempt0 failures/aborts1512ms; ONE clean build8.66s0 warnings/errors.
Build2.275.0/save140/codecs/sole clock unchanged. Source frozen after green. Full P1-P12 active.
[Evidence](../../docs/migration/P4_AUTHORED_COLONY_SETUP_VERIFICATION.md).

## 0. Problem statement

ColonyStart in tests/ExtensionProof/ColonyGame.cs hardcodes lake/island and per-company entities.
The original Standard/WorldSetup.cs contract/pipeline already owns startup dependencies, entity
placement/home links and samples, but TileLayerSetupDef handles only one cell and lacks explicit extent.
The colony master document is a compiled string with implicit extra default documents.

## 1. Goals

1. Original TileLayerSetupDef applies an explicitly authored positive rectangle through native
   MapLayer CheckSample/SetSample; whole rectangle preflight precedes any writes.
2. Reject missing dimensions, overflow/out-of-bounds, incompatible values and company ownership.
3. Replace ColonyStart/SpawnWorker with master-listed JSON lake/island/entities/home dependencies.
4. One real JSON colony master owns default module/document list, no compiled default fragment.
5. Initialization does not advance game clock or create a persisted setup/entity registry.
6. Normal player physical build/hauling/save/load uses same engine owners after authored setup.

## 2. Design

TileLayerSetupDef x/y/width/height are required. Store one TileRect, expose detached Region.
Validate using wide bounds against its authored TileSpaceDef without area narrowing or a fixed
framework cap. Check every native sample before Generate row-major writes; same layer value/owner
conversion and native version/metadata owner. Single-cell intent authors width1/height1 explicitly.
Migrate four existing setup JSON files directly; no old shape reader or dimension defaults.

Original WorldSetupModule/ProfileDef/DependencyGraph/EntitySetupDef/WorldSetupStage remain sole
initialization pipeline. New colony-startup.json authors lake11x11 and dependent island3x3 plus
ten stable entity entries for the authored two-company scenario; workers depend on matching depots.
Selected explicit sim.world-setup profile. Remove ColonyStart and helper, no second startup hook.
Custom scenarios must explicitly author/select their own startup; no implicit generic boot fallback.

Move original Colony.Manifest/default extra module and work documents into colony-master.json.
Factory loads it and only assembles caller-supplied optional module/documents, not gameplay decisions.
ColonyModule retains current navigation and placement providers; their JSON policy migration is separate.
Existing native sample assets gain explicit dimensions. No Tycoon/Oil/host/Draw/platform work.

## 3. API surface

Existing TileLayerSetupDef gains readonly Region; required width/height authoring, no new section.
Colony.Manifest becomes file-backed property. Public gameplay commands/lifecycle remain unchanged.
Build semantic identity changes; save140 and existing runtime codecs/GameClock unchanged.

## 4. Files to change

Standard/WorldSetup.cs; original public ColonyGame.cs; new colony-master/startup JSON and
original colony scenario selection; four native setup JSON assets; ExtensionProof embedded resources.
New exact bounded public-player test/Program/build identity/older identity fixtures compile-only.
Stage/evidence/manifest/master/README/AGENTS and original P4/P6/startup documents.

## 5. Error handling matrix

Missing/negative/zero/overflow rectangle → ContentException, no ignored dimensions or default1.
Wrong native value/company/layer/space → original typed refusal before usable NewGame.
Missing/cyclic entity/home deps or collision → existing dependency/current-placement rejection.
Failures do not grant defaults, reroute startup to old class or return resumable partial game.

## 6. Backward compatibility

Development direct content/API migration; delete old routine, no fallback/legacy or save reader.
Custom older colony fixtures need explicit selected setup authoring; not implicitly migrated at runtime.

## 7. Verification

ONE exact NEW bounded case: master authors actual startup, all lake/island cells, worker homes,
initial clock0, reordered declarations/stable entity facts, authored content edit changes actual initial
state, physical loaded haul/build/material timing, save/load/pause/isolation and strict invalid cases.
Native rectangle byte/int/fixed/private samples use actual migrated native fixtures via pure assembly,
not old runtime cases. Max2000 Update per stage with public diagnostics. Record every attempt.
No older/family/full/platform/stale run; freeze source/proof/tests/JSON immediately after final green.
139 older build-identity fixtures and NativeSurveyTests.Factory visibility compile-only. Reordered
different authored packs compare initial entity/home/layer facts, not cross-pack hashes; saved twins
of the SAME content pack compare hash after every subsequent Update. Only one build/one run occurred.

## 8. Risks

Profile has explicit authored companies0/1, not a framework player-count cap. Other game sizes/
companies/profiles are authored normally. World setup remains initial grants, not free player commands.
Whole sample preflight is not global startup rollback or hostile provider sandbox.
Colony navigation/placement remain explicit engine extension providers, not fully JSON policies yet.

## 9. Out of scope

All older custom colony fixture setup migration, terrain-generation randomness, company-template/
arbitrary-space region definitions, global providers/purity/atomic-startup/replay/stress/full P4/P6.
Tycoon business source/Oil/sibling/hosts/demos/Draw/platform edits. Full goal active, no Git publication.

## 10. Sub-task execution order

1. Extend same layer setup owner and migrate existing authored rectangles.
2. File-based colony master/startup, remove obsolete compiled startup routine.
3. One new public-player content/physical/invalid proof, build/gate/freeze.
4. Evidence/links/source hashes/tracker; keep broad acceptance and full goal open.

