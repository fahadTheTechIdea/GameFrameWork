# P4-G2h mixed productive scheduling — scoped verification

Verified2026-10-08. Build2.291.0 / Save141 / Replay1 / WorkView82.
This is a native scheduling slice, not a full engine/plan/regression completion claim.
[Stage](../../plans/enhancements/P4_MIXED_PRODUCTIVE_SCHEDULING_DESIGN.md),
[master tracker](../../plans/enhancements/MASTER_TRACKER.md),
[source manifest](P4_MIXED_PRODUCTIVE_SCHEDULING_SOURCE_MANIFEST.json).

## Implementation boundary

Existing WorkSystem collects actual current-call machine phase proposals and acquired timers,
then ranks across orders through original RankOffer/CompareAssignments. Each company submits
its complete feasible productive set to unchanged WorkBoard.AdvanceWork. Ordinary shortage
falls back through that same owner in current ranked order. WorkBoard remains the sole productive
resource/progress owner; original native nodes/jobs/effects/reservations/private views remain authoritative.
The transient identity list clears in finally and creates no saved/hashed parallel state authority.

Actual approach/readiness and external single-use phase invocation remain required; mod phase
waits never become productive offers. Departure, dependency completion, cancellation and original
final startup assignment remain physical; fresh starts earn no same-update quantum. GameClock,
persistent schemas and existing optional-money ownership are unchanged. No adapters or draw code.

Two prior-covered engine files changed: Standard/Work.cs and Kernel/SaveGame.cs. The latter
changes build identity.157 identity paths migrated compile-only; one existing pure fixture helper
visibility changed compile-only. One new explicit master-merged workflow JSON is embedded by
the proof project; no legacy path or duplicate workflow authority.

## Every build and runtime attempt

Build command: `dotnet build Beep.SimEngine.slnx`.

| Attempt | Build result | Same exact new runtime case | Observed proof repair |
| --- | --- | --- | --- |
| 1 | Clean14.89s,0 warnings/errors | 19 passed1 failed/aborted513ms | Query durable completion history, not retired active order |
| 2 | Clean6.77s,0 warnings/errors | 20 passed1 failed/aborted1850ms; bounded timeout | Use actual namespaced activity completion identity |
| 3 | Clean4.26s,0 warnings/errors | 146 passed1 assertion failed,0 aborted1921ms | Wait for actual mod phase arrival instead of assuming20 updates |
| 4 | Clean4.04s,0 warnings/errors | 147 passed0 failed0 aborted1882ms,exit0 | Final green; executable inputs frozen |

Runtime command for ALL four attempts:

```powershell
dotnet run --project tests\Beep.SimEngine.Tests --no-build -- "Mixed productive scheduling: ranked machine-timer quanta, shared inputs and saved physical work"
```

Only observed new test-driver/assertion failures were repaired; engine source remained unchanged
after initial build. No old/family/full/platform/stale runtime case was run. First build completed
after one session poll; that poll is not an extra build. Freeze:2026-10-08 10:48:58 UTC.

## Scoped147-check coverage

- Mixed machine/timer acquired and productive frontiers, no fake worker or same-update startup work.
- Shared whole-quanta stock consumption and exposure; four scarcity/priority permutations including edits.
- Dependency effect timing and durable namespaced completion history.
- Sole-clock pause; real hauling pickup/delivery before replenished work may proceed.
- Saved acquisition/progress/delay/loaded delivery twins; completion and canonical replay identity.
- Cancellation conserves already-consumed stock and retires actual active jobs without false completion.
- Injected later resource claim failure faults without productive prefix/stock loss; Save/Hash refuse.
- Actual external mod phase delay respected while parallel timer progresses; saved delayed continuation.
- Physical colony construction with loaded hauling, saved delivery/build continuation and conserved steel.

Physical waits are bounded at2000 updates with actual time/job/state/blocker/worker/fault diagnostics.
Tests are thin player/protocol drivers; no demo/harness gameplay decision owner was introduced.

## Still open

Distributed external obligations, trusted provider purity/CPU limits, cross-company/fairness breadth,
concurrency/lockstep failure matrix, broader optional-budget/accounting/provider/performance/Oil,
authorized P6 full regression and full P1-P12 acceptance remain open. Existing ranking semantics
were reused, not redesigned. The resource-only case does not claim new money-mode runtime proof.
Previous stage runtime checkpoints are historical and were not rerun on2.291.0.

Full goal remains ACTIVE. No staging, commit or push until full goal AND plan finish and verify.

## Read-only handoff audit

Inventory:1772 repository-visible/prior-referenced paths;578 Markdown documents;3453 local links.
The linked self-excluded source manifest resolves the final pending link; no duplicate paths.
All18 five-column phase rows valid; both new help targets exist. Actual git diff --check exit0
with31 existing LF/CRLF warnings.357 prior-covered engine files inspected by SHA256: only
Standard/Work.cs and Kernel/SaveGame.cs changed. All323 prior src/tests JSON unchanged;
one new explicit workflow JSON. Sole GameClock SHA256 unchanged from the2.290 checkpoint.
Final manifest consistency is checked read-only after the documentation hashes are refreshed.
