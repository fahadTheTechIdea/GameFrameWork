# P7-PR1 — Guarded cancellation return and participant release verification

Date2026-10-13. Core2.526.0/save154/replay1; production view and physical state/book schemas unchanged.

## Original engine owners

ResourceMovementIntent requires ResourceMovementParticipantDisposition.Retain or Release. All30 existing calls in17 existing files migrate directly, with no compatibility constructor or old wire path. The original shared proof encoder persists the disposition alongside the same three receipt sets. Unknown enum and conflicting disposition of a shared actual token refuse before callbacks. Identical Release references across legs deduplicate; spending sets remain the only transfer authority.

ResourceTransactions.PrepareMovements validates/captures all participants. CommitMovements carries requested release receipts to original CommitCapturedTransfers. After each actual transfer updates expected stock/promises/versions, the same ephemeral ClaimEndpoint collection validates and releases surviving participant records through FinishCapturedClaims. Consumed tokens are not recreated, partial source remainders are released only when requested, and nonmoving tools retain physical stock. Transfer success facts publish only after all requested releases and settlement succeed.

Transformer.ReturnWip supplies actual original tool records with Release for every original-input return leg. The separate post-return tool release is removed for material returns; inputless cancellation retains original complete-tool release. No new cancellation coordinator, participant service, stock/promise ledger, codec, scheduler or timer. Ordinary movement/native startup explicitly Retain; public transfer/claim implementations still use their original owners. No Tycoon business behavior changes.

Independent negative-provider instrumentation adds receive/release peer mutation modes to the existing pool facade. The original ResourceInventoryState remains the only stock/book. The test driver authors a half-second time variant of the existing master-listed JSON and sends normal player commands/reads detached views/uses Update and the sole clock. Contract transfer probes exercise resource protocol, not instant gameplay hauling.

## Every build and runtime attempt

1. Completed focused Release migration compile FAILED10.36s,exit1,0warnings/1error. Driver incorrectly expected TransformationWipSnapshot.Owner; repaired by reading the actual native tool receipt owner.
2. Focused repair compile CLEAN6.27s,exit0,0warnings/errors.
3. FIRST/FINAL new exact case243passed/0failed/0aborted in1022ms,PID17164/process1211ms/Exited0.

Streams untruncated/no observer error. Failed compile retained. No executable edits after FINAL. No old case, family suite, full executor or solution regression ran.

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Return participants: saved cancellation settles original inputs and reusable tools before movement success" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 24000
```

Profile-info726registered/included,0excluded/long markers,0runtime suites,exit0. Metadata lists coverage, not full regression success. The authorized one final full regression remains deferred until implementation finishes.

## Scoped runtime proof

Both second and half-second variants prove a two-leg source movement with shared release participants. Unknown enum and conflicting keep/release requests refuse purely. Pending command saves preserve the new disposition. Source receipt remainder and duplicate nonmoving tool release once; stock is exactly debited without tool consumption or fabrication. Detached old receipts, save/load twins, settled saves, replay and independent sessions pass.

Both time variants earn native mixed-input work with a shared raw/material tool and a separate reusable jig store, then stop operation. Changing current raw/tool selections never changes active original bindings. Full original input vessels hold cancellation with actual WIP, tools, progress and execution intact; saved pending/blocked cancellation and sole-clock pause pass. Freeing only one vessel cannot return a prefix or release a tool, and publishes no WIP movement. Once both fit, original inputs return, shared/separate tools release, exact stock remains in original stores, current alternative stores stay empty, no recipe outputs occur, and exactly two complete verified return facts publish. Saved twins/replay pass.

Four negative sessions across both times mutate the separate tool endpoint during original raw inlet receive or shared raw-tool release. The original full checkpoint catches them, stops updates/clock, retains prior publication and refuses save/hash/history observation. This is detection of a dishonest provider, not compensation or rollback. Deferring success facts through release is implemented in the original commit, not in the driver.

## Remaining scope and permissions

Original221criteria/193checked/28open text/status/line anchors unchanged; no original gate closed. Prior P5.009 human mechanical-only Tycoon amendment preserved; all Tycoon source unchanged in this stage. Arbitrary graph/dock/inbound/provider matrices, dynamic capacity, malformed-null wire, rebinding/conversions/spills/topology/lockstep/stress/performance/full P7.009/P7/P8/full goal remain open. No Oil/platform/Draw/clock edits, staging, commit or push.

Metadata-only validation:1923source anchors/1098selected executable inputs/13quickstart anchors,0hash mismatch/0post-pass executable drift;3076paths/1123Markdown/8831initial local links/0broken;18master rows/0malformed. All221current criterion/status/line checks pass without current amendment or closure. Diffcheckexit0 with34existing LF/CRLF warning lines; dirty3064/staged0. All12Tycoon hashes and sole clock unchanged. Twenty-four existing selected inputs changed; one new compiled driver added. Three new source files comprise driver/stage/evidence; six existing production sources changed and no new production source/book. Final document-only stage outcome adds one local link and requires anchor refresh, not gameplay rerun.

