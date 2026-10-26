# P4-I7: Authored colony routing and construction rules — Design Document

Historical implementation uses inline bands on2.276.0. Superseded by
[P4-I10 named condition definitions](P4_TILE_CONDITION_DEFINITIONS_DESIGN.md): direct native
lifecycle/contract/JSON replacement on2.278.0; original rules/travel/placement owners retained.
No old inline reader or alias. Its scoped64-check evidence does not rerun this older case.

Status: scoped implementation verified,2026-10-08.53 checks/ONE exact NEW bounded case,
FIRST runtime attempt0 failures/aborts1626ms. ONE clean build11.82s0 warnings/errors.
Build2.276.0/save140/codecs/sole clock unchanged; source frozen after green. Full P1-P12 active.
[Evidence](../../docs/migration/P4_AUTHORED_COLONY_RULES_VERIFICATION.md).

## 0. Problem statement

ColonyModule.Compose in tests/ExtensionProof/ColonyGame.cs still defines land/lane lambdas and
binds compiled ColonyPlacementRule water rejection. LayeredTileNavigationDef and
ITileNavigationRule already own authored layer-dependent routing; ConstructionService already
invokes IConstructionPlacementRule through the actual placement/preview/command owner.
LayeredTileNavigationCondition contains native public-layer interval logic that should not be copied.

## 1. Goals

1. Use existing layered_tile_navigation for JSON land/water and authored lane-layer routes.
2. Remove compiled colony navigation lambdas and ColonyPlacementRule completely.
3. Optional engine tile construction rules module selects explicit contract-based JSON profile;
   normal construction commands/previews use existing ConstructionService, not another placement path.
4. Share one native public layer-band evaluator; no copied condition/value interpreter/state/cache.
5. Strict space/layer/interval/identity/refusal/profile validation, deterministic priority/ID/cell order.
6. Prove JSON-edited placement/routing and actual timed native travel/haul/build/save/load through
   normal player API, not a test-owned gameplay implementation.

## 2. Design

Rename/extract original LayeredTileNavigationCondition to TileLayerBandCondition in
Standard/TileLayerConditions.cs. ITileLayerCondition declares native layer and Matches contract.
One public reusable Read/Resolve/Validate/Matches implementation retains inclusive minimum/
exclusive maximum, explicit null endpoints, integral byte/int bounds, same-space public layers,
actual session metadata and native MapLayer.GetSample. Original layered navigation uses it directly.
No legacy name alias or second evaluator; native band storage remains readonly content, not save data.

ConstructionPlacementRuleDef implements new IConstructionPlacementDefinition plus existing
IConstructionPlacementRule. Native TileConstructionPlacementDef authors space and explicit rules,
each with stable ID, priority, nonempty typed refusal, nonempty AND layer conditions. Sort rules by
descending priority then stable ID; scan each actual whole target footprint row-major. First matching
rule refuses; no match succeeds. Validate wide bounds/native presence/current content/session before
sampling. Profile with explicit [] means no additional authored exclusions, never missing default.

Optional TileConstructionRulesModule (sim.tile-construction-rules) requires original construction
module, declares tile_construction_rules, requires an explicit settings.profile and matching actual
TileMap space, binds the selected definition through the original IConstructionPlacementRule.
No extra placement coordinator/provider-state store, construction scheduler or clock. Existing
ConstructionService command/view/effect path remains unchanged. Additional declared content subclasses
can implement the same abstract definition contract; this slice does not prove arbitrary hostile mods.

New colony-navigation.json authors land base10/water>=1 blocked and lane base0/colony.lanes[1,2)
AND water[0,1) cost10. Startup adds explicit48x1 lane sample with stable dependencies before entities;
colony.json declares public byte lane layer, game dimensions still authored48x32. New
colony-placement.json authors colony.on-water refusal over water>=1. Actual colony-master.json lists
both documents with their declared owners. Scenario selects new optional module/profile. Delete old
compiled colony rules and constructor binding, no fallback/compatibility shim. Native modules available
in factory, which only assembles explicitly authored documents. Other custom scenarios must select
their appropriate policy/setup; they do not inherit a compiled default.

## 3. API surface

Direct condition class rename + reusable ITileLayerCondition/TileLayerBandCondition API.
IConstructionPlacementDefinition/ConstructionPlacementRuleDef; native JSON profile/module.
No new host command, draw API or mutable public gameplay access. Build identity changes, save140/
existing codecs/sole GameClock remain unchanged.

## 4. Files to change

Original LayeredTileNavigation.cs, new TileLayerConditions.cs and TileConstructionRules.cs;
original ColonyGame.cs/factory/module; actual colony master/world/startup and two new rules assets;
ExtensionProof resources; ONE new player test/Program/identity fixtures compile-only. Stage/evidence/
manifest/master/README/AGENTS/original P4/P6/startup documents. No Tycoon business/Oil/host edits.

## 5. Error handling matrix

Missing selected profile/space/layer/private layer/fractional byte band/empty interval/duplicate
rule ID or layer/nonempty refusal absence → ContentException before usable NewGame.
Incompatible runtime placement/session/definition/bounds → typed refusal; inconsistent actual
native layer metadata faults, never substituted layer/provider. Required conditions have no defaults.

## 6. Backward compatibility

Development direct migration. Delete original condition type/compiled rules without aliases/readers.
Older test identities/API references compile-only; old custom scenarios require explicit authoring.

## 7. Verification

ONE exact NEW bounded public-player case: authored terrain refusal from any footprint cell, preview
purity and command parity, JSON threshold/refusal/lane changes affect real state; ordinary water route
refusal, timed physical road motion, loaded material hauling/staged building and saved continuations,
one-clock pause/session isolation, strict malformed composition. Max2000 Updates per physical stage
with public order/job/blocker diagnostics. One build then exact case, repair/rerun only same observed
case failures if necessary. Record every attempt; source/proof/tests/JSON freeze after final green.
No old/family/full/platform/stale runtime. Full release/mod/industrial gates remain OPEN.
140 older identity fixtures compile-only. One pre-build test-field correction used the actual
CommandResult.Refusal API instead of an invented Reason field; no failed build or runtime resulted.
One live build89305/completion poll is one process, not a second build. Native/default movement and
construction still use original owners, and older tests/fixtures were not executed.

## 8. Risks

Changing profile content identity intentionally invalidates old cross-build saves; no legacy reader.
Selected public geometry bands do not implement private knowledge-dependent authorization. Rule reads
are stateless native samples; global hostile-provider purity remains separate. Lane is game-authored
data, not framework size cap. Explicit [] is intentional permission, not silently fabricated terrain.

## 9. Out of scope

Other custom scenario full authoring migration, generic arbitrary-space rule geometry, company/private
terrain knowledge, predictive/congestion route changes, full placement mod/fault/purity/atomic rollback/
huge-map cost/replay/stress proof. Tycoon/Oil/sibling/host/demo/Draw/platform changes. Full goal active.

## 10. Sub-task execution order

1. Extract/rename same layer-band owner and add selected placement definition/module contract.
2. Master-listed colony navigation/placement/lane data; delete obsolete compiled rules.
3. One distinct new focused physical/authoring/invalid case; build/gate/freeze.
4. Accurate evidence/source checks/tracker handoff; no claim full plan or publication complete.
