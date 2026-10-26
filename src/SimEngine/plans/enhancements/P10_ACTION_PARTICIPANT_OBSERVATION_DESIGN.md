# P10-AP1 — Control-action participant observation

Status: implemented2.625.0/save159; new/isolated action proofs2509/0. Broader
existing automation process case174/2 remains unresolved. [Every attempt and next
actions](../../docs/migration/P10_ACTION_PARTICIPANT_OBSERVATION_VERIFICATION.md).
Original P10.022/full goal remain open.

## 0. Problem Statement

ControlActionDispatch.Pure observes scalar facts and participant versions only.
A custom policy/command codec/check can alter the actual participant's persisted
state without Touch and escape pure admission. Its finally-based scalar verification
can also replace a callback's original exception. This affects normal action
binding, command creation/copying/fingerprinting and command admission.

## 1. Goals

1. Observe actual target/input entity identities and original whole-entity codecs
   for every existing pure dispatch callback, including typed refusal paths.
2. Reuse EntityObservation, not another participant state model or copied inventory.
3. Preserve the original callback error; verify observations after success only.
4. Keep genuine pure previews/refusals, deferred actions, saved twins and isolation.

## 2. Design

Replace the version tuple list in the existing Pure helper with existing scalar
entity scopes and actual entity.Sync provider scopes. Deduplicate identical bound
entity IDs. Wrap the interval in original ReadProvider for the actual selected
action policy and selected space. Normal read/copy/check ordering is unchanged.
No additional persistent fields, control queue, action executor or scheduler.

## 3. API surface

No public contract change. Pure dispatcher implementation only; build identity
advances and save payload remains unchanged.

## 4. Files to change

| Action | File | Risk |
|--------|------|------|
| Modify | Standard/ControlActionPolicies.cs | Medium: pure callback boundary |
| Modify | ControlActionPolicyTests.cs, AutomationTests.cs | Low: focused protocol proof |
| Modify | SaveGame.cs/current-engine-protocol.json | Low: build identity |
| New | stage verification report | Low: evidence |
| Modify | P10 design, tracker, README, AGENTS | Low: scoped status |

## 5. Error handling matrix

| Provider outcome | Required result |
|------------------|-----------------|
| Pure ready or typed refusal | Same result, unchanged authoritative state |
| Silent persisted participant mutation | Original observation fault, no successful preview |
| Mutation plus callback throw | Original exception retained; terminal observation fault |
| Shared target/input identity | Same original participant observed once |

## 6. Backward compatibility

Development-only; no compatibility implementation. Existing JSON/codec owners stay.

## 7. Verification

Add one exact bounded case across target/input/shared bindings, binding/create/
copy/check/fingerprint phases, fresh/restored sessions, ready/refused command
admission and silent-state/original-throw/pacing-throw mutations. Inspect actual
native codecs, clock, publication, callbacks, pending intents and isolated saved
sessions. Run only that literal for reproduction/final and the existing directly
affected automation process literal once if needed. No full/family/profile runs.

## 8. Risks

Whole codecs cost more than version checks; deduplicate actual participants and
do not hash unrelated entities or add global snapshots. Not a CLR sandbox: the
contract observes bound participants and selected providers, not arbitrary external
state. Faults have no fabricated rollback.

## 9. Out of scope

Oil/business/platform/Draw/adapters, broad performance/stress or full P10 closure,
new control rules/queues, Git staging/commit/push.

## 10. Sub-task execution order

1. Add the focused protocol test and reproduce actual current-core failure.
2. Repair the original dispatcher using original observation owners.
3. Build changed C# and run selected focused proofs.
4. Record every attempt/limit and update tracker; keep broad requirements open.
