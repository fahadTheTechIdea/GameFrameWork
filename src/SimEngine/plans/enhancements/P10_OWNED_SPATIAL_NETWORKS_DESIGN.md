# P10-A17: owned non-tile corridor networks — Design Document

## 0. Problem Statement

Existing PointSpace rejects Int32 layers and every owned line, while shared NetworkService already partitions actual owned vertices and withholds private pools/flow. Framework extension proof therefore still cannot use company-owned non-tile networks without revising the spatial provider.

## 1. Goals

1. Extend original PointLayer store to exact signed Int32 samples as well as Fixed; no second ownership map or codec.
2. Original point geometry stamps/checks/clears declared owner layer with depth through one spatial write owner, never overwrites another company.
3. Original graph geometry publishes actual vertex company; original NetworkService alone retains private pools/stock/flow/claims.
4. JSON explicitly declares ownership layer/line/private network; workers physically supply/build/cut/rebuild over sole time.
5. Normal public engine views/refusals, save/replay and interleaved sessions prove selected privacy without host rules.

## 2. Design

PointLayer.CheckValue/Read/Sync validates exact Int32 representation with existing material legality. PointSpace.RegisterLayer accepts declared Fixed/Int32. Original WriteLayer delegates a single spatial WriteLayers preflight/commit with local tuples; version admission uses complete changed write set and actual private/public observers. Original layer state/resource guards/Touch remain sole owners. No persisted batch or new state map.

PointLineGeometry validates actual integer owner-layer binding; CheckSegment/CheckErase validate company encoding and ownership; Stamp/Erase commit depth+owner writes together. PointNetworkGeometry derives company from actual owner samples and reports original invalidation, never pool state. Shared coordinators are unchanged. Extend actual point proof factory authoring only and master-list an owned variant JSON document.

## 3. API surface

External spatial provider WriteLayers (engine-side primitive only), exact Int32 PointLayer and actual point owned geometry. No new shared scheduler/topology/stock/clock/codec. Core2.464.0/save154/replay1/network6/view7/cursor3/system1; point existing codec5 unchanged because samples already sync exact Fix.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---|---|
| Modify | external PointLayers/PointLineGeometry/PointNetworkGeometry | 150 | high |
| Modify | existing point graph factory authoring hook | 3 | low |
| New | owned point network JSON and thin physical/private case | 180 | high |
| Modify | current protocol/build/tracker | scoped | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Fractional/out-of-range integer value | Typed refusal/corrupt saved state, no rounding |
| Foreign segment ownership | Original ForeignLine, no write/stock grant |
| Invalid/missing owner layer | Content refusal before Initialize |
| Failed full write admission | No earlier layer success |
| Owned pool/flow viewed by foreign company | Existing private withholding |
| Cut with actual stock/claims | Original disposition/refusal, never point-specific balance algorithm |

## 6. Backward compatibility

Dev only; extend existing contracts/storage/writer in place, delete single-write duplicate body. No compatibility overload, parallel map, point pool or owner fallback.

## 7. Verification

ONE NEW exact bounded public-player case: actual JSON/master, two minute/half physical owned point construction/flow/partial saves, foreign removal/refused command/privacy, earned cut/rebuild/stock conservation, ownership layer samples, replay/isolation and malformed ownership metadata. Pure engine-side fractional/duplicate write-set refusal leaves no earlier depth/owner prefix. FIRST/FINAL130/0/0 in2256ms; one clean solution13.49s/0warnings/errors. No repairs/reruns, historical/family/full runtime or executable edits after FINAL. Broad matrices remain open.

## 8. Risks

| Risk | Mitigation |
|---|---|
| Ownership gets another state store | Original PointLayer with existing sparse samples/Sync only |
| Point ownership bypasses shared pools | Actual company vertices; original NetworkService unchanged |
| Layer pair yields successful partial admission | One full spatial write preflight before commit |
| Narrow privacy proof becomes global closure | Arbitrary fog/private CLR/claims/fault/performance matrices stay open |

## 9. Out of scope

All arbitrary geometry/fog/private CLR/claims/fault/pressure/utility/global/stress/full matrices, Oil/platform/Draw/Tycoon/G19 and publication. Broad original gates remain open.

## 10. Sub-task execution order

Read actual layers/writer/company graph; create stage/tracker; extend exact integer store and single write-set owner; bind owned point geometry/graph; author physical private network proof; compile/verify exact case; preserve all attempts and freeze metadata without broad closure.

Metadata closure: 1,621 source anchors, 873 selected executable inputs and 13 quickstart anchors match their frozen SHA-256 values. All 221 original gate literals/checkboxes are unchanged (184 checked, 37 open); 190 additional scoped deliverables do not imply aggregate completion. All 8,616 local links resolve, 18 master rows have the expected shape, the new driver boundary scan is clean, and git diff --check exits 0. Nothing is staged; no executable changes occurred after the focused pass.
