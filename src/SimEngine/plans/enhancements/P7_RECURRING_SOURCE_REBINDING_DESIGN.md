# P7-ER4: Recurring source ownership through replacement

Current M69 selected recurrence178/0 on unchanged2.723/save161/replay2 preserves
native/direct schedules, current source, historical provenance and physical saved
continuation. No new implementation or rebuild. [Current finite owner evidence](../../docs/migration/P7_ENTITY_REFERENCE_REBINDING_VERIFICATION.md#current-m69-finite-native-reference-owner-qualification--2026-10-22).
The initial implementation and version-specific results below are historical;
broader original P7 accounting/stress and full goal remain open.

## 0. Problem

RecurringMembers equates historical PlanOrigin.Entity with the live source. Replacing a source copies its schedule but disconnects LastPlan/LastCompletedPlan from actual cycle history. Rewriting historical origins would fabricate provenance.

## 1. Goals

Preserve posting origin, plan IDs, progress and chronology; store one current binding on the existing cycle head; retain source settings and schedule through physical replacement; refuse incompatible capabilities; prove native/direct timed saved continuation.

## 2. Design

PlanEntry stores SourceOwner on recurring heads only. PlanOrigin remains historical. Original posting, source-member lookup and own-service dispatch use the head binding. PlanView exposes detached CurrentSource and gates it through existing private projection.

WorkBoard's existing replacement callback validates named recurring metadata and original state retention, stages pending arguments through their existing contracts, verifies actual retained schedules, and rebinds heads. No parallel lineage ledger, schedule, queue or historical rewrite.

## 3. API/state

PlanView.CurrentSource; unchanged commands/reference-owner contract. Build2.561.0/save157/replay1;WorkBoard codec53;work view104. No old save reader.

## 4. Files

WorkPlans/WorkRecurringPlans/WorkPlanReferenceReplacement, Upgrades retention check, PlanContracts, WorkModule, SaveGame/protocol, focused public recurrence proof and tracker/evidence.

## 5. Errors

Missing/differing source kind/codec or cadence/trigger/dispatch/template shape refuses before mutation. Incorrect retained schedules fault replacement. Active physical execution remains a blocker; no teleport/free completion.

## 6. Compatibility

Development-only layout migration; no legacy API/save facade.

## 7. Verification

New exact native/direct source case with earned completion, policy edits, staffed upgrades, history/current bindings, future recurrence, refusals and saved twins; affected pending-plan case; compile. No full regression.

## 8. Risks

Current ownership and historical provenance must not become copies. Live binding exists once on the actual head; members retain original origin/target/arguments. Broader template/corruption/provider/performance matrices remain required.

## 9. Out of scope

Active-order/job migration, unrelated endpoints/incidents/domain owners, Tycoon/business/platform/host changes, full tests and Git writes. Full original plan unchanged.

## 10. Execution

Separate head binding from provenance; validate/rebind retained sources; prove timed saved workflows/refusals; record evidence/master audit and leave broader gates open.

Status: implemented and scoped verified. Recurrence178/pending-plan187/current-protocol92:457/0 final focused checks;solution1.46s clean. Native/direct completed and paused-other-site cycles preserve policy, real completion deadlines, posting history and saved future work. Source-referenced execution, template/provider/fault/corruption/private/performance breadth and all original open gates remain required. See [evidence](../../docs/migration/P7_RECURRING_SOURCE_REBINDING_VERIFICATION.md).
