# P10-B6 — live typed automation thresholds

Status: native focused slice verified on2.133.0/save72,2026-10-06. Full P10/P1-P12 remains open.
Current [B6b](P10_AUTOMATION_PROFILE_VIEWS_DESIGN.md),2.134.0/save72, includes this coverage in its
expanded479-check/two-suite gate. It replaces threshold-only metadata with complete profile discovery;
settings/execution/clock owners remain unchanged. The388 checks below are historical, not additive.

## 0. Problem

Native numeric rule nodes currently read only immutable authored on/off values. Players cannot
configure hysteresis bands through engine commands without replacing content or running host logic.

## 1. Acceptance

1. Every rule exposes typed stable threshold declarations through a required public contract.
2. AutomationSettings explicitly supplies every declared numeric band; no omitted/default fallback.
3. Existing configure preview/command validate identities, exact units and band direction.
4. Same-binding edits preserve applied latch/cooldown and cancel old candidate/pending work;
   fresh dwell is evaluated against the new values inside existing phase290.
5. Settings, pending commands, snapshots, saves/hashes use the same owner/codec and immutable values.
6. Native and public-only providers use the same contract, without scheduler modifications.

## 2. Design

ControlThresholdBand holds exact unit/on/off. IControlThresholdDefinition declares input, direction
and authored initial band; native definitions project existing JSON numeric nodes. Boolean/logical
nodes declare no numeric band. The native evaluator reads context.Thresholds, never immutable initial
values as a runtime fallback. Existing AutomationSettings receives an explicit complete threshold map.
The same serializer owns configured values and queued command values; no second configuration store.
CheckShape enforces actual rule/input/signal metadata, exact map identities/units and hysteresis order.
One shared stateless validator serves composition, command/preview and checkpoints.

## 3. API

Required IControlRule.Thresholds metadata; ControlEvaluationContext.Thresholds immutable values;
AutomationSettings constructor requires threshold map. Existing ConfigureAutomationCommand/view
previews remain the only configuration path. Authored JSON on/off remain initial player values only.
At this slice OperationsView.TryGetAutomationThresholds exposed detached immutable declaration values, filtered
through the existing content visibility owner. Hidden/unknown/unseated viewers receive no metadata.
Configure uses that same content visibility before publishing parameter refusal details; durable
previous rule history still validates structurally, not against a new visibility grant.
B6b removes that threshold-only API in favor of TryGetAutomationProfile/CopyAutomationProfiles,
with Thresholds included in the single complete projection. No compatibility alias remains.

## 4. Files

AutomationContracts.cs, ControlRules.cs, AutomationSystem.cs, OperationsModule.cs, SaveGame.cs;
public proof rule/provider and authored fixtures; directly affected helpers/settings call sites;
same two complete automation suites, stage/master/evidence/README/help.

## 5. Errors

Missing/extra keys or reversed bands: typed invalid settings. Wrong units: typed unit mismatch.
Null or malformed metadata fails composition. Invalid saved shapes fail before continuation.
No silent clamping, conversion, substitution or unit inference.

## 6. Compatibility

No four-argument settings constructor or old payload reader. Build2.133.0/save72,
AutomationSystem3/OperationsView4; unrelated owner codecs unchanged.

## 7. Verification

Build, then exactly the two complete existing automation suites extended with threshold proof.
No third/family/full suite. Unrelated suites compile only.

## 8. Risks

Threshold edits cannot retroactively revert physical controls or bank old dwell. Existing candidate
and pending cancellation is reused. Retained settings and pending command payloads must stay immutable.

## 9. Non-goals

No new controller/scheduler/clock, runtime action/dwell/cooldown editing, reserve target provider,
utility physics, Oil/Tycoon/host/demo/platform/Draw or full roadmap completion.

## 10. Order

- [x] Contracts and shared validation; native authoring/evaluation and existing settings codec.
- [x] Required public proof and explicit call-site migrations, with no compatibility overload.
- [x] Focused identity/unit/immutability/physical/dwell/pending/cooldown/save/pause tests.
- [x] Clean build and two exact suites, then evidence and tracker alignment.

## Recorded gate

Clean build, zero warnings/errors;388 checks/two exact complete suites (176 contracts;212 execution),
zero final failures/aborts. [Evidence](../../docs/migration/P10_THRESHOLD_SETTINGS_VERIFICATION.md).
Includes previous B5 recovery coverage, not an additional321 current checks. Clock's preceding85-check
gate is historical, not rerun; its source is unchanged. Three threshold checkpoint corruption cases
reject through the actual saved-record validator. A public-only numeric rule changes bands through
ordinary controls without coordinator edits. Native production uses an explicitly authored20-minute
duration and proves pause/progress/material conservation/restart with saved twins. All observation
loops are bounded.62 build/59 save assertion files migrate; unrelated suites compile only.
Lifecycle guidance kept single-owner implementation, scoped proof and staged documentation aligned.
Full utility/backpressure/reserve/action editing/provider/replay/stress/allocations/P1-P12 remains open.
