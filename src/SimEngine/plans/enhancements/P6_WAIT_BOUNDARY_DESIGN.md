# P6 — Explicit wait failure and bounded CLI acceptance

## 0. Problem statement

TestKit.RunUntil currently returns an elapsed count even when its predicate remains false.
That can make a stalled physical workflow look like a completed verification stage.
The bounded CLI parent added in the preceding watchdog slice is compiled but unexecuted.

## 1. Goals

Scoped complete:57 parent checks,0 failures/aborts in6148ms, first runtime attempt.

Only return when the caller's public observation predicate is true, including at the final allowed step.
Throw a typed detached failure when the step budget expires or engine advancement stops.
Report actual scenario/seed/clock/state/outcome/native fault/retained snapshot/module/build facts.
Never hash a faulted partial update or fabricate progress; diagnostic hash failure stays explicit.
Execute the bounded CLI parent end-to-end for success, failed wait, engine fault, blocked Update,
unknown case and invalid arguments. No old/family/full runtime is authorized.

## 2. Design

Keep original TestKit.RunUntil and engine RunTicks as sole wait/clock-control owners.
Validate inputs; check the predicate once per observed boundary; clip final batch to remaining budget.
SimulationWaitException stores public detached facts only, not a live game, world or completion rule.
Its snapshot is explicitly observer-level developer diagnostics, not a player knowledge feed.
Extend the same BoundedTestProcess with strict explicit invocation arguments to observe the CLI parent;
both public entry forms delegate to its SAME lifecycle implementation. No copied process launcher.
TestRunner exposes exact-worker context so the SAME new case can run an ordinary default child
without recursively starting another parent. Child input remains verification-only.

## 3. API surface

RunUntil(SimGame, Func<bool>, int maxTicks, int step=10) keeps its signature; silent false return is deleted.
Zero budget is legal only for an already satisfied observation; otherwise typed step-limit failure.
SimulationWaitException exposes reason, completed/allowed steps, clock position, state, snapshot,
scenario/seed/module/build metadata, outcome/fault and optional hash with explicit hash-unavailable text.
BoundedTestProcess.RunInvocation accepts only explicit bounded-exact/worker-exact protocols.
Bounded CLI parser must refuse an empty exact name before launching a worker.

## 4. Files

Modify Testing/TestKit.cs, BoundedTestProcess.cs and TestRunner.cs.
Add Testing/SimulationWaitException.cs and Tests/WaitBoundaryTests.cs.
Add one explicit verification profile JSON and embed/register through existing project/Program owners.
Reuse existing update-watchdog master/game content and engine provider; do not rewrite game JSON.

## 5. Error handling

Null game/predicate or negative budget/nonpositive batch: explicit argument refusal without advance.
False predicate at bound: StepLimitReached; no overshoot or grant.
No further engine step: EngineStopped plus actual native state/fault; last safe snapshot stays retained.
Faulted hash unavailable: null plus native fault text; never zero/substitute hash.
CLI malformed/empty name: nonzero/no case; missing exact name: actual worker refusal remains non-success.

## 6. Compatibility

Direct development-only behavior correction, no compatibility mode or old silent path.
Other call sites compile unchanged but their historical runtime results are not revalidated here.

## 7. Verification

ONE distinct NEW exact bounded case:
P6 wait boundary: explicit unmet predicates and bounded CLI acceptance.
One ready build, then only this case; observed failures may be repaired/rerun.
Its explicitly named nested probes are the SAME new case, never broad filters or old suites.

## 8. Risks

This is not a synchronous-provider sandbox; externally bounded process execution is still required.
Diagnostic providers can themselves fail or block; failures are explicit and watcher remains necessary.
Default driver RunTicks is explicit engine fast-forward and keeps its existing pause semantics.
Cross-OS/observer-error/fuzz/soak/fairness/full-regression proof remains open.

## 9. Out of scope

New gameplay owners, platform adapters/Draw, Oil, Tycoon, instant fallback or broad/full test execution.
Commit/push remain deferred until the full goal and plan finish and verification.

## 10. Execution order

ONE clean build10.98s0 warnings/errors. First run passed. Executable inputs frozen at
2026-10-08 15:39:52 UTC. Subsequent edits are docs/read-only; no old/family/full runtime.
[Focused evidence](../../docs/migration/P6_WAIT_BOUNDARY_VERIFICATION.md).

Implement same-owner failure/CLI observation; add public physical wait and deliberate failure proof;
run one new case; freeze executable inputs after final green; update evidence/master/hash audit.

