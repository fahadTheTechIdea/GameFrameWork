# P8-F2 — Production execution preview verification

## Outcome and ownership

Existing TransformationManagementView now implements ITransformationExecutionView.ReadExecutionPreview. A single guarded actual provider observation publishes detached recipe inputs/yields/tools/consumption/rates, current controls/gates/head/WIP/flow, optional money and game-time estimates. No new coordinator, inventory, graph, progress balance, resource remainder, timer or clock. New WorkProgress.TryRemainingUpdates uses its original RemainingRaw and exact BigInteger ceiling arithmetic; original Time and GameDuration convert productive updates.

Engine2.409.0/save154/replay1/transformation-view18. Saved payload/component/system schemas unchanged. Native production execution, physical travel/handling, resource conversion and sole GameClock unchanged. The existing view gains early healthy-update checks for private/missing/current/recipe absence, preserving the original first fault rather than returning healthy-looking fallback values.

Batch nominal throughput is Outputs per NominalCycle at current target, excluding supply/output waits. Continuous throughput is exact authored OutputRates with explicit current target multiplier, not measured or quantized delivered history. ProductiveCompletionStep is conditional on the actual working batch head, positive unchanged rate, enabled operation, unpaused clock and healthy current gates/blocker. It excludes material arrival/output settlement/future failures; no invented full logistics ETA. Other situations have typed absence. Continuous streams never gain a fictitious completion date.

## ONE NEW exact bounded case

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Production execution previews: exact productive estimates and detached authored throughput reject faulted reads' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
```

Limits30000ms runtime/5000ms termination/30000outputchars; every physical stage256Updates. Both children Exited with untruncated stdout/stderr and empty observer error.

| Attempt | Passed | Failed | Aborted suites | Suite ms | PID | Exit | Observed ms |
|---|---:|---:|---:|---:|---:|---:|---:|
|1|6|1|1|109|42296|1|261|
|2 FINAL|72|0|0|1455|38852|0|1599|

FIRST correctly rejected the new fixture's zero native minimum. Native transformer authoring requires positive work-rate bounds. Fixture now explicitly uses1..2; zero target is tested as normal operation.target-bounds refusal. The generic exact math still tests zero-rate/range absence; no native policy fallback or parser weakening.

Two clean completed-slice builds:15.97s and5.63s FINAL, both exit0/0warnings/0errors. Command `dotnet build Beep.SimEngine.slnx --no-restore`. Historical registered cases compile only, not executed.

## Verified behavior

Exact rate1.5 consumes six required work steps in four productive updates; fractional1.5 earned progress leaves three updates; long.MaxValue at one raw rate has range absence, zero rate has no fake ETA, already completed work has zero remaining productive time (not delivered-result promise).

Explicit fictional JSON recipe previews thirty-second ten-kg ore to six-alloy/four-slag yield and reusable crucible without compulsory money. Native physical supply initially blocks and produces no predicted completion. One actual earned step gives five remaining updates. Disabled operation preserves ten kg real WIP/progress/tools and removes ETA. Ordinary rate1.5 command earns fractional progress2.5; preview reports four-update nominal cycle and three remaining productive updates. Sole clock pause preserves hashes/progress and removes schedule estimate. With unchanged conditions, native completion exactly matches conditional step, giving one whole result. Saved twins, final checkpoint, replay and retained facts pass.

A distinct continuous recipe is explicitly authored through the same catalog/composition (no driver production calculation). Physical native supply earns actual mass output. Preview returns two output rate declarations of one kg per five seconds and two-kg input per five seconds; no batch bills/cycle/ETA. Saved flow continues through the original native owners with conserved physical quantities.

Repeated reads leave hashes unchanged, other company/unknown recipe gives null facts/normal refusal. Negative update system is the existing PriorActivityProofModule, explicitly selected by two new master-listed negative documents; no duplicate fault owner or gameplay provider. Actual update fault causes retained execution/current/enqueue/content absence reads to reject before private/missing fallbacks. First fault and previously detached result facts remain; faulty save rejects, an interleaved session stays healthy.

## Scope and durability limits

503 selected executable inputs frozen 2026-10-10 00:00:53 UTC; twelve changed/newly-selected entries versus497, six newly selected inputs (four new files plus two already-existing engine owners). Audit contains exact hashes. No executable edits after FINAL.

All221 original requirements retain wording/checkboxes:154checked/67open. This is additional scoped evidence, NOT closure of original P8.acceptance.015: delivery ETA, all arbitrary providers/mixed ports/gated fuel/staffing/reentry/fault purity/continuous throughput/accounting/full scope remain open. No broad/family/historical/Oil/Tycoon/platform runtime. Protected G19 files and sole clock untouched; authority still pending.

Metadata review:1154 source anchors plus13 quickstart; zero hash mismatches.2443 paths/840 Markdown documents/6870 local links, zero broken links.18 master rows retain five columns; git diff --check exits0 (34 pre-existing warning/output lines). New driver boundary scan empty. Frozen503 executable inputs have zero post-pass drift. Sole clock/three protected G19 hashes unchanged. Dirty2433 using --untracked-files=all; staged0. Original221 texts/checkboxes unchanged.

The lifecycle skill guided the staged single-owner implementation and full attempt/evidence record; user/repository restrictions override its full-regression advice. No staging, commit or push before full goal AND plan finish.

