# P2-F6 — registered command startup validation evidence

Engine2.418.0/save154/replay1; no payload schema change. [Stage](../../plans/enhancements/P2_COMMAND_COMPOSITION_DESIGN.md).

## Actual defect and authoritative fix

Original ServiceContainer.ValidateComposition collected services, systems and world-generation stages but omitted CommandRegistry handlers. A registered command-only ICompositionPreparation/ICompositionValidator was not called. World.FinishComposition now supplies frozen Commands.Entries, and the original single collector adds each handler with the same reference-identity deduplicator. Existing provider order remains intact and previously omitted command-only participants append in captured registration order. Every participant prepares before any validates; both new and restored sessions execute this pass before any system Initialize. Aliased handler/service instances validate once.

No new validator registry/owner, provider default, command payload, state, time, catalog, stock or scheduler. Original commands remain their sole Check/Apply owners. Contract documentation explicitly includes registered command participation.

## Exact public-input proof

One new case: `Command composition: all registered handlers prepare and validate before Initialize with aliases and physical saved production`.

A ten-module explicit JSON master reuses original Planet X authoring and appends a registration-only probe document. Declared mode/resource/alias inputs are strict JSON. Non-friend probe handlers throw if ever checked/applied and implement no positive gameplay. Their external counters record startup order only; native initialization/generation/progress/stock remain authoritative.

Both command-only and service-aliased bindings prepare/validate exactly once each, every preparation precedes validation, unused handlers participate and saved restore revalidates without re-granting startup stock.16 preparation/validation/provider/resource/late-registration variants refuse before Initialize/generation and invoke no gameplay callback.16 ordinary registry conflicts exercise command type/ID, service contract/persistent identity, system order/ID, generation order/ID, event type/ID/provider/schema and view contract/ID/provider/schema; all fail before handler preparation or Initialize.6 invalid whole module graphs (missing/version/self/cycle/duplicate owner/duplicate requirement) run no preceding declaration callback.

Both valid sessions issue ordinary original smelting input, then engine-owned hauling/loading/travel/tool acquisition and timed WIP work. Every update conserves real stock/WIP/tools. Pending/partial/completed saves, twins, whole6-alloy/4-slag output, replay and interleaved original session isolation pass. Probe commands never run, so no positive game logic moves into the driver/module instrumentation.

## Observed commands and results

`dotnet build Beep.SimEngine.slnx`: exit0,8.52s,0 warnings/0 errors; all five projects compile.

`dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Command composition: all registered handlers prepare and validate before Initialize with aliases and physical saved production' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000`

FIRST/FINAL126 passed,0 failed,0 aborted; suite2113ms. Bounded process Exited, PID43920, exit0,2269ms observed; stdout/stderr untruncated, observer error empty. Outer runtime30000ms, owned termination5000ms, output30000chars. Each physical stage256Updates with actual phase/blocker/progress/stock diagnostic. ONE NEW case only, no repairs or historical/family/full/Oil/Tycoon/platform runtime.

## Post-pass executable and metadata inventory

UTC 2026-10-10 02:37:48 UTC:579 selected executable inputs,11 changed/new vs573. Six newly selected inputs include four new executable files and the existing Kernel/Services.cs and World.cs; no claim that six source files were newly created. Source identity captured after FINAL; read-only check confirms0 executable drift.

125 additional scoped deliverables,1238 source anchors plus13 quickstart anchors with0 mismatches;2513 selected paths,864 Markdown documents and7248 local links with0 broken;18 five-column master rows,0 malformed. Original221 words/checkboxes unchanged,204 line anchors refreshed. New driver state-writer boundary scan0 matches. git diff --check exit0,34 pre-existing output lines.

Actual git status --porcelain=v1 --untracked-files=all:2503 dirty files,0 staged. Sole clock and all three protected G19 files match the previous baseline. Metadata checks run no historical runtime.

## Remaining original scope and publication

All221 original requirement words and checkbox states unchanged:158 recorded checked/63 open. P2.acceptance.004 remains OPEN for its broad provider/ownership/order matrices; this actual missing command-participant gap and scoped matrix are verified, not all possible module callbacks or full regression. Full goal/plan still OPEN.

Protected G19 authority remains pending, its three Tycoon sources and the sole clock stay unchanged. No Oil/business behavior, client/demo gameplay, adapters, Draw, Unity/Godot or renderer change; no compatibility shim or duplicate gameplay model. Lifecycle guidance retained the original responsibility and an explicitly scoped gate. Nothing staged/committed/pushed; publication waits for full goal AND full plan.
