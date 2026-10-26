# P3-S26: required dock geometry — Design Document

## 0. Problem Statement

`Standard/WorkScheduling.cs` admits every loaded dock's metadata before initialization, but `Standard/WorkDocks.cs` first discovers a missing `ISpatialOverlap` during work. A selected spatial dock must not enter a session incapable of checking its physical service/departure geometry. This is a concrete part of the still-open original P3.acceptance.004, not a claim that every spatial consumer is covered.

## 1. Goals

1. All loaded dock definitions require an actual spatial-presence declaration/runtime pair.
2. Dock definitions belonging to the selected space require the actual matching `ISpatialOverlap` before any Initialize, even if unused.
3. Unselected geometry remains optional; deliberately nonspatial sessions never create a map.
4. Existing native-tile and independent-point providers remain valid. Ordinary physical hauling retains elapsed handling/motion, saves, hashes and replay.

## 2. Design

Extend the sole `WorkDockRules` admission with `ValidateComposition(World, EntityDef)`, called from the original `WorkSystem.ValidateComposition`. Observe the actual definition and selected space through existing `EntityObservation.ReadProvider`; keep `ValidateDefinition` as the single slot-metadata validator. Require `ISpatialPresenceDefinition`/`ISpatialPresence`, then skip only the selected-runtime requirement for a foreign authored space. Require `ISpatialOverlap` and matching actual identity for selected docks. No registration fallback, new requirement registry, scheduler, state owner, or compatibility layer.

Negative instrumentation declares an unused direct-contract dock/presence and a deliberately non-overlap space using the existing negative provider. It never supplies normal geometry/gameplay. New authored JSON/master composition selects original work, inventory, access, motion and startup modules. Positive drivers use actual JSON and public player interfaces only.

## 3. API surface

One public standard validation method for engine-side authors; existing dock/presence/runtime contracts and payloads unchanged. Build identity advances for changed production admission; save154/replay1 unchanged.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| Modify | `src/Beep.SimEngine/Standard/DockContracts.cs` | 25 | medium |
| Modify | `src/Beep.SimEngine/Standard/WorkScheduling.cs` | 1 | low |
| Modify | `src/Beep.SimEngine/Kernel/SaveGame.cs` and current protocol JSON | 2 | low |
| New | `tests/ExtensionProof/DockSpatialAdmissionProof.cs` | 90 | low |
| New | `tests/ExtensionProof/Content/dock-spatial-admission.json` | 35 | low |
| New | `tests/Beep.SimEngine.Tests/DockSpatialAdmissionTests.cs` | 180 | medium |
| Modify | proof resources / runner registration / checkpoint documents | scoped | low |

## 5. Error handling matrix

| Situation | Result |
|---|---|
| Selected dock without overlap geometry | ContentException before Initialize |
| Dock without actual declared spatial capability | ContentException before Initialize |
| Valid selected native/direct dock | Existing owner admits; no synthesized geometry |
| Foreign dock in explicit nonspatial session | Metadata admitted; no selected map required |
| Actual observer/provider failure | Existing first-fault observation semantics; no prefix/fallback |

## 6. Backward compatibility

Development framework: no compatibility or legacy path. Existing state schemas do not change.

## 7. Verification

ONE NEW exact bounded case: `Dock spatial admission: required overlap rejects before initialization and preserves physical hauling`. One solution compile attempt; necessary same-case repairs only. No historical/family/full runtime. Negative unused metadata, native/direct empty composition, explicit nonspatial save/update, and ordinary timed physical hauling/save/replay/session isolation comprise this one case. Preserve all attempts and selected executable hashes. Full P3.004 and P1–P12 remain open absent exhaustive provider evidence.

## 8. Risks

Over-requiring current geometry for foreign prototypes would break multi-scenario catalogs; skip selected-runtime checks by exact authored space identity only. Thin drivers must not create stock, jobs or progress. Dock provider instrumentation is unused negative admission, not a substitute implementation.

## 9. Out of scope

Oil consumer, SDKs/rendering/Draw, protected clock/G19/advanced Tycoon, all-consumer spatial completion, stress/full acceptance, commit/push before full goal AND plan completion.

## 10. Sub-task execution order

1. Inspect original dock, presence and composition owners; record actual gap.
2. Add original-owner selected-provider admission and new authored negative instrumentation.
3. Build once, then run ONE NEW exact bounded case, retaining every same-case repair.
4. Publish scoped evidence, refresh current tracker/hashes and preserve original gate wording/check boxes.

Status: scoped implementation verified on2.453.0/save154/replay1; original P3.004/full goal remain OPEN. Development-lifecycle skill supplies staged ownership/evidence discipline; user restriction supersedes its generic full-suite recommendation.

## Verification result

ONE NEW exact FINAL414 passed/0 failed/0 aborted in1985ms, Exited0/PID38228/process2129ms, no truncated output or observer error. All five invalid unused variants fail before Initialize; matching presence without required overlap refuses explicitly. Native tile/foreign nonspatial empty content initializes without fabricated instances, saves and continues. Actual tile minute hauling reaches loading7/delivery36/complete58; independent point minute5/24/41 and half-minute9/47/80. Finite32→24+8 or20→12+8, cargo settled, elapsed partial handling, physical dock position change, paused/saved twins, company-private logistics and replay/session isolation verified.

All three compile attempts and both runtimes are retained in [evidence](../../docs/migration/P3_DOCK_SPATIAL_ADMISSION_VERIFICATION.md). Initial solution compile13.40s failed with six fixture API errors; targeted repair8.37s clean. First runtime411/3/0 in2177ms had only wrong hidden-entity assumptions: geometry is visible while owned diagnostics are restricted. Driver changed to actual NotOwned diagnostics; targeted compile4.45s clean and same-case FINAL green. No production semantics changed to satisfy those fixture errors. No executable edit after FINAL or historical/family/full runtime. Original221/184checked/37open and every gate literal/checkbox preserved; additional179. Full selected-provider audit remains open.

## Metadata closure

Current closure:1531 frozen source anchors,13 quickstart anchors,802 selected executable inputs; zero hash mismatches/post-pass executable drift.2746 paths/966 Markdown files/8587 local links, zero broken links;18 valid master rows;221 original literal gate/checkbox anchors exact and unchanged (184checked/37open), additional179. Thin driver boundary scan empty. git diff --check exit0 (34 existing line-ending-warning output lines),2734 dirty paths/zero staged. Sole clock and all three protected G19 source hashes unchanged. Twelve executable inputs added to freeze: eight existing dependencies plus four genuinely new proof/driver/JSON files. Source anchors add seven existing dependencies plus six new files. Six existing executable inputs changed (three production); no executable edit after FINAL. Metadata is scoped integrity evidence, not gameplay/full acceptance.
