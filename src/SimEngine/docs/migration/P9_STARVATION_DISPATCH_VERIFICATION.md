# P9-B4a — Authored starvation dispatch verification

Date: 2026-10-05. Current working source **2.66.0/save38/work18/work-view21**.
Work18/native/logistics4/logistics-view8/transporter2/flow4/company-alert1 layouts unchanged.
[Stage](../../plans/enhancements/P9_STARVATION_DISPATCH_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Exact focused gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Dispatch aging: authored thresholds, private ranking and strict composition"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Dispatch aging: physical fairness, retained age and saved continuation"
```

Final build: **1.89 seconds**, zero warnings/errors. Policy suite: **71 passed / 410 ms**;
physical suite: **76 passed / 436 ms**. **147 checks / two exact suites / zero failures / zero
aborted suites**. Summed reported suite time: 846 ms. Each complete name selected one suite against
the same built binaries. Only these two distinct names ran/repeated; no third/family/full suite.
Six previous-stage test files received current identity literals only; they compiled, not reran.
Historical results do not establish current broad regression.

The first focused run caught an omitted scopes array being interpreted as an intentional empty
list by the general optional string-array reader. The new policy reader now explicitly requires
that array; malformed/missing scope lists cannot silently disable a requested gameplay rule.

## Proven ownership and behavior

- Optional sim.dispatch-policy selects explicit master-listed content and required scenario profile.
  IWorkerDispatchPolicy/DispatchProfileDef aliases that immutable catalog definition as a stateless
  service. It adds no persistent service, age codec, registry, scheduler, queue or clock.
  Unselected capability and explicit scope exclusion remain distinguishable from missing authoring.
- Selected scope references, identity alias and positive actual-session duration validate before
  Initialize. Duplicate/unknown/malformed/missing scopes, zero/negative durations and absent binding
  refuse. Read-once immutable definitions remain shared safely; five-billion-tick and finer-session
  durations prove no integer gameplay ceiling or implicit minute conversion.
- WorkSystem's sole CopyAssignments applies existing company alert priority, then pure policy.
  WorkerDispatchOffer exposes derived nullable StarvationAfterTicks and Starved only; source offers
  cannot submit these facts or effective priority. Private/observer reads remain detached and pure,
  foreign-company facts stay hidden, and retained views do not follow later state.
- Existing original intent/job creation ticks determine age. Ready work precedes blocked, and actual
  emergency precedes ordinary. Within each class, eligible starved work precedes normal work; oldest
  starvation-cohort age precedes effective priority. Normal work keeps priority before age; existing
  travel cost/stable tie-breaks follow. No arithmetic composite score or unbounded priority increment.
- Actual older low-priority activity beats both younger nonstarved maximum-priority activity and a
  younger already-starved maximum-priority cohort member. New queued high-priority work remains real
  intent and never preempts the acquired worker. Each completion rebuilds actual admission before
  the next assignment. Isolated sessions retain independent clock/jobs despite shared definitions.
- Older native stock demand acquires its actual carrier/receipts before young high-priority activity.
  Real timed pickup/travel/delivery conserves source/cargo/destination stock. Priority edits preserve
  the acquired job and native protection; loaded save continuation is exact. Existing native owners
  remain authoritative; no policy-created stock, promise or transfer.
- Young actual incident response precedes old starved ordinary work. The physical worker travels,
  performs timed authored repair and only then resolves the real incident. Incompatible role/busy
  worker and deferred route retry remain blocked despite starvation. Ordinary reacquisition retains
  original age; explicit cancellation clears the actual worker relation and leaves no ranking ghost.
- Independent public-only profile content supplies different authored thresholds for the existing
  shipment source and ordinary work through base aliases, without editing scheduler/execution.
  Zero, negative and undeclared-scope results fault normal Update and refuse save/hash.
- Saved pre-assignment/cohort, loaded haul, emergency, ordinary retry and cancellation continuation
  produces identical hashes under the same commands. Different authored duration fingerprints cannot
  load the old content. No derived offer/rank is serialized or retained as acquisition authority.

Existing Dirty/source/fallback triggers retain sole assignment cadence; threshold crossing changes
the next eligible shared pass, not an extra per-tick dispatcher. Physical helper bounds are diagnostic
timeouts, not game/world/quantity limits.

## Source-only review

Source-only checks covered **30 explicit stage paths**, including untracked source/documents.
Scoped `git diff --check` and trailing-whitespace search reported no findings. Git emitted ordinary
LF/CRLF policy notices only. Both explicit new JSON documents parsed successfully. These checks
are syntax/source review, not a third behavioral suite or proof of older runtime workflows.
Final runtime/proof/test changes precede the recorded build and gate; handoff edits are documentation.

## Scope and remaining requirements

Only standalone SimEngine runtime, public-contract proofs/tests and linked framework documents changed.
No OilFieldDays/Tycoon, host/demo gameplay, renderer/platform adapters or Draw implementation.
The lifecycle skill kept design, actual implementation, bounded proof and tracker in the same stage.

This closes B4a's focused promotion gate, not the full plan or an unconditional fairness guarantee.
Endless emergency pressure, permanently blocked eligibility, never-released real commitments,
automatic preemption, evacuation/return ranking, fleet/plans/editable areas, repair/fuel providers,
congestion/different-endpoint replanning, comprehensive provider/replay/lockstep/fuzz/soak/allocation
and full acceptance remain open. Earlier source/test counts are historical only.
