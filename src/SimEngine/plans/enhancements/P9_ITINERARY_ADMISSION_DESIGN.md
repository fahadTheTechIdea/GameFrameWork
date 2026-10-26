# P9-C8e — Operating-area itinerary admission

Status: focused implementation verified,2026-10-06. Parent: P9. Prerequisite: C8d whole-route execution.
Clean build;259 checks/two exact suites (84 admission;175 physical), zero failures/aborts.
[Evidence](../../docs/migration/P9_ITINERARY_ADMISSION_VERIFICATION.md).

Follow-on [C8f committed dock replanning](P9_DOCK_REPLANNING_DESIGN.md) now extends the same selector
and physical owners. Its197/two exact suites are current scoped evidence; the preceding C8e suites
were not rerun. Structural replanning is not a future traffic/fuel/road reservation guarantee.

## 0. Problem

LogisticsService.Preview chooses source/destination slots from the worker's current position, but
does not prove the future loading-to-delivery leg before reserving stock/capacity. WorkBoard.PreviewDock
checks permitted slot geometry, not the complete approach/service/departure chain. Disconnected
painted areas can therefore acquire an impossible physical job.

## 1. Success

1. Actual motion providers answer pure prospective route queries from detached origin to exact slot
   or interaction geometry, returning their real reachable arrival geometry, never another live path.
2. Existing WorkBoard proves selected waiting/service/departure chains and future haul legs before
   the existing resource/job allocation. Alternate authored slots remain usable in stable order.
3. Current previews and actual haul acquisition share that admission; haul refusal leaves identities,
   jobs, routes, coordinates, stock and claims unchanged. Ordinary activity intent can remain open,
   but its existing Assign checks itinerary before resource acquisition. Active edits retain C8d waits.
4. Future promotion/departure reuses the same feasibility rules, not a persisted speculative route.
5. Native tile and public point proofs exercise physical timed work/hauling and interrupted saves.

## 2. Contracts and owners

Extend actual IMotionAreaConstraint with CheckRoute(World,ISpatialRegion,SpatialSnapshot origin,
MotionDestination destination). MotionDestination explicitly distinguishes Exact and Interaction;
MotionRouteCheck is detached refusal or proven arrival. MotionAreaContracts alone guards callback
identity/purity/shape/status/results. WorkPolicyRules alone resolves authored policy regions.

Native Mover queries the existing NavGrid/PathSearch. The one A* reports its reached tile without
allocating a second route. TileMap alone calculates ordinary interaction bounds, shared with actual
approach. Structural prospective routing ignores movable traffic, not static obstacles/terrain or
area exclusions. It does not fabricate current dock vacancy: existing claims/vacancy/queue checks
remain mandatory. Actual Advance still respects all current occupants and physical waiting.
Public point providers own their geometry/access and interval sweep proof through the same contract.

WorkBoard extends its existing dock preview to accept an explicit prospective origin and return an
actual final service/departure arrival. It traverses only finite authored slot alternatives, using
stable slot ordering. LogisticsService chains source arrival into destination admission before any
existing promise acquisition. Ordinary work candidates and actual promotion/departure share this
same owner. No route/slot plan is persisted; current state is freshly checked at commit/execution.

## 3. Errors and durability

Use existing typed Outside/Unsupported/Pending route-area waits and dock busy/compatibility reasons.
Unknown/mutating/null/foreign/contradictory provider results fault rather than grant admission.
No policy means this feature is absent, not an invented area. No legacy loader or compatibility shim.
Existing policy/job/motion codecs remain sole state owners; change descriptor/build identity for the
new semantics. Reservations still commit atomically through their existing owners after fresh preview.
Build2.120.0/save67/mover3/movement5/work system12/logistics service8/work view38/logistics view13;
motion view4 and owner payload layouts unchanged.54 earlier assertion files/65 identity lines migrate
and compile only, not rerun. Existing route proof helpers are reused for explicit inputs/observations;
they implement no gameplay and their preceding two suites were not run.

## 4. Verification

Build, then only these two exact complete names; same-name repairs allowed:

- Itinerary admission: prospective provider routes and atomic private previews
- Itinerary admission: permitted alternatives, physical queues and saved execution

Explicit master-listed JSON and public-only engine provider contracts. Bounded physical loops with
failure snapshots. Cover disconnected future legs/slots, alternative service/departure selection,
stationary/absent contracts, invalid pure results, refusal hash/allocator/native receipt retention,
normal work/loaded hauling, physical queues, policy edits, saved twins and isolated sessions.
Earlier suites compile only; no third/family/full run.

## 5. Boundaries

This proves current structural/policy route feasibility, not future traffic vacancy, fuel availability,
reserved road access, travel deadlines, committed-slot retargeting, arbitrary directed-provider global optimality or global
mutation rollback. Vehicle hulls/3D/editable areas, fleet/congestion/preferred routes, replay/lockstep/
stress/allocations/full framework remain open. No Oil/Tycoon/host/demo/platform/Draw/size cap changes.

## 6. Execution

- [x] Extend pure prospective motion contract and existing native/public routing owners.
- [x] Integrate existing dock/candidate/haul/promotion/departure admission.
- [x] Add two exact suites; build and verify those names only.
- [x] Align master/parents/guidance and record current source evidence.
