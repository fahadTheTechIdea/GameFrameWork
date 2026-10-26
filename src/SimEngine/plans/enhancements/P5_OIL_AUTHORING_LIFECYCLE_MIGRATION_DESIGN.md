# P5-A1: Oil extension authoring lifecycle migration — Design Document

## 0. Problem Statement

The actual OilCo.Game project references the external Beep.SimEngine runtime but does not compile: the baseline Release build reports 88 errors. Its definitions override removed public authoring methods, state providers depend on concrete EntityStateDef, and other resource/work/time/composition APIs still target the old framework. No compatibility API should be restored.

## 1. Goals

1. Migrate every oil definition callback to the current guarded ReadCore/ResolveCore/ValidateCore authoring lifecycle.
2. Resolve and return entity state through IEntityStateDefinition instead of concrete native content types.
3. Preserve domain parsing/formulas in this mechanical migration and introduce no second framework or compatibility shim.
4. Report remaining compilation blockers honestly; this foundation is not oil runtime acceptance.

## 2. Design

Update callbacks in the existing Autopilot, Campaign, Commands, Fog, Operations, Research, Reservoirs, Settings and Storyteller source files. Current Def and ComponentDef own once-only read/link/validation/freeze; subclasses implement protected callbacks, not public lifecycle wrappers. Well/Wear/PowerSource/Lab use IEntityStateDefinition properties, resolver aliases and GetState signatures, leaving actual entity Update/state ownership in the framework.

Legacy resource identities, schedule properties, work effects/plans, module composition, domain research/campaign/economy owners and old content remain separate open integration work. Do not make an old project compile by restoring deleted framework APIs. Migrate or replace each responsibility through the authoritative current contract in subsequent implementation.

## 3. API surface

Oil internal authoring callback signatures and state-provider return types only. Public client commands are not changed. Core build/state/view identity remains 2.531.0/save154/replay1.

## 4. Files to change

Nine existing OilCo.Game sources listed above; canonical stage/evidence/master/audit. No framework source, Tycoon, older sibling engine, renderer or platform change.

## 5. Error handling matrix

The existing framework guarded authoring wrappers enforce once-only lifecycle and strict unread-key detection. Missing state aliases resolve through the actual interface catalog. Remaining compile failures prevent runtime claims, not silent fallback.

## 6. Backward compatibility

Development only. No old authoring alias, native state inheritance requirement or compatibility layer.

## 7. Verification

Read current framework lifecycle/state contracts and actual oil callers; capture pre-edit file hashes; focused OilCo.Game Release compile before and after completed migration. No test executor while the assembly cannot compile. Final full regression remains deferred until implementation finishes.

## 8. Risks

Compiler error count can expose additional unresolved symbols as earlier errors disappear; a lower count is not gameplay proof. Resource/work/time/business migrations must not be hidden by this callback migration. Existing dirty source is preserved.

## 9. Out of scope

Full P5 closure, oil domain rule/content redesign, test/host/platform changes, Tycoon business changes, framework compatibility or Git writes.

## 10. Sub-task execution order

1. Record actual external reference and 88-error baseline.
2. Migrate all oil guarded authoring callbacks and state aliases in place.
3. Compile actual extension and record remaining blockers without runtime/full-gate claims.
4. Update canonical master/source evidence; continue remaining integration.

## Partial implementation outcome

All nine source files migrated: 51 signature/state-alias lines changed and mechanically compared against captured pre-edit source, with no unplanned body changes. Baseline compile: 88 errors, exit1, 1.49s. After migration: 35 errors, exit1, 1.71s. No test executor ran. Remaining resource/work/time/visibility/composition and single-owner migration must continue before P5 runtime acceptance. See [verification evidence](../../docs/migration/P5_OIL_AUTHORING_LIFECYCLE_MIGRATION_VERIFICATION.md).
