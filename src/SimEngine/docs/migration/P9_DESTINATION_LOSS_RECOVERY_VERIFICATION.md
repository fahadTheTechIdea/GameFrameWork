# P9-C5d — Retained destination-loss recovery verification

Date:2026-10-07. Historical focused dirty framework checkpoint, not full completion.
Current source checkpoint: [C3g selected haul-cost authority](P9_HAUL_CANDIDATE_COST_VERIFICATION.md).
[Stage](../../plans/enhancements/P9_DESTINATION_LOSS_RECOVERY_DESIGN.md) /
[master](../../plans/enhancements/MASTER_TRACKER.md) /
[current source manifest](P9_DESTINATION_LOSS_RECOVERY_SOURCE_MANIFEST.json).

## Current identity and ownership

Build2.203.0/save109. WorkBoard39/WorkSystem39/logistics16/maintenance2/work view72/logistics view27;
native recurring8/public plan6/service5/company5 unchanged. No payload or format changes. The sole
existing Job keeps its shipment/worker/amount/creation, cargo-out and old missing inlet receipt,
committed baseline, elapsed work and optional dock-recovery provenance. Blocker/State represent
the wait; no new ledger, promise, retry timestamp, route cache, timer or scheduler.

LogisticsService checks actual missing destination and explicit selected policy permission for
core.not-found, including loss during an earlier differently caused dock departure. With no full
eligible output it handles recovery by retaining that job with DestinationUnavailable and stopping
obsolete motion. WorkSystem's preflight observes loss before trying to execute absent dock geometry.
Surviving docks still require physical departure. Existing selector rechecks complete destination
room, worker, policy, compatibility, areas, route, dock and trip eligibility. On readiness it promises
new full room before releasing any surviving old room, then admits actual new travel/service.

WorkBoard validates ordinary missing-output waits against actual loaded recovery, missing target,
completed pickup and explicit policy. ReadRecoveryStage derives immutable WaitingForDestination
without another store. Existing participant-loss validation permits genuinely removed receipt
owners pending normal execution; save/load/hash reuse the existing codecs. No compatibility reader.

## Final scoped gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Recovery destination loss: retained cargo, missing dock and saved waiting"
```

Final clean build:0 warnings/errors,3.96seconds. Exactly one new named scenario:96 passed,0 failed/
aborted,913ms. Other suites compile only;81 old build-identity assertion files migrate mechanically.
No previous/family/full/platform/Draw suite. No automatic per-edit tests.

First build was clean; first execution passed95 assertions then failed fixed-haul fixture admission:
its enabled automatic evacuation had already taken the carrier. That fixture now explicitly selects
recovery.off before normal manual acquisition. Needed rebuild and only the same failing new scenario
were rerun. This was fixture setup, not a runtime fallback or privileged resource grant.

## Proven behavior

Six physical fixtures combine native return, public separate-inventory return and loaded evacuation
with non-docked partial service or actual dock departure. All first earn a real delivered prefix and
unfinished handling. Normal CheckDespawn refuses removal of protected participants. Explicit internal
participant-loss injection removes the destination only to exercise the runtime failure protocol.
This is not an authorized player destruction API or a game-side incident implementation.

Lost-target checkpoints restore before recovery execution. Full alternate stock or a real owned
competing inbound reservation prevents complete output admission. The same original job waits;
actual cargo receipt, old receipt provenance, amount/creation and unfinished work remain. Native/public
saved twins agree through repeated stationary updates. The one clock advances without stock transfer,
phantom dock movement or service credit. Private detached snapshots show the wait; another company
cannot read the job. A missing recovery provenance is rejected by the existing validator.

Consuming existing alternate stock or releasing its owned competing room promise makes a real output
feasible. Same job rechecks, promises complete remaining room and resets only unfinished old service.
Its original delivered prefix remains the unloading baseline. Saved retargets perform actual travel,
timed unloading and normal departure; surviving cargo finishes once and owned claims/baseline/cause/
bay clean normally. Destroyed endpoint stock is never recreated or counted as surviving delivery.

Existing interruption cancels a retained wait and releases only surviving claims, leaving actual cargo
intact; the cancelled state restores identically. Fixed player hauls and explicitly empty reroute lists
cannot inherit the new wait. All movement/dispatch/service decisions occur in engine Game.Update.

## Boundaries and remaining scope

Exactly one unchanged GameClock construction. GameClock.cs SHA256:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
The lifecycle skill kept the existing owners, stage and scoped evidence aligned with the master.

This proves point-space participant-loss recovery, not a general industrial destruction system.
Source/carrier destruction and physical cargo-loss accounting, arbitrary provider failure signals,
fuel/maintenance/shared-fleet combinations, cancellation-origin routing, wider geometry/congestion,
global purity/replay/stress/allocations and full acceptance remain open. No Oil/Tycoon/host/demo/
Unity/Godot/Draw/adapter changes. Whole plan and persistent implementation goal remain incomplete.
