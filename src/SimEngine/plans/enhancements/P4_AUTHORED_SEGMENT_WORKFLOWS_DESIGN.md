# P4-G4b — Authored reusable infrastructure segment DAGs

## 0. Problem

LineDef still owns flat role/executor/duration/requirement fields. LinePlan synthesizes one node per
tile; completion/restore use NodesDone as tile count. This prevents authored multi-stage or parallel
preparation and duplicates gameplay definition outside ActivityDef. G4 requires authored workflows.

## 1. Goals

1. Required activity reference binds a selected master-listed ActivityDef; segment_node identifies
   its single final stamping node. Delete flat execution fields, no old parser/default translation.
2. Profiles are non-orderable, no target tags/standalone final effect. Final stamping node must have
   every other profile node in its dependency ancestry and positive physical work in that ancestry.
   Its actual effect must be a segment LineEffectDef; no other node can stamp/finish the cursor.
3. Each actual tile binds the SAME authored graph data to stable segment.<index>.<node> IDs.
   Authored edges stay intra-segment; roots additionally depend on previous segment's final node.
   Preserve parallel branches. WorkBoard/WorkSystem alone own progress/jobs/resources/effects.
4. Derive completed tile prefix from final node completion, never node-count or a second counter.
   Restore/effects validate the exact actual bound graph/cursor/earned work. Only real final node
   completion stamps current tile and advances the intent cursor; workers/stock never teleport.
5. Native player proof covers multi-stage content, physical approach/input/parallel readiness,
   cancellation preserving completed prefix, private immutable views and saved continuation.

## 2. Contracts and integration

IActivityWorkflowBinding unifies the existing immutable Workflow reference for construction/removal/
line bindings, without a second catalog or execution owner. LineDef resolves activity; linked graph
validation occurs in ValidateResolvedCore after all references resolve, independent of read order.
segment_node and finish_effect are explicit. LinePlan expands stable IDs/dependencies through the
existing ActivityGraph compiler; no authored rate/duration/roles/requirement/effect is synthesized.

WorkBoard's existing SupplyCapacity projection gains graph overload, shared by construction and
line capacity admission. Actual resource receipt/spending/supply policy algorithms stay unchanged.
CheckIntent validates native representational count/time overflow, not an arbitrary/platform cap.
LineWork Sync bytes stay the same; rebuilding actual binding after restore uses selected immutable
content, with strict engine build identity and no old graph reader. All payloads/save139 unchanged.

## 3. Files

Networks/InfrastructurePlans/ActivityGraph/ConstructionContracts/WorkStart/Construction/SaveGame;
affected JSON/fixture sources; new native profile/layer/startup content and one bounded player case;
Program/resources; stage/master/P4/P10/AGENTS/evidence/scoped manifest. No Oil/platform/Draw/host/demo/
business gameplay changes, second clock/store/scheduler or compatibility shim.

## 4. Verification

Build completed slice; ONE exact distinct NEW bounded native player case. Repair only observed
failures, record each invocation. Older metadata/fixtures source/compile-only. No old/family/full/
platform/stale gate. Freeze code/tests/JSON after final green; docs-only inventory/tracker handoff.
Full G3/G4/G5 and P1–P12 remain open; publication ONLY after full goal/plan completion/verification.

## 5. Remaining

Provider-neutral/non-tile corridors, process/team/haul workflow bindings, broader shared tools/
parallel contention/docks/target effects/global purity/fault/replay/stress/CPU/ETA and full gates.

- [x] Selected profile contract, graph binding and actual completed-prefix runtime migration.
- [x] Direct authored/source migration and one native bounded proof.
- [x] Focused evidence, trackers and scoped inventory handoff.

Verified2026-10-07,build2.242.0/save139/all payload bytes unchanged. TWO builds:1 new proof duration
comparison error0 warnings5.65s, clean3.79s. ONE exact NEW bounded native case80 checks0 failures/
aborts486ms FIRST runtime attempt.108 older build assertions/six JSON/nine bindings/profiles/nodes/
two C# fixtures source/compile ONLY. Source/test/JSON frozen after green; docs-only afterward.
[Evidence](../../docs/migration/P4_AUTHORED_SEGMENT_WORKFLOWS_VERIFICATION.md). Wider gates above
remain open; FULL goal/plan incomplete. Commit/push ONLY after FULL completion/verification.
