# P8-MX1 mixed industrial production acceptance verification

## Current M137: per-update mixed material accounting — 2026-10-23

Unchanged native **2.767.0/save161/replay2**, master base
`cb61483ab81404c11944931aedb4703ba694edb5` plus preserved dirty work. The complete
M1362238-input freeze matches before work. This addresses the original P8.017/P7.017
accounting qualification, not another engine feature or original gate closure.

### Actual gap and existing-owner extension

The EXISTING exact mixed-production case passes **181/0** on current native code.
Its physical mixed-input/output/remote catalyst/utility/staffing/entity-result
workflow already works. The uncovered gap is proof granularity: material totals
were asserted at selected stages, not after every driven primary Update. No engine
defect is found or manufactured to justify a production change.

Only `MixedProductionAcceptanceTests.cs` changes executable source. Its new private
Accounting observer reuses the SAME Total reader, actual public entity/cargo/WIP/
scoped-port snapshots and the ORIGINAL ResourceFlowView.ReadSummary. Independent
fixture opening oracles are4ore/8feed/1flux/8fuel/0product/0waste, matching the already
authored scenario. They are not a running ledger, rate remainder, saved state or
engine defaults. Each material is compared in its own native raw quantity; mass,
volume and count are never added across resource identities.

After each primary update, including normal command updates, the observer requires:

- Original Initialized quantity never increases or disappears.
- Only Initialized, StockTransfer, Produced and Consumed classifications occur.
  Purchases, authored creation, destruction, loss or sales cannot compensate a leak.
- Inputs/catalyst/fuel are never fabricated as Produced; finished products/waste
  are never consumed to balance a duplicate result.
- Actual stock+cargo+WIP+existing scoped-vessel stock equals original opening plus
  original classified production minus original classified consumption.

Original helper Step still performs actual Game.Update and twin-hash comparison.
Advance and Send add **read-only assertions** after that original update/command
helper; they own no decisions, stock, dispatch, handling, work, time or lifecycle.
Promises are not added to physical stock, and reusable tool holdings are not counted
again. Actual reserved vessel/cargo quantities remain with their original engine
owners. The original whole-output wait, same catalyst receipt, physical output
drain, private preview, saved twins and isolated session assertions all remain.

The original recording also gains explicit start/live-final identity, complete
step/checkpoint coverage and actual verified final-hash checks. The recorded primary
is never replaced by a loaded twin. No prefix-only success is used as whole-workflow
proof. The original600Updates/stage and current explicit60000ms case bound remain;
no JSON quantity, time, geometry, role, capacity, worker or budget rule changes.

### Actual execution and image identity

ONE necessary targeted Release build12.24s/12555ms observed,0warnings/errors/exit0:
`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`.

Only the exact literal below runs, baseline then strengthened final. No other case,
family, profile, default, full or soak body executes:

`Mixed production acceptance: physical solid liquid catalyst utility staffing and whole resource entity results`

Both invocations use the existing Release/net8.0 tests DLL with `--bounded-exact`,
60000ms timeout/5000ms termination/50000output characters. Baseline181/0 in6430ms,
PID5308/exit0/6572ms observed; **final11712/0** in7292ms,PID23348/exit0/7433ms observed.
Both exit normally, untruncated. Added checks exercise the actual original workflow,
not duplicated context setup or a new artificial manufacturing rule.

| Context | Actual steps | Commands/checkpoints | Expected/live AND verified final hash |
| --- | --- | ---: | --- |
| Minute quantum |0→158|12/158|2087F2CD32F3E0C9|
| Half-minute quantum |0→301|12/301|1D2CE1DA30EC6A40|

Two actual **full** replay verifications include all12 original player inputs each:
queueing, eight physical hauls, support work, operation enabling and support cancel.
Both original batches still require physical supplies and productive elapsed work;
four total legally placed entity results across the two independent contexts are
not instant spawns. All settled updates conserve all six authored materials under
the selected real mixed workflow, including both partially drained output waits.

