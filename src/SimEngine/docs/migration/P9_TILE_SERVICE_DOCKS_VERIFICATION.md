# P9-C8b native tile service facilities — scoped verification

Date: 2026-10-06. Current dirty-workspace source, not a clean-HEAD/full-regression claim.
Build **beep-simengine-2.113.0**, save **65**, WorkBoard **23**, work system **9**, work view **36**,
new native tile dock codec **1**. Existing owner payloads/schemas unchanged; no old reader or alias.
[Stage](../../plans/enhancements/P9_TILE_SERVICE_DOCKS_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Existing authority, native geometry

TileWorkDockDef/TileWorkDock implement IWorkDockDefinition/IWorkDock through WorkModule's explicit
tile_work_dock registration. JSON requires work_roles/resources arrays and slots[{id,kind,x,y}].
Unique immutable slot identities/positions, real catalog references, service/departure, stationary
tile presence and resource-service inlet requirements validate before gameplay. Repeated Read/Resolve
is refused. Work-only facilities require no ports. Slots cannot overlap the facility's own blocking
footprint, and their offsets must be capable of fitting the actual authored space.

The native provider owns geometry only. Actual ITilePresence supplies coordinates/footprints;
IMobility supplies CanReach/Goal/GoTo. Native cost uses the existing tile metric for the real slot,
not building center. Existing movement handles paths, current terrain/occupancy and elapsed time.
Identical in-flight goals are preserved. Vacancy inspects blocking footprints and every actual worker/
transporter presence, including nonblocking machines. No marker entity, route/goal copy, queue,
occupancy ledger, progress, stock balance or service clock was introduced.

WorkBoard/Job still owns cross-kind claims, arrived FIFO, admission, pause, service and departure.
Native hauling keeps the existing timed handling/receipts/cargo transaction owner. Ordinary activity
keeps its existing graph/positive work duration/effects owner. Private WorkJobView and node/order
blockers report the same actual physical commitments; hosts calculate no facility gameplay.

## Canonical placement and checkpoint boundaries

ITilePlacementConstraint is an optional multi-part definition hook, not another singular component
capability or registry. TileMap.CheckPlacement invokes such definitions in existing component order
after normal footprint/space validation and before allocation/occupancy commit. The native constraint
uses wide relative-coordinate sums, complete authored slot bounds and static blocking geometry;
mobile occupants are not permanent placement reservations. Normal live vacancy still governs them.
Native spawn, construction/replacement/rollout call that same existing placement preflight. This
slice directly proves spawn/placement/replacement checks, not every construction/template workflow.

Placement callback guards observe existing map version/tick/entity sequence and replacement identity/
version/owner. A public probe proves native and independent constraints compose; touching the actual
replacement refuses usable admission. Hidden global/service/coordinate mutations are not all guarded.

IEntityCheckpointValidation is a pure multi-part lifecycle hook invoked by Entity.Sync after every
component payload has finished decoding/writing. It adds no payload or gameplay update loop. Native
TileWorkDock implements this and existing IEntityInitialization, checking actual placed/stationary/
space/footprint/slot bounds. Presence remains the sole coordinate codec. This avoids assuming that
tile_presence appears before tile_work_dock in JSON. Entity version/owner/state/since guards refuse
mutating validators. A public probe before presence proves complete decode and faults actual Touch
mutation before a usable save can return. It is not a global rollback/sandbox guarantee.

## Final commands and results

Working directory: C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Tile service docks: authored geometry, placement and shared admission"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Tile service docks: native physical queues and saved continuation"
```

- Final build: zero warnings/errors, **3.44 seconds**.
- Authored geometry/placement/shared admission: **92 passed,0 failed**, **648 ms**.
- Native physical queues/saved continuation: **38 passed,0 failed**, **576 ms**.
- Total **130 checks,0 failures,0 aborted suites**, exactly two distinct complete suite names.
- First admission run stopped because test initialization published real initial-stock events before
  hashing. The fixture now finishes an actual Update before checkpointing; pending-event refusal was
  not weakened. Same-name expansions added public multi-part callback proof, footprint and absent-
  argument checkpoint guards, followed by complete rebuild/reruns of those same two suite names.
- 46 preceding build assertion files migrate2.112.0→2.113.0 and compile only. Their suites, the
  preceding shared dock gates and all other historical results were not rerun for this change.
- No third/family/broad/full/soak/replay/extraction/OilFieldDays/Tycoon gate was run.

## Proven behavior

- Required arrays/slot coordinates/types, exact integer offsets, empty declarations/slots, duplicates,
  unknown slot kinds/catalog references, moving/absent presence, missing resource inlet, own blocking
  footprint and impossible authored offset extent refuse explicit content.
- Native runtime aliases immutable JSON definitions; role/slot lists and repeated reauthoring/rebinding
  cannot mutate actual declarations. Default/unsupported-role/cross-session requests refuse.
- Canonical placement includes negative and far departure offsets beyond the building footprint.
  Out-of-bounds or static blocked slots refuse before entity IDs, initial stock or map mutation.
  Current replacement ignores the actual replaced building only; pure callbacks compose in the same
  selected geometry owner. Missing public placement identity refuses explicitly.
- The actual source JSON authors dock before presence. Current save/load succeeds after complete
  component decode. Corrupted actual origin/footprint bounds cannot save/hash even when all negative
  slot offsets would individually fit. Restored instrumentation returns to legal checkpoints.
- One native hauler and two ordinary activity nodes own the same bay/waiting pool. Full mixed capacity
  refuses another haul without allocating a job/receipt. Foreign private views leak no commitments;
  native removal protects actual live mixed claims.
- Native work-only service has no invented inlet/outlet/storage and completes real positive-time work.
  Nonblocking actual worker presence blocks the bay even when the static map occupancy table is empty;
  hauling selects a real free waiting tile instead of concealing that actor.
- An unused authored2147483647-wide tile space accepts its native facility definition. Relative sums
  near that boundary produce exact valid/refused geometry without wrapping or an artificial platform
  cap. This is authored/geometry proof, not generation/performance proof of a billion-tile scenario.
- Native machines physically occupy service/wait tiles, earn no remote/queued work, resolve stopped
  state through Entity.Update and report actual graph blockers. Arrived oldest work starts only after
  native haul clearance; the younger node cannot overlap or overtake the service actor.
- Paused occupied native service keeps its exclusive claim/progress, earns no time and continues
  identically from save. Exact authored work retains node/machine until actual departure. Native
  hauling delivers one exact batch with source/destination/cargo conservation and terminal claim cleanup.
- Occupied departure positions retain actual actor/bay/earned progress and typed graph blockers.
  Completed blocked service earns no extra work. Normal player home commands physically clear the
  occupants, allowing exactly-once completion. Native queued/paused/departing/blocked phases and
  interleaved saved continuation agree; untouched sessions receive no shared facility state.

All physical waits are bounded500 Updates with tick/job/toil/slot/blocker/tile timeout diagnostics.
Tests author master-listed content, initialize actual engine entities, submit PostOrder/work-policy/
home commands, call Update and observe. Initial-stock transactions/events, movement, routing, queue
selection, native transfers, work timing, departure, home return and persistence execute in engine
modules. Checkpoint-origin reflection is explicitly fault instrumentation, not gameplay logic.

## Remaining requirements

Vehicle-specific maintenance cadence, service subject selection and repair effects remain open.
Rotated/3D facilities, cross-facility overlapping claims, route collision/congestion, complete slot/
route operating-area containment, preferred routes/trip budgets, full construction/template matrices,
arbitrary provider purity/exception/hidden mutation/global rollback, replay/lockstep/fuzz/soak/
allocations/full P9/P1–P12 acceptance are not proven here. Actor positions may outlive claims and
occupy finite authored exits until actual next work/home movement clears them.

No OilFieldDays/Tycoon/host/demo/platform/Draw change, duplicated queue/route/stock/clock, compatibility
path, artificial size cap or full gate. Full framework goal remains active/partial.
The lifecycle skill kept implementation, stage, tracker and source-specific evidence aligned.

## Source fingerprint at final gate (SHA-256)

| File | SHA-256 |
|---|---|
| src/Beep.SimEngine/Standard/TileWorkDocks.cs | D42F48F89EEC7D93532AFC79D95545D991BD65AC2FF2CC26138EBFC96966B474 |
| src/Beep.SimEngine/Map/TilePresence.cs | B0D7B98DB8A7134FA2863F1EDD69BD8CD029955D3D0DA3850D86DEF7D74BB2EA |
| src/Beep.SimEngine/Map/TileMap.cs | AE3CD92B17AA5AB570DDE6B615396632B828536F7C4F6D4830BC2A10C3FB0168 |
| src/Beep.SimEngine/Standard/WorkModule.cs | D852D8A4F6CC1C9689F9987A7D63D86EF03C601121030253C4CE70843B049821 |
| src/Beep.SimEngine/Kernel/Entities.cs | 38E8255C469B50A3354B7DDA4F7AB306E535564408AB9E76FCD3AC6D6D62F25A |
| src/Beep.SimEngine/Kernel/SaveGame.cs | 218AD01367CD9D2C45FAE9FDC3B7AD829A0694F0B8964A6875C7DB588AE2548F |
| tests/ExtensionProof/CheckpointValidationProof.cs | 13C0A7A749A2C19AAFDC34C3B623E747D55DFA0E872F2D8BC88253E49EB37649 |
| tests/ExtensionProof/ColonyGame.cs | E78E95BFFB42426DD52A956DD777938502ADF36C126120A35D31AC86D39EF09B |
| tests/ExtensionProof/ExtensionProof.csproj | 781D29FC95AED13CC44D2572A2372B219E90A6FBACD5695F94EAABAEF3F609DB |
| tests/ExtensionProof/content/tile-service-docks.json | 6B50EFB6AFF243578F49E5839F60B035D5779B9BE340CA402B715246711443F0 |
| tests/Beep.SimEngine.Tests/TileServiceDockTests.cs | 1DE3447B31D16F0C116ADCC1A57A84F6D610AD5842A3065CA3B7CC9DCF4BAFDE |
| tests/Beep.SimEngine.Tests/Program.cs | 74B98509F63DC775AE879AB3F3133388A518451A2080082933E57163E176E445 |
