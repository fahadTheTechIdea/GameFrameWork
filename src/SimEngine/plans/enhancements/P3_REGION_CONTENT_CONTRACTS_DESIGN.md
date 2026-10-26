# P3-S13 — Contract-owned authored regions and operating geometry

## 0. Problem statement

`Content/SpatialRegionDef.cs` is a required base for region resolution in work policy, target
requirements, operating areas, contact exploration and regional layer discovery. Public geometry
contracts exist, but a direct content provider cannot join these original engine paths.

## 1. Goals

1. Every authored-region consumer resolves the same actual `ISpatialRegionDefinition` alias.
2. Share strict space/shape declaration and version construction; retain sole geometry algorithms.
3. Native helper remains an optional implementation, not a mod dependency or compatibility path.
4. Prove direct non-tile region definitions through physical supply/contact, work requirements,
   private editable areas, saved policy/shape continuation and regional layer visibility.
5. Refuse invalid metadata and contradictory factories without a default region or extra owner.

## 2. Design

`ISpatialRegionDefinition` combines the existing content/geometry/factory contracts and actual
space-definition metadata. `SpatialRegionDeclaration` is the one shared reader/resolver/canonical
shape/version builder. Native SpatialRegionDef delegates it; the existing point interval provider
becomes a direct Def and retains its sole interval implementation for containment/travel/version geometry.

Migrate existing consumer types/selectors; no additional region registry, operating-area store,
knowledge ledger, route planner, game loop or clock. `SpatialRegionContracts` validates frozen
actual aliases, actual space and explicit shape metadata. Existing OperatingAreas owns versions
and persistence; scoped observations guard the selected template factory. Content/format shape
errors remain ordinary typed refusals; contradictory provider output faults the session.

## 3. API surface

New region content contract and shared declaration. Existing region/travel/operating-area contracts,
commands and immutable WorkView/ExplorationView/layer views retained. No host/rendering implementation.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| Modify | Content/SpatialRegionDef.cs | 100 | high |
| Modify | Standard/OperatingAreas.cs | 30 | high |
| Modify | Existing work/exploration/regional visibility selectors | 20 | medium |
| Modify | ExtensionProof/PointRegionProof.cs | 60 | medium |
| New | ExtensionProof/RegionContentProof.cs and authored JSON | 90 | medium |
| New | Tests/RegionContentContractTests.cs | 200 | high |
| Modify | Existing pure factory/test/resource/protocol registration | mechanical | medium |

## 5. Errors

Unknown aliases/space/shape metadata fail content composition. Invalid version shapes refuse through
original OperatingAreaCodes.InvalidShape. Stale revisions and assigned removal retain existing typed
semantics. Contradictory geometry is a contract error, never a fallback. Physical outside/unsupported
checks retain original blockers and preserve actual stock/progress. Invalid saves reject restore.

## 6. Compatibility

Development-only direct contract migration, no legacy reader or alternate registry. Build2.381.0,
save154/replay1 and original operating-area/contact/knowledge codecs retained.

## 7. Verification

ONE NEW exact bounded case: `Region content contracts: physical discovery and private area versions`.
Build when implementation is ready; repair/rerun only this same new case. Freeze executable inputs
after final green and record all builds/runs. No historical/family/full/Oil/Tycoon/platform runtime.

## 8. Risks

Metadata/factory observation is scoped, not rollback or a hostile-code sandbox. Every runtime area
is a projection of original company-owned saved shape intent, not another authoritative definition.
Point geometry has authored bounds; no platform/game-size cap is introduced.

## 9. Out of scope

Excluded Tycoon files, OilFieldDays, adapters/Draw/hosts/demos, remote/cell-wide exploration, broad
utility/provider/fault/scale/fuzz/soak/full acceptance and premature commit/push. G19 remains pending.

## 10. Execution order

Inspect original owners → shared contract/declaration → migrate original consumers/provider → author
new proof scenario/player driver → build → one exact case → freeze → tracker/audit/evidence refresh.

## 11. Status

Focused implementation verified2026-10-09, build2.381.0/save154/replay1. One clean15.01s build,
zero warnings/errors; ONE NEW exact bounded case FIRST/FINAL110/0/0 in747ms, PID21312/exit0,
observed907ms. Selected264 inputs frozen after this green at15:46:47UTC; no later executable edits.
Native rectangular/painted runtime, arbitrary provider mutation/reentry, broad utility/geometry,
stress/fuzz/soak/full acceptance and full P1-P12 remain OPEN. No old cases or broad suites rerun.
[Evidence](../../docs/migration/P3_REGION_CONTENT_CONTRACTS_VERIFICATION.md). No staging/commit/push.
