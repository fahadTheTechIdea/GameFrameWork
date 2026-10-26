# P8-F3 — Physical production blocker verification

## Outcome and ownership

Original P8.acceptance.012 is verified: supply/full output/disabled/fuel/worker blockers preserve actual progress and owned WIP. Thirteen explicitly listed existing modules execute a resource-only scenario from production-blockers-master.json/production-blockers.json plus the unchanged Planet X resource/recipe/work/logistics/world documents. Original TransformerManager/Transformer, WorkBoard, ResourceInventoryState, LogisticsService, tile navigation and sole GameClock own all gameplay. Existing EndpointProofModule/OutputPortProofModule provide contract-based independent actual vessels; no new process, stock, reservation, staffing, clock or compatibility owner.

Engine2.409.0/save154/replay1/transformation-view18 unchanged. No production engine/protocol repair was needed. Driver contains only composition, ordinary player commands, immutable view assertions, bounded Updates, saves and replay.

## ONE NEW exact bounded case

~~~powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Production blockers: physical supply workers fuel and full vessels preserve saved work and whole WIP' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
~~~

Every physical stage is bounded to256Updates, runtime30000ms/termination5000ms/output30000chars. Both children Exited; output untruncated and observer empty.

| Attempt | Passed | Failed | Aborted suites | Suite ms | PID | Exit | Observed ms |
|---|---:|---:|---:|---:|---:|---:|---:|
|1|3|1|1|2885|41296|1|3047|
|2 FINAL|77|0|0|25684|22620|0|25845|

FIRST reached a real missing-tool blocker, while the fixture incorrectly expected missing feed first. Existing startup admission prioritizes absent tools over absent feed. The corrected driver proves tool shortage, physically supplies the reusable tool, then proves missing feed and startup fuel separately. No engine admission rule was weakened. Removed unused journal settings; original ResourceFlowsModule selects the sole authored policy.

Two completed-slice builds:16.94s and11.40s FINAL, exit0/0warnings/0errors. Command: dotnet build Beep.SimEngine.slnx --nologo. Registered historical cases compile only, not executed.

## Physical verified sequence

A real engine-created staffing order travels to the factory and retains actual machine assignments. Initially missing tool/raw supply/startup fuel never acquires unearned WIP or outputs. Ordinary hauling loads, travels, delivers and departs; no instantaneous stock injection.

One-minute batch requires12 productive updates at5seconds/Update, using ten kilograms of raw ore, a reusable crucible and one kilogram of fuel per productive update. Limited four-unit fuel supply earns exactly four updates. Disable/restart and a held bound operator preserve earned WorkProgress, original WIP execution/raw quantity, tool hold and fuel exposure. Five-update saved waits match twins. Fuel exhaustion consumes no future fuel and does not reset work.

Further ordinary ore/fuel hauling completes the first batch and funds the second. Separate actual six-kg alloy and four-kg slag vessels retain the first result. The second batch completes its work but retains ten-kg raw WIP, tool and12-unit fuel exposure while either whole output cannot fit. Saved full-output waits preserve exact work/ownership.

Physical six-unit product export empties only the product vessel. With waste still full, no second product prefix is published: raw WIP remains intact. Physical four-unit waste export permits the one whole conversion and releases the tool. Final actual inventories contain exactly two whole six-alloy/four-slag results; all24original supplied fuel units are consumed, no fuel exists at source/feed/carrier. Every observed Update conserves original20kg raw/results/WIP and one tool.

Saved twins during all blocker waits and hauling, final checkpoint, retained detached facts, company privacy and replay of actual public inputs pass. Authored two-dimensional tile geometry permits actual carrier/operator movement; no one-dimensional crossing or teleport fallback.

## Inventory and scope

509 cumulative selected executable inputs frozen 2026-10-10 00:13:47 UTC; eight changed/newly-selected entries versus503. Six newly selected paths comprise three new executable files (driver/two JSON) and three pre-existing owners (ResourceFlows/EndpointProof/OutputPortProof). Only driver/registry/project/new content changed for execution. No executable edits after FINAL.

All221 original requirement texts unchanged. Only original P8.acceptance.012 checkbox is newly checked:155checked/66open. Mixed solid/fluid/catalyst/entity results, every-phase cancellation, arbitrary provider purity, stress/performance/full P8 and full P1-P12 remain OPEN. No old/family/full/Oil/Tycoon/platform runtime. Sole clock/three protected G19 files unchanged; pending G19 authority not inferred. No staging, commit or push before whole goal AND plan finish.

The lifecycle skill kept the stage tied to the original requirement, responsible engine owners and complete failed/final evidence. User/repository restrictions override its broad-regression advice.

Metadata validation is recorded in the authoritative completion audit; this document does not infer full regression from compilation or focused checks.

Final metadata:1159 source anchors plus13 quickstart anchors;2448 paths/842 Markdown/6901 local links, zero broken links.18 master rows retain five columns; diff-check exit0 with34 existing warning/output lines. New driver boundary scan empty. Frozen509 inputs have zero post-pass edits. Dirty2438/staged0. The first metadata observation found the reused test Program.cs quickstart anchor stale after normal new-case registration; refresh its current hash without claiming a quickstart runtime rerun. Final hash review is stored in the audit. Only one original checkbox changed and no requirement wording changed.
