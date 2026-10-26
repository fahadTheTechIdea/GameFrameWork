# P4-G60 — resource-flow journal content contract

## 0. Problem

ResourceFlowsModule.Compose selects Content.All<ResourceFlowJournalDef> even though IResourceFlowJournalDefinition exists. A direct JSON definition implementing the contract cannot configure the sole journal. This contradicts the framework's contract/file-owned extension boundary. ResourceFlows.cs currently owns the strict native reader and committed history, not another stock balance.

## 1. Goals and design

Extend IResourceFlowJournalDefinition with IContentDefinition. Introduce one ResourceFlowJournalDeclaration for explicit positive retained_records_per_company, reused by native and direct definitions. Compose selects actual frozen contract aliases, exactly one, and observes identity/capacity callbacks through original EntityObservation before initializing any system or granting stock. Capture admitted capacity once into the existing ResourceFlowJournal constructor; do not read unvalidated callbacks later or create another journal/ledger/state owner.

## 2. API and files

ResourceFlows.cs: contract, declaration, Compose admission and constructor capacity argument. Native ResourceFlowJournalDef remains ordinary JSON registration, not a required mod base. Direct non-friend definition delegates the shared reader. New authored addon is explicitly added to the existing point master through PointGame authoring; no duplicate factory. Behavior identity2.419.0, save154/replay1/journal6/view6 unchanged because fields/codecs do not change. No compatibility reader.

## 3. Verification

ONE NEW exact bounded case compares native/direct JSON policies with ordinary hauling into a destination: actual stock/cargo, retained immutable history, cumulative totals despite retention, privacy, pending/partial/terminal saved twins, replay and session isolation. Negative metadata checks cover strict missing/zero/negative/unknown input, invalid direct capacity/identity, missing or mixed policies before Initialize. Reuse original work/logistics/transactions/clock; driver only player commands and public views. Per physical stage bounded diagnostic waits. Build completed slice, same-case repairs only. No old/family/full/Oil/Tycoon/platform runtime.

## 4. Scope and tracking

This narrows remaining P2.003/P4.003 contract work but does not prove their whole requirement. Preserve all221 original words/checkboxes159checked62open. Broad provider faults/CLR purity/all resource lifecycle/quality/flow-view durability/performance/lockstep/full P1–P12 remain OPEN. Protected G19 and sole clock untouched. No staging/commit/push before full goal AND plan finish.