All453 native inputs (451source/project plus DLL/PDB) and956 proof inputs
(955source/content plus DLL) match M136 exactly. Native DLL
`04D2B0D16DC1E9883F9A9DDF1BB2245A05F5451A84DCDE3C1B8533405738F575`, PDB
`2BB4234CF79F1B2010CB7DF21D25A95AE462061E5B54850CBFFDDB53B8908B14`.
Reuse M136's450source/PDB/138typed calls/14primitive/8layer writes/zero unknown,
unmapped or indirect census **only after that preservation check**, not a fresh scan.
No native source, proof module/content, JSON, protocol, case registration or profile
row changes. Metadata-only profile-info confirms1099registered/1079included/20excluded,
zero bodies; no other runtime reruns after this test-only edit.

[Before](../../artifacts/optional-module-durability/774fe13fcbcb4206a395d73b5663e831/before.json),
[all attempts](../../artifacts/optional-module-durability/774fe13fcbcb4206a395d73b5663e831/attempts.json),
[tested inputs](../../artifacts/optional-module-durability/774fe13fcbcb4206a395d73b5663e831/tested-inputs.json),
[native/proof preservation](../../artifacts/optional-module-durability/774fe13fcbcb4206a395d73b5663e831/native-preservation.json),
[unchanged original gates](../../artifacts/optional-module-durability/774fe13fcbcb4206a395d73b5663e831/original-gates-current.json),
[structural checks](../../artifacts/optional-module-durability/774fe13fcbcb4206a395d73b5663e831/structural-checks.json),
[frozen inputs](../../artifacts/optional-module-durability/774fe13fcbcb4206a395d73b5663e831/final-inputs.json).

