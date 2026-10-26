# P10-A16: complete occupied graph admission — Design Document

## 0. Problem Statement

Standard/Networks.cs BuildTopology checks reciprocity only when traversing an unvisited same-company neighbour. A backward-only occupied edge into a previously visited vertex bypasses validation and silently yields separate components. Repeated provider calls can also give one admission multiple callback observations.

## 1. Goals

1. Validate all occupied vertices and all occupied edges before any component candidate, allocation, stock transaction or identity commit.
2. Reject duplicate/missing identities, malformed potential neighbours and nonreciprocal occupied edges regardless of visitation, company or removal intent.
3. Capture actual vertices/adjacency in one existing scoped spatial/provider observation; local admission data is transient, never a cache/codec/second graph owner.
4. Keep original topology, inventories, physical work, flow, clock, fault cutoff and durability owners.
5. Authored engine-side malformed providers fault explicitly; no silently disconnected fallback.

## 2. Design

Original NetworkService.BuildTopology reads vertices and sorted potential adjacency under one ReadGeometry callback scope. It validates actual IDs/company and every occupied reciprocal edge, then uses those local dictionaries for its original connected-component traversal. Original Redistribute/ApplyTopology and saved graph codec unchanged. Neighbours remains the saved settled-graph validator helper; no parallel scheduler or topology model.

External PointSpace's existing JSON graph authoring accepts explicit test-only fault modes. Fault activates only after two actually constructed sparse vertices exist. Provider data and callbacks are engine-side; player driver submits normal physical plans and Update only. Backward-only demonstrates original missed edge; duplicate/self/invalid-neighbour/duplicate-vertex/invalid-owner/throw/mutate-layer exercise the same admission/fault boundary.

## 3. API surface

No new engine service, state, command or graph codec. Core2.463.0/save154/replay1/network6/view7/cursor3/system1. Existing geometry contract explicitly documents reciprocal occupied adjacency. Fault authoring belongs external proof only.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---|---|
| Modify | Standard/Networks.cs and NetworkGraphGeometry.cs | 50 | high |
| Modify | existing PointSpaceDef/PointNetworkGeometry JSON fault authoring | 45 | medium |
| Modify | existing point graph factory optional authoring callback | 3 | low |
| New | one thin public-player graph admission case | 120 | medium |
| Modify | build/protocol identity and tracker | scoped | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Invalid/duplicate actual vertex/company | Original first fault before candidate commit |
| Self/duplicate/invalid potential neighbour | Original first fault |
| Backward-only occupied edge | Explicit reciprocity fault, no empty/disconnected success |
| Throw/mutation during callback | Original scoped observation fault, no healthy prefix/save |
| Healthy physical graph | Original stock/flow/save/replay behavior |

## 6. Backward compatibility

Dev only; revise original algorithm, no old validation path or compatibility mode.

## 7. Verification

One NEW exact bounded case: normal public player commands/views/Update load actual JSON/master, two healthy point minute/half physical graphs and sixteen authored malformed callback flows during real physical construction. Terminal fault/save/query/update cutoff retains first fault. FIRST/FINAL254/0/0 in2582ms; one clean solution12.52s/0warnings/errors. No repairs/reruns, historical/family/full runtime or executable edits after FINAL. Broad matrices remain open.

## 8. Risks

| Risk | Mitigation |
|---|---|
| Temporary admission data becomes parallel state | Local variables only, original codec unchanged |
| Validation merely skips bad edges | Complete occupied-edge pass before traversal |
| Test driver creates graph state | Authored provider behavior and normal physical construction only |

## 9. Out of scope

All arbitrary-provider/private CLR/claims/utility/pressure/performance/stress/global/full matrices, Oil/platform/Draw/Tycoon/G19 and publication. Broad original requirements stay open.

## 10. Sub-task execution order

Read current admission; create stage/tracker; replace traversal-only validation with complete scoped capture; author external provider faults and new public-player case; compile and verify exact bounded case; preserve attempts and freeze evidence/master without broad closure.


## Metadata closure

Post-pass freeze 2026-10-11 22:31:11 UTC: 1616 source anchors,13 quickstart anchors,870 selected executable entries. Scan2804 paths/986 Markdown files/8614 local links,zero broken links,18 well-shaped master rows. No source/quickstart/executable mismatch or thin-driver boundary matches; git diff --check exit0 (34 existing line-ending warning lines). All221 original gate texts/checkboxes/line anchors unchanged;184 checked/37 open; additional189. Eight frozen executable entries changed (three existing production),one new executable proof and three new source files; no new production owner. Protected clock/Tycoon hashes unchanged. Dirty2792/staged0. No executable edits after FINAL or commit/push. Integrity does not prove broad gameplay closure.
