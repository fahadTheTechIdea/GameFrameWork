# P3 wide authoritative timestamps — 2026-10-04

Historical evidence for 2.15.0/save 8, not the current source. See
[2.16.0 duration/progress evidence](P3_DURATION_PROGRESS_VERIFICATION.md). The 52-suite batch below
was too broad for the requested per-change policy. Do not repeat it or treat these filters as the
current verification recipe; run only exact directly affected tests, with broad/full runs requiring
an explicit user request.

## Scope

Only Beep.SimEngine runtime, standalone tests/proofs and local documentation changed. No oil, Tycoon,
sibling engine, platform SDK, adapter or rendering implementation was edited or tested. Initialize →
Update remains the sole engine lifecycle. The full framework goal and P3 remain active/incomplete.

Build **beep-simengine-2.15.0**, save format **8**. This is a breaking timestamp/layout replacement:
no old timestamp overload, compatibility reader, secondary clock or duplicate state ledger remains.
The development-lifecycle workflow keeps this implementation slice, focused evidence and active tracker
aligned; it does not substitute documentation or focused checks for the final stage gate.

## Existing owners migrated

World.Tick is long, with checked completion preflight before commands/systems. TickInfo and entity
Update context pass the same tick. Commands keep full-width issue/application times; pending commands
use Long in their existing inbox codec. Results, notifications and faults report full-width times too.

Entity.StateSince, entity construction/admission and state resolution use long without a second state
setter. Entity.Sync stores the same field. GameUpdate.StartTick/Tick, GameSnapshot, EntityView,
PlayerView, save/load-menu metadata and incident facts expose the same timestamp representation.
GameUpdate.TicksRun remains checked int: it counts one bounded Update's work, not session age.

WorkBoard is still the sole order/job authority. Order/Job.CreatedTick and Job.RetryAt are long;
its state schema is now 2. WorkView projects CreatedTick directly, not a mirrored timer. Retry deadline
addition is checked. Mover.TicksMoving is a checked long elapsed counter in its sole codec; component
schema is now 2. Script waits/deadlines and tick-addressed input widen in their existing owners.

SaveGame writes/reads a long header tick, validates nonnegative stored time and header/body parity.
World/command/entity/work/mover Sync paths hash exactly the same fields that they save/restore.
The build and format reject old layouts before reading their payload; no conversion layer was added.

NetSession uses (long tick, int seat) frame keys and long checksum keys, sealed-through state,
hello/frame/checksum timestamps, stall and desync diagnostics. The ahead window uses subtraction after
the stale-tick check, so validation near long.MaxValue does not overflow. Sealing target addition is
checked before committing frames. Negative/trailing checksum payloads refuse before comparison.
Independent event and incident proof providers widen their actual timestamps and bump affected codec/
event schemas; they still compile without framework friendship.

The existing calendar can project long elapsed days/years/hours, with bounded int day-of-year/hour/
minute results after modulo. It is still the old minute/calendar policy, not the future optional calendar.
Schedule periods/spans, step/recipe/line work durations and GameTime hour/day conversions remain
32-bit/current-scale until T1/T2b/T3. No inactive generic-time model was installed beside them.

## Focused verification only

Commands ran from the standalone root. Final build:
`dotnet build Beep.SimEngine.slnx --no-restore` — exit 0, zero warnings/errors, **1.47 s**.
Affected filters use `dotnet run --project tests/Beep.SimEngine.Tests --no-build -- '<filter>'`.

