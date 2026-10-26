# P12-D5 — Physical worker-scoped condition service

Status: focused verified, 2026-10-06; build2.154.0/save81/board25/work16/view45.
105 checks/two exact suites; full fleet orchestration remains open.
[Evidence](../../docs/migration/P12_WORKER_SERVICE_EFFECTS_VERIFICATION.md).
Parent: [P12](P12_INCIDENTS_ENVIRONMENT_DESIGN.md);
physical facility ownership: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).

## 0. Problem statement

Before this stage, condition requirements/effects addressed only Order.Target. A service bay could therefore
repair itself, but not the actual machine that travels there and completes its authored activity.
CompleteNode released the job/machine before completion effects; execution identity was lost.

## 1. Goals

1. The sole ActivityNodeExecution records CompletedBy only after its actual positive-time machine
   job has earned all work and completed any required physical departure.
2. Existing condition requirement/effect logic supports target and worker scopes through narrow
   authored provider types without another condition, receipt, service queue or scheduler.
3. A worker condition requirement selects actual compatible damaged candidates; healthy machines
   cannot consume the bay's spares. Worker effects apply to the recorded finisher, never the facility.
4. Timer/zero-time nodes and aggregate completion cannot author worker effects without a real actor.
5. Private node views, checkpoint validation, save/hash and native/public providers retain identity,
   physical timing, supplies/tools, cancellation, pause and exact once-only repair.

## 2. Design

Extend existing node record/embedded Sync with CompletedBy. CompleteNode captures its actual live
job actor before release, validates complete progress/itinerary and actual IWorker assignment, then
uses the existing completion/effect owner. No timestamp, actor registry or parallel receipt. Positive
machine Completing/Done nodes require this identity; all other nodes must not fabricate it. Historical
receipts may outlive a removed actor; live effects still require the actual owned worker/provider.
The receipt lives only in its existing authoritative node. Ordinary ended-order pruning removes it;
no separate completion history, retention policy or shadow ledger is added.

PhysicalWorkEffectDef adds a virtual effect-subject resolver and virtual plan validator. Existing
target effects retain their target; response/order validation always uses the original activity
target. Apply captures the checked subject once through shared CheckSubject and existing scoped pure/
ownership/phase guards; it never resolves a different subject for commit. ConditionEffectDef
is the sole condition parsing/validation/change implementation; WorkerConditionEffectDef selects the
recorded actor and restricts authoring to its own positive-time machine node. WorkerConditionRequirementDef
inherits the same condition-range parser/evaluator and implements existing IWorkerWorkRequirement,
selecting context.Worker rather than the target. Target variants remain legitimate capabilities, not
compatibility paths. New sections are explicit worker_condition_effects/worker_condition_requirements.

## 3. API surface

- ActivityNodeExecution.CompletedBy and immutable ActivityNodeView.CompletedBy.
- PhysicalWorkEffectDef.ResolveSubject(World, Order, node), protected virtual.
- WorkerConditionEffectDef/WorkerConditionRequirementDef use existing public work/condition contracts.
- Existing condition-effect/range JSON fields remain required; worker scope is explicit in section
  identity, never an inferred/default subject. No instant player repair command.

## 4. Files

Standard/Work.cs, WorkEffects.cs, EquipmentCondition.cs, EquipmentConditionModule.cs, WorkModule.cs;
Views/WorkView.cs, Kernel/SaveGame.cs; new public service condition module, explicit test JSON and
WorkerServiceTests. No host/demo/platform implementation.

## 5. Errors

Missing/unearned/future/illegal completion identities reject checkpoints. Worker effect on timer,
zero-time/aggregate completion fails content validation. Missing actor/provider/foreign owner/invalid
condition units/bounds refuse; dishonest condition commits fault the existing normal update. No
fabricated actor, clamped repair or remote service fallback. Existing embedded node decode and
whole-board restore validation remain authoritative; this stage does not prove atomic arbitrary decode.

## 6. Compatibility

Clean node codec/build/save identity replacement; no old receipt loader or compatibility shim.
Target and worker authored providers share one implementation per responsibility.

## 7. Verification

Clean build then exactly two complete named suites; same-name repairs allowed:

- Worker service: authored subjects, candidate requirements and completion ownership
- Worker service: physical bays, materials, repair and saved continuation

Prove positive-time admission, target/worker distinction, native/public candidate selection and
condition owners, private/pure views, receipt guards, actual travel/work/departure, spares/tools,
pause/cancel, no early/duplicate repair, provider faults, saved twins/session isolation/sole-clock pause.
All physical waits bounded with diagnostics. No third/family/full test run.

## 8. Risks

Interrupted work can be reassigned; CompletedBy identifies the actual final executing job actor,
not every historical contributor or an explicit fleet subject. A complete-progress job still waiting
to depart has no completion identity and must not repair early. Removed historical actors remain
historical facts, not alternate live targets.

## 9. Out of scope

Explicit player-selected fleet subjects, facility selection/service-cadence dispatch, multi-actor
repair teams/contributor ledgers, fuel service/route forecasts/traffic/preparedness, global callback
purity/replay/stress/full P9/P12/framework. This stage is a real physical service prerequisite, not
completion of all fleet policies. No Oil/Tycoon/host/demo/platform/Draw or second game clock.

## 10. Execution order

- [x] Add authoritative completion identity/Sync/views and subject resolver.
- [x] Extend shared condition providers with explicit worker sections.
- [x] Clean build: zero warnings/errors; two gates pass28+77 checks, zero failures/aborts.
- [x] Record evidence/update tracker and keep full fleet/framework acceptance open.
