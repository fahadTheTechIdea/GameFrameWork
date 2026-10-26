# P4-G34 — Contract-owned navigation and construction profiles

## 0. Problem statement

TileNavigationModule.Compose enumerates TileNavigationRuleDef instead of ITileNavigationRule.
TileConstructionRulesModule.Compose resolves ConstructionPlacementRuleDef instead of the existing
IConstructionPlacementDefinition. Both native base hierarchies impose unnecessary inheritance.
NavGrid.Cost silently clamps invalid negative provider results and does not enforce its declared
heuristic minimum. ConstructionService directly calls selected policy callbacks without a scoped guard.

## 1. Goals

1. Admit independent file-authored navigation/placement definitions through existing contract aliases.
2. Delete both base hierarchies, no compatibility layer or alternate loader.
3. Extract native uniform/layered navigation and footprint exclusions into sole reusable declarations.
4. Validate every loaded profile identity/space/minimum before Initialize, including unused content.
5. Original NavService/PathSearch/Mover and ConstructionService/work/stock remain execution owners.
6. A new exact player proof covers native/independent profiles, physical work, replay and scoped faults.

## 2. Design

UniformTileNavigationDeclaration, LayeredTileNavigationDeclaration and TileConstructionPlacementDeclaration
own their original JSON reader/resolver/validation/interpretation. Native direct Defs and independent
registered Defs delegate these declarations or implement the original contracts for new semantics.
ResolvedContent validates all loaded profile aliases in its existing lifecycle.
TileNavigationContracts validates actual authored space/identity and pure current costs.
NavService.Define(ITileNavigationRule) captures the declared immutable minimum and registers its guarded
callback in the same NavGrid list. Zero blocks; negative or positive-below-minimum costs fail explicitly,
never clamp/substitute. Existing manual Define remains a native extension hook, not a second catalog.
Construction module selects the actual interface definition; ConstructionService guards each actual
policy callback. EntityObservation.ReadProvider gains optional actual TileMap invalidation observation,
reusing the original scalar guard for predicates/profiles/placement, not hashing whole maps per cell.
No new route/placement/state/store/coordinator/clock. Existing typed refusals/ranking/footprint timing survive.

## 3. API surface

Existing ITileNavigationRule and IConstructionPlacementDefinition retained.
Reusable declarations expose immutable resolved space/rules and native evaluation.
NavService.Define(ITileNavigationRule) binds the original native router.
Existing concrete native Def properties remain their published metadata; obsolete abstract bases removed.

## 4. Files to change

Standard/TileNavigationRules.cs; LayeredTileNavigation.cs; TileNavigationModule.cs; Navigation.cs;
TileConstructionRules.cs; Construction.cs; TileLayerConditions.cs; Kernel/EntityObservation.cs;
Content/ResolvedContent.cs; independent engine module and thin exact proof; current protocol/tracker/evidence.

## 5. Error handling

Unknown/null/ambiguous space and profile aliases, invalid minimum and malformed unused profiles fail
before initialization. Normal route/placement refusals retain their actual authored code.
Live bad costs, callback exceptions and observed map/clock/identity mutation fault the session and
withhold results/checkpoints. No healthy partial answer, implicit profile, fabricated cost or default rules.

## 6. Compatibility

Development only; remove both base classes. Same save154/replay1/map6/mover4.
Build identity changes; no old reader/shim/duplicated native authoring.

## 7. Verification

ONE NEW exact bounded case: Spatial rule contracts: independent JSON profiles drive timed colony work.
Native and independent declarations plus a distinct custom profile demonstrate real substitution.
Pure route/placement preview-command parity, actual loaded hauling/build/materials/time, saved twins,
pause/replay/session isolation, authored edits, early malformed unused profiles and scoped callback faults.
Build only as needed; no historical/family/full/Oil/Tycoon/platform runtime.

## 8. Risks

Scoped codec/clock/identity/map invalidation is not a whole-world/CLR sandbox.
No new arbitrary map-size bound; existing bounded path-search budget is not a platform size restriction.
Provider/concurrency/fuzz/soak/allocation/performance/full accounting and original full gates remain OPEN.

## 9. Out of scope

Business/Tycoon, G19 excluded-file authority, rendering/Draw/platforms, new spatial representations,
new movement/work/placement loops or timing owners. Commit/push only after full goal/plan verification.

## 10. Sub-task order

Inspect existing selectors/owners; stage/tracker; extract sole declarations/delete bases;
actual catalog validation and guarded owner selection; independent content/public-contract proof;
one thin player case; build and bounded exact execution; honest evidence/master/audit refresh.

## 11. Status

Implemented existing contract selectors, sole native declarations and original scoped observation/routing/
placement owners. Core2.371.0/save154/replay1. ONE NEW exact case final193/0/0 in8415ms;
183 selected inputs frozen12:20:06UTC; no executable edits after final pass.
[Scoped evidence](../../docs/migration/P4_SPATIAL_RULE_CONTRACTS_VERIFICATION.md).
All provider/full/global P1-P12 gates OPEN; no publication.
