# P5-A4: Oil physical work-effect migration — Design Document

## 0. Problem Statement

The oil extension still uses removed OrderDef/StepDef/WorkEffectDef APIs. Drilling completion despawns its pad before well creation/admission, risking stock/site loss. Survey completion scans all surveys and can silently retire an unmatched stake. Effects lack explicit content dependencies and authoritative whole-order physical admission.

## 1. Goals

1. Survey/work/research/autopilot references use actual IAuthoredActivityDefinition aliases directly.
2. Drilling/survey effects implement current physical work contracts over authored dependencies, not compatibility base classes.
3. Drilling uses sole Upgrades replacement and kernel removal admission; refusal cannot delete the pad first.
4. Survey selects an explicit authored binding and retires through kernel guards; no unmatched silent completion.
5. Preserve unsurveyed drilling: start admission checks owned authored target only, never seismic/logging knowledge.

## 2. Design

Revise existing DrillingEffectDef/SurveyEffectDef as Def plus IPhysicalWorkEffect/IWorkEffectIntentAdmission. Their Check/Apply delegate PhysicalWorkEffectContracts; ValidatePlan requires whole-order completion. Explicit pads/results and stake/surveys JSON resolve actual catalog aliases. Runtime geological outcome remains oil logic; the generic scheduler is not modified.

Drilling final admission selects the actual reservoir under ITilePresence and checks removal for dry holes or Upgrades.Check for declared well results. Commit uses Upgrades.TryReplace to preserve geometry/state/stock; only successful replacement binds actual well/reservoir knowledge and notifications. Dry hole removal uses the existing kernel guard. Start admission has no surveyed/logged prerequisite.

Survey whole-order binding matches actual declared survey activity. CheckSubject validates target and kernel removal. Commit retires the actual stake before non-refusing discovery/notification; exceptions fault the existing world, not a healthy partial success. Unknown binding refuses.

## 3. API surface

Current activity/node/physical-effect contracts and authored effect fields replace old types. No compatibility alias, fallback dependencies or duplicate work/replacement/lifecycle owner.

## 4. Files to change

Oil Commands, Operations, Autopilot and Research; existing content/oil/effects.json; canonical stage/evidence/master/audit. Core/Tycoon/hosts/platforms unchanged.

## 5. Error handling matrix

Missing/duplicate authored source/result/survey references refuse content. Foreign/missing target refuses. Wrong order/node binding refuses whole-order validation. Unsafe replacement/removal returns original engine blocker unchanged. No geological knowledge prerequisite is inserted. Provider/commit exceptions remain first-fault behavior; no compensation or instant fallback.

## 6. Backward compatibility

Development only. Old OrderDef/StepDef/WorkEffectDef paths in the migrated effects/aliases are removed, not reintroduced. Incident response still has old work references and remains separate open integration.

## 7. Verification

Completed scoped OilCo.Game Release compile and old-type/source-boundary search; no executor until remaining integration compiles. Full physical unsurveyed drilling, durability, privacy and effect/provider acceptance remain open. One final full regression deferred.

## 8. Risks

Type errors may mask later body/composition/API failures. Actual effect dependency closure and physical replacement/cleanup must be tested after content/module migration. No compile-count improvement implies physical correctness.

## 9. Out of scope

Full oil runtime/content/DAG/business/incidents/visibility/controller migration, hosts/tests/platforms/core changes and Git writes.

## 10. Sub-task execution order

1. Migrate authored activity aliases and explicit effect JSON.
2. Replace destructive effect paths with authoritative physical contracts/coordinators.
3. Compile, retain blockers and update tracker/evidence/freeze.

## Partial implementation outcome

Four existing code files and authored effects JSON revised; destructive drilling despawn/spawn path removed. Compile12 ->7 reported errors, still exit1; three observed compiles1.36s/1.32s/0.98s, zero warnings. Authored pad/result/stake/survey references exist in current source files, not engine-load proof. Physical effects/start admission and dependencies are source-implemented; full unsurveyed execution/stock/privacy/durability/incident/content integration remains unproven. See [verification evidence](../../docs/migration/P5_OIL_PHYSICAL_EFFECT_MIGRATION_VERIFICATION.md).
