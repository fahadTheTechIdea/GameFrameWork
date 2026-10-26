# P10-EP1 — Topology erase postcondition evidence

Status: VERIFIED-SCOPED FINAL296/0/0; full goal/plan open.

[Stage](../../plans/enhancements/P10_TOPOLOGY_ERASE_POSTCONDITIONS_DESIGN.md). Original221/187checked/34open unchanged; exact new-case compile/runtime evidence follows, no broad tests or staging/commit/push.


## Scoped delivery —2026-10-12

Core2.489.0/save154/replay1; existing view/state schemas unchanged. The sole NetworkService.ApplyTopology boundary retains its original redistribution/transactions/pools/endpoint identities and interrupted-commit state. After explicit geometry erase it builds the actual topology using the SAME BuildTopology, verifies canonical vertex→partition mappings and partition owners against admitted candidates, and only then publishes graph maps. Exceptions latch the original first fault at the commit boundary. No second graph builder/solver/stock ledger, persistent candidate, refund or rollback is introduced.

Existing point geometry accepts optional network_graph.erase_fault metadata (noop/restore/extra/throw); negative behavior lives entirely inside that same engine-side geometry provider. Restore performs legal erase then restores the original physical vertex/owner, proving version changes alone cannot establish correct geometry. Extra removes an additional actual vertex; noop leaves all physical geometry untouched. The driver never stamps layers or changes stock.

### Exact compile/runtime evidence

- Solution: dotnet build Beep.SimEngine.slnx --no-restore;4.96s,0 warnings/1 error in negative point probe nullable NetworkStep. Repaired to required authored step value, no fabricated default.
- Focused probe/scenario compile: dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore;16.87s, clean0 warnings/0 errors.
- FIRST exact attempt:69 passed/1 failed/0 aborted in15884ms; PID9180 Exited1/process16246ms. New point scenario kept stock physically in pools but did not dispatch that stock to the reconnect cursor; real reconstruction reached unchanged512Update bound. Expanded diagnostic includes point/plan/blocker/order. Added ordinary player haul from actual named network port to actual work cursor; no stock grant or weakened bound/assertion.
- Focused driver repair compile: same focused project command;8.25s, clean0 warnings/0 errors.
- FINAL same exact case:296 passed/0 failed/0 aborted in18422ms; PID45904 Exited0/process18786ms, no truncation or observer error. No executable edits after FINAL.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Topology erase postconditions: physical graph stock commit parity and faulty erase cutoff" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 24000
```

Four actual tile/point minute/half-minute workflows conserve finite entity+pool stock through earned cut/split and physically supplied reconnect/merge (only authored reconstruction inputs consumed). Point network is owned/private, has real fourteen-unit pool stock and a named network resource port for physical hauling to the cursor. Original pool snapshot remains detached. Saved partial work/split/merge, sole-clock pause, private work withholding, replay and interleaved isolation pass. Eight point minute/half noop/restore/extra/throw erases occur only after real machine work; invalid commit publishes explicit fault with each viewer's last healthy time/map/entity payload, no healthy completion delta, first error retained, clock stopped and healthy hash/save/live pools refused. No rollback claim for already transferred stock inside interrupted commit.

One NEW exact case only;512Update stage/60000ms process/5000ms termination/24000-character output bounds. Historical cases compile-only. Original221/187checked/34open unchanged, additional217. Active promises, all ownership/spill/removal paths, arbitrary geometry/business/resource/incident/fault/lockstep/stress/performance/full gates remain OPEN. No Tycoon/Oil/platform/Draw/clock implementation changes. Authorized final full regression deferred until implementation finishes. No staging/commit/push.


## Post-FINAL metadata closure

Freeze:2026-10-12 10:24:39 UTC;1739 source anchors/971 selected executable inputs/13 quickstart anchors. Six existing selected inputs changed, one new executable driver; three new files (driver/stage/evidence), two changed production files and no new production source/state schema. No executable edits after FINAL.

Read-only closure:0 source/quickstart mismatch/0 executable drift;2925 paths/1041 Markdown/8697 local links/0 broken;18 valid master rows;221 original literal requirement/checkbox/line anchors validated with0 errors. Driver boundary0 and current master identity/commands/timings/limits checks all pass. git diff --check exit0 (34 existing warning output lines), dirty2913/staged0. Protected clock and all12 Tycoon anchors remain exact against the prior approved source freeze. Original221/187checked/34open unchanged, additional217 and goal_complete=false. Full/old/family runtime and staging/commit/push were not performed.