Self-review only. Original command outcomes, World first-fault, ResourceFlowJournal,
private immutable views, notifications and replay remain responsible; broader
[operational/assurance gaps](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
are not certified. Original221words/checks remain210source-recorded checked/11open,
full goal ACTIVE. This is not every material/provider, priced generation, all
continuous/cancellation/result/fault variants, arbitrary private CLR mutation,
lockstep/fuzz/soak/performance or full P7/P8/P1–P12 acceptance. Continue the actual
remaining original caller/accounting matrix; no second production/stock owner.
No Oil/marine/consumer/business/platform/Draw/adapter work or staging/commit/push.

## Historical MX1 category-exercise evidence — native2.474

## Requirement proved

Original P8.acceptance.011, unchanged wording: Mixed solid/fluid ports, catalysts/remote or entity tools, multi-output, utilities and entity outputs are exercised.

One NEW current public-player case exercises all named categories together, at minute and half-minute quantum. Only this original checkbox changes. Broader P8 and full regression do not become complete.

## Runtime composition

Core2.474.0/save154/replay1 unchanged; NO production code/API/codec change. Existing inventory, stock books, physical logistics, workers/areas/docks, batch WIP/tool/operating state, ResourceTransactions, entity rollout and sole clock own every decision and update.

Master-listed mixed-production.json/mixed-production-pools.json/mixed-production-modules.json author six resources with explicit positive mass and volume measures, compatible buffers/carrier handling, separate operator/haul operating areas, dock bays/exits, long staffed activity and two six-minute batches. Existing engine-side EndpointProofModule/OutputPortProofModule alias actual service vessels; no custom conversion command, stock injection, lease table or timer. Recipe numbers are fictional gameplay yields, not engineering claims. No business module or compulsory money.

## Exact build and attempts

One solution compile only: dotnet build Beep.SimEngine.slnx --no-restore failed8.55s,0warnings/1error: proof used nonexistent Completed enum. Correct original phase is Blocked; no solution rerun.

Necessary focused compilations: dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore, clean4.92s,8.45s,6.34s,FINAL7.00s; each0warnings/errors. FINAL precedes passing runtime; no executable edits afterward.

| Attempt | Passed | Failed | Suite ms | PID | Process ms | Evidence-driven repair |
|---|---:|---:|---:|---:|---:|---|
| FIRST | 10 | 1 | 5396 | 27764 | 5560 | Physical carrier blocked by the authored one-dimensional operator position; bounded600Update diagnostic |
| 2 | 31 | 1 | 4714 | 5024 | 4921 | New dock did not declare operator work role; actual work.dock-incompatible at600Update bound |
| 3 | 50 | 1 | 6109 | 25532 | 6277 | Shared service slots allowed operator/carrier conflict on later supply; retain real collision, author separate operating areas/bays/exits |
| FINAL | 181 | 0 | 6916 | 10504 | 7121 | Both complete mixed workflows passed |

Every process Exited; failures exit1, FINALexit0; no aborted process/observer error/truncated stream. All attempted stalls retained. No old/family/full runtime, teleport or collision bypass.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Mixed production acceptance: physical solid liquid catalyst utility staffing and whole resource entity results" --timeout-ms 120000 --termination-timeout-ms 10000 --output-chars 24000
```

Outer120000ms, termination10000ms, output24000chars. Each physical stage bounded600Updates; diagnostics include clock/process/WIP phase/results/actual machine positions/jobs/toils/docks.

## Actual physical acceptance

Ordinary player queue/control/hauls/post/cancel commands and Game.Update only. Machines load/travel/deliver finite solids, liquids, remote catalyst and fuel into actual ports, with saved physical twins; dedicated operator performs a real facility activity. Operating areas restrict operator and carrier bays/exits in the original engine, preventing physical one-dimensional overlap without host routing rules.

Each batch moves2 solid mass and4 liquid volume into the same original WIP, holds0.5 catalyst at actual remote proof.pools endpoint101, earns six productive minutes with real liquid utility draw, and only on complete commit consumes0.25 catalyst and publishes1 solid/2 liquid plus one legally placed empty-state entity. Staffing/utility/native-yield previews are engine calculated, pure and private; active saved WIP/tool/running state restores exactly.

Second execution has a distinct stable ID and completes work while both output vessels are full. It retains full2/4 WIP and the original0.5 catalyst token; saved waiting progress/exposure do not advance, fuel is not burned and catalyst wear is not repeated. Physically draining only the solid vessel publishes no new product or entity and consumes no extra catalyst. Draining the liquid vessel permits the whole second conversion once.

Final actual resource totals: raw solids0, raw liquids0, product2 across vessel/receiver, waste4 across vessel/receiver, remaining catalyst0.5, remaining source fuel2 (initial8 minus timed6). Exactly two entity results exist. Actual journal consumed/produced aggregates match catalyst0.5/fuel6/product2/waste4; original tool receipts release. Settled saves, full command replay and untouched interleaved composition hashes agree.

## Qualification

This closes only the literal P8.011 category-exercise requirement. It is not every mixed ratio/geometry, catalyst provider, entity-tool type, quality lot, priced generation, malformed/CLR/fault envelope, arbitrary module, congestion/deadlock layout, fuzz/performance/stress or complete P8/P1-P12 regression. Other earlier proofs are not rerun by this case.

Original221 gates now186checked/35open; unchanged requirement words/line anchors, one checkbox change. Additional201. No production, Oil/platform/Draw/protected G19/advanced Tycoon/sole-clock source edit or staging/commit/push. Full goal AND plan remain incomplete.

## Metadata closure

Fresh read-only metadata closure:1680 source anchors,933 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2868 paths/1010 Markdown files/8642 local links, broken0. All18 master rows parse and all221 original literal/checkbox/line anchors validate with0errors. Only P8.011 changes checkbox, wording/line unchanged;186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2856/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional201. Two prior selected inputs changed, four new executable inputs/six new source anchors, zero production sources changed/created. No runtime rerun for documentation checks.

