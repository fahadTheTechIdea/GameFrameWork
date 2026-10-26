# P9-A7 — portable removal templates and typed target resolution

Historical source checkpoint. [P9-A8 plan prerequisites](P9_PLAN_DEPENDENCIES_DESIGN.md) owns
current source/evidence and includes these original callbacks. Its direct codec/JSON revision
supersedes the previously unchanged-layout statement below.

Status: focused verified, 2026-10-07; build2.175.0/save95/board28/work30/work-view55/construction-view4.
Clean build;318 assertions/two exact suites (177 contracts;141 physical), zero final failures/aborts.
[Evidence](../../docs/migration/P9_REMOVAL_TEMPLATES_VERIFICATION.md).
Parent: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Prerequisite: [removal plans](P9_REMOVAL_PLANS_DESIGN.md).

## 0. Problem

Removal is persistent physical work, but its ordinary intent contains a live target ID. Reusing that
ID in a portable template would delete the original facility, not resolve the translated prototype.
The existing template contract also loses provider-specific failures by expressing expansion only
as JSON/exception. Missing/ambiguous/foreign targets need truthful per-entry refusal without a prefix.

## 1. Goals

1. The actual demolition provider implements the existing optional template capability.
2. Portable JSON contains prototype key plus provider-relative placement only; no target/worker IDs.
3. Capture/placement return typed blockers through the sole board and actor-aware provider contract.
4. Engine-owned target lookup uses selected placement identity, exact prototype and actual entities;
   missing, foreign or ambiguous matches are never replaced by a default or nearest entity.
5. Mixed groups and recurrence keep the existing expander/admission/order owners; physical removal,
   salvage, save/load and single-clock timing remain unchanged.

## 2. Design

Directly revise IPlanTemplateDefinition.CaptureTemplate/PlaceTemplate to actor-aware Code returns
with out JsonValue. Native construction/line providers retain their sole geometry paths. WorkBoard
uses ValidateTemplateAnchor for pre-Initialize recurrence authoring without inventing a player or
requiring a live target. Actual capture/expansion is actor-aware only during ordinary observation.
collects typed per-entry results, requires output iff successful, and creates no partial group.
Its existing pure callback guard additionally protects current GameClock identity/time/pacing.

DemolishPlan captures only an actual owned matching target with unbound worker intent. Explicit live
machine selection refuses a typed live-binding blocker, rather than silently dropping that policy.
Relative placement is authored/read/translated by selected ISpatialPlacementTemplates.
ISpatialPlacementIntent.WritePlacement supplies the exact native JSON inverse of ReadPlacement;
native tile and public point providers own serialization, never the removal provider or host.
Placement resolves exactly one current matching owned entity, then creates the normal target/unbound intent.
No target IDs enter the template, and no geometry arithmetic belongs to a host.

EntityPlacementTargets.Find is one stateless engine query over existing placement contracts and the
actual entity registry. Provider geometry determines exact identity. EntityObservation protects each
candidate callback; the board protects session allocation/clock facts. No new spatial index, selection
store, snapshot authority, scheduler, current time or mutable state codec is introduced.

Actual native/public recurrence proof exposed a stale future-target blocker hiding a still-live
admitted cycle after removal. RecurringPlanContracts now checks existing outstanding membership
before prospective request refusal. Existing source provenance/time/contiguous plan IDs still own
cycle validation, including removed targets; no target re-expansion of historical execution occurs.

## 3. API

Code CaptureTemplate(World, PlayerId, JsonValue arguments, JsonValue origin, out JsonValue template);
Code PlaceTemplate(World, PlayerId, JsonValue template, JsonValue anchor, out JsonValue arguments).
ValidateTemplateAnchor(World, JsonValue template, JsonValue anchor) is structural, not an actor grant.
ISpatialPlacementIntent.WritePlacement(SpatialPlacement) is the exact native JSON inverse.
Removal template arguments: {"placement":providerRelativePlacement}. Ordinary admitted removal
keeps target/worker JSON. PlanCodes.LiveBindings and EntityPlacementTargets.Ambiguous are typed
refusals. Existing group/template command/view/result codecs remain authoritative.

## 4. Files

PlanTemplates.cs, WorkPlanTemplates.cs, Construction.cs, InfrastructurePlans.cs, PlanContracts.cs;
stateless EntityPlacementTargets and focused PlanTemplateTests callbacks/master-listed JSON.
SpatialPlacement/TileMap, the existing EntityObservation and recurring owner, public point proof.
Mechanical build identity migration and stage/evidence/master/README/AGENTS/help reconciliation.

## 5. Errors

Bad schema/space/overflow -> InvalidArgument. Missing translated prototype -> NotFound. Single foreign
target -> NotOwned. Multiple exact matching targets -> explicit ambiguity. Selected live machine
bindings -> LiveBindings. Any entry refusal prevents the entire group; callback mutation faults.

## 6. Compatibility

Direct template contract revision. No old signature adapter, copied target-ID template, default
entity lookup or second execution path. Existing mutable-state layouts remain unchanged.

## 7. Verification

Build and exactly the two existing PlanTemplateTests suite names, including original construction
and recurrence callbacks: Recurring templates: authored groups, private admission and cycle ownership;
Recurring templates: physical projects, cancellation and saved continuation.
Add native/point capture/translation, master/strict/typed/whole refusal, physical removal/salvage,
selected-binding refusal, saved command/twins and sole-clock pause. Bounded waits; no third/full run.

## 8. Risks

Target resolution is current admission, not a future entity promise. Point worlds may legitimately
contain coincident prototypes; ambiguity must refuse. Scoped purity is not a full external sandbox.

## 9. Out of scope

Survey/upgrade providers, cross-entry dependencies, rotation/libraries/history, wider provider purity,
replay/lockstep/fuzz/stress/performance/full acceptance. No Oil/Tycoon/host/demo/platform/Draw.

## 10. Execution

- [x] Inspect current template/selection/spatial/physical owners and document the direct revision.
- [x] Extend typed actor-aware callbacks and exact stateless target query.
- [x] Add portable removal provider and master-listed native/point physical proof.
- [x] Run build/two exact gates and reconcile evidence/tracker; full roadmap remains partial.
