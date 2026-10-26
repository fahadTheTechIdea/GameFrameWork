# P10-CA1 — Observe complete configuration admission and initial state

Status: implemented and focused-verified on2.630.0. [All builds, failed/intermediate
attempts, final checks and limits](../../docs/migration/P10_CONFIGURATION_ADMISSION_VERIFICATION.md).
Original P10.acceptance.022 and full goal remain open.

## 0. Problem Statement

AutomationSystem.CheckConfigure reads visibility/shape metadata before the nested
action checks observe bound entities. Configure separately reads InitialLatch and
InitialState without observing those entities. Custom contract callbacks can mutate
actual private payloads without Touch, or throw outside the provider fault boundary.

## 1. Goals

1. Observe the complete original CheckConfigure through the existing
   ReadParticipants/settings.ResolveParticipants owner, including typed refusals.
2. Admit initial latch/state before publishing a new/rebound original rule record.
3. Prove public preview, direct engine contract configuration and ordinary inbox
   configuration respect these boundaries on fresh/loaded sessions.
4. Preserve existing records on failed rebind; stop later inbox work after first fault.
5. Preserve healthy previews, configurations, saves and deferred action behavior.

## 2. Design

Wrap the original configuration validation body, not duplicate it in a second
validator or command handler. Observe the selected actual rule plus actual target/
input entity codecs and selected space. Existing nested action-policy observations
remain their callbacks' owner. Keep typed refusal order and private-content disclosure.

Prepare the new Record's initial latch/state inside the same existing observation
mechanism before dictionary publication. Configure remains the only mutation owner;
same-binding edits retain historical latch/state/applied facts as before.

Extend the existing RuleProviderObservationTests and the existing non-friend
BandControlRuleDef's explicit transient test hooks. The initial-state/latch hooks
instrument actual getters, not authored behavior. Use ordinary commands for the
inbox; no second scheduler, rule state, clock, codec or game logic in drivers.

## 3. API surface

No production public API/content/payload changes. Changed behavior advances engine
build to2.630.0 and its independent expected identity; save159/replay1/automation5
remain unchanged. No compatibility support.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | Standard/AutomationSystem.cs | Medium: configuration provider intervals |
| Modify | Standard/AutomationContracts.cs | Low: explicit pre-write null-settings rejection |
| Modify | Kernel/SaveGame.cs + expected protocol JSON | Low: exact build identity |
| Modify | AutomationProof.cs | Low: transient negative instrumentation |
| Modify | RuleProviderObservationTests.cs + AutomationTests.cs | Medium: focused lifecycle proof |
| New/modify | stage evidence/master/P10/README/AGENTS | Low: scope and progress |

## 5. Error handling matrix

| Context | Required result |
|---|---|
| Healthy preview/configure/rebind | Same authoritative settings/state and saved continuation |
| Silent bound payload change | Existing observation.provider terminal fault |
| Provider payload/clock change then throw | Original exception/first cause retained |
| Failed seed/rebind | No new/replaced rule record or accepted/later inbox success |
| Unknown/private/invalid intent | Existing typed refusal semantics preserved |
| Absent required settings in submitted command | Explicit argument rejection before codec bytes/sequence/queue; no default configuration or payload change |

## 6. Backward compatibility

Development-only; no legacy models, aliases, fallback or old-build saves.

## 7. Verification

Before-fix targeted compile and only the new exact configuration case. After repair,
targeted compile and the new case, existing dwell admission, rule-provider and normal
control-action cases. Bounded60s/5s, no full/family/profile or broad automation bundle.

## 8. Risks

Do not recursively observe the AutomationSystem's own Sync while validating it.
Null/invalid settings and typed refusals must keep their previous meaning. Provider
failure does not roll back its hostile mutations; withhold rule publication instead.

## 9. Out of scope

All persistence/replacement metadata entry points, arbitrary handler/reentrant changes,
full P10.022/stress/profile acceptance, Oil/Tycoon/platform/Draw and Git publication.

## 10. Sub-task execution order

1. Add actual getter/entry-point failure proof and retain baseline evidence.
2. Wrap original validation and seed publication; update exact build identity.
3. Run focused current checks, document all attempts/limits and update tracker.
