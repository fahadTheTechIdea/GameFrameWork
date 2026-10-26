# P9-D5g original named recovery claim lifecycle verification

Date: 2026-10-08UTC. Native slice focused-verified; full P8/P9/P1-P12 remain OPEN.

## Native changes and ownership

LogisticsRecovery.TryReplanDestination now reads the original InReservation endpoint through the
existing IResourceTransaction.ReadClaims independently of the participant site's existence. An actual
surviving book must contain the exact original owner/direction/resource/Loaded amount. After acquiring
the complete replacement claim, it releases that surviving original claim even if geometry is gone.
A genuinely MissingEndpoint remains provenance, never a substitute inventory.

WorkBoard.CheckHaulReceipt still validates current named binding while the participant survives.
After site loss it now validates the actual surviving endpoint book rather than skipping it entirely.
ResourceReservationBook remains the existing exact receipt matcher. No claim authority, inventory,
registry, scheduler, command, compatibility reader or clock is added.

Normal scoped-reroute JSON/master author explicit construction effects, positive-duration native
build/removal DAGs, policy-capable hauler, physical demolition machine, removable named site, recovery
permission and painted operating area. The authoring contains1535 explicit tiles excluding the target;
this finite scenario is NOT an engine world-size restriction. The factory only composes and loads
files; the test only issues player commands and reads immutable views, Update, saves and replay.

## Protocol

Core2.323.0; save153; replay1; WorkBoard49; WorkSystem42; logistics-service21.
Existing work-view91/logistics-view35 and serialized layouts remain unchanged.
Old engine identities reject normally; no old reader or compatibility shim.
The sole GameClock SHA256 remains
35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1.

## Verification actually run

THREE clean compilation builds: dotnet build Beep.SimEngine.slnx --no-restore.
13.81s,8.32s,FINAL3.92s; all zero warnings/errors.

ONE NEW exact case, repaired/rerun only:

Scoped recovery lifecycle: original claim reroute, physical site loss, waiting and cancellation

| Attempt | Passed | Failed | Case elapsed | Process |
| --- | ---: | ---: | ---: | --- |
| 1 |2|1|290ms|PID27060, exit1, observed476ms|
| 2 |164|3|4955ms|PID5112, exit1, observed5114ms|
| FINAL |167|0|4512ms|PID26948, exit0, observed4657ms|

Attempt1 threw ContentException because the new JSON lacked explicit finish/demolish construction
effect declarations. Remaining case steps were not executed. The normal authored declarations were
added; no inferred/default engine effect was introduced.

Attempt2 completed all physical paths but three assertions expected demand port tank instead of the
correct ordinal admitted alias. Expectations were corrected, and the player command was strengthened
to change the actually admitted alias selection. No engine selection shortcut was added.

FINAL has zero failed/aborted suites. All process outputs untruncated; no observer errors,
OS timeouts or forced termination. Each Until stage has a1000-Update bound and actual job diagnostics.
No older, family, broad, full, Oil or platform runtime ran.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Scoped recovery lifecycle: original claim reroute, physical site loss, waiting and cancellation" --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

Post-pass executable freeze observed: 2026-10-08 22:55:38UTC. Only documentation/audit changes follow.

## Focused physical evidence

Each scenario begins with30 authored warehouse units, physically picks up8, and interrupts the
ordinary job through the player-held command. Native cargo retains8 and warehouse22. Native demand
then admits a complete original return to First; ordinal alias is retained separately from demand
name tank. The player edits the admitted alias to Second; original claim remains First.

Area reroute: a native work-policy change excludes original physical target geometry while allowing
the real replacement route. The SAME job/carrier retains Loaded8. Original First promise releases;
unrelated current Second is untouched; Fourth receives one complete8unit inbound promise. Actual
movement/timed unloading/departure delivers8, not admission. Saved twins and command replay pass.

Physical site loss: the hauler's disabled policy retains its live job while an ordinary demolition
machine travels and earns positive-duration native removal work. The named site disappears only
after actual completion. Independent First storage and its original8unit receipt survive. A3unit
alternative cannot receive a partial8unit replacement. Native DestinationUnavailable waits save/load
and continue without changing cargo or the original receipt.

Replacement path: enabling the worker while capacity remains3 continues waiting. A normal stock-band
command makes the full8unit replacement feasible; the same job reroutes to Fourth and releases original
First's surviving promise despite its site loss. Saved physical delivery empties cargo and both
original/replacement promises. Warehouse22+Fourth8 equals original30. Replay passes.

Cancellation path: the ordinary player-held command ends the lost-site job, releases its actual
surviving original promise, and retains8 cargo. Normal policy/band/player-unhold commands create a new
return execution, never silently resume the cancelled ID. Physical delivery/save twins/replay and
original30unit conservation pass.

## Remaining limits

This verifies policy-area reroute and PHYSICAL SITE loss with a SURVIVING independent provider.
It does not prove provider endpoint destruction/unavailability, corrupt/missing/foreign claim injection,
provider mutation matrices, dock egress/partial unloading reroute, named evacuation origins,
broad area/emergency/congestion/fuel/route stress, allocation, global accounting, lockstep/shuffled
input/full durability or full P8/P9/P1-P12. Local30unit accounting is not global engine accounting.
Prior runtimes remain historical/not rerun on2.323.0; prior docs saying named reroute source-only refer
to their historical checkpoint, superseded only for the exact paths described here.

No full phase checkbox promoted. Full goal remains incomplete; no staging/commit/push.

[Stage](../../plans/enhancements/P9_SCOPED_RECOVERY_LIFECYCLE_DESIGN.md) ·
[Master tracker](../../plans/enhancements/MASTER_TRACKER.md) ·
[Completion audit](../../plans/enhancements/COMPLETION_AUDIT.md)

