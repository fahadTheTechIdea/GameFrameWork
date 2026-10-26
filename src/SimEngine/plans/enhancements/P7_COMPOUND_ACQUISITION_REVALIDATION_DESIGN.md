# P7-C11: Revalidate compound acquisition participation

Status: six selected participant cases1672/0 plus affected alert-authoring case77/0
on unchanged2.611.0/save159. Original P7.009 remains OPEN: the separately registered
paid-source proof still requires explicit optional-business fixture migration.
[Full evidence, every attempt and limits](../../docs/migration/P7_COMPOUND_ACQUISITION_REVALIDATION_VERIFICATION.md).

## Requirement

Reconcile original P7 acceptance009: complete tool/dock/graph/inbound/physical-batch
participation and broader transaction semantics. Do not create another coordinator
or replace the existing WorkBoard/ResourceTransactions owners with a team ledger.

## Source audit

WorkAcquisition.PrepareNativeAdmission checks the entire actual job/worker/dock
set and timed nodes, then WorkResources.AcquireResources canonicalizes actual
order/node identity and preflights all resource/budget requirements before any
acquisition. CommitResourceAcquisition acquires native claims once and binds exact
returned receipts. Only then does Assign commit actual worker/bay/job relations.
Normal refusal is mutation-free; extension commit violations fault, not rollback.

ResourceTransferBatches.PrepareMovements is the same owner for single/multiple
physical legs. Selected source/destination and nonmoving participant receipts are
validated together; preserved tool claims must not be spent with feedstock.

## Verification

Inspect and run exactly these existing cases on the current engine:

- Native team acquisition: complete commitments, refusal purity and physical saved continuation
- Parallel acquisition: authored all-any roles tools and materials reserve whole contended sets
- Protected movement: complete participants survive acquisition and shared-tool startup or fault peer mutation
- Resource transfer batches: saved continuation, shared endpoints and provider fault boundaries

These distinguish direct protocol instrumentation from public player-command
physical acceptance. Original acceptance009 is not closed merely by finding these
names: inspect actual assertions and results, repair real regressions, and record
which participants and failure semantics are proven. Other P7 disposition/incident/
topology/quality and arbitrary-provider/stress requirements remain independent.

Add elapsed bounds to any changed physical wait helper, preserving update-count
bounds and diagnostic snapshots. No full/profile/family execution or publication.
No Oil/Tycoon/host/platform/Draw changes. Optional-budget assertions in the existing
native team case exercise unchanged optional contracts, not a Tycoon suite.

## Stages

- [x] C11.1 Inspect original requirement and actual participant ownership paths.
- [x] C11.2 Execute literal cases and repair only observed relevant defects.
- [x] C11.3 Record source/runtime coverage; retain original gate pending missing breadth.
