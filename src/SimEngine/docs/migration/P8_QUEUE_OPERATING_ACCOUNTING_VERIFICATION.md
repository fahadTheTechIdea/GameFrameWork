# P8-OC1 queue operating accounting verification

Verified scoped delivery,2026-10-12; full P8/goal remain open.

## Actual production ownership

Engine2.502.0/save154/replay1/transformation view22. Original TransformerManager uses one existing-snapshot comparison for earned operating exposure/consumed bills. Append always preserves historical costs; tail cancellation and active physical return preserve them. Normal unbuffered head retirement may reset its own exposure. Next demand, drawing and port presentation are not compared as historical costs because head selection and return legitimately change them. No additional clock/payment/resource/queue owner or fabricated rollback.

Existing independent Pulse provider now declares IUtilityConsumer facts with explicit empty ports/rates/bills for its free emission content. Its own productive-update exposure increments only on productive progress, resets on actual head retirement and persists in original component codec3. It is not game time or a copied native balance. JSON queue probes increase exposure on append/tail cancellation or reduce it on tail cancellation, while the ordinary snapshot remains otherwise valid. The new coordinator rejects all six minute/half faulty sessions through normal commands.

## Commands and observed results

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Queue operating accounting: append tail and physical return preserve earned consumption" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 24000
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --profile-info tests/Beep.SimEngine.Tests/testdata/framework-validation-profile.json
```

ONE solution13.77s clean, exit0/zero warnings/errors. ONE NEW FIRST/FINAL657 passed/0failed/0aborted in51401ms; PID42100, Exited0/process51655ms, no stdout/stderr truncation or observer error. No executable edits after FINAL. Profile-info701 registered/included,0excluded/0long markers/0game suites; full executor not run. No old registered case/family/full suite executed.

## Current runtime scope

- Two independent minute/half physical flows: three actual productive updates, disabled append and tail cancellation retaining head identity/progress/exposure, company privacy, saved disabled twins, sole clock pause, unbuffered head retirement, new head exposure distinct from rate1.5 progress, one actual surviving result, retained facts, terminal save, replay and unrelated session isolation.
- Six faulty minute/half flows: append increase, tail increase and tail reduction. Pure admission succeeds; saved pending callback refuses identically at transformations.enqueue/cancel with exact earned-cost diagnostic. No successful result/publication, further gameplay/time or healthy save; original detached costs and unrelated session preserved. Invalid mutation is terminal, not rolled back or advertised as healthy.
- Existing native public all-phase physical helper reused directly, not old registry: idle/tail/supplying/partial work/operating fuel wait/full-output/capacity-blocked return/continuous stop; saved pending commands and waiting twins, real hauling, WIP/raw/tool/fuel conservation, no duplicate yields/refunded burnt fuel, retained private facts and replay. It uses original engine commands/views, never driver-written progression.
- Four tile/point minute/half physical return helpers reused: actual original endpoint loss/restoration, disabled return blocker's precedence, retained operating consumption/tool receipts, saved twins, physical raw return once, pause, no productive result, replay/isolation.
- New public driver contains no World/TestKit/RunTicks/resource/progress mutation. Fault injection is declared engine-side mod behavior.

## Gate decision

Original221/190checked/31open unchanged; additional233. This closes the queue operating-cost preservation subtask, not original P8.013 cancel/save every phase across remaining providers/paid costs or P8.017 broader accounting/global/full acceptance. No historical test count promoted to current full-regression evidence.

Native authoritative save payload unchanged; independent proof component codec2 is deliberately replaced by3 with no compatibility reader. All12 Tycoon sources, sole clock, Oil/platform/Draw implementations unchanged. No business rules, size limits, extra timer, staging/commit/push. Authorized final full regression deferred until implementation finishes.

Metadata-only validation:1803 source anchors/1010 frozen executable inputs/13 quickstart anchors match; all221 original gates retain literal requirements/line anchors and existing checkboxes.2987 inventoried files/1073 Markdown/8765 local links, zero broken;18 master rows/zero malformed. Driver boundary0, diff-check0,2975 dirty/0staged. All12 Tycoon source hashes and sole clock unchanged; goal_complete=false.
