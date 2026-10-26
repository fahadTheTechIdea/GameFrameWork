# P9 — Recovery departure route retry and obsolete selector removal

## 0. Problem statement

Dock departure now precedes replacement admission, but private `viaRecoveryDeparture` parameters
and their prospective-output branch remain unused in `Standard/Logistics.cs`. The previous focused
scenario did not close route access during demand-only departure. Remove the dead alternative and
prove that ordinary engine commands cannot release the bay or move cargo through a blocked exit.

## 1. Goals

1. One destination preview starts from the actual carrier; no unused future-egress selector remains.
2. Departure admission selects only the actual exit, independently of future output demand.
3. A player route-access change pauses real motion while retaining the same job, bay, cargo, inlet
   promise, unfinished handling and chronology. Saved twins remain identical.
4. Reopening resumes actual departure; insufficient demand waits at the real exit, then full demand
   retargets and physically completes exactly once.

## 2. Design

Remove `viaRecoveryDeparture` from private `LogisticsService.Preview` and `PreviewDestination`.
Returning-cargo previews call the ordinary destination dock selector from the actual location.
Narrow `WorkBoard.PreviewRecoveryDeparture(Job,Entity,out Code)` to actual exit admission; remove
unused continuation/cost/travel outputs and prospective retained-exit logic. Existing `SelectDock`
still owns all dock policy, claim, occupancy, geometry and route checks. Existing job/WorkSystem/
motion/resource owners remain unchanged; no second planner or clock is introduced.

Reuse `DockDemandWait` with an explicit route-interruption option, not a duplicate workflow. The new
named scenario uses the existing master-listed demand-only JSON and dock fixture for native and
public separate-inventory cargo. After movement begins between bay and exit, issue normal
`SetPointAccessCommand`, observe bounded updates/save/load, reopen, then continue the existing
demand wait/retarget/unload assertions. No direct Reserve/Take/replan/failure-signal instrumentation.

## 3. API surface

Only private/internal signatures change. No public API, command, view, saved payload or content
schema changes. Runtime identity remains2.219.0/save123: removed code was unreachable after the
previous migration; no compatibility reader or alias is retained.

## 4. Files to change

- `src/Beep.SimEngine/Standard/Logistics.cs`: remove private flag and unused branch.
- `src/Beep.SimEngine/Standard/LogisticsRecovery.cs`: call narrowed departure admission.
- `src/Beep.SimEngine/Standard/WorkDocks.cs`: narrow exit preview and correct selector comment.
- `tests/Beep.SimEngine.Tests/RecoveryDispatchTests.cs`: extend the same thin-player workflow.
- Tracker, README, AGENTS and scoped evidence/inventory: record actual verification.

## 5. Error handling

Route closure must retain actual job/claims/progress and expose the native motion blocker. It must
not invent destination capacity, advance unloading, teleport to exit or silently discard cargo.
Scenario timeout remains360 updates with actual position/job/toil/cargo/blocker diagnostics.

## 6. Compatibility

Development-only direct removal. No old overload, alias, fallback or parallel selector.

## 7. Verification

One completed-slice build, then ONLY the new exact case:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Dock demand recovery: blocked egress preserves claims through saved retry"
```

Repair reruns only for observed failures. No older/family/full/platform suites. Read-only source,
link, hash and clock checks are not runtime gates. Full framework regression/acceptance remain open.

## 8. Risks

Motion arrival precedes bay-release update; observe both rather than force instant release. Player
commands advance game time, so freeze assertions use actual state after closure. No stale binary.

## 9. Out of scope

Fuel/endpoint loss/congestion/adversarial-provider/replay/stress/full acceptance, platforms/Draw,
hosts/demos/OilFieldDays, new state owners, economics and publication before full completion.

## 10. Execution

- [x] Remove the unused prospective path and narrow departure preview.
- [x] Add the new route-interruption variant using the existing workflow.
- [x] One clean build0 warnings/errors3.88s; ONE exact new scenario48 checks0 failures/aborts391ms,
  first attempt. No rerun, older/family/full/platform gate or post-gate gameplay edit.
- [x] Update authoritative tracker and [evidence](../../docs/migration/P9_DOCK_RECOVERY_ROUTE_RETRY_VERIFICATION.md);
  keep the full goal active. Scoped route-access proof does not close wider recovery/acceptance.
