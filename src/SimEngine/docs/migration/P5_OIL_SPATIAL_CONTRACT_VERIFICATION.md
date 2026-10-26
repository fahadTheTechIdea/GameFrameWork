# P5-A8 verification — spatial contracts and independent public queries

Core2.535.0/save154/replay1. Original framework geometry/query/placement/work/fault/clock owners retained.

## Build observations

- Framework focused test-project Release --no-restore: exit0,0 warnings/errors,12.88s.
- First scoped Oil project compile: exit1,0 warnings,40 errors,2.22s. Six new inaccessible fault/disclosure calls exposed the missing public module boundary; original APIs were revised in place.
- Final framework focused compile: exit0,0 warnings/errors,9.44s.
- Final Oil project compile: exit1,0 warnings,34 errors,1.39s; full output retained. Commands.cs8, Operations.cs10, OilModule.cs16. WorldGen/Reservoirs/Views/Research/Storyteller have no diagnostics. No successful Oil compilation/content load/gameplay inferred.

## Exact focused runtime

```text
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Spatial extension boundary: clipped bounds and public metadata queries" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 100000
```

ONE new exact attempt:64 passed,0 failed,0 aborted;494ms case,707ms outer,PID16532,exit0, no truncation. Geometry checks cover actual maximum-coordinate edges, maximum padding, nonzero bounds, disjoint/partly outside shapes, ordinary identity and explicit invalid arguments. Independent non-friend module uses original public fault/disclosure APIs with actual owned/missing prototypes, pure hashes and saved continuation. No second map, policy, state or clock owner. Driver has no World/TestKit/resource/progress mutation; existing authored point factory helpers reused.

The proof does NOT cover Oil physical gameplay, wide-world terrain/distance/circle generation, faulted retained queries, hidden target policies, malicious providers, complete spatial queries, replay/lockstep or industrial acceptance.

## Source boundary

Domain consumers use selected TileMap and actual presence contracts; planning delegates placement/content checks to original owners. No retired World.Map access remains in the five fully migrated files. Founding, power/inventory/module paths remain explicitly incomplete. No Tycoon business rules, game clock, host, renderer or platform changes.

Focused tests only; no old/family/full runtime. Original221/193 recorded checked/28 open unchanged; goal remains open. No staging/commit/push.

## Metadata verification

Current audit:1,998 source anchors,13 quickstart anchors and1,148 selected executable inputs; no hash mismatches or post-pass executable drift. All221 original criterion texts/checkboxes/line anchors unchanged (193 recorded checked,28 open); additional272, no original gate closed. All18 master rows valid;8,660 local links, zero broken. Current new driver has no forbidden state-mutation matches. Whole-worktree diff check exit0 (34 warning/output lines);3,109 dirty paths preserved, zero staged. Original game clock and all12 recorded Tycoon sources unchanged. Goal remains open; commit/push condition not satisfied.
