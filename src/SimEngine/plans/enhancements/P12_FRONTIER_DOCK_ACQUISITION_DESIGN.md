# P12-D13 / P4-G2: selected service-slot acquisition

## 0. Problem

Joint dock admission can select a later bay for an unrestricted worker to leave the only compatible
bay for another worker. `WorkSystem.FitsAssignmentSet` currently discards those chosen slots and
`WorkBoard.Assign` asks for its default bay again. Actual dispatch can therefore queue a worker
despite a feasible simultaneous team and a ready preparedness preview.

## 1. Goals

- Carry exact selected service identity from the SAME joint evaluator to the existing first native
  acquisition; fresh admission must check that exact bay, never silently substitute another.
- Preserve actual worker/target/role/policy/geometry/route/occupancy/live-claim/resource checks.
- Rebuild after each real acquisition as today. No preview-owned claim or atomic team promise.
- If no joint set exists, continue ordinary actual queue admission with honest readiness blockers.

## 2. Design

Extend existing PreviewDock/SelectDock with an optional required slot filter, applied alongside
productive-kind and existing exclusions. Missing/removed/incompatible/occupied/area-refused slots
do not fall back. A dockless target cannot satisfy a required real bay.

FitsAssignmentSet optionally projects its detached selected dock IDs by actual matched demand.
After matching, recompute that SAME admission once. Only a fully feasible current set supplies
slot intentions; refusal leaves the map empty. TryAssign passes the selected intention to existing
WorkBoard.Assign, which freshly checks it before acquiring any resource or worker. Existing
preemption still rechecks after actual interruption. Later jobs rebuild from actual newly held bays.
Selection is transient, not another reservation, saved owner, provider model, clock or command.

## 3. API

Engine-internal selection only. Existing public JSON, policy, commands and immutable preparedness
fields unchanged. Build identity changes; saved payload and view shape unchanged. No legacy shim.

## 4. Files

Modify WorkDocks, WorkScheduling and Work Assign in place. Add/register one new exact bounded case,
reuse master-selected point/workflow/dock documents, and migrate strict build assertions compile-only.

## 5. Errors

Exact bay refusal causes no acquisition and no alternate bay substitution. Provider faults propagate.
Only actual fully feasible joint admission projects selected identities. No partially admitted map.

## 6. Compatibility

Development replacement; no compatibility layer or old behavior promise.

## 7. Verification

One completed-slice build and ONE distinct NEW bounded case:
`Response service selection: exact joint bays, fresh refusal and saved acquisition`.
Repair/rerun only observed errors. No old/family/full/platform/stale gate. Freeze source/tests/JSON
after final green; docs-only afterward. Record every attempt and scoped evidence.

## 8. Risks

Current slot selection is not future departure reservation, external-provider joint admission,
atomic team acquisition, global resource-branch commitment or combinatorial CPU/stress proof.
Joint resource-source choice acquisition through actual jobs remains open; this slice only preserves
the selected service slots, not every selected obligation of the whole frontier.

## 9. Non-goals

No Oil/Tycoon source/business policy/host/demo/platform/Draw changes or extra state/clock.

## 10. Execution

1. Extend the existing exact dock admission filter and transient selection projection.
2. Wire existing first native acquisition/rebuild, preserving ordinary physical queueing.
3. Verify actual area-constrained slot backtracking, exact fresh refusal, saved physical parallel
   work and ordinary queued completion using normal player home commands.
4. Update master/parents/evidence and retain full P1-P12 ACTIVE/publication deferred.

## Current scoped outcome

Implemented in the existing owners; build2.254.0/save139 and all persistent payloads unchanged.
ONE clean build4.83s/0 warnings/errors. ONE exact new case48 checks0 failures/aborts486ms FIRST
attempt.120 older strict build assertions plus two internal call/helper migrations compile-only.
No older/family/full/platform/stale gate. Source/tests/JSON frozen after green, then docs-only.
See [evidence](../../docs/migration/P12_FRONTIER_DOCK_ACQUISITION_VERIFICATION.md).
