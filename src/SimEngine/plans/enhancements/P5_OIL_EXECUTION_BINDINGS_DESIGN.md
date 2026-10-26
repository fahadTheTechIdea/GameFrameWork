# P5-A10: Declared execution and access bindings

## 0. Problem statement

OilModule.Hook assigned retired CanWork/WorkRate/SpeedModifier/CanOperate callbacks directly to concrete systems. Construction/access delegate lists no longer exist. This bypasses named capabilities and prevents ordinary module composition.

## 1. Goals

Replace callbacks with existing IWorkerExecution, IMotionExecution and ITransformationGate capabilities; bind placement/access through original contracts; keep one research/state owner; explicitly author every binding in current JSON. Preserve compile/runtime evidence boundaries and no compatibility paths.

## 2. Design

New OilExecution.cs defines stateless domain bindings, not generic simulation owners. ResearchExecutionDef<T> is the sole reader of explicit named id/modifier; the original ContractIdentity grammar validates both, and ResolveCore requires the modifier to exist in loaded tech effects. Worker and motion definitions enforce their actual target capabilities. The shared ResearchExecution<T> reader asks ICompanyResearchEffects (an alias of the original Research service) for the derived multiplier and returns detached ExecutionRateSnapshot. Worker gates use actual incident state; motion retains the original rate-only behavior while the framework applies worker eligibility separately. No dispatcher, movement update, clock, work progress or research cache is duplicated.

OilOperationGate implements ITransformationGate on the actual processor entity and returns the original Operating refusal. Definition requires an actual transformer. There is no new processor or mutable operation state.

Research also implements IContentAccessRule; its persistent ID remains oil.research, while the rule ID oil.access.010-research sorts before oil.access.020-campaign. CampaignAccessRule reads original company outcome. Original content access owns all eligibility/disclosure and command/preview use. OilConstructionPlacementRule applies Ground then Territory to the actual TilePlacement and bound World, replacing two anonymous placement callbacks.

OilModule declares the three component types, binds the original Research instance under read-only effects/access aliases, and registers declared campaign/placement rules. Hook and its call are deleted, not retained as shims.

JSON vehicles explicitly declare17 worker and17 motion bindings; structures declare8 operation gates. IDs/modifiers are content facts, not inferred from tags or entity kinds. Stripping only those new fields recreates both exact pre-edit semantic hashes. Existing amounts, business fields, roles, states, prototypes and content values are otherwise unchanged.

## 3. API surface

Oil extension ICompanyResearchEffects.Multiplier(PlayerId,string). Native engine contracts unchanged; core2.536.0/save154/replay1 unchanged. No new host API or mutable World escape.

## 4. Files

New OilCo.Game/OilExecution.cs. Existing OilModule.cs, Research.cs, Operations.cs, content/oil/vehicles.json and structures.json. Tracker/stage/evidence. No framework executable, host, platform or Tycoon changes.

## 5. Errors

Missing/malformed identities, undeclared modifiers and wrong worker/motion/transformer targets refuse. Foreign processor/world/placement mismatches refuse or fault. Native systems own actual rate/gate validation and physical progress. No callback/default/instant fallback restored.

## 6. Compatibility

Direct development migration: old callback surface removed in place; no duplicate state, scheduler, reader or compatibility alias.

## 7. Verification

Two scoped OilCo.Game compiles only. First7.58s fails one incorrect ComponentDef lifecycle override before body analysis; corrected to its actual ResolveCore contract. Final2.60s fails16 existing errors (founding8, composition8), zero new execution/research/operations diagnostics. JSON inspection validates17/17/8 bindings, zero invalid entries and semantic identity outside added fields. No runtime tests executed in this source-only slice; no old/family/full tests.

## 8. Risks

Oil still cannot compile/load. New capabilities have NOT been admitted through native content/runtime validators, executed physically, saved/replayed or fault-tested. Legacy producer/worker/mover payloads and the remaining layer/navigation/infrastructure/economy/founding paths still need migration. Source compiler acceptance is not content or gameplay success.

## 9. Out of scope

Tycoon business-rule changes, generic scheduler/state additions, platforms/Draw/Unity/Godot, hosts, old sibling engine, full regression or commit/push before the full goal and plan finish.

## 10. Execution outcome

Callbacks/lists removed; declared contracts and42 explicit JSON component bindings added on existing owners. Oil errors24 to16 after one retained early declaration failure. No framework build or runtime rerun needed for this source-only slice. Original221 criterion texts/status/line anchors unchanged (193 recorded checked,28 open); no original gate closure.

[Evidence](../../docs/migration/P5_OIL_EXECUTION_BINDINGS_VERIFICATION.md).
