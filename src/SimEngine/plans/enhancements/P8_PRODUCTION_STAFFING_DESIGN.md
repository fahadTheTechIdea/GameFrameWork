# P8-D4b — JSON-authored actual production staffing

## Current M159 — shared staffing metadata (2026-10-25)

Implemented on engine2.776/save161/replay2. IStaffedTransformationGateDefinition now
declares facility/process policy, named processors, recipes and requirements. Requirements
include the actual IAuthoredActivityDefinition alias, not just a string that could name an
unrelated graph. StaffedTransformationGateDeclaration is the sole JSON reader/resolver;
the native definition and independent registered content kind delegate to it. The original
StaffedTransformationGate consumes the contract, not its built-in definition class.

StaffingGateBindingContracts admits all loaded runtime/metadata pairs through the existing
TransformerManager composition phase, before spatial filtering or Update. It validates frozen
owner/codec identity, actual recipe/activity aliases, ordered unique identities, compatible
positive-time machine nodes/roles, complete effect-free automatic teams and required work
services. Automatic team-graph validation moved out of the declaration into that one binding
owner; there is no second staffing scheduler, assignment store, progress model or clock.

Facility and process policies retain their existing semantics: facility work is ordinary
player-posted activity; process work is bound to the exact queue execution and named processor.
Physical assignment/travel/docks remain WorkBoard/worker dispatch, distinct matching remains
DistinctWorkerMatching, and original material/tools/WIP/production owners are unchanged.
Independent proof metadata contains no gameplay methods and uses the same runtime/codec2.

Focused acceptance completed: native/independent facility100 checks each and native/independent
process104 each,408/0 total/four actual final-live-hash replay checks. One targeted clean build,
no failed attempts or unrelated green reruns. No original compound gate closes merely from
this contract slice. [Exact evidence/self-review](../../docs/migration/P8_PRODUCTION_STAFFING_VERIFICATION.md#current-m159-contract-authored-staffing--2026-10-25).
Quality/lot remains explicitly deferred; no full/profile/family/soak run, Oil/host/demo/platform
change or publication. Continue the existing finite P8 processing/provider matrix, not new
speculative staffing variants.

## Historical D4b implementation

Status: scoped implementation verified98/0/0 in the final NEW exact bounded case on2.314.0.
Four builds, final clean3.49s. Three attempts:2/1 fixture content abort,85/0,final98/0.
Earlier cases not rerun; automatic queue-bound staffing lifetime/full goal remain open.
[Evidence](../../docs/migration/P8_PRODUCTION_STAFFING_VERIFICATION.md).

## 0. Problem Statement

Transformation execution already checks ITransformationGate before acquiring or consuming native
resources. D4a exposes actual assignments, but recipes cannot yet require a real productive machine
team. A role tag, idle candidate or visual entity state is not an assignment. Add a stateless gate
over existing authored work, not another worker scheduler/reservation/progress store.

## 1. Goals

1. Native JSON staffing requirements declare stable IDs, exact activity/node, alternative roles and
   positive counts. Missing/mismatched references and missing selected work services fail before Update.
2. IWorkAssignments freshly checks actual productive assignments: reciprocal worker/job, owner,
   target/phase, earned native work, current policy/rate/requirements and physical dock/reach.
3. Native production reuses ITransformationGate and DistinctWorkerMatching, never double-counting
   an actor across requirements. WorkBoard remains the only machine acquisition/lifetime authority.
4. No actual staffing means no startup WIP/tool claims, progress, utility draw or completion. Loss of
   staffing preserves existing process state until compatible actual work resumes; cancellation still
   follows the existing material-return path without requiring a machine to refund consumed inputs.
5. Private immutable staffing diagnostics and all saved behavior derive from existing job/queue owners.

## 2. Design

Add CheckProductiveAssignment to the existing IWorkAssignments owner, not a second facade. Native
StaffedTransformationGateDef references actual ActivityDef graphs and validates node/role identity.
Its composition requirement is conditional: unstaffed transformations do not require sim.work.
The component has no runtime fields/Sync payload. Each read obtains actual native productive workers,
matches canonical requirements with the existing stateless maximum matching algorithm and returns
copied WorkExecutionFacts. Production uses the same read to gate batch/continuous execution.

Staffing here is an explicit ordinary activity intent: players or existing engine plan/recurring
sources post physical work, and production operates while that work is actually productive. The
activity's authored duration owns the staffing interval. This is not automatic process-bound team
creation or a substitute for future queue-driven lifecycle admission/release. Paused production may
retain process WIP while its independently authored staffing activity ends normally.

## 3. API surface

IWorkAssignments.CheckProductiveAssignment(viewer, worker, target, out facts); native
ITransformationStaffing/ITransformationStaffingRequirement contracts, copied staffing snapshots,
and TransformationManagementView.ReadStaffing(entity, processCapability, transformation, out facts).
Optional component key staffed_transformation_gate; schema1. Build2.314.0/save148/replay1;
transformation view16. No old schema/compatibility reader or second clock.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | src/Beep.SimEngine/Standard/WorkContracts.cs, WorkAssignments.cs | medium |
| New | src/Beep.SimEngine/Standard/TransformationStaffing.cs | medium |
| Modify | src/Beep.SimEngine/Standard/TransformationContracts.cs, Transformations.cs, TransformationModule.cs | medium |
| Modify | src/Beep.SimEngine/Views/TransformationManagementView.cs | medium |
| New | tests/ExtensionProof/StaffedTransformationProof.cs and content/staffed-transformations.json | medium |
| New | tests/Beep.SimEngine.Tests/ProductionStaffingTests.cs | medium |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Missing/duplicate need, nonpositive count, undeclared/incompatible node role | ContentException |
| Selected gate without actual work/dispatch service | ContentException before first Update |
| Idle/travelling/paused/wrong-target/unsupported assignment | Staffing shortage, no process progress |
| Current policy/rate/material/reach refusal | Candidate not counted, no fabricated replacement |
| Foreign private view | NotFound, no team facts |
| Staffing cancellation during process | Existing WIP/tools retained, no process burn |
| Process cancellation without staffing | Existing physical material return/release owner |

## 6. Backward compatibility

Development only; no legacy reader or adapter. Existing unstaffed content remains explicitly
unstaffed because it does not select the optional capability, not through an invented default.

## 7. Verification

One new exact bounded case: authored two-machine teams/alternatives, actual travel and phase waits,
private/pure diagnostics, resource startup, staffing loss/replacement, saved continuation, completion,
process cancellation and player replay. Build for compile; no older/family/full/Oil/platform runtimes.

## 8. Risks

Facts describe the current completed work checkpoint plus fresh native policy/reach/requirements,
not permission to move or acquire a machine. System order stays350 production/400 work. No same-step
retrospective staffing, hidden order creation or duplicate station reservation. Dynamic/custom phase
and provider breadth require separate evidence; this case does not close the full machine-tool matrix.
Facility staffing is not a per-processor exclusive machine lease. Exact named sibling process binding
belongs to the still-open automatic queue-bound lifetime integration, not copied matching results.

## 9. Out of scope

Automatic queue-bound team admission/lifetime/release, scoped physical supply/export, full provider
fault/lockstep/stress gates and whole-plan completion. Full compound P8 acceptance remains open.

## 10. Sub-task execution order

1. Add fresh productive-assignment checks on WorkBoard.
2. Add strict authored staffing and use existing matching/gate dispatch.
3. Add private facts and native engine-owned fixture.
4. Compile/run only the new bounded player case; record all attempts and refresh tracker/audit.
