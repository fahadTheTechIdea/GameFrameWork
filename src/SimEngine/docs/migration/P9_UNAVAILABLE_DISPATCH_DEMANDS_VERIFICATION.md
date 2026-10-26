# P9-DQ1 visible unavailable demand verification

## Engine change

Core2.479.0/save154/replay1/logistics-view42. Original LogisticsDispatchOffer.Requested is now Fix?: null explicitly means an unavailable original stock cannot provide a computed delivery gap. Known route/recovery/ready quantities remain concrete. Constructor and fresh ready acquisition reject unknown authorization; no zero-default transfer.

Original BuildQueue now checks physical availability before suppressing quiet bands or satisfied emergency gaps. A selected enabled positive active unavailable demand yields one typed PolicyBlocked offer with Requested:null, original source/port/endpoint/age/priority/emergency and exact availability blocker. No stock/capacity read or claim acquisition is attempted for that unavailable offer. Original grouping/ties/scheduler/demand/stock/clock owners remain; no saved unknown flag, second queue or copied inventory.

Ready quiet/fulfilled and zero/disabled/expired intents retain original no-dispatch behavior. Restoration recomputes actual DeliveryGap through the same existing owner. View schema41 to42 and engine identity record public quantity semantics; simulation/save payloads unchanged.

## Exact build/runtime

One clean solution build: dotnet build Beep.SimEngine.slnx --no-restore,20.72s,0warnings/errors. No repair compile.

ONE NEW exact FIRST/FINAL377/0/0 in7377ms; PID3108 Exited0, process7572ms, zero aborted suites, observer error or truncated stream. Clean compile precedes runtime; no executable edits afterward. No old/family/full runtime.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Unavailable dispatch demand: active and quiet original stock intents remain visible without invented gaps" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 24000
```

Outer60000ms, termination5000ms, output24000chars. Each physical stage bounded1000Updates with actual clock/queue/kind/port/requested/blocker diagnostic.

## Actual public-player proof

Four native/point minute/half workflows use existing actual scoped restoration masters/files, ordinary player held/stock policy/selection/hauling commands, immutable queues/demands/diagnostics, Game.Update, saves/replay and sole clock only. No World, TestKit, raw stock, demand, job, geometry or progress writes.

An enabled8/8/8 band on the actual empty original endpoint starts replenishing and reports a known eight-unit gap. Ordinary selection to a different current store removes original access. The original intent remains visible with Requested:null and ResourcePortCodes.OriginalUnavailable, no selected transporter, unchanged source/endpoint/port/creation/priority/last-dispatch and original Replenishing=true. Policy Quantity8 remains distinct from the unknown gap. Private foreign queues are empty; repeated views are pure.

Saved unavailable continuation and sole-clock pause preserve stock/intent/age. Waiting Updates acquire no physical job or promise and never mark dispatch successful. Restoring original access reports a newly calculated8 gap; ordinary explicit hauling physically transfers8 from the real source, travels/unloads/departs and settles cargo. Actual8 original stock makes the band quiet and its ready queue offer disappear.

Selecting another store again while that band is quiet still exposes the unavailable original intent with Requested:null, not a fabricated eight-unit shortage or zero gap. Replenishing remains false through saved waits; actual original stock8 remains accessible through its separate retained alias, while unrelated current stock is0. Restoring original access again suppresses the physically fulfilled quiet band. Final saves/replay and untouched interleaved sessions match.

## Qualification

Original221/186checked/35open unchanged; additional206. No original literal, line or checkbox changes. This proves stock-band active/quiet availability in the current native/point workflows, not every emergency or external workflow demand class, multi-alias grouping policy, disabled/expired lifecycle, route/recovery quantity layout, all provider failures/private CLR mutation, concurrency, congestion, fuzz/performance/stress or full P9/P1-P12 runtime. Earlier proofs compile only, not rerun.

Canonical SimEngine only; protected G19/Tycoon/sole-clock source unchanged. No Oil/platform/Draw/adapters, staged files, commit or push. Full goal AND plan remain incomplete.

## Metadata closure

Fresh read-only metadata closure:1699 source anchors,943 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2887 paths/1020 Markdown files/8657 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors, unchanged186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2875/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional206. Five existing selected executable inputs changed, one new executable input/three new source files, three existing production sources changed/zero new. Logistics view42 records changed nullable quantity semantics. No runtime rerun for documentation checks.

