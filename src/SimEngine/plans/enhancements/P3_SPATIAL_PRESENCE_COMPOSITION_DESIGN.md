# P3-S22 — spatial presence contract admission before initialization

## 0. Problem

Original P3.acceptance.004 requires absent spatial providers to fail composition without a default map. World.FinishComposition validates selected space identity/layers and selected service consumers, but an unused presence-only entity has no generic geometry admission. TilePresenceDef resolves authored tile metadata while a same-ID non-tile runtime provider can survive until spawn. Startup checks and motion admission do not cover unused static presence without those consumers.

## 1. Goals

Require every actual selected-space presence definition, including unused definitions, to validate its required geometry before Initialize. Preserve unselected-space and nonspatial optionality. Detect actual callback clock/identity mutations and preserve the first thrown error. No default map or provider substitution.

## 2. Design

Extend the existing ISpatialPresenceDefinition with mandatory ValidateComposition(World, EntityDef). World.FinishComposition owns one all-entity pass after service freeze and layer admission, before the existing provider/service pass and Initialize. Reject mismatched capability/definition declarations, observe actual Space and callback through the original EntityObservation boundary, skip other authored spaces. TilePresenceDef requires its actual TileMap service alias. Independent PointPresenceDef requires its actual PointSpace. No new capability model, service, geometry, state machine, stock, scheduler or clock.

## 3. API surface

Mandatory pure presence metadata admission method; engine behavior identity 2.422.0. Save154/replay1 and all existing state payload schemas unchanged. No host API or renderer.

## 4. Files

Kernel/SpatialContracts.cs and World.cs own contract/call order. Map/TilePresence.cs and the existing independent PointSpaceProof.cs own geometry-specific admission. Kernel/SaveGame.cs and current-engine-protocol.json carry behavior identity. New SpatialPresenceCompositionProof.cs supplies negative instrumentation only; new thin SpatialPresenceCompositionTests.cs is registered once. Original master/world/routes files supply authored composition; driver edits authoring intent, never runtime state.

## 5. Error handling

Missing/mismatched selected geometry and capability declarations fail ContentException before Initialize. Pure callback clock/identity changes fail through the existing first-fault boundary. A callback throwing after mutation preserves its original exception; no finally-based replacement. Wrong runtime provider never reaches spawn or snapshot publication.

## 6. Compatibility

Development-only breaking mandatory contract migration. No compatibility overload, legacy path or invented default.

## 7. Verification

One solution build and ONE NEW exact bounded case, same-case repairs only. Native/independent tile definitions, absent/wrong-ID/wrong-shape/missing-layer providers, unused direct admission callback faults, valid empty geometry and nonspatial optionality. Public physical tile travel and independent point production, partial saves, paused sole clock, detached facts, replay and interleaved isolation. Max96 tile Updates/256 point Updates, outer30000ms, owned termination5000ms, output30000 chars. Record actual attempts, not historical successes.

## 8. Risks

Custom presence authors must implement actual geometry admission, not pretend a same-ID provider is compatible. The original scoped observation is not a hostile CLR sandbox. Layer callback purity and every higher-level construction/rollout/recovery/control provider matrix remain separate requirements; this stage alone does not close all P3.acceptance.004 or full P1-P12.

## 9. Out of scope

Oil/domain/Tycoon/platform/Draw/adapters, full or old runtime suites, new geometry or native assumptions in kernel, protected G19 migration. No staging/commit/push before full goal AND plan finish.

## 10. Execution

1. Add the mandatory existing presence contract and sole kernel admission pass.
2. Migrate native tile and independent point declarations in place.
3. Compile and run only the new bounded public-driver case, repair that case if necessary.
4. Record actual evidence and update master/audit without marking compound original gates complete.
