# P10-A15: spatial-contract connected resource networks — Design Document

Current framework2.712/save161/replay2; footprint-aware graph contracts remain those [implemented and qualified at M48/core2.709](../../docs/migration/P6_NETWORKED_MOD_DURABILITY_VERIFICATION.md#current-m48-connected-policy-replacement--2026-10-22), including original graph144/0/four replays. Graph runtime was not repeated in M49–M51. [Current recovery/binding follow-up and limits](../../docs/migration/P6_NETWORKED_MOD_DURABILITY_VERIFICATION.md#current-m51-independent-evacuation-replacement--2026-10-22). No full/profile/family run or original gate closure; prior results retain their historical identities.

## 0. Original problem

Before A15, shared topology used native tile identities/footprints. It now uses stable geometry-owned vertex IDs and one topology/stock/flow owner for native and independent spaces. There is no second point-network implementation.

## 1. Goals

1. Original topology/resource/flow owner becomes NetworkService with stable provider-owned vertex IDs; no native coordinate type in shared state, graph algorithm or codec.
2. INetworkGraphGeometry supplies actual occupied vertices/ownership, potential neighbours, entity contact, valid identities, placement identity, erase and layer versions. Geometry never owns pools, reservations, work or flow progress.
3. Native tile geometry is a stateless geometry-only implementation separated from shared service; original TileMap/layers remain sole spatial state.
4. Actual PointSpace can compose the same NetworkService using original sparse layers and explicit JSON graph adjacency/reach authoring, never a tile proxy or second pool implementation.
5. Original redistribution, capacity, resource transactions, solver/demand policies, elapsed flow, diagnostics, saves, replay and one clock remain authoritative.
6. Physical authored corridors/earned cut/rebuild affect actual connected stock/flow in native and point worlds through normal player commands.

## 2. Design

Standard/NetworkGraphGeometry.cs owns NetworkVertex (stable identity/company), NetworkGeometryVersion and INetworkGraphGeometry. Original NetworkService owns topology/stock/flow; native corridor geometry/routing stays in Map/TileLineGeometry.cs. NetState dictionaries/members use ordinal stable string identities; capacity multiplies actual member count. BuildTopology validates actual vertices/neighbours/owners and retains original split/join/transaction algorithms. ComponentOf(network,anchor,footprint) asks selected geometry for actual or projected definition contact at retained placement. Normal flow/diagnostics pass anchor.Def; replacement passes destination metadata. Shared core never approximates native footprints or creates a preview entity/pool.

NetworkService observes original actual spatial/provider state for graph callbacks, validates selected network bindings before Initialize, retains original pending topology and endpoint identities, and syncs stable vertex IDs through original codec. Codec6 replaces native coordinate payload; no compatibility reader. Shared InfrastructureModule composes one NetworkService from selected graph geometry, binds topology/simulation/diagnostics and shared immutable view. Optional native route feature remains actual tile geometry only. No implicit spatial provider.

Point graph rules come from explicit JSON space metadata, not defaults. Sparse layer presence supplies vertices; explicit step and reach supply adjacency/contact. Same generic topology owner and original resource endpoints/ports execute flow. Missing selected geometry is explicit composition refusal.

## 3. API surface

INetworkGraphGeometry: Space, ValidateNetwork, ReadVersion, CopyVertices, CopyNeighbours, ValidateIdentity, Touches(world,line,anchor,footprint,identity), VertexIdentity, CheckErase, Erase. INetworkTopology removal accepts SpatialPlacement, not TileCoord. NetworkPoolView exposes MemberCount, not Tiles. Native ITileLineRouting remains optional. Current core2.709/save161/replay2 retains network codec6; original native-to-generic migration was2.462/save154/replay1. No compatibility overload or new spatial/pool state.

## 4. Original migration file scope (historical)

| Action | File | Lines | Risk |
|---|---|---|---|
| New | Standard/NetworkGraphGeometry.cs | 70 | medium |
| Modify | Networks/NetworkFlow/NetworkDiagnostics and direct type references | 450 | high |
| Modify | Map/TileLineGeometry and InfrastructureModule | 240 | high |
| Modify | actual graph views/contracts and consuming source | scoped | high |
| Modify | PointSpace and explicit JSON graph metadata | 100 | high |
| New | point network JSON and one public-player physical network proof | 180 | high |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Missing/mismatched selected graph geometry | Content refusal before Initialize |
| Duplicate/invalid IDs, foreign companies or malformed edges | Explicit fault, no healthy fabricated graph |
| Changed geometry with occupied/promise-bearing pools | Original typed disposition refusal; no material loss |
| Dirty topology observation | Original topology-pending facts |
| Invalid saved IDs/graph/endpoint/flow | CorruptStateException |
| Provider mutation/throw | Original first fault; no prefix or save |
| Uninitialized topology | Actual composition hash only; no stable checkpoint |

## 6. One current path

Dev only; replace coordinate payload/aliases and delete old native topology paths. No second engine, compatibility facade, point pool implementation or legacy state.

## 7. Historical A15 verification

One NEW exact bounded public-player case: actual native/point JSON/master, physical point construction/material haul, timed connected transfer, earned split/rebuild, complete entity+pool stock accounting, saved twins, detached facts, replay and interleaved isolation. FINAL144/0/0 in8079ms; all seven builds/three same-case attempts preserved. Native starts with authored installed geometry and earns physical cut/rebuild; no instant startup construction claim. Claims/private/fault/pending/arbitrary-provider matrices remain broad open requirements, not inferred from this case. No historical/family/full runtime.

## 8. Risks

| Risk | Mitigation |
|---|---|
| Native implementation remains hidden in shared state | Remove all native graph/state/codec types, not merely wrap existing tile topology |
| Point network duplicates inventories | Original NetworkService and ResourceInventoryState only |
| Coordinate IDs become another world size cap | Opaque provider identity; actual geometry validates representation, no shared coordinate ceiling |
| Graph callback mutates layer or stock | Observe actual spatial/provider state and current owner admission |
| Previous native helper callers break | Migrate source in place, no old-name shim |

## 9. Out of scope

Oil/platform/Draw/Tycoon/G19; entire P10/P3/global/private/fault/stress closure. These remain requirements, not waived. Publication waits whole goal AND plan.

## 10. Sub-task execution order

Read current graph/redistribution/flow/codec; define spatial graph contract; extract native geometry; convert original topology members/adjacency/contact/codec; compose one shared owner and migrate all consumers; implement explicit point geometry; author physical native/point proof; solution build and same-case necessary repairs; preserve attempts and freeze stage/master evidence.


## Metadata closure

Post-pass freeze 2026-10-11 22:18:47 UTC: 1613 source anchors,13 quickstart anchors,869 selected executable entries. Scan2801 paths/984 Markdown files/8612 local links,zero broken links,18 well-shaped master rows. No source/quickstart/executable mismatch or thin-driver boundary matches; git diff --check exit0 (34 existing line-ending warning lines). All221 original gate texts/checkboxes/line anchors unchanged;184 checked/37 open; additional188. Thirty-eight frozen executable entries changed (ten existing production),four new executable files (one production),six new source files and one existing source anchor newly admitted. Protected clock/Tycoon hashes unchanged. Dirty2789/staged0. No executable edits after FINAL or commit/push. Integrity does not prove broad gameplay closure.
