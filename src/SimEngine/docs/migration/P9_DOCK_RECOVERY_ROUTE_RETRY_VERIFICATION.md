# P9 — Recovery route retry and obsolete selector removal

2026-10-07. One clean build, one exact new bounded scenario, first attempt:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Dock demand recovery: blocked egress preserves claims through saved retry"
```

Build0 warnings/errors3.88s. Scenario48 passed,0 failed,0 aborted suites391ms. No repair/rerun,
stale binary, older/family/full/platform run or post-gate gameplay/test edit. Identity2.219.0/save123,
existing payloads/logistics view28/work view76 unchanged. No metadata migration: removed private
prospective-output branch was unreachable after the preceding dock migration.

`LogisticsService.Preview`/`PreviewDestination` no longer take viaRecoveryDeparture. Returning
destination admission uses actual carrier location. `WorkBoard.PreviewRecoveryDeparture` returns
only real exit admission/slot through existing SelectDock; unused continuation/cost/travel and
prospective retained-exit logic are removed, with no old overload or new state owner.

The same `DockDemandWait` thin-player workflow adds an explicit interruption variant. Existing
master-listed demand-only JSON and dock fixture are reused. Native/public separate-inventory cargo
partially unloads, then player area policy starts departure with insufficient alternative demand.
Normal route-access commands close while the machine is physically between bay11 and exit12.
Bounded updates retain actual position/bay/job/age/cargo/inlet receipts/unfinished handling while
the one game clock advances. Saved blocked twins remain identical and transfer no cargo. Reopening
resumes real exit movement; only actual arrival/native release clears bay. Full-demand player
command later retargets the same job, physically unloads and clears claims, conserving6 units.
No direct Reserve/Take/replan/failure-signal probe or host-owned gameplay is used in this scenario.

Each Reach is bounded360 updates with actual position/job/toil/cargo/blocker diagnostics. This proves
route-access closure/reopening for the authored native/public fixtures, not arbitrary occupancy,
alternative exits, endpoint loss, fuel, congestion, provider/global-purity, replay/stress, advanced
or full framework acceptance. Development-lifecycle/SOLID review retained sole execution owners.
No new clock/API/payload/loader/policy store/economics or platform/Draw/host/demo/OilFieldDays changes.

[Design](../../plans/enhancements/P9_DOCK_RECOVERY_ROUTE_RETRY_DESIGN.md),
[tracker](../../plans/enhancements/MASTER_TRACKER.md),
[scoped source inventory](P9_DOCK_RECOVERY_ROUTE_RETRY_SOURCE_MANIFEST.json) (excludes itself).
Historical reports/inventories stay unchanged. Read-only hash/link/clock/diff checks are not
runtime gates. Inventory235 unique files/35 Markdown documents/1,207 local links; final verification
requires0 hash mismatches/broken links. `git diff --check` exited0. Sole GameClock source unchanged:
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.
Full goal/plan remain active; commit/push only after FULL completion/verification.
