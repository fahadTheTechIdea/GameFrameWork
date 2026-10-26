# P8-F5: contract-based recipe metadata disclosure

## 0. Problem statement

TransformationManagementView.ReadRecipe/TransformationsOf and TransformerManager.CheckEnqueue currently check visibility only for the selected recipe. ReadExecutionPreview/resource-rate projections also disclose resource metadata, quantity/capacity units and entity output. A known recipe can therefore reveal hidden referenced content.

## 1. Goals

1. One pure engine-owned disclosure walk covers actual recipe, bills/rates, catalog resource aliases, quantity/capacity units and entity output.
2. Menus, recipe projections and ordinary queue admission share that walk and refuse hidden entries without publishing partial metadata.
3. Native and independent JSON recipes use the same path; no scheduler, stock, visibility, knowledge or clock duplication.
4. Real timed discovery earns visibility, followed by physical manufacturing, saved twins and replay.
5. Broad fog/queue-history/all-provider/full regression gates stay open.

## 2. Design

Extend the existing internal TransformationContracts partial with CanDisclose(World, PlayerId, ITransformationDefinition). Read each selected definition inside original EntityObservation/provider/clock guards. Resolve resource metadata from the one IResourceCatalog, not a new resource table. Temporary identity sets avoid repeated visibility reads within one projection only.

Route TransformationManagementView.ReadRecipe, TransformationsOf and TransformationEntityOutput plus TransformerManager.CheckEnqueue through it. Preserve private ownership refusal and original content-access/queue evaluator. Existing earned queue execution is not replaced by another discovery scheduler.

Test through existing ConstructionPlanTests.Points factory and explicit master-listed capacity/recipe/discovery files; select original CompanyKnowledgeModule and existing typed visibility declaration registrations. Learn knowledge through an authored timed ordinary activity/KnowledgeEffect, never direct ledger edits.

## 3. API surface

No new public capability or save payload. Metadata visibility changes engine behavior; bump build identity only. Hidden projection returns UnknownDef with empty/null facts; hidden menu omits whole entry. No redacted fabricated recipe.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| New | src/Beep.SimEngine/Standard/TransformationDisclosure.cs | medium |
| Modify | src/Beep.SimEngine/Standard/Transformations.cs | medium |
| Modify | src/Beep.SimEngine/Views/TransformationManagementView.cs | medium |
| Modify | src/Beep.SimEngine/Kernel/SaveGame.cs and actual protocol identity fixture | low |
| New | tests/Beep.SimEngine.Tests/RecipeDisclosureTests.cs | low |
| New | tests/ExtensionProof/Content/recipe-disclosure.json | low |
| Modify | existing test registration/resource listing/tracker/audit | low |

## 5. Error handling

Hidden/missing content never returns resource/output details or creates queue state. Original observation faults reject throwing/mutating visibility callbacks and checkpoints. No hidden-content fallback, synthetic zero bill or default unit.

## 6. Compatibility

Dev-only; no compatibility shim or parallel catalog. Existing explicit saves identify the changed build; structural schemas unchanged.

## 7. Verification

One solution compile, ONE NEW bounded exact case; same-case fixture repairs only. No old/family/full runtime without authorization. Physical waits bounded512 Updates with public progress/stock diagnostics. Freeze inputs after FINAL; documentation-only closure.

## 8. Risks

Do not confuse own executing queue history with public recipe metadata. Scope covers menus/new admission/metadata projections, not erasing earned physical state on knowledge loss. Every quantity/capacity unit returned by ResourceInfo must participate.

## 9. Out of scope

Protected G19/Tycoon catalog migration, Oil consumer, Draw/platform/adapters, full P8/P11 fog/provider/performance matrices. Full goal remains active; no commit/push before whole goal AND plan finish.

## 10. Execution order

1. Add shared contract-only disclosure walk and migrate actual consumers/build identity.
2. Add one authored timed knowledge/physical manufacturing proof, compile then exact runtime.
3. Record outcomes, update master status and source/input audit without broad gate claims.

## 11. Scoped result

Implemented the shared contract-only disclosure path and actual menu/view/admission consumers on2.435.0/save154/replay1. FINAL257/0/0 in6390ms, eight native/direct hidden-reference variants with minute/half-minute discovery/manufacturing/saves/pause/privacy/replay/isolation. Both failed same-case attempts retained; one clean solution8.47s/two clean targeted compiles4.50s/5.90s. No executable edits/runtime after FINAL.

Original221/176checked/45open unchanged; additional157. P8.015/P11.033/full goal/publication remain OPEN; no Tycoon/Oil/platform/Draw changes. [Evidence](../../docs/migration/P8_RECIPE_DISCLOSURE_VERIFICATION.md).

Documentation-only closure:1390 source/13 quickstart anchors,691 selected inputs with zero drift; all221 original literals/checkboxes unchanged.2642 paths/922 Markdown/8239 links, no broken targets or thin-driver boundary matches;18 valid master rows, diff check exit0/staged0. Protected clock/Tycoon hashes unchanged; no publication or global completion claim.

