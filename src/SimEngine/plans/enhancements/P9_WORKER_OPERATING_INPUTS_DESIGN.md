# P9-B10 — Authored travel, idle and standby operating inputs

Status: focused verified, 2026-10-07; build2.170.0/save95/work view53/motion view6/work28/movement6.
279 assertions/two exact suites (135 contracts;144 physical), zero final failures/aborts; clean build.
[Evidence](../../docs/migration/P9_WORKER_OPERATING_INPUTS_VERIFICATION.md).
Parent [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md);
shared [P4](P4_REUSABLE_MODULES_DESIGN.md) elapsed inputs and [P10](P10_NETWORKS_AUTOMATION_DESIGN.md) utilities.

## 0. Problem statement

MotionOperatingInputs.cs owns exact cumulative input exposure for movement but cannot author idle
or standby expenditure. Copying its calculator/transaction path for workers would create competing
responsibilities. Extend that existing owner into phase-neutral OperatingInputs.cs and retire the
old contracts, component key and concrete types directly. WorkSystem remains the worker loop.

## 1. Goals

1. Explicit master-listed JSON declares a named movement, idle or standby phase and resource rates.
2. One exposure, quantization, combined-admission and commit protocol serves all three phases.
3. Each phase uses accepted exposure derived from the sole session clock, never another timer.
4. Inputs consume actual unpromised inventory; shortage consumes/advances nothing and replenishment
   resumes the same exposure without rounding reset or fabricated fuel.
5. Native and public providers, private detached views, physical work/travel and saves prove parity.

## 2. Design

Promote IMotionOperatingDefinition/Inputs and their context/snapshots/coordinator in place to
IOperatingInputsDefinition/IOperatingInputs, OperatingPhase and OperatingInputsContracts. Definitions
declare Phase explicitly. InventoryModule owns the operating_inputs component registration because
all phases consume the same existing inventory/transaction authority. MovementSystem calls the same
coordinator for Movement; WorkSystem calls it for Idle/Standby, not another system or roster.

Movement retains accepted active-attempt semantics. Idle/standby require an actual unassigned worker
with no active movement. Standby means an authored disabled work policy or emergency-only/self-service
assignment mode; otherwise the phase is idle. Held stationary workers still incur their authored
parked costs. Busy work, active travel and unrelated non-worker entities never incur idle costs.
WorkSystem captures only workers inactive at the start of its update, then rechecks after dispatch
and home/cargo intent. This avoids charging a completed productive interval as idle or charging
idle and travel for a newly acquired route. A shortage never forbids unrelated work from starting;
the operating-input phase is not a second generic worker readiness gate.

Each named provider owns one cumulative accepted exposure and last accepted game-clock coordinate.
Different phase providers therefore retain independent cumulative rounding across phase switches,
but cannot advance game time. ResourceRates and IResourceTransaction remain sole calculators and
stock mutation owners. Existing utility discovery/signals/network supply reuse the same consumer.
Private WorkView reads phase facts from the same coordinator; hosts do not determine phase or cost.
Entity.Update alone settles the declared blocked state when its current operating phase is unpaid.

## 3. API surface

OperatingPhase: Movement, Idle, Standby. IOperatingInputsDefinition.Phase/Inputs;
IOperatingInputs.ReadExposure/AdvanceExposure, existing IUtilityConsumer aliases;
OperatingInputsContracts.Read(world,entity,phase), ReadDemand and CurrentWorkerPhase;
WorkView.ReadOperatingInputs(entity,phase,out OperatingInputEvaluation).
JSON operating_inputs requires id, phase, inputs and states.disabled. No old key/default phase reader.

## 4. Files to change

- Move/generalize Standard/MotionOperatingInputs.cs to Standard/OperatingInputs.cs.
- Update MotionExecution/Movement/Work/InventoryModule/MotionModule/WorkModule/ResourceTransactions
  and WorkView; existing owners only.
- Directly migrate public proof providers, callers and explicitly embedded motion JSON.
- Add master-listed worker operating-input JSON, focused tests and current evidence/docs/tracker.

## 5. Errors and durability

Missing/unknown phase, missing role target/inventory/rate/resource, duplicate named capability,
impure/malformed demand, false exposure commit and changed inventory refuse/fault explicitly.
Pure shortage and range return typed blockers, not partial burns. Actual provider mutation faults
the existing session; no compensation or resumable partial-success checkpoint. Native codec stages
both exposure fields before publishing. Paused sole clock performs no updates or expenditure.

## 6. Compatibility

None. Rename all engine/test/public-proof callers and JSON directly; remove old type/key paths.
Build identity and affected view/system schema identities change. Kernel save95 stays unchanged:
the generic component retains the same two-long exposure payload and is a new declared component ID.
Old builds/content already refuse through strict engine/content identity; no save adapter is added.

## 7. Verification

Build Beep.SimEngine.slnx, then exactly these two names (same-name repairs permitted):

- Operating inputs: phases, public contracts and atomic resource admission
- Operating inputs: physical work, standby resupply and saved continuation

No third/family/full suite. Other migrated suites compile only. Record current fingerprints and
bounded diagnostics; prior slice results remain historical/not rerun.

## 8. Risks

Readiness is not a trip budget, guaranteed service or simultaneous team supply. Accepted exposure
is not proof of physical distance. Empty input arrays are explicitly authored no cost, not a default.
Large-fleet allocations, hostile global-provider mutation and broader utility matrices remain open.

## 9. Out of scope

Fuel prediction, compulsory worker needs, population, platforms/Draw, Oil/Tycoon, second clock,
parallel inventory/scheduler, reserved money, full regression and full framework completion.

## 10. Execution order

- [x] B10.1 generalize and directly migrate the sole input owner and authored callers.
- [x] B10.2 integrate inactive worker phases/private views and checkpoint validation.
- [x] B10.3 master-listed native/public proof, physical resupply and two focused gates.
- [x] B10.4 source evidence, ownership review, stage/master/README/AGENTS/help update.
