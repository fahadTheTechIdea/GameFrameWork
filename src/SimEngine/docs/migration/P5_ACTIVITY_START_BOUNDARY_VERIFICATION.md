# P5-A35 focused verification

Core beep-simengine-2.551.0; save156/replay1 unchanged. Gameplay change is confined to the existing activity-start boundary. Oil, Tycoon, clock and client production sources are unchanged in this stage.

## Builds and attempts

Framework test project builds: first failed with two missing GameClock namespace errors (6.05s); second failed with one incorrect proof enum name (15.00s); third clean (10.01s). The first new runtime had19 passed/1 failed in636ms, observer935ms, PID7320: its observer incorrectly expected CopyOrders to include completed orders. Native work had already completed. The observer now checks native active-order count and public remaining stock.

Final framework build clean, 0 warnings/errors,6.04s. New exact case `Activity start rules: pure binding admission and fail-closed settlement`: FINAL55 passed/0 failed in494ms; observer896ms; PID24092; exit0, untruncated.

Affected Oil PublicProof build clean, 0 warnings/errors,2.28s. Exact case `Oil native research: authored DAG facility gates plans fees and saved knowledge`: FINAL95 passed/0 failed in25029ms; observer25230ms; PID31980; exit0, untruncated.

Both cases used60000ms execution timeout,5000ms termination timeout,20000-character output bounds. Two focused suites,150 final passing checks; no aborted suite. No executable edits after these final runs. No full/family regression.

## Scope of proof

The independent provider is registered in a separate non-friend assembly and authored through JSON listed by the actual master manifest. Negative instrumentation alone deliberately violates content/callback contracts; it is not normal mod gameplay.

Native timed work remains resource-only without Tycoon, starts once, consumes two authored kits once, saves partial work and continues identically. Pure queries do not commit. Getter clock mutation, preview clock mutation/private target mutation, and callback binding replacement latch the original session fault. Direct module settlement throw/refusal/binding replacement cannot be caught and followed by a healthy save. First callback exception is retained.

The affected actual Oil case verifies ordinary research plans, facility gates, fees, poor-company refusal, once-only charging and saved native knowledge. Authored amounts and business rules were not changed.

This is NOT proof of arbitrary private CLR purity, unrelated-service purity, accounting rollback/atomicity for every provider, all client workflows, full physical research power/recovery/rival paths, global performance, lockstep/soak, or all27 original open gates.

## Tracker

No original gate closed or wording changed. Original221 /194 recorded checked /27 open; additional299. Full goal/plan incomplete. No staging, commit or push; user requires those only after full completion. [Stage](../../plans/enhancements/P5_ACTIVITY_START_BOUNDARY_DESIGN.md).

