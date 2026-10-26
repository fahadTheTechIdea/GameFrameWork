# P10-A13: shared physical corridor execution — Design Document

## 0. Problem Statement

Networks.cs still owns admission, cursor creation, order binding, segment progress effects and recovery alongside native tile topology. InfrastructureModule.cs requires tile navigation and instantiates that native owner unconditionally. LineDefinitions.cs restricts every corridor layer/value to bytes. Portable JSON paths alone therefore cannot construct a corridor in another space.

## 1. Goals

1. One shared LineConstructionService owns existing corridor admission, actual marker/order binding, earned effects, cancellation cleanup and checkpoint validation for every selected geometry.
2. Selected spatial authorities compose one ILineGeometry provider; geometry owns bounds, continuity, current layer/ownership, stamping and cursor location/movement only.
3. InfrastructureModule requires work/inventory, never a tile navigation dependency. Native tile provider alone composes native route/topology/flow and requires its navigation.
4. Line values use deterministic Fix; layer/provider validates representation. Native byte topology remains byte with exact range/integrality validation.
5. Original JSON cursor/compiler, WorkBoard, physical machines, resource recovery and sole clock remain authoritative; no duplicated scheduler/progress/stock/legacy owner.

## 2. Design

Move generic methods out of LineService into Standard/LineConstructionService.cs, deleting old implementations. ILineGeometryComposition on the selected ISpatialSpace composes geometry; native TileMap composition binds the original LineService to geometry/routing/topology/flow. Independent PointSpace geometry delegates its original sparse PointLayer and PointPresence, never another world/layer state store. ILineSimulation contains native topology/flow only; original LineSystem dispatches shared cursor cleanup/checkpoint validation plus optional declared topology/flow. Geometry pure checks use original provider/entity observation; earned commits recheck current state. Shared marker moves only after original stock evacuation admission and earned segment completion.

## 3. API surface

ILineGeometry: Space, ValidateLine, CheckPath, CheckSegment(out alreadyLaid), IsCursorAt, MoveCursor, Stamp. ILineGeometryComposition.ComposeLineGeometry(WorldComposer). ILineConstruction adds Advance/ValidateMarkers; ILineSimulation retains RebuildDirty/Flow. ILineDefinition.Value becomes Fix and JSON value is required. Native LineService no longer implements construction/effects/activity-source. Infrastructure manifest dependencies migrate in place from work/inventory/tile-navigation to work/inventory. Core2.460.0/save154/replay1; cursor3, native topology4 and system1 codecs unchanged.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---|---|
| New | Standard/LineConstructionService.cs | 240 | high |
| New | Map/TileLineGeometry.cs (native composition entry only) | 30 | medium |
| Modify | InfrastructureWork/InfrastructureModule/Networks/LineDefinitions | 300 | high |
| Modify | actual manifest dependencies and explicit line values | scoped | medium |
| Modify | engine-side PointSpace provider and existing ownership assertions | scoped | medium |
| New | authored point corridor content and one public-player physical case | bounded | medium |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Selected space lacks declared corridor geometry | Content refusal before Initialize |
| Unsupported layer/value/cursor metadata, unused included | Content refusal before Initialize |
| Invalid path/current policy/foreign ownership | Original typed refusal or pending blocker, no allocation |
| Invalid restored marker/path/graph/prefix/location | CorruptStateException |
| Canceled stocked marker cannot evacuate | Original physical recovery blocker; no cargo teleport/destruction |
| Pure provider mutates state or throws | Original fault; no healthy completed prefix |

## 6. Backward compatibility

Development-only; delete old generic implementations, migrate signatures/content in place; no compatibility module, extra scheduler, duplicate path or old owner facade.

## 7. Verification

One NEW exact bounded case uses actual JSON/master composition and ordinary public player commands/views/Game.Update for native and independent point physical three-segment work, real materials/travel, pause/cancel, partial save/twins, private detached facts/replay/isolation. Five malformed unused line declarations refuse before Initialize; composition probes report actual distinct construction/topology owners. FINAL198/0/0 in5201ms. One solution compile and necessary focused repairs only; all five builds/three runtime attempts preserved in the evidence document. No historical/family/full runtime.

## 8. Risks

| Risk | Mitigation |
|---|---|
| A provider becomes another construction engine | Geometry-only callbacks; original shared owner alone creates/posts/completes/cleans |
| Native manifests retain hard dependency | Migrate all actual framework manifests and source-authored declarations |
| Sparse point samples silently become tile topology | Reuse selected PointLayer; no raster or invented connected flow |
| Generic work is mistaken for all network portability | Cross-provider network flow/removal/pressure/global matrices remain open |

## 9. Out of scope

Other native network topology/utility/pressure/removal portability, arbitrary geometry/private purity, complete P10.003/P3.004/full/global/stress gates, Oil/platform/Draw/Tycoon/G19 and publication. These are not waived requirements.

## 10. Sub-task execution order

Inspect owners; define selected geometry contracts; move generic execution in place; bind native geometry separately; migrate authored dependencies/value signatures; extend actual independent point geometry/layer; add one public-player proof; compile/verify focused case; preserve all attempts and freeze stage/master evidence. Whole goal and plan remain active.


## Metadata closure

Post-pass freeze 2026-10-11 21:30:51 UTC: 1602 source anchors,13 quickstart anchors,863 selected executable entries. Metadata scan2791 paths/980 Markdown files/8608 local links,zero broken links,18 master rows with correct shape. No source/quickstart/executable mismatch or driver boundary matches; git diff --check exit0 (34 existing line-ending warning lines). All221 original gate texts/checkboxes/line anchors unchanged,184 checked/37 open; additional186. Thirty-four frozen executable entries changed (seven existing production sources),six new executable files (two production),three existing consuming inputs admitted to the freeze; eight new source files and three existing source anchors. Protected clock/Tycoon hashes unchanged. Dirty2779/staged0; no executable edits after FINAL or commit/push. Integrity is not full gameplay verification.
