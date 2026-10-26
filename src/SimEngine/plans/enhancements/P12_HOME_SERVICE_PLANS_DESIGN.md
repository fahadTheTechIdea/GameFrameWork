# P12-D6b — Home-targeted recurring service and plan provenance

Status: focused verified, 2026-10-06; build2.156.0/save83/board27/work18/view47.
209 checks/two exact suites (136 contracts;73 physical), zero failures/aborts, clean build.
[Evidence](../../docs/migration/P12_HOME_SERVICE_PLANS_VERIFICATION.md). Parent: [P12](P12_INCIDENTS_ENVIRONMENT_DESIGN.md),
[P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md); prerequisite: [D6](P12_SELECTED_SERVICE_WORKERS_DESIGN.md).

## 0. Problem statement

Standard/RecurringPlans.cs hardcodes the source entity as target and cannot express an unavailable
request without fabricating intent. Standard/WorkRecurringPlans.cs validates historical postings
against today's intent, so a legitimate changed worker home would invalidate a checkpoint.

## 1. Goals

1. Explicit JSON selects self or worker_home and named machine nodes; no missing-field default.
2. A worker_home request binds those nodes to the actual source worker and targets its actual home.
3. Missing/removed/foreign/unsupported homes return typed refusal, not fake targets or instant repair.
4. Existing plans retain one source origin; changing home never rewrites active work or invalidates history.
5. A source cannot post another plan while its last admitted plan is live, even after home changes.
6. Native/public workers and conditions use physical resources, bay travel, work and departure.
7. Save/load, private previews, cancellation and cadence use the sole GameClock and existing owners.

## 2. Design

Replace IRecurringPlanSource.ReadIntent with ReadRequest returning immutable RecurringPlanRequest:
exactly one PlanIntent or typed refusal. RecurringPlanContracts validates provider purity, metadata,
current request and history separately. RecurringPlanView exposes RequestBlocker alongside the effective
schedule/trigger/admission blocker. Recheck current request before submission; changed requests refuse.

RecurringPlanBinding owns strict authored target and canonical worker_nodes. Optional definition
contract IRecurringPlanBindingDefinition exposes it. WorkerHomeContracts.ReadHome observes IWorker.Home
through its existing pure binding guard; CheckHome remains the only home validation protocol.
ActivityWorkerSelection.ValidateMachineNode remains the shared positive machine-node validator.

PlanOrigin(entity,source) is embedded once in the existing PlanEntry codec/private PlanView. Only normal
recurring admission records it; player-created/repeated plans have no origin. Historical validation
checks actual plan ID, source origin, company, definition and original posting time, not today's target.
Existing WorkBoard/WorkSystem alone submit/promote/dispatch/execute; no service queue, clock or scheduler.

## 3. API surface

RecurringPlanRequest.Ready/Refused; IRecurringPlanSource.ReadRequest; RecurringPlanView.RequestBlocker;
RecurringPlanBinding and IRecurringPlanBindingDefinition; WorkerHomeContracts.ReadHome; PlanView.Origin;
RecurringPlanView.Entity identifies the actual observed source alongside its named capability.
Native JSON requires target:"self"|"worker_home" and worker_nodes:[]|["service",...]. Worker-home
requires nonempty selected nodes and an actual worker capability. Existing commands control home,
recurring settings and plans; no repair command or host gameplay.

## 4. Files

Modify Standard/RecurringPlans.cs, WorkRecurringPlans.cs, WorkerHomes.cs, ActivityWorkerSelection.cs,
PlanContracts.cs, WorkPlans.cs, Work.cs, WorkModule.cs, Kernel/SaveGame.cs; public recurring proof,
explicit fixture JSON, focused HomeServiceTests and test registration. Update stage/master/evidence.

## 5. Errors

Invalid authoring fails before play. Missing home uses work.recurring-no-target; changed request uses
work.recurring-request-changed; existing live posting uses work.recurring-outstanding. Actual home
refusals reuse Codes.NotFound/NotOwned and WorkerHomeContracts.Unsupported. Malformed provider requests,
forged origins or history acknowledgements reject rather than substitute.

## 6. Compatibility

Clean public protocol, content and checkpoint replacement. No old request method/loader/adapter.

## 7. Verification

Clean dotnet build Beep.SimEngine.slnx before only these two exact suites (same-name repair reruns allowed):

- Home service: authored requests, source origins and admission
- Home service: recurring physical repair, home edits and saved continuation

Strict binding/missing-home/private/pure/provenance/public-provider cases plus bounded native/public
physical repair, retained active target, cadence, cancellation, saved twins and sole-clock pause.
All other tests compile only. Full regression/replay/stress remain open.

## 8. Risks

Dynamic intent must never serve as historical identity. An edited home applies only to future postings.
Origin is provenance, not another execution/resource claim. Removed sources may remain historical IDs.

## 9. Out of scope

Nearest/preferred facility search, fuel/traffic/preparedness/standby/science, global callback rollback,
full providers/replay/stress/framework gates. No OilFieldDays/Tycoon/host/demo/platform/Draw work.

## 10. Execution order

- [x] Inspect owners and record this stage.
- [x] Implement request/binding/home contracts and existing plan provenance/admission.
- [x] Migrate explicit fixtures/public provider and add bounded two-suite proof.
- [x] Clean build, exact gates, review and evidence/tracker update.
