# P4-G42 contact exploration bindings — scoped verification

2026-10-09; core2.380.0/save154/replay1. Full goal and original P1-P12 remain OPEN.
[Stage](../../plans/enhancements/P4_EXPLORATION_BINDINGS_DESIGN.md).

## Authoritative implementation

`IContactExplorationProfileDefinition` and `IContactExplorationDefinition` replace the reusable
component's concrete profile/source dependencies. Sole `ContactExplorationProfileDeclaration` and
`ContactExplorationDeclaration` parse and resolve original strict JSON. Native and direct independent
definitions delegate them; the original ContactExploration owns unfinished dwell, CompanyKnowledge
owns learned history, and GameClock alone advances time. No compatibility reader, copied scheduler,
profile authority, stock, observation or state machine was introduced.

All loaded profiles validate frozen actual aliases, nonempty canonical entries, region/subject/state
ownership, first-learned subjects and duration representability. Source validation checks actual named
sensor/presence/space agreement. Fresh call facts retain actual immutable references only. Metadata
reads use existing scoped observations, including the queried entity's actual codec when present.
Selected null-entry metadata is a typed ContentException, not a NullReferenceException.

The independently compiled extension configures the same ContactExploration component through public
contracts. New driver has no World access or stock/progress/knowledge mutation. Existing pure point
sensor factory receives generic extra content/module hooks; old point sensor case was NOT run.

## Commands and results

Four `dotnet build Beep.SimEngine.slnx --no-restore` invocations, all terminal exit0, zero warnings/errors:
17.95s,6.36s,4.63s,5.79s. No full/family/historical/Oil/Tycoon/platform runtime.

The ONLY new exact bounded case, repeated for observed setup repair/final review:

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Exploration bindings: physical point discovery and saved interruption" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 50000
```

| Attempt | Checks | Failed/aborted | Case ms | PID/exit | Observed ms | Result |
|---|---:|---|---:|---|---:|---|
| 1 | 21 | 1/0 | 1561 | 35196/1 | 1720 | Mobile endpoint occupancy blocked supply |
| 2 | 21 | 1/0 | 1533 | 14408/1 | 1688 | Added diagnostics confirm real loaded carrier waiting |
| 3 | 191 | 0/0 | 1016 | 23872/0 | 1167 | Authored dock and accessible outside site succeed |
| 4 FINAL | 193 | 0/0 | 960 | 38692/0 | 1107 | Selected null-entry refusal repaired and proven |

Every child is observed Exited; no timeout, abort, truncation or observer error. Final selected253
source/proof/test/JSON inputs frozen2026-10-09 15:23:09UTC before attempt4. This selected inventory is
NOT a complete transitive compilation/content closure. Later actions only documentation/read-only
audit. Earlier freezes:15:19:36UTC,15:20:38UTC,15:22:14UTC; all attempt records retained.

## Scope proven

Native and independent profile/source bindings reference the same original component and actual frozen
metadata. Physical carrier transfers one unit (source20→19, machine0→1) through authored dock slots,
waits for departure, and is normally held. Ordinary staffed work moves the other machine5→10,12.5→10;
active supplied sensor and actual interval-region membership gate12/15-minute dwell. Disabled arrival
reveals nothing. Reset/retain and actual outside-region interruption follow explicit JSON. Game.Update
settles the authored sampling entity state. Partial supply, travel, dwell and last-interval saves,
paused clock, saved twins, detached facts and private source diagnostics are covered.

Original ledger earns both discoveries; no sensor/geological reading is fabricated. Ordinary sharing,
own-company forgetting and physical rediscovery retain recipient history. Replay verifies normal
commands and control inputs, and untouched interleaved sessions retain their hashes. Fifteen invalid
authoring cases fail before Initialize, including unused and selected null-entry profiles. Two actual
registered binding callbacks reach throw/pause; real clock mutation is rejected before Initialize.

## Still open

Live metadata getter/entry iteration/private codec/recursion fault matrix, unrelated private state and
global rollback/sandbox/performance are NOT proven. Region metadata still uses the existing authored
SpatialRegionDef seam; remote/cell-wide sweeps, utility-consuming sensors/exploration, arbitrary geometry,
protected-cell/visibility breadth, shuffled-input lockstep, fuzz/soak and compound gates remain OPEN.
Native tile contact, sensor, fresh/regional visibility and earlier cases compiled only, NOT rerun.
Full regression awaits human authorization. G19 excluded-file scope decision remains unanswered;
Tycoon/OilFieldDays/hosts/demos/platforms untouched. No staging/commit/push before full completion.
