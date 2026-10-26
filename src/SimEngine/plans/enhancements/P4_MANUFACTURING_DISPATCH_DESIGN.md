# P4-G4d — Authored manufacturing dispatch

## 0. Specification

Physical manufacturing already belongs to ITransformer and its actual queue/WIP. Authored activity
graphs lack an engine-owned way to dispatch production after preparation. Add a JSON work effect
through existing effect admission; do not recreate manufacturing, scheduler, resource or queue state.

Success: an ordinary authored target activity performs parallel physical preparation and dispatches
an exact named production request once. Existing queue processing spends real materials and time.
Player commands, effects and management previews share one contract-based queue coordinator.
No budget/business requirement, instant production, host logic, new saved state or compatibility path.

## 1. Design and API

ITransformationQueue aliases SAME TransformerManager. CheckEnqueue preserves existing company,
content, named capability, supported definition and provider checks. Enqueue rechecks current state,
then calls actual ITransformer.Enqueue once and validates exact appended entries/unchanged prefix
and processing state. Ordinary EnqueueTransformation handler and management preview migrate to
same contract. No old static enqueue path, alternate queue or retained readiness authorization.

TransformationDispatchEffectDef : PhysicalWorkEffectDef, IWorkEffectIntentAdmission is declared
by TransformationModule in transformation_dispatch_effects. Required JSON: transformation,
capability, count, repeat, home (none or target). Repeat requires count1; no inferred capability,
home, transformation or quantity. It aliases selected TransformationDef, requires positive-time
machine ancestry and actual queue service. Exact entity-activity target validates supported named
transformer definition. Intent/start/preview/completion use the SAME queue admission. Completion
queues work only; it never advances production or consumes/creates materials directly.

Existing WorkBoard effect status owns exactly-once dispatch. Existing transformer queue/Sync owns
all subsequent production; cancelling dispatch before completion adds nothing, cancelling after
dispatch does not secretly cancel a different owner's physical queue.

## 2. Files and errors

TransformationContracts/Transformations/TransformationModule/TransformationManagementView,
new TransformationDispatchEffect, strict EngineInfo build2.245.0/save139. Existing assertion metadata
migrates source-only. New native JSON master/world/startup/manufacturing/work documents and ONE
bounded player case; no driver gameplay or external provider scheduler. No changed persistent bytes.

Missing/unknown/invalid JSON, wrong named target or timer-only physical ancestry fail explicitly.
Queue refusal causes ordinary typed effect result; provider wrong append/state mutation faults,
not compensation or silent success. Unselected transformations remain optional with no Work dependency.

## 3. Verification and sequencing

1. Canonical queue coordinator and all enqueue consumers migrate in place.
2. Register strict authored physical dispatch effect and native content proof.
3. Build completed stage; ONE distinct exact NEW bounded case: preparation/holds/private previews,
   cancellation before dispatch, exactly-once queue append, saved continuation before/after dispatch,
   physical WIP/material/time, disabled processing, ordinary command parity, strict authoring.
4. Record every build/run; repair observed failures only. After final green freeze source/proof/
   tests/JSON; docs-only tracker/evidence/hash-link inventory.

No older/family/full/platform/stale gates. Provider violation/global purity, continuous/repeat/home/
external-provider breadth, completion-wait executors, team/haul/generated/non-tile/template workflows,
ETA/replay/stress/CPU/full G3-G5/P1-P12 remain open. Full goal active, no publication until FULL done.

## 4. Verified scoped checkpoint,2026-10-08

Build2.245.0/save139/all persistent bytes unchanged. THREE clean builds8.61s/4.93s/5.13s;
THREE runs of SAME exact NEW case:2 checks1 aborted286ms (new optional auto_repeat:null fixture),
22 checks1 aborted545ms (queue Touch legitimately advances tool inventory's entity-backed revision),
then68 checks0 failures/aborts536ms. Repair only observed JSON and new append check: compare actual
tool commitments, not unchanged enclosing revision.111 older identity assertion files/lines compile
ONLY. Source/public proof/tests/JSON frozen after final green; docs-only afterward.
[Evidence](../../docs/migration/P4_MANUFACTURING_DISPATCH_VERIFICATION.md).

Ordinary public plan/command/control/views and saved actual tool/work/WIP/queue execution proved;
strict authored quantity/home/reference/named-target and timer-only refusal proved. No external
provider mutation-fault/continuous/repeat/home/entity-output/full G4-G5 acceptance claim.
