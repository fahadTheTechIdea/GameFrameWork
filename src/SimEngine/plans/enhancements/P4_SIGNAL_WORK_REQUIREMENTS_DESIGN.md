# P4-G2b/G4 — Authored operational-signal work guards

Current shared-reader follow-up2.249.0/save139 removes this requirement's redundant codec wrapper:
AutomationSignalDef.ReadFrom now owns observation for ALL compatible actual source reads. Existing
conditions/evaluator/jobs remain unchanged. Independent physical work/provider fault boundary has
separate scoped evidence, not a rerun of the historical native pipeline below.
[Follow-up stage](P10_SIGNAL_OBSERVATION_BOUNDARY_DESIGN.md),
[evidence](../../docs/migration/P10_SIGNAL_OBSERVATION_BOUNDARY_VERIFICATION.md).

## Specification

Activity DAG nodes must wait on the same typed signals already used by engine automation and
execution gates. Add target/worker signal requirements through IWorkRequirement, not another
predicate parser, scheduler, signal owner, queue, progress model or host decision.

## Design and API

SignalWorkRequirementDef implements ISignalWorkRequirement and existing binding validation.
TargetSignalRequirementDef and WorkerSignalRequirementDef select the actual target/candidate.
The worker variant implements IWorkerWorkRequirement. WorkModule declares JSON sections
target_signal_requirements and worker_signal_requirements.

Each leaf has explicit nonempty ordinal-named conditions, interpreted ONLY by SignalGateCondition:
named source, selected signal, Boolean expected or numeric minimum/maximum/unit, typed blocker.
Use existing all/any WorkRequirementGroupDef for alternatives. Resolve through actual catalog;
composition requires a compatible selected source definition for every condition. Share source
compatibility with existing SignalGateCondition.Validate. No fabricated default or silent source.

Evaluate actual live owned subject, protect signal visibility, read actual named source through
AutomationSignalDef.ReadFrom guarded by existing CaptureProvider/Sync, and delegate threshold/type
semantics to SignalGateCondition.Evaluate. Missing/unavailable signals report typed Pending.
Ownership/liveness and mutation failures remain typed refusals/faults. Existing WorkBoard handles
previews, worker selection, rechecks, progress, requirements, cancellation and save/load unchanged.

## Authored proof and limitations

New master-listed work/operation documents extend existing manufacturing fixture: physical worker
preparation dispatches two real batches once at a node; a zero-work dependent guard waits for actual
backlog zero before final physical verification. Worker Boolean/numeric guards select a genuinely
enabled compatible machine and pause its real job when disabled, retaining earned progress/tools.
The backlog predicate means queue drained, NOT a receipt proving a particular execution succeeded;
cancelled work can also drain a queue. Do not fabricate a completed-production history.

Strict2.248.0/save139, persistent layout unchanged. One exact NEW bounded case: current private/pure
previews, worker selection and pause/resume, node dispatch/disabled queue wait, real physical
production/time/materials, saved interleaved continuation, strict unknown/invalid signal definitions,
all/any composition and missing runtime source. Older identity assertions compile-only.
Build/run only that case, repair observed failures only, freeze source/proof/tests/JSON after green;
docs-only tracker/evidence/scoped inventory afterward. No full/older/family/platform/stale gate.

## Files and scope

SignalWorkRequirements, WorkModule, SignalGateCondition, SaveGame, reusable manufacturing test
fixture helper, new authored proof documents/embedded references, new case/runner and older metadata.
No Oil/host/demo/Draw/platform/business changes; unchanged GameClock. Generic provider signals are
supported, but this case uses native transformer providers. Broader independent provider/fault/
privacy/type/area/haul/team/templates/ETA/replay/stress/CPU/full G2/G4/G5/P1-P12 remain open.
Full goal active; no commit/push until full completion and verification.

## Verified scoped checkpoint,2026-10-08

Implemented2.248.0/save139/persistent layout unchanged. ONE clean build5.53s/0 warnings/errors;
ONE exact NEW bounded case69 checks0 failures/aborts555ms FIRST attempt; no repair.114 older
identity assertion files/lines source/compile ONLY. Source/public proof/tests/JSON frozen after
green; docs-only evidence. No older/family/full/platform/stale gate.
Actual native worker Boolean/numeric any branch, current assignment/pause/resume/held tools,
node dispatch, disabled backlog wait, real timed batches, private pure views, saved twins,
zero-work unsettlement, cancellation/strict authoring/missing-source diagnostics proved.
Only native signal provider runtime proof here; broader scope remains open.
[Report](../../docs/migration/P4_SIGNAL_WORK_REQUIREMENTS_VERIFICATION.md).
