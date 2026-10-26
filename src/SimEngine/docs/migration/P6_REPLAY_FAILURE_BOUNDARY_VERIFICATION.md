# P6-R2 scoped verification — 2026-10-08

[Design](../../plans/enhancements/P6_REPLAY_FAILURE_BOUNDARY_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md),
[source manifest](P6_REPLAY_FAILURE_BOUNDARY_SOURCE_MANIFEST.json).

## Outcome and actual changes

Existing Hosting/Replay.cs ReplayVerifier is the single owner of verification. Unexpected provider
exceptions now return replay.provider, original callback/step/record and exception provenance.
No partially verified Game escapes. Caller null errors and specific invalid/identity/order/state/
results/refused original WorldFault paths retain their original classifications. No alternate queue,
dispatcher, codec/hash implementation or clock. Build2.282.0/save140/replay1, saved layouts unchanged.

External diagnostic module uses declared public composition/system/command contracts without engine
internals, friendship or scheduler edits. Failure selection is explicit diagnostic injection, not
gameplay or a saved/default policy. The master lists its actual authored document. Two additional
JSON documents declare module ownership and physical incident startup intent. Existing215 authored
JSON files remain unchanged. Engine changes: original verifier plus strict build identity only.
148 existing identities (EngineInfo+147 prior fixtures) migrated mechanically, compiled only.

## Every build and runtime attempt

1. Build: dotnet build Beep.SimEngine.slnx --no-restore. Clean0 warnings/errors12.22s.
   Initial call returned live59225 after10.005855s; one completion poll terminalexit0.
2. Exact NEW case: Replay failure boundary: extension callbacks and active process control recovery.
   FIRST run120 passed1 failed/aborted442ms, exit1, tool2.0501836s. New authored incident startup
   used unregistered tile_entity_setups. No fallback accepted it; strict content validation refused.
3. Corrected ONLY that new document section to native registered entity_setups.
4. SAME build command clean0 warnings/errors5.42s, exit0, tool6.0281792s.
5. SAME ONE exact NEW case final141 passed0 failures/aborts1403ms, exit0, tool3.194422s.

Runtime command:

```powershell
dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build -- "Replay failure boundary: extension callbacks and active process control recovery"
```

No old/family/full/platform/stale runtime. Source/test/proof/authored JSON frozen immediately after
final green; subsequent edits are documentation-only. This is not full-regression evidence.

## Requirement coverage

- Thirteen deliberately hostile provider modes: composition, restore, OnLoaded, initial/checkpoint/
  final hash, constructor, decode, original inbox copy writing/reading, ordinary Update failure,
  corrupt payload, argument payload. Exact record/sole-clock step/callback and original exception
  provenance checked. Failed verifications expose null Game; healthy source/other session unaffected;
  a healthy factory still verifies afterward. Caller nulls remain explicit argument errors.
- Real mapless native and unrelated external manufacturing: ordinary batches, positive partial WIP/
  queue, replay to active work, partial save, ordinary control disable/resume, clock pause, bounded
  physical completion, six real produced entities,20 to14 native supply conservation. Foreign
  company process view remains private. No harness-produced outputs.
- Authored automation through immutable profile view and ordinary configuration: two-minute dwell,
  pending decision at actual current step, no same-step shutdown, replay/save while pending, pause,
  actual next-step action once, saved twin/replay hash equality, detached retained/private facts.
- Incident target created by native authored startup, never test-side SpawnAt. Explicit resource-only
  fixture omits a monetary requirement instead of installing a fake account. Ordinary raise command,
  physical real responder and positive repair progress, completed detection/isolation, disabled
  parallel cleanup, native material/tool commitment,30 to26 steel; saved active response, ordinary
  cleanup permit, parallel real repair/cleanup, dependent restart, completed original response,
  no duplicate spending and matching saved/replayed final state.

Public gameplay path uses player commands/views, clock and Update. Pure factory/JSON assembly
authoring does not execute gameplay. Diagnostic fault providers are separate from this player path.
80 manufacturing/20 control/600 per response-stage Update counts bound scenario failure. They do
not preempt a synchronous hung provider or prove CPU, allocations, fuzzing or soak budgets.

## SOLID and remaining work

One verifier responsibility, existing command/inbox/save/loop contracts and authority retained.
External provider types expose deliberate faults, not second production/work implementations.
No public API/signature expansion or compatibility reader. Privileged replay diagnostics may include
stack/inner exception details; they are not a company knowledge view.

Full P6/P1–P12 remains ACTIVE. Wider exception/global purity/privacy/corruption/parallel contention/
incident/rival/lockstep matrices, packet/pacing capture, Oil migration, process watchdog, fuzz/soak/
allocation and authorized final broad gates remain open. No Oil/Tycoon implementation/host/demo/
sibling/Draw/platform changes. No Git mutation or publishing; commit/push only after full goal AND
plan finished and verified. Previous evidence is historical and was not rerun on this build.

## Final scoped source checks

849 canonical covered paths;178 Markdown documents;2437 local Markdown links,zero broken/duplicate
paths.18 five-column master phase rows valid;both new HTML links exist. Actual git diff --check
exit0 with31 existing LF/CRLF warnings. Of180 previously covered engine files,only Replay.cs and
SaveGame.cs differ.215 prior authored JSON files unchanged;three new proof JSON documents.
Sole GameClock SHA unchanged. One documentation patch initially requested an earlier checklist
hunk after later table hunks and was refused without changes; separate ordered patches resolved it.

Recorded in the self-excluded [source manifest](P6_REPLAY_FAILURE_BOUNDARY_SOURCE_MANIFEST.json).
Inventory is scoped coverage, not whole-repository ownership or whole-plan completion proof.
