# P11-RE1 — Authored contact and whole-region sensor exploration

Status: implemented, scoped verification complete. Original P11.026/full goal open.
[Every attempt, fixture correction, identities and limits](../../docs/migration/P11_REGION_EXPLORATION_REQUIREMENTS_VERIFICATION.md).

## 0. Problem Statement

The sole reusable exploration runtime hardcodes presence-inside-region before
checking its named sensor. Remote telemetry exists, but a sensor outside a region
cannot earn discovery even when it sees the entire region. This leaves the remote/
cell-wide exploration part of original P11.026 incomplete.

## 1. Goals

1. Each authored entry references a stable JSON-authored requirement contract.
2. Contact and whole-region sensor requirements use the same existing progress,
   interruption, state, knowledge recording, save and update owner.
3. Region providers expose complete detached observation footprints through an
   optional spatial contract; no center-point or bounding-box guess for arbitrary
   shapes, and no native tile lookup in the exploration scheduler.
4. A non-tile extension proves timed remote discovery, incomplete coverage refusal,
   shutdown interruption, saved twins, player privacy, sharing/forgetting and replay.
5. Existing contact exploration keeps physical entry/dwell semantics after explicit
   content migration. No old schema/default requirement/compatibility path remains.

## 2. Design

Rename the existing reusable ContactExploration owner/declarations/contracts to
AuthoredExploration. This is a replacement, not a second component/system/ledger.
The content keys become `authored_exploration` and `exploration_profiles`.

Add `IExplorationRequirementDefinition` with pure target validation and blocker
evaluation. Each `AuthoredExplorationEntry` requires `requirement` and resolves the
actual frozen alias. Native contact and sensor-region requirement definitions are
JSON-listed content; the scheduler only calls the selected contract. Existing
subject observation/recording and reset/retain rules stay in the original owner.

Add optional `ISpatialObservationRegion` exposing complete detached footprint
snapshots. Rectangular tiles return their full rectangle; painted tiles return all
authored cells, never a guessed envelope. The independent point interval returns
one full interval snapshot, and its existing sensor recognizes that whole shape.
Unsupported geometry refuses sensor-region authoring, rather than pretending a
center sample proves coverage. All footprints must be nonempty, nonnull, same-space
and contained by their original region; every footprint must pass actual current
sensor visibility/occlusion/gates for the whole dwell interval.

Original observation guards cover requirement, region, actual source and selected
space. Required metadata/reference validation applies to unused loaded profiles
before Initialize. Immutable progress facts expose the selected requirement ID.
Build identity advances2.632→2.633; no saved progress layout/clock changes.

## 3. API surface

Required entry `requirement`; required content definitions under
`contact_exploration_requirements` and `sensor_region_exploration_requirements`.
The old reusable component/profile names are removed, not aliased. Existing
`IExplorationSource`, engine-issued update context and public ExplorationView remain.

Example within a master-listed content document (referenced sensor/state/region/
subject IDs must already resolve to actual authored definitions):

```json
{
  "sensor_region_exploration_requirements": [{ "id": "field.remote-coverage" }],
  "exploration_profiles": [{
    "id": "field.scan", "states": { "sampling": "field.scanning" },
    "entries": [{
      "id": "north", "region": "field.north", "requirement": "field.remote-coverage",
      "subject": "field.north-known", "duration": { "numerator": 12, "denominator": 1, "unit": "minute" },
      "interruption": "retain"
    }]
  }]
}
```

The entity component binds `authored_exploration` with explicit `id`, `sensor` and
`profile` keys. A contact workflow instead references its authored definition under
`contact_exploration_requirements`. New requirement providers implement the public
validation/evaluation contract; they do not change the scheduler or own progress.

## 4. Files to change

- Original `Standard/Exploration.cs`/`ExplorationModule.cs`: generic named requirement
  dispatch and rename, original ownership unchanged.
- `Kernel/SpatialRegions.cs`/`Map/TileRegions.cs`: optional complete footprint contract.
- Existing point region/sensor proof: complete interval shape and sensor support.
- Existing contact profile JSON/callers: mechanical explicit schema migration.
- New focused public-player remote-region case, registration, protocol identity.
- Stage evidence, master tracker and applicable instructions.

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Missing/unknown requirement | Content refusal before Initialize |
| Sensor requirement with unsupported region | Content refusal, no fallback |
| Contact source outside region | Existing Outside blocker |
| Incomplete/disabled/obstructed current sensor | Existing NoSensor blocker; authored reset/retain |
| Valid whole-region dwell | Original ledger record once, progress cleared |
| Invalid/throwing provider | Existing first-fault observation boundary, no invented discovery |

## 6. Backward compatibility

Development-only. Migrate current authored files and CLR callers directly; no old
content reader, default requirement or parallel legacy exploration runtime.

## 7. Verification

One targeted framework test build after implementation; exact new remote-region
case and exact existing contact/binding case selected after source inspection.
No full/family/profile test. Record failed attempts/fixture corrections as well as
final results. P11.026/full goal do not close on this single sub-requirement.

## 8. Risks

Endpoint samples do not prove an arbitrary interval: the point extension must
publish a complete interval shape understood by its actual sensor. Painted tiles
must preserve disconnected shape and occlusion, not use a rectangle around holes.
No renderer/platform bounds, new resource ledger, implicit geology knowledge or
seismic/logging prerequisite is introduced.

## 9. Out of scope

Changes to Oil/business/platforms/Draw, all-provider/global purity/full P11 matrices,
second exploration/knowledge schedulers, new work/cost/resource owners and publishing.

## 10. Sub-task execution order

1. Replace contact-specific reusable naming; migrate explicit profile entries.
2. Add requirement and complete-region contracts using existing owner/observers.
3. Author non-tile remote-region proof and run only selected focused cases.
4. Record exact evidence and retain original broad gates open.

Completed: sole reusable runtime renamed/reconfigured, actual requirement dispatch,
complete-region contracts and native/non-tile shape support; all existing source/JSON
callers migrated without aliases. Four remote physical variants plus selected
native/independent contact cases pass549 checks. Native geometry probes are not full
tile remote/occlusion acceptance; arbitrary provider/stress/global gates stay open.
