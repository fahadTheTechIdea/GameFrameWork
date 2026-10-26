# P9-D5k verification: named resource dock and physical recovery egress

Date:2026-10-08 UTC. Native focused slice only; full P9/P1-P12 and active goal remain OPEN.
[Stage](../../plans/enhancements/P9_DOCK_ORIGINAL_BINDING_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md),
[audit](../../plans/enhancements/COMPLETION_AUDIT.md).

## Current native boundary

Core2.327.0/save154/replay1, WorkBoard51/WorkSystem44/work-view93,
logistics25/logistics-view39/policy-component3. Tile dock content3; external point dock4.
Existing saved fields/layout unchanged; no compatibility reader/default, second job/queue/
resource/claim/geometry authority, inventory fallback, platform implementation or clock.

Executable source/content/tests frozen after the final run at2026-10-08 23:44:42UTC.
Only docs/audit metadata changes follow. Sole GameClock SHA256 remains:
35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1.

## Actual engine changes

WorkDockRules.ValidateResourceService is one shared content admission for compatible named
IResourcePortDefinition or existing IInletPort access. Native TileWorkDockDef and external PointDockDef
delegate, eliminating their duplicated unnamed-inlet assumptions. Every named-only served resource
must be accepted by an explicitly authored port; no fabricated local stock capability.

WorkSystem.RunJob recognizes the same already-acquired original-access loss during recovery
departure. Existing work/dock/motion rules still govern actual egress. A different loss, including
physical site destruction, still takes its own admission path. LogisticsService and WorkBoard retain
the original job/cargo/receipts and derive both old bay and selected exit claims from that job.

WorkBoard.ReadRecoveryStage requires actual retained-bay release as well as exit reach before
reporting WaitingForDestination. Geometry can settle after work execution in an Update; immutable
facts must not claim that the following native execution/claim release has already occurred.

## ONE NEW bounded exact case

Name: Named dock recovery: partial unloading, retained bay, physical queue, exit wait and saved replacement.

Normal master-listed scoped-dock-original-loss JSON uses only native tile dock geometry,
stock/transport/work policy and an existing named pool-port provider. Source warehouse30; cargo
capacity8; primary/queue carriers; named-only target with service21,10/wait20,12/exit14,10.
Coordinates and capacities are authored scenario facts, not engine size or fleet limits.

The thin driver uses ordinary commands and detached snapshots/views/diagnostics only:

1. Physically pick up8, interrupt via held command without deleting cargo, configure named First
   demand8 and replacement demand0, then return through native selection.
2. Actually unload positive prefix1 at the service bay, leaving real residual7. Select unrelated
   Second through normal routing: same job begins physical access-loss departure; old bay and exact
   original residual receipt remain. No future replacement promise or teleport.
3. Work policy pauses egress at the bay. An ordinary second carrier hauls2 to the current Second
   store, physically picks up its cargo and waits at the authored waiting tile. It cannot transfer
   through or claim the held bay. Saved twins retain original job/cargo/claims and queue.
4. Enable original worker policy. Observe intermediate actual movement and entity state moving.
   Bay remains claimed even when physically empty until actual exit is reached and departure settles.
5. Original carrier waits at real exit with cargo7/receipt7 and no replacement capacity promise.
   Freed bay lets queued carrier physically unload2 into Second. Its departure remains physical
   while the original carrier holds their shared exit.
6. Player stock policy requests observed remaining7, not8: same original job acquires exact
   residual replacement Fourth, then releases original receipt and moves/unloads normally.
   This also releases the shared exit so both original physical workflows can complete.

Final First1 + Second2 + Fourth7 + warehouse20 =30; both cargo stores empty, all actual inbound
claims0. Saved paused/queued/intermediate/exit/replacement twins settle identical hashes; normal
command replay verifies all motion, handling, policy, stock and claim transitions.
Every stage is bounded to1000 Updates with job/tile diagnostics on failure; each Step checks no
two carriers physically overlap the actual service bay.

## Actual build/runtime evidence

TWO builds, both0 warnings/0 errors:

- First16.50s; live session70529 observed through write_stdin until actual exit0.
- FINAL8.69s, exit0.

ONE NEW exact case, TWO attempts:

| Attempt | Passed/failed | Case time | Actual observation | Diagnosis |
|---|---|---|---|---|
| FIRST | 93/5 | 1546ms | PID40184 exit1, observed1729ms | Driver expected state key haul.moving rather than published state moving; native ReadRecoveryStage reported waiting before actual bay release; player desired8 legitimately attracted one extra auto delivery, moving Second2 to Second1/Fourth8. |
| FINAL | 98/0/0aborted | 1386ms | PID18632 exit0, observed1540ms | Native diagnostic requires actual claim release; driver observes published state and requests actual residual7. All physical stages, claims, saved twins/replay/conservation assertions retained/pass. |

All stdout/stderr untruncated, observer error null, actual process exited. No OS timeout or forced
termination. No other runtime, family/full/historical suite, Oil consumer or platform/Draw execution.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Named dock recovery: partial unloading, retained bay, physical queue, exit wait and saved replacement" --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

## Explicit remaining limits

External point dock resource-admission delegation compiled only, not a point-space runtime proof.
Native tile named-access departure/queue covered here, not all dock geometries/slot configurations/
fleet policies. Actual backing-store/provider destruction, arbitrary faults/corruption, fuel/weather/
emergency/route-congestion stress, global accounting, lockstep/shuffled inputs, allocation/soak and
full durability/P8/P9/P1-P12 acceptance remain OPEN. All older runtimes historical/not rerun on2.327.0.
No staging/commit/push; the user's full goal AND plan completion/publication condition is unsatisfied.
