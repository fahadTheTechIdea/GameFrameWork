# P7-PM1 — Complete movement participant verification

Date2026-10-13. Core2.525.0/save154/replay1; existing physical books, state layouts and production view schemas unchanged.

## Engine ownership and direct migration

ResourceMovementIntent requires an explicit ParticipantClaims set alongside source and destination receipt sets. These are actual endpoint-owned receipts, not a new ledger or balance. Null sets and invalid/duplicate/foreign/stale participants are refused. Identical references can participate in multiple roles; source and destination sets still declare spending. All29 existing calls in16 files migrated directly, without optional arguments, compatibility constructor or old wire path.

ResourceTransactions remains the sole owner. PrepareMovements captures every participant endpoint before claim admission. CommitMovements keeps that same ephemeral ClaimEndpoint collection through AcquireCapturedClaims and CommitCapturedTransfers. Existing exact stock/version/promise checks run after callbacks. Only authorized acquisition/spending advances expected snapshots; nonmoving tool endpoints cannot change. Public transfer/acquisition contracts still delegate the same implementations, with no duplicate algorithm, rollback compensation or participant service.

Transformer.StartWip supplies its entire acquired input/tool/WIP-inbound set. Shared input/tool receipt remainders and separate reusable-tool stores remain protected in original execution/books. No graph, job, dock, clock or state owner is replaced.

Existing independent EndpointProofModule adds detached private observations and explicit negative-provider instrumentation. The provider still aliases the actual ResourceInventoryState. The driver uses public player commands, views, Update and the sole clock; no raw world/stock/completion writes. Contract-level transfer probes are not a new instant gameplay transport path. Existing master-listed input-ports JSON authors capacity, resources, selected ports, recipe and duration; no duplicate content schema or artificial grants.

## Every build and exact-case attempt

1. Focused Release compile CLEAN10.79s,exit0,zero warnings/errors.
2. FIRST case2passed/3failed in356ms,PID23140/process624ms/Exited1. The movement fixture attempted5ore in an authored4capacity vessel. Existing engine correctly refused; fixture adjusted to4 without weakening capacity.
3. Focused fixture repair CLEAN5.07s,exit0,zero warnings/errors.
4. SECOND case133passed/3failed in548ms,PID21044/process703ms/Exited1. Negative fault checks incorrectly expected player Paused state. Provider violations were detected, and save/hash/history refusals passed. Assertion changed to actual fault state/zero updates/stopped clock.
5. Focused assertion repair CLEAN5.92s,exit0,zero warnings/errors.
6. INTERMEDIATE case136passed/0failed in480ms,PID30348/process629ms/Exited0.
7. Added an explicit peer mutation during fresh outbound acquisition and stronger retained-publication assertion. Focused compile CLEAN6.88s,exit0,zero warnings/errors.
8. FINAL case151passed/0failed/0aborted in675ms,PID44108/process843ms/Exited0.

All streams untruncated/no observer error; every attempt retained. No executable edits after FINAL. No old case, family suite, full executor or solution regression ran.

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Protected movement: complete participants survive acquisition and shared-tool startup or fault peer mutation" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 24000
```

Profile-info725registered/included,0excluded/long markers,0runtime suites,exit0. This lists profile coverage; it is not a full regression result. One final full regression remains deferred until implementation finishes.

## Scoped proof

Two direct contract flows cover fresh acquisition and fully held source/inlet movement with a nonmoving peer tool. Pure malformed participant refusals, private immutable receipt publication, pending-command save/load using the shared three-set encoder, exact debits/remainders, unchanged peer version/token/stock, explicit later release, settled save, replay and independent session isolation pass.

A native authored three-second mixed-input batch shares its raw endpoint with a reusable ore tool and holds a second jig tool at a separate store. Real input enters original WIP; one work step is earned rather than instant completion. Shared remainder and separate stock remain exact. Saved twins, sole-clock pause, original timed completion, exact product/waste amounts, empty WIP and released reusable receipts pass.

Four independently armed violating-provider sessions mutate a nonmoving peer during held receipt preflight, fresh outbound acquisition, movement take, and native startup take. Each faults the engine, refuses further clock steps/save/hash/history observation, and retains prior snapshot publication. This proves detection, not atomic rollback of a dishonest provider; no compensation fiction or successful partial history is asserted.

## Remaining scope and permission

Original221criteria/193checked/28open and current text/checkbox/line anchors unchanged; no original gate closed. Prior human mechanical-only Tycoon amendment preserved. Tycoon business rules remain unmodified; no Tycoon source changes in this stage. Arbitrary graph/dock/inbound/provider faults, dynamic capacity, malformed-null wire, rebinding/conversions/spills/topology/lockstep/stress/performance/full P7.009/P7/P8 and full goal remain open. No Oil/platform/Draw/clock changes, staging, commit or push.

Metadata-only validation:1920source anchors/1097selected executable inputs/13quickstart anchors,0hash mismatch/0post-pass executable drift;3073paths/1121Markdown/8828initial local links/0broken;18master rows/0malformed. All221current criterion/status/line checks pass with no current amendment or closure. Diffcheckexit0 with34existing LF/CRLF warning lines; dirty3061/staged0. All12Tycoon hashes and sole clock unchanged. Twenty-four previously selected inputs changed; one newly created compiled driver added. Three new source files comprise driver/stage/evidence; six existing production sources changed and no new production source/book. Final document-only stage outcome adds one local link and requires anchor refresh, not gameplay rerun.

