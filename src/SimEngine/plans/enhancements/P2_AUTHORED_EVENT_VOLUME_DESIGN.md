# P2-F12: Authored event dispatch volume

## 0. Problem Statement

Kernel/Events.cs imposed a fixed 1,048,576-event threshold. This is neither a
game-authored rule nor a platform limit. Large legitimate completion notifications
faulted identically to runaway handlers. The original EventBus remains the sole owner.

## 1. Goals

1. Remove the global hardcoded event-volume cap, without changing publication order.
2. An optional master-selected JSON policy declares a positive 64-bit budget or explicit null.
3. A selected budget refuses before adding the offending event, retains the original
   terminal fault, discards undeliverable pending events and prevents later listeners.
4. Physical work can generate more than the old cap, with exact saved/replayed results.

## 2. Design

IEventDispatchPolicy is immutable content. EventDispatchPolicyModule binds its selected
actual catalog definition after validating all loaded aliases. EventBus captures the
immutable scalar at composition freeze. No selected policy means unbounded volume,
subject only to actual representation/memory limits, never a fabricated default.
The existing event bus checks a selected limit before queue mutation and owns cleanup.
The budget applies to a dispatch batch, including initial dispatch and handler-produced
events, not a second clock or variable simulation-time step.

## 3. API surface

Optional module sim.event-dispatch-policy, section event_dispatch_policies, scenario
settings profile. Policy JSON requires maximum_events_per_dispatch: positive integer
or null. Detached EventDispatchPolicyView exposes selected metadata. Build2.447.0;
save154/replay1 unchanged. No second runtime queue/configuration/state model.

## 4. Files to change

Original Events and build identity; new native policy registration/module and detached
metadata view; actual proof JSON/module and one public-input acceptance case.

## 5. Error handling matrix

Missing/invalid profile or nonpositive/fractional/unknown/missing field refuses content
before Initialize. Exceeded selected budget latches events.budget before publication,
clears pending events and stops Update/save. Unlimited finite publication remains
ordinary synchronous deterministic dispatch, with no frame slicing or delayed gameplay.

## 6. Backward compatibility

None; no hidden replacement default or legacy threshold.

## 7. Verification

One solution compile attempt, same-case repair compiles only as necessary, ONE NEW
bounded exact acceptance case. Actual master/JSON and public player commands drive
travel/work/hauling and a finite large authored event effect. Saved twins/pause/private
consumer observations/replay/session isolation and explicit budget refusal are included.
Other historical/full profiles are not rerun; all attempts retained in evidence.

## 8. Risks

Unlimited event dispatch deliberately requires finite module behavior. Games selecting
runaway-handler protection must author a budget appropriate to their workload. Generic
CPU/private CLR purity/network buffering/allocation/global/whole-plan guarantees remain open.

## 9. Out of scope

Lockstep's separate buffering policies, rendering, Oil, Tycoon and platform SDKs.
No original compound gate is closed by this scoped event change alone.

## 10. Sub-task execution order

1. Replace the original cap with optional contract-based composition metadata.
2. Author actual master-listed policy/effect JSON and a thin public-input acceptance case.
3. Compile and run only that new bounded case, retaining every attempt.
4. Update the existing master, headers, audit/source hashes and evidence; keep full goal open.

## Current scoped result

Implemented at2.447.0/save154/replay1. FINAL226/0/0 in5297ms; both runtime
attempts and all three compile attempts retained in
[verification](../../docs/migration/P2_AUTHORED_EVENT_VOLUME_VERIFICATION.md).
All original221 gates remain180checked/41open. No executable edits after FINAL;
no historical/family/full runtime, aggregate closure, staging or publication.

## Authoring

Master module declaration selects sim.event-dispatch-policy version1.0.0 and
its event_dispatch_policies document. Scenario modules select the same module;
scenario settings require an explicit profile such as events.large. Documents use:

```json
{"event_dispatch_policies":[{"id":"events.large","maximum_events_per_dispatch":2000000}]}
```

Use explicit null for unlimited selected policy, or omit the optional module.
The declaration reader rejects missing values, noninteger/nonpositive/overflowing
budgets and unknown fields. There is no legacy threshold or implicit profile.

## Metadata closure

Integrity:1473 source anchors,747 selected inputs and13 quickstart anchors;
zero mismatch/drift.952 Markdown documents/8554 local links, zero broken;
18 valid master rows, all221 literal gates unchanged. Driver boundary matches0,
git diff --check exit0;2702dirty/staged0 preserved, four protected hashes unchanged.
This is source/evidence integrity, not full gameplay or plan acceptance.
