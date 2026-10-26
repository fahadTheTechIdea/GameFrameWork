# P8-OC1: Queue mutation preserves earned operating costs

## 0. Problem statement

`TransformerManager.AppendQueue` and `CancelQueue` validate identities, progress and material/tool commitments but omit an explicit before/after comparison of earned operating exposure and consumption. A contract provider can report a different internally consistent elapsed exposure while retaining head progress and passing standalone snapshot validation. Queue edits are player intent, not productive execution.

## 1. Goals

1. Appending any request preserves actual earned operating exposure/consumption.
2. Cancelling a tail or entering physical return preserves earned exposure/consumption. Removing a head without WIP still retires its exposure normally.
3. Existing native physical all-phase cancellation remains correct; independent providers obey the same coordinator.
4. Faulty engine-side extension callbacks are refused through ordinary commands with original first-fault cutoff, no healthy save or success prefix.

## 2. Design

Extend the existing manager with one private comparison using existing `TransformationSnapshot.Operating` values and original `SameBill`. Compare elapsed exposure and consumed bills only: next demands/drawing can legitimately change when a head enters return or an empty queue gains a recipe. Do not add a payment ledger, timer, compatibility branch, provider-specific lookup or rollback fiction.

Extend the existing independent Pulse provider's own productive-update exposure facts through `IUtilityConsumer`. Empty authored operating rates remain explicit empty bills/ports; the count is earned execution exposure, not another clock. It increments only when productive work advances, resets when the actual head retires, and is saved in the same provider codec (schema3). JSON queue probes alter exposure on append or tail cancel only after genuine work. No driver mutates gameplay state.

Engine build2.502.0; save154/replay1/transformation view22 unchanged. Native authoritative save payload unchanged.

## 3. API surface

No new production contract. Existing ITransformer/IUtilityConsumer/queue/commands/views remain authoritative. Independent proof facts use existing UtilityDemandSnapshot.

## 4. Files to change

- Standard/Transformations.cs existing queue manager and Kernel/SaveGame.cs build identity.
- Existing ExtensionProof/TransformerProof.cs actual independent provider/codec.
- One new public-player driver; reuse existing native all-phase physical helper without invoking old registered cases.
- Registration/profile/protocol fixture and tracker/evidence.

## 5. Error handling

Malformed or changed earned costs latch the existing transformations.enqueue/cancel fault. No command success, productive output, healthy checkpoint or later execution is inferred. Normal head retirement may reset execution-derived costs; physical return and tail edits may not.

## 6. Compatibility

Development only; no old proof codec or compatibility shim.

## 7. Verification

One solution build and one new bounded exact case. Public authored JSON, commands, immutable snapshots/results, one clock, Game.Update, saves and replay only. Negative probes live in the engine-side extension, not the driver. Full regression remains deferred until implementation is finished.

## 8. Risks

Derived next-input/port/drawing facts are not historical costs and must not be compared blindly. A failed mod mutation is terminal; this is not invented transactional rollback.

## 9. Out of scope

No Tycoon rules, paid-mode migration, Oil consumers, rendering/platforms, global accounting closure, new time owner, host gameplay, staging or publication. Whole P8.013 remains open until its remaining provider/paid-cost scope is proven.

## 10. Execution order

1. Enforce shared queue cost preservation.
2. Extend original independent provider/probes/codec and add public proof.
3. Build/run only new exact case; retain failures.
4. Record actual evidence and tracker without whole-gate promotion.

## Delivery outcome

NEW FIRST/FINAL657/0/0 in51401ms; one solution13.77s clean. Current two independent legitimate/six faulty/four original physical return flows and native all-phase cancellation helper executed. Original221/190checked/31open unchanged,additional233; P8.013/P8.017/full goal remain open. Profile-info701 included/0excluded/0game suites; full executor not run. No executable edits after FINAL.

Metadata-only validation:1803 source anchors/1010 frozen executable inputs/13 quickstart anchors match; all221 original gates retain literal requirements/line anchors and existing checkboxes.2987 inventoried files/1073 Markdown/8765 local links, zero broken;18 master rows/zero malformed. Driver boundary0, diff-check0,2975 dirty/0staged. All12 Tycoon source hashes and sole clock unchanged; goal_complete=false.
