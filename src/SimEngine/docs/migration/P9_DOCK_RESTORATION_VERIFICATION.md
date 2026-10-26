# P9-D5l verification: restored original acquired dock delivery

Date:2026-10-09 local. Focused native slice only; full P9/P1-P12 and the active goal remain OPEN.
[Stage](../../plans/enhancements/P9_DOCK_RESTORATION_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md),
[audit](../../plans/enhancements/COMPLETION_AUDIT.md).

## Native changes and boundary

Core2.328.0/save154/replay1, WorkBoard52/WorkSystem45/work-view94,
logistics26/logistics-view40/policy-component3. Existing saved fields/layout unchanged;
tile/point dock content schemas3/4 unchanged. No compatibility reader, new claim/stock/job/
queue authority, host gameplay, platform/Draw, Oil, Tycoon dependency, or second clock.

LogisticsService's existing private Preview recognizes the original acquired destination,
validates the exact owned residual receipt, and checks current admission through the same native
worker/company/resource/quantum/dock/route/trip rules. It does not subtract its own acquired capacity
from a fresh stock gap or request capacity twice. A later stock-band edit cannot retroactively
withdraw an already acquired delivery promise.

WorkBoard's existing dock selector distinguishes explicit productive-service previews from
the current departure toil. Restored partial unloading requires a real productive bay, not a
waiting slot, and rechecks actual dock/occupancy/queue/route policy rather than inheriting departure
permission. No second service calculator or fake arrival.

TryReplanDestination still requires settled actual egress. It first previews the exact restored
original physical binding; if unavailable it uses the same existing full replacement selector.
Original continuation reuses its existing token, same carrier/job, earned handling and original
unload baseline. A different destination still acquires the full residual before releasing the
old receipt and resets only its new leg's handling. No teleport, free transfer or compensation.

Executable source/content/tests frozen after the successful run, observed at2026-10-08 23:59:52UTC.
Only docs/audit metadata changed afterward. Sole GameClock SHA256 unchanged:
35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1.

## ONE NEW exact case

Name: Named dock restoration: original receipt, partial handling, physical egress and busy service.

Normal master-listed scoped-dock-restoration JSON reuses D5k machine, policy, capacity and handling
definitions. The old D5k fixture remains unchanged. The new named-only target has a current-only
original alias, a separate retained queue port, service21,10/wait20,12, primary exit14,10 and an
explicit alternative departure25,0. The second exit is real authored capacity so the busy carrier
can leave without needing the original carrier's occupied exit. These are scenario dimensions,
not framework limits or a fallback in the driver.

The existing thin driver contains one new exact case with three player episodes:

1. Physically pick up8 and interrupt, configure First demand8/Fourth0, and actually partially
   unload1. Lose original access, pause physical egress at the old bay, and restore access.
   No immediate release/retarget/transfer; saved pause retains bay/cargo7/receipt7/earned handling.
   Enabling the worker still reaches the actual exit before same-original continuation.
2. Lose access after the same partial unload, physically depart and settle at exit with the
   original residual receipt7. Restore the original alias. Original stock1 plus reserved7 already
   reaches the maximum8, so a fresh stock-gap admission would fail: native owned delivery resumes
   without another claim, handling reset, or instant unloading. Saved continuation returns physically.
3. At that actual exit wait, a separate ordinary carrier physically picks up2 through the stable
   queue port and reaches the bay. Pause it before unloading, restore the original alias and set
   the original stock target to0. Original job remains at real exit with cargo7/receipt7 and earned
   handling; it cannot steal service or enter the incompatible waiting slot. Save/load during this
   restored-but-busy state. Enable the second carrier: it unloads2 and physically selects the real
   alternative exit. Only then does original continuation reacquire productive service and finish.

Same job/carrier/original binding/progress/baseline and actual promise quantity are checked at
each resume. All three episodes end First8; Other0/0/2; Fourth0; warehouse22/22/20; cargo0 and all
inbound claims0. Each conserves thirty actual units. Checkpoint twins and replay pass. Every
physical stage is bounded to1000 Updates; diagnostics identify current jobs/tiles on failure.
Every Update checks no two vehicles physically overlap the actual bay.
No direct mutation of world, resources, receipts, progress, geometry or provider flags in the driver.

## Actual build and process evidence

THREE build attempts; first two compilation failures repaired, third clean:

| Attempt | Time | Warnings/errors | Outcome |
|---|---|---|---|
| 1 | 3.83s | 0/2 | HaulPreview is a value type; replaced invalid null use with explicit nullable preview selection. |
| 2 | 10.86s | 0/1 | Thin driver's varied seed required explicit ulong arithmetic. Live session29293 observed to actual exit1. |
| FINAL | 5.57s | 0/0 | Actual exit0, complete solution build. |

ONE exact runtime attempt:209 passed,0 failed,0 aborted; case2109ms.
Actual PID22256 exited0, observed2369ms. All stdout/stderr untruncated; observer error null.
No OS timeout or forced termination. No other historical/family/full/Oil/platform runtime.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Named dock restoration: original receipt, partial handling, physical egress and busy service" --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

## Remaining limits

This proves native tile dock restoration before/after physical exit and under busy service, not
every dock geometry, changed machine/area/fuel/service policy combination, external point provider
runtime, backing-store/provider destruction, faults/corruption, congestion/emergency/starvation
stress, global accounting, lockstep/shuffled inputs, soak/allocation or full durability/acceptance.
All prior runtimes, including D5k, are historical/not rerun on2.328.0.
The full plan/goal remains active and incomplete. No staging/commit/push.