| Filter | Passed | Failed | Suites | Runner time |
| --- | ---: | ---: | ---: | ---: |
| `Wide time:` | 141 | 0 | 6 | 0.2 s |
| `Lifecycle:` | 61 | 0 | 6 | 0.5 s |
| `Standard:` | 49 | 0 | 8 | 0.5 s |
| `Protocol boundary` | 70 | 0 | 4 | 0.4 s |
| `Net:` | 5 | 0 | 1 | 0.4 s |
| `Framework: save, load, describe` | 7 | 0 | 1 | 0.2 s |
| `Framework: determinism` | 6 | 0 | 1 | 0.3 s |
| `Framework: spread schedules` | 4 | 0 | 1 | 0.2 s |
| `State codecs:` | 94 | 0 | 5 | 0.3 s |
| `Responses:` | 72 | 0 | 5 | 0.3 s |
| `Event ownership:` | 63 | 0 | 6 | 0.1 s |
| `Operations:` | 92 | 0 | 6 | 0.3 s |
| `Capabilities: independent inventory production` | 21 | 0 | 1 | 0.5 s |
| `Time boundaries: tick, identity` | 14 | 0 | 1 | 0.2 s |
| **Total** | **699** | **0** | **52** | **Focused, not full regression** |

All final listed filters exit 0. A mistaken `Events:` filter matched zero suites/exit 1 and was replaced
with the actual `Event ownership:` name; no empty filter is counted as coverage. No --all run, oil gate,
full replay proof, fuzz/soak/allocation gate or P3 completion is claimed. The latest full standalone
gate remains the historical 2.13.0 source, not this working tree.

## Wide boundary and physical proofs

- Queued commands save/load before application at int.MaxValue-2 and 2^45, then cross the former
  boundary through normal Update. Results/notices/new entity state/private snapshot times are exact.
- Periodic/spread schedules and upper timestamp hash bits are tested; header/body mismatch refuses.
- Movement state and persisted movement elapsed age cross the old bound. Arrival takes actual ticks.
- A manual move deliberately holds its machine until the player's normal ReleaseVehicle command.
  Construction then physically hauls material and works at least the authored five hours. In-flight
  work/jobs/retry/reservations save and continue identically; completion reports the wide timestamp.
- Independent incident response at 2^40 consumes normal physical machines/material/cash, survives an
  in-flight save, completes once and retains private facts. Independent event payloads/listener state
  also preserve wide timestamps through nested dispatch and saved twins.
- Shuffled peer frames progress at int.MaxValue-2 and 2^45 with exact delayed command application,
  matching hello/hashes and typed desync/stall diagnostics. Near-long-max frames validate safely;
  an unrepresentable sealing target refuses without authoritative mutation.
- Calendar/script deadlines remain exact at wide ages. The last representable tick completes, and the
  next faults at begin before gameplay. Numeric limits are explicit, not a replacement platform cap.

Boundary seeding uses reflection only in the permitted framework test assembly; no public mutable
tick setter or external-provider reflection was introduced. Initial proof failures were test setup:
manual movement intentionally held the crane; a desync packet was compared after the checkpoint had
already been consumed; a body-parity test attempted restore into an already populated world. Tests now
use the normal release command, an unconsumed checkpoint and a freshly composed restore target.
No engine rules were weakened to force those scenarios to pass.

## Executable-source identity

All **115 authored src/tests files** from `rg --files src tests`, excluding bin/obj, sorted by path.
Each line is `slash-normalized-relative-path|SHA256`; LF joined, UTF-8 encoded, then SHA-256 hashed.

- Source identity: `377A1D2209DEF4C40D6334DBFB70CE2B1F1A24EE3C14ADE84205F0E11BF17197`
- Runtime DLL SHA-256: `0E1DD5970651E0088BEE5E89F4B9469FEBA08FD33CA1168D62C27011D44D3306`
- DLL: `src/Beep.SimEngine/bin/Debug/netstandard2.1/Beep.SimEngine.dll`

## Remaining requirements

T1/T2b/T3 must resolve exact authored time definitions, per-session quantum, wide durations/schedules,
units/rates and optional calendar/horizon/frame policy, removing static minute assumptions in place.
S1–S4 must complete optional/provider-owned space, remove the tile side cap, generalize presentation
facets/state references and prove nonspatial/large-coordinate worlds. Other roadmap stages and the
authorized oil-consumer migration remain open. Full regression runs at stage completion or by request,
not after each change.
