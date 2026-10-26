# P7-SS1 — Observe the actual spill-site predicate at admission and commit

Status: scoped implementation verified on2.661.0/save160/replay1.
Baseline292/20; final360/0, plus two separately selected physical cases322/0 each.
[All attempts, identities and remaining limits](../../docs/migration/P7_SPILL_SITE_OBSERVATION_VERIFICATION.md).
Original P7.017 and the full goal remain OPEN.

## 0. Problem and specification

SpatialResourceTransferContracts.Check observes the cell's CheckSite callback but
does not capture the complete target codec. Native deposit exposure's prepared
commit calls Cell.CheckSite directly, before CommitMovement. A private target
mutation can become the transaction's new baseline; a clock mutation can also
escape this unguarded commit call. Existing incident preparation guards end
before legitimate Raise/publication and cannot replace this predicate boundary.

## 1. Goals

1. Reproduce silent target mutation during direct Check and actual delayed
   propagation commit on native and independently implemented tile storage.
2. Share one original full target/provider/space/clock predicate observation in
   both existing call sites, retaining first exceptions and typed pure refusals.
3. Fault before ANY material movement, without rollback fiction. A delayed target
   episode already legitimately raised before commit remains partial/faulted,
   never a healthy successful spill or savable game.
4. Preserve ordinary timed spill/recovery/hauling/treatment saved/replay behavior.

## 2. Design

Add one internal ReadSite helper to the EXISTING SpatialResourceTransferContracts:
EntityObservation.ReadProvider over actual cell and target, selected world.Space,
observeTargetState:true. Direct Check and SpatialResourceTransferEffectDef's
fresh prepared site validation both delegate to it. Original movement intents,
transaction preflight/commit, cell/buffer/receipt/layer, incident phases and clock
remain sole owners. No new cache, compensating transfer or incident coordinator.

Extend existing BrokenTileCellDef negative instrumentation with world-scoped
target payload/clock/throw/refuse modes and an after-actual-Raise selector. Reuse
existing ExposurePrivateProbe codec; no fake incident or resource state. A new
master-listed JSON cell/probe overlay replaces the original cell definition in
the existing spill fixture. Existing IndependentTileWorkTests.Factory gains
strict optional module/declaration/document authoring, not another factory.
Same SpillResourceAccountingTests owner adds one separately selected contract
literal via a partial file. Negative direct calls use real current state;
commit faults occur during normal Update after a real player Raise command and
the original authored propagation delay. Another session remains isolated.

## 3. API and state

No public API or codec/layout change. Build2.661/save160/replay1 for changed
runtime safety. Old saves stay historical/incompatible, never header-rewritten.
Probe diagnostics are explicitly negative-only and not saved gameplay state.

## 4. Files

Original Standard/SpatialResources.cs, SaveGame.cs/current protocol; existing
SpatialCellContentProof.cs, IndependentTileWorkTests.cs and spill registration;
same-owner partial proof, explicit probe JSON/resource, stage/evidence/tracker.

## 5. Errors

Healthy site refusal returns its original Code with no mutation. Mutating refusal,
payload/clock change and provider throw fault through the original observer.
Fresh pure refusal after successful preparation retains original commit-disagreement
fault, no resource prefix. First exception/last healthy snapshot/clock remain
authoritative. No stock/terrain restoration after a failed callback.

## 6. Compatibility

Development only; original owner migration in place, no fallback/legacy path.

## 7. Verification

One new exact contract case baseline then final, timeout30000/termination5000,
output12000. After completed target build, select the existing independent and
native physical spill cases separately. No full/profile/family run. Keep all
failures, record actual callbacks/material prefixes and source identities.

## 8. Risks

Actual Raise precedes prepared commit: incident episode may already change before
fault. Assert material did not move and no healthy publication/save, not rollback.
Scope guards declared target/cell/spatial/clock; arbitrary unrelated private CLR
state is not a sandbox guarantee or closed broader P7.017 gate.

## 9. Out of scope

New resource model, lot/quality feature, Oil/business/platform/Draw, native harbour
performance, full acceptance/gate closure and Git publication.

## 10. Execution

1. [x] Reproduce through actual direct and delayed commit call sites.
2. [x] Repair both through one complete original target observation.
3. [x] Await build and run same exact contract plus two affected physical literals.
4. [x] Update evidence and original unchanged completion audit.
