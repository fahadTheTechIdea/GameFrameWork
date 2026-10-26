# P9-C5a — Authored retained haul retry verification

Date: 2026-10-05. Working source **2.65.0/save37/work18/work-view20**.
Transporter2/logistics4/logistics-view8/flow4/company-alert1/native inventory layouts unchanged.
[Stage](../../plans/enhancements/P9_HAUL_RETRY_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Exact focused gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Haul retry: authored policy, checkpoint guards and private facts"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Haul retry: retained physical cargo, timed recovery and saved continuation"
```

Final build: **2.30 seconds**, zero warnings/errors. Policy/checkpoint/private suite:
**57 passed / 349 ms**; physical continuation suite: **91 passed / 451 ms**.
**148 checks / two exact suites / zero failures / zero aborted suites**.
Summed reported suite time: 800 ms. Each complete name selected exactly one suite against the same
already-built binaries. Only those two distinct suites ran/repeated; no third/family/full gate.
Other proof callers/current identity assertions compiled only, not fresh behavioral evidence.

Intermediate runs exposed a named-capability proof lookup, incorrect test assumptions about tick
checkpoints, and a real policy-pause precedence bug. The proof now resolves the explicit named
availability operation; WorkSystem.CheckAssignment checks current policy before reporting temporary
worker unavailability. Systems execute at current tick, then Update advances the checkpoint index.
A due approach may earn one normal move in the declared subsequent motion phase of the same Update.
Tests assert those actual engine semantics, never teleport or a second clock/loop.

## Proven ownership and behavior

- Master-listed work_retry_profiles and required settings.sim.work.retry select the actual immutable
  IWorkRetryPolicy/WorkRetryProfileDef alias. Strict rational initial/maximum durations, multiplier
  and explicit blocker identities refuse malformed/duplicate/missing authoring. Independent derived
  profile content works through existing base aliases, with no scheduler type switch or registry.
- Positive checked session delay saturates at the authored maximum, including fine session quanta
  and extreme multiplier/failure counts; no fixed exponent ceiling. Ordinary reopened physical work
  uses that same policy instead of deleted Tuning delay constants. Invalid provider delay faults
  normal Update and rejects save/hash, with no fallback or fabricated compensation.
- Existing Job.Failures/RetryAt are the sole retained retry state. WorkSystem stops physical motion
  and pauses the same job/carrier/source/cargo/destination/dock commitments and original age.
  No alternate endpoint, replacement job, stock grant, retry queue/timer or promise ledger.
  Before the deadline no approach, transfer, progress earning or counted attempt occurs.
- Due attempts recheck actual worker, remaining endpoints, current policy and spatial provider.
  Recognized policy pauses suppress attempts without erasing a retained reason/deadline, even when
  the worker is also unavailable. Accepted physical approach/service resets consecutive backoff.
  Reopening a route alone does not bypass the deadline; recovery moves normally before delivery.
- Partial pickup/unloading retain actual paid/held/delivered stock, exact receipt tokens and earned
  handling progress. Native payment/stock/capacity waits schedule authored retries without extra
  service earning. Finite paid-supply proof charges only actual chunks; funded continuation preserves
  source/cargo/destination accounting. Direct physical transfer cannot bypass the retained deadline.
- Actual source and far-end dock commitments survive a stopped service carrier. Waiting work cannot
  steal its bay or overlap transfer. Recovery physically departs before the next arrived carrier
  promotes; saved twins conserve stock and reproduce the actual dock sequence.
- Explicit cancellation clears the existing deadline and only its own live commitments, preserving
  actual held cargo. Permanent participant loss ends execution immediately, even if content lists
  Codes.NotFound as retryable; no generic policy converts an invalid participant into an endless wait.
- WorkBoard's sole Sync embeds selected profile identity and validates failure/deadline/state/phase
  causality on write/restore. Corrupt counts, impossible future deadlines, assigned retry state and
  mismatched saved policy refuse. Private immutable WorkJobView derives FailureCount, RetryAt and
  RetryRemainingTicks from that same job; foreign-company jobs remain hidden. Saved partial/due/
  paused/native/dock/ordinary continuation and interleaved session isolation pass.

Proof availability is an actual authored controllable operation read by the existing work modifiers,
not a host retry decision or new job/resource authority. All finite bounds in physical test helpers
are failure diagnostics, not gameplay/world/quantity caps.

## Source-only review

Source-only checks covered **51 explicit stage paths**, including untracked source/documents.
Scoped `git diff --check` and a trailing-whitespace search reported no findings; Git emitted ordinary
LF/CRLF policy notices only. All **15 explicit JSON documents** parsed successfully. A scoped runtime/
test search found no old retry constants or prior build/save/work identity assertions. These are
syntax/source checks, not execution of another suite. Final changes after the gate were documentation
only; no runtime/proof/test change followed the recorded final build and two-suite run.
Explicit static work manifests/scenarios/factories were migrated to the current required retry
definition. Colony's canonical master directly lists that document; raw proof consumers load the
same file instead of a second loader, inferred scenario setting or compatibility path.

## Scope and open gates

Only standalone SimEngine runtime, public-contract proof/tests and linked framework documentation
changed. No OilFieldDays/Tycoon, adapter, host/demo gameplay, rendering/platform or Draw work.
Current content/API/save semantics replace the prior path; no old reader or fallback.

The lifecycle skill kept the runtime, bounded verification and tracker in one implementation stage.
This closes C5a's focused gate, not the complete framework roadmap. Different-endpoint replanning,
authored actual fuel/repair providers, congestion/collision/deadlocks, fleet/plans/editable areas,
starvation/preemption/evacuation-return arbitration, broader provider matrix, replay/lockstep/fuzz/
soak/allocation and full acceptance remain open. Prior reports remain historical evidence.
