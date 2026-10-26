# P2-F10: Component composition prerequisites — Design Document

## 0. Problem Statement

`World.FinishComposition` validates loaded spatial presence metadata before initialization, but other JSON-backed component definitions have no equivalent generic contract. Mods must invent a module-specific validation wrapper for required service/geometry contracts. A common lifecycle hook must not become another singular entity capability or a second validation registry.

## 1. Goals

1. Every actual loaded component implementing `IComponentCompositionValidation` validates its session prerequisites exactly once per composition, unused prototypes included.
2. Multiple metadata validators coexist on one entity; native presence retains selected-space filtering and its existing contract.
3. Missing/mismatched providers and observed clock/geometry mutations refuse before any system initializes, with no fallback map or fabricated service.
4. Ordinary point-space work and physical hauling retain time, resource conservation, saved continuation, replay, private views and session isolation.

## 2. Design

Extend `Content/Defs.cs` with `IComponentCompositionValidation : IComponentDefinition`, exposing `void ValidateComposition(World world, EntityDef entity)`. It is a plural lifecycle contract, excluded from singular metadata ownership and query paths. `ISpatialPresenceDefinition` inherits it; no legacy wrapper remains.

The existing `World` composition owner enumerates the actual frozen `EntityDef.Components`. It validates actual owner/catalog identity and invokes callbacks after registration freeze, before service preparation/validation and system initialization. Providers may inspect frozen service aliases but cannot assume initialized/prepared state. Only inactive spatial presence skips selected geometry validation; other component hooks are always invoked. Original `EntityObservation` scopes guard actual metadata, scalar world facts and the selected spatial provider codec. This is not a CLR sandbox or whole-world rollback guarantee.

Non-friend engine-side proof metadata reads explicit required-contract names and space identity from a master-listed JSON document. It owns no gameplay state or scheduling. Positive driver uses public player input/views and bounded `Update`, never `Game.World`, `TestKit`, direct stock or progress writes. Invalid-provider and mutation modes are negative instrumentation only.

## 3. API surface

One public metadata lifecycle interface; no host rendering, controller, command, save-payload or view provider is added. Build identity becomes 2.444.0; save154/replay1 unchanged.

## 4. Files to change

- Modify `src/Beep.SimEngine/Content/Defs.cs`, `Kernel/SpatialContracts.cs`, `Kernel/World.cs`, `Kernel/SaveGame.cs`.
- Add engine-side proof metadata, master-listed JSON and one exact public-input case under `tests`.
- Refresh current protocol JSON, tracker and scoped evidence.

## 5. Error handling matrix

Missing authored references fail original content resolution. Missing service/space capabilities, wrong selected identity, callback exceptions or observed mutation fail original composition before Initialize. The first original observation fault is preserved; no replacement provider, partial startup or instant completion exists.

## 6. Backward compatibility

Development-only breaking contract change. No compatibility or legacy path.

## 7. Verification

One solution compile, then ONE NEW exact case: `Component composition: authored prerequisites validate before initialization and preserve physical execution`. Same-case repairs only. Retain every build/runtime attempt; no historical/family/full-suite runtime. Metadata hash/link/gate checks follow final executable freeze.

## 8. Risks

Plural hook ambiguity is prevented by explicit enumeration and rejection of singular lifecycle queries. Provider callbacks remain scoped observations, not arbitrary private CLR purity or global rollback. Loaded other-space presence stays inactive. Services needing prepared state belong to the existing service validation pass, not this earlier registration-prerequisite hook.

## 9. Out of scope

Oil consumer, Draw/platform adapters, Tycoon/business, alternate catalog/state/clock/scheduler, broad regression, stress budgets, generic arbitrary-provider purity and full P1–P12 completion. Original P2.003/P3.004/global gates remain open absent their complete evidence.

## 10. Sub-task execution order

1. Extend actual metadata contract and original composition owner.
2. Author independent prerequisite declarations and bounded physical/refusal proof.
3. Compile and run only the exact new case; preserve failed attempts and diagnose same-case repairs.
4. Freeze executable inputs, reconcile tracker/verification hashes and leave unsupported broader claims open.

Status: implemented and VERIFIED-SCOPED. FINAL106/0/0 in2405ms, PID32376 Exited0; native physical corridor plus two point quantum work/haul flows and12 early refusals. The initial solution compile failed new negative-clock instrumentation (CS1061); focused repair compiles succeeded6.18s/11.97s. First106-check pass is historical because the metadata/geometry scope ordering was subsequently repaired; only the final run proves current executable source. No executable edits after FINAL. Original221/178checked/43open remain unchanged. Full goal/plan/publication remain open.

[Scoped evidence](../../docs/migration/P2_COMPONENT_COMPOSITION_VERIFICATION.md)

## Read-only closure

Actual metadata check:1450source anchors/734selected executable inputs/13quickstart anchors; no hash drift. All221literal requirements/checkboxes retain their exact text, status and line anchors;18master rows remain valid.2692paths/944Markdown/8532local links, zero broken. New positive driver boundary matches0; git diff --check exit0 with34existing line-ending warnings. Four protected clock/G19 hashes unchanged. Fourteen current headers replaced in place; original178checked/43open and full goal/publication remain open. Dirty2680/staged0; no executable edits after FINAL.
