# P7-WI1: WIP inbound participation in manufacturing startup — Design Document

## 0. Problem Statement

Existing WorkBoard/jobs own tools, workers and docks; ResourceTransactions owns resource promises. Transformer.StartWip checks WIP capacity separately before acquiring input/tool claims. ResourceMovementIntent can reuse source receipts only, so it cannot consume an inbound receipt acquired together with the other startup participants.

## 1. Goals

1. Require an explicit destination-receipt collection on every movement intent; no compatibility constructor or implicit null substitution.
2. Validate source/destination receipts, aggregate spending, ownership and direction in the original complete-set transaction owner.
3. Acquire WIP inbound capacity together with input/tool outbound promises, then consume that actual receipt during movement instead of reserving twice.
4. Prove partial/full held movements, competing/stale/foreign/null receipt refusals, durable remainder, replay and physical manufacturing startup without another ledger.

## 2. Design

Extend the existing immutable ResourceMovementIntent with DestinationClaims. Empty collections explicitly request acquisition; nonempty collections name actual owned receipts. PrepareMovements uses one aligned direction loop for both sides and validates all held receipts before mutation. CommitMovements uses held destination tokens when supplied.

StartWip adds an inbound WIP claim to its existing startup set, retains actual source and destination receipts, and passes them to the original movement batch. Existing input protection, tool books, source bindings, execution state and timing remain authoritative. No new participant service or rollback logic.

## 3. API surface

Breaking movement constructor adds required IReadOnlyList<ResourceReservation> destinationClaims. Existing callers and proof command codec migrate directly. State books/layout remain unchanged; build identity changes. Proof codec serializes both actual receipt sets for command/replay durability.

## 4. Files to change

Core movement contract; ResourceTransactions batch; Transformer.StartWip; all constructor callers; proof command codec; current protocol/build; one public-player case and JSON fixture/profile. Stage evidence/master tracker updated.

## 5. Error handling matrix

Null collections are InvalidRequest. Wrong direction/owner/endpoint, stale receipts or aggregate overdraw are InvalidReceipt. Full WIP refuses before input/tool acquisition with existing WIP-full blocker. Honest refusals preserve stocks/books/sequences. Provider violations still fault; no fabricated compensation.

## 6. Backward compatibility

Development only; every constructor caller migrates, no overload, shim or old wire path.

## 7. Verification

Build after completed migration, then only the new exact bounded case. Preserve failures and repair evidence. Full regression remains deferred until implementation finishes.

## 8. Risks

One held token may serve several legs, but total use must not exceed its actual remaining amount. Shared input/tool receipts must retain unused tool quantity. WIP startup must not reserve inbound capacity twice or leave transient claims after movement.

## 9. Out of scope

Standalone tool callback mutation during later movement, arbitrary graph/dock/fault/stress matrices and full P7.009 are not closed by this slice. Existing job/dock books remain owners. No Tycoon/Oil/platform/Draw/clock changes.

## 10. Sub-task execution order

1. Extend immutable intent and migrate all callers/codec.
2. Add destination-receipt admission/commit to existing movement owner.
3. Include inbound WIP in startup claims and verify ordinary saved physical execution.
4. Record scoped evidence without closing broader original gates.

## Stage outcome

Scoped inbound participation delivered: FINAL161passed/0failed/0aborted; original221/193checked/28open current criteria retained,additional256. [Evidence](../../docs/migration/P7_WIP_INBOUND_CLAIMS_VERIFICATION.md). Initial metadata1917source anchors/1096selected inputs/13quickstart anchors,0hash mismatch/drift/broken links. All221current literal/status/line checks pass. Existing early content validation correctly rejects authored WIP too small for a complete recipe; no runtime fallback was introduced. All12Tycoon sources and sole clock unchanged; full participation/provider/global gates remain open.
