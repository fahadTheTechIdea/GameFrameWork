# P7-C5a / P8-A2a — One owned native transfer-set protocol

Status: focused-verified on 2026-10-05, build2.76.0/save46: **285 checks/two exact suites**
(166 native;119 durability), zero failures/aborts; clean build. See
[evidence](../../docs/migration/P7_RESOURCE_TRANSFER_BATCH_VERIFICATION.md).
This is the resource prerequisite for recoverable transformer
WIP, not completion of WIP or P8. Parent designs: [P7](P7_RESOURCES_DESIGN.md),
[P8](P8_TRANSFORMATIONS_DESIGN.md); the [master](MASTER_TRACKER.md) remains partial.

## Evidence and scope

The current IResourceTransaction moves one owned source/destination receipt at a time. Calling that
API repeatedly can commit an early input while a later input refuses, and publish partial batch
history. Recoverable process input movement must not be implemented as such a loop in a transformer,
host or mod. Existing ResourceInventoryState/ResourceReservationBook endpoints remain the only
stock/promise authorities. ResourceTransactions remains the only protocol owner.

This slice extends IResourceTransaction with EvaluateTransfers and CommitTransfers over detached
ResourceTransferIntent requests. Single-leg Evaluate/Commit use that same implementation. There is
no second transaction service, batch queue, stock ledger, persistent codec or legacy reader.

## Admission and ordering

1. Copy the request set before invoking providers; reject null/empty sets, malformed legs and
   duplicate source/destination receipt tokens before mutation.
2. Multi-leg requests contain only physical Stock transfers. Authored-creation/payment-bearing
   Supply remains a single-leg operation until aggregate supplier payment admission is implemented;
   never claim individually affordable purchases are an affordable batch.
3. Canonical ordering is source scope/identity, destination scope/identity, resource identity and
   receipt sequence. Caller ordering cannot change authoritative mutation/event order.
4. Resolve each endpoint once per call through existing declared IResourceEndpointProvider services.
   Entity, non-entity and positive 64-bit endpoint identities retain their original geometry/units.
5. Check actual company access, native quantities/common quantum, exact owned receipt records,
   stock and whole-inventory protected capacity for every leg before the first mutation. Existing
   owned inbound promises already reserve aggregate destination capacity. Do not acquire them here.
6. One ephemeral ClaimEndpoint checkpoint per endpoint verifies the complete vector/book/metadata/
   version across the whole set. An intermediate endpoint can receive and send, but outbound stock
   must already be protected at admission; incoming stock is never borrowed to authorize a leg.

## Commit and fault boundary

Commit always performs fresh whole-set admission. Each mutation reduces the actual owned receipt
by its explicit native quantity, preserving unspent remainders and unrelated promises. Validate all
participating endpoints after every source/destination mutation. Publish existing ResourceFlowFact
events only after the entire set verifies. Return a committed leg count, never add incompatible
native units into a fictitious total. Ordinary refusal moves no stock, releases/acquires no claims,
publishes no history and changes no token sequences or versions.

Provider contract violations fault normal Game.Update; do not compensate, fabricate rollback or
save/hash a partial tick. This is deterministic all-leg admission, not ACID rollback of malicious
provider mutations. No timing, travel or player authorization is added: physical work/haul/transform
owners remain responsible for calling this engine protocol only after their actual execution gates.

## Verification gate (exactly two suite names)

- `Resource transfer batches: whole-set admission, native ownership and canonical commits`
- `Resource transfer batches: saved continuation, shared endpoints and provider fault boundaries`

Cover mixed discrete/fluid quantities, partial receipts, aggregate protected capacity, missing and
foreign endpoints, invalid/duplicate receipts, stock shortage, stale previews, canonical shuffled
order, shared intermediate endpoints, single-leg parity, no partial history, conservation, private
feeds, interleaved saved twins and normal Update provider faults. Bounded tests only. Build the
standalone solution; earlier suites are compiled, not executed. No family/full suite run.

## Remaining work, deliberately not represented as complete

- Complete inbound/outbound claim-set acquisition is now focused-verified in [P7-C6](P7_BIDIRECTIONAL_CLAIMS_DESIGN.md) through the original owner; broader joint participant acquisition remains open.
- Real transformer-owned WIP endpoint, stable execution identity and its sole provider codec.
- Authored cancellation disposition, recoverable WIP and catalyst/tool retention.
- Atomic resource conversion/entity creation and mixed process ports.
- Aggregate paid/creation supply batches, overlapping same-token fragments and generalized exchange.
- Replay/stress/provider breadth and full framework acceptance.

No Oil/Tycoon/old sibling, host/demo/harness gameplay, platform adapter or Draw implementation.
