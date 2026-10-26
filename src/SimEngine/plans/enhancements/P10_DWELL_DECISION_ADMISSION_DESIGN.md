# P10-DA1 — Observe dwell metadata before publishing rule decisions

Status: implemented and focused-verified on2.629.0. [Before-fix evidence, final exact
checks and limits](../../docs/migration/P10_DWELL_DECISION_ADMISSION_VERIFICATION.md).
Original P10.acceptance.022 and full goal remain open.

## 0. Problem Statement

AutomationSystem.Update currently observes Evaluate/ValidateEvaluation but then
writes Record.State/Candidate/Since before reading rule.Dwell outside that scope.
A custom provider may mutate a bound entity, change the sole clock or throw during
that getter. This must not publish a new state/dwell/next-step intent prefix.

## 1. Goals

1. Reproduce the actual interval using the existing independent BandControlRuleDef
   TimingMetadataProbe; no new provider, catalog, hook or executor is needed.
2. Keep Evaluate, result validation, dwell conversion and checked due-time admission
   inside the existing EntityObservation.ReadParticipants interval.
3. Publish prepared rule state only after that interval succeeds. Preserve existing
   cooldown short-circuiting, latches, pending actions and next-step timing.
4. Prove fresh/loaded sensor/target/shared bindings fail safely on silent payload
   mutation, payload+throw and clock+throw, with original first cause and no rule prefix.
5. Preserve ordinary saved deterministic transition/action behavior with focused tests.

## 2. Design

Extend RuleProviderObservationTests with one directly registered dwell exact case,
reusing its Rules/Bound helpers and existing actual independent content. Use ordinary
probe commands to change the condition; inject only the negative provider callback.

Move original evaluation decision preparation into its existing observed callback.
Return transient result/candidate/since/pending/due values; commit to the original
Record only after verification. This is not another state machine, state owner,
scheduler or persistent copy. Preserve blocker suspension and already-pending/already-
applied fast paths. Read Dwell only when the existing cooldown branch permits it.

Bump EngineInfo.Build and the independently authored expected build JSON to2.629.0
for changed simulation behavior. Save159/replay1 and automation codec5 remain unchanged.
No compatibility support is added.

## 3. API surface

No production public API, content schema or payload changes. Original module rule
contracts and sole game clock remain authoritative.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | Standard/AutomationSystem.cs | Medium: original decision preparation order |
| Modify | Kernel/SaveGame.cs + current-engine-protocol.json | Low: explicit build identity |
| Modify | RuleProviderObservationTests.cs + AutomationTests.cs | Medium: direct exact negative proof |
| New/modify | evidence/design/master/README/AGENTS | Low: scoped progress |

## 5. Error handling matrix

| Provider behavior | Required result |
|---|---|
| Healthy dwell getter | Original deferred action and saved twin identity |
| Silent bound payload mutation without Touch | Original observation.provider fault |
| Payload/clock mutation followed by throw | Same original exception/phase, not verification masking |
| Failed metadata interval | No new rule state/candidate/due prefix, no completed step/save/hash |

## 6. Backward compatibility

Development-only; exact build matching, no shims or migration path.

## 7. Verification

Build once for failing proof, run only its exact literal for baseline. After owner
repair, targeted build and the dwell literal, existing rule-provider literal,
deferred-action commit literal and normal independent control-action literal.
All bounded60s/5s, no full/family/profile or broad automation bundle.

## 8. Risks

Do not read Dwell on branches that previously skipped it; do not apply newly evaluated
commands in the same step. Do not assert rollback of actual hostile provider mutation.
Only withhold rule-success publication and retain original fault ownership.

## 9. Out of scope

Broader arbitrary command/reentrant metadata/provider/stress P10.022 matrix, Oil,
Tycoon/platform/Draw, full tests and staging/commit/push. No original broad gate closes.

## 10. Sub-task execution order

1. Add the existing-provider dwell interval proof and retain actual failing baseline.
2. Move original decision preparation inside the sole observation owner; bump build.
3. Run selected exact checks, record all attempts/identities/limits, update trackers.
