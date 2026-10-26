# P3-S23: wide geometry acceptance — Design Document

## 0. Problem Statement

Original P3.acceptance.006 requires large-duration/coordinate success or explicit checked failure. Native sparse maps and exact game durations already remove old platform-sized limits, but current acceptance is incomplete. TileMapView.CopyEntities returns void and invokes rectangle endpoints without a wide bounds preflight, unlike existing layer pages.

## 1. Goals

1. Give existing entity-region queries a typed refusal for malformed/out-of-world geometry, without overflow or partial publication.
2. Exercise native/direct authored sparse spaces at100000 and int.MaxValue dimensions using small real allocations.
3. Move a real worker near large positive coordinates through ordinary commands and elapsed game time; preserve partial saves/pause/replay.
4. Prove large exact duration conversion and explicit coordinate/arithmetic exhaustion without inventing a second clock.
5. Record actual focused allocation/time evidence; never claim infinite memory or arbitrary providers from native cases.

## 2. Design

Existing CopyEntities returns Code, clears output and checks signed bounds using long before touching checked rectangle endpoints. Empty in-bounds queries succeed with no entries. Existing observation owns whole-target validation before publication. No new spatial store, coordinate type, scheduler, clock or platform cap. Tile view registration advances5 to6 for its changed return contract; state codecs/save154 remain unchanged.

One bounded public test authors selected native/direct world dimensions and translated startup coordinates through existing master-listed files. It queries a tiny page inside a region whose logical cell count exceeds int, checks the same sparse layers/entities and physically moves a held worker. Numeric assertions call existing public duration/vector operations; tests own no routing, timing, visibility or movement algorithm.

## 3. API surface

TileMapView.CopyEntities(TileRect, IList<EntityView>) returns Code.None or existing Codes.OutOfBounds. Null output throws ArgumentNullException; faulted-world cutoff precedes any healthy query. Other signatures remain unchanged.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| Modify | ../../src/Beep.SimEngine/Map/TileMapView.cs | 12 | medium |
| Modify | ../../src/Beep.SimEngine/Content/SpatialSpaceDef.cs | 1 | low |
| Modify | Protocol and test registration | 3 | low |
| New | ../../tests/Beep.SimEngine.Tests/WideGeometryAcceptanceTests.cs | 150 | medium |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Negative/excess rectangle bounds | OutOfBounds, cleared output, unchanged state |
| Empty in-bounds query | None, no entities |
| Large valid sparse extent | Small pages/actual nearby entities, no full-world buffer |
| Unrepresentable distance/area/vector/duration | Original checked OverflowException |
| Hidden sample/entity | Original private visibility refusal |

## 6. Backward compatibility

None. One original query receives the explicit result, not a second query or shim.

## 7. Verification

ONE NEW exact case: Wide geometry: sparse worlds physical motion pages and checked durations. FIRST/FINAL293/0/0 in942ms; PID29460 Exited0/1096ms observed, untruncated/no observer error. Initial solution compile failed after2.50s on incorrect Codes namespace qualification. The existing root Codes reference repaired it; targeted test-project compile clean7.32s. Outer60000ms/termination5000ms/output30000 characters, waits256 Updates. Observed108526304 current-thread allocated bytes across all eight fixtures and replay, not general allocation-budget acceptance. No historical/family/full/Oil/Tycoon/platform runtime or executable edits after FINAL. [Evidence](../../docs/migration/P3_WIDE_GEOMETRY_ACCEPTANCE_VERIFICATION.md).

## 8. Risks

Native coordinates remain representable finite integers/fixed point, not platform world-size policy. Sparse support avoids dense allocations but real populated maps consume memory. Provider-specific numerical policies and broad allocation budgets are not inferred from this case.

## 9. Out of scope

Oil minute-session acceptance, full private cross-provider spatial gate, renderers/platforms, protected clock/Tycoon, full regression and publication before entire goal AND plan.

## 10. Sub-task execution order

1. [x] Inspect original bounds, pages, sparse storage and exact time conversion.
2. [x] Update original entity-query preflight and versioned contract.
3. [x] Author one public wide-world physical/numeric proof.
4. [x] Observe solution compile and exact runtime.
5. [x] Only original P3.006 checked against its unchanged literal requirement. All221 literal requirements validated;204 anchors shift seven lines,173checked/48open, additional151.1368 source anchors/13 quickstart headers/679 selected executable inputs frozen at2026-10-11 11:11:56 UTC match with zero post-FINAL drift. Links/master columns/thin driver/protected owners verified; publication condition remains unsatisfied.
