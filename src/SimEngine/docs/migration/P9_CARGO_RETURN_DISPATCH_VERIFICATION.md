# P9-C3e — Shared recovery/cargo-return verification

Date:2026-10-07. Historical C3e dirty checkpoint, before the C3f observation migration.
Current evidence: [C3f](P9_CARGO_BINDING_OBSERVATION_VERIFICATION.md). Not full regression or full-plan completion.
[Stage](../../plans/enhancements/P9_CARGO_RETURN_DISPATCH_DESIGN.md) /
[master](../../plans/enhancements/MASTER_TRACKER.md) /
[current source manifest](P9_CARGO_RETURN_DISPATCH_SOURCE_MANIFEST.json).

## Identity and boundary

Build2.198.0/save106/board36/work36/logistics16/maintenance2/work view69/logistics view27.
Native recurring8/public recurring5,5,6 unchanged; existing resource/job payloads unchanged.
Recovery kind and chronology are embedded once in existing LogisticsService.Sync.
No legacy reader, compatibility facade, second dispatcher, stock mirror or new clock.
No OilFieldDays/Tycoon/platform/Draw changes.

## Actual commands and results

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Recovery dispatch: authored policy, private chronology and shared ranking"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Recovery dispatch: physical conservation, cleanup and saved contention"
```

| Final gate | Result |
|------------|--------|
| Solution build | 0 warnings,0 errors;3.93seconds |
| Authored policy, private chronology and shared ranking |85 passed,0 failed,0 aborted;420ms |
| Physical conservation, cleanup and saved contention |95 passed,0 failed,0 aborted;633ms |
| Combined assertions |180 passed;only these two exact suites executed |

Assertion counts are checks, not180 independent workflows. These same suites were rerun during
development. No third, family, full regression, platform or Draw gate ran. Other suites compile only.

## Observed behavior

Strict both-kind JSON and explicit master/scenario profile selection; native/public-only providers;
absent/disabled kinds; pure company-private pending views; original saved age; priority/attempt
settings; wrong-carrier and stale-acquisition refusal; full-output saved waits; protected stock bands.
Return jobs bind the actual worker/cargo with real cargo-out and destination-in promises, no invented
pickup. Low/high priorities compete with ordinary work in the shared scheduler.
Actual timed movement, unloading and departure conserve stock. Retained operating reserves stay.
Simultaneous evacuation and return run through the same pass and physically deliver both.
Another provider's cargo promises remain protected, including saved continuation and normal release.
Partial unload interruption preserves the delivered prefix, releases only own claims, retains active
age, and re-admits residual cargo through normal enabled policy. Native/public twins agree.
Earlier evacuation conservation/removal-guard regressions remain included in these two suites.

Foreign reservation/release and WorkSystem.Interrupt calls are explicit protocol instrumentation.
Tests do not implement selection, movement, delivery, repair, elapsed-time or conservation rules.

## Development failures, now repaired

The earlier handoff failed CS1579 by directly enumerating Bill; current code reads Bill.Lines.
A test fixture accidentally left an empty nearby source Public=true, making it a valid nearer output;
the authored fixture was corrected to Public=false rather than altering valid engine selection.
A receipt assertion used the wrong signature (CS1501); it now reads the actual inventory contract.
No no-build tests were used as evidence after failed builds.

## Sole game-clock verification

GameClock.cs was not edited. SHA256:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
Exactly one construction remains in Kernel/World.cs. Game.Clock and World.Clock reuse that owner.
StepIndex/Tick are read-only coordinates, not clocks. Pause and saved twins are focused proof.

## Explicit exclusions and continuation

CargoRecoveryTests, ResourceDispositionTests, TimedHandlingTests, HaulTripPolicyTests and
ItineraryDurationTests migrated to current public content/protocol and compile; their separate
suites were not run. Metadata-only migrations are compilation evidence, not behavioral reruns.
Mixed-resource/alternate-inventory providers, home/service/emergency interactions, larger fleets,
congestion, endpoint-removal combinations, global provider purity, replay, performance/allocation,
soak and full framework acceptance remain open. Historical C3d reports/manifests are unchanged.
