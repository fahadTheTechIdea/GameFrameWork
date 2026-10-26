# P5-A18 verification — Native production and optional costs

## Current evidence

Core2.543.0/save154/replay1. FINAL exact881/0/0,16686ms case,16890ms bounded observer,
PID33608, exit0, untruncated. Only this NEW exact case ran repeatedly for its own repairs; no old/family/full suites.
No runtime logic edits after FINAL. Oil targeted builds clean2.49s and1.44s, zero warnings/errors.

[Design](../../plans/enhancements/P5_NATIVE_PRODUCTION_AUTHORING_DESIGN.md).
Actual production data:21 native transformations,17 separate optional cost bindings, eight native processors.
Original IDs/material bills/output identities/hour durations/amounts preserved. Signals use native contracts.
No recipes/producer/inline cash reader compatibility path. Existing transformation/resource/budget/clock owners retained.

## All focused compilation attempts

- 1: exit1, errors1, elapsed00:00:14.39.
- 2: exit0, errors0, elapsed00:00:05.61.
- 3: exit0, errors0, elapsed00:00:11.01.
- 4: exit0, errors0, elapsed00:00:07.18.
- 5: exit0, errors0, elapsed00:00:06.88.
- 6: exit0, errors0, elapsed00:00:04.88.
- 7: exit0, errors0, elapsed00:00:12.88.

Initial compile fixed one test-only nonexistent EntityView.Tile member using existing spatial snapshots.
No failed patch validations. One diagnostic PowerShell foreach-pipeline syntax error changed nothing.

## All exact runtime attempts

- 1: 877 passed, 2 failed (15539 ms); bounded case: Exited, pid=46536, exit=1, elapsed-ms=15718, stdout-truncated=False, stderr-truncated=False, observer=; tool-output-truncated=false.
- 2: 879 passed, 2 failed (18768 ms); bounded case: Exited, pid=12040, exit=1, elapsed-ms=19012, stdout-truncated=False, stderr-truncated=False, observer=; tool-output-truncated=false.
- 3: 879 passed, 2 failed (17329 ms); bounded case: Exited, pid=24184, exit=1, elapsed-ms=17571, stdout-truncated=False, stderr-truncated=False, observer=; tool-output-truncated=false.
- 4: Timed out without complete case summary; bounded case: TimedOut, pid=15020, exit=-1, elapsed-ms=30041, stdout-truncated=False, stderr-truncated=False, observer=; tool-output-truncated=false.
- 5: 17 passed, 864 failed (10825 ms); bounded case: Exited, pid=29004, exit=1, elapsed-ms=10988, stdout-truncated=True, stderr-truncated=False, observer=; tool-output-truncated=true.
- 6: 881 passed, 0 failed (16686 ms); bounded case: Exited, pid=33608, exit=0, elapsed-ms=16890, stdout-truncated=False, stderr-truncated=False, observer=; tool-output-truncated=false.

Fixture release assertions initially assumed blocking vehicles, but its pre-existing test truck was
explicitly nonblocking. The finer-step attempt also timed out. A diagnostic local-frame override caused
saved twins to use different local pacing and produced864 failures; its observer output was truncated.
Final fixture authors a blocking result vehicle (matching Oil intent) and consistent ten-update frames.
No production rollout/clock algorithm was changed or teleport/instant fallback added.

## Focused command

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Oil production: canonical timed manufacturing and optional cost annotations" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 100000
```

Two36-hour builds run sequentially through actual native queues. Saved twins match across both stages;
load after first release continues the second. Two actual entities appear clear of existing blocking
trucks. Original18000 amounts charge twice in native frontier money mode, never in sandbox. Accounts
and receipts come from the original owners. Driver only authors fixture content, submits commands,
calls Update, reads immutable/native views and saves/loads/hashes.

## Actual admission boundary and remaining work

Both public actual Oil preflights refuse before initialization:
`oil@1.0.0 oil/work.json: unknown section 'orders'`.
Previous recipes blocker is gone. No actual Oil world update ran; this is a targeted diagnostic, not
a successful gameplay/regression result. Native manufacturing projection does not prove all Oil entities,
recipes/providers, domain views, industrial scenarios or private facts.

Static zero-cost fixture fields and one90-cost purchase fixture migrated. Resource-only/cancellation
authoring callers are compiled, not runtime revalidated. One old TransformerToolTests physical-supply
caller still injects positive inline cash and needs native financial fixture migration. Invalid inline
negative/missing-economy probes remain refusal inputs. Do not treat prior green claims as current coverage.
Original221/193recorded checked/28open retained; additional282. Full goal open; no Git writes/full suites.

## Final metadata

2051 source anchors,13 quickstart anchors,1195 selected executable inputs: zero hash drift.
8675 local links: zero broken;18 master rows valid; thin-driver boundary search empty.
All221 original criterion texts/checkboxes/line anchors match; no changes this stage.
Exact authoring comparisons confirm every original21 ID/input/output/entity-output/hour duration and17 amount preserved.
Git diff --check exit0 with34 existing diagnostic/warning lines;3155 dirty paths, zero staged.
Of15 recorded Tycoon sources, only TycoonModule changed; one new binding source added.
Original account/pricing/value/purchase/budget and GameClock algorithms unchanged. Baseline/Oil runtime/remaining fixture migration open.
