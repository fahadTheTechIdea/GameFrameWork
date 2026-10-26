# P10-A7 — authored utility readiness in the activity DAG

Current declaration migration: [P11-A3](P11_UTILITY_RESEARCH_DESIGN.md), core2.374.0.
UtilityWorkRequirementDef below describes historical A7 only; that base is now deleted.
Native leaves and direct independent contracts share UtilityWorkRequirementDeclaration;
UtilityContracts.Read guards the actual codec. Historical A7 suites were not rerun.

Status: native focused slice complete on2.143.0/save76. Existing views/codecs unchanged.
Clean build;291 checks/two exact requirement-expression suites (129 admission;162 execution), zero
final failures/aborts. [Evidence](../../docs/migration/P10_UTILITY_WORK_REQUIREMENTS_VERIFICATION.md).
Framework only; existing WorkBoard evaluation and execution own state. Full P10 is not complete.

## 0. Problem and acceptance

Resource utility consumers and physical network admission now exist, but authored activity nodes cannot
declare a named target/worker consumer as a readiness requirement without a custom provider. Extend
the existing open requirement registry, not the scheduler, resource balance or utility consumer owner.

1. Explicit target_utility_requirements/worker_utility_requirements contain stable id and required
   consumer capability identity. Native derived definitions implement IWorkRequirement; worker-scoped
   definitions also implement existing IWorkerWorkRequirement. No implicit consumer, alias or legacy path.
2. One UtilityWorkRequirementDef reads the actual named IUtilityConsumer through UtilityContracts.
   Actual company/live identity and provider purity are validated. Missing consumer, inactive execution,
   representation blocker and shortage are typed pending results. Foreign owner is blocked. Satisfied
   demand is satisfied; do not fabricate a single combined quantity for heterogeneous resources.
3. Existing all/any groups, candidate selection, previews, active diagnostics, pause/resume, physical
   travel/work, choices, cancellation, save/hash and the sole GameClock use the same leaf evaluation.
   No WorkBoard/WorkSystem type case, resource reservation or double utility consumption is added.
4. Prove native timed consumption and independent public-contract target/worker consumers. Physical
   inbound stock, outbound promises and ordinary operation controls change readiness; no test scheduler.
5. Clean build then exactly two existing requirement-expression suites. Other suites compile only;
   no third/family/full gate. Update stage/evidence/master/parent/AGENTS/README/help consistently.

## 1. Source/content and execution order

Standard/UtilityWorkRequirements.cs owns strict authoring and shared readiness semantics; target and
worker subclasses select context.Target/context.Worker only. WorkModule registers native sections.
Existing UtilityContracts validates actual detached facts/stock/units. No new service, system, codec,
mutable request or clock. Explicit embedded/master-listed utility-work.json contributes requirements,
groups, activities and a physically mobile independent worker. Existing native/public consumer fixture
and actual network flow are reused. Tests attach to the existing two named expression suites.

1. Register authored leaf contracts and shared pure reader.
2. Add explicit JSON and public-only proof through existing modules.
3. Prove pure/private admission, alternatives, candidate scope and physical paused/saved execution.
4. Clean build, two exact gates and current documentation; preserve historical evidence.

All four native tasks are complete. Actual candidate previews may select an eligible machine;
explicit workerless contexts still report the existing no-worker blocker. Shortage proof guards
already queued physical pool stock as well as closing its source; closing a source is not teleportation
or removal of queued supply. Directly exercised older graph/expression fixture assumptions are updated
against the current authored wrappers, JSON and actual worker-busy/arrival semantics, not old ordering.

## 2. Semantics and limits

Follow-on [A8 motion utility consumers](P10_MOTION_UTILITY_CONSUMERS_DESIGN.md) extends actual motion
inputs to this generic contract; its focused motion gate is not full activity/network integration proof.

This is a readiness guard, not another utility demand, reservation or consumption ledger. To require
held/consumed work resources, author existing resource requirements; utility consumers remain actual
engine-side owners. A readiness guard does not create a missing consumer, start an inactive operation,
burn resources itself or guarantee supply for future steps. All/any composition retains existing stable
selection and committed-choice behavior. Workerless previews retain the existing no-worker blocker.
Malformed/forged/mutating provider facts fault, never silently become readiness. Missing capability is
explicit pending utility.missing-consumer, permitting physical restoration through normal engine actions.

No persistent layout changes: save76 and existing work/consumer/network codecs remain authoritative.
Exact build/content identity changes. Electrical/thermal/pressure physics, aggregate bandwidth, global
utility priorities, graph-provider breadth, wider consumer implementations, rollback/replay/stress/
allocations and full P10/P1–P12 acceptance remain open. No Oil/Tycoon/host/demo/platform/Draw changes.
