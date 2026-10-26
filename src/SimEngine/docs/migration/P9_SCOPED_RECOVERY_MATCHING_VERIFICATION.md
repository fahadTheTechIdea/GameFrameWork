# P9-D5f actual-endpoint recovery and same-site matching verification

Date: 2026-10-08UTC. Native slice focused-verified; full P8/P9/P1-P12 remain OPEN.

## Implementation and ownership

HaulResourcePorts.VisitPublicPorts is the one deterministic local/named public-port discovery path
used by the existing automatic source matcher and recovery destination selector. Actual endpoint
identity, not whole-site identity, excludes self-transfers. Distinct stores at one site now use the
same physical carrier, timed pickup, travel/reach, unloading and departure as other native hauls.

RecoveryDestinationFacts now requires actual source/destination endpoint IDs and the named target.
Native policy gap and priority use actual endpoint demand rows, including aliases at different sites.
The native preference/travel/site ordering adds ordinal port tie-breaking. Recovery offers retain
selected names/IDs and the existing TryAcquire passes them into actual jobs. Current port changes
do not relocate the admitted destination.

Cargo return acquires complete original cargo-out/destination-in claims through the existing
IResourceTransaction. The original reroute implementation now resolves the original destination
through ReadHaulEndpoint, resolves the replacement through PhysicalPort, and uses the same native
claim transactions. This reroute path is SOURCE/COMPILE ONLY in this slice, not runtime verified.
A missing original at a surviving site refuses replacement; it is never replaced by current selection.

The existing local evacuation intent remains authoritative. Named evacuation-origin policy is not
added or claimed. No second matcher, inventory, demand book, claim authority, scheduler, clock,
host gameplay, compatibility reader or new public command was introduced.

Normal scoped-recovery JSON and master declare the actual fourth public store, named far site,
evacuation stock, explicit demand-only recovery policy, world setups and two scenarios. Existing
scoped-haul/scoped-sources JSON supplies resource units, capacity, handling, navigation and machines.
The factory only composes/modules/loads files. The new exact test only simulates player commands,
immutable views, Update, save/load and replay. One older detached-facts constructor and the native
protocol declaration were migrated and compiled; older runtimes were NOT executed.

## Protocol

Core2.322.0; save153; replay1; logistics-service20; logistics-view35.
WorkBoard48/WorkSystem42/work-view91 and other native saved layouts unchanged.
Changing simulation identity rejects old saves normally; no compatibility path.
The sole GameClock SHA256 remains
35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1.

## Verification actually run

ONE compilation build: dotnet build Beep.SimEngine.slnx --no-restore.
Clean15.41seconds, zero warnings and errors.

ONE NEW exact case, FIRST attempt:

Scoped recovery matching: same-site physical supply, named cargo returns and evacuation

100 passed,0 failed,0 aborted in1717ms. PID23456 exited0; observed1895ms.
stdout/stderr untruncated; no observer error, OS timeout or forced termination.
Every Until stage is bounded to1000Updates and reports current native jobs and dispatch queue.
No historical, family, broad, full, Oil or platform runtime ran.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Scoped recovery matching: same-site physical supply, named cargo returns and evacuation" --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

Post-pass executable freeze observed: 2026-10-08 22:44:28UTC. Only documentation/audit changes follow.

## Physical proof

Player commands physically fund First with8units. An actual Second6unit stock demand at the same site
automatically assigns a real carrier. Source aliases choose a-stock stably; actual same-store aliases
refuse self-transfer. Assignment has zero loaded cargo. Timed partial pickup and saved continuation
conserve8units; completion leaves First2,Second6 and empty cargo.

An ordinary8unit pickup is interrupted through SetWorkerHeldCommand. Original job promises release,
all8 remain in actual cargo, and the cancelled Third destination receives none. Player bands request
Second3 at priority200, farther Fourth2 at100, and First3 at20. Held recovery physically waits.
The foreign company sees no queue; repeated observation does not create state.

The same native carrier returns3 to original Second, then2 to original Fourth, then3 to First.
Both current target selections change after acquisition, yet saved jobs/claims physically deliver to
the original actual endpoints. Native gaps cap each delivery. Ordinal aliases choose a-stock for First.
Final First5+Second9+Fourth2+warehouse14 equals the original30; cargo is zero.
Resolved recovery intent retires normally; saved twins and command replay remain identical.

The second normal JSON scenario evacuates an actual local6unit store into named Second.
Its current port changes to empty First after full physical pickup. Original Second receives all6
through timed unloading/departure; current First receives none. Saved twins and replay pass.

## Explicit remaining limits

Named reroute/dock egress and endpoint-loss paths were migrated/compiled only. Scoped evacuation
origins, external recovery/provider mutations, removed endpoints, purity fault injection, corruption,
optional journal absence/expiry, dock/area/emergency interactions, congestion/fuel/routes, stress,
allocation, global accounting, lockstep/shuffled input/full durability and full P8/P9/P1-P12 remain OPEN.
Local30unit accounting is not proof of global conservation across all modules.

No full phase acceptance checkbox was changed. Previous runtime checkpoints remain historical,
not rerun on2.322.0. Full goal remains incomplete; no staging/commit/push.

[Stage](../../plans/enhancements/P9_SCOPED_RECOVERY_MATCHING_DESIGN.md) ·
[Master tracker](../../plans/enhancements/MASTER_TRACKER.md) ·
[Completion audit](../../plans/enhancements/COMPLETION_AUDIT.md)

