# P4-I10: Authored tile condition definitions — Design Document

Status: scoped implementation verified2026-10-08; full goal and phases remain ACTIVE/OPEN.
[Evidence](../../docs/migration/P4_TILE_CONDITION_DEFINITIONS_VERIFICATION.md).

- [x] Existing Def lifecycle, native evaluator and shared contract references replaced in place.
- [x] Actual JSON/master/scenario migration; old probes/identity fields compile-only.
- [x] External predicate content and ONE exact NEW case64/0/0 1332ms, first runtime.
- [x] Final build3.42s0/0 after one new enum-name test correction; source frozen.
- [ ] Broader/global provider/hostile-mod/replay/stress/full release acceptance.

## 0. Problem Statement

`Standard/TileLayerConditions.cs` currently exposes repeatable public Resolve on a shared
inline object outside Def's lifecycle. Routing and placement expose concrete band lists,
duplicate their water intent, and cannot reference extension-owned condition definitions.
This violates frozen shared content and contract/file-based gameplay authoring.

## 1. Goals

1. Stable condition IDs in master-owned JSON, resolved to one actual definition instance.
2. Native bands use existing Def authoring permissions, not a parallel lifecycle or registry.
3. Navigation and placement consume ITileConditionDefinition, with an external content proof.
4. Reject unknown/duplicate/foreign-space conditions and old inline objects before Initialize.
5. Prove physical travel, hauling and construction still use original owners and game time.

## 2. Design

Replace old inline class/interface with ITileConditionDefinition and abstract TileConditionDef.
The base owns explicit TileSpaceDef resolution and a nonvirtual Matches entry checking frozen
metadata, actual catalog/session/map/space and bounds. Protected read/resolve/validate/match
hooks support external providers. TileLayerBandConditionDef implements native sampling;
its layer/public visibility/native integral bounds are validated before content publication.
Content-only optional TileConditionsModule declares tile_layer_band_conditions. It owns no
service, state, scheduler, command or clock. Uniform/navigation-only games do not require it.

Existing rule entries parse nonempty condition ID arrays and resolve original catalog entries;
their public lists expose contracts. TileConditionReferences is the single internal reference
reader/linker, not a definition store. Parents invoke it only within their existing Def phases.
Duplicate IDs refuse; distinct compatible predicates may legitimately constrain the same layer.
References validate after all definitions resolve, independent of document order.

Colony's water exclusion is one shared definition referenced by land routing and construction.
Lane and native layered-route examples directly migrate to named bands. Master declarations,
selected scenario modules and available providers are explicit. No inferred module/default.

## 3. API surface

`ITileConditionDefinition : IContentDefinition` exposes TileSpaceDef Space and
`bool Matches(World world, TileMap map, TileCoord tile)`.
TileConditionDef provides protected ReadConditionCore/ResolveConditionCore/
ValidateConditionCore/MatchesCore hooks under the original Def lifecycle.
JSON rules use `conditions: ["stable.condition.id"]`; band definitions author
id, space, layer, minimum and maximum (explicit numbers or null).

## 4. Files to change

| Action | File group | Risk |
|---|---|---|
| Replace | Standard/TileLayerConditions.cs | medium |
| Modify | Standard/LayeredTileNavigation.cs, TileConstructionRules.cs | medium |
| Modify/Add | ExtensionProof content, ColonyGame, resource declarations | medium |
| Modify | old authored/native routing probes, strict build identity fixtures | compile-only |
| Add | external condition provider and one new bounded runtime case | medium |
| Modify | current tracker, README, AGENTS, phase/evidence documents | low |

## 5. Error handling matrix

| Operation | Result |
|---|---|
| Missing/inline/duplicate condition reference | ContentException before Initialize |
| Wrong space/private layer/non-integral native bound | ContentException before publication |
| Repeated Read/Resolve/Validate after publication | Original ContentAuthoring refusal |
| Unfrozen/foreign catalog/wrong actual map evaluation | InvalidOperationException |
| Out-of-bounds tile | false, no fabricated sample |

## 6. Backward compatibility

Dev only; old inline shape/class removed directly, no legacy readers or aliases. Build identity
advances to 2.278.0 for semantic changes. Save140 and original codec/runtime owners unchanged.

## 7. Verification

Build the canonical solution once compilation is ready. Run ONE distinct NEW bounded exact case:
`Authored tile conditions: frozen shared contracts and physical saved colony work`.
It covers lifecycle, reference validation, extension-defined predicates, JSON behavior, actual
normal-player admission/travel/delivery/build, partial saves/twins/pause/conservation/isolation.
At most2000 Updates per physical stage with immutable diagnostic facts on timeout.
All build/runtime attempts recorded; repair and rerun only observed same-case failures.
Old identity/diagnostic migrations compile-only. No old/family/full/platform/stale runtimes.
Freeze source/proof/tests/JSON after final green; docs-only evidence handoff afterward.

## 8. Risks

Public extension code can violate its hooks' purity; this phase is not hostile-mod sandboxing.
Shared Def freeze protects lifecycle, not arbitrary reflection or external mutable provider fields.
Full P2/P4/P6/replay/stress/other plan requirements remain open until independently proven.

## 9. Out of scope

Oil/sibling/host/demo/Draw/platform code, Tycoon business implementation, full-suite runtime,
new clocks/state/save owners, compatibility, publishing before full goal and plan completion.

## 10. Sub-task execution order

1. Replace condition authoring and migrate existing consumers in place.
2. Migrate native files/master declarations/scenario selection and old probes directly.
3. Add external-provider proof and one bounded public-player/lifecycle test.
4. Build/run exact case, repair only observed failures, freeze after green.
5. Update authoritative tracker/evidence/checks, keep full goal active and publication deferred.
