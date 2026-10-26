# P4-RG1 — Removal admission observation evidence

Status: VERIFIED-SCOPED FINAL650/0/0; full goal/plan open.

[Stage](../../plans/enhancements/P4_REMOVAL_ADMISSION_OBSERVATION_DESIGN.md). Original221/186checked/35open unchanged. No broad runtime or staging/commit/push.


## Scoped verification — completed2026-10-12

Engine beep-simengine-2.487.0, save154/replay1; existing view schemas unchanged. World.CheckRemoval is still the sole admission owner. Every actual guard now uses EntityObservation.ReadProvider over its codec, whole actual target entity and selected space; RequireHealthyUpdate applies before removal admission and before public ConstructionView.CheckRecovery. Original ordered first refusal and operation-owned exclusion remain unchanged. No removal evaluator, rollback, lifecycle state copy, inventory book, recovery subsystem or clock was added.

The selected WorkflowExtensionModule declares an independent removal_admission_probe component in existing master-listed JSON. Its negative-only saved counter belongs to that new optional proof component; production state/schema is unchanged. Explicit modes activate during public clock pause. Guard probes alter own persisted state without Touch, sibling inventory, actual clock or selected point-space state, or throw the original error. They run in the engine, not the host driver.

Four point minute/half-minute normal/refusing workflows use ordinary work-policy and plan commands. The actual builder executes authored packing and removal nodes over game time; original20 stock is placed into five authored4-unit recovery containers and survives retirement. Every public update checks20 total. Saved active-work and packed checkpoints, sole-clock pause, healthy pure typed refusal, foreign refusal, terminal save, public replay and session isolation pass. Ten negative guard previews latch the first fault, reject further live recovery/hashes/saves, advance no clock and publish each viewer's retained healthy time/spatial payload without partial packing/deletion deltas on the next Game.Update.

### Compile and exact runtime evidence

- One solution compile: dotnet build Beep.SimEngine.slnx --no-restore;12.65s,0 warnings/1 error. New driver used nullable HasValue on actual integer PlanView.Order; engine/proof projects compiled. Corrected to original positive order identity.
- Focused syntax repair compile: dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore;6.94s, clean0 warnings/0 errors.
- FIRST exact attempt:525 passed/1 failed/0 aborted in4666ms; PID6540 Exited1/process5078ms. New authored scenario had not enabled the builder policy and parked other vehicles on its point-space route; actual workflow hit its512Update diagnostic bound. Bound/assertions retained; authored parked positions and public builder-role policy corrected.
- Focused authored scenario repair compile: same focused project command;10.24s, clean0 warnings/0 errors.
- FINAL same exact case:650 passed/0 failed/0 aborted in4317ms; PID45576 Exited0/process4560ms, no truncation or observer error. No executable edits after FINAL.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Removal admission observation: physical packing refusal saved removal and callback first-fault cutoff" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 24000
```

Only the new exact case ran. Stage bound512Updates, process60000ms, termination5000ms and output24000characters. Historical tests compile-only. No claim of arbitrary CLR guard correctness, all physical fault/removal paths, full accounting/topology/disposition, performance/stress/concurrency, Oil or full regression. Original221/186checked/35open unchanged; additional214. Whole goal/plan remains open; protected G19/Tycoon/sole-clock implementation, Oil and platform/Draw files untouched. No staging/commit/push.


## Post-FINAL source and metadata closure

Source freeze: 2026-10-12 09:08:45 UTC;1729 source anchors,961 selected executable inputs and13 quickstart anchors. Five previously selected inputs changed, existing ConstructionView joins the selected freeze and source anchors, and two new executable proof files are admitted. Four new files total (probe/driver/stage/evidence); three changed production sources, no new production source or engine state schema. No executable edits after FINAL.

Read-only closure:0 source/quickstart mismatch and0 executable drift;2916 inventoried paths/1036 Markdown files/8681 local links/0 broken;18 valid master rows;221 literal requirement/checkbox/line anchors validated with0 errors. Driver boundary0 forbidden accesses. git diff --check exits0 with34 existing warning output lines. Exact all-untracked dirty inventory2904/staged0. Protected clock/Tycoon hashes remain exact. Original221/186checked/35open unchanged, additional214 and goal_complete=false. Scoped evidence is not a completion claim or authorization to publish.
