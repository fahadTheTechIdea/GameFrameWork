# P3-SC1 observed spatial snapshot comparison verification

## Engine change

Core2.485.0/save154/replay1. The original EntityView.Matches receives actual World context from the existing GameSnapshot merge. Spatial comparison runs before primitive equality, so entity revision changes do not conceal an invalid comparison callback.

Null pairs and differing shape types/spaces are compared without callbacks. Same-shape pairs use the existing observation guard over actual presence/entity/selected space; both self comparisons must be true, cross results must agree in both directions and remain stable on repeated reads. Primitive equality and the original delta lists/merge remain the only publication owner.

No new equality encoder, gameplay state, snapshot cache, timer, publication stream or saved comparison result. Generic algebraic/purity checks cannot infer whether arbitrary custom fields were honestly included; full mod contract/payload guarantees remain qualified.

The existing independent point presence accepts JSON-authored negative comparison metadata with an explicitly authored trigger position. Its probe snapshots are derived point snapshots, preserving the actual point geometry contracts. Detached normal point values carry facts; only declared malformed comparison instrumentation holds a source reference/cross-read flag. No movement, stock or provider mirror. A second valid derived shape proves cross-type transitions are changed without invoking a cross-type callback.

## Builds and both runtime attempts

One solution compile: dotnet build Beep.SimEngine.slnx --no-restore,14.83s,0warnings/2errors. New proof used two C# implicit multi-variable declarations; production and independent modules compiled.

Two focused compiles using dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore:

| Compile | Seconds | Result |
| --- | --- | --- |
| Proof syntax repair | 7.10 | Clean,0warnings/errors |
| FINAL point-geometry subtype repair | 11.96 | Clean,0warnings/errors |

No solution rerun.

| Runtime | Passed/failed | Case ms | PID / process ms | Result |
| --- | --- | --- | --- | --- |
| FIRST1 | 2005/1 | 1527 | 20180 /1734 | Healthy workflow reached1000Update failure bound because new probe wrappers were not accepted point geometry subtypes |
| FINAL2 | 464/0 | 1096 | 32200 /1303 | Exited0, no suite abort, truncation or observer error |

The first attempt preserved its physical completion assertion and bound; probes were corrected to derive from the existing point snapshot representation rather than weakening assertions or adding a geometry fallback. Both attempts used the same ONE NEW exact case. Clean final compile precedes FINAL; no executable edit afterward. No old/family/full runtime.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Spatial snapshot comparison: observed symmetric stable delta matching and first-fault retention" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 24000
```

Outer60000ms/termination5000ms/output24000chars. Healthy/fault physical stages bounded1000Updates with actual clock diagnostics.

## Public-player proof

Four actual point minute/half flows cover normal same-shape comparisons and a real authored shape-type transition. Normal PostOrderCommand/Update drives actual motion and completed inspection. Physical position changes appear in the existing Changed delta, publication revisions preserve the same viewer baseline, and differing shape types do not invoke forbidden cross-type comparison. Retained initial payload remains unchanged; saved twins, terminal save/replay and untouched interleaved session match. Native map and nonspatial session updates also publish successfully.

Ten negative cases cover each time quantum for nonreflexive, asymmetric, stateful, original exception and actual entity-revision mutation callbacks. Actual movement activates the authored position2 probe; invalid callbacks fault before partial changed/spawned/removed facts appear.

Each viewer retains its own last healthy time/map/entity payload, with no malformed delta prefix. Original callback error survives. Faulted session cannot advance clock, save/hash healthy or read live entity projections; subsequent calls retain first fault and an independent session remains unchanged. No rollback is claimed.

The driver only composes actual master-listed JSON and uses public seat commands/results/views/snapshots, Update, saves/replay. No World/TestKit/direct stock/job/progress/geometry writes. Comparison fault behavior lives in independent engine-side providers; assertions inspect facts rather than implementing a second delta builder.

## Qualification

Original221/186checked/35open unchanged; additional212. No original checkbox/text/line changes or gate closure. This proves affected comparator algebra/purity and publication retention, not arbitrary consistently omitted custom fields, foreign raw CLR state, every visibility/private-cell/spatial-query/delta/mod matrix, allocation/performance, fuzz/concurrency/stress or full P3/P11/P1-P12 regression. Earlier cases compile only, not rerun.

Canonical SimEngine only; protected sole-clock/G19/Tycoon unchanged. No Oil, Draw/platform/adapters, staging, commit or push. Full goal AND plan remain incomplete.

## Metadata closure

Fresh read-only metadata closure:1721 source anchors,957 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2909 paths/1032 Markdown files/8675 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors, unchanged186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2897/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional212. Six existing selected executable inputs changed; one newly created executable driver/three new source files, three existing production sources changed/zero new. No previously existing input newly admitted. Existing view schemas unchanged; guarded internal comparison adds no projected fields. Only primary P3 row updated, distinct historical P3 row untouched. No runtime rerun for documentation validation.
