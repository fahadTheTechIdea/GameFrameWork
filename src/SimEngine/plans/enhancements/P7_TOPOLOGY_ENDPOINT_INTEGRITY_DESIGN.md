# P7-D9: Topology endpoint integrity across physical geometry commits

## 0. Problem statement

D8 protects scalar facts, admitted stock allocations and drained retired endpoints. It does not observe reservation changes or transient stock mutations on retained/new commit participants while geometry is erased. Existing reservations on disconnected unchanged components must remain valid, not be forbidden globally.

## 1. Goals

1. Physically construct two disconnected corridors using existing authored workflows; supply the second pool through the original resource transaction.
2. Prove unrelated physical removal preserves exact inbound/outbound token authority on the unchanged pool, including saved continuation.
3. Reproduce stock/claim changes during geometry erase, including restored final stock and reserve/release cycles.
4. Observe actual commit participant versions and original inventory codecs without another receipt/stock model or persistence format.

## 2. Design

Extend the existing topology proof registration with a separate literal. Reuse Factory/Ready/Until and public commands for physical preparation. Direct transfer/receipt calls and callback intrusion are explicitly protocol instrumentation, not simulated player decisions.

Capture unique original components and proposed live components immediately before geometry. Use original EntityObservation provider scopes over each ResourceInventoryState.Sync and its owning component Version. Verify after successful callback return, before publication. The existing allocation validator and scalar guard remain. No global claim clearing, unrelated world freeze, manual rollback or second owner. If evidence requires a different repair, record it before implementation.

Inspection also finds provider callbacks in final graph validation and the publication-time `state.Version` getter. Inject late vertex/version read mutations as well. Gather final original geometry versions before the last participant verification and publish those transient admitted stamps without further provider callbacks. This removes the callback-after-validation gap instead of adding a later compensating check.

## 3. API surface

No public contract change. Behavior change increments engine build; save159/replay1 and all existing endpoint identities/codecs remain.

## 4. Files to change

Existing Networks.cs commit, existing topology proof tests, engine/protocol build identity, tracker/checkpoints and this stage's verification report.

## 5. Error handling

Healthy unchanged claims survive. Callback mutation faults at network.topology.commit. Original callback exceptions remain primary; no post-throw observation masks them. Faulted state refuses healthy live queries/save/hash. Material already moved is not falsely rolled back.

## 6. Compatibility

Development only; no compatibility or duplicate state path.

## 7. Verification

Build affected tests; reproduce the new literal before production edits. After repair run the new retained-endpoint literal, D8 commit observation and physical erase literals only. All are exact selections with60s process/5s termination watchdogs; physical preparation reuses512-update/20s stage bounds. No full/profile/family test run.

## 8. Risks

Observation is scoped to actual topology participants, not arbitrary CLR state. Codec observation must use original inventory Sync, not a new serialization model. Native endpoint Version detects successful transient mutations even when final stock matches. Existing receipt counters are owned only by ResourceReservationBook.

## 9. Out of scope

Business, hosts/demo/platforms/Draw, arbitrary provider sandboxing, broad stress and full P7.017 completion.

## 10. Execution order

1. Add healthy retained-claim and hostile-callback proof; build/reproduce.
2. Repair original commit participant observation, increment build identity.
3. Build and run the three exact literals; record all attempts and limits.
4. Update tracker; leave broad original requirements open and do not publish Git changes.
