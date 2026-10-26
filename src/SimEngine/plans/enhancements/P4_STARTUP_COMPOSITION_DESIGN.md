# P4-N4 — Native startup composition admission

Historical P4-N4 runtime evidence; not rerun on2.331.0. P4-N5 now resolves startup definitions/profiles through content contracts; composition admission and actual generation remain with the same owners. [Current evidence](../../docs/migration/P4_STARTUP_CONTENT_CONTRACTS_VERIFICATION.md).

Status: scoped implementation verified on core2.330.0/save154/replay1; ONE NEW bounded exact case92/0/0 in537ms. Follows [sole native point startup](P4_NATIVE_POINT_STARTUP_DESIGN.md); full P1–P12 remains open.

## Problem and goals

In Kernel/Modules.cs, GameDefinition.Build freezes composition and initializes systems before invoking world-generation stages. Before this slice Services.ValidateComposition visited only services and systems. Native WorldSetupStage had no composition-validation callback, so invalid actual company enrollment, cross-company authored homes and malformed/provider-mismatched startup placement can reach Initialize before refusing. WorldSetupDef previously silently supplied an empty after list when the JSON field was omitted.

Use the existing ICompositionPreparation/ICompositionValidator pipeline for registered generation stages as well, with once-only reference alias handling. Native selected entries must explicitly validate their authored declarations before any system Initialize. Do not create a second startup service, validator registry, scheduler, graph, spawner or clock.

## Contracts and ownership

Extend World.FinishComposition and ServiceContainer.ValidateComposition to receive the composer's already ordered stage list. Prepare all actual service/system/stage providers before validating any, preserving the existing alias deduplication. GameDefinition.Build obtains the stage list once and uses it for both validation and the existing generation loop. Restore composes and validates but does not generate or grant anything.

Require WorldSetupDef/IWorldSetupDefinition.ValidateComposition(World). WorldSetupStage implements the existing ICompositionValidator and delegates to each selected native graph entry under the existing scoped entity/clock observation guard. No concrete entry type switch in the stage.

EntitySetupDef checks actual requested seat enrollment, authored same-company home definition, selected presence/provider identity and strict provider-owned placement decoding. Share the current company rule between composition and actual generation. Existing ISpatialPlacementIntent handles JSON geometry; use the current scoped provider observation guard, not host geometry interpretation. No retained decoded placement or new state.

TileLayerSetupDef and the external PointLayerSetupDef explicitly implement their existing pure declaration checks. WorldSetupDef requires an explicit after array, with topology still handled by the sole DependencyGraph compiler.

## Static versus actual admission

Composition checks declarations, capabilities, authoring and requested seats. It does not fake actual home entities, generate terrain early, reserve sites, predict occupancy or preflight a future world's live conditions. Actual Check/Generate remains authoritative for current placement bounds, generated terrain, collision, current dependency results and actual home ownership. No whole-generation rollback/transaction promise.

## Files and execution

1. Extend existing composition traversal in Kernel/Modules.cs, World.cs and Services.cs.
2. Extend native startup definition/stage validation in Standard/WorldSetup.cs, and update the external PointLayerSetupDef contract implementation.
3. Change engine build/protocol identity together to2.330.0, retaining save154/replay1 and state layouts. Update old source assertions that intentionally described the now-corrected startup timing; do not rerun them.
4. Add a single new explicitly authored normal-master public-player case. Include stage lifecycle/failure instrumentation only in engine module proof code, not a host-side startup implementation.
5. Compile as needed, then ONE NEW bounded exact case; repair/rerun only that case. Prove before-Initialize refusals, once-only stage preparation/validation, stage generation only for new games, pure decoding guards, real timed work/hauling, saves/no regrant and replay.
6. Record actual evidence and update the master/audit without changing original acceptance checkbox text/counts.

## Exclusions and risks

All older fixtures source/compile only. No full/family/Tycoon/Oil/platform runtime, business behavior expansion, renderer/Draw/adapter, second clock or compatibility reader. Provider codecs must remain readable at the same pre-Initialize boundary used by composition; scoped observation is not a global sandbox. Actual runtime/generation occupancy and broad fault/scale/mod/global-purity/full P1–P12 remain open. No staging/commit/push until full goal AND plan completion and verification.
