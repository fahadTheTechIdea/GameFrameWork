# P3 authored session time — 2026-10-04

## Scope and status

Build **beep-simengine-2.17.0**, save format **10**. Only standalone Beep.SimEngine runtime,
proof/test fixtures and local documentation changed. No oil, Tycoon, sibling engine, demo gameplay,
renderer, adapter, Unity or Godot work changed or ran. Initialize → Update remains the sole lifecycle.
The development-lifecycle skill kept source, stage and master evidence aligned; the user's exact-test
instruction overrides its full-regression guidance. P3 and the original overall goal remain incomplete.

## Implemented authoritative paths

- Existing ContentTypes/ContentSet/ResolvedContent load `time_definitions` and optional `calendars`
  from explicitly listed master-manifest documents. ScenarioDef requires a stable `time` reference.
  SimulationTimeDef stores a reduced positive rational quantum in seconds, required `frame_ticks`
  and `ticks_per_second`, optional rational horizon and calendar reference. Invalid/missing inputs
  refuse rather than creating a default minute clock. Old frame_minutes/horizon_days fields are removed.
- GameDuration retains immutable exact decimal/rational units independently of sessions. GameTime
  resolves them to long ticks with one final ceiling; ExactTicks refuses nonalignment. Extreme
  exponents use bounded carry/linear digit traversal, not enormous powers of ten or floating point.
  GameTime is a converter, not a second ticking clock. World.Tick alone owns completed simulation time;
  the existing GameClock accumulates host elapsed input with its existing retained backlog/budget.
- Shared steps, recipes, demolition and line definitions retain duration intent. WorkBoard/Producer
  preserve their existing WorkProgress owners/codecs and resolve requirements against World.Time.
  Movement, physical providers, retries, logistics and network flow use that same quantum. Line prefix
  requirements ceil the full authored prefix, not an already-rounded per-tile count.
- GameSystem.GetSchedule resolves declarations once during composition. Period/phase/span are long;
  periodic durations require exact alignment. Network flow's interval is preflighted during composition,
  not first discovered to be invalid after ticking. No compatibility Schedule property remains.
- ICalendar / CalendarDef allow extension-owned calendar behavior. CycleCalendarDef authors independent
  field names, exact durations, radices and origins. CalendarReading is detached/read-only. Null means
  no calendar, not a fabricated date. Missing/empty references and incompatible units refuse explicitly.
- SimGame/PlayerView expose immutable ISimulationTime. NewGameRequest.Horizon resolves in the engine;
  null uses authored intent, explicit zero disables it. Engine-owned duration waits/scripted inputs
  resolve against their session. Clock frame/host elapsed rates are authored separately from game units.
- Save 10 settings contain selected time identity and horizon ticks. World.Sync's declaration preflight
  includes time ID, reduced quantum, horizon and captured wide schedules before initialization/payload
  callbacks; the same owner hashes them. Loaded active content chooses pacing without an all-module
  override. Prior layouts are rejected, with no old codec/loader retained. Lockstep settings were migrated;
  peer/replay behavior was not exercised by this change's two tests.

## Verification actually run

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Session time:'
```

| Exact selected suite | Passed | Failed | Time |
| --- | ---: | ---: | ---: |
| Session time: authored quantum, exact schedules and optional calendars | 52 | 0 | 55 ms |
| Session time: physical consumers, partial updates and saved twins | 45 | 0 | 217 ms |
| **Only these two new suites** | **97** | **0** | **0.3 s runner** |

Test exit **0**; no aborted suite. Final build exit **0**, **zero warnings/errors**, **1.74 s**.
The exact test run preceded documentation/comment-only updates; the final build was rerun, but tests
were not repeated merely for documentation. No full suite, broad filtered batch, prior family rerun,
quick-setup execution or oil integration ran. Migrated old tests compile; that is not behavioral proof.

Coverage includes minute/subsecond/third-second rational conversion, near-boundary decimals,
long.MaxValue success/refusal, extreme exponents, 12 independent rational-oracle cases, wide exact
schedules, optional/non-earth calendars, retained reading immutability, missing/invalid configuration,
partial elapsed updates, isolated interleaved sessions sharing the same immutable recipe definition,
physical construction/material hauling and production, saved continuation hashes, explicit horizon
ending/backlog cancellation and duration waits. Physical stages use bounded watchdogs with tick/state/
fault diagnostics; machines travel and resources are supplied/consumed normally. No old-limit progress
seeding is used by these new session-time tests.

Network interval/rate arithmetic is checked here; physical partial/final line/network behavior,
retry failure combinations, scripted PressAt input and hostile manifest tampering were not exercised.
Help XML and quick-setup JSON syntax were read/validated only, not rendered or executed. The example
now explicitly authors its time; its displayed old 2.13.0 hash remains labelled historical.

## Source identity

All **120 authored src/tests files** from `rg --files src tests`, excluding bin/obj, sorted by path.
Each line is `slash-normalized-relative-path|SHA256`; LF joined, UTF-8 encoded, then SHA-256 hashed.

- Source identity: `31B5ACE3CC85CBBF11D839D0FA372230DCC9A2B8BC8C6344D4F283492E5FA448`
- Runtime DLL SHA-256: `BA8B5F043B6F95FDBF59C788204392845B38F00157ED2DAD447271B67C40D140`
- DLL: `src/Beep.SimEngine/bin/Debug/netstandard2.1/Beep.SimEngine.dll`

## Remaining original scope

P3 optional/chunked space, mandatory TileMap/vehicle assumptions, 8..4096 side restriction,
provider-owned placement/navigation and capability-authored state references/facets remain open.
Standard planner/retry/flow policy constants now use durations but are not yet fully player-authored
policy definitions. Oil minute-calendar parity is deferred to P5. Peer/replay, hostile-save fuzzing,
allocation/soak and full stage closure are not inferred from two exact green tests. Broad/full execution
requires explicit user authorization. P4/P7 and later gameplay/framework stages keep their original scope.
