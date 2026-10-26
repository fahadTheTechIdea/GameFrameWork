# P4-G64: contract-based logistics profile content

## 0. Problem statement

`Standard/LogisticsPolicies.cs` requires native LogisticsPolicyDef in stock initialization, entity-port bindings and validation. `ExtensionProof/ContractOutletPolicy.cs` repeats that dependency. Independent JSON metadata therefore cannot supply the same physical stock bands and outlet policy through a contract alone.

## 1. Goals

Replace those native profile dependencies with ILogisticsPolicyDefinition. One shared strict LogisticsPolicyDeclaration owns authoring/resolution and actual catalog validation; native registration becomes an ordinary implementation. Both native and independent profiles must control the original timed hauling, finite reserves, receiving maxima and destination permissions. Invalid unused metadata must fail before Initialize. Saves, replay, private facts and interleaved sessions use the same existing owners.

## 2. Design

Add a metadata contract exposing Stock, Outlet and States, no live balance/clock/demand state. Move the original parser/resolver to one shared declaration. Native and independent definitions delegate to it, with no inheritance or copied parser. Validate actual catalog identity, resources, exact quantities, ordered unique stock entries, destinations and state aliases. Original LogisticsService composition admission covers every loaded profile, including unused content, through existing observation guards. Existing entity-port capacity checks and stock initialization consume the same contract with explicit actual content; original stock rows, reservation receipts, dispatch, hauling and policy codecs stay authoritative.

## 3. API surface

ILogisticsPolicyDefinition : IContentDefinition. LogisticsPolicyDeclaration.Read/Resolve/Validate; LogisticsPolicyRules.InitializeStock and ValidateProfile now accept the contract. ValidateProfile receives explicit ResolvedContent, removing native profile.Content dependence. No compatibility overload, native-required mod base or duplicate catalog.

## 4. Files to change

Production: Standard/LogisticsPolicyContent.cs (new shared declaration/contract), LogisticsPolicies.cs, Logistics.cs, Kernel/SaveGame.cs. Non-friend proof: ContractOutletPolicy.cs and new independent profile/module. Add one new exact thin player driver and explicit JSON content registration; update current protocol identity and evidence/tracker.

## 5. Error handling

Missing/unknown/invalid fields, quantities, resources, destinations, state aliases and malformed unused profiles fail admission before Initialize. Preserve actual first provider error and original fault semantics, never fabricate a profile or empty stock. Physical workflows fail within bounded Update stages with public diagnostic snapshots.

## 6. Backward compatibility

Dev only; no compatibility shim or legacy path. Bump engine identity for changed admission/contract semantics; payload layout remains unchanged unless observed implementation requires a layout change.

## 7. Verification

ONE NEW exact bounded case only: native and direct metadata on original physical point-space/stock/work/haul owners, reserve and maximum limits, blocked destination/area/worker-reserve waits then normal policy release, truthful timed delivery, saved partials/pause/replay/isolation and pre-initialize negatives. Build solution once; targeted same-case repairs only. Historical/family/full/Oil/Tycoon/platform runtimes remain unauthorized/open. This scoped proof cannot close original broad P2/P4/P9 requirements by itself.

## 8. Risks

Preserve named-port identity and actual capacity validation. Definition read/resolution is not a live stock owner. Do not weaken unused-profile admission to satisfy historical invalid fixture content. Point geometry uses authored non-crossing routes; no teleport/occupancy fallback.

## 9. Out of scope

Protected G19 files, new economy behavior, rendering/adapters/Oil, replacement schedulers/stock/reservation/clock owners, global CLR purity/performance claims, full regression or publication before full goal/plan completion.

## 10. Execution order

1. Inspect native policy bindings and original physical consumers.
2. Move parser/validation to the shared contract declaration; migrate all in-scope native callers.
3. Register independent authored content and drive original physical policy waits/release through normal commands.
4. Compile/run only the new bounded case, repair observed defects, then refresh actual tracker/source/metadata evidence.

Status: scoped implementation verified: ONE NEW exact FINAL919/0/0 in1729ms, PID35996 exited0/1873ms, outer60000. Four compiles/final4.24s and three same-case attempts retained. Core2.424.0/save154/replay1, no payload/layout change. Original221/160checked/61open unchanged; broad P2/P4/P9 and full P1-P12 OPEN. See [evidence](../../docs/migration/P4_LOGISTICS_POLICY_CONTENT_VERIFICATION.md). No staging/commit/push.
