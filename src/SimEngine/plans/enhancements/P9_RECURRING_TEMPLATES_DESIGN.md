# P9-A4b — recurring authored template groups

Earlier M131 extends the original source protocol without another template/scheduler owner:
[known revision diagnostics, sight-gated player replacement and physical saved proof](P9_RECURRING_LIBRARY_DESIGN.md#current-m131-actual-selection-disclosure-and-shared-player-admission--2026-10-23).
Engine2.764/two literal finals615/0, not a rerun of the older broad template groups below.

Current M134 qualifies original receiver selection on unchanged native2.765:
[existing recovery owner](P9_RECOVERY_DESTINATION_POLICY_DESIGN.md#current-m134-discarded-receiver-dependencies-through-operating-areas--2026-10-23).
Its two literal411/0 results do not rerun any template body here.

Historical focused report, not rerun at current HEAD. The next
[A5 infrastructure stage](P9_INFRASTRUCTURE_PLANS_DESIGN.md) includes native/public corridor
recurrence through this same group/source protocol; it also revises actual graph binding and supply.

Status: focused verified, 2026-10-07; build2.172.0/save95/board27/work29/work-view54.
Clean build;226 assertions/two exact suites (146 contracts;80 physical), zero final failures/aborts.
[Evidence](../../docs/migration/P9_RECURRING_TEMPLATES_VERIFICATION.md). Parents: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md),
[single authored recurrence](P9_AUTHORED_RECURRING_INTENTS_DESIGN.md),
[templates](P9_PLAN_TEMPLATES_DESIGN.md).

## 0. Problem statement

Before this stage, RecurringPlanRequest could carry only one PlanIntent. WorkPlanTemplates already owns pure expansion
and whole-group admission, but a recurring source cannot select it. LastPlan alone also cannot show
whether a later member of a grouped submission is still live.

## 1. Goals

1. Master-listed source JSON explicitly selects either a full plan key or an authored template.
2. The existing template expander supplies provider-owned placement/arguments. No host geometry.
3. One source pass submits one whole PlanGroupIntent; every real plan receives existing provenance.
4. Source LastPlan references the first admitted ID. Its accepted game-time stamp and origin identify
   every member; no separate cycle/group record/counter/current time is saved.
5. Any live member prevents repetition, even after first completion, pause, cancellation or regrouping.
6. Native/public sources, strict selection/private previews, real material/machine work and saved
   grouped lifecycle are verified through exactly two focused suites.

## 2. Design

IRecurringPlanDefinition adds optional PlanTemplateDef Template; exactly one Template or PlanKey is
required. RecurringPlanBinding.Template uses immutable arguments as the explicit provider anchor;
empty source worker_nodes/no reserve exemption/no facility policy remain mandatory. Native/public
definitions share the strict selector reader. Ordinary plan JSON explicitly declares template:null.

RecurringPlanRequest is one tagged envelope: single intent, existing grouped intent or typed refusal.
The existing board exposes ExpandPlanTemplate; preview/placement/source requests all use that function.
Template relative entry priorities add to effective source priority with checked range admission;
entry pause is authored and preserved. One declared source priority ranks posting, native order
priorities continue to rank physical work. Source configuration never rewrites admitted members.

Generated group identity derives from explicit source group base, entity/source identity and the
last accepted plan ID. Current game time remains solely GameClock. Existing PlanOrigin plus
CreatedTick/ordered plan IDs identify a submission, independent of mutable UI group labels. The
source records only its actual first plan ID and accepted timestamp. Checkpoints validate all members
against immutable template entry order/definitions; historical placement is never rebuilt from
today's source arguments. Pending/active progress and receipts stay native WorkBoard/WorkSystem state.

## 3. API surface

Source JSON requires plan:null, template:"field.layout", target:"template", explicit argument object
for the selected provider anchor and nonempty group base. Single sources require template:null.
RecurringPlanRequest.Ready(PlanGroupIntent), RecurringPlanView.GroupIntent and private per-entry
admission facts extend the existing envelope/view. Existing source/plan/group commands alone control
the work; no repeat system or instant template-placement gameplay.

## 4. Files

RecurringPlans.cs, WorkRecurringPlans.cs, WorkPlanTemplates.cs, PlanContracts.cs, Work.cs/WorkModule.cs,
OperationPolicies.cs and public source metadata. Direct JSON/build/schema migrations, master-listed
template recurrence proof and two tests, stage/evidence/parents/master/README/AGENTS/help.

## 5. Errors

Missing/both/neither selectors, unknown/unselected template, malformed/duplicate/out-of-range entry,
invalid anchor/group/reserve/facility and priority overflow refuse before allocation. Whole admission
has no accepted prefix. Occupancy/material/route waits remain per-entry native blockers. False
acknowledgement, partial/forged group/provenance or impure callbacks fault before clock completion.

## 6. Compatibility

Direct JSON and public metadata update. No missing template-field default or old reader. Existing
payload layouts remain unchanged; build/system/view semantics change explicitly.

## 7. Verification

Build, then only Recurring templates: authored groups, private admission and cycle ownership;
Recurring templates: physical projects, cancellation and saved continuation. Bound all waits with
source/member/job diagnostics. Other migrated suites compile only; no full regression.

## 8. Risks

Changing a display group must not end the source's outstanding cycle. First completion cannot authorize
repost while another member lives. Template changes require different content fingerprints, not save
translation. Whole submission is not simultaneous physical construction or resource reservation.

## 9. Out of scope

Runtime template library editing/history pruning, cross-entry dependency authoring, new template
providers, replay/stress/performance/full acceptance. No Oil/Tycoon/host/demo/platform/Draw work.

## 10. Execution order

- [x] Inspect owners and record this stage.
- [x] Extend the existing request/expansion/source-cycle protocol and migrate authoring.
- [x] Add native/public JSON and bounded physical/ownership proof.
- [x] Build, exactly two gates, ownership review and evidence/tracker update.

Focused proof is not full P9 or full-plan completion. Runtime repeat libraries/history pruning,
broader providers/global purity/replay/stress/performance/full acceptance remain required.
