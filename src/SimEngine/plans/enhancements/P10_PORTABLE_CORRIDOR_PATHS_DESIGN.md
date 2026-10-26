# P10-A12: provider-owned corridor paths and cursors — Design Document

## 0. Problem Statement

Standard/InfrastructurePlans.cs decodes every placement to TilePlacement and formats identity with tile coordinates. InfrastructureWork.cs exposes tile paths and tile routing in the shared construction contract; Networks.cs persists a List<TileCoord> in the sole cursor. Those assumptions prevent another spatial provider from using the original corridor DAG.

## 1. Goals

1. Shared intent uses an explicit JSON path decoded by the selected ISpatialPlacementIntent, with provider-owned inverse/identity and no tile cast.
2. The original LineWork owns a frozen JSON path and one order binding; its original LinePlan still derives progress from WorkBoard nodes.
3. Shared ILineConstruction consumes SpatialPlacement; optional ITileLineRouting alone exposes tile routing.
4. Generic marker admission requires spatial and inventory contracts; native footprint rules remain in the native provider.
5. Migrate existing callers/content in place without old tiles arguments, old cursor payload, fallback provider, or compatibility codec.

## 2. Design

Extend the existing LineDeclaration path reader/identity authoring rather than introducing another path model. ISpatialPlacementIntent.ReadPlacement/WritePlacement/PlacementIdentity remain the only geometry codec. ILineWork.Path is frozen JSON; BindPath writes canonical provider placements. Cursor schema3 stores that canonical JSON string and original line/order, not a duplicate tile cache or progress. LinePlan uses segment count only. Native LineService converts at its explicit tile boundary and retains original topology/material/work effects. Native LayLineCommand keeps its optional tile command payload and formats placement JSON through TilePlacement.

## 3. API surface

ILineDefinition.CreateIntent(JsonValue path, string group, int priority, bool paused). ILineWork.Path: JsonValue; BindPath(World, ILineDefinition, IReadOnlyList<SpatialPlacement>). ILineConstruction.CheckIntent/Check/Begin accept IReadOnlyList<SpatialPlacement>. ITileLineRouting owns PlanPath. JSON line arguments are {space,path}; old tiles is invalid. Core2.459.0/save154/replay1, original line_work and proof_line_work codec3. The global save envelope is unchanged.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---|---|
| Modify | Standard/InfrastructurePlans.cs, InfrastructureWork.cs, LineDefinitions.cs | 120 | medium |
| Modify | Standard/Networks.cs, InfrastructureModule.cs, Views/InfrastructureView.cs | 100 | medium |
| Modify | Map/TilePresence.cs and actual line authoring/proof callers | 50 | medium |
| New | scoped public-player path proof and engine-side spatial observation proof | bounded | medium |
| Modify | protocol, registration, master tracker and evidence | scoped | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Missing/empty/wrong-space path or unknown/extra placement field | Original strict content/refusal path, no allocation |
| Repeated/noncontiguous native tile placements | Native provider refusal |
| Invalid restored canonical path/order/cursor prefix | CorruptStateException before checkpoint |
| Geometry observation mutates clock/map/entity | Original terminal observation fault, preserve first exception |

## 6. Backward compatibility

Development-only; no legacy tiles intent, old tile cursor payload or compatibility codec.

## 7. Verification

One new exact bounded case covers public native/direct cursor execution, authored JSON path/template identity, physical machines/material conservation, partial save/load/replay and detached private views. Engine-side point provider probes verify shared decoding and provider-owned canonical identity without tile assumptions; they are not a claim of non-tile physical construction. One solution compile and only necessary same-case repairs. Historical/family/full runtime remains unexecuted.

## 8. Risks

| Risk | Mitigation |
|---|---|
| Geometry/cache becomes another authoritative model | Persist only frozen provider JSON in original cursor |
| Native rules leak back into shared metadata | Native module checks tile footprint; shared marker checks spatial contract |
| Intent portability mistaken for complete non-tile infrastructure | Keep native execution/topology/module extraction open for the next stage |

## 9. Out of scope

Non-tile physical construction/topology and generic infrastructure module extraction are required follow-on work, not dropped requirements. Full P10.003/P3.004/provider/global/stress acceptance, Oil/platform/Draw/Tycoon/G19 and publishing remain open. No second clock, scheduler, stock, graph or state owner.

## 10. Sub-task execution order

Inspect actual contracts; replace shared path/cursor APIs and native boundaries in place; migrate consuming authoring; add one scoped case; compile once and repair only necessary failures; record exact evidence and freeze tracker metadata. No broad gate closure from this path-only stage.

Status: scoped verified2.459.0/save154/replay1; [evidence](../../docs/migration/P10_PORTABLE_CORRIDOR_PATHS_VERIFICATION.md). One exact FIRST/FINAL182/0/0 in4877ms, PID7348 Exited0/process5077ms. Initial solution failed only two new driver numeric comparisons; necessary focused repair clean5.78s. Four native/direct minute/half physical cursor flows and selected point-provider path observations; no executable edits after FINAL or historical/family/full runtime. Non-tile physical corridor/module extraction and full goal/plan remain open; no commit/push.

## Metadata closure

Current metadata closure:1591 source anchors,13 quickstart anchors,854 selected executable inputs; zero hash mismatches/post-pass executable drift.2783 paths/978 Markdown files/8606 local links, zero broken links;18 valid master rows. All221 original literal gate/checkbox anchors and JSON records exact and unchanged (184checked/37open), additional185. Thin driver boundary scan empty; shared LineDeclaration/ILineDefinition contain no TileCoord/TilePlacement/ITilePresence or tile cursor state. Remaining tile counts are native network pool geometry, not a compatibility cursor. git diff --check exit0 (34 existing line-ending-warning output lines),2771 dirty paths/zero staged. Sole clock/protected G19 hashes unchanged. Eighteen previously frozen executable inputs changed (eight existing production sources); three new external-proof/JSON/driver executables and two new stage/evidence docs, five new source anchors. Consuming dependencies already frozen; no executable edits after FINAL. Metadata integrity is not non-tile physical construction or broad gameplay/provider acceptance.
