# P4-G33 — Contract-owned authored tile conditions

## 0. Problem statement

Standard/TileLayerConditions.cs exposes ITileConditionDefinition but TileConditionReferences.Resolve
requires TileConditionDef inheritance. Navigation and construction invoke predicates directly.
This prevents independent file-authored definitions from substituting through the declared contract.

## 1. Goals

1. Resolve actual ITileConditionDefinition aliases without a native predicate base class.
2. Keep native layer-band authoring in one reusable TileLayerBandDeclaration.
3. Validate every loaded condition's actual identity and tile-space alias before Initialize, including unused content.
4. Navigation and construction use one context/pure-evaluation owner, original map invalidation and observation guard.
5. Prove independent JSON predicates change real routing, placement and timed construction through public commands/views.

## 2. Design

Remove TileConditionDef rather than preserving a compatibility hierarchy. TileLayerBandConditionDef
implements the existing interface directly and delegates native band reading/linking/validation/sampling
to TileLayerBandDeclaration. Independent definitions can reuse that declaration or author other predicates.
TileConditionContracts validates actual catalog aliases in the original ResolvedContent lifecycle.
Its Matches validates frozen actual content/map/space, bounds, scoped provider/clock/identity facts and
the original TileMap.Version before returning a result. It does not hash the whole map per route cell.
Both existing consumer loops call this owner. No new predicate registry, condition cache, state codec,
work coordinator, update loop or clock. Original rule ordering, AND composition and physical work remain.

## 3. API surface

Existing ITileConditionDefinition.Space/Matches unchanged. New reusable TileLayerBandDeclaration
Read/Resolve/Validate/Matches with immutable published Layer/Space/Minimum/Maximum properties.
TileConditionContracts.Validate/RequireContext/Matches owns shared admission, not authored predicate semantics.
No public compatibility base or old reader.

## 4. Files to change

Standard/TileLayerConditions.cs; Standard/LayeredTileNavigation.cs; Standard/TileConstructionRules.cs;
Content/ResolvedContent.cs; independent engine extension proof; one new thin player test and registration;
historical compile-only alias assertion; current engine identity and protocol metadata; tracker/evidence.

## 5. Error handling matrix

Missing/foreign/null/ambiguous actual predicate or space aliases fail content composition, even unused.
Malformed band bounds, private layers and cross-space consumer references fail explicitly.
Invalid live context refuses; out-of-bounds samples return false without calling the provider.
Callback exceptions, clock/identity mutation and normal map writes latch the original provider fault,
withhold the result and prevent continued Update/hash/save. No fallback predicate or healthy partial result.

## 6. Backward compatibility

Development only; delete the native predicate base. Save154/replay1 and map6 unchanged.
Build identity changes; no old-save reader, shim or duplicated catalog.

## 7. Verification

ONE NEW exact bounded case: Tile condition contracts: independent JSON governs physical colony work.
Build only as needed. Include actual independent predicates/native declaration reuse, rule alias identity,
pure accepted/refused placement and route previews, physical hauling/construction, interrupted saves,
paused single clock, twin continuation/replay/session isolation; early malformed references and scoped faults.
Do not run historical/family/full/Oil/Tycoon/platform suites.

## 8. Risks

TileMap.Version observes native writes, not arbitrary hostile reflection or unrelated world state.
The provider observation guard is scoped, not a CLR or whole-world sandbox.
Broad provider/concurrency/fuzz/soak/allocation/performance/global acceptance gates remain OPEN.

## 9. Out of scope

Remaining navigation/placement profile base hierarchies, business/Tycoon, platform/Draw, GameClock edits,
new transport media and all broader P1-P12 requirements. Pending G19 authority is unchanged.
Commit/push only after the complete goal and plan are achieved and verified.

## 10. Sub-task execution order

Inspect existing owners and tracker; document stage; replace base/linker and reuse native declaration;
validate all actual aliases in the catalog lifecycle; route both consumers through the shared owner;
add independent engine proof and one thin player case; compile and run that exact case; record evidence.

## 11. Status

Implemented original contract aliases/declaration/evaluation owners. Core2.370.0/save154/replay1.
Final ONE NEW exact case137/0/0 in6550ms after strengthening the actual-mutation fixture.
178 selected inputs frozen12:05:01UTC; no executable edits after final pass.
[Scoped evidence](../../docs/migration/P4_TILE_CONDITION_CONTRACTS_VERIFICATION.md).
Full P1-P12/provider/global gates OPEN; no publication.
