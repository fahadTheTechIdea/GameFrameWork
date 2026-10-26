# P3-S29: Shared spatial observation — Design Document

## 0. Problem Statement

EntityObservation.ReadProvider accepts a concrete TileMap and guards only its version; common executor/facility/access/construction/knowledge/technology/weather/line/effect paths cast away non-tile spaces. Refused effects and template callbacks repeat that concrete guard. The previous utility-specific nested guard fixes only one caller. These are spatial framework/contract guarantees; the separate original P3.acceptance.004 missing-required-provider composition gate remains open.

## 1. Goals

1. The original observer accepts ISpatialSpace and guards its actual existing codec and original observation version, without a second state owner.
2. Every existing tile-only observation argument uses the actual selected space; native geometry algorithms remain in their provider.
3. Original refused-effect/template guards use the same shared spatial scope, verifying only refused effects so earned applied effects may legitimately change space.
4. Prove native/point minute/half physical effects, pure previews, partial saves, replay and isolation; negative persisted and restored-payload/version-only callback changes fail.

## 2. Design

ISpatialSpace requires a pure nonnegative ObservationVersion backed by its existing invalidation counter. TileMap exposes Version; PointSpace exposes its existing observer counter. No counter, codec or layer is added. Immutable negative composition fixtures explicitly return0.

EntityObservation gains one transient SpatialScope over actual identity, original ProviderScope and ObservationVersion. Constructor and Verify check the actual selected alias, pure version getter and nonnegative version. ReadProvider replaces TileMap argument/version code with that scope. Common callers migrate in place; utility-specific nested observation becomes the shared space argument.

WorkEffectContracts/PhysicalWorkEffectContracts verify this scope on refused commits only. WorkBoard template callbacks use it on success and typed authoring refusal. The existing first-fault boundary remains authoritative.

## 3. API surface

ISpatialSpace.ObservationVersion is required, with no default or compatibility overload. ReadProvider remains internal; its optional spatial argument is ISpatialSpace. Existing public player commands/views and payload schemas unchanged.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| Modify | src/Beep.SimEngine/Kernel/SpatialContracts.cs, EntityObservation.cs | 55 | medium |
| Modify | src/Beep.SimEngine/Map/TileMap.cs, tests/ExtensionProof/PointSpaceProof.cs | 2 | low |
| Modify | common Standard observation call sites, WorkEffects.cs, WorkPlanTemplates.cs, UtilityConsumers.cs | 40 | medium |
| Modify | existing negative access/effect probes and authoring factories | 50 | low |
| New | tests/ExtensionProof/content/shared-spatial-observation.json | 20 | low |
| New | tests/Beep.SimEngine.Tests/SharedSpatialObservationTests.cs | 190 | medium |
| Modify | Program.cs, ExtensionProof.csproj, engine build/protocol, immutable composition fixtures | 10 | low |

## 5. Error handling matrix

| Callback | Required result |
|---|---|
| Custom layer changed | No healthy facts; original fault and update/save cutoff |
| Layer restored but original observer version advanced | Still refused; codec equality alone is insufficient |
| Callback throws after changing space | Original callback error survives; no successful facts |
| Refused effect changes space | Fault, not healthy pending completion |
| Successful earned effect changes space | Normal original commit and durability |
| No selected space | No invented geometry/version provider |

## 6. Backward compatibility

Dev only; replace concrete observation in place, migrate every provider, no legacy overload or default interface method.

## 7. Verification

One NEW exact bounded public-player case: FINAL776/0/0 in118982ms, process119175ms of120000ms. All four attempts retained, including one aborted timeout without returned totals. One failed solution compile (probe local shadow) and four clean focused compiles11.39s/18.26s/9.58s/9.21s; no solution rerun, historical/family/full suite or executable edit after FINAL. Four native/point physical construction/template flows, two earned research flows,48 access callbacks and8 refused-effect flows pass. Source audit finds no observation tile casts. Near-limit wall time is a qualification, not a performance certificate; every private CLR/fog/performance/global gate remains open.

## 8. Risks

An actual observation version must detect transient invalidation, including restore-to-same-payload; it must not be copied into save state. Successful effects legitimately mutate geometry and must not be treated as pure reads. Space getter instrumentation must preserve the original first fault.

## 9. Out of scope

Oil/platform/Draw/adapters/Tycoon/G19, arbitrary providers/fog/claims/pressure/control/stress/full completion and publication.

## 10. Sub-task execution order

Inspect original observers and actual version owners; record stage; implement shared contract/scope and migrate callers/providers; extend negative probes and author public-player proof; compile/run exact case; retain failures and refresh tracker/audit identities with qualified evidence.

Metadata closure: 1,630 source anchors, 878 selected executable inputs and 13 quickstart anchors match frozen SHA-256 values. All 221 original gate objects/literals/checkboxes remain unchanged (184 checked, 37 open); additional192 is scoped evidence, not aggregate completion. All 8,620 local links resolve; 18 master rows have the expected shape, the new driver boundary scan is clean, and git diff --check exits0. Source audit has zero observation tile casts; all five current spatial providers implement the required original revision. Thirty-one existing inputs changed, twenty production, with no new production owner. Sole clock/protected Tycoon hashes unchanged; no executable edit after FINAL and nothing staged.
