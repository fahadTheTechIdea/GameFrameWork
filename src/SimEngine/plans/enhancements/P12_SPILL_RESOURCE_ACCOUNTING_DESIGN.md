# P12-E3: Spill resources, physical recovery and treatment

> P7-SS1/build2.661.0/save160: actual prepared-spill site predicates are now fully observed before movement, including private target codec and clock mutation. [Stage](P7_SPILL_SITE_OBSERVATION_DESIGN.md), [baseline292/20 and final scoped evidence](../../docs/migration/P7_SPILL_SITE_OBSERVATION_VERIFICATION.md). Existing native and independent physical accounting cases each pass322 assertions separately; no resource/incident owner replacement or rollback fiction. Broader incident/environment/history/disposal/stress and full-plan acceptance remain OPEN.

## 0. Problem

Terrain contamination levels are persistent but not material stock. Existing SpatialExtractionEffectDef already uses actual cell/inventory movement; adding a separate spill balance/transfer implementation would duplicate that responsibility. Extend this original owner bidirectionally, remove extraction-only metadata and prove finite fluid accounting through recovery and processing.

## 1. Goals

1. Use one contract/declaration/algorithm for authored cell -> entity withdrawal and entity -> cell deposition.
2. Incident exposure deposits existing finite material through original transactions; no resource generation/loss or terrain reset.
3. Recover spill material through positive-time machine work, then haul it and convert it through the original processor.
4. Preserve resource reservations, capacity, ownership, actual cell location, journal/progress/save/replay and exact raw+treated material totals.
5. Refuse missing/invalid/incompatible transfer metadata and insufficient spill stock before changing a target episode.

## 2. Design

Rename the extraction-only definition/section to SpatialResourceTransferEffectDef / spatial_resource_transfer_effects and introduce ISpatialResourceTransferEffectDefinition. Mandatory operation withdraw/deposit replaces a hidden fixed direction; migrate every existing caller/document with explicit withdraw. No compatibility reader or alias.

One strict declaration resolves cell/bill/direction; one common contract owner delegates complete-set admission/commit to IResourceTransaction. Physical effects use existing earned-work admission. Deposit exposure uses existing prepared publication and exact original incident receipt, then fresh atomic movement checks. Actual cell CheckSite prevents remote transfer. SpatialResourcesModule retains its original endpoint resolver and validates every loaded transfer effect.

The original SpatialResourceEndpoint now accepts inbound material under its authored capacity policy, linked resource and actual layer representation. Its original ResourceReservationBook owns both directions. Incoming promises never create outgoing stock; outgoing promises never free incoming room. Raw layer changes and owner changes protect outstanding receipts. StageSync validates both directions against candidate stock before publishing a restored book. No separate spill inventory is introduced.

ISpatialResourceQuantity<TLocation> adds CheckDeposit/PrepareDeposit/Deposit alongside withdrawal. Shared SpatialResourceChange/Status/Commit replace withdrawal-specific result names without aliases. Native tile, independent tile and point owners prepare both directions through their existing mutation/publication paths. SpatialResourceValues calculates exact native/stored conversion, compatible quantum and numeric storage headroom without imposing an arbitrary world-size limit.

One JSON scenario add-on authors liquid material, resource-quantity spill layer/cell, deposit/recover effects, six-minute recovery activity, counted-fluid handling, treatment recipe/facility and setup. Existing original response cleanup cannot erase the quantity channel. Recovery and hauling take time; treatment converts three raw units into three treated units with the existing queue/conversion owner.

## 3. API/state

Direct replacement of extraction-only type/section and withdrawal-only result names; new transfer contract/declaration, explicit direction and exposure binding. Existing resource/spatial/work payload layouts unchanged; original embedded cell book now supports both existing reservation directions. Engine2.595.0/save157/replay1. No legacy shim or extra balance.

## 4. Files

Original SpatialResources.cs/module and Kernel/SpatialResourceValues.cs; native Map/TileMap.cs and original independent tile/point quantity owners; all four extraction JSON documents and in-scope C# callers; protocol expectation; authored spill document and existing fixture composition; focused spill/recovery/treatment and incoming-receipt proofs; tracker/evidence.

## 5. Errors

Missing actual endpoints, insufficient unreserved stock, incompatible native quantity/capacity, wrong owner/site or moving source retain existing typed refusals. Fresh commit disagreement faults instead of producing a successful prefix. Environmental terrain reset remains prohibited on resource-quantity data.

## 6. Compatibility

Extraction-only identity/reader is removed. All current documents explicitly declare withdraw; no default direction or forwarding type.

## 7. Verification

Targeted compile and six literal cases: native/independent spill workflows, insufficient-stock/full-cell exposure, incoming receipts, existing extraction ownership/provenance and single-use preparation. Every process has a30-second watchdog and5-second termination deadline; physical stages are bounded. No full/profile/family runs.

## 8. Risks

Mixed lots/quality, multiple spill cells, evaporation/chemical transport, environmental metric/objective integration, disposal by destruction and arbitrary provider/private/fault/global matrices remain broader gates. Counted liquid units are authored game quantities, not unverified scientific volumes.

## 9. Exclusions

Oil/Tycoon/platform/host/Draw rules, material generation/deletion fallback, second spill store/scheduler/clock and Git publication.

## 10. Status

- [x] Replace extraction-only content contract and migrate current framework documents, no compatibility reader.
- [x] Enable bounded incoming reservations/deposits through original cell/layer/book owners.
- [x] Prove authored delayed spill -> physical recovery -> hauling -> treatment on native and independent tile storage.
- [x] Prove refused exposure, incoming-receipt durability/capacity/ownership and retained extraction/preparation behavior within the six literal gates.
- [x] Record all attempts in [verification](../../docs/migration/P12_SPILL_RESOURCE_ACCOUNTING_VERIFICATION.md) and update master checkpoint.
- [ ] Broader mixed-fluid, multi-cell, disposal, environmental metric/objective, custom-effect/private/fault/global matrices; full original plan/goal remains active.
