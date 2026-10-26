# P4-G33 scoped verification — 2026-10-09

Core beep-simengine-2.370.0; save154/replay1/map6 unchanged.
Full P1-P12 and active goal remain OPEN. Nothing staged, committed or pushed.

## Implemented ownership

TileConditionDef deleted. Native and independent definitions implement ITileConditionDefinition
directly. TileLayerBandDeclaration is the sole native band reader/linker/validator/sample interpreter.
TileConditionReferences resolves actual interface aliases in the original catalog.
ResolvedContent validates every loaded predicate identity/space alias, including unused definitions.
TileConditionContracts is the sole shared context/scoped evaluation boundary for navigation and
construction. It observes original TileMap.Version and EntityObservation facts, never whole-map
hashing per route cell, another registry, cached readiness, scheduler, persistent state or clock.
Independent column predicates and independent band metadata alter original consumer rules without
consumer/scheduler changes. The extension assembly uses public engine contracts only.

## Commands and actual results

Compilation: dotnet build Beep.SimEngine.slnx --no-restore.
Initial build: one test-only nonexistent GameCommand type; repaired to existing Command.
Clean builds: 4.79s,4.49s,4.42s,4.23s; zero warnings/errors.

ONE NEW exact case only:

    dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Tile condition contracts: independent JSON governs physical colony work" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 50000

Attempts of this SAME new case:

| Attempt | Passed | Failed | Aborted | Case ms | PID | Exit | Observed ms |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Initial | 129 | 0 | 0 | 6625 | 14836 | 0 | 6803 |
| Strengthened actual-mutation assertion | 135 | 2 | 0 | 6562 | 6588 | 1 | 6718 |
| Explicit but off-map fault fixture placement | 135 | 2 | 0 | 6553 | 25880 | 1 | 6696 |
| Final actual in-bounds mutation fixture | 137 | 0 | 0 | 6550 | 5064 | 0 | 6697 |

All processes exited; no output truncation or observer error. Runtime60s/termination5s/output50000
bounds unchanged. The two failed attempts exposed a weak identity-mutation fixture: unplaced then
out-of-bounds native creation refused before identity allocation. Actual public engine placement
at44,28 now makes the mutation happen before the original guard refuses it. No engine fallback or
weakened assertion was introduced. Initial129 pass is superseded, not final current-source proof.
178 selected executable inputs frozen2026-10-09 12:05:01UTC before FINAL runtime. Executable
source unchanged after final pass. Prior freezes retained in the audit; no historical suite rerun.

## Narrowly proved

Native bands, independent bands using the same declaration, and independent column predicates
share actual catalog/space aliases. Same predicate governs real route and footprint admission.
Accepted/refused previews stay pure, normal refusals create no project, ordinary accepted builds
create staged sites. Machines haul loaded material, move and construct over game time. Interrupted
hauling save/load twins, sole-clock pause, completion, exact50-minus30 steel, replay and interleaved
session isolation pass. JSON edits change route AND placement without changing prior frozen content.
17 early malformed-content cases and8 scoped predicate faults cover actual exceptions, native map
writes, clock changes and identity allocation in route/placement reads; no healthy faulted Update/hash/save.
Historical case received a compile-only contract alias migration, not runtime.

## Still OPEN

Whole-world/CLR sandboxing, arbitrary hidden metadata/state mutations and unrelated owner changes
are not proved by scoped observation/map invalidation. Remaining navigation/placement profile
hierarchies, all provider matrices, lockstep/concurrency/fuzz/soak/allocation/performance, full
accounting and original full P1-P12 gates remain OPEN. No Oil/Tycoon/platform/Draw or broad runtime.
Pending G19 excluded-file scope decision remains unanswered and unchanged.
The lifecycle skill governed stage/owner/tracker sequencing; repository/user focused-test and
delayed-publication instructions take precedence over generic broad-test/early-commit guidance.
