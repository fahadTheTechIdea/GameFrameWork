# P7-C4a — Whole-set owned resource claims

Status: focused-verified on 2026-10-05, build2.48.0/save23/flow4. **218 checks / two exact suites**,
zero failures/aborts; zero build warnings/errors. See
[evidence](../../docs/migration/P7_RESOURCE_CLAIM_BATCH_VERIFICATION.md).
Prerequisite for P4-G2b2/G2c source/tool selection and acquisition, not completion
of those activity stages. Parent: P7_RESOURCES_DESIGN; current authority is ResourceTransactions.

## Problem and goals

The existing transaction owns exact two-ended transfer and whole local-inventory lifecycle operations,
but activity all/any resource/tool requirements need one atomic set of promises across actual endpoints.
Per-endpoint acquisition in a caller can partially reserve before a later endpoint refuses. Add the
whole-set protocol to IResourceTransaction/ResourceTransactions; all actual promises stay in each
endpoint's existing ResourceReservationBook. No new transaction service, claim ledger or stock store.

Success: pure canonical multi-endpoint/native-quantity checks; aggregate competing owners before any
promise; acquire all or none; release/consume exact owned receipt sets with whole-set preflight;
provider violations fault without fake compensation; saved scoped/entity endpoints and private actual
consumption history. Future activity nodes own only selected references/receipt links in their codec.

## API and ownership

- ResourceClaimIntent declares Owner, Endpoint and immutable native Bill. Company is explicit per set.
  Distinct owners may request the same endpoint/resource (e.g. tools and consumables); total quantities
  must fit current unpromised stock. Duplicate endpoint/owner/resource requests fail; no hidden merge.
- IResourceTransaction.EvaluateClaims/AcquireClaims checks/acquires the same normalized canonical set.
  ResourceClaimResult returns immutable actual ResourceReservation facts, never a provider or a second
  promise. Caller-retained pure checks are never accepted as mutation authority.
- EvaluateClaimReceipts/ReleaseClaims/ConsumeClaims resolve the existing endpoint owners and validate
  exact owner/token/resource/quantity/direction. Every leg preflights before any release/spending. Native
  units and capacity quantum remain authored catalog/policy authority. ConsumeClaims publishes the
  existing ResourceFlowFact only after the entire verified commit, not partially successful history.
- ResourceTransactions owns a transient checkpoint of declared endpoints' native stock/records/version
  for exact mutation verification. Checkpoints never persist, reserve or authorize later commits.
- ResourceFlowFact.Endpoint becomes ResourceEndpointId. Ordinary lifecycle facts convert their entity
  identity explicitly; owned-consumption facts carry their actual outbound token and scoped endpoint.
  Existing event/journal/view/Sync migrates to schema4, not a separate accounting event/feed. Old saves
  and flow payloads refuse; no compatibility reader.

## Files, stages and failure semantics

Core/ResourceTransactions.cs declares intents/results/contracts; Standard/ResourceTransactions.cs
implements the whole-set protocol beside existing transfer/lifecycle operations. ResourceFlows.cs,
InventoryModule.cs and source consumers migrate one shared fact codec/feature. Build/save break together.

Invalid/duplicate/empty/native-incompatible requests, foreign endpoints, unavailable stock, absent
endpoints and wrong receipts refuse before mutation. A provider that mutates a check, returns a fake
receipt, changes another balance/promise, or violates a promised exact commit throws and faults its
normal engine Update. No rollback/compensating inventory, partial-success result or fallback receipt.

Read contracts/owners → declare set API → pure normalization/admission → exact acquisition/release/
consumption → sole fact migration → two exact suites → evidence/parent/master/README/AGENTS.
Verification uses one multi-endpoint/native/ownership suite and one saved exact mutation/provider-fault
suite; not broad older resource families. Stage/full regression remains open unless explicitly requested.

Completed: pure complete-set admission, native stock and existing/new outbound-promise capacity checks,
actual canonical acquisition, exact release/consumption, scoped flow codec4 and breaking save23. The
existing endpoint proof adds only delegating fault instrumentation and public commands over that same
transaction. Saved pending commands, live receipts and consumed history, shuffled twins, unrelated
promises, late-set refusal and four normal-Update provider faults pass. No second persistent claim state.
The per-change gates are the two exact suites in the evidence report; broader gates remain open.

## Remaining scope

P7-C6 now extends this exact protocol to explicit mixed inbound/outbound claims and ordinary movement;
see the [stage](P7_BIDIRECTIONAL_CLAIMS_DESIGN.md) and [current evidence](../../docs/migration/P7_BIDIRECTIONAL_CLAIMS_VERIFICATION.md).
This document's original outbound gate is historical and was not rerun on2.283.0.

This provides the resource primitive, not activity source selection, tool lifecycle, continuous inputs,
money/worker integration, authored source/area definitions, physical delivery/extraction or full batch
transfer. P4-G2b2/G2c must still wire selected contracts/receipts into existing node/job/physical spending
and prove cancellation/loss/reselection. Broader replay/stress/soak/accounting gates remain required.
Only SimEngine and its engine-side proof change; no oil, Tycoon, host, demo, Draw or platform work.
