# P9-C8a — Shared physical activity and haul service docks

Status: focused implementation verified,2026-10-06. Full framework goal remains active/partial.
Clean build;153 checks/two exact suites, zero failures/aborts.
[Evidence](../../docs/migration/P9_SERVICE_DOCKS_VERIFICATION.md).

## 0. Problem

Before this stage, Standard/HaulDocks.cs and WorkDocks.cs restricted the physical slot coordinator to hauls.
Ordinary activity jobs approach endpoint centers and earn work there without a facility queue.
Adding a separate service-bay scheduler would duplicate existing claim/geometry/queue authority.

## 1. Goals

1. One contract/provider and WorkBoard coordinator serve ordinary positive-time machine activity
   and native loading/unloading, using actual service/waiting/departure geometry.
2. Existing jobs own all slot claims. Shared contention cannot double-book or preempt a paused bay.
3. An ordinary job earns no work while travelling, queued or departing; cancellation/reopen removes
   only its own claims. Actual physical occupancy still blocks a bay after releasing a claim.
4. Master JSON explicitly authors supported roles/resources; existing public point provider proves
   independent geometry and work-only facilities without invented ports.
5. Candidate previews/current assignment, private immutable job facts, checkpoint validation,
   normal Update/entity states and save/load use the same queue and actual provider.

## 2. Design

- Replace haul-only dock contract names with IWorkDockDefinition/IWorkDock, WorkDockSlot/Kind/Rules/
  Codes in Standard/DockContracts.cs. Remove HaulDocks.cs, no aliases or legacy interfaces.
- WorkDockRequest is an immutable discriminated request: explicit ordinary worker role or explicit
  native resource/loading-unloading phase. One CheckRequest signature; no parallel work/haul provider.
- WorkBoard.WorkDocks remains the only physical claim/fair promotion/departure owner. Existing
  SourceDockSlot/DestinationDockSlot fields stay on Job. Ordinary jobs use Target/DestinationDockSlot,
  never another queue/store/clock. Cross-kind arrived FIFO ties use original job age and stable ID.
- Candidate checks select actual current work slot/cost. Assignment rechecks before resource claims.
  Ordinary activity jobs append existing LeaveDock; machine is released/node completion occurs only
  after physical departure. Waiting/progress/departure checks use provider slots, not endpoint center.
- Generalize existing checkpoint/dock-removal/private waiting facts to all live docked jobs. Regular
  activity itinerary is GoTo→Work→LeaveDock only when actually docked, otherwise existing two toils.
  Open/reopened/ended jobs own no slot. Paused service remains exclusive; arrived assigned waiters
  alone compete for the free bay. No handling progress is invented for ordinary work.
- PointDockDef reads required work_roles and resources arrays, allows either/nonempty combined set,
  resolves actual role/resource catalogs and validates immutable geometry. Resource-serving facilities
  require inlet; work-only facilities do not fabricate inventory/ports. PointDock detects every actual
  worker/transporter occupant and accepts requests only through its explicit authored sets.
- Keep existing gameplay outcome/effect/resource/work-policy/readiness/area/motion owners. This stage
  supplies physical work facilities, not a new condition/repair/cadence/provider selection subsystem.

Build2.112.0/save65/WorkBoard23/work system9/work view36. Existing binary field layouts unchanged;
the job itinerary/claim semantics intentionally break by current identities. Point provider codec2.

## 3. API

IWorkDock.CheckRequest(World,Entity,WorkDockRequest); existing geometry methods unchanged.
WorkBoard shared slot methods use one request and one current job. Private WorkJobView continues to
project existing slot IDs/queue/blockers, never live provider handles or a service queue model.

## 4. Files

Rename Standard/HaulDocks.cs→DockContracts.cs; revise Standard/WorkDocks.cs,Work.cs,WorkScheduling.cs,
Logistics.cs,WorkModule.cs and Kernel/SaveGame.cs. Revise existing public PointDockProof/PointModule,
explicit point-dock JSON/literals and compile-only version/name callers. Add ServiceDockTests and
verification evidence; update stage/master/P9/README/AGENTS/help.

## 5. Errors

Invalid request/role/resource/phase/empty declarations/missing slot/reference refuse explicitly.
Full/occupied/waiting/departure-blocked remain typed reasons. Providers alias actual entity/definition;
pure callbacks cannot touch actual endpoint/worker facts. Corrupt duplicate/open/ended/missing/illegal
phase commitments, unearned-work departure and malformed activity itineraries cannot checkpoint. Participant loss cancels
native work; no teleport, compensated global rollback or default facility/geometry is introduced.

## 6. Compatibility

Development-only clean replacement. No old contracts/loaders or duplicate service dock implementation.

## 7. Verification

Build, then exactly these two complete named gates (same-name repair/final reruns only):

- Service docks: shared contracts, admission and checkpoint ownership
- Service docks: physical mixed queues, work and saved continuation

Prove strict work-only/mixed content, native haul↔activity contention, pure/private/frozen previews,
claims/corrupt phases, actual waiting/service/departure, paused occupancy/cancellation/participant loss,
no overlap or remote work, native resource conservation and saved/interleaved continuation.
All physical waits bounded with diagnostic timeout. Older suites migrate/compile only.

## 8. Risks

Ended service claims release before an actor's physical occupancy disappears; vacancy must inspect the
real geometry. Work progress may be complete while departure remains blocked, but node effect must not
commit repeatedly or release the machine early. Saved old claim semantics are intentionally rejected.
Generalized queue fairness must never count remote or paused waiters as arrived assigned waiters.

## 9. Open work

Vehicle-specific maintenance cadence/subject selection/repair effects, native tile facility geometry,
collision/path congestion/preferred routes/whole-trip fuel, wider provider purity/rollback/replay/
stress/allocations/full P9/P1–P12 remain separate unfinished requirements. No Oil/Tycoon/host/platform/
Draw changes, artificial world cap or full gate. This is a prerequisite, not full fleet completion.

## 10. Sequence

- [x] Replace sole dock contract and generalize existing queue/current-job protocol.
- [x] Integrate ordinary activity admission/itinerary/state/claims/checkpoints and graph blockers.
- [x] Revise explicit public JSON/provider and add two mixed-physical proof suites.
- [x] Build/run only the two names; record scoped evidence and align active tracker/guidance.
