# P5-A32: Oil research uses native technology activities

## 0. Problem

Oil still stored active/paused study progress and ran an hourly point scheduler after learned facts
moved to CompanyKnowledge. Generic activity commands also needed to enforce the same optional fee
as the Oil selection command; a command-only payment would create a bypass.

## 1. Goals

1. WorkBoard owns study plans, paused state, execution, requirements and progress.
2. TechnologyDeclaration/ActivityDeclaration own the sole authored technology DAG; CompanyKnowledge
   and TechnologyService own learned facts, prerequisites, routes and unlock access.
3. Generic file-authored facility capacity replaces Oil Lab/point execution.
4. Every start path settles existing optional Tycoon prices and once-per-company receipts.
5. Oil retains only domain metadata, modifiers, safety and player study-selection policy.

## 2. Design

All16 Oil technologies now author ordinary native nodes, dependencies and final completion effects.
Former points become nominal work durations in days; original12/day lab capacity becomes rate12.
Thus a120-unit study still has nominal ten-day work with one original lab. Progress runs on the native
update schedule, not the removed hourly minute15 quantization. No instant or compatibility fallback.

`TechDef` is now a TechnologyDef specialization reading only Oil era/effects, exposed through the
public `IOilTechnologyDefinition` contract. Oil consumers use that contract, not the concrete metadata.
The remaining `Research` class is stateless: selection edits/submits native plans, learned/route access
delegates to native services, and multipliers derive from their observations. No Seat, paused list,
private graph, learned provider, saved research service, Lab/LabDef or ResearchSystem remains.

ResearchView3 exposes native `ActiveProgress`; TechView exposes the plan key instead of points.
Unlock entries are native unlock-policy IDs. There is no old progress/point API alias. Native typed
infrastructure unlocks reuse TechnologyUnlockDeclaration; duplicate unlock-policy ownership refuses.
Explicit starting knowledge must satisfy native prerequisite validation; the engine invents no ancestors.

Canonical research_lab uses company_activity_rate with its original rate12, generic operation controls
and disabled state. Existing OilOperationGate now also implements ICompanyActivityRateGate, delegating
the same power/failure checks to the original Operating owner. The generic source's capacity, state and
operation views respect those gates. Actual construction remains the existing physical construction flow.

## 3. Authoring and contracts

Master manifest declares sim.technology and its explicit `oil/technology-execution.json` document;
Oil requires that module. All existing scenarios therefore select it through normal module closure.
The document declares the pooled executor, native completion effect and typed entity/activity/recipe/
line unlocks. No second catalog, scheduler, stock, clock or technology graph compiler.

New core `IActivityStartRule` is an optional immutable binding on an authored activity. The existing
WorkBoard checks it in public admission and commits it before allocating execution. One rule per
activity; actual aliases and frozen ownership validate before play. Generic direct orders and planned
activation use that same boundary. Core owns no financial rule or balance.

Optional Tycoon `content_action_activity_bindings` reference the existing charge definitions in
business-research-costs.json. They copy no amounts or receipt state. Existing IBusinessContentCharges/
BusinessAccounts remain the sole payment owners. Oil's StartResearchHandler no longer pays separately.
Money-off scenarios load no binding or account. Prices and existing charge category/reason/once flags stay.

## 4. Files

- Framework ActivityStartRules, Work/WorkPlans/WorkModule: single native start boundary.
- Tycoon ContentActionActivityBindingDef/TycoonModule: optional existing-charge binding.
- CompanyActivityRates: additive safety-gate contract on existing source controls/state.
- Technology/TechnologyModule: typed line unlock and unique unlock-policy ownership.
- Oil Research/OilModule/Commands/Views/Settings/Campaign/Autopilot/OilExecution: native delegation
  and IOilTechnologyDefinition consumers; old generic execution removed in place.
- Oil master manifest, technology/structures/technology-execution/research-cost JSON: explicit authoring.
- Focused public research proof and affected charge/route fixture API migration.

## 5. Refusals

Unknown references/cycles, old points JSON, mismatched fee targets, duplicate unlock ownership and
missing named capacity capabilities refuse. Unaffordable domain/native orders share the same refusal;
no fallback money. Disabled/gated facilities retain native progress without advancing it. Known study
and active/paused plan admission use native semantics, not old Oil refusal shims.

## 6. Compatibility

None. Core2.550.0/save156/replay1; Oil research view3. Old research service/system/lab codecs disappear;
native work/knowledge schemas and existing account receipts remain unchanged. Old saves/peers fail
the existing exact build handshake rather than being read through a legacy model.

## 7. Verification

FINAL native Oil research95/0 in10,821ms; affected route1513/0 in12,492ms. Two exact cases,1608 passing,
zero final failures/aborts; no full/family tests. Targeted Release build clean4.84s. [Attempts/evidence](../../docs/migration/P5_NATIVE_OIL_RESEARCH_MIGRATION_VERIFICATION.md).
Proof uses actual Oil modules and public commands/views,24x24 authored worlds and one-minute proof
frames. It authors an HQ capacity source and five-minute study nodes; canonical12/day and study
durations are preserved in production JSON, not claimed as long-duration physical lab acceptance.

## 8. Risks

Canonical lab construction/power supply, domain modifier work/motion/extraction parity, every optional
module combination, rivals, arbitrary provider faults, lockstep, soak and broad industrial acceptance
still need focused proof. Engine notifications/presentation and unrelated older runner APIs are not
validated by this migration. Native scheduling intentionally replaces hourly point quantization.

## 9. Non-goals

No Tycoon pricing/business-rule change, platform/Draw/adapters, host gameplay, full tests or publication.
Original221/194recorded checked/27open unchanged; no original compound gate closes. Full goal remains open.

## 10. Stages

- [x] Native authored technology/work/knowledge ownership and contract-only Oil consumers.
- [x] Replace Lab and point scheduler with generic facility rates and existing Oil safety gates.
- [x] Bind existing optional fees to every native start path; remove Oil-only payment.
- [x] Prove native earned research, paused/control saves, money-off play, direct fee/refusal parity,
  deep graph routes and invalid authoring through the public engine.
- [ ] Prove canonical constructed/powered labs and full authored-duration/modifier/rival/provider parity.
- [ ] Finish remaining industrial/core/host-boundary/durability acceptance before commit/push.

P5-A33 now proves canonical lab/grid physical construction and powered/shutdown saved native research.
The compound parity row stays open: original long study durations, modifiers, rivals and providers
are not all proved. [Physical facility stage](P5_PHYSICAL_NATIVE_RESEARCH_DESIGN.md).
