# P4-G1 — Sole authored activity graph and named execution

[P4-G8 open work catalogs](P4_WORK_CONTENT_CONTRACTS_DESIGN.md) now resolves actual role/executor/requirement content and validates declared group topology against evaluated entries. Shared resource/source/staffed/group owners retain the existing WorkBoard graph and physical execution. [75-check scoped evidence](../../docs/migration/P4_WORK_CONTENT_CONTRACTS_VERIFICATION.md); all original broader gates remain open.

[P4-G7 open authored activity contracts](P4_ACTIVITY_CONTENT_CONTRACTS_DESIGN.md) now shares one ActivityDeclaration parser/compiler and actual file-owned aliases across player plans, construction, incidents and other authored consumers. The same ActivityGraph/WorkBoard owns execution. [84-check scoped evidence](../../docs/migration/P4_ACTIVITY_CONTENT_CONTRACTS_VERIFICATION.md); broader catalog/acceptance gates remain open.

Current [G4a authored construction/removal](P4_AUTHORED_CONSTRUCTION_WORKFLOWS_DESIGN.md) removes
their inline node readers/private graph compiler/stale hours guard. Native bindings alias selected
ActivityDef.Graph; same board/site/tool/input/time/finalization owners. ONE exact NEW bounded native
case61 checks passes; ONE clean build11.23s. Build2.241.0/save139/all payloads unchanged.107 older
assertions/four JSON/nine C# fixture migrations source/compile ONLY. No full gate; source frozen.
[Evidence](../../docs/migration/P4_AUTHORED_CONSTRUCTION_WORKFLOWS_VERIFICATION.md). Full G3/G4/G5
and remaining P1–P12 stay open.

Current [G3d fixed-position execution](P4_FIXED_POSITION_EXECUTORS_DESIGN.md) extends the SAME
executor contracts with typed access waits and pure worker admission. Actual native JSON/player
proof63 checks/one exact NEW bounded case passes; THREE builds, final clean3.52s. Build2.240.0/
save139/Board43/WorkSystem41/View79 bytes unchanged.106 older assertions compile ONLY. No full gate.
[Evidence](../../docs/migration/P4_FIXED_POSITION_EXECUTORS_VERIFICATION.md). G3 remains partial:
dock-fixed/team/process/haul/provider/G4/G5 and broader P1–P12 remain open. Source frozen after green.

Current consumer [P10-A3 physical corridor DAG](P10_SEGMENT_CONSTRUCTION_DESIGN.md) uses this same
graph/executor: one reached segment's time/input/cost/effect, dependency-gated future supply,
completed-prefix progress and ordinary cancellation/save/load.2.125.0/save68/line_work2;386/two
exact suites, [evidence](../../docs/migration/P10_SEGMENT_CONSTRUCTION_VERIFICATION.md).
No new scheduler or progress store. Earlier G1/G3 suites compile only, not rerun; broad P4 stays open.

Current [G3a typed completion](P4_COMPLETION_EFFECTS_DESIGN.md) is focused-verified on
2.72.0/save44/work21/work-view26: **214 / two exact suites**, zero warnings/errors/failures/aborts.
The same graph/node/order codec retains Completing and authored retry facts; dependencies wait
for Applied, earned work/spending is not repeated, and saved final effect-applied facts prevent
duplicate effects behind replacement guards. Actual workers/tools release once; pause/cancel,
private diagnostics, checkpoint/target fault guards and native construction/recovery pass.
[Evidence](../../docs/migration/P4_COMPLETION_EFFECTS_VERIFICATION.md). No second executor/queue/
state owner or compatibility callback. Generic G3 executor breadth, G4/G5 and full P4 remain open.

Status: G1 implemented and focused-verified on 2026-10-05; not whole P4 completion.
Build2.45.0/save20, work schema7/view6; **118 checks / two exact suites**, zero failures/aborts.
See [current evidence](../../docs/migration/P4_ACTIVITY_DAG_VERIFICATION.md). The approved framework objective remains
P1–P12. Existing P7 prerequisites are available; advanced selection/acquisition/executors follow G1.

## Problem and scope

The removed StepDef.ResolveAfter rejected dependencies declared later and
stored definition/runtime relationships by array position. ActivityGraph now replaces that path. WorkBoard/WorkSystem continue to own physical
orders/jobs, native inputs, progress, cancellation and persistence. The dependency and positional state paths are replaced in place; do not add an ActivityManager, second scheduler, progress store or compatibility path.

## Contracts and definition authority

- Rename the current node/definition/plan contracts in place to ActivityNodeDef, ActivityDef,
  IActivityNodeDefinition, IActivityDefinition and IActivitySource. ActivityDef remains a normal Def
  in the existing content registry. Master-listed activities/nodes are the only canonical work JSON;
  remove orders/steps section/property acceptance and source-migrate the standalone proof content.
