# P9-A4 — provider-owned recurring plan intents

Historical stage evidence, not rerun at current HEAD. The next
[A4b template recurrence stage](P9_RECURRING_TEMPLATES_DESIGN.md) directly adds required explicit
template:null-or-ID selection to source authoring; its scoped evidence is current.

Status: focused verified, 2026-10-07; build2.171.0/save95, existing owner payloads unchanged.
Clean build;471 assertions/two exact suites (272 contracts;199 physical), zero final failures/aborts.
[Evidence](../../docs/migration/P9_AUTHORED_RECURRING_INTENTS_VERIFICATION.md).
Parent: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Prerequisite: [recurring plans](P12_RECURRING_MAINTENANCE_DESIGN.md).

## 0. Problem statement

Before this stage, Standard/RecurringPlans.cs resolved ActivityDef directly and required IEntityTargetPlanDefinition
for every authored binding. Construction and other existing IPlanDefinition providers cannot recur
through JSON, despite the sole WorkBoard resolver already knowing them.

## 1. Goals

1. A recurring definition authors a stable full plan key, not a concrete activity reference.
2. Explicit authored_intent binding carries immutable provider-owned JSON arguments; ordinary entity
   bindings explicitly use arguments:null and retain actual self/home/facility targeting.
3. Composition rejects unknown/non-intent plans, invalid arguments, incompatible bindings and reserves.
4. Native and independent public sources use the same WorkBoard admission, origin and cadence protocol.
5. Physical construction repeats only through existing sites, material delivery and timed machine work.
   Live plans prevent reposting; pause, cancellation, changed geometry and shortages retain native rules.
6. Saved pending/active/terminal/posting states, private diagnostics and interleaved sessions remain exact.

## 2. Design

IRecurringPlanDefinition exposes PlanKey. RecurringPlanContracts.ResolvePlan consults the existing
WorkBoard resolver; no source-side definition registry, copied graph or concrete lookup survives.
RecurringPlanBinding gains AuthoredIntent plus a frozen Arguments value. This mode requires an object,
empty worker_nodes, reserve_use:none and facility_policy:null. The plan provider owns machine selection
inside its arguments. Entity-target modes require null arguments and the existing target-intent contract.
IEntityTargetPlanDefinition validates authored target compatibility through a pure definition method,
so ordinary orderability/target metadata are preserved without concrete casts.

WorkBoard composition validates authored identities against the actual selected space/provider before
play. ObserveRecurringPlans remains the only posting pass; PromotePlans and WorkSystem own execution.
Source settings/history codecs remain unchanged; authored arguments are frozen content and each admitted
PlanEntry already stores its own intent. Historical origins continue to reference actual accepted plans,
not today's geometry. No new clock, scheduler, claim ledger, plan model or resource balance.
Every referenced graph uses WorkBoard.ValidatePlan. Runtime observation reuses Kernel.EntityObservation;
the source's narrower local Pure implementation is deleted. Fixed authored request identity/group must
match the binding; forged arguments or pacing mutation fault before game-clock completion.

## 3. API surface

Required JSON: plan:"build:point.tower" (or another registered key), target:"authored_intent",
arguments:{"placement":{"space":"field.points","position":12}}, worker_nodes:[], reserve_use:"none".
The existing native recurring_activity key remains the one source implementation; it no longer reads
the old activity reference field or republishes an IActivitySource graph. Recurring source settings,
group, interval, conditions and operation-policy fields remain explicit as before.
Self/home/facility use full plan keys and explicit arguments:null. Existing commands/views alone control
enabled state, priority, cadence, plan pause/cancel and physical construction/demolition.

## 4. Files to change

Standard/RecurringPlans.cs, WorkRecurringPlans.cs, WorkFacilities.cs, PlanContracts.cs, Work.cs;
public recurring source and directly migrated JSON/callers; new master-listed recurring construction
proof content and two exact suites. Build identity, stage/master, current baseline and evidence.

## 5. Error handling

Missing/invalid plan, arguments, target or incompatible binding fails composition. Dynamic occupied
placement remains an ordinary admitted pending plan blocker, not an instant rebuild. The posting
timestamp belongs to that accepted intent; subsequent blocked observations never earn another timestamp.
Outstanding live plan, disabled source and cadence use existing typed codes. A provider mutation or
invalid request faults rather than inventing arguments or selecting another plan.

## 6. Compatibility

Direct contract and JSON migration. No old activity field reader, default arguments or compatibility shim.
Existing owner payload layouts remain unchanged; build identity rejects earlier checkpoints.

## 7. Verification

Build Beep.SimEngine.slnx, then only:

- Authored recurrence: plan contracts, JSON and private admission
- Authored recurrence: physical construction, repeat and saved continuation

Bound all physical waits with current plan/source/job diagnostics. Other migrated tests compile only.
No full regression or third/family gate without the user's explicit request.

## 8. Risks

An intent may be pending while placement is occupied. Never post another while it is live. Disabling
a source must not cancel its admitted plan. Repetition is cadence between accepted intents, not a
second current-time counter. Provider-owned argument semantics must not be inferred by the source.

## 9. Out of scope

Grouped automatic templates, editable repeat libraries/history pruning, new workflow providers,
replay/stress/performance/full framework acceptance. No Oil/Tycoon/host/demo/platform/Draw changes.

## 10. Execution order

- [x] Inspect owners and define this stage.
- [x] Replace source binding/resolution and preserve contract-owned entity targeting.
- [x] Migrate all active authoring/callers and add native/public physical JSON proof.
- [x] Build, run exactly two gates, review ownership and update evidence/master.
