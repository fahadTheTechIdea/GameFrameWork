# P11-D9: Live sensor coverage — Design Document

Status: scoped implementation verified,2026-10-07. Build2.233.0/save136; tile_sensor codec1,
all existing codecs/views unchanged. TWO clean builds6.20s/3.13s; TWO SAME-case runs, first53 passed/
one failed assertion/one aborted suite1194ms, corrected player hold/actual Arrived assertions;
final75 passed0 failures/aborts465ms.99 older build identity assertions compile only.
[Evidence](../../docs/migration/P11_LIVE_SENSOR_COVERAGE_VERIFICATION.md).

## 0. Problem Statement

`Standard/KnowledgeVisibility.cs` already selects one visibility policy and admits samples through
`IVisibilitySampleTarget`. Remembered spatial readings are owned by `CompanyKnowledgeService`, but
no entity capability supplies live coverage as its actual physical position changes.

## 1. Goals

1. JSON-authored machine sensors expose live coverage through contracts, not a second map or ledger.
2. The existing selected visibility policy applies ownership, content, and coverage conjunctively.
3. Movement, operation disable/range changes and signal gates affect current sight; historical
   observations remain earned only through physical work.
4. Existing end-Update/restore scope refresh invalidates company-private pages when coverage changes.
5. One new bounded native-only case proves physical travel, controls, privacy and saved continuation.

## 2. Design

Add `ISpatialSensorDefinition`, `ISpatialSensor` and immutable abstract `SensorCoverage` in
`Standard/SpatialSensors.cs`. Providers return null explicitly when inactive. Geometry is provider
owned; the policy knows only space, opaque scope identity and `Covers(SpatialSnapshot)`.
`TileSensorDef`/`TileSensor` reuse `ControlledExecutionDef`/`ControlledExecution` for authored controls,
signal gating, entity state and the sole component Sync. Range is an explicit nonnegative integral
tile distance; native coverage contains whole sample footprints in a square expanded around the
actual source footprint. An explicit zero range is inactive, matching the shared controlled-execution
state/operation semantics. Long arithmetic avoids inflated-int-rectangle overflow. No copied position.

Add `LayerSensorVisibilityDef` implementing the existing sample target. JSON selects an actual layer
and explicit unique named sensor capabilities. Any active owned compatible sensor may cover a sample;
separate matching visibility rules remain conjunctive. Reads guard actual provider state and world
identity through `EntityObservation`. Derived keys include actual source/capability/coverage identity,
sorted independently of iteration order. No clock, scheduler, fog cache or saved policy state.

`KnowledgeVisibilityModule` declares these two content types. Normal controls still belong solely to
optional `OperationsModule`; no sensor-specific command or second control service. Existing map
sample access, page preflight, Set/Fill privacy and transient scope refresh remain authoritative.

## 3. API surface

Engine contracts and JSON component/section only. Hosts continue using existing immutable layer views,
`ConfigureOperationCommand`, work commands and `Game.Update`. No renderer or platform API.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| New | `src/Beep.SimEngine/Standard/SpatialSensors.cs` | medium |
| New | `src/Beep.SimEngine/Standard/LayerSensorVisibility.cs` | medium |
| Modify | `src/Beep.SimEngine/Standard/KnowledgeVisibilityModule.cs` | medium |
| Modify | `src/Beep.SimEngine/Kernel/SaveGame.cs` | low |
| New | `tests/Beep.SimEngine.Tests/LiveSensorTests.cs` | medium |
| New | `tests/ExtensionProof/content/native-sensor-visibility.json` | low |
| Modify | exact existing test registration/embedded-resource/identity metadata | low |
| Modify | tracker/README/AGENTS and this stage evidence | low |

## 5. Error handling matrix

Missing/duplicate sensor IDs, wrong space, absent actual prototype sensor, invalid range/unit and
invalid target shape refuse explicitly. Foreign/unseated viewers cannot borrow coverage. Disabled
or signal-blocked sensors return no coverage. Pure provider violations fault, never grant fallback sight.

## 6. Backward compatibility

Development only; direct current contracts. No old reader, shim or default sensor.

## 7. Verification

One build and ONE distinct exact new bounded runtime case; repair/rerun only an observed failure.
Older identity expectations migrate/compile only. Freeze source/JSON/tests after final green; record
all invocations. Focused evidence does not establish global provider purity, full regression or P1–P12.

## 8. Risks

The policy enumerates actual entity components and does not prove constant CPU/allocation bounds.
Observation guards are scoped provider/identity checks, not a global arbitrary-code sandbox.
Tile distance is this native provider's geometry, not a kernel geometry or engine/world size cap.

## 9. Out of scope

Persistent exploration/forgetting/sharing, automatic survey capture, line-of-sight occlusion, weather
attenuation, non-tile sensor provider acceptance, universal performance/fault/replay/soak/full gates,
OilFieldDays/hosts/demos/Tycoon/Draw/platforms. Full goal remains active; publication waits for full finish.

## 10. Sub-task execution order

1. [x] D9.A: coverage/provider and selected-policy content without another state owner.
2. [x] D9.B: native master-selected visibility fixture and physical moving-worker coverage/controls.
3. [x] D9.C: build and ONE distinct exact new case; observed fixture assumptions repaired only.
4. [x] D9.D: source frozen after green; tracker/evidence/scoped inventory and broader open gates.
