# P4-R2: Shared observed activity-intent admission

Status: implemented/focused-verified on2.607.0/save159/replay1/work-view106.
Three literal finals1096/0; [all attempts/limits](../../docs/migration/P4_ACTIVITY_INTENT_OBSERVATION_VERIFICATION.md).

## 0. Problem

The previous target-sight repair does not observe arbitrary contract callbacks.
`WorkView.PreviewOrderRequirements` and `WorkBoard.CheckPlayerActivity` directly call
`IPlayerActivityDefinition.CheckIntent`. A mod that changes its target, selected
space or sole clock can return normally from a supposedly pure preview. A thrown
exception outside an update can escape without the normal first-fault latch.
`PostServiceActivity` uses the narrower EntityObservation.Read path instead.

## 1. Goals

One callback admission boundary observes actual activity provider, target state,
selected space and sole clock for proposed diagnostics, shared player admission
and service admission. Mutations and exceptions withhold results and latch the
original engine fault; healthy provider outcomes and physical execution remain.

## 2. Design

Add one internal `ActivityDefinitionContracts.ObserveIntent` method calling the
existing EntityObservation.ReadProvider on the actual activity and whole target
with selected space. Migrate the three existing invocation sites; keep authored
intent rules and the sole WorkBoard evaluator unchanged. No compatibility route.

Extend existing WorkObservationBoundaryTests/WorkObservationProbe. A negative-only
independent activity subtype reuses the existing IndependentActivityContentDef and
ActivityDeclaration parser. It reads an explicit immutable JSON fault selection,
then injects target Touch, clock resume, selected point-space mutation or an
original exception ONLY while the host has paused. No mutable global content
callback and no new runtime state/book/codec. Compare three public surfaces:
requirements, PostOrder command preview and persistent-plan preview.

## 3. API

No public signatures change. Pure callbacks remain mandatory. Bump engine identity
to2.607.0 and work-view106; save159/replay1/state schemas unchanged.

## 4. Files

ActivityContent, WorkPlans, WorkView, ActivityExecutionLifetime; WorkModule and
SaveGame identities; existing protocol expectation, external negative proof and
work-boundary test; stage/report/current tracker.

## 5. Errors

Original observation owner latches mutation or original callback exception; no
successful prefix, no healthy save/hash, no later update or private snapshot leak.

## 6. Compatibility

No shim or old-build support. Remove raw calls at all three owned sites.

## 7. Verification

Reproduce within the existing literal work-observation case, then targeted clean
build and that exact case plus existing activity-graph disclosure and staffed
service literals (service admission is also changed). No
full/profile/family runtime. Preserve every failed attempt in the report.

## 8. Risks

Observation is scoped, not an arbitrary CLR sandbox. Negative content must remain
per-factory immutable and not install shared callbacks into frozen definitions.
Do not replace or duplicate the actual intent or requirement evaluator.

## 9. Out of scope

Full P4.014 closure, unrelated plan callback audit, Oil/Tycoon/platforms/hosts/Draw,
new clocks/serializers, full regression and Git staging/commit/push.

## 10. Stages

- [x] R2.1 Reproduce callback purity/fault bypass across three public surfaces.
- [x] R2.2 Introduce sole observed invocation and migrate actual three sites.
- [x] R2.3 Focused verification and explicit remaining-scope reporting.
