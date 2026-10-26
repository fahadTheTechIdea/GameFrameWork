# P8-F1 — Planet X production verification

## Outcome and owners

Original P8.acceptance.001 is verified, with unchanged requirement wording. Six explicitly listed JSON documents introduce mass smelting and count assembly through existing ITransformationDefinition/ITransformer, native TransformerManager, actual ports/stock/resource transactions, WorkBoard/Logistics, PointModule and the sole GameClock. No production framework source or protocol changes: engine2.408.0/save154/replay1/inventory-view6/logistics-view41/work-view98.

New content uses fictional gameplay yields and capacities, not engineering claims: ten kg ore gives six kg alloy plus four kg slag after thirty seconds; three discrete blanks with a reusable fixture give one part after sixty seconds. Five-second quantum and hourly movement/handling are engine interpreted. Source/receiver sixty kg, carrier ten kg, smelter twenty-one kg including crucible; count items explicitly measure one kg per item. No budget, Tycoon, progression, controllers, knowledge or platform module is selected.

## Exact runtime and all attempts

ONE NEW exact bounded case only:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Planet X production: authored smelting and count assembly with physical saved queues and whole outputs' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
```

Limits30000ms runtime/5000ms owned termination/30000 output characters; each physical stage bounded512Updates. All children Exited, untruncated stdout/stderr, empty observer error.

| Attempt | Passed | Failed | Aborted suites | Suite ms | PID | Exit | Observed ms |
|---|---:|---:|---:|---:|---:|---:|---:|
|1|0|1|1|173|27512|1|339|
|2|24|1|1|4781|22108|1|4922|
|3|44|0|0|5058|39868|0|5198|
|4|47|1|0|4988|42000|1|5135|
|5 FINAL|48|0|0|4889|30036|0|5031|

Attempt1 correctly refused incomplete capacity metadata: resources must supply every measure used by a selected store. Fixed explicit fictional kg measures and capacity policies, not the capacity validator.
Attempt2's stage assumed a third raw batch could enter a full shared native store. The engine correctly retained ten kg upstream. Revised proof to actual supply backpressure; no teleport/extra buffer or output fallback.
Attempt3 passed physical production; extended the same case with authored invalid-reference/input/duration checks.
Attempt4 assumed a positive31-second duration must be refused for a five-second quantum. Existing converter legitimately rounds it. Replaced that false requirement with negative-duration refusal; converter untouched.
FINAL additionally proves missing listed recipe file, invalid port, fractional discrete input and negative duration refuse.

## Builds

Completed-slice command: `dotnet build Beep.SimEngine.slnx --no-restore`.

| Build | Exit | Warnings | Errors | Seconds |
|---|---:|---:|---:|---:|
|1|1|0|1|1.09|
|2|1|0|2|10.88|
|3|0|0|0|8.08|
|4|0|0|0|5.36|
|5|0|0|0|4.29|
|6|1|0|5|3.43|
|7|0|0|0|4.30|
|8 FINAL|0|0|0|3.93|

Build1 caught an existing master filename/embedding collision. Build2 repaired test use of actual UnitInfo.Unit, not nonexistent Id. Build6 repaired the new test's use of actual JsonValue API (ToString/Number). Historical registrations compile only; no old/full/family/Oil/Tycoon/platform runtime.

## Physical proof

Simultaneous ordinary player queue commands save before Update. No instant supplies or outputs. Native carrier movement/amount-scaled handling supplies both actual processors and reusable tools. Every advanced step checks mass, count equivalents and tool conservation using actual stock/WIP facts, plus saved twin hashes.

Partial thirty-second work survives operation disable and saved continuation; the sole clock pause freezes movement and processing. Two smelts fill the shared store: original third input stays upstream, no partial third result. Ordinary ten-kg alloy export physically frees receiving room, then third smelt supplies/works/finishes. All alloy and slag remain conserved. Two assemblies reuse one fixture. Partial third assembly cancellation returns the exact three original blanks with no new part/tool consumption, then ordinary requeue earns the third part. Retained facts stay detached/private; foreign commands refuse. Final save, ordinary command replay and interleaved session independence pass.

## Existing master collision and recovery limitation

The initial add inadvertently overwrote the pre-existing untracked planet-x-master.json before its bytes were inventoried. Build caught duplicate embedding. New production master is now distinctly named; the original embed remains. Base thirteen module declarations were reconstructed from the unchanged planet-x-control-master.json prefix before sim.knowledge, matching actual PlanetX.Factory registered modules/files. This is semantic reconstruction, NOT verified byte-identical restoration: no original Git blob or prior anchor/hash was available. Existing extraction/control acceptance was not rerun. Both reconstructed base master and unchanged reconstruction source/Factory are explicitly anchored in this checkpoint; no extraction completion claim is added.

## Scope and source identity

497 selected executable inputs frozen 2026-10-09 23:48:13 UTC; twelve changed/newly-selected inventory entries versus487, ten newly selected entries (including three pre-existing recovery evidence files), not ten newly created files. Exact hashes are in the audit. No executable edits after FINAL.

Original221 requirements:154checked/67open; ONLY P8.acceptance.001 changes checkbox, zero requirement text changes. Full P8/P1-P12/goal still open. No completed-WIP separate-store output blocking, mixed fluids/entity tools/utilities/entity outputs, all cancellation phases, throughput/ETA, arbitrary providers/fault/stress/performance or full regression is implied. Protected G19 authority pending; its three files and sole clock untouched.

Metadata review:1147 source anchors plus13 quickstart, zero hash mismatch;2437 inventoried paths/838 Markdown files/6839 local links with zero broken;18 master rows retain five columns. git diff --check exits0 (34 pre-existing warning/output lines); new driver boundary scan empty. Frozen497 executable inputs have zero post-pass drift. Sole clock/three protected G19 hashes unchanged. Dirty2427 (--untracked-files=all), staged0. Read-only JSON comparison confirms reconstructed thirteen-module base equals the actual control-master prefix; original byte identity and old runtime remain unverified.

Lifecycle skill informed staged ownership review and explicit failure/evidence recording; user/repository focused verification overrides its full-regression recommendation. No staging/commit/push before full goal AND plan finish.

