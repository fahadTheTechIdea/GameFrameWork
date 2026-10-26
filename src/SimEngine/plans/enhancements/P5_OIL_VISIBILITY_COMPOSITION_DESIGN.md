# P5-A6: Native visibility and modular Oil composition — Design Document

## 0. Problem Statement

The Oil Fog implementation owns a duplicate sight grid and depends on removed entity/map properties. Its declaration error hides remaining integration errors. The old StandardModule and fog module no longer represent the framework's contract-based composition.

## 1. Goals

1. Use the existing KnowledgeVisibility and spatial sensor owners; delete the duplicate grid and module.
2. Globally public towns require explicit typed JSON prototype rules, not unlisted entity disclosure.
3. Replace StandardModule references with actual native module dependencies and a matching master manifest.
4. Author geometry and sensor intent on existing prototypes, without duplicate entity catalogs.
5. Prove the new public-prototype rule with one bounded exact case; retain complete Oil integration gates as open.

## 2. Design

EntityPublicVisibilityDef implements IVisibilityTarget in the existing visibility content family. It resolves one actual EntityDef, matches by reference identity and admits real seats only. The original KnowledgeVisibility remains the sole rule evaluator and still ANDs every matching rule; this rule does not bypass other restrictions or create knowledge.

KnowledgeVisibilityModule registers entity_public_visibility. A public rule makes the town prototype visible globally. Forty-one sensor rules scope other prototypes. Own entities remain visible, unlisted entities are explicitly hidden, and ordinary prototype content remains public. No fabricated knowledge fact or globally open entity fallback is introduced.

All forty-two existing Oil prototypes receive explicit tile_presence and tile_sensor metadata. Authored radii remain 14 for bases, 6 for vehicles, 8 for others. Native sensors use rectangular footprint coverage, not the deleted circular cached grid. Vehicles use movable one-tile presence, structures retain authored dimensions, markers do not block. These are deliberate native rules, not compatibility behavior.

OilModule declares seven direct native dependencies and OilCoGame registers thirteen native modules plus Oil and storyteller. The master manifest mirrors actual native module metadata, with one owner per document. All six scenarios select native visibility explicitly. No Tycoon provider or business behavior is changed.

## 3. API surface

New JSON section: entity_public_visibility entries require id and definition. Runtime uses the existing IVisibilityTarget contract and owner. Oil no longer registers FogRulesDef/Fog/FogModule or StandardModule aliases.

## 4. Files to change

Framework KnowledgeVisibility.cs, KnowledgeVisibilityModule.cs and build identity; protocol expectations, one new thin test and two authored fixture files. Delete Oil Fog.cs and old fog_rules document. Revise OilModule, master, scenarios and existing prototypes; add Oil visibility.json. Update tracker/evidence/audit.

## 5. Error handling matrix

Missing public prototype: content refusal, no substitution. Invalid seat/foreign target: no visibility grant. Unlisted unrelated entity: hidden. Additional matching knowledge/sensor rules: original conjunction remains authoritative. Unresolved Oil geometry, old content schemas and body APIs: explicit integration failures, no compatibility shim or default map.

## 6. Backward compatibility

Development only. The old Fog code/document and StandardModule references are deleted. Engine identity advances to 2.532.0; save154/replay1 unchanged. Prior runtime evidence is historical, not rerun.

## 7. Verification

Framework test project Release --no-restore build; exactly one new bounded public-visibility case. One OilCo.Game Release --no-restore compile after the completed source slice. JSON source inspection validates manifest paths/dependencies and prototype/rule counts. No old/family/full runtime.

## 8. Risks

Oil compilation is still failing. Native geometry currently names oil.tiles, but per-scenario geometry is unresolved. Six differing map dimensions remain preserved, not forced into one size. Native activity, resource, navigation, storage and module settings still require migration. Source JSON inspection is not engine content loading.

## 9. Out of scope

Platform/Draw/adapters, Tycoon business behavior, complete Oil runtime or original broad gate closure, final full regression and Git writes.

## 10. Sub-task execution order

1. Add explicit public prototype metadata to the original visibility owner.
2. Prove exact scope, hidden unrelated entities, saves and missing references.
3. Delete Oil Fog and declare current module/JSON composition.
4. Compile Oil to expose previously masked body errors and preserve them.
5. Continue sole-owner per-scenario space authoring, then spatial/resource/activity/operation and optional business migration.

## Required next spatial slice

Extend the sole tile-space authoring/composition contract to explicit per-scenario geometries under a stable space identity. Each session must derive its own bounds from its selected immutable scenario, with no mutable shared metadata, duplicated prototypes, universal dimensions or engine size clamp. Preserve 96x96, 128x128, 224x224, 64x64, 112x112 and 96x96 respectively. Save/load, interleaved sessions and spatial previews must use the same resolution.

## Outcome

Framework exact case: 50 passed, 0 failed, 0 aborted. Oil source migration remains incomplete: 85 body/API errors exposed after the obsolete Fog declaration was removed. See [evidence](../../docs/migration/P5_OIL_VISIBILITY_COMPOSITION_VERIFICATION.md).
