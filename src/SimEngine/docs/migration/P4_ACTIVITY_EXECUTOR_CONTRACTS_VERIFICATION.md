# P4-G52 direct activity-strategy evidence — 2026-10-09

## Protocol and scope

Core `beep-simengine-2.395.0`, save154/replay1; existing persistent layouts unchanged. Native staffed/fixed/timed executors implement IActivityExecutor directly on Def. ActivityExecutorDef is deleted. Sole shared StaffedActivityExecution/FixedActivityExecution/TimedActivityExecution author strict JSON and supply decisions; original WorkBoard, jobs/nodes, phase catalog, stock/claims, productive commitments, motion and clock execute gameplay.

Selected callback consumers now observe actual immutable executor aliases, provider/clock/map facts and whole target/worker codecs, including retained/restored itinerary matching. No registry, itinerary state, scheduler, stock or clock was copied. Existing independent staffed proof implementation is reused, not duplicated; normal fixed/timed proofs delegate engine strategies. Probe behavior is explicitly invalid fault injection, not host gameplay.

## Actual builds

Command: `dotnet build Beep.SimEngine.slnx --no-restore`.

1. Clean **15.83s**, exit0, zero warnings/errors. Initial session57298 observed running; same handle later observed terminal.
2. After the observed engine/error-preservation and driver repairs: clean **8.77s**, exit0, zero warnings/errors. No further build or executable edit after the final green run.

Seven existing test/proof paths migrated from the deleted base, compile-only. ActivityGraph was additionally selected as a dependency not edited in this slice; no preceding selected hash exists for that file, so no prior-checkpoint identity is claimed. No historical case was executed.

## ONE new exact bounded case — both attempts

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Activity executors: direct JSON strategies share physical work and observed state" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
```

1. **171 passed,15 failed, zero aborted suites,1169ms**. Owned PID37420, Exited1, observed1316ms. Actual failures: two incorrect driver assertions that command-receipt Updates earn no timed work; six typed composition errors masked by secondary faulted-session errors; six creation faults armed after the itinerary existed; one matching fault observed only by the later checkpoint call, not the preceding Update.
2. FINAL: **182 passed, zero failed/aborted suites,992ms**. Owned PID27060, Exited0, observed1152ms.

Both attempts used runtime timeout30,000ms, owned termination budget5,000ms and output30,000 characters. No timeout, stdout/stderr truncation or observer error. Only this new exact case ran; no historical/family/full/Oil/Tycoon/platform runtime.

## Repairs justified by observed evidence

ActivityExecutorContracts.Read originally verified codecs in finally after ReadProvider had already latched a thrown composition callback. The subsequent healthy-session check replaced the original ContentException. The repaired path preserves that typed first error and returns no facts from a thrown callback; the session remains faulted and cannot checkpoint. Successful callbacks still verify actual worker/target codecs before returning facts.

Command receipt intentionally advances the original game clock in TestKit.Order. The driver now checks unearned physical/dependent work separately and bounds timed progress by actual elapsed game time. Creation/matching probes use public Submit, armed before the first Update that creates the itinerary. No engine lifecycle or gameplay timing was changed to accommodate a proof.

## Proven behavior

- Actual native/direct strategies, no deleted base, one owned catalog and shared algorithms. Engine-only resource mode has no Tycoon dependency.
- A master-listed reverse-authored DAG compiles parallel timed/physical prerequisites followed by fixed productive work. Timed nodes earn elapsed progress without worker/jobs; fixed work waits on both prerequisites and uses the actual worker/job.
- Original motion moves the machine, travel earns no physical work, and actual target stock20 becomes12 on the first eight-unit physical input. Fixed/timed strategies create no extra resources; a second real execution leaves4, retained after ordinary cancellation without invented refunds.
- Private views, detached progress, partial/fixed/completed/cancelled saves, continued saved twins, sole-clock pause, canonical plan/clock/cancel replay and interleaved isolation succeed. Saved itinerary matching uses original jobs, not another itinerary authority.
- Thirty-four native/direct malformed authoring or role cases refuse before Initialize. Unused definitions cover missing/empty/unknown/wrong-purpose phases, blockers, missing/nonpositive rates and extra instant fields; the role mismatch case alters the selected activity. Three additional unused direct Id/Module/composition-pause violations refuse before Initialize.
- Seven selected invalid callbacks cover creation target-private mutation/clock pause, matching target-private mutation, worker-check private mutation/throw, and rate worker-private mutation/clock pause. The private component mutates its actual codec without Touch. Actual observations fault, and save/hash refuse.

## Frozen selected input evidence

**400 selected executable source/proof/test/project/JSON inputs** hashed AFTER final green at **2026-10-09 19:51:14 UTC**. Twenty inputs are newly selected or changed versus the preceding selected inventory, including ActivityGraph dependency selection, not an asserted prior-checkpoint change. This is selected-input evidence, not full transitive closure or a pre-run capture. No executable edits afterward.

## Remaining scope

All metadata getters across other graph/scheduling paths, reapproach fault/lost-access matrices, non-tile/docks/lost targets, recursive/nonparticipating/private CLR/global rollback, allocation/performance/fairness, lockstep/fuzz/soak and full original acceptance remain OPEN. Reapproach uses the new guard but has no new fault matrix in this case. Historical proofs, including G51, were not rerun on2.395.0. Protected GameClock and excluded Tycoon files unchanged; G19 authority unanswered. Original221 compound gate text/checkboxes preserved. Full goal active; no staging/commit/push.

[Stage](../../plans/enhancements/P4_ACTIVITY_EXECUTOR_CONTRACTS_DESIGN.md), [master tracker](../../plans/enhancements/MASTER_TRACKER.md).
