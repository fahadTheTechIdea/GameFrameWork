# P2-F4a — Snapshot lifecycle verification

2026-10-08; focused game-loop boundary only. Full goal/P1-P12 unfinished.
Commit/push waits for full goal AND plan completion and verification; no Git mutation.

## Original engine owners

SimGame now holds its original lifecycle flag for Initialize as well as Update.
RequireIdle precedes idempotence; callbacks cannot enter even another idempotent
initialization while a phase is active. try/finally releases the flag, including
after a host Updated callback fails. Initialization still earns no game time and
emits no Updated event. Outside-phase repeated initialization is unchanged/idempotent.

PublishSnapshot receives actual completedSteps:0 from initialization, ran from
UpdatePhase. The original sole GameClock.FinishUpdate replaces Halt in place: it
reports actual completed steps and clears pacing/slow status only for a terminal
fault/outcome. Healthy diagnostic batches preserve prior earned backlog/Alpha.
No old overload, additional time owner, saved epoch, loop or phase flag survives.

Original F4 preparation/commit/retain remains authoritative. Every viewer prepares
before publication; a failure retains each viewer's safe facts and first cause.
New/load initial failure discards all prepared observer/seat facts, using empty
private initial projections. Game.Tick/Time report actual completed time while
faulted snapshot Tick identifies its earlier retained facts epoch. No provider retry.

Build2.289.0/save141/replay1; persistent layouts/module schemas unchanged. The sole
GameClock source changes deliberately to finish every published update correctly.
EngineInfo plus154 older fixture paths migrate build identity only, compile-only.
The existing external proof module gains a transient ConfigureVisibility hook and
pure Definition/Content fixture helpers; there is no duplicated composition owner.
Both master-listed view proof JSON files and all other authored JSON stay unchanged.
No Tycoon/business/Oil/sibling/host/demo/Draw/platform source edits.

## One exact new case

`Snapshot lifecycle: initialization guards, actual batches and private fault epochs`

FIRST attempt233 passed,0 failed,0 aborted,2044ms.

- A real definition builds one fresh engine world/session, observed just before
  Initialize for engine-side lifecycle instrumentation. Initial callbacks cannot
  call Update, RunTicks or Initialize recursively. Caught refusals preserve a
  healthy zero-time initialization and private settled facts; no duplicate loop.
- Updated likewise rejects nested Initialize/Update/batch. Outside-phase new and
  restored Initialize are truly idempotent and publish no extra revisions.
- Capped elapsed Update really earns2 whole pending steps plus fractional time.
  A healthy three-step batch reports3, preserves earned backlog/interpolation,
  and publishes actual settled entity versions without changing retained facts.
- Zero batch reports0; paced zero elapsed spends previously earned time. Explicit
  diagnostic batches use the same fixed loop while paused and preserve backlog.
- Six saved mixed-path twin batches match actual clock coordinates and hash.
  Mixed elapsed/batch/pause inputs replay canonically through the existing recorder.
- Throwing host Updated callback reports the exception AFTER three real steps and
  settled snapshots; Time/Clock still report3. Removing it permits the next update.
- Three new initial-capture faults: last-seat exception, persistent visibility
  mutation without entity Touch, and clock pause mutation. All initial projections
  are empty/private, not partially prepared observer facts; initialization time0,
  first cause retained, no retries, and poisoned Save/Hash refuse.
- Load at actual game time5 can fail initial projection and exposes no viewer facts;
  initialization still reports0 work at5. Canonical replay refuses the poisoned
  restored session with replay.provider/initial.hash and no verified Game.
  Source and interleaved healthy world hashes remain unchanged.
- Late projection failure after actual batch4, paced1 or batch0 reports the current
  count, not a prior frame. Every viewer retains its own safe facts epoch/deltas;
  pending pacing, Alpha and slow status clear. Subsequent Update reports0 and never
  retries the poisoned callback or replaces the first fault.
- Real public construction preview/command, loaded but undelivered hauling, saved
  varying-size physical batches through delivery/machine construction, retained
  loaded-stage facts, fifty-minus-thirty steel conservation and replay all pass.

Normal acceptance uses public scoped decisions/views/lifecycle/save/replay. Only
pre-initialize setup and actual provider failure instrumentation use engine-side
state. Gameplay values, time, geometry and prototypes remain the same explicit JSON.

## Every build and run

| Attempt | Command/scope | Result | Elapsed |
|---|---|---|---|
| Build1 | dotnet build Beep.SimEngine.slnx |0 warnings,0 errors |12.57s |
| Runtime1 | exact NEW case above, --no-build |233 passed,0 failed/aborted |2044ms |

Build initially returned live session33280; one completion poll confirmed exit0.
No compiler/runtime repairs or reruns. No older/family/full/platform/stale runtime.
All executable inputs frozen after first green; subsequent changes are docs-only.
Each physical phase is bounded by2000 batch Update calls of1-3 steps, with actual
time/job/fault diagnostics. This is not process-watchdog/CPU/soak/allocation proof.

## Remaining scope

Expanded source inventory:1763 repository-visible/prior-referenced paths,all357
engine files,574 Markdown and3400 local links.18 valid five-column phase rows;
both new help references exist. Actual git diff --check exits0 with31 existing
line-ending warnings. Complete357-path engine comparison finds only GameClock,
SimGame and SaveGame changed. All323 prior authored proof JSON unchanged; no new
JSON or engine file. The sole GameClock source SHA deliberately changes from
339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31 to
35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1.
This is the same one clock owner; actual current hashes are retained in the
self-excluding manifest. Ignored/hidden build artifacts are not acceptance proof.

Initial/new/load and actual batch/paced/zero fault-publication cases are now focused
verified, not a full lifecycle regression. All external spatial provider/Matches,
whole-world purity/security, arbitrary custom view methods and omitted codecs,
controller/provider fault breadth, lockstep/fuzz/soak/allocation/performance,
cross-order/mixed productive scheduling, distributed obligations, accounting,
Oil migration, authorized broad P6 and full P1-P12 acceptance remain open.
Earlier checkpoints are historical and were not rerun on2.289.0.

See [stage](../../plans/enhancements/P2_SNAPSHOT_LIFECYCLE_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md), and
[source manifest](P2_SNAPSHOT_LIFECYCLE_SOURCE_MANIFEST.json).
