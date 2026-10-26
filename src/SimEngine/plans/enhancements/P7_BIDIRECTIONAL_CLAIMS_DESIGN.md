# P7-C6: Complete bidirectional resource commitments

Status: focused-verified and source frozen, 2026-10-08. Parent P7/P8/P6 and full P1–P12 remain open.

## 0. Problem Statement

Core/ResourceTransactions.cs ResourceClaimIntent assumes outbound stock. Standard/ResourceTransactions.cs
PrepareClaims/AcquireClaims/ReleaseClaims cannot reserve or release a complete mixed inbound/outbound
set. ResourceTransferBatches.CommitMovement manually acquires destination promises after source claims.
P7 explicitly requires whole source/destination admission before promises; endpoint books stay authoritative.

## 1. Goals

1. Explicit required direction on the existing intent, availability and transient leg; no old overload.
2. Evaluate/acquire arbitrary mixed native endpoint sets through the single existing protocol.
3. Preflight aggregate source stock and destination capacity, including different resources/owners,
   existing promises and current stock; an outbound reservation never creates inbound free space.
4. Exact current mixed receipt evaluation/release; inbound receipts can never be consumed or extended
   as outbound stock. Normal refusal creates no partial claims, stock, IDs, history or version changes.
5. Original movement uses the same complete acquisition path; transfer/physical timing remains unchanged.
6. One new bounded case covers native/scoped/64-bit endpoints, save/replay, refusal/fault boundaries
   and actual public-player timed loaded hauling/construction.

## 2. Design

ResourceClaimIntent(ReservationDirection,ReservationOwner,ResourceEndpointId,Bill) replaces the old
signature. PrepareClaims copies the set, resolves each current endpoint once, distinguishes inbound
and outbound native checks and aggregates both vectors before any acquisition. Duplicate tuples
include direction; the same owner/resource may legally hold one receipt on each side.
ClaimEndpoint remains an ephemeral exact checkpoint; actual books alone own promises.
Inbound availability is the provider's bounded Offer up to required quantity, not a fabricated scalar
total over incompatible units. Outbound availability remains actual unpromised stock.

AcquireClaims dispatches native ReserveIn/ReserveOut and verifies the whole set after every mutation.
PrepareClaimReceipts validates actual direction; release dispatches ReleaseIn/ReleaseOut. Consumption,
extension, existing source-only conversion/recovery/tool callers explicitly restrict outbound receipts.
No refund of spent material or incoming capacity borrowed from future outgoing movement.

PrepareMovement shares mixed-set admission and CommitMovement calls AcquireClaims once for all fresh
source/inlet claims (or just inlet claims when exact source receipts already exist), then existing
CommitTransfers. Remove its direct destination ReserveIn loop rather than add another coordinator.
All other consumers explicitly declare outbound intent. Diagnostic ClaimProbe.Sync includes direction;
strict Build2.283.0/save141 rejects old bytes. Endpoint/book/service layouts and the sole clock unchanged.

## 3. API surface

Required Direction on existing ResourceClaimIntent and ResourceClaimAvailability; no new transaction
service/API family. EvaluateClaims/AcquireClaims/EvaluateClaimReceipts/ReleaseClaims support mixed sets.
ConsumeClaims and ExtendClaim refuse inbound requests. Existing IOutletPort remains IInletPort.

## 4. Files

| Action | File | Risk |
| --- | --- | --- |
| Modify | Core/ResourceTransactions.cs, Standard/ResourceTransactions.cs, ResourceTransferBatches.cs | high |
| Modify | ResourceConversions.cs and explicit native/proof claim consumers | medium |
| Modify | Kernel/SaveGame.cs, old current identity fixtures, ClaimProbe.Sync | mechanical/medium |
| New | tests/Beep.SimEngine.Tests/BidirectionalClaimTests.cs | medium |
| Modify | Program.cs, tracker/P7/P8/P6/README/AGENTS/help/evidence | low |

## 5. Error handling

Malformed direction/duplicate/native quantity/owner: InvalidRequest. Existing owner-side receipt:
InvalidReceipt. Foreign/missing endpoint: original refusal. Whole vector shortage: NoStock/NoCapacity.
Invalid provider callback, fake/missing receipt, unrelated mutation or absent version advance faults
normal Update; no compensation, silent fallback or saved partial state.

## 6. Compatibility

None. Old constructor deleted; all current consumers migrate directly. Diagnostic pending-command
bytes change, so save141/build2.283.0 are explicit. No legacy reader.

## 7. Verification

Build once after the slice, then ONE distinct NEW exact bounded case:
Bidirectional resource claims: atomic mixed commitments and physical saved continuation.
Only observed same-case failures may be repaired/re-run. Every attempt recorded; freeze source/
tests/proof/JSON after final green, then documentation-only inventory/hash/link/diff checks.
Older consumer/identity migrations compile-only; no old/family/full/platform runtime.

## 8. Risks

All-leg preflight is deterministic native admission, not rollback of dishonest extension mutations.
Optional capacity providers still own authored vector semantics. Resource promises do not replace
worker/dock/team acquisition; broader joint acquisition and fault/global-purity/stress gates remain.

## 9. Out of scope

Extra stock/reservation authority, physical schedulers/transport media, Tycoon/Oil/host/demo/Draw/
platform changes, broad regression/allocation/soak acceptance or publication before full completion.

## 10. Execution

- [x] C6.1 Replace existing intent/admission/acquisition/receipt direction handling.
- [x] C6.2 Unify original movement and migrate all consumers/codecs.
- [x] C6.3 Add/run the one new bounded protocol and physical-player proof; record every attempt.
- [x] C6.4 Freeze green source; update tracker/parents/evidence/scoped inventory/help.
- [ ] Full P7/P8/P6/P1–P12 acceptance and final authorized commit/push.

Open decisions: none within original whole-commitment requirement.
