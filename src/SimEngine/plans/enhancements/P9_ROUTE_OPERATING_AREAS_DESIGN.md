# P9-C8d — Whole-route operating areas

Status: focused implementation verified,2026-10-06. Parent: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Clean build;280 checks/two exact suites, zero failures/aborts.
[Evidence](../../docs/migration/P9_ROUTE_OPERATING_AREAS_VERIFICATION.md).
Prerequisites: physical dock areas and sole native motion checkpoints.
Follow-on [A1b](P9_EDITABLE_OPERATING_AREAS_DESIGN.md) now migrates runtime policy region resolution
to one company-owned editable authority and reruns these same two complete suites with its added
creation/edit/private/physical/save checks:429 total, not280 plus429. The authoring-only resolution
description below is historical; shared containment/travel owners remain authoritative.
Follow-on [C8e](P9_ITINERARY_ADMISSION_DESIGN.md) now adds structural/policy itinerary admission before
actual haul claims/jobs. The historical two C8d suites below were not rerun; future traffic/fuel/road
guarantees and committed-slot retargeting remain open.

## Ownership and scope

The existing WorkPolicy settings select one master-listed immutable region. A new optional
ISpatialTravelRegion proves swept segments; checking just two endpoints never proves containment.
IMotionAreaConstraint must alias the actual IMotion and publishes pure current-route admission.
Missing provider/shape support returns an explicit blocker, not unrestricted travel.
WorkPolicyRules resolves automatic-motion policy once; direct Held player movement remains explicit
manual control. Native Mover and public point proof providers retain their own sole route/position
owners. Native A* accepts an edge constraint, not another search or persistent route.

## Execution

Check containment before operating-input consumption and Advance. A restricted current native path
reopens the same pathfinding owner; an available detour may be longer but cannot leave the area.
Every traversed segment is checked, including fractional current position and diagonal corner cells.
Tile regions conservatively require the complete union rectangle of adjacent unit cells; this is
cell coverage, not a physical vehicle hull. Continuous point regions prove their own interval sweep.
Blocked active work retains its intent, job, earned work, cargo, receipts and claims. Policy edits and
map changes may reopen it; no teleport, free resources, cancellation or instant fallback.
Current-route waits belong to CheckJob/actual approach, not proposed task admission: an unrelated
blocked home trip cannot starve a valid local activity. Newly started routes publish job/node refusal
in the same work phase. Active route-area waits retain motion intent; endpoint/slot policy waits still
stop it through the existing physical-slot rules.
Motion execution/arrival private views expose the current area ID and typed refusal. No new codec,
timer, job store, commands or mutable area copy. Existing saves hash actual settings and motion state.
Build2.119.0/save67/native mover3/movement system5/work system11/motion view4. Actual owner payload
layouts remain unchanged; native GoTo/Stop and public point intents now touch actual entity versions.

## Verification

Build, then only these two exact complete names (same-name repair runs allowed):

- Route areas: provider contracts, admission and private diagnostics
- Route areas: physical constrained travel, cargo and saved continuation

Fixtures supply explicit master-listed JSON, ordinary player commands, bounded Update and observations.
Cover swept versus endpoint containment, native rectangular/painted detours, public point providers,
unsupported/mutating callbacks, private immutable diagnostics, manual intent, policy edits, physical
hauling, conservation, interrupted saves and interleaved twins. No third/family/full test run.

## Open boundaries

Full itinerary pre-reservation route feasibility, vehicle hulls, wider editable-region/layer providers, congestion,
preferred routes, arbitrary provider global rollback, replay/lockstep/soak/allocation gates and full
P1–P12 remain open. No Oil/Tycoon/host/demo/platform/Draw/world-size changes or compatibility reader.

## Checklist

- [x] Implement contracts, shared policy gates and provider-owned constrained travel.
- [x] Author the two exact suites and JSON inputs.
- [x] Build and record exact verification evidence.
- [x] Align tracker, parent and guidance without closing broader gates.
