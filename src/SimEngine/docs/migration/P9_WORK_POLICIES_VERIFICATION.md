# P9-B1 — Worker policy focused verification

Date: 2026-10-05. Working-tree identity: **2.53.0 / save27 / work12 / work-view12 /
logistics-view4**, work_policy component codec1, resource-flow4 unchanged.
Scope: standalone Beep.SimEngine only. [Stage](../../plans/enhancements/P9_WORK_POLICIES_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md). No compatibility reader or second scheduler.

## Final gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Work policies: strict content, private commands and shared destination admission"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Work policies: physical pause, protected cargo, incident eligibility and saved controls"
```

Build: **0 warnings / 0 errors**, 2.11 seconds. Only the same two directly affected named suites
ran during development. Final result: **147 checks / 2 suites / 0 failures / 0 aborted suites**.

| Exact suite | Passed | Runtime |
| --- | ---: | ---: |
| Work policies: strict content, private commands and shared destination admission | 59 | 162 ms |
| Work policies: physical pause, protected cargo, incident eligibility and saved controls | 88 | 309 ms |

Total reported suite runtime: 471 ms. Previous target-region/resource reports are historical, not
rerun here. No full/broad regression, OilFieldDays, replay/lockstep, stress, soak or allocation gate ran.

Scoped `git diff --check` over edited tracked runtime/test/tracker files reports no whitespace errors
(only repository LF-to-CRLF notices). An explicit trailing-whitespace scan of new policy/runtime/
fixture/stage/evidence files finds none. Runtime source search confirms no CanAssign path remains;
CheckWorker is private and called only by shared destination-aware CheckAssignment. These source
checks are not additional test-suite evidence. Documentation updates did not trigger another test run.

## Source and ownership

WorkPolicies owns explicit JSON profiles, immutable settings, the IWorkPolicy contract and its one
component codec. ConfigureWorkPolicyCommand and WorkView use the same pure contract admission.
WorkSystem remains the sole worker selector/executor; LogisticsService remains the sole carrier
matching/acquisition owner. Destination-blind CanAssign is deleted, not wrapped. CheckAssignment
checks actual workers/roles and all proposed physical stops before native receipt acquisition.
Both selectors compare exact machine preference before travel cost/stable identity.

Restrictive edits stop actual motion and pause the existing job with a saved typed blocker, retaining
its worker relation, receipts, cargo and progress. Loaded hauls check only remaining destinations;
the visited pickup does not become a new delivery restriction. WorkBoard's assigned readiness uses
the same CheckJob so its later phase cannot erase a policy blocker. Restore accepts paused hauls
only with a recognized policy wait and still checks actual claims/toils/worker/native quantities.
Manual commandeering checks interruptibility; explicit order cancellation is a different command.
Automatic home return checks enabled/area intent, including policy edits during an existing return.
Entity.Update commits the profile's authored disabled state, not a command/test/host state setter.

## Focused proof

- Strict required fields, null-area semantics, missing/unknown roles and regions, negative priorities,
  duplicate role identities and canonical immutable settings. Explicit empty roles permit no jobs.
- Private detached settings/job facts, foreign denial clearing output, pure preview/command parity,
  cross-session stop rejection and command serialization including maximum integer priority.
- Alternate compatible role selection; enabled/role/area refusal through shared work/haul admission;
  all pickup/delivery stops preflight before acquisition and refusal without hash/receipt mutation.
- Per-machine preference for both work and hauling, before actual travel cost; no numeric score cap.
- Real travel/loading, loaded-cargo pause without creep/spending, retained source/destination/cargo
  claims, corrupt paused-haul refusal, same-job physical delivery and saved twin identity.
- Assigned ordinary work preserves progress through area pause/resume and saved continuation.
- Actual bound incident response respects eligibility; enabling it permits physical response through
  public-only external worker contracts. Ordinary work is not inferred to be an emergency.
- Noninterruptible live work refuses both move-preview and direct dispatch interruption without
  stealing its claim. Explicit cancellation remains possible and does not refund spent inputs.
- Home outside the assigned area does not receive automatic return; enabling the destination starts
  real travel, restrictive edits stop that travel, and later permission reaches the actual home.
- Tile and public-only non-tile region providers; existing master-listed proof content, no test-owned
  matching/resource/progress algorithm. Physical waits are bounded with job/tick/blocker diagnostics.

## Remaining gates

This is not complete P9. Company alert policy/global emergency ordering, starvation/aging/preemption,
persistent plans/templates, editable/versioned areas, route containment, all-dispatch/service/source
area enforcement, stock bands/reserves, timed docks/congestion, fleet fuel/service controls, automated
recovery and broader fault/replay/stress/performance acceptance remain open. Machine preference is
not a new global job ordering algorithm. Non-policy machines use their actual capabilities without
an inferred policy profile. No platform adapter, Draw implementation, demo or game-domain changes.
