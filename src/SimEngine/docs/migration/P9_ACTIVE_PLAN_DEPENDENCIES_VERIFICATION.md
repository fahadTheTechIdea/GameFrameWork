# P9-A14 — Active manual plan prerequisites verification

2026-10-07 working-tree checkpoint, not clean-HEAD attribution or full regression.
Build beep-simengine-2.185.0; save101; board35; work system32; work view63;
native recurring component7/public5; public history retention component2; construction view4.
Direct current semantics revision, no legacy reader. Existing fields still use their sole codecs.
The full framework implementation objective remains active.

## Implemented ownership

The existing IPlanBoard.PreviewPlanDependencies/ConfigurePlanDependencies, immutable
PlanDependencyChange/Changes, ConfigurePlanDependenciesCommand and WorkView are extended,
not duplicated. Owned live manual PlanEntries accept canonical prerequisite replacement sets.
Terminal records retain NotPending; recurring admitted records retain SourceOwnedDependencies.
Missing/foreign references refuse explicitly. The complete proposed company topology uses
the existing DependencyGraph compiler before any dependency set is committed.

For an active plan whose proposed prerequisites are unresolved, preview checks every actual
assigned native job through the existing WorkSystem.CheckInterruption permission. A protected
machine returns existing WorkPolicyCodes.NotInterruptible for the complete edit. No otherwise
valid pending prefix commits. Actual callbacks/resources/jobs remain normal engine state.

WorkBoard.PlanOrderBlocker replaces the old pause-only boolean path. It resolves explicit
pause and actual prerequisite pending/cancelled/failed state for native execution, assignments,
candidate readiness, effects, supply source admission and private plan views. There is no
second graph, gate state, scheduler, current-time field or lifecycle. Entire dependency sets
commit before any affected native order is settled.

The existing PausePlannedOrder now takes its actual blocker. Explicit pause keeps established
behavior; dependency waits stop motion and reopen assigned jobs through normal Interrupt/Reopen.
The actual target, native order/job IDs, earned node progress, completed effects and spent
bindings remain. Unspent claims return through the existing resource owner; no refund of consumed
stock. Workerless active nodes also stop at the same gate and release unspent promises. The
only machine can execute a prerequisite rather than remain permanently held by its successor.

Completing order/node effects are gated before further application. Existing completion retry
blockers are preserved instead of overwritten by a dependency blocker; this detail is source
reviewed, not separately fault-injected in this stage. No completed effect is rewound/replayed.
Saved validation allows unresolved actual owned prerequisites on manual active/failed/cancelled
records, but rejects missing/foreign/cyclic topology and successful completion with unresolved
requirements. Recurring native execution retains its frozen authored admission invariant.

Workflows/templates/worker policy still come from explicitly master-listed JSON and existing
contracts. No new JSON loader/schema, default gameplay, live entity copy, index/cache, plan model
or parallel executor.89 identity/schema metadata paths revise current constants/assertions;
functional owner overlaps are recorded separately in the source manifest.

## Verification actually run

Final build: dotnet build Beep.SimEngine.slnx --no-restore; zero warnings/errors,3.46s.
Only the same two exact existing suites ran:

| Exact suite | Passed | Failed | Runtime |
|---|---:|---:|---:|
| Recurring templates: authored groups, private admission and cycle ownership |1101|0|2534ms|
| Recurring templates: physical projects, cancellation and saved continuation |564|0|5945ms|

1,665 final assertions, zero final failures or aborted suites. A13.4/A13.3/A13.2/A13.1/A12/
A11/A10/A8/A7 callbacks remain included. Separate A9/controller/operation/incident/construction/
completion/replay and other suites compile only, not rerun. No third, family or full gate ran.

The first contract run passed1101; the first physical run passed535 and failed1: an obsolete
A8 fault-injection assertion rejected all unfinished prerequisites on active manual records.
That deliberately replaced rule is now tested on completed history, which must still reject
unfinished prerequisites. No runtime fallback or compatibility restriction was added. After
reconciliation both suites were green; further moving/workerless/supply proofs produced the
final results above. Documentation-only edits after final gates change no runtime inputs.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build -- "Recurring templates: authored groups, private admission and cycle ownership"
dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build -- "Recurring templates: physical projects, cancellation and saved continuation"
```

### Direct coverage

- Real command-authored partially earned construction, normal machine policy edits, protected
  interruption refusal, active cycle/foreign-reference/terminal/source-owned refusals, pure
  private previews and atomic pending/active reversal without prefix mutations.
- Actual native/public recurring active shapes refuse manual editing and remain valid saves.
  Immutable retained views do not change; queued atomic commands survive save/load and execute
  only through normal game updates.
- Adding a pending prerequisite stops native work without setting a fake player pause. Exact
  plan/order/target/job/progress remain; the sole machine is freed and the original job is not
  ready. Native supply source returns Expired with the same DependencyPending blocker.
- During ordinary waiting updates no extra work/effect/input is earned/consumed/refunded.
  Physical prerequisite completion uses the released machine, then the original earned project
  resumes. Exactly2 completed structures consume8 finite units from20, leaving12; completed
  preparation and spent inputs are not replayed.
- An actual moving worker is stopped/released at its exact physical position, without teleport.
  Waiting updates leave its position unchanged. The same machine routes to the prerequisite,
  then resumes the successor. Saved native trips and graph continuation agree.
- An active future stage without a posted job reports DependencyPending for actual worker
  candidates using its existing native node/order identity. The candidate read leaves hash
  unchanged; previews do not bypass the execution gate before a job exists.
- An explicitly JSON-authored workerless stage has no synthetic job, freezes its real earned
  node progress, resumes after actual prerequisites, and consumes no duplicate material.
- Physical failed/cancelled prerequisites hold active earned state instead of granting completion
  or implicitly cancelling descendants. Explicit ordinary edits remove those references and
  resume the same execution; saved twins agree throughout. Completed history stays immutable.
- Pausing the sole game clock freezes every native workflow. New callbacks contain player
  commands, updates, authored-content selections and assertions only, no test-owned scheduler.

## Clock, scope and continuation

World constructs GameClock once, SimGame reuses it, and Tick is only Clock.StepIndex projection.
No GameClock/time ownership changes. No artificial engine/world/platform size cap.
OilFieldDays/Tycoon/hosts/demos/platforms/Draw were untouched.
[P9_ACTIVE_PLAN_DEPENDENCIES_SOURCE_MANIFEST.json](P9_ACTIVE_PLAN_DEPENDENCIES_SOURCE_MANIFEST.json)
records current functional edits, metadata paths, prior ownership/proof dependencies and docs.
It is scoped dirty-working-tree evidence, not full-HEAD attribution or regression certification.

This stage edits prerequisites between admitted manual plans, not the authored node graph of
an ongoing activity or a recurring admitted cycle. Those larger topology revisions, fleet/fuel/
team feasibility, broader provider purity/completion fault injection, performance/allocations,
replay/lockstep/soak/full framework acceptance remain open. Do not close the overall goal.

GameClock.cs SHA256 remains339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31.
Final structural checks:144 source and10 document hashes matched with no duplicate paths or
source trailing whitespace. Evidence links resolved; offline help parsed as XML with html root.
Scoped git diff --check passed (ordinary LF/CRLF warnings only). One GameClock construction/
embedded Sync remains; no obsolete IsPlanPaused or preceding2.184.0 current-build identity
remains in C# sources. These are structural checks, not additional runtime-suite evidence.
The lifecycle skill aligned the stage, shared implementation, proof scope and master tracker.
User/repository instructions override generic full-suite advice; only these exact two suites ran.

