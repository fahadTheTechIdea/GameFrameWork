# P9-B2 — Physical work dispatch verification

Date: 2026-10-05. Current working tree: **2.54.0 / save28 / work13 / work-view13**.
Logistics-view4, work_policy component1 and resource-flow4 unchanged.
[Stage](../../plans/enhancements/P9_WORK_DISPATCH_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Final scoped gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Work dispatch: private queue, priority age cost and canonical admission"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Work dispatch: physical contention, incident ordering and saved continuation"
```

Build: **0 warnings / 0 errors**, 1.82 seconds. Only these same two directly affected exact named
suites ran during development. Final gate: **116 checks / 2 suites / 0 failures / 0 aborted suites**.

| Exact suite | Passed | Runtime |
| --- | ---: | ---: |
| Work dispatch: private queue, priority age cost and canonical admission | 53 | 206 ms |
| Work dispatch: physical contention, incident ordering and saved continuation | 63 | 329 ms |

Total reported suite runtime: 535 ms. No broad/full regression, Oil/Tycoon, platform/host/demo,
lockstep/replay, fuzz/stress/soak or allocation gate ran. The previous B1 policy and earlier resource/
region reports remain historical; those suites were not rerun here. Two early development assertions
were corrected to use actual API/seat/module semantics; the final gate above has no failures.

Scoped `git diff --check` on edited tracked source/test/tracker files passes, with only repository
LF-to-CRLF notices. An explicit trailing-whitespace scan of new dispatch/contracts/tests/JSON/stage/
evidence files finds none. Source checks confirm the old _open sort, order-to-job priority copy loop
and 100 command ceiling are absent from the changed owners; the queue alias, view and work/save
identities match. These checks are source evidence, not a broader test gate. Docs-only changes did
not trigger another build or test run.

## Implementation / sole owners

IWorkDispatchQueue aliases the same WorkSystem instance as IWorkerDispatch. WorkScheduling is a
source file of that existing partial class, not another scheduler or persistent queue. WorkView
projects immutable WorkDispatchOffer values privately. The temporary offer list owns no claims,
jobs, resources or saved state. Existing WorkBoard, native transactions and worker.Job remain the
only acquisition authorities. Offers are re-evaluated before mutation, never retained authorization.

Ready jobs sort by valid bound incident response, exact current player priority, original job age,
selected eligible machine's provider travel cost and stable target/node/job IDs. Pending/blocked/
invalid jobs report actual reasons and cannot overtake ready work. Deferred retry is pending with
work.retry-deferred. Invalid incident binding is not an emergency. Machine policy preference still
ranks workers within each job, separate from job ordering. No composite numeric score or age cap.

After successful assignment or cancellation releases state, the remaining queue is rebuilt. An
attempted-ID set bounds the pass to one attempt per job; pure unsuccessful offers are visited without
rebuilding a full queue for each blocked job. Existing Dirty/fallback cadence alone determines passes.

Activity JSON and player priority commands accept nonnegative integer priorities through int.MaxValue;
the arbitrary 100 ceiling is removed. Parent Order.Priority is sole node-job intent. Redundant job
copies and command-update loops are deleted; the conditional sole work codec saves Job.Priority only
for standalone jobs. Negative/future creation timestamps, negative priorities and redundant node-job
priorities refuse checkpoints. Work/save/view identities migrate together with no old reader.

## Focused proof

- Actual command-authored queue, immutable retained facts, private foreign output clearing,
  observer reads, cross-session owner rejection, pure hash-identical queue queries.
- Original equal-priority age before cheaper newer work; player priority edits before age, exact
  maximum integer values, explicit zero change and negative refusal. Parent priority is not copied.
- Same-checkpoint cost before job ID; canonical named parallel-node ties instead of authored array
  order. View offers and actual selected worker/job agree.
- Conditional standalone priority restore, deferred retry/age restore, negative JSON priority,
  future timestamps and redundant node-job priority corruption refusal.
- Real distant high-priority assignment with zero progress before arrival; physical interruption
  retains job identity/progress/age. Saved twins complete both high and remaining low-priority work.
- Three same-age jobs/two machines: the initial point-unit distances are 0, 0.5 and 1; the provider
  compares their fixed-point raw costs. After the zero-distance machine is claimed, the second job's
  distance becomes 9.5; the other machine correctly takes the third job at distance 1. The unclaimed
  second job stays real pending work with busy-worker diagnostics. Saved twins
  physically finish all three; no double machine claims or lost jobs.
- Provider access closes during actual approach: existing Reopen/backoff retains original job age.
  Reopening access does not bypass the retry deadline. Saved waiting twins claim no worker early,
  then travel and finish identically. Cancellation removes pending parallel offers via existing pruning.
- Ordinary maximum priority versus an actual bound incident using a public-only external worker.
  Ineligible emergency work does not overtake ready ordinary work; eligible response does. Neither
  priority nor incident state is fabricated. Physical response completes before ordinary work and
  saved twins agree. Renewed episode diagnostics reject stale binding and the existing owner retires it.
- All authored activity/role/policy/incident content remains explicitly master-listed JSON; Point/Tile
  providers remain geometry/motion authorities. Bounded waits report actual tick/job/blocker facts.
  Tests issue commands or use existing provider-state fault probes; no test-owned scheduling algorithm.

## Open acceptance

Not complete P9 or the full framework. Strict priority/oldest-first within priority does not prove
low-priority starvation prevention against unlimited higher-priority work. Company alert modes,
starvation promotion, automatic preemption, unified order-supply/production/route haul scheduling,
stock bands/reserves, plans/templates, editable/versioned areas, timed docks/congestion, fleet controls,
provider mutation/fault breadth, replay/lockstep/performance and full stage gates remain open.
Already-acquired hauls retain their real carrier/endpoint claims; they are not unreserved open work.
