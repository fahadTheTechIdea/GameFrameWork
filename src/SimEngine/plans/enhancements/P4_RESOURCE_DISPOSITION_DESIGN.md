# P4-G57 — Contract-authored physical resource disposition

## 0. Problem Statement

`ConstructionModule.Compose` selects `ResourceRecoveryDef` directly and `ConstructionService.ValidateRecoveryDefinition` requires that native type, despite the existing `IResourceRecoveryDefinition` contract. Independent master-listed JSON profiles cannot use the original physical stock-disposition owner. Only the selected profile is currently checked for empty compatible container metadata.

## 1. Goals

1. Select actual catalog-owned recovery metadata through its contract, with no native inheritance requirement.
2. Use one strict `ResourceRecoveryDeclaration` for native and independent JSON authoring.
3. Reject every loaded malformed profile before Initialize, including unused profiles; preserve explicit null selection.
4. Prove supplied cancellation and earned demolition yield use the original inventory, packing, transactions, logistics, work and clock owners, with conserved stock and saved/replayed continuation.

## 2. Design

`IResourceRecoveryDefinition` extends existing `IContentDefinition`, supplying its actual stable catalog identity. `ResourceRecoveryDeclaration.Read/Resolve/Validate` owns the existing required space/container file schema and resolved container admission. Validation runs after all references resolve, not during linking. The container must be the actual catalog entity, with matching spatial metadata and empty compatible inventory. Runtime composition additionally verifies frozen profile identity and selected geometry through the original construction/recovery owner. No parallel catalog, recovery service, ledger, state codec or scheduler is introduced.

Native `ResourceRecoveryDef` delegates the declaration. Independent proof registration derives only from `Def` and the contract, and delegates the same declaration. Construction selects `Get<IResourceRecoveryDefinition>` and validates all loaded profiles before Initialize. Only the selected profile must fit the selected runtime space; other valid authored spaces are not silently converted or forbidden.

## 3. API surface

Engine-only `ResourceRecoveryDeclaration.Read(JsonReader)`, `Resolve(ContentResolver)`, `Validate(ContentResolver)` and immutable `Space`/`Container`. Existing `IResourceRecoveryDefinition` gains inherited stable content identity. No host, renderer, money or platform API.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| New | Standard/ResourceRecoveryDeclaration.cs | 70 | medium |
| Modify | Standard/ResourceRecoveryDef.cs, Capabilities.cs | 15 | low |
| Modify | Standard/ConstructionModule.cs, ConstructionRecovery.cs | 25 | medium |
| New | tests/ExtensionProof/RecoveryContainerProof.cs and content/recovery-container-contracts.json | 70 | low |
| New | tests/Beep.SimEngine.Tests/RecoveryContainerContractTests.cs | 220 | medium |
| Modify | proof resource registration, test registration, current build protocol | 5 | low |

## 5. Error handling matrix

| Input | Before | After |
|---|---|---|
| Independent registered recovery profile | native type required | actual contract identity admitted |
| Missing space/container or extra file field | native reader refusal | sole strict declaration refusal |
| Unused incompatible container | not selected, incomplete validation | composition refuses before Initialize |
| Explicit null profile | no fabricated recovery | unchanged typed missing-recovery behavior |
| Stocked site cancellation | original actual disposition | same original receipts and physical hauling |

## 6. Backward compatibility

Development only; no legacy or compatibility path. Existing native JSON is ordinary authoring, not a fallback. Saved state codecs remain unchanged unless actual persistent state changes.

## 7. Verification

Build the completed slice, then ONE NEW exact bounded ordinary-player case only. Outer30000ms, owned termination5000ms, output30000 characters; each physical stage is update-bounded. Repair/rerun that same case if needed. Preserve failures and final source hashes. Do not run old, family, full, Oil, Tycoon or platform suites. Original aggregate gates remain open.

## 8. Risks

Whole-stock preflight and quantum packing must stay in the original owner. Cancellation must wait for actual delivered materials before testing disposition. Retained immutable facts must not be live entity handles. Scoped tests do not prove arbitrary provider purity, contention, allocation budgets or full regression.

## 9. Out of scope

Protected G19 resource-catalog/Tycoon migration, arbitrary custom geometry implementations, new gameplay in drivers, compatibility, adapters, rendering, new clocks/schedulers and publication before the full goal and original plan finish.

## 10. Sub-task execution order

1. Extract sole authoring and resolved metadata validation; migrate native selection to contracts.
2. Add explicit independent master-listed profile and thin ordinary-player physical proof.
3. Build once for the completed slice; run the one new bounded case, repairing only that case as necessary.
4. Record evidence and update the master/audit without closing original compound gates.

## P4-G57 delivery — scoped contract-authored resource disposition

[Stage](P4_RESOURCE_DISPOSITION_DESIGN.md), [evidence](../../docs/migration/P4_RESOURCE_DISPOSITION_VERIFICATION.md). Core2.405.0/save154/replay1/construction-view4. Existing recovery contract carries stable catalog identity; native/direct JSON delegates sole ResourceRecoveryDeclaration. Construction selects actual contract aliases; all loaded profile metadata validates after linking and before Initialize, unused included. Original geometry/packing/resource receipts/work/logistics/clock owners remain. ONE NEW exact FINAL125/0/0 in1696ms (PID18008 Exited0/1844ms observed), outer30000ms; failed first fixture attempt and four builds retained. Supplied site cancellation produces two local real containers, physically hauled return conserves20, earned construction consumes8 and timed demolition yields6 once, later hauling conserves18. Native/direct chronology, private pure recovery views, clock pause, saved twins/replay/isolation, eighteen malformed loaded native/direct bindings and explicit null selection pass narrowly.466 inputs frozen2026-10-09 22:33:40UTC,12 changed/new, no post-pass executable edits. Claims/mixed lots/arbitrary providers/all lifecycle/congestion/performance/global/full P1-P12 gates remain OPEN; G19 authority pending. Original requirement text/checks unchanged; no staging/commit/push.
