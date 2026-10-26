# P4-G30 — Contract-owned reusable storage bindings

## 0. Problem

Storage still inherits Component<StorageDef>, despite its inventory contract. Native filter/initial
stock configuration cannot bind the original runtime from an independent definition. InventoryView
capacity/reservation/prototype reads do not observe actual providers before publishing facts.

## 1. Goals

1. Original Storage accepts independent JSON metadata, keeping the same single stock/promise owner.
2. StorageDeclaration is the sole native/generated storage authoring/filter/resolution implementation.
3. Paired IStorageDefinition/IStorage validate actual session catalog, owned capacity and initial bill
   for all loaded definitions before Initialize, including unused and invalid capability pairs.
4. Original public inventory views guard provider reads and publish immutable facts only afterward.
5. Prove physical cargo/manufacturing and no initial-stock regrant through public commands/saves.

## 2. Design and ownership

Standard/Capabilities.cs adds IStorageDefinition : IInventoryDefinition, exposing actual frozen
ResolvedContent, capacity/initial/filter contracts and Public/Evacuate configuration. Content is not
a World or mutable game state. IStorage : IInventory identifies the reusable configured stock owner;
other inventory providers remain on IInventory and are not forced into the native metadata layout.

StorageDeclaration moves the original fields/reader/resolver/Accepts implementation out of StorageDef.
Native JSON and generated StorageDef.Make delegate this same declaration. Original public/evacuate
optional-field semantics are unchanged; no new defaults or compatibility reader. Accepted resource/
category filters now reject empty, duplicate and empty-category selectors, just as phases reject
empty/duplicate entries. Capacity and initial-stock validation retain original exact native semantics.
Accepts reads IResourceDefinition, without changing the pending catalog return-type migration.

Storage becomes Component consuming IStorageDefinition, with its SAME ResourceInventoryState.
Endpoint/company/version/initialization/removal/stock/promises/receipts/Sync/inspection remain original.
No new balance, resource registry, reservation book, stock cache, initial grant path or coordinator.

Original ResourceTransactions.ValidateComposition calls StorageDeclaration.ValidateDefinitions
before initialization. Paired runtime/metadata checks are contract-based. Actual metadata reads
are observed through original EntityObservation, and catalog/capacity/initial-bill ownership,
compatibility, quantum and complete capacity are validated, including unused definitions.

InventoryView.TryGetCapacity observes actual port codec/entity/clock, rejects unknown status and
assigns output only after verification. TryGetReservations observes/copies its actual receipt book
before output publication. StoreAccepts observes the authored provider's prototype compatibility.
CapacityCheck already deeply detaches its immutable scalar usage; no second projection model.

## 3. API and state

IStorageDefinition: actual Content, Public, Evacuate plus existing inventory metadata.
IStorage: reusable inventory capability. StorageDeclaration.Read/Make/Resolve/Accepts.
Core2.366.0/save154/replay1; native storage codec4 and inventory view schema5 unchanged.
Every IOutletPort remains IInletPort. No legacy inheritance, compatibility alias or duplicate stock.

## 4. Files

Capabilities.cs, Inventory.cs, ResourceTransactions.cs, InventoryView.cs.
Independent proof metadata binds original Storage; malformed pair and fault-injection diagnostics only.
New StorageBindingTests reuses original recipe/content/player helpers; Program/current protocol;
stage/evidence, master/checkpoint documents and completion audit.

## 5. Errors

Missing/wrong-kind capacity, invalid filters, incompatible/overflow initial stock, missing actual
catalog/policy/bill and wrong reusable capability pairing refuse before Initialize.
Actual capacity metadata exceptions, hidden original stock-codec mutation, version/clock/identity
mutation fault the original observation before output. No healthy retry/hash/save or rollback claim.

## 6. Compatibility

Development-only, no old reader or alternate loader. Generated buffers still use StorageDef.Make,
which delegates the same declaration and authored policy. Existing specialized inventory providers
are not migrated into another stock owner.

## 7. Verification

ONE NEW bounded exact case:
Storage bindings: independent metadata retains one physical inventory and pure views.

Native and independent cargo/factory/stocked prototypes use the original Storage. Authored initial5
is created exactly once through original transactions. Real loading, input3/tool1 delivery, timed
manufacturing, actual carrier receipts, saved cargo twins, sole-clock pause, replay, retained/private
capacity facts, no regrant on load and untouched interleaved sessions are verified.
27 malformed native/independent JSON/contract/pair declarations refuse before Initialize.
Five capacity-view faults include hidden native stock mutation without Touch through the original
codec while preserving the original receipt suffix. These are diagnostics, not another stock model.

Status: narrowly verified. Clean7.65s build, zero warnings/errors. New exact FIRST/FINAL131/0/0 in2437ms.
[Actual evidence](../../docs/migration/P4_STORAGE_BINDINGS_VERIFICATION.md).

## 8. Open risks

This is not full inventory/transaction validation. All providers/codecs, arbitrary cross-provider
hidden mutation, whole-world/CLR sandbox, service/remote endpoints, generated-buffer runtime breadth,
lot/quality, cancel/remove/replace/recovery/network/accounting stress, lockstep/concurrency/fuzz/soak/
allocation/performance and full P1-P12 acceptance remain OPEN. Prototype observation has no live-stock
codec scope; no cross-provider protection is claimed.

## 9. Out of scope

Pending excluded Tycoon/catalog migration authority, money/business defaults, domain/platform/Draw,
historical/family/full/Oil/Tycoon/platform runtime and staging/commit/push.

## 10. Order

Read original storage/state/initialization/views → sole authoring declaration and paired contracts →
migrate original runtime and composition validator → guard original views →
new thin JSON/player/fault proof → focused compilation and one new bounded case →
actual evidence/tracker/audit. Full goal remains OPEN.
