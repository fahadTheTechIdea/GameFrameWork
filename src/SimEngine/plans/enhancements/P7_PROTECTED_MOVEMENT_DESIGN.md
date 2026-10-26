# P7-PM1: Complete movement participant protection — Design Document

## 0. Problem Statement

Manufacturing startup now acquires tools, inputs and WIP inbound together, but CommitMovements rebuilds transfer checkpoints around only the moving endpoints. A reusable tool in another endpoint can therefore be mutated by a transfer callback without participating in the movement's exact checkpoint.

## 1. Goals

1. Require explicit participant receipts on a movement, including nonmoving startup holds, without another reservation ledger.
2. Validate actual owners/receipts and capture every participant before admission callbacks.
3. Retain those original endpoint checkpoints through acquisition and transfer callbacks; authorized movement deltas update existing expectations, unrelated participants must remain unchanged.
4. Prove normal protected movement/startup and detection of peer mutation during preflight/commit, including no successful checkpoint/history after a provider violation.

## 2. Design

Extend ResourceMovementIntent with required ParticipantClaims. These are actual receipt references, not balances. Duplicate references across movement roles/legs are allowed only when identical; duplicate entries within one participant set are invalid. Source/destination receipts still declare spending. Participants not spent are observed, not reacquired or released.

PrepareMovements captures and validates the full set. CommitMovements passes the same ephemeral ClaimEndpoint collection to the original transfer commit. Existing snapshot checks and exact movement deltas remain the sole protocol. StartWip passes its acquired input/tool/inbound set. No compensation or rollback fiction; an extension violating purity/commit faults the session.

## 3. API surface

Breaking constructor adds required IReadOnlyList<ResourceReservation> participantClaims. Callers and proof codec migrate directly. Private transfer preparation accepts captured endpoints; public transfer API remains unchanged. Build changes, physical save books/layout unchanged.

## 4. Files to change

Core intent, original movement/transfer/claim owner, native startup, existing constructor callers/proof codec, native identity/protocol; independent fault instrumentation and one public-player bounded case/profile using existing master-listed JSON. Stage/master evidence updated.

## 5. Error handling matrix

Null participant set is InvalidRequest. Duplicate/foreign/stale/invalid participant receipts are InvalidReceipt. Missing endpoints retain typed refusal. Honest refusals preserve books/stock. Mutating preflight/commit is a session fault with no success/save fiction.

## 6. Backward compatibility

Development only; all constructors migrate, no optional fallback or old wire path.

## 7. Verification

Completed implementation build then only the new bounded exact case, including explicitly authored negative callbacks. Record all failures/repairs. Full regression remains deferred until implementation finishes.

## 8. Risks

Shared input/tool receipt spending must retain its legitimate remainder; the same endpoint's expected snapshot advances only for actual transfers. Nonmoving tool endpoints must remain unchanged. Negative instrumentation must not be treated as a gameplay feature.

## 9. Out of scope

Broader graph/dock/provider/settlement/fault/stress/lockstep matrices and full P7/P8 remain open. No new state book, Tycoon/Oil/platform/Draw/clock work.

## 10. Sub-task execution order

1. Extend intent/codec and migrate callers.
2. Preserve complete endpoint capture through original movement/transfer commit.
3. Supply startup participants and verify normal/violating providers via public inputs.
4. Record scoped evidence, leaving original compound gates open.

## Scoped stage outcome

Implemented and focused-runtime verified: FINAL151/0/0, four clean focused builds; two failed same-case attempts and intermediate136/0 retained. Fresh/held movement, three-set saved/replayed commands, native shared/separate reusable tools and four peer-mutation violations proven. No original compound gate closure or full regression claim. See [verification evidence](../../docs/migration/P7_PROTECTED_MOVEMENT_VERIFICATION.md).
