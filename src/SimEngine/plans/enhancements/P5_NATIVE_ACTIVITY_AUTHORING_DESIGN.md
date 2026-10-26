# P5-A19 — Native activity DAG authoring and optional work costs

## 0. Problem statement

Actual Oil content still declared orders/steps/needs, while the framework has one authoritative activity
graph and WorkBoard. Optional money must not be a physical activity prerequisite.

## 1. Goals

Preserve all ten activity identities, total durations, material totals, priorities, roles and survey
parameters; migrate to existing graph contracts and optional Tycoon cost bindings. Author drilling as
rig-up, drill, casing/cementing and completion. Keep seismic/logging optional, never dependencies.

## 2. Design

Oil work.json owns staffed/timed executors, target resource requirements, nine activities, one entity
activity and one native entity replacement effect. Nodes name stable IDs, dependency IDs, executor,
roles, rational-hour duration, requirement and effect. WorkBoard remains the sole scheduler/reservation/
completion owner; no new interpreter, order compatibility reader or parallel entity state.

Land drilling remains108 machine-hours; offshore remains180. Casing moves from drill to the24/36-hour
casing_cementing stage together with cement. Zero-hour completion is an explicit timed effect stage,
not an instant shortcut for machine work. Pad/site preparation remains a separate physical prerequisite;
its full construction migration is not verified here. Surveys retain their authored IDs/radii/confidence
and now reference activity, without an order alias. Pumpjack installation selects existing replacement
contracts, not a domain copy of generic replacement logic.

Ten original work costs move to Tycoon-owned business-costs.json alongside eight prior line costs.
Amounts and credit flags stay unchanged; drilling's completion amount binds to casing_cementing.
Physical work.json contains no money fields. Resource-only composition omits Tycoon.

JsonReader.NullableString is the shared optional-reference reader: absent or explicit null means no
reference; wrong types still reject. Existing activity, node, transformation and upkeep reference readers
use it. This also fixes explicit-null declarations previously missed by narrow projections; prior stage
success did not prove full Oil content. No persistent state or clock algorithm changes; build2.544.0,
save154/replay1.

## 3. API surface

Existing IAuthoredActivityDefinition/IActivityNodeDefinition, executor/resource-requirement/work-effect,
IActivityCostBinding and public PostOrderCommand/WorkView APIs are retained. Oil SurveyDef.Activity
replaces Order; no compatibility path. Completed compacted orders are verified using earned completion
history, not inferred from disappearance.

## 4. Files to change

Canonical Oil work/business-costs JSON, Commands.cs/Views.cs; framework Json.cs, ActivityContent.cs,
Work.cs, Transformations.cs, EntityOperatingCosts.cs and SaveGame.cs; one new thin focused driver,
its registration and linked canonical content resource. Stage/evidence/master/audit are updated in place.

## 5. Error handling

Missing references/cycles/types retain original content refusal. Explicit-null optional references are
valid absence. Wrong command identities retain unknown-def. Absent active orders must have actual
earned completion history. Actual Oil initialization currently refuses company_base storage's missing
capacity_policy before a world is initialized; it is not silently supplied.

## 6. Backward compatibility

Development migration only. Old orders/steps/needs/cash and survey.order authoring are removed outright.

## 7. Verification

One new bounded exact case exercises the canonical land-drilling DAG through native fixture contracts,
money absent/enabled, dependency settlement, three machine-stage saves and saved twins. Its whole-order
Oil effect is explicitly omitted; it does not prove actual geological well creation, offshore execution,
surveys, pumpjack replacement, all costs, full content or old suites. See [evidence](../../docs/migration/P5_NATIVE_ACTIVITY_AUTHORING_VERIFICATION.md).

## 8. Risks

Preserved total materials do not mean preserved consumption timing: casing now belongs to its actual
stage. A legal fixture pad does not prove real pad construction. Static parity is not runtime coverage
for all ten activities. Full business consolidation and actual Oil gameplay remain open.

## 9. Out of scope

Full regression, renderers/Draw/Unity/Godot/adapters, host gameplay, new clock, new money ledger,
business-rule changes, instant completion fallbacks and Git writes.

## 10. Execution stages

1. Replace canonical activity authoring and survey references; retain IDs/physical totals.
2. Move monetary leaves to optional Tycoon bindings, preserve amounts/credit.
3. Repair shared optional-reference admission using the existing reader.
4. Verify only the new bounded physical DAG case and actual-content refusal boundary.
5. Update tracker/evidence/audit; close no original acceptance gate.
6. Continue storage/mover/worker/construction, sales/research financial ownership, remaining paid-supply
   fixture and complete Oil physical/public-view/privacy/durability acceptance. Goal and plan stay open.
