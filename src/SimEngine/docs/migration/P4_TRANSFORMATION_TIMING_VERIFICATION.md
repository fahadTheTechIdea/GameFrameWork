# P4-G20b single transformation timing owner — scoped evidence

2026-10-09; core2.355.0/save154/replay1/persisted schemas unchanged.
Full P1–P12/goal OPEN; no staging/commit/push. Lifecycle skill influenced staged ownership/design/proof/
docs, with focused user/repository gates overriding broad regression/early publication advice.

## Implemented engine change

Removed RequiredTicks from existing ITransformationDefinition, sole TransformationDeclaration, native
TransformationDef and independent recipe proof. No compatibility helper/extension method/parallel
converter. Existing execution progress, Inspect progress fraction and pre-Initialize composition
duration-range validation now call original world.Time.Ticks(recipe.Duration), exactly as snapshots
already do. Authored duration is data; existing ISimulationTime/GameTime is the single converter.
No new clock, rate math, queue or scheduler. Three older SessionTimeTests references mechanically
use existing SimulationTime.Ticks(shared.Duration); that historical runtime was NOT rerun.

Thin authoring/player helpers in TransformationContentContractTests are shared with the new driver.
Additional setup/world authoring edits only change explicit JSON inputs, never runtime game rules.
Independent continuous fixture uses existing master-listed transformation-metadata.json authored row
through the original recipe reader and actual independent kind. Native processor, WIP/tools/resource
transactions/hauling/rates/operating/cancellation/result owners execute all gameplay.

## Actual commands and observations

Compilation only: `dotnet build Beep.SimEngine.slnx --no-restore`, clean7.74s, zero warnings/errors.

ONE NEW exact bounded case, FIRST/FINAL only:

```powershell
dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build -- --bounded-exact "Transformation timing: one session converter and independent continuous physical flow" --timeout-ms 30000 --termination-timeout-ms 2000 --output-chars 24000
```

58passed/0failed/0aborted10761ms, PID31992 exit0/observed10920ms. Exec yielded its live session18749;
polled that SAME process to completion, not restarted. Output untruncated/no observer error.
Stages capped1800 public Updates with actual step/phase/blocker/progress/source/factory/cargo diagnostics.
No old/family/full/Oil/Tycoon/platform runtime. No repair or post-final executable edits.

## Narrow behavioral evidence

No RequiredTicks method remains on actual recipe contract/native/shared declaration. Ninety authored
seconds resolves to2 minute-sized Updates or9 ten-second Updates. Ordinary enqueue preview is pure,
physical hauling supplies4units, partial WIP progress saves, paused twins remain identical, and exact
remaining Updates match the detached Required value. One entity result only; input3 consumed/tool1
returned, final source16/factory1/carrier0. Saved continuation/completed saves, immutable retained facts,
and ordinary command/pause canonical replay checked in both selected session quantums.

Actual independent continuous recipe binds original native processor. Physical hauling and resupply
earn at least3 output units. Native cumulative facts show input2/tool wear1/operating1 per output1.
First retained flow facts remain1 produced/1 earned progress. No batch entity result. Running save/
load twins and sole-clock pause preserve identical decisions. Same public cancellation on both twins
retires queue and releases WIP/tool holds/receipts; original actual stocks across source/destination/
carrier/factory total20 minus3 per produced unit, accounting for draw4 and output1. Replay verifies
ordinary continuous enqueue/pause/cancel inputs. No host-side execution/rate/stock/progress writes.
Unused independent huge duration overflow refuses before Initialize through original converter.

## Freeze and remaining scope

39 executable inputs frozen2026-10-09 07:36:17UTC, SHA256 in machine audit. Original GameClock and
three excluded Tycoon files unchanged. Resource catalog type-binding authority remains pending.
Arbitrary CLR-provider callback faults/metadata mutation, network/staffing/technology/knowledge
runtime breadth, endpoint/route removal, simultaneous continuous lanes, mixed dimensions/content,
shuffling/lockstep/concurrency/fuzz/soak/performance/global/full acceptance remain open.
This narrow proof does not close the original compound P4/P8/P11 gates or whole goal.
No optional business/default, Oil/domain, adapter/Draw/renderer/Unity/Godot work.
Commit/push only after genuine full goal AND plan completion and verification.

[Stage](../../plans/enhancements/P4_TRANSFORMATION_TIMING_DESIGN.md),
[tracker](../../plans/enhancements/MASTER_TRACKER.md), [audit](../../plans/enhancements/COMPLETION_AUDIT.json).
