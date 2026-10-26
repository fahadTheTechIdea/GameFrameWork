# P12-D3 — Recurring activity intents for inspection and maintenance

Current source follow-on: [P9-A4](P9_AUTHORED_RECURRING_INTENTS_DESIGN.md) directly replaces the activity
field/definition reference with full plan keys resolved by WorkBoard and explicit arguments. Native/
public sources no longer republish IActivitySource graphs. Original D3 evidence below is historical;
current2.171.0/save95 proof includes the promoted recurring cases plus physical authored construction.

Status: historically focused verified, 2026-10-06; build2.101.0/save64/source codec1/work view30/work system2.
**96 checks/two exact suites** (70 contracts;26 physical), zero failures/aborts, clean build.
[Evidence](../../docs/migration/P12_RECURRING_MAINTENANCE_VERIFICATION.md).35 earlier identity assertion
files compile only; preceding suites were not rerun. Full P12/P1–P12 remain partial.

Current source is [P12-D3b](P12_EDITABLE_RECURRING_POLICIES_DESIGN.md): build2.152.0/save80/native source3/
public proof2/work15/work view43 replaces separate controls with shared settings including editable
interval. Existing required operation_policy:null/ID from [D4](P12_OPERATION_POLICIES_DESIGN.md) and
linked readiness/criticality remain. The same two recurring suites now pass324 checks (191 contracts;
133 physical); D3's96 checks are historical/included, not additive. [Current evidence](../../docs/migration/P12_EDITABLE_RECURRING_POLICIES_VERIFICATION.md).

## 0. Problem statement

EquipmentCondition owns real damage/exposure and physical repair effects. WorkBoard owns persistent
plans and actual jobs, but nothing can author recurring inspection/service intent. WorkSystem.Update
currently calls PromotePlans at400; SettlePlans/Prune run in CompleteUpdate. Extend these owners rather
than adding a maintenance scheduler, shadow job, condition balance or client decision.

## 1. Goals

1. Master-listed JSON authors a named recurring_activity capability: real activity reference, positive
   aligned interval, explicit enabled/priority/group and typed conditions (explicit [] for none).
2. First eligible observation posts immediately; interval is minimum game time between accepted intents,
   not time since completion. Blocked/disabled/duplicate attempts earn no posting timestamp.
3. Each source owns only enabled/priority and its last accepted plan/tick. WorkBoard owns every intent,
   native execution, pause/cancel/repeat/history; source stores references, not copied execution state.
4. WorkBoard captures all source observations before submitting any intent, rechecks ordinary admission
   when committing, and then uses existing PromotePlans/WorkSystem execution. No same-pass feedback.
5. Player commands/previews manage source enabled/priority. Disabling stops new postings, never silently
   cancels existing plans; ordinary plan pause/cancel commands retain their existing authority.
6. Private immutable diagnostics, public independent provider, strict bindings/purity/codec/time/link
   guards, competing/manual plans, physical inspection/service, cancellation/repost and saved twins pass
   exactly two named suites. No compatibility or old behavior path.

## 2. Design

IRecurringPlanDefinition extends named definition metadata with IPlanDefinition Plan, GameDuration Interval
and explicit initial settings. IRecurringPlanSource exposes pure ReadSchedule/ReadIntent/CheckTrigger and
exact Configure/RecordSubmission. RecurringPlanContracts owns actual live/catalog/read/purity/admission/
commit checks. Native RecurringActivitySource owns enabled/priority/lastPosted/lastPlan in one codec1.
RecurringActivitySourceDef resolves the actual ActivityDef and exposes it through existing IActivitySource;
SignalGateCondition remains the sole condition parser/binding/evaluator. No condition-module requirement:
thresholds can use any declared compatible signal provider. Inspections are ordinary positive-time DAGs.

WorkBoard's new partial WorkRecurringPlans.cs collects a stable entity/component pass, uses existing
PreviewPlan/SubmitPlan and records only actual admitted plan identity/tick. Existing duplicate intent
and direct ActiveOn guards prevent overlapping equivalent work. A cancelled or failed plan may retry
after cadence if its trigger still holds; disable the source to suppress automatic repost. Pending/paused
plans remain authoritative and prevent duplicates. Concurrent providers are rechecked by descending
explicit priority, then stable entity/source identity; equal-priority outcomes do not depend on array order.
The board checks each source's latest reference against actual plan company/key/intent identity/created
tick on read/write/post-restore; edits of active priority/group remain solely on native plans/orders.

