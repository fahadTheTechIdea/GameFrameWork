# P12-E6 — Physical disposal through original inventory work contracts

## 0. Problem

The initial E6 stage established physical timed disposal through shared contracts. D2c adds explicit ports, D2d logical wrapper/fault proof and D2e named incident cancellation/repeated episodes. Current D2f requalifies native/independent spill recovery/disposal/treatment and adds public saved-cargo interruption/demand/return without native changes. [Current design/evidence](P7_CONVERSION_ACCOUNTING_REVALIDATION_DESIGN.md). Do not introduce another disposal/recovery subsystem, native inheritance requirement or local-inventory fallback.

## 1. Goals

1. Extract IInventoryWorkEffectDefinition, one strict declaration and one shared lifecycle intent/admission/commit algorithm from the original InventoryEffectDef.
2. Whole-order disposition intent may wait for supply; productive start requires the complete real unpromised material bill through its authored ports. Earned completion alone commits the original transaction.
3. Author a disposal facility, four-minute machine activity, metric, objective and setup through JSON and the master manifest.
4. Prove incident spill -> recovery -> physical haul -> disposal on native/independent spatial storage, including a direct external effect definition.
5. Pause, disable, cancel and reload before completion without losing material; final remaining material plus recorded disposal must equal original supply.

## 2. Design

InventoryEffectDef remains a direct Def delegating metadata to InventoryWorkEffectDeclaration. IInventoryWorkEffectDefinition requires immutable Quantities, Kind and Ports plus the existing physical/intent contracts. Every resource has exactly one explicit JSON port mapping, even for local inventory. ResourcePortContracts alone owns shared route parsing/coverage, metadata/packing/capacity admission, actual store binding and grouping. InventoryWorkEffectContracts evaluates the whole selected outbound claim vector; earned completion acquires it atomically and delegates one classified ResourceConversionIntent to the original IResourceTransaction. The former effect-specific ResourceInventoryIntent helper/direct lifecycle mutation path is removed. PhysicalWorkEffectContracts remains the sole earned-work boundary. CheckIntent validates actual company/entity/port metadata without requiring future supply; CheckStart checks complete current readiness without acquiring anything.

No new disposal command, job, stock ledger, clock or scheduler. The authored activity has the effect on earned completion. Local InventoryPort aliases the existing inventory; selected/connected ports may alias independent actual stores. Hauling and disposition use those same stores/books. Missing bindings never substitute local inventory. An external direct definition delegates to the same declaration/contracts, no native subclass or compatibility facade. Current selection changes after acquisition are terminal provider violations, not permission for a substitute commit. External store wrappers may differ between resolutions of the same authoritative endpoint/book; CLR wrapper identity is not a port-selection rule. D2d verifies healthy wrapper parity and hostile acquired redirects/outages. Temporary pre-acquisition unavailability retries only under the selected JSON work policy, preserving earned work without acquiring a partial bill.

The new JSON scenario add-on authors an empty disposal station with physical dock slots, four-minute staffed activity, Destroyed-oil metric and objective after recovery. The test fixture only selects/enrolls content. The existing spill scenario supplies finite material and original response/recovery workflows. Destruction is an explicit game disposal sink, not a scientific claim that matter vanishes physically.

## 3. API/state

Original E6 contract extraction was engine2.598.0/save158/replay1. Current D2f remains2.717.0/save161/replay2. [Current proof](../../docs/migration/P7_CONVERSION_ACCOUNTING_REVALIDATION_VERIFICATION.md#current-d2f-m59-spill-cargo-interruption) distinguishes baseline physical spill/disposal/treatment from final saved cargo interruption. No native/layout/header/legacy or duplicate transaction change; scoped proof is not broad P12 completion.

## 4. Files

Standard/WorkEffects.cs and new inventory work contract/declaration file; original caller tests; public external definition proof; authored disposal documents and original fixture/physical scenario tests; current protocol, tracker and verification.

## 5. Errors

Invalid kinds/bills/aliases/capabilities or no positive-time machine ancestry refuse composition. Wrong target/company and insufficient unpromised material use original typed refusals. Cancellation never disposes stock. Only a successful original lifecycle commit records Destroyed facts.

## 6. Compatibility

Direct metadata contract migration, no legacy branch or forwarding wrapper.

## 7. Verification

Targeted compile; two exact physical disposal cases with60-second process watchdogs and bounded stages, plus focused existing inventory/environment contract regression as needed. No full/profile/family run.

## 8. Risks

Physical destruction is a game sink; waste chemistry, landfill/deposition capacity, emissions/byproducts, continuous/multi-cell leaks, arbitrary custom/private/fault matrices and global durability/performance remain broader gates.

## 9. Exclusions

Oil/Tycoon/platform/host/Draw changes, remote/instant disposal, separate material ledger, compatibility aliases, full regression or Git publication.

## 10. Status

- [x] Extract original inventory-effect metadata/execution into reusable contracts and migrate current concrete lookups.
- [x] Separate structural intent from material-ready activation; original physical boundary alone permits earned completion.
- [x] Master-listed disposal facility/activity/effects/metric/objective, including direct external effect metadata.
- [x] Native/independent physical supply, unearned refusal, pause/disable/cancel/restart, saved continuation, disposal accounting and replay.
- [x] Three final literal cases934 successful checks; one clean targeted compile. [Verification](../../docs/migration/P12_PHYSICAL_DISPOSAL_VERIFICATION.md).
- [ ] Waste chemistry/deposition/byproducts, continuous/multi-cell leaks, hostile/contended/private provider matrices and broad durability/performance; whole goal remains active.
