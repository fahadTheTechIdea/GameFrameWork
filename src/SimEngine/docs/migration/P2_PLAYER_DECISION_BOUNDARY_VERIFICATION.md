# P2-F5/P11-C5 — Player decision boundary verification

2026-10-08. Build2.290.0/save141/replay1. Focused proof only; full goal/P1-P12 remains
unfinished. No staging/commit/push. [Stage](../../plans/enhancements/P2_PLAYER_DECISION_BOUNDARY_DESIGN.md).

## Original authorities

PlayerDecisionContext.Invoke is the sole attached/native invocation/expiration owner.
Operational reads, preview, submit, blocker and condition/site access expire after success
or exception. Immutable company identity remains available. A swallowed already-latched
observation failure stops at the post-decision health gate; first cause is not overwritten.
Native saved strategy/cadence/answers stay with the existing PlayerControllersSystem.

The original PlayerSeat attachment lists use reference identity and refuse duplicates.
SimGame captures all seats' attachments before any callback; attach/detach edits do not
add work or skip already-scheduled callbacks in that pass. TickCore catches the latched
failure before network sealing/World.Step. Original publication settles actual completed
steps and retains each viewer's previous safe epoch, not partial state or observer facts.

ScriptedController no longer takes a context-bearing iterator factory. Wait.Do supplies
the current invocation context to deferred player actions. The two existing callers migrate
to that sole shape and compile only. No retained-context bridge or additional scheduler.

World.Submit/SubmitStamped/Check reject already-faulted sessions before inbox admission
or handler callbacks; SimGame.SubmitFor checks before lockstep buffering. Existing pending
input is retained unapplied and receives no fabricated result. This is a game-thread fault
boundary, not a proven concurrent terminal-admission linearization or global mutation sandbox.

Six existing engine source files change in place (context,session,native controller,script,
World,EngineInfo). EngineInfo plus155 older identity fixture paths migrate compile-only.
The sole GameClock and all authored JSON are unchanged. No Oil/Tycoon/renderer/adapter changes.

## Exactly one new bounded case

`Player decision boundary: expired contexts, stable dispatch and terminal command admission`

FIRST175 passed,0 failed,0 aborted,2619ms. No observed repairs or reruns.

- Successful attached decisions expire every operational context access while preserving Id.
- Self/other detach and cross-seat attach affect the next pass only; duplicate/null attachment refuses.
- Ordinary preview and accepted command are real engine operations; saved no-input twins stay identical.
- Attached faults before any work and after two completed updates stop later controllers/seats/commands.
  Real queued inputs remain pending, no replies are fabricated, private facts retain prior safe epochs.
- Faulted public/native/stamped input and previews refuse; continuation preserves first cause and retries nothing.
- Lockstep callback fault occurs before frame sealing and emits no frame; later local buffering refuses.
- A swallowed visibility failure remains the first cause and cannot admit input or advance the update.
- Existing JSON-authored budget and expired-context modes use the same invocation fault phase.
  Healthy authored state/pending reply/cadence survives save/load with twin hash identity.
- Deferred scripted actions receive distinct fresh contexts and one actual retained accepted answer.
- Player construction moves physical cargo, delivers and builds over game time, saves while hauling,
  continues identically, conserves fifty-minus-thirty steel and replays to the actual final hash.
  Each physical wait is bounded by2000 updates and reports time/job/state/blocker/fault on failure.

## Every build and runtime invocation

1. `dotnet build Beep.SimEngine.slnx`: initial live session35362, one completion poll.
   Succeeded13.47s,0 warnings,0 errors. Chunks edcad0 and1284fb.
2. `dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Player decision boundary: expired contexts, stable dispatch and terminal command admission"`:
   exit0,175 passed/0 failed in1 suite,2619ms. Chunk bd59fc.

No older/family/full/platform/stale runtime. Prior F4a and all preceding runtime results are historical,
not rerun on2.290.0. Source/proof/tests/JSON frozen after first green; subsequent work is documentation
and read-only inventory. [Source manifest](P2_PLAYER_DECISION_BOUNDARY_SOURCE_MANIFEST.json).

## Review and open gates

Read-only handoff inventory:1767 repository-visible/prior-referenced paths,576 Markdown,
3424 local links,0 broken links/duplicate paths.18 existing five-column phase rows validate;
both help targets exist. All357 prior engine paths covered: only the six listed owners changed.
All323 prior authored src/tests JSON remain unchanged; sole clock SHA unchanged.
Actual git diff --check exits0 with31 pre-existing LF/CRLF warnings. Inventory provenance
and source review do not substitute for any unrun regression or platform acceptance gate.

Original context/loop/inbox/controller state/clock/project owners remain single authorities;
public controller contracts remain substitutable, no concrete gameplay shortcut or compatibility path.
Captured dispatch is finite because callback changes cannot extend it, not because an arbitrary
controller/entity/map cap was added. Existing script immediate-yield guard is unchanged.

Retained views/time interfaces are not revoked security objects. Arbitrary CPU hangs, direct trusted
provider World mutation, global/cross-provider purity, full concurrency/lockstep fault matrices,
strategy/recovery breadth, broader accounting/performance/Oil and authorized P6/full P1-P12 acceptance
remain OPEN. No full completion or publication claim follows from this scoped case.

