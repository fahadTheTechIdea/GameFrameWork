# P9-C8a shared physical service docks — scoped verification

Date: 2026-10-06. Current dirty-workspace source, not a clean-HEAD/full-regression claim.
Build **beep-simengine-2.112.0**, save **65**, WorkBoard **23**, work system **9**, work view **36**,
public point dock codec **2**. Existing binary field layouts are unchanged; itinerary/claim semantics
break explicitly by these identities. No old contract alias, loader or compatibility path remains.
[Stage](../../plans/enhancements/P9_SERVICE_DOCKS_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Single engine authority

IWorkDockDefinition/IWorkDock and immutable WorkDockRequest replace the haul-only contract in
Standard/DockContracts.cs. A request names one ordinary worker role or one resource/loading-unloading
phase. Work-only facilities require no resource ports. Resource-serving point facilities require the
existing inlet; every outlet still has the inlet contract. No new endpoint/stock/receipt owner exists.

The existing partial WorkBoard.WorkDocks derives all slot claims from live Job.SourceDockSlot and
DestinationDockSlot. Ordinary jobs use actual Target/DestinationDockSlot; native hauls use their actual
source/destination. Service/waiting/departure, vacancy, arrived FIFO by original age/ID, paused exclusive
service and removal use the same claim pool. Providers own immutable geometry, not a queue or scheduler.

The sole worker candidate/assignment path previews/rechecks actual facility request, slot and cost
before AcquireResources. Ordinary positive-time activity jobs append the existing LeaveDock toil:
GoTo→Work→LeaveDock when actually docked, GoTo→Work when deliberately undocked. Work progresses only
at actual service. Full work retains the machine and node until physical departure; no remote/queued/
departure work or instant completion is granted. WorkSystem carries actual job blockers into the
activity graph and owning order. Entity.Update resolves the existing authored stopped state.

WorkBoard.Sync/load validation owns activity itinerary, live slot identity/exclusivity/phase and
earned-work-before-departure checks. Open/reopened/ended activities retain no slot. Current immutable
WorkJobView projects the same commitments/waiting facts; no parallel service view model was introduced.
Cancellation/ordinary interruption frees only the actual claims. Physical occupants still block bays
after claim release; another job may reuse its own current physical position, but other actors cannot
declare that position vacant. Removed targets retain normal next-Update order cleanup.

Public-only PointDockDef/PointDock extend the existing point geometry provider. Explicit master-listed
work_roles/resources arrays are required; either may be empty, never both. Slots/references/geometry
are validated, and repeated Read/Resolve is refused. Work-only content does not fabricate inventory.
Native and independent public workers use the same coordinator. Provider guards inspect existing
actual endpoint/worker versions and declared status/cost boundaries, not a universal mutation sandbox.

## Final verification commands and results

Working directory: C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Service docks: shared contracts, admission and checkpoint ownership"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Service docks: physical mixed queues, work and saved continuation"
```

- Final build: zero warnings/errors, **3.55 seconds**.
- Shared contracts/admission/checkpoint suite: **95 passed,0 failed**, **435 ms**.
- Physical mixed queues/work/saved suite: **58 passed,0 failed**, **454 ms**.
- Total **153 checks,0 failures,0 aborted suites**, exactly two distinct complete suite names.
- Earlier same-name repair runs exposed authored single-exit blockage, an obsolete no-departure
  assertion after adding exits, and incorrect assumptions about a reused actor/occupied waiting place.
  Fixtures now explicitly author departure capacity or move actual exit occupants with normal home
  commands. No runtime fallback parking, teleport or artificial vacancy was added. A test compilation
  error referenced a nonexistent CommandResult.Code property and was corrected before test execution.
  The graph-blocker and earned-work checkpoint assertions were added to these same two names, which
  were rebuilt/rerun completely. Their final counts above supersede the intermediate repair results.

45 preceding build assertions,19 Board-schema assertions and five work-system assertions in51 files
migrate/compile only. Older dock contract callers are mechanically migrated. No preceding, third,
family, broad, soak, replay, extraction, OilFieldDays or Tycoon test gate ran for this change.
Earlier evidence/fingerprints are historical and do not certify this current source.

## Covered behavior

- Explicit typed requests; default/None/undefined requests refuse. Required JSON arrays/slots, types,
  empty service declarations, catalog references, duplicate roles/resources/slot IDs/geometry, absent
  service/departure and geometry outside the authored space refuse without inferred defaults.
- Actual immutable public definition alias, repeated authoring refusal and pure ordinary candidate
  reads. A shared bay plus two waiting claims admits one haul and two ordinary nodes; full cross-kind
  refusal allocates no extra job/receipt/resource. Unsupported work roles retain typed refusal.
- Native/public workers share actual claims. Private frozen facts retain previous slot identities;
  another company reads no service jobs. Work-only removal is protected without inventory ports.
- Checkpoint/hash rejects mixed duplicate/missing/unknown commitments, waiter service-phase claims,
  malformed ordinary itineraries and departure with unearned work. Restored instrumentation saves.
- Public request/vacancy mutation, negative cost and undefined reach refuse previews before creating
  jobs. Endpoint mutation during Approach faults normal Update and prevents a faulted save.
- Work-only and deliberately undocked positive-time activities physically complete using the same
  engine; no service/inventory adapter or duplicate workflow is introduced.
- Mixed haul/ordinary waiters physically occupy authored locations, earn no queued work and report
  actual job/node/order blockers. Oldest arrived activity receives the physically vacated haul bay;
  the younger node cannot overlap/overtake it. Paused service remains exclusive and earns no time.
- Exact authored work duration, machine/node retention through departure, one exact native haul batch,
  source/destination/cargo conservation and terminal slot cleanup. Saved travelling/waiting/paused/
  completed-work exit phases continue identically in interleaved sessions.
- Normal cancellation preserves earned history, frees claims without inventing physical vacancy,
  and leaves a later native haul waiting for an actual occupant's home return. Participant-loss fault
  injection cleans up through the existing order owner. Single occupied departure retains the bay/
  machine/progress and reports a typed graph blocker until normal home orders really clear the exit.

All physical waits are bounded400 Updates with tick/job/toil/slot/blocker/location timeout diagnostics.
Initial entities and the explicit participant-loss probe use the actual engine scenario/lifecycle APIs.
Tests author content, submit player commands, advance Update and observe; movement, FIFO, reservations,
work timing, vacancy, home return, resource transfers and save state all execute in engine modules.

## Scope limits and remaining work

This closes shared physical activity/haul facility coordination, not vehicle-specific maintenance
cadence, service-subject selection or repair effects. Native tile facility geometry remains open;
the independent point provider proves the contract without embedding tile/platform assumptions.
Released actors may remain in finite authored departure positions until their real next job/home
movement clears them. The engine does not promise unconditional queue progress through occupied exits.

Collision along routes, cross-facility shared geometry, congestion/preferred routes/whole-trip budgets,
all area/role/emergency interactions, arbitrary provider exceptions/hidden mutation/global rollback,
replay/lockstep/fuzz/soak/allocation/full P9/P1–P12 acceptance remain unverified. Current provider checks
do not compensate arbitrary callback writes or detect every possible state mutation.

No duplicate scheduler/queue/stock/clock, legacy reader, OilFieldDays/Tycoon/host/demo/platform/Draw
change, artificial world cap or full gate. Full framework goal remains active/partial.
The lifecycle skill kept implementation, stage, master and source-specific verification aligned.

## Source fingerprint at final gate (SHA-256)

| File | SHA-256 |
|---|---|
| src/Beep.SimEngine/Standard/DockContracts.cs | 9E795595429B65C18B6291F8BC39871EF30CBD6BD0232E66380BB68D63152A13 |
| src/Beep.SimEngine/Standard/WorkDocks.cs | 7382DEF026A3B86812A2A727AC274C96D236A141E066F788B8783C51D04453DA |
| src/Beep.SimEngine/Standard/Work.cs | 73FDD40CF32599E203BFAD3258478D0E228A823906308645632B80B5057D08D5 |
| src/Beep.SimEngine/Standard/WorkScheduling.cs | A1157AB9C8629A0EFDACE1BAACDA68C3D3667E78F19B3C672C27E48F347C7ADA |
| src/Beep.SimEngine/Standard/Logistics.cs | 4E333588EA51DC828B0466D314606B21DA811FDBB2DDAE3296A818AA1F30BE3A |
| src/Beep.SimEngine/Standard/WorkModule.cs | 032E2F5901588E95A9226DA698410FB6B587DB8862C067C679691CFDCF8B4A80 |
| src/Beep.SimEngine/Kernel/SaveGame.cs | 2B6129665B4454A6DB6B74958F6565C4831FDF65002FF334E60872772D444CE3 |
| tests/ExtensionProof/PointDockProof.cs | 91DC8C83343475BEF396D4794D12FBF41B4DEDA1019317BCCD85E8F6EE70A821 |
| tests/ExtensionProof/PointSpaceProof.cs | 6488AA032DC8D535E19839D5AD7C85964833EA9DC2B8EAFC23D0F93980456FCE |
| tests/ExtensionProof/ContractCapabilities.cs | AC19A987114349A9A2CB9C7877602F36088F1DB9FE422B151B06AF1162F0B30B |
| tests/ExtensionProof/content/point-docks.json | 4FC66433334D9C68DB5ED1B9A9F5190140BD225F04E7ADE030F652B42F3F572A |
| tests/Beep.SimEngine.Tests/ServiceDockTests.cs | 9566D3827E0E9E5ED7CC4C424F8B71B0B853702AA0DBDA8BA3F92B8227266794 |
| tests/Beep.SimEngine.Tests/Program.cs | 8158C6FEE52A8D14F46BFD304AAE5D72FBF725B8F0566441A733FF633A320FFC |
