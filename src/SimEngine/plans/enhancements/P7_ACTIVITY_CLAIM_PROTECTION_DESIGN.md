# P7-GP1: Protected activity productive claims — Design Document

## 0. Problem Statement

WorkBoard.AdvanceWork separately validates retained tool/material receipts, acquires running inputs, and consumes each node's bill. ResourceTransactions only captures the endpoint set named by each isolated call. A provider can change nonmoving peer stock while leaving its retained receipt valid. Later receipt checks recapture the already-changed stock.

## 1. Goals

1. Require explicit participant receipts in claim evaluation/acquisition/consumption contracts.
2. Capture all request/receipt/participant endpoints before admission callbacks and check them after every authorized mutation using original ClaimEndpoint snapshots.
3. Make the existing productive frontier pass its actual retained commitments throughout running acquisition and per-node consumption; remove spent records from the transient retained set only after verified consumption.
4. Prove honest resource waits, saved physical activity quanta and dishonest peer mutation without host gameplay.

## 2. Design

Extend IResourceTransaction.EvaluateClaims, AcquireClaims and ConsumeClaims with required participant receipt sets. Directly migrate every caller; empty means explicitly no additional participant. Public methods prepare a complete ephemeral endpoint capture, validate actual participant records through PrepareClaimReceipts, then delegate original PrepareClaims/AcquireCapturedClaims/FinishCapturedClaims. Participants can alias consumed records but cannot duplicate within their own list. No new transaction owner, ledger, scheduler, state layout, rollback or implicit compatibility overload.

WorkStart passes retained to running evaluation/acquisition. Each consumption passes allRetained, then removes exactly consumed tokens before the next node. Existing budget checks/spend order, progress/spent-state publication and resource choice mapping remain unchanged. Graph cancellation/release and nonresource worker/dock participation remain separate pending scope.

## 3. API surface

Required IReadOnlyList<ResourceReservation> participants on the three claim methods. Null/duplicate/malformed/stale actual references refuse purely; honest shortage retains typed refusal. Mutating provider callbacks fault through existing engine boundary, with no resumable partial save/hash/success observation. Build changes; physical books/save154/replay1 remain.

EvaluateClaimConsumption is the matching pure consumption contract; its PrepareClaimConsumption is shared with commit. Existing proof command carries participants through the original receipt encoder. No test/harness claim evaluator is introduced.

## 4. Files to change

Core claim interface; original ResourceTransactions and WorkStart; all actual compiled callers/proof command encoder; build/protocol; one new thin case/profile plus authored scoped activity requirement fixture under the existing master. Plans/master/evidence updated.

## 5. Error handling matrix

Invalid participant set refuses before stock mutation. Missing/unauthorized endpoint remains typed. Capacity/stock shortages cannot consume tool/input prefixes or earn work. Provider mutation faults; no compensation. One completed node's consumed records never masquerade as valid participants of later consumption.

## 6. Backward compatibility

Development only; direct migration, no legacy API, shim or duplicate resource model.

## 7. Verification

One new bounded exact case after completed focused compilation; direct held/acquisition/consumption and engine-owned timed activities, actual private books, saved twins/replay, peer faults and shortage waits. Record repairs. Full regression only after full implementation.

## 8. Risks

Running and held obligations have different actual owners; participant identity validation must not require all tokens to share one owner. Public claim availability must retain native shape and existing refusal semantics. Retained receipts may include the current consumed token once; duplicate aliases across roles are legitimate, not duplicate promises.

## 9. Out of scope

Complete graph/dock/release/inbound/fault/global closure, Tycoon rules/Oil/platform/Draw/clock and Git writes. No new quality/lot subsystem.

## 10. Sub-task execution order

1. Extend explicit contracts and original checkpoint capture; migrate actual callers.
2. Connect complete retained sets in native productive frontier.
3. Prove contract and authored activity flows through public player commands/views.
4. Freeze evidence and update tracker; original compound gates stay open.

## Scoped stage outcome

Implemented and focused-runtime verified: FINAL183/0/0, four clean focused builds and three failed same-case attempts retained. Complete actual retained/request/spending endpoint capture, native parallel timed resource shortage/refill/save/pause/accounting/replay and four running acquisition/consumption peer faults proven. Full graph/dock/release/inbound/global original gates remain open. See [verification evidence](../../docs/migration/P7_ACTIVITY_CLAIM_PROTECTION_VERIFICATION.md).

