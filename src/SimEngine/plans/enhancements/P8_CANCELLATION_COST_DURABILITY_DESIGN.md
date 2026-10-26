# P8-CC1: Cancellation durability with optional monetary costs

## 0. Problem statement

The existing queue coordinator preserves resources/tools/operating exposure but does not observe the optional monetary owner's persisted state across queue admission or mutation. A mod can charge or credit accounts while editing intent. Native all-phase cancellation has not yet been proven with current enabled monetary costs and disabled-cost accounts.

## 1. Goals

1. Queue previews, append and cancellation never change the selected account owner. Real payments remain native productive-start decisions.
2. Save/load/cancel across Idle/Supplying/Working/Blocked/Returning and continuous stop conserve WIP, tools, outputs, fuel and optional money without repeated costs.
3. Disabled manufacturing costs retain accounts unchanged; resource-only sessions require no fake accounts.
4. Independent bad callbacks cannot publish success or a healthy save after altering the account owner.

## 2. Design

Observe actual optional IEconomy through the existing EntityObservation codec dispatch inside TransformerManager. One private nullable scope helper; no second account ledger, balance copy, pricing rule, clock or concrete Tycoon lookup. Verify admission before returning any result and mutation before publishing success. Normal productive system execution occurs after that scope has ended and may charge according to existing authored mode rules.

Extend existing public physical factory/helper with data-authoring inputs and optional monetary assertions. Reuse actual JSON mode properties/account policy, existing TycoonModule and business views; never change business sources or rules. Use actual physical hauling/return/output wait rather than test-side stock changes. Existing independent Pulse JSON probes attempt account changes through IEconomy while editing queues.

Engine build2.503.0; native save154/replay1/view22 and Pulse codec3 retained.

## 3. API surface

No new production API. Existing public queue/control/views/Game.Update/save/replay and optional IEconomy remain the only paths.

## 4. Files to change

- Existing Standard/Transformations.cs optional observation and engine identity/protocol fixture.
- Existing public factories/physical helper and Pulse negative probes.
- One new focused public-player driver, registration/profile and this stage/evidence/tracker.

## 5. Error handling

Any actual persisted account change during queue admission/mutation latches transformations.queue-account. No prefix, healthy save or rollback fiction. With no account owner, the nullable scope is absent and physical rules are unchanged. Hidden/private management views continue original refusals.

## 6. Compatibility

Development only; no legacy, compatibility readers or parallel runtime.

## 7. Verification

One solution build and one new bounded exact cancellation case. Three native all-phase configurations: resource-only, accounts/costs off, accounts/costs on. Independent account-changing callbacks and saved pending refusal. Per-workflow bounds/diagnostics plus final watchdog; no full suite until implementation finishes.

## 8. Risks

Legitimate next demand or capacity reservations are not payments; existing WithdrawInputs only removes demand rows. Do not move productive-start charging into queue commands. Mod faulted state is terminal, not artificially refunded.

## 9. Out of scope

No Tycoon business edits, market/lease/auction rules, Oil consumers, platform/Draw work, global mod sandbox, new state owner or staging/publication. Gate closure requires actual final evidence, not the planned test matrix.

## 10. Execution order

1. Guard original monetary owner around queue admission/mutation.
2. Author optional mode/account data and extend existing physical assertions/probes.
3. Build/run one bounded exact case; repair only actual failures.
4. Record outcomes and audit original cancellation gate without promoting broader accounting/full regression.

## Delivery outcome

Original P8.acceptance.013 alone closed after current FINAL976/0/0 in106959ms; FIRST1/1 fixture ownership failure retained. One solution9.89s/focused document-validation6.34s/focused repair6.09s clean. Three actual native all-phase configurations andfourteen independent monetary fault sessions executed. Original221/191checked/30open,additional234; whole P8/provider/global/full goal open. Profile-info702 included/0excluded/0game suites; full executor not run. No executable edits after FINAL; all12 Tycoon implementations unchanged.

Metadata-only validation:1808 source anchors/1013 frozen executable inputs/13 quickstart anchors match; all221 original gates retain literal requirements/line anchors, only P8.013 checkbox changed this slice.2992 inventoried files/1075 Markdown/8767 local links, zero broken;18 master rows/zero malformed. Driver boundary0, diff-check0,2980 dirty/0staged. All12 Tycoon sources and sole clock unchanged; goal_complete=false.
