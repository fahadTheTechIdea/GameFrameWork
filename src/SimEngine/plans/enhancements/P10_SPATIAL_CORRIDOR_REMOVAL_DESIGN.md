# P10-A14: provider-neutral earned corridor removal — Design Document

## 0. Problem Statement

Standard/LineEffects.cs CheckIntent/RemovalTiles/CheckStart/CommitSubject still inspect ITilePresence and invoke INetworkTopology directly. A13 shared construction cannot remove a non-tile corridor through ordinary authored timed work. This retains the wrong game-framework boundary.

## 1. Goals

1. One shared corridor execution owner admits actual removal subjects and invokes earned removal through selected geometry, never native footprints.
2. Optional ILineRemovalGeometry derives actual subject placements and checks/commits whole removal without owning jobs, progress or supplies.
3. Native geometry delegates its original topology disposition; fixed-layer point geometry uses its original sparse layer. No new stock/topology store or instant player command.
4. Physical staffed JSON activity, current ownership, preflight, pause/cancel, saved continuation and replay remain with original WorkBoard/clock owners.
5. Remove obsolete shared tile-footprint logic; no compatibility path or duplicate removal coordinator.

## 2. Design

Add ILineRemovalGeometry to InfrastructureWork.cs with ReadRemovalPath(World,ILineDefinition,Entity,out IReadOnlyList<SpatialPlacement>), CheckRemoval(World,PlayerId,ILineDefinition,path), Remove(World,PlayerId,ILineDefinition,path). Add ILineRemoval with CheckIntent/Check/Remove using actual company/line/subject. Original LineConstructionService implements this narrow execution contract using its selected geometry, actual catalog aliases/target identity and pure provider observation. Bind this alias only when actual geometry supports removal; composition validates the same geometry/removal alias owner. Path values are detached, selected-space placements encoded through original LineDeclaration; no persisted copy or new graph.

LineEffectContracts delegates removal admission/execution to ILineRemoval; progress/physical plan validation unchanged. Native LineService derives original fixed tile footprint and delegates original CheckRemoval/Remove pool preflight/conservation. PointSpace derives fixed point subject geometry and checks original layer write admission before original layer removal. Its capability alias binds under original points module, never infrastructure.

## 3. API surface

Optional ILineRemovalGeometry and ILineRemoval; same LineConstructionService alias. Existing INetworkTopology retains native tile topology-only primitive. No new player command, order type or stock ledger. Core2.461.0/save154/replay1; original cursor3/system1 remain. Native topology codec5 represents its actual pre-initialization state for composition observation; uninitialized state cannot be written/read as a stable checkpoint. Geometry checks observe original actual spatial state as well as provider state, with no substitute graph or observation-only serializer.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---|---|
| Modify | InfrastructureWork/InfrastructureModule/LineConstructionService/LineEffects | 100 | high |
| Modify | Networks and TileLineGeometry | 45 | high |
| Modify | actual external PointSpace/PointLineGeometry contracts | 45 | medium |
| Modify | prior physical corridor factory helper visibility | 4 | low |
| New | authored point removal activity JSON and one thin public-player proof | 160 | medium |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Loaded removal effect lacks selected removal geometry/service | ContentException before Initialize |
| Missing/foreign/mobile/nonspatial/wrong-space actual subject | Typed refusal, no job or geometry change |
| Removed corridor/changed owner/occupied native pool | Existing typed refusal/blocker; no successful prefix |
| Unearned/canceled physical activity | Original progress/plan rejection; no removal |
| Provider mutates/throws during admission | Original first fault, no healthy prefix |
| Saved partial physical removal | Original order/machine/geometry state and validation |

## 6. Backward compatibility

Dev only; delete shared native path and migrate aliases in place. No old facade, default geometry, legacy engine or duplicate responsible class.

## 7. Verification

One NEW exact bounded case loads actual JSON/master for native and point minute/half physical construction/removal. Ordinary public previews/commands/results/views/Game.Update only. Point rebuild uses original physical supply and shared corridor owner; native removal retains actual split pools and stocked-last-pool refusal. Partial work/cancellation/current target ownership/pause/save twins/private detached completion facts/replay/isolation and owner identity covered. FINAL166/0/0 in2124ms; all five builds/five runtime attempts retained. Missing selected removal capability before Initialize is source-reviewed, not runtime-proven by this case. One solution compile and necessary focused repairs only, no historical/family/full runtime.

## 8. Risks

| Risk | Mitigation |
|---|---|
| Removal contract duplicates topology or jobs | Original native preflight/stock and WorkBoard remain sole owners |
| Provider returns mutable/invented removal path | Observe actual subject/state, detach selected-space canonical placements, recheck at earned commit |
| Point proof accidentally claims generic network conservation | No point network store invented; native conservation scope explicit |
| Mobile workers become removal sites | Geometry requires fixed actual subject; no shared concrete type case |

## 9. Out of scope

Generic topology/utility/pressure flow, arbitrary network removal plans/providers, all private/fault/stress/global/full gates, Oil/platform/Draw/Tycoon/G19 and publication. These remain required, not waived. Broad P10.003/P3.004 remain open.

## 10. Sub-task execution order

Inspect actual native disposition and effect/work contracts; add narrow geometry/removal contracts to original owner; delete shared tile footprint dispatch; bind selected native/point aliases; author actual physical removal proof; compile and run one new exact bounded case; retain all attempts; freeze evidence/master metadata without broad closure.


## Metadata closure

Post-pass freeze 2026-10-11 21:44:39 UTC: 1606 source anchors,13 quickstart anchors,865 selected executable entries. Scan2795 paths/982 Markdown files/8610 local links,zero broken links,18 well-shaped master rows. No source/quickstart/executable mismatch or thin-driver boundary matches; git diff --check exit0 (34 existing line-ending warning lines). All221 original gate texts/checkboxes/line anchors unchanged;184 checked/37 open; additional187. Seventeen frozen executable entries changed (seven existing production),two new executable files and four new source anchors; no new production owner. Protected clock/Tycoon hashes unchanged. Dirty2783/staged0. No executable edits after FINAL or commit/push. Integrity does not prove full gameplay closure.
