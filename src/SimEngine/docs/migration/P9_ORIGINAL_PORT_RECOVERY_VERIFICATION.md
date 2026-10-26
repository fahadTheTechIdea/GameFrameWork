# P9-D5j verification: original named-port access loss

Date: 2026-10-08 UTC. Native focused slice only; full P9/P1-P12 and active goal remain OPEN.
[Stage](../../plans/enhancements/P9_ORIGINAL_PORT_RECOVERY_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md),
[audit](../../plans/enhancements/COMPLETION_AUDIT.md).

## Current boundary

Core2.326.0/save154/replay1; WorkBoard50/WorkSystem43/work-view92;
logistics24/logistics-view38/policy-component3. Existing saved fields/layout remain unchanged.
PoolPort definition semantic schema3 requires explicit original_access retained/current_only.
Eleven existing JSON files/45 port rows explicitly migrated to retained; the new target authors
current_only. No legacy reader/default/shim, new stock ledger/scheduler/clock or host gameplay.

All executable source/content/tests frozen after the final run at2026-10-08 23:33:51UTC.
Only documentation/audit metadata changes follow that freeze. Sole GameClock SHA256:
35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1.

## Native implementation

IHaulExecution.CheckDestinationBinding delegates to the existing exact PhysicalPort resolution.
It reports core.not-found for removed geometry and haul.original-destination-binding-lost for
unavailable explicit-original access. Recovery requires the respective explicitly authored blocker.
WorkSystem observes this before loaded delivery; the original replan owner retains geometry-based
dock departure and acquires a complete residual replacement before releasing the old actual receipt.

WorkBoard validates original token endpoint identity independently of physical access, then proves
surviving records through IResourceTransaction.ReadClaims. Missing access never authorizes using
the current selected store. EarnedHandling retains exact authored port packing if access disconnects.
ValidateLoadedDemands similarly retains original stock intent and uses the shared CheckStockCapacity
owner; current-binding admission rules remain strict. SyncRecoveries restores an intentionally absent
cargo-return SourcePort as Code.None, not an invalid Code(null).

## ONE NEW exact runtime case

Name: Original port recovery: partial unload, saved unavailable wait, restoration and cancellation.

Normal master-listed scoped-original-port-loss JSON, ordinary routing/stock/held/haul commands,
immutable diagnostics/WorkView, Game.Update, native saves and replay only. Composition factory has
no stock/claims/job/progress writes; the driver contains no gameplay implementation. Every stage
is bounded to1000 Updates and prints job state on failure.

Three scenarios inside that ONE case:

1. Physically pick up8 from original warehouse30 and interrupt through held command. Return to the
   named First store, actually unload a positive prefix, then change the port to Second. Same job/
   carrier retain real residual; replacement Fourth receives only that residual. Original completed
   prefix remains, original surviving receipt releases, unrelated Second stays empty/unpromised.
2. Disconnect before unloading while Fourth demand3 cannot fit actual cargo8. Original full receipt
   remains and recovery pauses with WaitingForDestination, not a fault/partial claim. Saved twins
   retain cargo/handling progress. Restore First through the normal routing command: same job and
   original receipt resume real movement/unloading, with no new promise or instant delivery.
3. Save/continue the same no-capacity wait, then ordinary held cancellation releases inaccessible
   original receipt without deleting cargo. Raise Fourth demand8 and unhold: a new native return
   physically moves/unloads all cargo. Cancelled execution is never silently resumed.

Every completed scenario proves no claim leakage, exact original/replacement stock amounts,
unrelated store empty, local30-unit conservation, private company queue, saved twin hash identity
and normal command replay.

## Actual attempts and diagnostics

FIVE builds:6.68s,3.95s,6.11s,5.09s,FINAL5.21s. All0 warnings/0 errors.
No runtime runs beyond this exact new case; failures repaired/rerun only here.

| Attempt | Checks passed/failed | Case time | Actual observation | Diagnosis |
|---|---|---|---|---|
| 1 | 10/1 | 446ms | PID39368 exit1, observed598ms | Driver tried to find an ended/pruned job; assertion now checks absence among live jobs. |
| 2 | 25/1 | 529ms | PID39848 exit1, observed700ms | Real recovery serializer reconstructed absent cargo-return source port as Code(null); fixed native Code.None restoration. |
| 3 | 39/1 | 506ms | PID21488 exit1, observed651ms | Real saved-demand validator rejected legal disconnected original access; shared authored-packing validation now preserves original intent. |
| 4 | 158/0 | 1630ms | PID2700 exit0, observed1794ms | Initial completed native partial/wait/restoration/cancel/save/replay proof. |
| FINAL | 172/0/0aborted | 1513ms | PID23072 exit0, observed1656ms | Added unconditional original receipt endpoint check plus precise prefix/residual/worker/unrelated-store assertions. |

All outputs untruncated, observer error null, actual process exited; no OS timeout/forced termination.
The final run verifies all final executable changes, including strengthened receipt identity guard.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Original port recovery: partial unload, saved unavailable wait, restoration and cancellation" --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

## Limits

This is lost physical original-port access with a surviving actual backing store/book, NOT provider
destruction. Arbitrary provider loss/fault/corruption, dock/egress/service/fuel/congestion/emergency
stress breadth, global accounting, lockstep/shuffled arrival, allocation/soak and full durability/
P8/P9/P1-P12 acceptance remain OPEN. All older suites compiled only, not rerun on2.326.0.
No Oil/Tycoon/platform/Draw runtime or implementation. No staging/commit/push; full goal AND plan
completion/publication condition remains unsatisfied.