- IActivityDefinition exposes one IActivityGraph. ActivityGraph is the immutable sole dependency
  compiler over stable node IDs and typed node data, not runtime state. Definitions resolve resources,
  requirements/roles/effects through existing registries before graph compilation.
- Reject empty/duplicate node IDs, duplicate/missing/self edges, and cycles with deterministic named
  diagnostics. Allow forward references. Canonical topological order uses ordinal ID tie-breaking,
  independent of declaration/dependency-list ordering. Use an iterative algorithm; no authoring size cap.
- Dependency ancestry for physical effects comes from this same graph, deleting old Index/AfterIndex/
  ResolveAfter paths. The graph contract exposes immutable nodes and named lookup/ancestry facts.
  Authored document fingerprints remain exact content identity: reordered documents may differ in
  fingerprint while topology/execution is equivalent. Do not claim cross-document save compatibility.

## Execution owner and durability

- WorkBoard still owns orders/jobs. Each order holds one named node execution record per graph node:
  ID, state, job identity, progress and typed blocker. Remove parallel positional state/job/progress
  arrays. Named lookup aliases these same records; it is not another execution authority.
- Jobs refer to NodeId, not a serialized numeric step. WorkSystem advances canonical graph nodes,
  uses named dependencies, assigns actual workers and preserves real physical timing/materials.
  Parallel ready nodes compete through the same worker/job/receipt owners.
- Aggregate outstanding material demands once per order/resource across eligible supplying nodes;
  parallel nodes must not overwrite each other's demands through the existing order demand identity.
  Consume actual native bills per node; no new demand or stock ledger. End/cancel withdraws that order.
- Complete nodes clear their finished job links; cancelled/paused execution preserves actual progress
  and named state. Per-node blockers are saved and projected; no host evaluates prerequisites.
- Sync writes named node records and named job links, rejects missing/duplicate/mismatched IDs and
  live orphan links after load. Existing work service owns the only codec; break its schema/build/save
  identity together, no old array reader. Hash/save use those same records.
- WorkView exposes detached per-node dependencies/state/progress/job/blocker from the same graph.
  Existing summary/requirements/effect previews migrate; privacy and pure reads remain unchanged.

## Consumers and migration

Migrate every standalone work consumer/proof: construction/demolition definitions, line plans,
incidents/effects, ordinary work commands and views, state/duration/source probes and JSON fixtures.
Generated build/line plans implement the same graph contract, not an old executor. Their conversion to
reusable authored templates remains G4. Oil consumes the breaking API through its P5 gate; do not edit
or claim verification of oil/Tycoon/hosts/renderers here. Seismic/logging are never injected prerequisites.

Files: Standard/Work.cs, ActivityGraph.cs, WorkContracts.cs, WorkEffects.cs, WorkModule.cs,
Construction.cs/ConstructionModule.cs, Networks.cs, Incidents.cs, StandardCommands.cs, WorkView.cs,
SaveGame.cs, directly affected public-contract proof/source/content/tests and existing stage/master docs.

## Verification and sequence

1. Replace definition/compiler contracts and canonical JSON; migrate current consumers in place.
2. Replace positional execution/codec/job references with named records and per-node diagnostics.
3. Advance real workflows through existing WorkSystem, including parallel material-demand aggregation.
4. Build standalone; run only two exact new suites: deterministic graph validation/admission and
   named physical parallel work/blockers/cancellation/saved execution. Reuse only directly affected
   existing physical recovery method if needed; no broad family or full suite. Old suites compile,
   are not all rerun, and do not become current regression evidence.
5. Update master/report/README/AGENTS and parent P4 after actual evidence; leave G2–G5 and full gates open.

Acceptance: out-of-order dependencies execute correctly; duplicate/missing/cyclic graphs refuse with
named diagnostics; graph order and named board state are deterministic; real parallel nodes are bounded
by available machines and material delivery; node views are private/detached/pure; saves at dependency,
supply/work/pause/cancel stages restore exact IDs/progress without duplicate effects or resources.

## Remaining scope and risks

This does not complete all/any provider selection, tools/fluid-continuous acquisition, generic executor
registration/ToilKind removal, prior-activity facts, optional branch selection, authored construction/line
templates, critical-path/ETA, areas/policies/docks, oil graph migration or full replay/stress/soak gates.
Those remain G2–G5/P5/P7–P12. A stable graph/node record does not prove every future requirement category.
Provider modules remain trusted. Migration is breaking and preserves unrelated dirty edits; no shim,
legacy loader, duplicate graph/progress authority, Draw or platform code is introduced.