Overflow of next due tick is an explicit typed blocker; no int age cap or invented horizon. Positive
intervals must align to the chosen session quantum. Native staged codec rejects negative/inconsistent
schedule facts; actual session chronology/reference validation occurs after full restore. Pure guards
cover actual entity/tick/allocator, not global transactional rollback or every cross-service mutation.

## 3. API surface

ConfigureRecurringPlanCommand(Entity,Source,Enabled,Priority), matching WorkView preview/read, and detached
RecurringPlanView (source, immutable intent, schedule, due tick/blocker). Existing plan views provide
actual execution/progress/requirements. Open source contracts allow a game-defined trigger/intent without
changing the coordinator. No host work logic, free repair, private maintenance queue or new registry.

## 4. Files

New Standard/RecurringPlans.cs, WorkRecurringPlans.cs and Commands/RecurringPlanCommands.cs; extend current
WorkModule, WorkSystem/WorkBoard checkpoints, WorkView, engine identity and test program. Master-listed
recurring-maintenance.json supplies inspection/service examples; independent public source proves extension.
Update this stage, master/P12 parent/README/AGENTS/help and scoped evidence with final source fingerprints.

## 5. Errors

Missing activity/source/condition, non-orderable or wrong-target activity, unaligned/nonpositive interval,
duplicate named capability, negative priority and actual catalog mismatch fail explicitly. Disabled,
cadence, unavailable trigger, existing work/plan and next-tick overflow return typed blockers. Impure
providers, wrong intent/schema/settings, dishonest commits and forged chronology/plan links fault actual
Update/checkpoints. Foreign management never exposes private source intent/clock/history.

## 6. Compatibility

Development-only direct change: build2.101.0/save64/new source codec1/work view30/work system2. WorkBoard22
and all existing owner payloads remain unchanged. No old schemas, facade, absent-field default or shim.

## 7. Verification

Build, then only these exact names (repair reruns of these same names allowed):

```
Recurring plans: authored schedules, controls and checkpoint ownership
Recurring plans: physical inspection, maintenance and saved continuation
```

Other identity assertions compile only. Prior/full/family/third tests remain unexecuted.

## 8. Risks

Two sources may offer identical intents: capture both, but recheck existing board admission before each
submission. Do not advance the refused source's clock. Source settings control future postings, not
current order priority/pause. Latest accepted references must survive native order pruning via plan
history, never copy progress. Trigger readiness and physical execution requirements remain distinct:
posting an intent cannot reserve/spend/repair early. Every physical wait has a bounded diagnostic failure.

## 9. Out of scope

Operating/fleet policy thresholds, preparedness reserve capacity/materials, inspection-discovery science,
arbitrary incident/condition effects, contamination/formulas, replacement/reference migration, broad
provider/replay/lockstep/fuzz/stress/performance/full roadmap gates. No Oil/Tycoon/host/demo/platform/Draw.

## 10. Sequence

Source contracts/JSON/provider → extend sole board observation/admission/checkpoint path → command/view →
authored inspection/service/public provider → exactly two focused gates → scoped evidence/docs/tracker.

## 11. Focused implementation result

Native recurring_activity lives in the existing WorkModule; no additional module, scheduler or persistent
service was introduced. WorkBoard22's payload is unchanged. Source metadata/catalog/time/intent/history
validation runs through the existing composition/checkpoint/post-restore paths. Native decode stages the
whole source payload before replacement. Independent public permit/plan-backlog triggers and five fault
modes demonstrate the contract boundary, including capture-before-submit and exact reference acknowledgements.
Physical inspection/service scenarios prove normal command control, real travel/spares/tools/progress,
pause/cancel/repost, terminal order pruning and exact cadence across wide saved interleaved sessions.
Operating threshold/restart policies and linked maintenance criticality now have scoped D4 proof.
Fleet/preparedness/incident policies and broader durability/full acceptance remain open.
