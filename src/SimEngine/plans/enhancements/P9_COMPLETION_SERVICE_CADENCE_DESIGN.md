# P9-B11.3c1 — Successful-completion service cadence

Status: c1 focused verified, 2026-10-07. Parent B11.3c and the full goal remain open.
Build2.191.0/save104/board36/work33/work view66/native recurring8.
[Current evidence](../../docs/migration/P9_COMPLETION_SERVICE_CADENCE_VERIFICATION.md).

## 0. Problem

RecurringPlanContracts.Read currently measures every interval from LastPostedTick. A service
job may spend that entire interval travelling, queuing or waiting for spares. Completion must
start the service interval; failures/cancellations must not acknowledge successful service.

## 1. Goals

1. Explicit JSON selects submission or successful_completion cadence with no default/old reader.
2. Existing source stores only its last successful native cycle ID, not a copied completion time.
3. WorkBoard derives completion time from all actual cycle members and protects referenced history.
4. Existing command/private views, save/hash and scheduling use that same relation.
5. Condition effects support authored bounded restoration targets through timed physical work.
6. Existing machines, resource/tool claims, service docks and travel remain execution authorities.

## 2. Design

Extend IRecurringPlanDefinition with a stable authored cadence and IRecurringPlanSource with
exact completion acknowledgement. RecurringPlanSchedule carries LastCompletedPlan. The source's
existing Sync embeds that ID; WorkBoard retains the actual cycle and reads the latest EndedTick
only when every member is Done. It captures acknowledgements before ordinary recurring admission.
Failed/cancelled attempts retain the prior success and use posting-based retry cadence only.
No success is fabricated from partial work, repair effects or posting. Whole-cycle completion
includes physical departure and later DAG stages. Disable/home/interval edits do not rebase history.
Between board settlement and the next source observation, a typed CompletionPending refusal
reports the actual whole-cycle deadline without mutating acknowledgement or pretending readiness.

ConditionChangeKind.Restore means repair to at least an authored target, never lower condition,
exceed maximum or add an arbitrary delta. The existing condition contract supplies one value-after
calculation and exact provider verification. Existing worker_condition_effects can select restore;
their positive-time physical machine-node requirement and resource requirements remain mandatory.

## 3. API

RecurringCadenceBasis; IRecurringPlanDefinition.Cadence; RecurringPlanSchedule.LastCompletedPlan;
IRecurringPlanSource.RecordCompletion; RecurringPlanView.Cadence/LastCompletedTick; Restore condition
intent. No new service scheduler, fleet model, condition store, route owner or clock.

## 4. Files

Existing RecurringPlans, WorkRecurringPlans, WorkPlanHistory, EquipmentCondition and module/view
metadata; public recurring/condition proof providers; explicitly migrated authored fixtures;
two focused completion-service suites, stage evidence and master tracker.

## 5. Errors

Missing/unknown cadence fails composition. Missing/foreign/failed completion references fail
checkpoint validation. Outstanding work has unknown completion deadline. Ineligible restoration
targets and invalid providers refuse/fault. Supplies/tools retain native physical claims.

## 6. Compatibility

Direct content/provider/source-codec/build/save migration; no defaults, legacy reader or shim.

## 7. Verification

Build and only two named completion-service suites covering pure/invalid/private admission,
whole-cycle cadence, cancelled/failed attempts, history retention, normal settings, actual machine
travel/work/material/tool usage, exact restoration and saved continuation. No full/family gate.

## 8. Risks

This is the successful-completion scheduling prerequisite, not all fleet maintenance acceptance.
Early condition overrides, policy/trip coordination and broader native queue/provider stress remain
separate required work. Generic source history must retain actual success IDs until superseded.

## 9. Out of scope

Oil/Tycoon/hosts/demos/platforms/Draw, instant repair, independent current-time owners or copied ages.

## 10. Stages

- [x] Explicit native/public cadence, actual success reference/acknowledgement and history retention.
- [x] Bounded target restoration through existing condition effects and physical work.
- [x] Focused physical/private/command/save proof and current evidence/tracker: clean build;
  208 assertions/two exact suites (92 admission, 116 physical), zero final failures/aborts.
- [ ] B11.3c2 integrated maintenance/operation/fleet readiness and broader B11.4 acceptance.
