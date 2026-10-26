# P9-C3c — One machine arbitration pass for work and logistics

Status: C3c focused-verified; broader P9 open. Parent [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Build2.59.0/save32/work14/work-view15/logistics4/logistics-view8: **130 checks / two exact suites**
(76 admission; 54 physical), zero warnings/errors/failures/aborts. No broad/full gate or prior-suite
rerun. [Evidence](../../docs/migration/P9_SHARED_MACHINE_DISPATCH_VERIFICATION.md).

## Problem and goals

WorkSystem.Update/Assign currently claims open physical work at system order 400. LogisticsSystem
independently calls LogisticsService.DispatchQueue at 450. Each queue respects emergency/priority/age/
cost internally, but an ordinary work job can claim the only multi-role carrier before urgent supply.
This is an engine phase-order shortcut, not player policy. Remove separate standing-haul dispatch.

1. WorkSystem alone arbitrates current work, demand and route offers before idle home/cargo behavior.
2. Existing providers retain candidate selection, admission and real acquisition; no new jobs, requests,
   inventory, reservations, incident bindings, saved queue or copied priorities are introduced.
3. Public IWorkerDispatchSource admits engine-side modules through existing declared service discovery.
   Current offers carry stable scoped identities and exact detached ranking facts, never authority.
4. Readiness, emergency, explicit priority, original age, current provider cost and stable identities
   use one comparison. Rebuild after each actual acquisition/cancellation so two offers cannot claim
   the same machine or stale source/capacity promises.
5. Existing workload budgets count attempts by original intent identity within one pass, not world-size
   limits. Source schedules wake the shared pass; once awake, all participating sources compete.

## Design and ownership

Extend WorkContracts.cs with immutable WorkerDispatchOffer and IWorkerDispatchSource (session/scope,
schedule, pure CopyOffers, PrepareDispatch, rechecking TryAcquire). ServiceContainer.Providers remains
the sole declaration/discovery authority. WorkSystem validates session, unique scope and well-formed
offers before use; aliases deduplicate. No source registry or source-kind enum is added.

WorkScheduling.cs derives merged offers from the existing work projection and declared source
projections. IWorkDispatchQueue gains CopyAssignments, exposed through private WorkView. CopyQueue
remains specifically open work-job diagnostics, not a second assignment path. Native queues delegate
ranking to the common comparison. WorkSystem.Assign becomes the shared pass; its previous job-specific
body becomes one rechecking TryAssign operation. WorkBoard Dirty/assignment fallback and source
schedules wake the same pass after physical advancement and order demand posting.

LogisticsService implements the source over its existing demand/route queue, using stable destination/
resource and route identities. PrepareDispatch prunes expired intent and refreshes the same stock
latches. TryAcquire resolves a fresh native offer then calls existing atomic acquisition. Delete
DispatchQueue. LogisticsSystem becomes LogisticsMaintenanceSystem: only existing evacuation cleanup/
dispatch and post-load validation remain at 450. Evacuation/return policy arbitration is explicitly
not closed by this slice; they remain native physical behaviors, never merged falsely as authored
request identities. Loading/travel/unloading and acquired cargo remain WorkSystem/IHaulExecution-owned.

Build identity migrates for changed scheduling; no old save reader. Work/logistics state codecs retain
their existing layouts; WorkView version changes for the new immutable assignment projection.

## Content and proof

Add master-listed shared-dispatch.json containing explicit multi-role worker policy and positive-duration
ordinary activities. A public-only proof module contributes a JSON-authored shipment through the new
source contract and normal IHaulExecution, with no private job store or custom movement/transfer loop.
Only two exact new suites run: admission/private ranking/provider composition and real contention/
saved continuation. Existing directly changed assertions compile, but are not rerun or counted.

## Errors, limits and acceptance

- Foreign session, invalid/duplicate scope or duplicate offer identity faults before selection.
- Invalid owner/worker/target/age/cost/attempt/ranking facts are explicit provider violations.
- Fresh native refusal cannot spend, fabricate stock or commandeer a worker.
- Acquired shipments remain real jobs; removal/retirement does not erase claims or cargo.
- All physical waits are bounded and report current job/state/blocker snapshots.

- [x] Ready emergency supply beats maximum-priority ordinary work on one multi-role machine.
- [x] Emergency work beats ordinary hauling; ordinary cross-kind priority/age/cost/ties are deterministic.
- [x] Recomputed contention, unavailable offers, pure private views and JSON source extension pass.
- [x] Timed delivery/work, stale intent refusal/retired loaded requester, save/load twins and interleaved sessions preserve actual owners.
- [x] Clean build/two exact suites recorded; final scoped checks accompany evidence and master/README/AGENTS/P9/P4 handoff.

Company alerts, starvation promotion, preemption, evacuation/return ranking, fleet/docks/areas/plans,
provider fault matrices/replay/stress/performance and full framework acceptance remain open. No
Oil/Tycoon/hosts/demos/platform/Draw or compatibility implementation is included.
