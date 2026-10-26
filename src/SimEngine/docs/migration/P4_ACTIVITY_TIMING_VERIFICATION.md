# P4-G54 sole activity timing evidence — 2026-10-09

## Scope and protocol

Core beep-simengine-2.397.0/save154/replay1; persistent layouts unchanged. Deleted IActivityNodeDefinition.RequiredTicks and native/captured implementations. All original engine calls now convert Duration through the actual executing ISimulationTime. Graph/line analysis, node/requirement advancement, continuous resource cumulative budgets, checkpoints/docks and immutable work views share that owner. RequiredTicks view scalar remains public detached data, never a provider callback or compatibility helper. GameClock unchanged; no second clock, scheduler or stock ledger.

## Actual completed-slice builds

Command: dotnet build Beep.SimEngine.slnx --no-restore.

1.Failed6.51s, exit1, zero warnings/one error: new driver's WorkProgress == comparison; engine projects compiled.
2.Clean4.59s, exit0, zero warnings/errors after using existing Equals.
3.Clean3.98s, exit0, zero warnings/errors after first runtime driver repair.

Ten existing historical test call-site files mechanically migrated, compilation only. Existing shared executor factory accepts world JSON edits; it implements no normal gameplay.

## ONE new exact bounded case — actual attempts

Case: Activity timing: session converter owns graph work and cumulative physical inputs.
Command: dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Activity timing: session converter owns graph work and cumulative physical inputs" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000.

1.20 passed1 failed, one aborted suite,507ms. PID31228 Exited1, observed655ms. Driver queried active graph after ordinary plan completion had removed the resolved order; suite threw missing order graph.
2.FINAL129 passed0 failed0 aborted,1953ms. PID2504 Exited0, observed2115ms.

Both terminal, no timeout/stdout/stderr truncation/observer error. Driver now inspects fixed work/graph before completion and persistent plan after resolution. Existing engine lifecycle/removal unchanged. Only new exact case; no historical/family/full/Oil/Tycoon/platform runtime.

## Narrowly proven

Four actual JSON combinations (native/direct strategies x60-second/10-second quantum) execute a90-second parallel timed/physical DAG then fixed successor. Required work is2/9 respectively, not another authored callback. Machine physically reaches before progress; no timed machine is invented. Original continuous resource requirement spends native eight-unit total proportionally over earned progress: first-unit stock16/19 then final12 in both sessions. Remaining physical/fixed updates agree with detached goals/graph remaining work. No extra spend/refund at finish.

Saved partial continuous input/actual itinerary, continued twins, sole-clock pause, private diagnostics, detached facts, interleaved sessions and normal command/clock replay are verified. Wide2.4b node goals stay long and save; positive subquantum duration uses shared ceiling; overflow refuses before Initialize.

## Frozen source and remaining gates

Selected412 executable inputs frozen after FINAL green at2026-10-09 20:14:32 UTC; no executable edits afterward. Audit records every hash and distinguishes source/link/row/protected-file validation from runtime proof.

Original221 compound requirement text/checkboxes remain unchanged. Pre-world executor metadata, arbitrary graph/provider purity/reentry, unrelated/private CLR state, all lost-worker/dock/line/congestion cases, allocation/soak/fuzz/lockstep/global/full acceptance remain OPEN. Different quantums intentionally round to different productive durations; whole-world travel/chronology equivalence across quantums is not claimed.

Protected GameClock and three excluded Tycoon files untouched; G19 resource scope decision remains unanswered. No Oil/platform/Draw/host/business changes. Full P1-P12/goal stays active/open; nothing staged/committed/pushed.

[Stage](../../plans/enhancements/P4_ACTIVITY_TIMING_DESIGN.md), [master](../../plans/enhancements/MASTER_TRACKER.md).
