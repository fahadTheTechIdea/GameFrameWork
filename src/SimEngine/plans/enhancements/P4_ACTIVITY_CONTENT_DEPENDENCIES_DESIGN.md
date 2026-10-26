# P4-CD1: Required activity-provider content dependencies — Design Document

## 0. Problem Statement

The original activity catalog now traverses graph aliases, but a requirement or effect can internally refer to a resource, unit, state, technology, region, signal, network, prototype or knowledge subject without exposing that reference. Private metadata can escape through a visible provider. Contract-based modules must declare their actual content references; the scheduler must not learn concrete provider types.

## 1. Goals

1. Require explicit immutable content dependencies on every work requirement and effect; no default implementation or compatibility fallback.
2. Migrate existing native and independent providers using their existing JSON-resolved bindings, not repeated authored dependency lists.
3. Traverse those references through the original catalog and existing observation guard, rejecting missing/foreign/mutable/duplicate aliases before gameplay.
4. Prove native and independent internal-resource/subject privacy through normal player intent, physical execution, forgetting, saves and replay.

## 2. Design

Add small `IContentDependencyDefinition` beside `IContentDefinition`. `IWorkRequirement` and `IWorkEffect` require it. `WorkRequirementDef` declares its property abstract. Empty references are explicit only for providers with no content bindings. The original `ResourceRequirementDeclaration` exposes its resolved resource aliases; `Bill` derives references from the existing catalog. Group declarations expose their actual requirement entries. Other providers expose their existing resolved typed references.

`WorkBoard.WalkDisclosure` reads dependencies through `EntityObservation.ReadProvider`; transitive references are checked with the same immutable-alias rules. Shared references terminate through visited identities; duplicate entries within a provider declaration are rejected. Prior-activity requirements traverse their existing stable plan binding through the actual captured activity map. No permission cache, execution book, completion system or extra clock.

Tycoon `WorkCostRequirementDef` receives an explicit empty dependency declaration only; its budget/category/amount/evaluation behavior is unchanged. Existing providers are revised directly, with no legacy path.

## 3. API surface

Required `IReadOnlyList<IContentDefinition> ContentDependencies` on `IContentDependencyDefinition`. Required for work requirements/effects. Build identity changes; save/replay/physical state layout remains unchanged.

## 4. Files to change

Original content contracts/bill, work contracts/catalog, native and independent provider declarations, engine identity/protocol, authored proof JSON/registration/profile and one public-player verification driver. Master tracker and stage evidence updated after the focused gate.

## 5. Error handling matrix

Null dependency sets and nonactual/mutable/service aliases fail composition. Empty is valid only as an explicit no-binding declaration. Hidden transitive content withholds the whole activity via existing typed refusals. Physical execution uses original eligibility/resource/effect owners; metadata traversal never evaluates requirements or applies effects.

## 6. Backward compatibility

Development only; no compatibility layer, default implementation or parallel model.

## 7. Verification

Compile the completed migration, then run only the new exact bounded case. Retain all failure and repair evidence. Profile-info is metadata only. The single authorized final full regression remains deferred until implementation completes.

## 8. Risks

Every provider must expose actual resolved references, not guessed IDs or duplicate authoring. Negative proof providers must still compile without relaxing their intended validation. This migration is broader source evidence than one runtime case; unexecuted providers remain explicitly unverified.

## 9. Out of scope

No Tycoon business rule change, Oil/platform/Draw/clock change or broad test execution. Performance, hostile callbacks, every unrelated workflow and full acceptance are not inferred from this stage.

## 10. Sub-task execution order

1. Introduce required dependency contract and migrate native bindings.
2. Migrate independent implementations and the one mechanical Tycoon declaration.
3. Extend original catalog traversal and prove authored transitive dependencies.
4. Record exact evidence and update tracker without closing unproven compound gates.

## Stage outcome

Required provider migration and scoped runtime proof delivered: FINAL863passed/0failed/0aborted; original221/193checked/28open unchanged,additional254. [Evidence](../../docs/migration/P4_ACTIVITY_CONTENT_DEPENDENCIES_VERIFICATION.md). Initial metadata:1907source anchors/1087selected inputs/13quickstart anchors,0hash mismatch/drift/broken links; all original221gate literals and line anchors retained. Sole Tycoon property-only migration reconstructs previous full source bytes; other11Tycoon files and clock unchanged. No aggregate gate/goal closure or final full regression claim.
