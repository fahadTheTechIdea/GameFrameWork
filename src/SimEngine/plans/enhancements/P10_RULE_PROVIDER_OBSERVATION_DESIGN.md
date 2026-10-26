# P10-RP1 — Rule provider evaluation and composition

Status: implemented2.627.0/save159; baseline286/44, five selected current-core
proofs3108/0. [Every attempt/limits](../../docs/migration/P10_RULE_PROVIDER_OBSERVATION_VERIFICATION.md).
Original P10.022/full goal open.

## 0. Problem Statement

AutomationSystem still evaluates IControlRule without observing actual bound
entity codecs, and verifies in finally. Its action/rule/dependency composition
intervals likewise verify in finally. Silent persisted participant changes and
masked original exceptions contradict the pure-provider boundary already used by
action dispatch and recovery.

## 1. Goals

1. Observe all actual rule-bound entities, provider and selected space across rule
   evaluation/result validation before writing rule state.
2. Preserve original metadata/evaluator exceptions without finally masking.
3. Use EntityObservation.ReadParticipants/ReadProvider, not another observer.
4. Keep shared source/target bindings, saved continuation, typed control timing and
   normal composition validation intact.

## 2. Design

Add one internal bound-entity resolver to existing immutable AutomationSettings.
Action dispatch, recovery and evaluation use it; EntityStore remains identity
authority, and EntityObservation remains deduplication/codec authority.

Evaluate and validate the returned named state inside ReadParticipants before
assigning Record.State or deriving pending work. Replace the three remaining
composition finally intervals with existing ReadProvider, checking after success.
No new public API, persistent fields, scheduler, clock, rule executor or defaults.

## 3. API surface

Internal resolution and callback admission only. Build identity advances; save and
replay payloads stay unchanged.

## 4. Files to change

| Action | File | Risk |
|--------|------|------|
| Modify | AutomationSystem.cs, AutomationContracts.cs, ControlActionPolicies.cs | Medium: original provider intervals |
| Modify | tests/ExtensionProof/AutomationProof.cs | Low: transient explicit fault hooks |
| New | tests/Beep.SimEngine.Tests/RuleProviderObservationTests.cs | Low: focused protocol evidence |
| Modify | AutomationTests.cs, SaveGame.cs/current-engine-protocol.json | Low: registration/build identity |
| Modify | ControlContentContractTests.cs | Low: selected assertion uses canonical save-protocol owner |
| New | verification report | Low: exact evidence |
| Modify | tracker, P10 design, README, AGENTS | Low: stage status |

## 5. Error handling matrix

| Provider result | Required result |
|-----------------|-----------------|
| Pure composition/evaluation | No unintended state; saved twins continue |
| Silent bound payload mutation | First observation fault before result/state publication |
| Clock mutation plus throw | Original exception/message preserved |
| Metadata throw at node/action/rule validation | Original cause, no finally replacement |

## 6. Backward compatibility

Development-only; no shim. Existing authored modules and codecs remain authoritative.

## 7. Verification

One new exact case covers fresh/restored bound target/input/shared identities,
silent saved-payload/throw/pacing faults, and actual dependency/action/rule
composition callbacks. Composition calls are explicit protocol instrumentation of
the actual validator, not public-player gameplay. Run only this reproduction/final
and directly affected recovery/action literals as needed. No full/family/profile.

## 8. Risks

Not a CLR sandbox or proof of all unrelated global mod state. Existing scoped
codecs have a cost; deduplication remains in one owner. Metadata reads outside
these named intervals are not implicitly declared proven.

## 9. Out of scope

Oil/business/hosts/platform/Draw/adapters, new gameplay formulas, broad P10 stress
or final profile closure, Git staging/commit/push.

## 10. Sub-task execution order

1. Instrument existing provider hooks and reproduce current defects.
2. Migrate original evaluation/composition intervals to existing observers.
3. Build and run exact affected proofs; document failures rather than waive them.
4. Update tracker/evidence without closing broad original requirements.
