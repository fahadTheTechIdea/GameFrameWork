# P9-C3a — Unified demand and route dispatch

Status: C3a focused-verified; broader P9 open. Parent: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Build2.57.0/save31/logistics4/logistics-view7: **120 checks / two exact suites** (70 admission;
50 physical), zero warnings/errors/failures/aborts; no broad/full run. Work/view13 and flow4 unchanged.
See [evidence](../../docs/migration/P9_LOGISTICS_DISPATCH_VERIFICATION.md). Earlier gates were not rerun.

Follow-up [C3b emergency supply](P9_EMERGENCY_HAUL_DESIGN.md) uses build2.58.0/save32/work14/
work-view14/logistics-view8, logistics4 unchanged. **82 / two exact suites**, zero warnings/errors/
failures/aborts; no broad/full run. Current owner classification and actual carrier eligibility now
precede ordinary priority; sole jobs preserve original requester identity and loaded physical
commitments. C3a's report above is historical, not rerun. Broader alerts/fleet/docks/plans gates open.

## Problem and goals

LogisticsService.Plan currently visits demand groups in insertion order, acquiring multiple carriers
before it considers routes. Job priority cannot repair this: a haul already owns its carrier. Replace
these passes, not wrap them. One existing LogisticsService exposes and executes the same detached
offers. Ready offers precede waits, then explicit priority, original creation tick, actual provider
travel cost and stable destination/resource/source/route identity. After each real acquisition,
rebuild offers against current stock, cargo, destination receipts and worker claims.

Routes gain required immutable enabled/batch/priority settings through AddRouteCommand and
ConfigureRouteCommand. The existing route record owns settings and original age. Delete fixed
RoutePriority, the old route Batch command field and old save layout; no compatibility reader.
Stock/source profiles remain existing explicit master-listed JSON. A mod-owned demand uses the same
declared source contract and queue; no second request registry or scheduler is introduced.

## Ownership and API

ILogisticsDispatchQueue.CopyQueue(viewer, destination) aliases LogisticsService; private LogisticsView
delegates to it. LogisticsDispatchOffer contains source intent, route ID, owner, destination, resource,
gap, priority, created/age ticks and HaulPreview (actual candidate, quantity, cost or typed wait).
These are immutable derived facts, never durable reservations or retained acquisition authority.
Merged demands use the highest-priority contributing enabled requester, then oldest/stable source;
quiet stock bands do not elevate another request. Combined native quantity stays in existing rows.
The preview uses the existing shared policy/native/worker admission and checked pickup-plus-delivery
provider cost. This is not an ETA; no renderer geometry or fabricated route duration enters the engine.

## Files and execution

1. Add LogisticsScheduling.cs (partial existing owner) and required LogisticsRouteSettings.
2. Replace PlanDemand/PlanRoute with unified ranked acquisition; retain existing evacuation and
   loaded-cargo return ownership until their authored policies join this queue.
3. Extend existing command/service/view codecs and current build/save identities together.
4. Add two exact suites: queue/admission/private/native/stable ordering, then actual contention,
   multi-carrier recomputation, routes, partial saves and interleaved isolation.
5. Record actual build/check counts and update master, README, AGENTS and parent in one handoff.

## Errors, durability and bounds

Invalid route owner/reference/native batch/priority/settings refuse before ID allocation. Disabled
routes retain age/settings and do not cancel already acquired cargo. Removed endpoints lapse through
the existing normal planning phase. Save/hash rejects duplicate/invalid/future-aged route records.
Demand loads retain the existing per-pass work bound; each real acquisition rebuilds current offers,
while blocked offers cannot spin or consume carriers. This is not a world/content size restriction.

## Verification and remaining scope

- [x] Pure queue ordering agrees with actual acquisition and hides foreign operational facts.
- [x] Explicit route edits preserve age, live receipts and saved physical continuation.
- [x] Contention, changed readiness and stable tie-breakers pass two exact bounded suites.
- [x] Build recorded; final scoped source/whitespace checks accompany evidence. No broad/full run.

Company alerts, emergency haul provenance/eligibility, starvation promotion, automatic preemption,
global work-versus-haul competition, evacuation/return policy ranking, timed docks, fleet policy,
persistent plans/templates, replay/stress/performance and full acceptance remain open. Oldest-first
within explicit priority is not a starvation guarantee. No host/demo/platform/Oil/Tycoon changes.
