# P6-FD1 — Bounded normal-lifecycle failure diagnostics

## 0. Problem statement

The existing wait helper uses explicit administrative RunTicks, so a normal player-facing stalled/paused workflow lacks a shared bounded Update-call path. The original P6.012 gate needs current deliberate starvation/corruption/blocking evidence.

## 1. Goals

1. Keep one shared test wait owner and typed diagnostic failure while allowing normal SimGame.Update without fast-forward.
2. Distinguish lifecycle-call budgets from authoritative simulation step age explicitly.
3. Report actual snapshot/hash/module/build/queue/blocker facts for material starvation, pause, engine fault andcorrupt save.
4. Terminate actual synchronously blocked providers by the existing owned process deadline, with bounded output.
5. Prove the ordinary physical resource workflow still completes through public engine input/views.

## 2. Design

TestKit.UpdateUntil is a thin normal lifecycle driver; its loop is shared with the existing explicit admin wait. SimulationWaitException adds a required budget-unit value, not another clock or state owner. Normal pause/lockstep waits use the declared Update-call budget even when game age is unchanged. Gameplay completion predicates remain engine views, not manufactured transitions or formulas.

The new exact case launches only itself in seven explicit modes using BoundedTestProcess. Limits come from one embedded JSON profile. Negative update behavior is the existing master-listed UpdateWatchdog module, never normal content. Physical starvation is a normally accepted batch with no delivered inputs andautomatic supply disabled; no helper injects stock or progress. Actual corrupt save bytes fail the normal loader. Existing public physical helper supplies a positive completion proof.

## 3. API surface

Testing-only UpdateUntil andtyped diagnostic BudgetUnit. Core engine2.510.0/save154/replay1 andall runtime modules unchanged. No compatibility or default fixture limits.

## 4. Files to change

Existing testing TestKit/SimulationWaitException only; new exact player/diagnostic case andexplicit JSON limits; test project/profile/registration; tracker/evidence. No production or game rule changes.

## 5. Error handling

Unmet budgets throw typed failure; paused calls never invent engine steps. Actual engine fault preserves original cause andlast safe snapshot/hash-unavailable state. A blocked synchronous call requires the existing owned child-process termination; emitted output cannot renew the deadline. Corruption refuses without changing another healthy session.

## 6. Compatibility

Development tooling change; constructor call sites migrated in the same owner, no duplicate legacy loop.

## 7. Verification

One solution build andone new exact case with90second outer budget. Seven exact children cover healthy/starved/paused/corrupt/throw/block/noisy-block; deliberately failing child outcomes are asserted by the outer case, not reported as green engine workflows. No older/family/full runtime.

## 8. Risks

Lifecycle calls are an observer budget, not simulation time. Typed budgets must prevent that confusion. Diagnostic snapshots are explicit observer-level developer facts, not player transport payloads. Arbitrary hostile CLR sandboxing is not claimed.

## 9. Out of scope

Tycoon/Oil/platform/Draw, game clock/rules/stock/state owners, unrestricted raw driver writes, full regression andoverall goal completion.

## 10. Execution order

1. Share the original wait loop andadd explicit normal Update/budget unit.
2. Add authored bounds andthe isolated seven-mode physical/failure case.
3. Build/run bounded evidence; repair only this case as needed.
4. Close P6.012 only if deliberate stalled/corrupt/blocked diagnostics are proved; preserve remaining gates.

## 11. Scoped result

P6-FD1 FINAL63/0/0 in11238ms, owned outer PID18756/Exited0; deliberate seven child boundaries and real physical completion verified. Only original P6.acceptance.012 closes,221/193checked/28open/additional242. Failed attempts, same-case diagnostics and reporting repairs are retained in [evidence](../../docs/migration/P6_PHYSICAL_FAILURE_DIAGNOSTICS_VERIFICATION.md). Production/clock/Tycoon unchanged; whole goal and final full regression remain open. Post-pass source/input/link/gate metadata is refreshed without another game runtime.
