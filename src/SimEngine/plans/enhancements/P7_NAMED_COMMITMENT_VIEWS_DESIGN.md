# P7-N1: Actual named endpoint commitment views

Status: implemented/focused-verified on2.610.0/save159/replay1/inventory-view9.
Four exact finals1132/0;60old calls/28test files explicitly migrated.
[API/all attempts/limits](../../docs/migration/P7_NAMED_COMMITMENT_VIEWS_VERIFICATION.md).

## Problem and acceptance

InventoryView capacity/reservation queries currently inspect only an entity's local
IInletPort. Named resource ports can bind service-owned stock with physical inbound
claims, but the same public queries cannot inspect those actual books. Hosts must
not recreate commitments or guess a local stock fallback.

## Design

Change the EXISTING TryGetCapacity/TryGetReservations signatures to require Code
port and accept an optional original ResourceEndpointId after the output. Code.None
means the actual local inlet ONLY; named IDs resolve via ResourcePortContracts.Store.
An unavailable original never substitutes the current selection or local stock.
Migrate all repository callers explicitly; no old signature or compatibility shim.

Keep InventoryView as sole projection owner. One private resolver admits entity
sight/ownership and actual store ownership (or explicit observer); a public neutral
pool is not authority to expose another company's private book through an owned
proxy. One private read boundary observes actual endpoint identity/version/company/
policy plus existing provider/entity/space scopes. Clone returned reservation
records and retain existing immutable CapacityCheck. No new inventory/claim book.

Extend the existing resource-port selection case: real partial delivery, old endpoint
claims, current selection, unavailable explicit-original refusal, retained immutable
records, restored exact claims, foreign refusal and final cleanup across native/
point and30/60second games. Preserve existing physical selection/recovery/replay.
Add elapsed deadlines to its1000-update stages. Repair its historical save154 pin
using the independently authored current protocol expectation.

Build2.610.0/inventory-view9; save159/replay1/state codecs unchanged. No business,
Oil, renderer/platform/Draw or host gameplay changes. Existing local receiving-only
ports remain supported; every outlet remains an inlet.

## Verification

Current original selection literal baseline, targeted project build after migration,
then only the changed selection, inventory visibility, bidirectional physical ports
and logistics-view durability literals. Explicitly report compile-only API migration
coverage versus those runtime cases. No full/profile/family execution or Git writes.
P7.009 and broader private-content/transaction/stress gates remain open.

## Stages

- [x] N1.1 Inspect original endpoint query/selection contracts and baseline.
- [x] N1.2 Extend sole projection and migrate explicit callers without legacy overloads.
- [x] N1.3 Verify real named receipts/capacity and saved/private/fault behavior; tracker.
