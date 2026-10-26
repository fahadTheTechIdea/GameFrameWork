# P7-GR1: Protected activity claim release and surviving cleanup — Design Document

## 0. Problem Statement

WorkBoard.ReleaseResources releases one receipt per call and ignores missing/invalid results. It does not guard other nodes or running-input stores whose receipt has already been spent. A release callback can change another store's free stock undetected. Whole-set release also needs explicit endpoint-loss semantics so a missing claim does not strand surviving tools.

## 1. Goals

1. Use a required immutable release intent describing company, actual release/participant receipts, protected endpoint references and Exact/Surviving policy.
2. Capture the complete set before callbacks, validate actual book records, release only valid surviving claims through the original owner and never clamp a mismatched existing record.
3. Have native node cleanup protect every resource endpoint/remaining receipt of its actual activity order and release its own set once.
4. Prove saved cancellation/completion, legitimate prior receipt retirement, private exact refusals and release mutations of held or unheld peer stores.

## 2. Design

ResourceClaimReleaseIntent carries detached reference sets; it owns no stock, ledger or persistent state. ReleaseClaims takes that one required intent; EvaluateClaimRelease shares its preflight. Every caller directly migrates. Exact refuses missing endpoints/tokens; Surviving skips only absent endpoints/tokens, not unauthorized or mismatched existing records. Invalid enum/null/duplicate records refuse before mutation.

Original CaptureClaimParticipants expands to capture explicit endpoint references, including unheld running inputs, and supports release inspection without requiring operational consumption readiness. A shared filter classifies actual missing records against those same snapshots. PrepareClaimRelease and FinishCapturedClaims retain complete captures through release; no separate cleanup implementation.

WorkBoard.ReleaseResources builds one own release set and the actual order's resource references/remaining participant receipts, calls Surviving once, then clears original resource bindings and costs. All budget rules and physical spent-input accounting remain unchanged. It does not recreate material or rewrite progress.

## 3. API surface

Required ResourceClaimReleaseIntent with ResourceClaimReleasePolicy.Exact/Surviving; no legacy signature/default overload. Matching pure EvaluateClaimRelease. Existing result type and native books remain. Build changes; save154/replay1 and production views unchanged. Proof command encodes policy/endpoints with its original receipt encoder.

## 4. Files to change

Core intent/interface, original ResourceTransactions and WorkResources; actual release callers; existing proof command codec; build/protocol; one new thin driver/profile using existing master-listed activity JSON. Stage/evidence/master/audit updated.

## 5. Error handling matrix

Exact missing references refuse; Surviving absent references skip without consuming stock. Existing token with wrong owner/resource/amount/direction refuses in both policies. Provider mutation faults with no valid save/hash/success history or compensation. Empty surviving set commits cleanup only; no fabricated promise.

## 6. Backward compatibility

Development only; all callers directly migrate. Remove unused old finish wrapper. No duplicate owner or compatibility shim.

## 7. Verification

One new bounded exact case after completed focused compile. Pending saved cancellation before/after productive work, completion and original books, prior legitimate receipt retirement before same-Update cancellation, typed missing endpoint reference and pure mismatches, unheld-peer mutations during cancel/completion. Failed attempts retained; full regression only at final implementation validation.

## 8. Risks

Other node receipts must remain protected after current node release. Running/spent resource endpoint identities remain relevant even when their own receipt is gone. Surviving must never excuse a live mismatched/foreign token. Node state and partial mutation can fault; no rollback claims.

## 9. Out of scope

Full physical endpoint-loss/worker/dock/rebinding/inbound/conversion/spill/topology/fault/global matrix, Tycoon/Oil/platform/Draw/clock changes and Git writes.

## 10. Sub-task execution order

1. Replace release contract and migrate actual callers.
2. Capture/filter/release under one original checkpoint; connect whole-order protection.
3. Prove lifecycle and negative providers via public commands/views.
4. Freeze evidence and update tracker, keeping compound gates open.

## Scoped stage outcome

Implemented and focused-runtime verified: FIRST/FINAL323/0/0, one clean7.80s focused build, no failed attempts. Required exact/surviving release intent, full order endpoint protection, saved cancellation/completion/prior actual receipt retirement, exact stock and six held/unheld peer release faults proven. Missing-reference policy is tested; full physical endpoint loss remains separate. Original compound gates and full regression stay open. See [verification evidence](../../docs/migration/P7_GRAPH_CLAIM_RELEASE_VERIFICATION.md).

