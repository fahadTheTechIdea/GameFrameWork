# P11-D10: Entity-specific live sensor visibility — Design Document

Current M100/native2.736/save161/replay2: target geometry uses existing
EntityObservation.ReadSpatial, removing the separate capture/finally interval. Invalid
captures cannot become healthy invisibility; complete target/space/clock and first callback
errors use the sole observation owner. Actual baseline729/60 becomes1001/0 in one new exact
case with70target contexts and full0-to17/2input/17checkpoint physical replay112C5F1A92225CBF.
Affected native tile sight80/0 after preserved79/1 old private-refusal oracle; no command
rule change. Three clean target builds; no full suite or original gate closure.
[Current evidence, self-review and handoff](../../docs/migration/P11_ENTITY_SENSOR_VISIBILITY_VERIFICATION.md#current-m100-one-target-spatial-read-and-original-provider-errors--2026-10-22).

Earlier scoped implementation verified,2026-10-07. Build2.234.0/save136/all existing codecs/views
unchanged. ONE clean build0 warnings/errors4.91s; ONE exact NEW bounded case80 checks0 failures/aborts
467ms first attempt.100 older build identity lines compile only; no broad gate/post-green source edit.
[Evidence](../../docs/migration/P11_ENTITY_SENSOR_VISIBILITY_VERIFICATION.md).

## 0. Problem Statement

The original prototype-only visibility boundary could not restrict actual foreign instances
by current geometry. The implemented IVisibilityEntityTarget extends the same selected
IVisibility/KnowledgeVisibility path; counts, inspector, map queries and snapshots share it.
M100 removes a duplicate target-capture interval and uses the original spatial observation
owner, so malformed captures and original callback errors follow the same contract as
entity projection. No visibility state is copied into hosts or a second subsystem.

## 1. Goals

1. JSON-authored entity sensor targets require actual foreign entity geometry inside current coverage.
2. One shared sensor rule base owns selection, validation, current reads and derived keys for layer
   and entity targets; no copied sensor loop, visibility state, scheduler, position or clock.
3. Current counts/lookups/inspect/spatial queries and snapshot appearance/removal obey the same policy.
4. Explicit own-entity and observer rules remain intact; seen enemy operations/resources stay private.
5. ONE new bounded native case proves physical enter/leave, saved continuation and controller controls.

## 2. Design

Extend existing `IVisibilityTarget` with optional `IVisibilityEntityTarget.AllowsEntity(World,PlayerId,
Entity)`. `KnowledgeVisibility.CanSee` applies all matching prototype and actual-entity restrictions
conjunctively, after existing world/alive/seat/own-entity gates. No knowledge-by-appearance grant.

Replace layer-only sensor rule implementation with abstract `SensorVisibilityDef<T> where T:Def`
in `Standard/SensorVisibility.cs`. SAME explicit sensor identities, active owned provider contract,
pure current reads, space binding, containment and keys. `LayerSensorVisibilityDef` specializes
actual layer space/sample access. Delete old layer-only source, not preserve an alternate path.

`EntitySensorVisibilityDef` specializes actual prototype presence space and pure capture of actual
live target `ISpatialPresence`. It requires valid spatial target metadata before composition and
uses the same sensor containment; no kernel tile geometry branch. Register JSON
`entity_sensor_visibility` alongside the existing sensor section. PlayerView and GameSnapshot
remain projection owners; no duplicate counts, IDs, inspector or visibility history.

## 3. API surface

Engine contract and authored rule section only. Existing immutable entity views/seat snapshots,
map queries and operation/work/move commands remain the public game interaction surface.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Delete | `src/Beep.SimEngine/Standard/LayerSensorVisibility.cs` | medium |
| New | `src/Beep.SimEngine/Standard/SensorVisibility.cs` | medium |
| New | `src/Beep.SimEngine/Standard/EntitySensorVisibility.cs` | medium |
| Modify | `src/Beep.SimEngine/Standard/KnowledgeVisibility.cs` and module declaration | medium |
| Modify | engine build identity and old test identity metadata only | low |
| New | bounded test and native master-selected JSON entity rule/startup assets | medium |
| Modify | tracker/AGENTS/README/help and focused evidence/inventory | low |

## 5. Error handling matrix

Missing/nonspatial/wrong-space prototypes, missing/incompatible sensors and duplicate rule IDs fail
explicitly. Foreign/unseated/missing/dead targets refuse. Capture/provider mutation faults through
existing observation guard; no fallback entity sight or synthetic last-seen entity.

## 6. Backward compatibility

Development only, direct replacement of layer-only implementation. No legacy/parallel reader.

## 7. Verification

ONE exact NEW bounded native case, build as needed and repair/rerun only observed errors. Old identity
assertions compile only. No old/family/full/platform/stale gates. Freeze source/tests/JSON after final
green, then update evidence and scoped inventory. Full P1–P12 goal remains active.

## 8. Risks

Dynamic coverage enumeration is not constant CPU/allocation proof. Snapshot known IDs are presentation
delta history, not engine exploration state. Scoped provider checks are not a global mod sandbox.
Visibility hides observation, not physical route blockers or authorization of arbitrary commands.

## 9. Out of scope

Persistent last-seen/exploration/sharing/expiry, occlusion/weather, non-tile provider acceptance,
arbitrary command target/fault/privacy/performance/replay/full gates, Oil/hosts/demos/Tycoon/platforms.

## 10. Sub-task execution order

1. [x] D10.A: one generic sensor owner and actual-entity visibility contract; layer-only source deleted.
2. [x] D10.B: actual native entity rule/master/ordinary physical enter/leave/controls/snapshot case.
3. [x] D10.C: ONE clean build and ONE exact NEW case, first runtime attempt green.
4. [x] D10.D: source freeze; tracker/docs/evidence/inventory and broader open obligations.
