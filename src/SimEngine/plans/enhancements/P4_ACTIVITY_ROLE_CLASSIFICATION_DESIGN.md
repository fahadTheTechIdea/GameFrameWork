# P4-G55: Sole authored activity-role classification — Design Document

## 0. Problem Statement

Executor.RequiresWorker duplicates the captured node Roles and permits a metadata callback to disagree with both pre-world graph ancestry/staffing and runtime scheduler decisions. Prior observation guards detect some mutations but leave two competing classifiers. ActivityGraph additionally invokes ValidateDefinition while its captured Node still lacks requirement, effect and canonical dependencies.

## 1. Goals

1. Delete executor worker-classification callbacks and retired runtime observer path, without shims.
2. ActivityExecutorContracts.RequiresWorker derives participation solely from captured authored role alternatives; all graph, staffing, scheduling, acquisition, productive work and checkpoint consumers use it.
3. Native executor strategies still validate their required role shape. Custom validators receive complete captured fields and bound canonical dependencies.
4. One new exact bounded case covers native/direct physical/timed DAGs, typed early refusal, manufacturing staffing, saves/twins/pause/replay/privacy/isolation.

## 2. Design

Remove IActivityExecutor.RequiresWorker and implementations, WorkBoard's callback wrapper and ReadWorkerRequirement. Keep detached RequiresWorker offer/view facts. Derived classification belongs to ActivityExecutorContracts, while authored graph roles remain the sole immutable data source. ValidateRoles uses that classifier; staffed/fixed/timed strategies continue enforcing their own shape.

ActivityGraph captures every node completely, compiles dependency topology, binds canonical edges, then invokes ValidateDefinition before machine-ancestry derivation/publication. No world, worker, clock or duplicated classification state is created.

The previous executor-bool metadata test is revised in place with a new exact case identity; obsolete callback fault probes are removed, not left as dead test logic. Existing master-listed executor and staffing fixtures/player helpers are reused. A diagnostic definition-inputs flag checks complete captured metadata; normal execution remains the shared engine strategy.

## 3. API surface

Breaking deletion of IActivityExecutor.RequiresWorker and concrete implementations. New public pure ActivityExecutorContracts.RequiresWorker(IActivityNodeDefinition) reads role data only. Existing offers keep detached RequiresWorker facts. No compatibility accessor or platform API.

## 4. Files to change

| Action | Files | Risk |
|---|---|---|
| Modify | ActivityExecutors.cs, ActivityGraph.cs, TransformationStaffing.cs | medium |
| Modify | Work.cs, WorkScheduling.cs, WorkAcquisition.cs, WorkStart.cs, WorkDocks.cs | medium |
| Modify | Existing executor proof metadata test/module, shared staffing helpers | low |
| Modify | Historical authored executor assertions compile-only and protocol identity | low |

## 5. Error handling matrix

| Input | Result |
|---|---|
| Missing/duplicate/unknown roles | Typed content refusal, no implicit machine classification |
| Staffed/fixed executor with empty roles | Native strategy rejects before Initialize |
| Timed executor with roles | Native strategy rejects before Initialize |
| Missing/cyclic dependencies | Sole dependency compiler rejects |
| Invalid custom validator | Typed first definition error before world initialization |
| Machine staffing bound to a timed node | Original staffing reader rejects derived role shape |

## 6. Backward compatibility

Development only; no legacy/shim. Core2.399.0/save154/replay1, persisted layouts unchanged.

## 7. Verification

Build completed slice; ONE NEW exact bounded case: Activity roles: authored nodes solely classify work and validate complete metadata. Outer30000ms/owned termination5000ms/output30000chars. Repair/rerun this same case only. No prior/family/full/Oil/Tycoon/platform runtime. Full regression remains open.

## 8. Risks

Roles declare whether a node needs a worker; custom executors may validate additional authored constraints but cannot supply a second boolean classifier. Complete validation timing is a breaking order change. This does not prove arbitrary custom validators' purity or every provider/reentry/performance/global scenario.

## 9. Out of scope

Protected clock, excluded Tycoon files, Oil/hosts/Draw/platforms; full original P1-P12 gates remain open. Publishing remains deferred until full plan/goal completion.

## 10. Sub-task execution order

1. Remove callback classifier and migrate all original consumers.
2. Validate complete captured nodes after canonical edges bind.
3. Retire obsolete callback probes and reuse JSON/player fixtures for the new exact proof.
4. Build and run only new case; record all attempts and source identity.
5. Reconcile current/historical trackers without changing compound requirements.

## P4-G55 delivery — scoped, not phase completion

[Stage](P4_ACTIVITY_ROLE_CLASSIFICATION_DESIGN.md), [evidence](../../docs/migration/P4_ACTIVITY_ROLE_CLASSIFICATION_VERIFICATION.md). Core2.399.0/save154/replay1: authored immutable roles solely classify worker participation; executor boolean callbacks and wrapper removed. Fully captured nodes with canonical edges validate before ancestry/staffing composition. Original graph/work/queue/resource/clock owners retained. FIRST/FINAL84/0/0 in1124ms; clean14.61s build. Native/direct physical DAGs, alternative machine staffing, saved twins/pause/replay/privacy and strict early metadata refusals are narrowly proven. Previous cases are historical, not rerun; arbitrary validator/provider/reentry/performance/global/full gates OPEN. No compatibility path or gameplay in drivers. Original compound requirements/checkboxes unchanged; publication deferred.
