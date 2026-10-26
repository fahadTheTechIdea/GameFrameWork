# P3-S24: observed spatial layer admission — Design Document

## 0. Problem Statement

World.ValidateSpatialLayers reads an external provider's selected layer metadata before Initialize without the normal observation boundary. EntityObservation.CaptureProvider observes component/system/persistent-service codecs, but not the already-authoritative ISpatialSpace.Sync. A layer getter may change the clock or provider state while returning otherwise valid metadata.

## 1. Goals

1. Observe selected registered layer admission through the original provider/clock guard.
2. Capture a spatial provider's existing Sync codec, not a second state model.
3. Reject missing/wrong providers and malformed layers before Initialize, with no native fallback.
4. Preserve the original callback error after mutation instead of replacing it in finally.
5. Prove healthy native/direct tile travel and independent point production remain physical and durable.

## 2. Design

EntityObservation's sole codec selector adds ISpatialSpace.Sync. World.ValidateSpatialLayers wraps its original metadata/ownership/visibility/registration checks in existing ReadProvider. Deliberately nonspatial sessions return with no selected layers and no invented map. Same World owns admission order: freeze services/commands/events, selected identity, layer admission, presence admission, existing provider validators, Initialize.

Existing negative UnavailableSpace instrumentation exposes valid metadata-only layer registration plus explicit null/missing/state/clock/throw modes. It never initializes, spawns, runs geometry or simulates gameplay. The existing pure factory accepts those authored modes; one new driver uses public inputs/views/save/time and asserts early refusals. Native sparse map and independent point state remain original owners.

## 3. API surface

No new public contract or state schema. Existing provider Sync is now observed. Behavior identity advances2.433.0; save154/replay1 and view registrations unchanged.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| Modify | ../../src/Beep.SimEngine/Kernel/EntityObservation.cs | 2 | medium |
| Modify | ../../src/Beep.SimEngine/Kernel/World.cs | 5 | medium |
| Modify | ../../tests/ExtensionProof/SpatialPresenceCompositionProof.cs | 30 | low |
| Modify | Existing pure factory/protocol/registration | 6 | low |
| New | ../../tests/Beep.SimEngine.Tests/SpatialAdmissionGuardTests.cs | 140 | medium |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Missing/wrong selected spatial identity | ContentException before Initialize |
| Null/missing registered layers | Original ContentException before Initialize |
| Silent codec or clock mutation | Original observation guard rejects before Initialize |
| Callback throws after mutation | Original callback exception preserved |
| Valid nonspatial session | No geometry or fallback allocation |

## 6. Backward compatibility

None; extend original codec observation and admission, no compatibility registry or duplicate validator.

## 7. Verification

ONE NEW exact case: Spatial admission: selected providers layers and callbacks refuse before initialization. FIRST/FINAL86/0/0 in1493ms; PID22640 Exited0/1701ms observed, untruncated/no observer error. Initial solution compile failed in4.06s because the new test omitted Maps using; targeted repair compile clean4.85s. Outer60000ms/termination5000ms/output30000 characters, tile waits96/point waits256 Updates. No old/family/full/Oil/Tycoon/platform runtime or executable edits after FINAL. [Evidence](../../docs/migration/P3_LAYER_ADMISSION_GUARDS_VERIFICATION.md).

## 8. Risks

Codec observations scale with actual populated spatial state, not declared world area; performance budgets remain open. A provider hiding authoritative state from Sync is still invalid and is not made a hostile-CLR sandbox by this guard. Other higher-level provider requirements remain separate.

## 9. Out of scope

Compound P3.004 closure without all higher-level geometry consumer evidence, full P1-P12, new geometry, platforms/Draw, protected clock/Tycoon and publication before entire goal AND plan.

## 10. Sub-task execution order

1. [x] Inspect actual spatial admission and codec observation owners.
2. [x] Extend original guard and single layer admission path.
3. [x] Add bounded public physical/negative proof.
4. [x] Observe build and exact runtime, preserve attempts.
5. [x] All221 original literal requirements/checkboxes validated unchanged;204 anchors shift seven lines,173checked/48open, additional152.1371 source anchors/13 quickstart headers/680 executable inputs frozen at2026-10-11 11:20:48 UTC match with zero post-FINAL drift. Local links/master columns/thin driver/protected owners verified; compound P3.004 and full publication condition remain unsatisfied.
