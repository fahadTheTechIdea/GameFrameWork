# P4-N3 — Native point startup verification

Historical P4-N3 runtime evidence; not rerun on2.330.0. P4-N4 now validates selected startup declarations before Initialize; actual geometry/occupancy remains during generation. [Current evidence](P4_STARTUP_COMPOSITION_VERIFICATION.md).

Date:2026-10-09. [Stage](../../plans/enhancements/P4_NATIVE_POINT_STARTUP_DESIGN.md).

## Outcome

Removed PointStartDef, PointStart, the point_starts section registration and points.start world-generation stage from the external PointModule proof. Native WorldSetup is the sole startup authority; point geometry still implements the existing placement/presence/motion/dock contracts. No compatibility grammar or prototype-position inference was introduced.

The native root profile retains its ordinary authored stable ID points.start; that is a profile reference, not the deleted world-generation stage or a compatibility alias. Strict obsolete-section refusal is covered below.

Migrated35 JSON documents/82 entries/35 profiles, plus native fixture authoring and explicit supplemental profile selections. Each entry now declares after/entity/company/placement/home. Every original non-startup JSON value remains structurally unchanged, except the six scenarios explicitly enrolling/selecting WorldSetup. Master lists explicitly register that module. Supplemental content is not automatically selected merely because it is loaded.

StartupContentAuthoring edits input JSON only: explicit additions/entry membership, profile includes and actual placement. It contains no World, spawner, stock grant, gameplay validation, work/transfer advancement, save handling or clock. Historical raw fixture paths now target native authoring. All older migrated suites (including optional-money fixtures) are source/compile only; no broader runtime claim.

The new case uses ordinary PointGame/master composition and the explicitly embedded/master-listed native-point-startup-extra.json document. Unselected supplemental entries create nothing; explicit shared includes create exactly two extra entities once. Main placements2/18/4 differ from prototype0/10/0. Supplemental company1 source40/machine35 bind the actual source as home and grant20 units exactly once. A geometry-only composition with no native startup content has zero initial entities; the point provider cannot spawn anything itself.

Public player commands drive physical staffed inspection and four-unit pickup/travel/loading/unloading. Source16/destination4, travelling and hauling saved twins, no repeated entities/grants, replay, pure previews, and five native before-Initialize schema/reference/cycle refusals are verified. No new test driver live-World mutations.

## Exact evidence

Three clean builds, zero warnings/errors:6.61s,5.36s,4.01s.

ONE NEW exact case ran twice:

| Attempt | Passed | Failed | Aborted | Case time | Process |
| --- | ---: | ---: | ---: | ---: | --- |
| First | 19 | 1 | 0 | 290ms | PID4528 exit1, observed443ms |
| Final | 44 | 0 | 0 | 546ms | PID9676 exit0, observed707ms |

Both processes exited normally; stdout/stderr untruncated, no observer error, timeout or forced termination. First failure was a new fixture authoring error: geometry-only input retained native sections while their provider was inactive. Corrected that input by removing its startup sections and supplemental document; no engine ownership bypass/fallback. Only this same exact case was rerun.

Executable freeze observed2026-10-09 01:04:35 UTC. Later edits documentation/audit only. Core2.329.0/save154/replay1, WorkBoard52, WorkSystem45, work-view94, Logistics26, logistics-view40, policy3, point-space5/motion2/dock5 and tile-dock3 unchanged. Sole GameClock SHA256:35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Native point startup: explicit selection, physical work and saved delivery without provider spawning" --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

## Limits

This completes the scoped duplicate point-startup migration, not the entire framework plan. Prior P4-N2/N1/D5m and all other suites were not runtime rerun after this migration. No family/full/Tycoon/Oil/platform runtime. No business rules, engine clock, native saved field or renderer/adapter changed. Existing company/spatial admission occurs during generation after Initialize; no whole-generation atomicity guarantee. Broad durability, faults, scale/performance, provider/mod breadth and remaining P1–P12 requirements stay open.

Full goal and plan remain unfinished; no staging/commit/push.
