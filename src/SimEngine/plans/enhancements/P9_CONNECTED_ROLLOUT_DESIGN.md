# P9-PBH1.R — Authored launch geometry and connected mobile rollout

Status: PARTIAL on2.650.0/save160/replay1. Engine launch/egress algorithm and
selected framework fixtures are verified. Actual consumer layouts are authored;
[current69-pass focused checks and failed harbour continuation](../../docs/migration/P9_OIL_PRODUCER_LAYOUTS_VERIFICATION.md)
leave full canonical/performance acceptance unfinished. Ten earlier focused literals2001/0 across the SAME
core, with artifact groups and earlier failures retained in
[verification](../../docs/migration/P9_CONNECTED_ROLLOUT_VERIFICATION.md).
Parent [P9-PBH1](P9_PRODUCER_BERTH_HOME_DESIGN.md)
and original P9.acceptance.051 remain OPEN; original audit221/209 recorded checked/12 open.

## 0. Problem and specification

TileMap.FindRollout currently scans expanding rings across the whole authored
world. It can create a movable result on a free but disconnected cell after the
producer's only contact becomes occupied. CheckRolloutPlacement tests occupancy
and planned overlap but not a launch/exit route. This violates physical production
and hid the canonical marine berth obstruction.

## 1. Goals

1. Reproduce remote/disconnected birth with ordinary JSON production, native
   previews and existing rollout queries; keep real WIP and timings.
2. Mobile output uses explicit launch/exit IDs in the producer's existing dock
   geometry, not world-wide fallback or a second coordinate/layout registry.
3. Existing NavGrid/PathSearch proves actual movement-class connectivity and
   occupancy/corner clearance, including prospective unavailable footprints,
   without spawning a temporary worker or adding a route owner.
4. Missing/invalid bindings fail before Initialize. Dynamic obstruction or search
   budget exhaustion returns a typed blocker, retains WIP and publishes no result.
5. Native tile and independent point providers use the same generic rollout
   request/definition protocol and shared launch metadata, with save/twin/replay
   and pure live/prospective parity checks. Static placement remains distinct from
   mobile egress; no generic kernel rule assumes tile geometry.

## 2. Design and owners

Optional IWorkDockRolloutDefinition extends the SAME actual IWorkDockDefinition
with immutable Rollouts. Service-only/negative providers need no fake launch
capability. Each authored lane has
stable ID, actual output prototype identities, start slot IDs, exit slot IDs and
positive explicit expansion budget. Empty/absent rollouts selects no production
launch capability; it is not a fallback policy. Existing Slots alone own relative
geometry. Add Rollout slot kind for launch-only facilities; a service slot may
also be explicitly selected as a launch start. Exits reference Departure slots.
Parking never substitutes for a start/exit, and no launch slot matches work/haul
service requests. Launch-only facilities need no fabricated resource/work policy.

Native TileWorkDockDef exposes its existing TryTile through a typed definition
contract. TileMap resolves the actual frozen layout and selected lane, validates
the requested candidate against start geometry and checks a route to an authored
exit. Find and Check use the same admission. All live/prospective prefix overlap,
actual native placement and provider/space/clock observation remain in the
original EntityRollout boundary. No second layout, pathfinder, traffic claim,
reservation, queue, state machine or clock is introduced.

NavGrid gains prospective-definition route admission through its ORIGINAL search,
terrain/occupancy/clearance and origin-constraint owners. It includes the output
footprint and every unavailable planned footprint. Budget exhaustion is pending,
not a successful launch or a permanent unreachable claim. Birth creates no
reserved future path; actual later motion still obeys live occupancy and timing.

Independent point rollout interprets the same lane/slot metadata with its own
geometry and traversal rules. Kernel EntityRollout remains geometry-neutral.
Stationary placement has no mobile egress requirement; ordinary occupied-prefix
and complete-footprint checks still apply.

## 3. JSON / API

Existing dock JSON adds, for example:
`rollouts:[{id:"workers",outputs:["worker"],starts:["launch"],exits:["exit"],max_expanded:1024}]`.
The geometry remains in existing slots, e.g. kind Rollout for launch and kind
Departure for exit. IDs must be unique/resolved, starts and exits distinct and
nonempty, outputs actual mobile prototypes in the same geometry. Existing dock
service admission remains independent of launch policy.

## 4. Files

Kernel/SpatialPlacement.cs (protocol documentation / typed pending code),
Standard/DockContracts.cs and shared launch declaration, TileWorkDocks.cs,
Navigation.cs, native Map/TileMap.cs, independent PointDock/PointSpace proof,
affected explicit producer JSON, exact focused tests, build identity and docs.
No renderer, adapter, platform or host game logic.

## 5. Errors

Missing launch/invalid prototype/slot references/incompatible geometry: content
refusal before Initialize. Occupied birth/exit or disconnected route: Blocked;
budget exhausted: rollout Pending. No healthy refusal may consume WIP, grant
stock, relocate occupants, allocate result IDs or publish a partial result.
Callback faults retain the original terminal-fault behavior.

## 6. Compatibility

Development only. Remove mobile ring fallback; no old reader or compatibility
mode. Explicitly migrate existing authored producers. Older native checkpoints
remain historical and are never header-rewritten.

## 7. Verification

First failing disconnected/occupied-birth exact case, then physical resume,
planned-prefix/corner/clearance, independent geometry, malformed unused content,
saved WIP/twins/replay and affected current producer integration literals. Only
focused separate bounded cases; no full/profile/family run.

## 8. Risks

Service and launch can share a real location but never bypass actual occupancy.
Do not reserve/copy future paths, ignore planned static geometry, manufacture a
temporary worker, silently select an unlisted result or treat budget failure as
reachable. Preserve endpoint and content ownership, original job claims and time.

## 9. Out of scope

Host steering, refunds/grants, changed recipe amounts/times, easier canonical
geography, new transport media, Tycoon/business changes and publication.

## 10. Execution order

1. [x] Audit native/live/prospective rollout, dock metadata and original routing.
2. [x] Reproduce a free remote-cell birth while launch access is obstructed.
3. [x] Implement shared authored lane admission and original-owner connectivity;
   migrate current producer declarations without fallback.
4. [x] Focused durability/provider/refusal checks and current artifact identities.
5. [ ] Update parent/master/evidence; earn canonical physical acceptance only
   after actual berth/parking bindings and normal full-duration execution.

Step3 engine work and actual four-producer JSON authoring are implemented.
Canonical Frontier, maximum11-machine Sandbox saved physical continuation and
marine preparation pass; one native harbour continuation times out. Full actual
service/repair/land/sea execution remains unproved. Any fixture deliberately
rewiring a mobile output must explicitly rebind the lane's output identity too.
There is no ring fallback to mask missing declarations. Step5 documentation is
current, but canonical acceptance and wider corner/clearance/fault/performance
proof remain open. No full/profile/family run or publication.
