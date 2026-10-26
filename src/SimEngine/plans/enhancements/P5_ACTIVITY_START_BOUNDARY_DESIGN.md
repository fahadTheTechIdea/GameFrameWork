# P5-A35: Activity start-rule boundary

## 0. Problem statement

The optional activity-start contract already connects physical work to optional module settlement. Its binding getter was read outside observation, its admission callback did not observe the whole target codec, and a direct module call could catch a settlement failure without latching the original session fault. This stage hardens that existing boundary, not financial rules.

## 1. Goals

- Observe actual rule/activity binding getters with the original provider, space and clock guards.
- Require actual frozen catalog aliases, reject duplicate runtime selections, and reject binding changes during admission or settlement.
- Observe the actual target codec while running the pure Check callback.
- Latch failed direct settlements before order allocation; preserve the first error and prohibit faulted saves.
- Retain ordinary resource-only physical work and existing optional Tycoon fees.

## 2. Design

The sole owner remains `src/Beep.SimEngine/Standard/ActivityStartRules.cs`. ReadBinding validates actual content aliases. Find invokes it inside EntityObservation and rejects multiple matches. Check observes the target codec and rechecks the selected activity after the callback. Commit uses the existing World first-fault latch, rejects non-success outcomes, verifies healthy state and the selected binding before WorkBoard allocates an order.

No new registry, account, transaction, work-progress model, scheduler, clock, or compatibility path. Immutable providers must keep their bindings fixed. This scoped guard detects callback-time changes; it is not a sandbox for arbitrary CLR fields or unrelated services.

## 3. API surface

No signature changes to IActivityStartRule. Check is pure; Commit may settle its existing owner's state only on success. A failed commit terminates the session. This is fail-closed behavior, not rollback or permission to publish a partial transaction.

## 4. Files

- Modify ActivityStartRules.cs and EngineInfo.Build; save156/replay1 payload schemas unchanged.
- Update the authored current-engine protocol identity.
- Add independent non-friend ActivityStartRuleProof.cs and master-listed activity-start-rules.json.
- Add ActivityStartRuleTests.cs and register exactly one focused case.
- Update current tracker/audit and this stage's evidence.

## 5. Errors

Binding/preview violations latch the existing observation fault. Failed settlement latches work.start unless an earlier fault exists. No order is allocated after a failed start; no faulted checkpoint is allowed. Provider business state is not silently restored or replaced.

## 6. Compatibility

Development-only replacement in the active path; no legacy or compatibility shim.

## 7. Verification

One new bounded exact framework case proves resource-only local work, pure preview, physical material consumption, saved continuation, getter clock mutation, Check clock/target mutation, callback binding substitution, and directly caught settlement exceptions/refusals/substitution. One affected exact Oil native-research case proves existing plans, fees and saved knowledge through the changed boundary. No family/full regression.

## 8. Risks

Whole-target codec observation adds admission cost; global allocation/performance gates remain open. Frozen content is a contract, not protection against malicious private fields. Unrelated service mutation and transaction rollback are not proven here.

## 9. Out of scope

Tycoon rule/amount changes, financial redesign, host/platform/Draw work, all mods, all incidents/logistics, global durability/performance, and full industrial acceptance.

## 10. Execution status

- [x] Harden the original start boundary.
- [x] Run the new exact provider-fault case.
- [x] Run the affected exact optional-fee case.
- [x] Update current records without closing any original compound gate.

Full goal/plan remains open: original221 /194 recorded checked /27 open.

