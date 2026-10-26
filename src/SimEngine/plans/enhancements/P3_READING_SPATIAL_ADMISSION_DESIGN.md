# P3-S27: contract-based spatial reading admission — Design Document

Current M86/native2.727/save161/replay2 repairs direct fresh/regional coverage observation through
the original EntityObservation owner; reading admission/declaration/state payloads are unchanged.
Old direct errors/selected-space mutation genuinely reproduce304/38; final coverage398/0 and
affected positives569/0 pass narrowly, not a default admission/full regression claim.
[All attempts, source ownership, actual images and limitations](../../docs/migration/P11_KNOWLEDGE_FRESHNESS_VERIFICATION.md#current-m86-self-observed-spatial-coverage-strategies--2026-10-22).

Earlier M85/unchanged native2.726/save161/replay2 verifies finite independent PointReadingDef
coverage through the same SpatialReadingDeclaration, ledger, native fresh visibility and point
provider.264/0, one new literal case/two time settings/two complete final-hash replays. No new
provider/executor/clock or native implementation. [Actual selected scope and remaining gates](../../docs/migration/P11_KNOWLEDGE_FRESHNESS_VERIFICATION.md#current-m85-independent-finite-spatial-readings--2026-10-22).
The original admission repair below is already implemented; old save/build numbers are historical.

## 0. Problem Statement

`Standard/SpatialReadings.cs` locates layers only during `CheckTarget`/`Observe`; native `TileReadingDef` can initialize with metadata-only geometry and later throw because its runtime layer is not `MapLayer`. `KnowledgeContentContracts.ValidateSubject` validates retention/time but not this dependency, and its `finally` observation can mask a first callback error. Native/point readers also inherit an obsolete `SpatialReadingDef` base.

## 1. Goals

1. Every loaded spatial knowledge subject validates its actual layer/space metadata and selected reading geometry before Initialize, including unused definitions.
2. Expose the selected-layer validation through `ISpatialKnowledgeSubjectDefinition`; native and independent definitions implement contracts directly and share one declaration/normal observation strategy.
3. Remove the abstract reading base without a shim, duplicate catalog or sample/knowledge state. Preserve JSON shape and save154/replay1.
4. Keep foreign geometry optional in explicit nonspatial sessions; native/point actual physical work learns typed samples over game time and survives saves/pause/replay/private queries.
5. Preserve the actual first admission callback error even if that callback also changes the game clock.

## 2. Design

Replace `SpatialReadingDef` in place with `SpatialReadingDeclaration`: one reader/resolver for layer/retention/freshness, one existing generic target/observation/value validation strategy. Native `TileReadingDef` and independent `PointReadingDef` become direct `Def, ISpatialKnowledgeSubjectDefinition` implementations delegating that sole declaration. Extend the spatial subject contract with `ValidateLayer(World, ISpatialLayer)` for pure provider-specific selected geometry admission. Native reader requires actual matching `TileMap`/`MapLayer`; point reader requires actual `PointSpace`/`PointLayer`. No universal tile assumption in the shared strategy.

`KnowledgeContentContracts.ValidateSubject` remains the sole all-loaded admission owner. Replace its `finally` guard with existing `EntityObservation.ReadProvider`, and invoke `SpatialReadingDeclaration.ValidateBinding` for spatial subjects. Check frozen actual layer/space aliases even for foreign content; selected geometry checks use original whole-space/clock observation. Skip runtime-layer requirements only when the authored space is genuinely unselected. Original CompanyKnowledgeService, WorkBoard, entity motion/resources/clock remain authoritative.

## 3. API surface

New mandatory spatial-subject `ValidateLayer(World, ISpatialLayer)`; public shared declaration authoring/operations, no required inheritance base. Removed `SpatialReadingDef` with no compatibility alias. Existing metadata and observation payloads unchanged; build2.454.0/save154/replay1.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| Modify | `src/Beep.SimEngine/Standard/SpatialReadings.cs` | 130 | medium |
| Modify | `src/Beep.SimEngine/Standard/CompanyKnowledge.cs` | 8 | medium |
| Modify | `tests/ExtensionProof/PointReadingProof.cs` | 25 | medium |
| New | negative admission module/driver and explicit JSON/master/point activity documents | 250 | medium |
| Modify | protocol build, proof resources, runner and checkpoint documents | scoped | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Missing selected provider/layer or unsupported actual reading geometry | ContentException before Initialize |
| Valid frozen foreign reading with no selected geometry | Metadata admitted, no map or samples synthesized |
| Provider admission throws | Preserve original first exception; no initialized session |
| Admission changes actual clock | Original observation refuses, zero Initialize |
| Normal physical observation | Existing work/input/knowledge owners commit earned typed sample |

## 6. Backward compatibility

Development framework: remove obsolete inheritance, no compatibility/legacy path. No second saved knowledge or layer model.

## 7. Verification

ONE NEW exact bounded case: `Spatial reading admission: direct contracts require actual layers and preserve physical knowledge`. One solution compile attempt; necessary same-case targeted repairs only. Invalid unused provider/layer/direct-callback variants and explicit nonspatial saves; native tile and direct point minute/half physical material-consuming sample work, saved partials/pause/private facts/session isolation/replay. No old/family/full runtime. Preserve all attempts/current source/input hashes. Original P3.004 remains open until every spatial consumer has adequate evidence.

## 8. Risks

Do not confuse an actual metadata-only layer with a readable quantity provider. Observe actual geometry rather than probing live samples before initialization. Foreign loaded layers remain valid authoring, not current runtime requirements. Public driver never calls subject/provider/world directly or writes learned stock/progress. Shared declaration must not copy native/point geometry rules.

## 9. Out of scope

Oil consumer, platform/Draw, protected clock/G19/advanced Tycoon, broader exploration/profile/provider purity, all-consumer/full/stress/performance gates, publication before entire goal AND plan completed and verified.

## 10. Sub-task execution order

1. Review existing contracts/readers/admission and author stage.
2. Replace inheritance with one declaration/direct bindings; require actual selected geometry through the contract.
3. Add one explicit master/JSON/public-player proof, compile and run the one bounded case; retain failures.
4. Refresh staged evidence/tracker/anchors; preserve original221 gate text/line/check box states and leave broader requirements open.

Status: scoped implementation verified on2.454.0/save154/replay1; full goal AND plan remain OPEN. Lifecycle skill governs staged ownership/evidence; user prohibition supersedes generic full-suite advice.

## Verification result

ONE NEW exact FIRST/FINAL319 passed/0 failed/0 aborted in1432ms; Exited0/PID36136/process1585ms, no stdout/stderr truncation or observer error. Six invalid unused cases reject before Initialize, including metadata-only actual layer and direct clock/throw callbacks preserving first errors. Native and foreign explicit nonspatial empty sessions save and continue without fabricated geometry/entities.

Four ordinary physical variants learn only after actual motion/material-consuming work. Native minute partial28/learn57/refresh70; half54/113/138. Direct point minute6/23/30; half10/45/58. Each starts20 inputs, consumes8 once, learns actual31 native deposit or actual0 empty point terrain, freezes company-private facts, saves paused partial work and earned refresh, retains detached old payloads and matches replay/interleaved-session hashes. Existing native empty layer semantics are actual world data, not a sample fallback. Both compile attempts retained: initial solution8.24s failed on a fixture-only nonexistent JsonValue.IsFrozen property; targeted4.57s clean after replacing that assertion with an actual rejected payload-mutation check. No runtime before clean compilation and no executable edits after FIRST/FINAL. Original221/184checked/37open remain unchanged; original P3.004 is broader than this proof and remains OPEN.

## Metadata closure

Current closure:1553 frozen source anchors,13 quickstart anchors,822 selected executable inputs; zero hash mismatches/post-pass executable drift.2754 paths/968 Markdown files/8589 local links, zero broken links;18 valid master rows;221 original literal gate/checkbox anchors exact and unchanged (184checked/37open), additional180. Thin driver boundary scan empty. git diff --check exit0 (34 existing line-ending-warning output lines),2742 dirty paths/zero staged. Sole clock and protected G19 source hashes unchanged. Twenty selected executable inputs added: fourteen existing dependencies (PointReadingProof refactored in this slice, thirteen unchanged) plus six genuinely new proof/driver/JSON files. Source anchors add fourteen existing dependencies plus eight new files. Six previously frozen executable inputs changed (three production); no executable edit after FIRST/FINAL. Metadata integrity is not full gameplay/provider/acceptance evidence.
