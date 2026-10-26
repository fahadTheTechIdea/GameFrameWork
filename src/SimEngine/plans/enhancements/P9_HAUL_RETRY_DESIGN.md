# P9-C5a — Authored physical haul retry and retained commitments

Status: historical focused gate, 2026-10-05. **148 checks / two exact suites** (57 policy; 91 physical),
build2.65.0/save37/work18/work-view20; zero warnings/errors/failures/aborts.
[Evidence](../../docs/migration/P9_HAUL_RETRY_VERIFICATION.md). No broad/full/third suite ran.
Parent [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md); the complete roadmap remains partial.
Prerequisite: [C4c progressive handling](P9_PROGRESSIVE_HANDLING_DESIGN.md).
Follow-on [B4a starvation dispatch](P9_STARVATION_DISPATCH_DESIGN.md) now changes combined ranking.
This retry gate's count/identity is historical; its suites were compiled, not rerun in B4a.

## 0. Problem

Before this stage, WorkSystem.RunJob called Reopen for an unreachable approach or unavailable worker.
Reopen immediately failed every haul and released its native/dock/carrier commitments, even with
valid partial cargo. Ordinary activity retries used hard-coded Tuning durations and a fixed exponent
ceiling. Neither was an
authored cross-game retry policy. Existing Job.Failures/RetryAt already provide the needed sole state.

## 1. Goals

1. Master-listed work_retry_profiles define explicit retryable blocker identities, initial/maximum
   rational durations and backoff multiplier. Each selected sim.work scenario names a profile through
   settings.sim.work.retry. Missing/invalid content fails before Initialize; no fallback or old reader.
2. IWorkRetryPolicy is an open immutable content contract using existing definition aliases. One
   policy delay calculation serves ordinary reopened work and retained physical haul retries. Positive
   checked session durations saturate at the authored maximum, not an arbitrary failure-count limit.
3. Unreachable approaches, unavailable carriers and declared native transfer waits retain the same
   haul/carrier/source/cargo/destination/dock claims, original age, actual stock and handling progress.
   WorkSystem alone pauses, stops motion and sets the existing failure count/deadline.
4. Before the deadline, no reapproach/transfer/earning occurs. Due attempts recheck actual participants,
   eligibility and the same spatial provider. Missing/invalid participants terminate normally;
   cancellation releases only this job's promises and preserves actual delivered/held cargo.
5. Policy pause never erases an already scheduled retry reason/deadline. Recovered physical work
   resets backoff only on accepted approach/service; no teleport, resource grant or different endpoint.
6. WorkBoard's sole Sync and phase validator protect retry state. Private immutable WorkJobView adds
   failure/deadline/remaining facts; save/hash/twin continuation and session isolation remain exact.

## 2. Ownership and implementation

- New Standard/WorkRetries.cs: IWorkRetryPolicy, open WorkRetryProfileDef and strict authored profile
  reader. Existing ContentTypes/ResolvedContent own identities, references and immutable fingerprints.
- WorkModule registers work_retry_profiles and resolves the required scenario binding; WorkBoard
  aliases that definition, validates session delay and embeds its identity in the current work codec.
- WorkSystem.RunJob/AdvanceHandling/AcceptHandling own retained haul deferral using existing job
  fields. Reopen for ordinary work uses the same profile; old Tuning retry constants are removed.
- WorkBoard.ValidateHaul validates paused retry/claim/phase causality; WorkView projects detached
  current retry facts. No new scheduler, queue, retry ledger, reservation book, timer or system phase.
- Proof files/scenarios/masters migrate explicit profile references; no test-side gameplay fallback.

## 3. API

IWorkRetryPolicy: Id, CanRetry(Code), Delay(ISimulationTime,int failures). Delay is pure, positive,
bounded in actual session time. WorkBoard constructor takes the selected immutable policy.
JSON: work_retry_profiles with retry_blockers, initial_delay, maximum_delay, multiplier;
settings.sim.work.retry names the selected definition. WorkJobView exposes FailureCount, RetryAt and
RetryRemainingTicks without executable policy/provider handles. No host retry command is needed.

Authored document (its name is explicitly listed in the sim.work master module's documents):

```json
{
  "work_retry_profiles": [{
    "id": "game.retry",
    "initial_delay": {"numerator": 2, "denominator": 1, "unit": "minute"},
    "maximum_delay": {"numerator": 8, "denominator": 1, "unit": "minute"},
    "multiplier": 2,
    "retry_blockers": ["blocker.unreachable", "work.worker-unavailable",
      "haul.payment-wait", "haul.stock-wait", "haul.capacity-wait"]
  }]
}
```

The scenario explicitly binds `"settings": {"sim.work": {"retry": "game.retry"}}`.
These example durations and blocker choices are content, not framework defaults.
Systems execute at the current tick, then Update advances the checkpoint tick. RetryAt is the
next eligible system tick; RetryRemainingTicks is derived from the checkpoint tick. A due approach
can earn one ordinary movement step in the subsequent motion phase of the same Update, never teleport.
Current policy constraints take precedence over temporary worker unavailability; a disabled worker
neither attempts a retry nor loses its retained retry deadline/reason.

## 4. Files

WorkRetries/WorkModule/Work/Logistics direct execution guard/Tuning/WorkView/SaveGame;
explicit proof masters/scenarios/factories; two exact tests; linked parent/master/README/AGENTS/evidence.

## 5. Errors

Missing profile/settings, duplicate/invalid blocker, nonpositive durations, maximum below initial or
invalid multiplier fail content composition. Invalid provider delay faults, never silently substitutes.
Unknown nonretryable failures end the job. Missing participants release surviving owned promises;
provider violations still fault without compensation/save. Deadline overflow faults explicitly.

## 6. Compatibility

Development only; current content/API/save identity breaks together. No legacy settings or reader.

## 7. Focused gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Haul retry: authored policy, checkpoint guards and private facts"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Haul retry: retained physical cargo, timed recovery and saved continuation"
```

Bound physical stages with diagnostic snapshots. Only these two names may run/repeat this stage;
previous suites compile only. No broad/family/third/full gate without explicit user authorization.

## 8. Risks

Retained blocked jobs intentionally occupy real resources. Authored retry avoids busy loops, not all
congestion deadlocks. A game needs actual repair/fuel/service providers; this stage must not create them
or guess why CanWork reports unavailable. Profile mutation and broader provider faults remain gates.

## 9. Out of scope

Endpoint replacement/rerouting to a different destination, new fuel/repair providers, congestion solver,
fleet/area/plans policies, starvation/preemption, G3 executors, full adversarial/replay/stress acceptance,
Oil/Tycoon, host/demo rules, platform adapters/rendering/Draw. The complete roadmap remains partial.

## 10. Execution

- [x] C5a.1 strict open JSON policy and ordinary retry owner migration.
- [x] C5a.2 retained physical haul deferral/reapproach with current eligibility and native commitments.
- [x] C5a.3 sole checkpoint/view/identity migration and bounded two-suite proof.
- [x] C5a.4 linked current evidence and tracker, broader gates explicitly open.
