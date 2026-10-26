# P9-C8c — Physical facility operating areas

Status: focused implementation verified,2026-10-06. Parent: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Clean build;222 checks/two exact suites, zero failures/aborts.
[Evidence](../../docs/migration/P9_DOCK_OPERATING_AREAS_VERIFICATION.md).
Prerequisite: [native tile facilities](P9_TILE_SERVICE_DOCKS_DESIGN.md). Full framework remains partial.
Follow-on [C8d](P9_ROUTE_OPERATING_AREAS_DESIGN.md) now gates actual whole-route travel; the historical
slot-only suites below were not rerun. Vehicle envelopes/full itinerary reservation admission remain open.

## 0. Problem

WorkPolicyRules.CheckDestination checks the endpoint footprint, but WorkBoard.PreviewDock,
ApproachDock and DepartDock select outlying physical slots without applying that same area policy.
A permitted building must not authorize travel or service at an excluded waiting/bay/exit position.

## 1. Goals

1. IWorkDock publishes pure immutable actual slot geometry through the existing SpatialSnapshot.
2. Existing WorkPolicyRules/regions alone check geometry, independent of native tiles/public points.
3. Preview and actual assignment reject unusable service/departure geometry before promises/jobs.
4. Live edits pause existing jobs without discarding claims, cargo, earned work or physical position.
5. Promotion/departure select only permitted slots; restricted old waiters cannot block eligible peers.
6. Existing Update, entity state, private facts, saves and deterministic continuation remain authoritative.

## 2. Design

Add mandatory IWorkDock.CaptureSlot(World,Code), implemented by TileWorkDock and the public-only
PointDock extension. Actual presence remains the coordinate owner; snapshots are detached query
results, not another geometry store. WorkPolicyRules.CheckGeometry is shared with CheckDestination.
WorkBoard guards captured session/space/entity versions/tick/identity before publishing usable checks.
No compatibility alias, alternate scheduler or inferred area is introduced.

PreviewDock checks that at least one service and (for work/delivery) departure geometry is permitted, then selects a
permitted service/waiting slot through existing stable ordering/claims/vacancy/reach/cost checks.
Assigned actual source/destination slots are rechecked before execution. Policy waits retain the
existing job/node/receipts and stop actual motion. Future promotion and departure are gated through
the same checker. FIFO only considers arrived workers eligible for a currently available service bay.
Saved restriction is valid durable policy, not checkpoint corruption.
Pickup has no source-exit toil and does not invent one; its obsolete slot is ignored after pickup.
ServiceAvailable is shared by arrived FIFO and promotion, not duplicate selection rules.

Build2.114.0/save65/WorkBoard23/work system10/work view37/tile dock2/point dock3.
Existing owner payloads remain unchanged; descriptor versions identify the changed execution contract.

## 3. API

IWorkDock.CaptureSlot(World world,Code slot) returns immutable SpatialSnapshot in the actual endpoint
space, or faults invalid provider usage. WorkPolicyRules.CheckGeometry(World,Entity,SpatialSnapshot)
uses existing typed Disabled/OutsideArea/UnsupportedArea reasons. No new command or policy setting.

## 4. Files

Extend Standard/DockContracts.cs, WorkDocks.cs, WorkPolicies.cs, TileWorkDocks.cs, Work.cs/WorkModule.cs,
Kernel/SaveGame.cs; public ExtensionProof/PointDockProof.cs/PointSpaceProof.cs. Add two exact proof
suites plus master-listed authored region inputs. Update tracker/parent/evidence/README/AGENTS/help.
Prior identity assertions migrate mechanically and compile only, not rerun.

## 5. Errors

Excluded physical geometry returns OutsideArea before admission. Unsupported/missing region shape
returns UnsupportedArea. Invalid/null/foreign-space slot snapshots or mutating pure callbacks fault
explicitly. Busy allowed geometry retains existing full/occupied/departure-blocked reasons. Dynamic
exclusion pauses in place without resource loss, fallback parking, teleport or invented destination.

## 6. Compatibility

Development only; mandatory provider contract migration, no legacy reader or shim.

## 7. Verification

Build then only these two complete exact names; same-name repair/final reruns permitted:

- Dock operating areas: provider geometry, policy admission and checkpoint ownership
- Dock operating areas: physical slot restrictions and saved continuation

Prove native rectangular/painted and independent point regions, admission purity, live restrictions,
restricted waits/bays/exits, node/job/state/receipt ownership, physical timed continuation and save twins.
All physical loops have bounded failure diagnostics. No third/family/full run.

## 8. Risks

Endpoint footprint and physical slot checks are both mandatory. An allowed waiting slot is insufficient
without allowed eventual service/departure geometry. Removed area permissions can strand an existing
actor until an explicit player edit restores them; retaining its claims is intentional, not teleporting.
Pure guards refuse usable results but do not promise arbitrary provider/global mutation rollback.

## 9. Out of scope

Whole-route/vehicle-envelope operating-area containment, rotated/3D facilities, cross-facility overlap, collision/
congestion, preferred routes, vehicle service/repair/trip budgets, global provider/replay/stress/full
roadmap remain open. No Oil/Tycoon/host/demo/platform/Draw or artificial map cap changes.

## 10. Execution

- [x] Extend provider geometry and canonical policy checker; gate admission/live physical transitions.
- [x] Author two complete suites exercising native and public contract paths.
- [x] Build and run exactly the declared names; record current source evidence.
- [x] Align tracker and guidance; retain broader open gates.
