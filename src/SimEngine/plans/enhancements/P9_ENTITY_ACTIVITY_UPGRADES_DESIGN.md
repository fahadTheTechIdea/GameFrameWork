# P9-A9 — Contract-authored entity activities and physical upgrades

## Current follow-up: outcome-selected replacement — 2026-10-20

Actual MS1 offshore drilling earns all180h but fails at72759 with
work.replacement-active-execution; pad remains, no well/completion. Original
WorkPlanReferenceReplacement only recognizes static single source/target effect
metadata. Oil drilling is a declared physical effect with several authored results,
so its own earned completion is incorrectly treated as an unrelated active order.

Revise EXISTING IEntityReplacementEffect directly, no legacy/static shim: declare
source-definition compatibility plus pure current source/destination admission.
Native static replacement and Oil dynamic drilling implement that same contract.
The sole reference owner observes callbacks with existing full entity/space/clock
guards and permits ONLY its own fully earned whole-order Completing replacement;
all other active target/worker references still refuse. Pending plan references use
the same declared source compatibility, not a fabricated single source prototype.
Upgrades remains the sole stock/state/geometry/reference commit owner.

Prove an independent, master-listed outcome-selecting provider with physical work,
two possible authored results, material consumption, foreign claim wait, saved
twins and original target history. Reproduce before core repair, then run only
that literal plus directly affected static contract/reference guards. Migrate Oil
mechanically to the shared contract; do not alter geology, work quantities/timing,
business, driver outcomes or guards. Core simulation change MUST bump strict build
identity; old native2.672 images become historical, never header-rewritten. Native
marine reproduction on the new build remains separate acceptance work if unrun.
Current implementation/results and all failures belong to the existing MS1 report.
Status: selected contract repair implemented/verified on strict2.673. Outcome40/0,
static contracts171/0, plan references187/0 and actual Oil unsurveyed drilling617/0.
[Every attempt/current native image/remaining offshore gate](../../docs/migration/P5_MARINE_WORKFLOW_STAGING_VERIFICATION.md#current2673-outcome-replacement--2026-10-20).
No full regression or whole-plan closure; historical status paragraphs below are
not current-build native marine certification.

## 0. Problem statement

At the start of this stage, Work.cs ActivityDef owns replace_with, and WorkSystem.CompleteOrder casts the
ordinary graph to ActivityDef before applying a second replacement operation. That hard-coded
scheduler branch prevents replacement from behaving like other registered completion effects.
Portable templates currently cover construction, infrastructure and removal, but not ordinary
authored work on an exact entity prototype. This stage implements the corresponding P9 intent
and upgrade requirements; it does not close the full framework roadmap.

## 1. Goals

1. Replacement is an explicit JSON work effect with required source/target prototypes.
2. Positive-time machine work and whole-order completion are mandatory; no instant fallback.
3. Structural intent can wait for real reservations; activation and completion recheck actual
   owner, content access, capacity, geometry and state through the existing Upgrades coordinator.
4. Entity activities support relative templates through selected spatial contracts, sharing
   target capture/resolution with removal rather than copying that implementation.
5. Existing native orders, resource claims, completion retry history, saves and the single
   GameClock remain authoritative. New content does not require scheduler edits.

## 2. Design

- WorkEffects.cs: IWorkEffectIntentAdmission separates CheckIntent from CheckStart.
  IEntityReplacementEffect declares source compatibility/current-outcome admission.
  EntityReplacementEffectDef implements IPhysicalWorkEffect; Upgrades remains the
  sole state-preserving replacement owner, including outcome-selecting modules.
- Work.cs: ActivityDef delegates optional completion admission to its declared effect.
  CompleteOrder only invokes the generic effect and existing retry/terminal logic.
- EntityActivities.cs: EntityActivityDef extends ActivityDef and implements the existing
  IPlanTemplateDefinition using a required target prototype. EntityTargetPlanTemplates is
  stateless shared capture/placement logic for existing-target providers. Geometry belongs to
  ISpatialPlacementIntent/ISpatialPlacementTemplates, never tile or point branches.
- Construction.cs: removal delegates capture/placement to that same helper.
- WorkModule declares entity_activities and entity_replacement_effects, loaded only from
  explicitly master-listed documents. No new content registry, entity store or update system.
- StandardCommands.cs: immediate post-order preview checks current activation; persistent plan
  submission remains structural. IPlayerActivityDefinition now requires the existing plan
  protocol and authored Priority; direct commands call that same Activate, without a concrete
  ActivityDef cast or another posting path. Technology/incident replacement is rejected by contracts.

## 3. API and authoring

Entity activities retain activity:<stable-id>, ordinary node roles/requirements/dependencies,
on_complete and normal player controls. Portable arguments contain relative placement only;
capture rejects selected live machine IDs. Required effect source/target references are actual
resolved entity prototypes, not saved entity IDs. Removing replace_with is a direct strict
authoring revision, without an old reader or compatibility shim.

## 4. Files

Runtime: WorkEffects.cs, Work.cs, EntityActivities.cs, Construction.cs, WorkModule.cs,
PlanContracts.cs, ConstructionContracts.cs, StandardCommands.cs, Technology.cs,
IncidentWorkflows.cs and engine identity metadata.
Proof: master-listed completion-effects.json, CompletionEffectTests and direct replacement
authoring in ProviderPlacementTests. The existing separately compiled CompletionEffects provider
implements the public admission contract; EntityActivityUpgradeTests drives player commands and
asserts native behavior, without implementing gameplay rules.

## 5. Errors

Missing prototypes, incompatible activity targets, nonphysical graphs and node replacement fail
before gameplay. Missing/foreign/ambiguous targets return existing typed refusals. Real promises
and busy machines block activation/completion rather than disappearing. Successful replacement
preserves physical stock via verified receipts; completion never reruns finished node effects.

## 6. Compatibility

Development-only direct revision. Delete obsolete authoring and scheduler special cases.
No parallel models, compatibility readers or legacy fallback.

## 7. Verification

Build the framework solution. Run only these two exact existing suites:

- Completion effects: typed contracts and private retry state
- Completion effects: physical blockers and saved continuation

Add bounded strict/private/template/physical/save/pause assertions to those callbacks. Other
suites are compile-only this turn, not claimed runtime-verified. Full regression stays open.

## 8. Risks

Replacement retires an entity identity; existing terminal order history retains its original
target. Do not invent automatic reference rebinding or promises to future entities. Selected
geometry providers must serialize exact placement and remain pure during capture/resolution.

## 9. Out of scope

Draw, platforms, adapters, Oil/Tycoon changes, new clocks, reference-rebinding policy, runtime
template libraries, dependency editing, global provider purity, soak/replay/performance gates.

## 10. Execution

1. Remove scheduler replacement branch and introduce ordinary authored physical effect.
2. Add prototype-target entity activity templates and share target logic with removal.
3. Migrate actual positive authoring directly; extend the two focused proof callbacks.
4. Build, run those exact gates, and reconcile evidence and the master tracker honestly.

Status: implemented and focused-verified, 311 assertions in the two exact named suites, clean
build, zero final failures/aborts. Broader regression and full P9/overall goal remain open.
[Evidence](../../docs/migration/P9_ENTITY_ACTIVITY_UPGRADES_VERIFICATION.md).

Historical checkpoint: A10 now owns current source2.178.0; the completion-effect gate above
was not rerun there. [Current stage](P9_PLAN_DEPENDENCY_EDITING_DESIGN.md).
