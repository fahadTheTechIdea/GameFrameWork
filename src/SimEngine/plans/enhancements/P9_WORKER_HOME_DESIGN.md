# P9-C7 — Canonical worker home intent and physical return

Status: focused implementation verified,2026-10-06. Continues P9 fleet policies; full P9/P1–P12 remains partial.
Clean standalone build;253 checks in exactly two named suites, zero failures/aborts.
[Scoped evidence](../../docs/migration/P9_WORKER_HOME_VERIFICATION.md).

## 0. Problem

`Standard/Work.cs:55` already owns Home on IWorker. `SetHomeHandler` cannot clear it and lacks a matching
management preview. IdleBehaviour skips moving workers before checking current home; stale movement
survives reassignment/removal. RefreshPolicy can also stop a manually held worker as if it were an idle
home return. WorkView exposes no immutable home/return facts. Adding another fleet/home store is incorrect.

## 1. Goals

1. Set/clear actual owned home through shared pure command/preview validation and exact IWorker commit.
2. Retarget moving idle returns and stop invalid/cleared returns before movement, without teleportation.
3. Preserve live jobs, manual Held control, resources, native promises and physical cargo-return priority.
4. Publish private detached home/return facts from the same canonical reader and existing execution gates.
5. Prove native and public-only IWorker implementations, timed motion and saved continuation in two suites.

## 2. Design

- Add stateless `Standard/WorkerHomes.cs`: WorkerHomeContracts, immutable WorkerHomeView and return-state
  classification. No persistent service, new component, queue, timer, route, resources or second setting.
- IWorker.Home remains its sole runtime identity. Document that a changed setter touches its entity and
  changes no other state. Native Worker uses one private backing field; native Sync stages its existing
  four fields before replacement and rejects negative job/home identities. No old reader/layout.
- CheckHome resolves actual worker/owner, explicit None (clear), different actual owned endpoint, motion
  capability and provider-supported geometry. Current route/area/fuel readiness is execution state,
  not permission to record a future home intent. Blocked destinations may be selected and remain blocked
  in canonical diagnostics; no current policy/route/fuel blocker is bypassed during actual movement.
- Pure observations guard actual worker and selected target versions/owners/world tick/entity allocator.
  Exact commit guards Home, entity touch and unchanged job/manual/motion status/owner/tick/allocator.
  These are scoped guards, not global rollback or every provider field/cross-service mutation.
- Native idle return reuses SpatialInteraction.Approach every relevant pass, so the existing spatial
  provider retargets current destinations and retains unchanged actual goals/paths. Missing/foreign/
  unsupported/disabled/outside-area homes stop automatic motion. Readiness/fuel gates remain movement
  owner rules, retain real goals and use MotionExecutionContracts/WorkerExecutionContracts in views.
- Idle actual cargo uses the existing IHaulExecution.PostReturnCargo first. Live jobs and Held direct
  control cannot be redirected by a home edit. ReleaseVehicle explicitly ends its manual motion before
  ordinary idle behavior; policy refresh checks only autonomous idle motion, not Held direct goals.
- WorkBoard.Assign ends the actual prior idle motion before committing the worker/job relation.
  Assignment happens after running existing jobs but before movement; the new job must not earn one
  extra step toward an old home while waiting for its first toil. This covers ordinary work, standing
  hauling and residual cargo through the same assignment owner, never a home-only job path.
- MotionExecutionContracts.ReadStatus provides the existing pure/enum-checked actual motion status;
  Stop verifies the actual provider becomes Idle without changing owner/time/entity allocation.
  Home, release and assignment use it. Ignoring Stop faults normal Update and prevents checkpointing;
  it is not a universal proof of all provider coordinates, side effects or other historical Stop callers.
- None means unassigned, never an inferred depot. Missing historical target remains the saved intent
  and a typed blocker until explicitly cleared/reassigned; never silently substitute another entity.
- Runtime IDs are commands/state, not JSON prototype IDs. Existing JSON authors workers, geometry,
  resources, motion/operation inputs and work policies; no parallel home-content model is required.

Build2.109.0/save65; native payloads unchanged. Work view34/work system7; worker component1 unchanged.

## 3. API surface

Existing SetHomeCommand accepts Home=None to clear. WorkView.ReadHome(EntityId,out WorkerHomeView) and
PreviewHome(EntityId,EntityId) expose detached facts/shared admission. IWorker/ISpatialInteraction/IMotion
stay the generic extension seams; no concrete tile/point dependency in WorkerHomeContracts.

## 4. Files

| Action | File | Scope / risk |
|---|---|---|
| New | Standard/WorkerHomes.cs | Shared stateless protocol/view; medium |
| Modify | Standard/Work.cs,StandardCommands.cs,Capabilities.cs | Existing state/idle/commands; medium |
| Modify | Standard/MotionExecution.cs | Shared pure status/verified Stop; medium |
| Modify | Views/WorkView.cs,Standard/WorkModule.cs,Kernel/SaveGame.cs | View/identity; low |
| Modify | ExtensionProof/ContractCapabilities.cs,PointSpaceProof.cs | Public IWorker proof; medium |
| New | Tests/WorkerHomeTests.cs | Exactly two named gates; medium |
| Modify | prior identity/view/system assertions | Compile-only migration; low |
| New | docs/migration/P9_WORKER_HOME_VERIFICATION.md | Actual scoped evidence |
| Modify | stage/master/parent/README/AGENTS/help | Accurate current boundaries |

## 5. Error handling

Missing/foreign worker/endpoint, malformed IDs, self-home or unsupported geometry refuse explicitly.
Clear is allowed on a worker without motion. Operational blockers are retained as typed return facts,
not default home/stock/route grants. Lying/mutating IWorker callbacks fault ordinary engine execution.
Native malformed/truncated decode retains all prior worker fields. No forged-global-state guarantee.

## 6. Compatibility

Development-only clean revision, no compatibility shim, duplicate fleet facade or legacy save reader.

## 7. Verification

Build standalone solution; run exactly:

- `Worker home: private intent, provider guards and canonical command admission`
- `Worker home: physical retargeting, protected work and saved continuation`

Cover no-home/set/clear/private/observer/refusal parity, pure/frozen facts, independent public worker,
faults, staged decode/pending commands, moving retarget/clear/removal/ownership, route/area/readiness/
fuel waits, manual-held immunity/release, live work/cargo priority, actual timed travel and saved twins.
Also cover new work acquired during a physical home return, residual cargo acquired before home travel,
stock conservation and ignored-Stop refusal at the canonical assignment boundary.
Other suites compile only; no third/family/full run.

## 8. Risks

Public Home setter semantics become explicit. Engine-side initial creation providers still own their
normal state initialization; wider manufacturing/provider creation fault matrices are not this gate.
Approach semantics belong to each selected spatial provider; no geometry fallback or route copy.

## 9. Out of scope

Docked home/service bays, preferred-route authoring, service selection, proactive trip fuel budgets,
named tanks/holds, full manufacturing/public provider faults/replay/stress, Oil/Tycoon, hosts/demos,
rendering/Draw/platforms and full P9/P1–P12 completion.

## 10. Execution order

- [x] Revise existing home protocol/state, assignment/idle behavior, commands and immutable views.
- [x] Extend independent public worker proof; add focused authored/physical tests and migrations.
- [x] Build and run only the two exact suites; repair within these same gates.
- [x] Record actual evidence and align master/parent/guidance, leaving full roadmap open.
