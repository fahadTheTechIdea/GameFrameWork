# P8-D3a — Authored batch catalyst disposition through actual reusable receipts

Current M70 adds actual physical supply/blocked WIP return/saved catalyst wear proof
through the existing native processor and inventory/transport contracts:1012/0/one
replay on unchanged2.723/save161/replay2. [Current accounting evidence and limits](../../docs/migration/P7_CONVERSION_ACCOUNTING_REVALIDATION_VERIFICATION.md#current-m70-classified-accounting-and-physical-wip-return--2026-10-22).
The initial D3a design/status below is historical; no new catalyst or material owner
was introduced, and older instant transaction-probe fixtures are not real-haul proof.

Status: focused slice implemented/verified; NOT full P8. Core2.310.0/save147/replay1/transformer8/
transformation-view14/operations-view11. One clean --no-restore build10.36s0 warnings/errors;
ONE NEW bounded exact case FIRST186/0/0 in463ms, childPID20296 exited0, observed620ms, untruncated.
Frozen18:38:04 UTC; sixteen JSON files/28 completion Bills migrated. Older cases compile only.
[Observed evidence](../../docs/migration/P8_CATALYST_DISPOSITION_VERIFICATION.md).

## Problem and success criteria

At the initial D3a baseline, Standard/Transformations.Deliver consumed raw WIP and
released every reusable receipt in full. The former conversion intent separated whole
consumed/released records and could not yet describe bounded wear of one retained
catalyst receipt. The implemented explicit receipt disposition replaces that baseline.

1. Require explicit tool_consumption JSON Bill for each recipe, {} for fully reusable tools.
2. A batch catalyst is an explicitly held reusable resource with authored positive completion
   consumption no greater than its tools quantity; it uses the existing tool_ports and ToolSources.
3. One real conversion input declares its original full receipt, bounded consumed quantity and
   whether to release the remainder. The transaction validates the actual full record once.
4. Full output/result admission precedes every raw-input/catalyst mutation; partial successful output,
   extra refund, fabricated remainder or second reservation/stock owner is forbidden.
5. Cancellation/disable/output waits retain or release actual tools without completion wear; only
   actual successful production consumes the authored catalyst quantity.
6. Public immutable facts/previews, native capacity/units, saved commands/execution, accounting and
   canonical replay use that same conversion and authored definition.

## Design and API

Core/ResourceTransactions.cs now requires ResourceConversionInput(Kind,Receipt,Consumed,ReleaseRemainder).
Kind explicitly classifies the physical draw as Consumed, Destroyed or Lost; initial catalyst callers
pass Consumed. [Current shared transaction/journal extension](P7_CONVERSION_ACCOUNTING_REVALIDATION_DESIGN.md#d2b-explicit-disposition-at-scoped-receipt-endpoints)
preserves the same receipt/remainder authority without an old constructor or inferred kind. The initial stage introduced the input vector
and revised ResourceConversionIntent to one Inputs collection, removing the old Consumed/Released
collections and signature. Each original token appears once. Exact original metadata is mandatory;
Consumed lies in [0,Receipt.Amount] at the selected endpoint's native quantum. Consume zero without
release is not an operation. Partial consumption may leave an actual held remainder; release retires
the remaining actual record, never recreates stock. No compatibility overload or duplicate processor.

Standard/ResourceConversions.PrepareConversion captures every endpoint before callbacks, validates
all full receipt inputs with existing PrepareClaimReceipts, then derives transient consumed/released
ClaimLegs. CommitConversion uses existing TakeReserved/Reduce and ReleaseOut/Reduce, verifying all
actual participants after each mutation before events, optional generation settlement or entity
result success becomes visible. ResourceGeneration uses only declared actual consumed quantities.

ITransformationDefinition.ToolConsumption extends existing definitions. TransformationDef requires
tool_consumption; resolved resources must be held tools and amounts <= their holds. TransformerDef
validates original selected packing. Nonempty completion consumption on a continuous recipe refuses
before Update: continuous replenishment/rate wear needs its own declared ongoing requirements and
is not silently treated as a batch. That remaining stage is not removed from full P8 scope.

Transformer.Deliver derives ResourceConversionInput rows from actual WIP and original tool receipts.
WIP consumes in full; tool consumption uses authored amount with remainder released. ToolSources,
actual books, saved execution identity, running progress, cancellation and return stay authoritative.
ReadToolConsumption in TransformationManagementView exposes the authored Bill through the same
owned/provider/content admission. No catalyst balance, condition bar, queue, scheduler or clock.
Spent catalyst byproducts are ordinary explicit output Bills/ports, never implicit disposal/refund.

## Files and migration

Change Core/ResourceTransactions, Standard/ResourceConversions/ResourceGeneration/Transformations/
TransformationContracts, existing transformation view/module/identity and proof ConversionProbeCommand
codec. Source-migrate every conversion caller directly and every master-listed recipe to explicit
tool_consumption. Revise the sole one-time authoring migration script; it is not a loader. Existing
tests compile only. New proof reuses the input-port master/common resources, actual pool/provider,
native processors and public command/view/Update helpers. No runtime rule belongs in the test.

## Error handling and verification

Missing/foreign/overheld/negative/discrete-invalid consumption or unsupported continuous policy:
ContentException before Update. Duplicate/stale/forged/wrong-owner/nonquantized/overconsumed input:
typed transaction refusal without mutation. Full output/result waits: no consumption/release prefix.
Provider mutation: fault, no successful checkpoint/history. Native wear range is validated, not clamped.

One NEW bounded exact public-player case proves shared original tool/catalyst holds, fractional wear,
changed live ports, output blockage, cancellation without wear/refund, exact original remainder release,
saved execution/command twins, immutable private facts, cumulative accounting/replay and invalid inputs.
Build for compilation; run only this exact new behavior. No old/family/full/Oil/platform runtime.

## Sequence, constraints and remaining full goal

1. Shared conversion input contract and sole transaction migration.
2. Native authored completion disposition, packing/preview and direct content migration.
3. Independent engine proof command codec and thin public-player case.
4. Focused verification, docs/master/acceptance/source-anchor reconciliation.

No legacy compatibility, platform Draw, new clock, mandatory budget/business or publication.
Continuous catalyst replenishment/wear, entity tools, scoped physical supply/export, dynamic endpoint
recovery, provider/global purity, accounting/lockstep/stress and full P8/P1–P12 remain required.
The full goal stays active; commit/push only after the complete goal AND plan are verified finished.
