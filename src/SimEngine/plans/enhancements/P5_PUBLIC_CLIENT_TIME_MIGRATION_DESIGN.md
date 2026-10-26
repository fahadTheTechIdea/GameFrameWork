# P5-A40: Public client tests use the engine's time and lifecycle contracts

## 0. Problem statement

Remaining OilCo.Tests clients referenced removed static clock helpers, HorizonDays and PressOnDay. The old demo test demanded that a host implement every gameplay order, contradicting the thin-host rule.

## 1. Goals

Migrate original client/input suites to the non-friend public runner without duplicate registration. Use session SimulationTime and GameDuration, not another clock. Replace the obsolete host-strategy expectation with an app integration test for native command handoff and scenario termination. Keep long acceptance and remaining legacy compilation explicitly open.

## 2. Design

Move ClientTests.cs, InputTests.cs and DemoTests.cs from OilCo.Tests into OilCo.PublicProof. Register each once in its actual runner. Reuse ThinClientTests.Files through ClientTests.Factory; no additional JSON fixture loader or gameplay owner.

Client shell duration, saved controller handover and presenter horizon use actual public engine APIs. ScriptedInput.PressAt accepts elapsed GameDuration and resolves through the session clock. The original 26-hour input timings and 24-hour shell/three-day presenter intents remain; these long cases compile but are not executed in this stage.

The demo case drives SimApp with a twelve-minute sandbox horizon and normal RunFrame calls. FieldPlayer requests ordinary founding and core.set-controller only. The test observes accepted commands, engine-owned controller/company facts, native horizon termination and terminal save identity. It is not a substitute for every engine command or long industrial acceptance.

BudgetTests retains its original allocation scenario and uses the session's ExactTicks conversion only. Its long runtime is not executed.

## 3. API surface

No production API changes. Core 2.555.0, save 156 and replay 1 remain unchanged. No new clock, lifecycle, finance or gameplay class.

## 4. Files to change

Original ClientTests/InputTests/DemoTests move to the public project; both runner registrations change. ThinClientTests exposes its existing fixture internally to its own assembly. BudgetTests performs native time conversions. Stage, evidence and current master/audit metadata update.

## 5. Error handling

Exact cases run in bounded child processes. A timeout is retained as an aborted attempt. A later retry starts only after terminal observation and may increase the process budget, never alter authored cadence. Remaining private/stale callers are compilation backlog; no friendship or compatibility shim is restored.

## 6. Compatibility

Development migration only. Remove obsolete source/registration paths rather than retain duplicate suites. The obsolete demo-wide command-coverage expectation is superseded explicitly by the user's thin-host rule; engine-wide coverage remains open.

## 7. Verification

Build the public proof project and its dependencies. Execute only exact controller handover and the bounded demo lifecycle case. Run a compile-only diagnostic of the remaining old test project. Do not run long shell/presenter/input/allocation cases or any full profile.

## 8. Risks

Actual two-hour autopilot cadence can exceed short wall-clock test budgets. The twelve-minute demo proves handoff, not subsequent physical expansion. The old test project and full canonical solution remain non-green until other suites migrate.

## 9. Out of scope

Production strategy or financial rules, changed game rates, adapters, Unity/Godot, Draw, additional framework objects, full regressions and Git writes.

## 10. Status

Migration implemented; public build clean and exact demo8/0. Controller runtime timed out twice and remains open. [Evidence](../../docs/migration/P5_PUBLIC_CLIENT_TIME_MIGRATION_VERIFICATION.md). Original 221 acceptance requirements and their 194 recorded checks/27 open gates are unchanged.

