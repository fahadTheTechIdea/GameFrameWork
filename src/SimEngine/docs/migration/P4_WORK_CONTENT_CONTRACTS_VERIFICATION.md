# P4-G8 — Work content contracts verification

Scoped verified 2026-10-09; full P1–P12 and the active goal remain OPEN.

## Ownership and implementation

IWorkRequirement now extends IContentDefinition and IDependencyNode. Native requirement bases are implementations only. WorkBoard catalog, node resolution, evaluation, resource restore and supply consumers use actual owned contract aliases. DependencyGraph<IWorkRequirement> remains the sole topology compiler; native activity-prerequisite analysis consumes its same nodes.

WorkRequirementContracts derives topology from actual all/any entries; composition requires declared dependencies to match them, preventing hidden cycles or a different scheduling/evaluation graph. WorkRequirementGroupDeclaration is the only original group JSON parser/reference binder/canonical sorter. Native and direct groups delegate; WorkBoard retains the original evaluator, chosen occurrence paths, reservations, resource/tool/cost checks and saved state.

IActivityExecutor is actual file content; node resolution, plan alias admission and executor composition no longer require ActivityExecutorDef. StaffedActivityExecution extracts the original travel/productive/departure settings and behavior; native/direct executors delegate. ActivityExecutorContracts is the sole worker-role shape validator used by native timed/fixed/staffed and external staffed definitions. Actual toils/jobs/progress remain native WorkBoard/WorkSystem. Executor validation callbacks use existing provider observation.

IWorkerRoleDefinition exposes the canonical WorkerRoleId. Composition requires exact owned content identity. Worker definitions, activity roles, policies, work load, signals, alerts/reserves, docks, staffing, fleet metadata and saved jobs resolve it. Necessary fleet dependency metadata now uses IContentDefinition rather than Def; fleet lifecycle and behavior unchanged. All old proof callback migrations compile only.

ResourceRequirementDeclaration extracts the original use/bill/rates/unit parser and once-only binding; EntityResourceSourceDeclaration extracts the original explicit local/named-port source lookup and binding. Native/direct definitions reuse these owners. No source/stock/token/claim store is copied. Named-port, elapsed and continuous paths compile; the NEW runtime directly proves local consume/hold paths only.

The external engine module introduces direct role, staffed executor, resource leaf, worker guard and group definitions without native WorkerRoleDef/ActivityExecutorDef/WorkRequirementDef inheritance. These are engine-composed mod providers, not host/player gameplay. One explicitly master-listed work-contracts.json owns them. Native group contract.native-root references a direct all group, containing a direct any group. Shared fixture helpers author documents and startup worker roles only; the thin player driver performs public commands/Clock/Update/views/save/hash/replay, no World access or stock/progress writes.

## Build and exact bounded evidence

All builds: `dotnet build Beep.SimEngine.slnx`.

1. Clean6.38s/zero warnings/errors: core contracts and consumer migration before new proof compilation.
2. Clean10.56s/zero warnings/errors: direct module, document and NEW player driver. Build returned a live session72142, then its terminal exit0 was observed; it was not restarted.
3. Final clean6.64s/zero warnings/errors: source review consolidated common role validation and explicit once-only resource resolution.

One multi-file apply_patch failed validation because the protocol JSON key was engine_build, not build. It applied NO partial source changes; read current state and reapplied with the actual key. No failed build or stale artifact runtime.

Only ONE NEW exact case ran; final source review reran only that same case:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Work content contracts: independent JSON roles executors and nested requirements drive physical player plans" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 24000
```

- First:75 passed/zero failed/zero aborted,884ms; PID32688 exit0/observed1041ms.
- Final current-source:75 passed/zero failed/zero aborted,767ms; PID14956 exit0/observed910ms.
- Both output streams untruncated; no observer errors. No full/family/historical/Oil/Tycoon/platform runtime. Old suites and QuickGame compile only.

## Verified player behavior and refusals

Actual independent owned contract aliases and native group references; once-only Initialize; pure per-entry requirement diagnostics. Paused persistent intent saves without acquiring inputs. Ordinary policy exclusion of contract.inspect leaves the order waiting without machine assignment/progress/material spending; enabling that role through ConfigureWorkPolicy resumes normal physical work.

Native machine moves from4 to20, receives the exact new role/job and direct staffed itinerary. The nested ANY chooses b.compatible (two materials), never the unavailable thirty-material branch. Native stock20→18 once, two tools are physically held (available16) and then released (available18). Detached choices are immutable/private; retained facts stay unchanged. Pause earns no work; partial choices/progress/resource claims restore in hash-identical twins. Parallel inspection/calibration join occurs only after both finish. Plan history settles through the original owner. Final saved state, ordinary policy/plan command stream replay and fresh interleaved-session isolation pass.

Twenty malformed authoring cases reject before Initialize: invalid/mismatched role identity, missing guard role, invalid/empty group, duplicate entry, missing/wrong requirement, self/mutual cycle, false declared topology, absent/wrong-purpose/duplicate staffed phases, absent resource, empty/negative bill, invalid use, invalid/incompatible port. No fabricated role/provider/material/default or instant fallback.

## Protocol and remaining full-plan obligations

Core2.340.0/save154/replay1; all existing component/system/view schemas and sole GameClock unchanged. Executable inputs frozen2026-10-09 03:48:11 UTC.

Catalog contract migration is scoped-verified. This is not complete arbitrary mod/source/executor admission, all prior requirements/roles/areas/fluid/technology/continuous/elapsed/named-port runtime, malicious provider/global purity/fault matrices, distributed obligations, all industrial incident/environmental behavior, lockstep/stress/soak/scale/full regression or final P1–P12 acceptance. Those original gates remain OPEN.

The lifecycle skill guided shared responsibility, staged implementation and scoped evidence; repository/user constraints override broad tests and early publication. No compatibility loader/native fallback, duplicated DAG/claims/progress/intent, mandatory money, business expansion, host/Draw/platform gameplay or second clock. Original primary acceptance requirement text/checkboxes unchanged. FULL goal AND plan must finish and be verified before commit/push; condition NOT satisfied, no staging/commit/push.

[Stage](../../plans/enhancements/P4_WORK_CONTENT_CONTRACTS_DESIGN.md), [tracker](../../plans/enhancements/MASTER_TRACKER.md), [audit](../../plans/enhancements/COMPLETION_AUDIT.md).
