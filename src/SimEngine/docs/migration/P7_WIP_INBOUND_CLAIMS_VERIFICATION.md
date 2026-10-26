# P7-WI1 — WIP inbound startup and held movement verification

Date2026-10-13. Core2.524.0/save154/replay1; physical books/view schemas unchanged.

## Original owners and breaking intent

ResourceMovementIntent now requires explicit source and destination receipt sets. Null remains invalid; no overload/default/shim. Twenty-eight constructor calls in fifteen existing files migrated. The proof command codec uses one receipt-set encoder for both directions; runtime replay verifies held incoming/outgoing receipts and retirement. Malformed-null wire replay remains unverified.

ResourceTransactions.PrepareMovements uses one direction loop for both sides, validates actual owner/direction/endpoint/bill alignment and aggregate spending, then checks every held receipt against its actual book. CommitMovements consumes existing inbound tokens rather than creating duplicates. Fully held movement performs no new claim acquisition; ordinary empty receipt sets explicitly request native acquisition. Original complete-set transfer/version/stock/promise guards remain.

Transformer.StartWip now adds inbound WIP to its existing input/tool claim set, acquires all together and passes exact directional receipts to the original movement batch. Input protection, reusable tool records, source bindings, execution/clock/state owners remain unchanged. No new transaction ledger, participant service or stock model. Existing WorkBoard/jobs still own workers/docks.

## Builds and every exact-case attempt

1. Release solution FAILED,2.82s,exit1,0warnings/1error: local acquisition variable shadowed the final transfer result; renamed without behavior change.
2. Focused migration repair CLEAN,10.28s,exit0,0warnings/errors.
3. FIRST exact case80passed/1failed/0aborted in1725ms,PID5156/process1906ms/Exited1. Held movement and first physical flow passed; undersized WIP fixture was correctly rejected by existing content validation before runtime.
4. Focused fixture repair CLEAN,9.87s,exit0,0warnings/errors. Removed the invalid runtime-stall assumption; authored insufficient capacity is explicitly tested as pre-initialization refusal, without weakening engine validation.
5. FINAL exact case161passed/0failed/0aborted in4217ms,PID8648/process4543ms/Exited0.
6. Final unchanged-source Release solution CLEAN,5.68s,exit0,0warnings/errors.

```text
dotnet build Beep.SimEngine.slnx -c Release --no-restore
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "WIP inbound claims: held destination movement preserves remainders and physical startup reserves complete capacity" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 24000
```

All runtime streams untruncated/no observer error. Failed attempts retained. No executable edits after FINAL; final solution rebuild compiled unchanged sources. No old/family/full runtime ran. Profile-info724registered/included,0excluded/long markers,0runtime suites,exit0; not a regression result.

## Scoped runtime proof

Four minute/half-minute held movement flows use ordinary engine-side probe commands and public inventory views. Source/outbound and destination/inbound claims are acquired together. Partial movement preserves real receipt remainders and detached old snapshots. Converging legs share one destination token; aggregate overdraw is refused before either leg. Null, wrong-direction, foreign-owner and stale-amount destination sets produce pure refusals. Foreign viewers cannot read private receipts.

Saved remaining receipts commit once in identical twins and retire exactly. Remaining independent outbound promises are explicitly released, not clamped or recreated. Stock remains conserved; settled saves, new bidirectional command replay and session isolation pass. This is low-level contract instrumentation, not a new instant player transport feature.

Two minute/half-minute physical flows use normal machine enrollment, finite real hauling and native manufacturing. Three input units enter actual WIP while the remaining reusable tool unit is held in its original store. Saved partial execution, sole-clock pause, completion, one manufactured entity, source16/tool1/cargo0 and zero remaining input/inbound/tool promises pass. Two undersized authored WIP cases fail before a game or partial startup exists.

## Remaining scope

Standalone tool callback mutation during later movement, arbitrary graph/dock/inbound provider faults, dynamic WIP-capacity refusal, malformed-null wire replay, entity rebinding, conversions/spills/topology, lockstep/stress/performance/full P7.009/P7/P8 and complete goal remain open. No current original criterion/checkbox/line change or closure. Prior P5.009 permission amendment is preserved, not repeated. No Tycoon/Oil/platform/Draw/clock edit/staging/commit/push. One final full regression deferred until implementation finishes.

Metadata-only validation:1917source anchors/1096selected executable inputs/13quickstart anchors,0hash mismatch/0post-pass executable drift;3070paths/1119Markdown/8825initial local links/0broken;18master rows/0malformed. All221current criterion/status/line checks pass with no current amendment or closure. Diffcheckexit0 with34existing LF/CRLF warning lines; dirty3058/staged0. All12Tycoon hashes and sole clock unchanged. Eighteen previously selected executable inputs changed; six input anchors added, of which two are new files and four are previously existing compiled callers. Four new source files comprise those two executable files plus stage/evidence; one previously existing source anchor is newly included. Five existing production sources changed; no new production source file or state owner. Final document-only footer adds one local link and requires anchor refresh, not gameplay rerun.
