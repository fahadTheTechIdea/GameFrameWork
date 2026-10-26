# P7-GP1 — Protected activity productive claims verification

Date2026-10-13. Core2.527.0/save154/replay1; original physical book and production view schemas unchanged.

## Original contract and mutation owners

IResourceTransaction.EvaluateClaims, AcquireClaims and ConsumeClaims require explicit participant receipt sets. All74 existing calls in22 existing files migrate directly:24evaluation/40acquisition/10consumption. No optional overload, old wire or compatibility path. Empty explicitly means no extra participant, not fabricated stock/authority. No Tycoon caller/source change was needed.

ResourceTransactions.CaptureClaimParticipants validates actual distinct records, gathers participant and request/spent identities and captures every endpoint before native receipt/admission callbacks. Original PrepareClaimReceipts/PrepareClaims/AcquireCapturedClaims/FinishCapturedClaims operate against that same ephemeral collection. Existing exact version/stock/promise checks catch nonmoving peer mutation immediately. Participants can have different actual owners and can alias spent records across roles, but cannot duplicate within their own set. No extra ledger, service, scheduler or stock total.

EvaluateClaimConsumption is the matching pure consumption contract and shares PrepareClaimConsumption with commit. Existing proof command uses it for previews, carries explicit participant records, and encodes them through the original receipt encoder. No driver reproduces claim/capacity/refusal logic.

Original WorkBoard productive frontier passes retained actual commitments through running-input evaluation/acquisition and passes allRetained to each node's consumption. Only after a verified consumption are those spent tokens removed from the transient participant list. This prevents stale spent references during the next node, while every unspent tool/material remains guarded. Existing optional budget checks/spend order, earned-progress/spent flags and choice mapping are unchanged. No parallel activity/resource execution model or host gameplay is introduced.

A new master-listed JSON document authors scoped original pool stores, selected ports, native held/startup/elapsed resource requirements and a parallel three-minute timed activity. Existing native/external capacity/resource/port definitions and original WorkBoard execute it. The non-friend engine module remains the real provider; test drivers use commands, detached private views, Update, the sole clock, saves and replay.

## Every build and same-case attempt

1. Completed focused Release compile CLEAN8.01s,exit0,zero warnings/errors.
2. FIRST case1passed/1failed in66ms,PID42576/process218ms/Exited1. Explicit proof JSON embedding was missing; no game initialized.
3. Explicit document registration repair compile CLEAN5.48s,exit0,zero warnings/errors.
4. SECOND case49passed/6failed in687ms,PID22332/process834ms/Exited1. Authored flow2 met only one full startup-plus-first-quantum admission. Fixture wrongly expected both zero-work nodes and then queried retired nodes.
5. First-quantum supply fixture repair compile CLEAN5.99s,exit0,zero warnings/errors. Author flow4 initially, then a real later shortage, followed by finite4refill. Native resource admission was not relaxed.
6. THIRD case54passed/1failed in884ms,PID28296/process1063ms/Exited1. Work had completed, but driver queried history using raw prototype instead of actual activity plan key and subsequently dereferenced retired nodes.
7. Actual completion identity repair compile CLEAN6.08s,exit0,zero warnings/errors.
8. FINAL case183passed/0failed/0aborted in1610ms,PID19312/process1856ms/Exited0.

All streams untruncated/no observer error; all failures retained. No executable edits after FINAL. No old case/family/full executor/solution regression ran.

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Activity claim protection: complete retained tools guard running acquisition and productive consumption" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 24000
```

Profile-info727registered/included,0excluded/long markers,0runtime suites,exit0. This is profile metadata, not full-regression success. The authorized one final full regression remains deferred until implementation finishes.

## Scoped runtime proof

Minute and half-minute direct contract flows prove actual participants with distinct owners. Null/duplicate/stale sets refuse purely. Pending protected-acquisition commands save/load through the shared receipt encoder. Exact consumption permits a spending token to alias the participant set, retires only spent promises, and preserves nonmoving tool stock/token/version. Later explicit release, detached old facts, settled saves, replay and independent sessions pass. Contract probes are not an instant player hauling feature.

Both time variants start two authored timed nodes without instant work. A real later running shortage freezes both earned positions while retaining tool promises; supplied input alone is consumed. Finite refill resumes the same saved frontier. Partial saved twins, one-clock pause and eventual native completion preserve exact eight-input consumption, reusable tools3, once-only tool release, immutable old node facts, settled saves and replay. These are timed resource DAG flows, not a claim that worker/dock/transport participation is fully accepted.

Four negative sessions arm an existing flow provider to mutate free stock in a separate tool store during actual running acquisition or consumption. Valid held tool tokens do not conceal the stock/version change: complete checkpoints fault before native earned-state publication. Updates/clock stop, prior snapshot stays retained, and save/hash/progress observations refuse. No rollback or compensation is asserted.

## Remaining scope and permissions

Original221criteria/193checked/28open text/status/line anchors unchanged, no original gate closed. Prior human mechanical-only P5.009 Tycoon permission preserved, not broadened. Full graph/dock/release/inbound/provider matrices, dynamic capacity, malformed-null wire, rebinding/conversion/spill/topology/lockstep/stress/performance/full P7.009/P7/P8/full goal remain open. No Tycoon/Oil/platform/Draw/clock edits/staging/commit/push.

Metadata-only validation:1931source anchors/1107selected executable inputs/13quickstart anchors,0hash mismatch/0post-pass executable drift;3080paths/1125Markdown/8834initial local links/0broken;18master rows/0malformed. All221current criterion/status/line checks pass without current amendment or closure. Diffcheckexit0 with34existing LF/CRLF warning lines; dirty3068/staged0. All12Tycoon hashes and sole clock unchanged. Twenty-one previously selected inputs changed; nine input anchors added, two new files and seven previously existing compiled callers. Four new source files comprise JSON/driver/stage/evidence; four previously existing source anchors added. Eleven existing production sources changed, no new production class/book. Final document-only stage outcome adds one local link and requires anchor refresh, not gameplay rerun.

