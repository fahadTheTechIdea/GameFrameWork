# P7-PR1: Guarded cancellation return and participant release — Design Document

## 0. Problem Statement

Transformer.ReturnWip currently transfers protected input to original stores using empty participant sets, then releases tools in a separate transaction. A return inlet or tool-release callback can alter an otherwise nonmoving tool/store without sharing the complete return checkpoint. Successful transfer facts are published before release finishes.

## 1. Goals

1. Declare retain/release intent explicitly for movement participants through contracts, with no implicit compatibility default.
2. Keep the original complete endpoint checkpoint through return transfer and surviving participant release.
3. Publish movement success facts only after all requested releases succeed.
4. Prove saved blocked cancellation, original-source return, reusable-tool release and provider violations using a thin public-player case.

## 2. Design

ResourceMovementIntent gains required ResourceMovementParticipantDisposition (Retain/Release). PrepareMovements validates the enum and rejects conflicting dispositions for the same actual token across legs. Participants can alias spending receipts; only their actual surviving remainders are released after transfers. Identical release references across legs are deduplicated.

CommitMovements passes the release set into the existing CommitCapturedTransfers. Its original exact transfer loop updates expected stock/receipts, then FinishCapturedClaims validates and releases remaining participant records using that same ephemeral endpoint collection, before success facts publish. Public claim/transfer methods retain one implementation; no new service, codec/book or rollback.

ReturnWip passes actual tool receipts and Release to every original-input return leg, with original WIP source claims. It no longer separately releases tools after a material return. Inputless cancellation retains original complete tool release path. Caller migration is direct; native startup and ordinary movement explicitly Retain.

## 3. API surface

Required participant disposition on the movement constructor and shared proof-command encoder. Invalid enum or conflicting token dispositions refuse before callbacks. No optional overload, old wire path or parallel lifecycle coordinator. Build changes; existing physical save state/layout unchanged.

## 4. Files to change

Core movement contract; original ResourceTransactions/ResourceTransferBatches and native ReturnWip; actual constructor callers and proof codec; independent negative provider instrumentation; current identity/protocol; one new driver/profile entry; stage/evidence/master/audit. Existing master-listed input-ports JSON supplies resources, capacity and timed recipes.

## 5. Error handling matrix

Normal capacity/compatibility/access/missing endpoint refusal retains WIP, tool promises and execution for retry. Invalid enum/conflicting release declarations refuse purely. Provider mutation/refusal after acquired movement faults without compensation, resumable save, valid hash or successful history observation. Success facts defer until release completes.

## 6. Backward compatibility

Development only; migrate all callers directly. No legacy or compatibility paths.

## 7. Verification

Completed focused build then only the new bounded exact cancellation case; save/load pending and blocked returns, rerouted current ports but retained originals, exact material/tool accounting and provider mutations during receive/release. Record failures and repairs. Full regression only at final implementation completion.

## 8. Risks

Shared input/tool source must regain input while its tool receipt remains protected, then release exactly once. Multiple legs must not double release a shared token. Dishonest providers may partially mutate; detection must not claim rollback. Cancellation progress and full-vessel wait must remain engine-owned.

## 9. Out of scope

No full graph/dock/inbound/provider/stress/global closure, quality/lot selection, Tycoon/Oil/platform/Draw/clock changes or Git writes.

## 10. Sub-task execution order

1. Extend explicit contract and migrate constructor/command callers.
2. Retain original checkpoints through remaining release and defer facts.
3. Connect native cancellation return; prove player/negative-provider flows.
4. Freeze focused evidence and update tracker without narrowing original gates.

## Scoped stage outcome

Implemented and focused-runtime verified: FIRST/FINAL243/0/0 across both clock variants; one fixture-property compile failure retained and repair clean. Complete original material return plus reusable participant release, shared token deduplication, pending/saved full-vessel cancellation, actual history/accounting and receive/release peer faults proven. Original compound gates and full regression remain open. See [verification evidence](../../docs/migration/P7_RETURN_PARTICIPANTS_VERIFICATION.md).

