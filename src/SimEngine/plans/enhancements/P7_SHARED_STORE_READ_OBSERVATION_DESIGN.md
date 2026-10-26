# P7-N2: Observe authoritative shared-store reads

Status: implemented/focused-verified on2.611.0/save159/replay1/inventory-view9.
[All attempts and limits](../../docs/migration/P7_SHARED_STORE_READ_OBSERVATION_VERIFICATION.md).

## Problem and acceptance

P7-N1 observes the returned endpoint facade and target entity, but a shared pool
is persisted by its IResourceEndpointProvider service, not by that facade.
A pure capacity or receipt callback must not mutate another pool behind an
unchanged selected endpoint version and publish healthy facts.

## Design

Extend InventoryView.ReadStore, the existing commitment projection boundary,
using EntityObservation and the registered endpoint providers' original codecs.
Capture before reading endpoint metadata and verify before returning detached
facts. No new registry, stock book, codec, transaction service or compatibility.
Keep the existing entity, space, clock and endpoint metadata checks.

Exercise the actual endpoint-provider Resolve contract in the existing external
PoolPort proof rather than bypassing it through a concrete Get. Add only negative
provider instrumentation for capacity/receipt reads that mutate a real sibling
pool. First deliver stock physically, save/load, then arm each negative callback.
Require fault, withheld outputs, no subsequent queries/save, unchanged detached
history and an unaffected independently advancing restored session.

## Verification and scope

Build the affected project, then run only the exact resource-port selection case
for reproduction and correction, plus the existing logistics-view durability case.
No full/profile/family execution, Oil/Tycoon/platform/Draw edits or Git publication.
Build identity advances for the engine behavior change; state/save/replay layouts
stay unchanged. P7.009 and the other original tracked gates remain open.

## Stages

- [x] N2.1 Reproduce sibling mutation behind stable endpoint metadata.
- [x] N2.2 Guard actual provider codecs at the existing view boundary.
- [x] N2.3 Focused verification, evidence and master tracker update.
