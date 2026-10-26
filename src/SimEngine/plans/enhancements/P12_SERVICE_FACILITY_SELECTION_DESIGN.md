# P12-D6c — Contract-based recurring service facility selection

Current B4i evaluates ALL ready source-worker roles before applying the same public preference,
then captures the selected probe-node role through original ActivityWorkerSelection/PlanIntent.
No separate facility selector or view-only role choice. [Current stage](P9_ROLE_FACILITY_INTENT_DESIGN.md).
Earlier results below are historical, not rerun on2.302.0/save144/board44/work-view86.

Status: focused verified, 2026-10-06; build2.157.0/save84/board27/work19/view48.
Prerequisite: [D6b](P12_HOME_SERVICE_PLANS_DESIGN.md).
Parents: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md), [P12](P12_INCIDENTS_ENVIRONMENT_DESIGN.md).

## 0. Problem statement

Before this slice, RecurringPlanBinding supported only self and a fixed worker home. A machine could not seek an
eligible alternative service facility when home has no stock, incompatible bays or no valid route.
Existing IWorkerDispatch.CopyCandidates already owns these readiness/cost checks; do not duplicate them.

## 1. Goals

1. Add explicit service_facility binding with a master-authored named selection policy.
2. Only owned actual IWorkDock endpoints are candidates; existing admission, stock/tools, selected
   worker, role, area, route, occupancy and queue checks determine readiness.
3. Policy supplies deterministic preference over detached candidate facts; ready offers rank by
   preference, actual travel cost and stable target ID. No provider may admit unsafe work.
4. Authored nearest/home-first and unrelated public policy providers share one selection algorithm.
5. Immutable private candidate diagnostics retain blockers; failed selection returns a typed refusal.
6. A selected target becomes ordinary immutable plan intent; later observations never retarget active
   work or invalidate origin history. Existing jobs alone move, reserve, consume and repair.
7. No additional clock/scheduler/queue/cache/execution; all time remains the sole GameClock.

## 2. Design

IEntityTargetPlanDefinition extends the existing plan contract with provider-owned CreateTargetIntent.
ActivityDef implements it using ActivityWorkerSelection.CreateArguments; delete RecurringPlanBinding's
duplicate JSON writer. Optional public plan providers can implement their own target argument schema.

IActivityFacilitySelection aliases WorkBoard. Its stateless query traverses only actual owned dock
capabilities, reuses ordinary PreviewPlan plus IWorkerDispatch.CopyCandidates for one explicitly
authored positive machine probe node, filters to the source worker and publishes detached readiness/
cost facts. This checks current probe-node readiness, not a promise that all future DAG stages already
have resources. The board retains no selection state or provider caches.

ActivityFacilityPolicyDef/IActivityFacilityPolicy exposes Node and Rank(detached facts). The native
profile authors node and prefer_home; mod profiles return alternative nonnegative ranks over the same
safe facts. Null excludes an otherwise ready offer deliberately. Actual readiness is never overridden.
Schema/content references are checked before play, outputs validated at observation.

Native recurring JSON explicitly requires facility_policy:null or a policy identity for every target
mode; only service_facility may bind a policy. Its node must be in worker_nodes. Resolved definition
metadata uses the actual existing catalog. Recurring request/checkpoint/history owners stay unchanged.

## 3. API surface

IEntityTargetPlanDefinition.CreateTargetIntent; IActivityFacilitySelection.ReadFacilities;
IActivityFacilityPolicy/ActivityFacilityPolicyDef; immutable ActivityFacilityCandidate/SelectionView;
WorkView.ReadRecurringFacilities. Native target gains service_facility; facility_policy is required.
New master-listed activity_facility_policies section. No player instant-service or renderer command.

## 4. Files

New Standard/ActivityFacilities.cs and WorkFacilities.cs. Modify PlanContracts, ActivityWorkerSelection,
Work/WorkModule/RecurringPlans/WorkRecurringPlans/WorkView/SaveGame. Explicit fixture/native content
migration, public policy proof and two focused FacilitySelectionTests. Stage/master/evidence align.

## 5. Errors

Unknown/missing policy/node/required fields fail content load. No safe target returns
work.recurring-no-facility with per-candidate actual blockers. Foreign source reads hide facts.
Negative policy ranks fault; null deliberately excludes a candidate. Worker/target-scoped observation
guards reject the exercised impure dock. Whole-world hidden callback mutations remain unverified.
Dynamic stale requests reuse
request-changed refusal. Removed active endpoints use existing waits/cancel/recovery, never teleport.

## 6. Compatibility

Clean content/protocol replacement; no old loader or argument writer/selector fallback.

## 7. Verification

Clean build before only these two exact suites; same-name repair reruns allowed:

- Service facilities: authored policies, private candidates and deterministic selection
- Service facilities: physical repair, replanning and saved continuation

Strict authoring, pure/immutable/private facts, real route/stock/area/bay refusal, stable ties,
home preference only when safe, public ranking and dishonest policies; actual native/public timed
repair, cancellation/new facility, retained active targets, saved twins/session/sole-clock pause.
Other suites compile only; broad/full/replay/stress remain open.

Final clean build: zero warnings/errors, MSBuild3.59s. Contracts101 passed/0 failed/458ms;
physical65 passed/0 failed/549ms:166 checks/two exact suites, zero aborted suites.
[Evidence](../../docs/migration/P12_SERVICE_FACILITY_SELECTION_VERIFICATION.md).

## 8. Risks

Readiness offers are not claims. Concurrent work can invalidate them, so normal admission/acquisition
rechecks remain authoritative. Probe-node evaluation is explicitly authored; future stages still wait
through the graph. A preferred home is a ranking preference, never permission to bypass safety.

## 9. Out of scope

Mutable facility preference controls, global optimization/congestion forecasts, standby/preparedness/
fuel/traffic/incident science, full provider/replay/stress/performance gates. No host/demo/Tycoon/Oil/
platform/Draw work or independent time/update loop.

## 10. Execution order

- [x] Inspect existing owners and record design.
- [x] Implement target intent/query/policy contracts and native recurring binding.
- [x] Add authored fixtures, public policy proof and bounded focused tests.
- [x] Clean build/two exact gates, ownership review and tracker/evidence.
