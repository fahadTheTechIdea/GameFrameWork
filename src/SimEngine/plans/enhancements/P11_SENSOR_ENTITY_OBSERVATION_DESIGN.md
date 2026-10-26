# P11-SO1: Complete sensor-entity observation

Current M100/native2.736/save161/replay2: the original target rule now shares
EntityObservation.ReadSpatial, complementing the already guarded sensor-owner callbacks.
Existing proof owner adds transient target-capture instrumentation; shared physical body
verifies full replay and70target/20sensor contexts. Target literal1001/0 on build2; native
tile literal80/0 on build3, whose only further changes are that older test's exact private
refusal oracle/runner DLL. Native/proof/target inputs match; target literal is not rerun.
[Current target boundary, every attempt and limitations](../../docs/migration/P11_ENTITY_SENSOR_VISIBILITY_VERIFICATION.md#current-m100-one-target-spatial-read-and-original-provider-errors--2026-10-22).

Earlier implementation focused verified on2.636.0/save159/replay1. Four exact final cases376/0.
[Every build/attempt, fixture correction, scope and identities](../../docs/migration/P11_SENSOR_ENTITY_OBSERVATION_VERIFICATION.md).
Original221/208 recorded checked/13 open; full plan is not complete.

## 0. Problem and specification

The original2.635 SensorVisibilityDef<T>.Current and Covers guarded only the sensor
component codec via CaptureProvider. The outer visibility guard owns the queried target (which can be a
different entity), not the sensor's sibling components. A mod sensor can change persisted
sibling state without touching the entity revision and still return apparently pure sight.
SO1 corrected those sensor-owner reads using the existing whole-entity observation.
M100 completes the selected target-capture boundary through the same original spatial read;
the engine must reject violations before publishing coverage, keys, sight or samples.

## 1. Goals

1. Sensor metadata, coverage and live admission read through the existing full entity/space/
   clock observation boundary; no new observer, codec, scheduler or state owner.
2. Detect private persisted sibling changes in coverage and live sight, including callbacks
   that throw. Preserve the original callback error as the first fault.
3. Keep actual non-tile physical supply, visibility, saved continuation and session isolation.
4. Leave broad original P11.026/.029/.035 open; this is a specific provider-purity proof.

## 2. Design

In `Standard/SensorVisibility.cs`, replace the two manual `try/finally` intervals with
`EntityObservation.ReadProvider(..., space: world.Space, observeTargetState: true)`.
For coverage, pass the actual enumerated sensor entity and read runtime/definition metadata
inside that interval. For live sight, infer the actual component owner from the provider.
Use the original entity codec, selected space codec and clock facts; do not cache sight.

Validated capability and actual owner identities are detached within the same interval and
reused for observation keys, without invoking provider identity getters after verification.

The independently compiled `PointSensor` receives transient negative instrumentation only.
Reuse existing `CompletionProbe` as its authored sibling rather than creating another saved
counter or resource owner. A dedicated proof module declares that existing component type.
The driver supplies the sensor using ordinary routes and reads engine visibility; it never
implements geometry, logistics, timing or state transitions.

## 3. API surface

No production contract changes. Engine identity advances to 2.636.0 because observation
admission changes. Save159/replay1 and all component layouts remain unchanged.

## 4. Files

- Modify `Standard/SensorVisibility.cs`, `Kernel/SaveGame.cs` and protocol expectation.
- Extend independent PointSensor's diagnostic hooks; add one proof module using the existing
  `CompletionProbeDef`/`CompletionProbe` and one exact registered driver case.
- Update tracker and write bounded verification evidence.

## 5. Errors

Private sibling mutation or clock mutation faults the session before visibility facts escape.
Throwing providers preserve their original exception, including mutate-then-throw. Faulted
sessions refuse further queries and saves. No rollback, fabricated denial or fallback sensor.

## 6. Compatibility

No compatibility path or legacy API. No persisted layout changes.

## 7. Verification

First run only the new exact negative/physical case on the current core to verify the gap.
After implementation, run that exact case and the existing non-tile remote utility sensor
and native occlusion exact cases. The new physical proof authors interaction reach1: a
mobile sensor occupies its point, so a hauler must stop beside it rather than overlap it.
The older point-sensor fixture also needs explicit resource/work docks and a non-crossing
source/carrier layout on its one-dimensional line. Correct that test authoring and run its
original exact physical case too, retaining original material counts, sensor range, work
duration and assertions. No full, profile or broad family runs; retain all attempts and bounds.

## 8. Risks

Full sensor entity codecs cost more than component-only codecs; correctness is mandatory.
Keep per-callback scopes local and preserve existing selected sensor filtering/order.
Do not claim a sandbox for arbitrary hidden CLR state outside declared codecs.

## 9. Out of scope

Oil/business source, platform integration, Draw, spatial representation changes, new clocks,
positive sensor caches, broad gates, Git staging/commit/push.

## 10. Execution order

1. Add independent negative probe and physical exact case; observe current-core result.
2. Replace manual sensor intervals with the existing complete observation boundary.
3. Build affected test project; run only the four exact cases described above.
4. Record evidence and update master tracker without narrowing the original requirements.
