# P9-C4b — Physical haul dock queues

Status: focused implementation verified, 2026-10-05. Parent: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Historical stage: its haul-only API names were removed by [C8a shared service docks](P9_SERVICE_DOCKS_DESIGN.md).
Use C8a for current contracts; this document records the earlier slice, not a compatibility path.
Build2.62.0/save34/work16/work-view18; transporter2/logistics4/view8/flow4 unchanged.
**108 checks / two exact suites**, zero warnings/errors/failures/aborts; no broad/full gate.
[Evidence](../../docs/migration/P9_DOCK_QUEUES_VERIFICATION.md).

## 0. Problem statement

`WorkSystem.RunJob` approaches an endpoint entity before timed load/unload. Multiple carriers can
therefore service the same endpoint concurrently without authored capacity or distinct waiting places.
The existing `Job`, `WorkBoard.Sync`, native receipts and `IHaulExecution` must remain the only owners
of physical shipment commitments. A new independent queue/scheduler would duplicate that authority.

## 1. Goals

1. Optional `IHaulDock` capabilities describe stable service/waiting slots and pure geometry checks.
2. The current haul admission checks both endpoints' slots before allocating jobs or native receipts.
3. Each existing job saves its selected source/destination slot; no separate queue ledger exists.
4. Waiting carriers physically approach their assigned location and accumulate no handling time.
5. Oldest arrived waiter (original job age, stable ID) advances only to an unclaimed, physically vacant
   service slot. Occupied/paused service is not preempted. Departure, cancellation and loss free only
   the job's claims; actual occupied geometry remains a provider check, not a fabricated vacancy.
   Explicit departure locations and a final `LeaveDock` toil move empty carriers out of service before
   the same job finishes. No fabricated home/parking location or instantaneous departure is allowed.
6. Private immutable job facts, checkpoints and saved twin continuation share the same rules.

## 2. Design

`Standard/HaulDocks.cs` defines `HaulDockSlot`, `IHaulDockDefinition`, open engine-side `IHaulDock`
and typed blockers. `HaulDockRules.ValidateDefinition(EntityDef)` is the sole common validator;
the runtime provider aliases the actual registered immutable slot list, not another content copy.
Providers author immutable stable IDs/kinds through their registered JSON component definitions and
own geometry, physical vacancy, compatible resources/carriers, navigation/cost and approach. Approach
requests existing motion only; it cannot teleport or own job/stock/progress. The framework neither
assumes tiles nor invents marker entities, positions, slot counts or service capacity for undocked
endpoints. Undocked endpoints deliberately retain ordinary entity interaction as a distinct capability.

`Standard/WorkDocks.cs` is another file of the same partial `WorkBoard`. It derives occupancy/queue
order from current `Job.SourceDockSlot`/`DestinationDockSlot` fields. Admission prefers unclaimed free
service then waiting slots in ordinal identity order. Both selections precede mutations. A loaded
carrier retains its pickup slot until it physically leaves it, preventing service overlap during
departure. Completion/cancellation releases its relations; subsequent admission still checks real
provider occupancy. Removed endpoints retain normal next-update native-claim cleanup semantics.

`LogisticsService.Preview/Acquire` use these selections; `CheckPhysicalContext` refuses commits outside
a reserved service slot. `WorkSystem.RunJob` delegates haul approach/reach to WorkBoard, advances only
actual service, and leaves other activity travel unchanged. Existing handling, cargo return and native
batch stock transactions remain authoritative. A waiting blocker remains an assigned physical job,
not a second queue or paused policy state. Entity.Update resolves authored stopped state while waiting.

`WorkBoard.Sync` adds only two stable slot IDs; the existing itinerary saves the final LeaveDock toil.
It validates live endpoint bindings, exclusivity and
phase/progress on writes and after complete restore. Build/save/work/view identities migrate together,
without old readers. Providers have no additional queue codec.

## 3. API surface

- `IHaulDockDefinition : IComponentDefinition`: immutable authored slots, common pre-tick validation.
- `IHaulDock : IEntityCapability, IEntityRemovalGuard`: immutable slots; pure request, slot reach, vacancy and cost checks;
  motion-only approach. No public mutable queue/store.
- `HaulPreview`: detached selected source/destination slot IDs.
- `WorkJobView`: selected slot IDs, current waiting fact and typed blocker.
- Public-only `ExtensionProof/PointDockProof.cs`: registered JSON point dock component, existing
  PointPresence/PointMotion geometry and actual carrier occupancy. This is an engine-side module,
  not gameplay implemented by a test or host.

## 4. Files to change

New dock contract/rule file, WorkBoard partial, point provider/explicit fixture and exact dock tests.
Extend Logistics.cs, Work.cs, WorkView.cs, WorkModule.cs, SaveGame.cs, proof registrations/resources,
Program.cs. Update master/P9/P4/README/AGENTS and one stage evidence report at handoff.

## 5. Error handling

Missing/duplicate/empty/undefined slots or invalid JSON geometry refuse content. Full/occupied docks
refuse admission before IDs/receipts. Waiting reports `haul.dock-waiting`; physically blocked promotion
does not teleport or spend time. Unreachable routes use existing physical haul failure/cargo recovery.
Invalid provider identity/result faults, not an instant service fallback. Corrupt saved slot identity,
duplicate ownership, or waiting service progress refuses checkpoints/restore.

## 6. Compatibility

Development only. Current schema only; no legacy reader, shim or default authored dock.

## 7. Verification

Build the solution, then only these two exact directly affected suites:

```powershell
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Haul docks: authored admission, atomic commitments and checkpoint guards"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Haul docks: physical waiting, fair service and saved continuation"
```

Bound every physical stage with a timeout and job/position diagnostics. Compile older suites only;
do not claim their prior counts as a current regression gate.

## 8. Risks

Physically occupied released slots: provider vacancy checks prevent overlap. Both-endpoint commitment
is intentionally conservative and bounded by actual authored waiting locations, not a global world
limit. Provider path congestion/collision breadth and batching optimization need later stress gates.

## 9. Out of scope

General repair/fueling dock services, route-wide collision avoidance, fleets, editable areas,
incremental subquantity stock receipts, starvation promotion, plans/templates, broad/full regression,
Oil/Tycoon/platform/Draw. This stage is haul dock service, not completion of all P9.

## 10. Execution order

- [x] C4b.1 contract and existing-job admission/codec.
- [x] C4b.2 physical waiting/departure/promotion/current service gate.
- [x] C4b.3 public-only authored provider and two bounded exact suites.
- [x] C4b.4 scoped verification evidence and linked tracker/instruction updates.

Broader P9/framework gates remain unchecked in the master; this is scoped proof, not full completion.
