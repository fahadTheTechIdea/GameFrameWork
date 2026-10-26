# P9-D5m verification: external point named dock and native continuation

Date:2026-10-09 UTC. Focused external engine-provider slice; full P1-P12/active goal remain OPEN.
[Stage](../../plans/enhancements/P9_POINT_NAMED_DOCK_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md),
[audit](../../plans/enhancements/COMPLETION_AUDIT.md).

## Current boundary and actual changes

Core2.328.0/save154/replay1/logistics26/WorkBoard52/WorkSystem45/work-view94/logistics-view40
remain unchanged: no core scheduler, resource ledger, codec fields or clock was added or altered.
External point space schema5, point motion2, point dock5; native tile dock remains3.
Existing point saved fields unchanged; no compatibility reader.

PointSpace.CheckMobileVacancy owns one actual machine/worker occupancy scan. PointMotion checks
the whole proposed movement segment before changing actual position, so it neither overlaps nor skips
past another physical carrier; waiting retains motion intent and retries on ordinary Update.
PointDock.CheckVacancy delegates to that same scan for an exact service position. This is engine-side
external geometry/physics, not a host/player-driver completion rule.

The new composition exposed duplicate PointStartDef registration in EndpointProofModule and PointModule.
The endpoint module no longer registers endpoint_starts or adds PointStart world generation: it owns
only actual pool providers/probes. EndpointGame now explicitly selects native WorldSetupModule and
endpoint-startup.json; endpoints.json/master directly migrated. No suppression flag, second startup
class, compatibility section or duplicate initializer. The endpoint-only fixture compiled, not rerun.

Five canonical shared JSON files now hold reused game-mode/time/state/resource/capacity/role/handling,
source pools, recovery pools/policies, original-binding policy, and dock work policy. Definitions were
removed from the five original scenario documents, not copied.13 scoped masters explicitly reference
their applicable shared files. Point master selects only shared/common content plus its point scenario,
never tile-only navigation/entities. Tile compositions compile but were not rerun. A read-only authoring check of all13 migrated scoped
masters found zero missing documents and zero duplicate section/definition IDs. This is source evidence,
not a substitute for runtime composition or older-suite regression.

Executable source/content/tests frozen after the final run, observed at2026-10-09 00:12:02UTC.
Only docs/audit metadata changes afterward. Sole GameClock SHA256 unchanged:
35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1.

## One shared thin workflow, ONE NEW exact runtime

Name: External point dock: named original restoration, physical service and saved continuation.

ScopedHaulGame.Factory selects normal preloaded master-listed JSON, existing public PointModule,
pool-port provider and native work/logistics/transaction owners. Six entities are created once through
native WorldSetup; every actual presence is PointSnapshot; active modules exclude tile navigation,
money and Tycoon. Named-only target has no fabricated local inventory.

The same existing three-episode player driver runs both tile and point scenarios through immutable
geometry observation. No duplicated gameplay workflow, snapshot-synthesized tile, route/ETA formula,
direct world/stock/promise/progress mutation or host logic.

Point content: source8/sink28/far35, primary2/standby0, target22/service21/wait20,
primary exit25/alternative exit14, rate30units/hour, extent48. These are authored scenario facts,
not framework limits. One-dimensional traffic must be physically feasible: the initial copied standby4
and exit14 blocked real pickup/entry after proper collision enforcement. Explicit authored standby and
egress were corrected; the occupancy check was retained.

Three episodes exercise restoration while original partial unloading1/cargo7 egress is policy-paused;
restoration at actual settled exit; restoration with another carrier paused at the productive bay
and a later original target0. Same original job/carrier/receipt/earned handling/baseline survive.
No new capacity reservation or instant arrival/unload. Busy service holds original cargo/receipt
at actual exit; second carrier physically unloads2 and uses its authored alternative exit.
All episodes finish Original8, Other0/0/2, Fourth0, warehouse22/22/20, empty cargo/all inbound0.
Local30 conservation, saved twins and replay pass; every stage bound1000 Updates with geometry/job
diagnostics. Driver checks no actual two-carrier overlap in the service bay after each Update.

## Actual builds/processes — no other runtime

FIVE builds, all0 warnings/0 errors and actual exit0:
4.72s,4.60s,4.50s,4.66s,FINAL4.05s.

ONE NEW exact case, FIVE attempts:

| Attempt | Passed/failed | Case ms | Process observation | Diagnosis |
|---|---|---|---|---|
| 1 | 2/1 | 87 | PID9448 exit1/observed235ms | Duplicate PointStartDef registration. Endpoint startup migrated to native WorldSetup. |
| 2 | 2/1 | 72 | PID42392 exit1/observed212ms | Tile-only document required inactive tile provider. Canonical shared definitions extracted/master-listed. |
| 3 | 211/1 | 1217 | PID19640 exit1/observed1364ms | Physical point motion overlapped occupied service bay during home movement. Shared native point occupancy scan added. |
| 4 | 13/1 | 1124 | PID40928 exit1/observed1271ms | Enforced physical occupancy correctly blocked pickup behind held standby4; authored standby/egress corrected, physics retained. |
| FINAL | 221/0/0aborted | 1331 | PID28892 exit0/observed1477ms | All three external geometry/receipt/handling/physical service/saved/replay/conservation episodes pass. |

All stdout/stderr untruncated; observer errors null; actual processes exited; no OS timeout or forced
termination. No previous tile/endpoint/historical/family/full/Oil/platform/Draw runtime.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "External point dock: named original restoration, physical service and saved continuation" --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

## SOLID and limits

WorkBoard/LogisticsService/transactions remain their original authorities. Point geometry owns physical
occupancy, dock delegates, and modules only compose capabilities/content. Native WorldSetup owns actual
entity startup; no endpoint provider startup subtype. Shared JSON keeps one definition per selected
catalog. Existing player workflow is reused without scheduler modifications.

This is a one-dimensional external-provider proof, not every collision/parking/head-on/route,
dock/area/fuel/emergency/provider fault/stress/global accounting/durability/lockstep/soak matrix.
Endpoint-only and other shared-content migration runtimes compiled only, not revalidated.
Other PointStart-based proof-module startup paths remain outside this demonstrated endpoint conflict;
do not claim every proof startup has been migrated or full composition coverage.
Earlier runtime results, including tile D5l on the same core version, are historical/not rerun after
this source/content migration. Full acceptance and all required later gates remain OPEN.
No staging/commit/push until the full goal AND plan finish and are verified.

The development-lifecycle skill guided stage/evidence/master alignment and single-responsibility review.
User/repository instructions override its full-regression and early-commit recommendations.
