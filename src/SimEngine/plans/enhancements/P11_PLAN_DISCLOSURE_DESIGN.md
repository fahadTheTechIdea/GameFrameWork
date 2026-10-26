# P11-A7 — whole plan and blueprint disclosure

## 0. Problem

The original board's template library, group projection and history projection can reveal an activity identity after ordinary activity disclosure is revoked. Authored template reads check only the outer definition, not the activities inside it. A structural preview can invoke a hidden provider without a viewer. Native EntityActivityDef also omits its exact target prototype from DisclosureContent.

## 1. Goals

Require existing activity authorization for every entry before disclosing a whole authored/stored/snapshot blueprint, structural preview or expanded intent. Require every member and its actual prerequisite closure for a group/history cohort. Hidden entries are withheld as a whole, never fabricated as empty prerequisites or unsupported template providers. Revoking disclosure must not corrupt saved templates, erase plans, or stop the existing execution/history owners.

## 2. Design and contracts

Add CanDisclosePlanTemplate(PlayerId, PlanTemplate) to existing IPlanBoard/WorkBoard; it walks original immutable entries through IActivityCatalog.CanDiscloseActivity, without another catalog/cache/state owner. Native EntityActivityDef declares itself and its exact TargetDefinition. Authored template property reads use original EntityObservation and visibility; stored reads/listing and snapshot resolution use the same board method. Public WorkView structural preview authorizes first, while the original unscoped structural validator remains available internally for composition and saved schema validation.

Public template store/expansion/capture uses whole declaration admission before callbacks. Original persisted template validation deliberately remains structural and unscoped: a valid saved design stays valid while knowledge is revoked. Group/history collection projections stage local detached facts, require all actual cohort members/prerequisites, and publish only after the original healthy observation boundary. Internal history policy/retirement/assignment/timing remain unchanged.

## 3. Errors

Hidden authored/stored reads return null/NotFound; hidden blueprint provider previews/commands use UnknownDef before any schema callback. Hidden capture uses NotFound with no member diagnostics. Invalid arguments and selected-space structural validation remain existing rules. Provider failures retain original World first-fault semantics. No compatibility shim, second topology or redacted fake data.

## 4. Files and verification

Existing PlanContracts, WorkPlanTemplates/TemplateLibrary/Groups/HistoryPolicy, EntityActivities and WorkView; one thin new public-input driver and two explicit master/add-on JSON files reuse the previous physical game composition. One solution compile and ONE NEW exact bounded case, same-case repair only. Prove initial hidden blueprints, physical discovery, captured/stored authored templates, pending ordinary commands, original paused dependency group, revocation, history/group/list/read/preview withholding, saved twins/replay and physical re-discovery of the same identities/revision.

## 5. Limits

This does not close broad P11.acceptance.033: mutation/retirement diagnostics, company-scope completion targets, recurring/load/operating projections, arbitrary requirement/effect/extension-private reads, global fog/deltas and performance/full regression still need original acceptance proof. No Oil/Tycoon/platform/rendering changes; protected G19 and sole clock untouched. Full P1–P12 and goal remain open; no commit/push before completion.
