# P4-G56 — Authored construction and site bindings

## 0. Problem Statement

`Standard/Construction.cs` embeds construction JSON/plan/removal authoring in native ConstructibleDef, while reusable Site requires native SiteDef. IConstructionDefinition/IConstructionSiteDefinition exist, but independent file registrations cannot drive the same runtime without native authoring inheritance. Concrete construction service registration is also redundant with existing contracts.

## 1. Goals

1. Sole ConstructionDeclaration owns strict construction JSON, activity bindings, placement/template intent and removal-plan authoring.
2. Sole ConstructionSiteDeclaration owns strict target JSON; original Site selects IConstructionSiteDefinition and keeps its original order/state codec.
3. Native/direct registrations use the same graph, site/target aliases, materials, movement, construction/removal/recovery owners and clock.
4. Every loaded construction/site definition, unused included, validates actual bindings before Initialize. Selected site reads observe actual entity/provider codecs and clock.
5. Remove concrete service registration without compatibility aliases; prove one original construction/recovery owner and one new bounded ordinary-player workflow.

## 2. Design

Move existing ConstructibleDef algorithms into `Standard/ConstructionDeclarations.cs`; native registration delegates, never copies the declaration. Nested removal plan holds IConstructionDefinition instead of ConstructibleDef. ConstructionDeclaration returns its actual registered definition from BindExecution/ActivityDefinitions; graph is the actual authored activity graph, never recompiled. Native Constructible has no state and no generic native definition cast.

Add ConstructionState to existing IConstructionSiteDefinition. Site derives from Component, selects that contract and keeps its original order field/Sync/IConstructionSite/cancellation/retention/state/inspection roles. Shared binding admission checks frozen owner aliases, original reusable codec1, actual target/site/state/workflow graph and bills. Original construction service validates before Initialize; no second catalog, state cache or project coordinator. Runtime reads use existing EntityObservation protections; do not mask first callback errors.

## 3. API surface

Engine contracts and declarations only. ConstructionDeclaration.Read(JsonReader, ComponentDef), Resolve(ContentResolver) and existing construction/plan/template members. ConstructionSiteDeclaration.Read/Resolve, Target and ConstructionState. IConstructionSiteDefinition gains the authored ConstructionState. No host/platform/rendering API.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| New | Standard/ConstructionDeclarations.cs | 220 | medium |
| New | Standard/ConstructionBindings.cs | 100 | medium |
| Modify | Standard/Construction.cs | move/replace authoring | medium |
| Modify | Standard/ConstructionContracts.cs, ConstructionModule.cs | 15 | medium |
| New | tests/ExtensionProof/ConstructionContentContractsProof.cs | 100 | medium |
| New | tests/Beep.SimEngine.Tests/ConstructionContentContractTests.cs | 170 | medium |
| Modify | selected historical concrete service assertions | mechanical | low |

## 5. Error handling matrix

| Operation | Before | After |
|---|---|---|
| Independent site with reusable Site | native definition cast | actual contract binding |
| Unknown site/activity/state/target | native resolution refusal | same declaration refusal |
| Unused malformed direct binding | uneven native validation | original composition rejects before Initialize |
| Site getter changes entity/clock | native-only getter | selected codec/clock observation refuses |

## 6. Backward compatibility

Development only. No legacy, compatibility registration or parallel implementation. Native content syntax remains its normal declared file format, not a fallback path.

## 7. Verification

Build completed slice; run ONE NEW exact case only with outer30000ms/termination5000ms/output30000 characters and bounded physical stages. Repair/rerun that same case if needed. Historical/family/full/Oil/Tycoon/platform runtime remains unrun/open. Preserve failed attempts and source hashes.

## 8. Risks

Changing definition binding must retain actual graph and saved order identity. Site state must settle through the original entity Update; do not add a state system. Independent getters may throw or mutate during reads; preserve original first fault and forbid healthy checkpoint afterward. Scoped proof is not an arbitrary CLR sandbox.

## 9. Out of scope

New gameplay in drivers, second builders/schedulers/clocks, business/Tycoon/default changes, rendering/platforms, full regression or publication before the entire original goal/plan finishes. G19 protected resource-catalog authority remains pending.

## 10. Sub-task execution order

1. Move existing authoring into sole declarations; migrate original native and reusable bindings.
2. Validate loaded bindings and guard selected site observations; remove concrete service binding.
3. Add direct JSON registration/proof using original physical owners; focused build/exact case only.
4. Update evidence/master/audit while preserving original compound requirement text and checkboxes.

## Delivery — scoped, not aggregate completion

Completed this slice with [verification evidence](../../docs/migration/P4_CONSTRUCTION_CONTENT_CONTRACTS_VERIFICATION.md). Core2.403.0/save154/replay1/construction-view4. ONE NEW exact case FINAL158/0/0 in907ms; first failed attempt and three builds retained. Native/direct content shares original physical supply/work/removal/state owners; all loaded bindings validate before Initialize. Original compound/full gates and protected Tycoon scope remain OPEN. No executable edits after the final pass; no staging/commit/push.
