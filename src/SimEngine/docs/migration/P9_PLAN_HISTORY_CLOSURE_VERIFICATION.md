# P9-A13.4 — Single-admission history dependency closure evidence

Historical checkpoint. [Current M103 callback/physical evidence](P9_PLAN_REFERENCE_OBSERVATION_VERIFICATION.md#current-m103-first-failures-and-full-component-owner-observation--2026-10-23)
does not rerun the broad closure-chain/group helpers or alter their algorithm.
[A14 active dependencies](P9_ACTIVE_PLAN_DEPENDENCIES_VERIFICATION.md) is a separate dated scope.
Prior closure results/source manifest remain historical, not current source hashes.

2026-10-07 working-tree checkpoint, not clean-HEAD attribution or full regression.
Build beep-simengine-2.184.0; save100; board34; work system31; work view62;
native recurring component7/public5; public history retention component2; construction view4.
Saved schemas and the sole game clock are unchanged. The full framework goal remains active.

## Implementation and ownership

SafeHistoryCohorts on the existing WorkBoard formerly repeated whole native reference admission
after each rejected dependency wave. It now groups actual candidate cohorts by company, builds
the transient native ID-to-cohort lookup once, and calls PreviewRetirePlanHistory once per company.
Initial diagnostics reject whole cohorts in stable entry order and preserve the first direct
blocker. A queue visits actual rejected cohort members and their PlanEntry.Dependencies. A
candidate prerequisite not already rejected receives HistoryDependency and enters the queue once.
Safe cohorts keep their original order. Final RetirePlanHistory still performs ordinary native
admission before mutation; no weakened reference or provider-observation rule is introduced.

A retained dependent retains prerequisites, not descendants. Whole groups may contract an acyclic
native DAG into a cohort cycle; the rejection set terminates without changing that actual DAG.
No persisted topology, index/cache, completion tokens, second board/history model, contract,
serializer, scheduler, game loop, cleanup timer or clock was added. References still win over
age/newest retention targets; IDs never recycle. No artificial engine/world/platform size cap.

Only WorkPlanHistoryPolicy.cs changes the runtime algorithm. Existing reference fixture plumbing
accepts explicit edits to the selected master-listed history JSON. New proof callbacks use
ordinary commands, cancellations and game updates, not injected ages/records or test-side cleanup.
79 existing current-build assertions/constants move mechanically from2.183.0 to2.184.0.

## Verification actually run

Build: dotnet build Beep.SimEngine.slnx --no-restore; zero warnings/errors,3.78s.
Only these same two exact existing suites ran:

| Exact suite | Passed | Failed | Runtime |
|---|---:|---:|---:|
| Recurring templates: authored groups, private admission and cycle ownership |1062|0|1692ms|
| Recurring templates: physical projects, cancellation and saved continuation |496|0|4142ms|

1,558 assertions, zero failures or aborted suites. No runtime-test failure occurred in this stage.
A13.3/A13.2/A13.1/A12/A11/A10/A8/A7 callbacks remain included. Separate A9/controller/operation/
incident/construction/replay and other suites compile only; no third/family/full gate ran.
Documentation reconciliation changed no runtime/test inputs after these green gates.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build -- "Recurring templates: authored groups, private admission and cycle ownership"
dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build -- "Recurring templates: physical projects, cancellation and saved continuation"
```

### Direct counted and physical coverage

- 128 actual sequentially command-authored, immediately cancelled plans form a native chain.
  Required authored512-minute age is earned through ordinary updates using the sole clock.
  A service provider holds only the actual tail; all actual ancestors remain as HistoryDependency.
  The tail keeps its direct HistoryProvider blocker. One diagnostic observation makes exactly128
  service-reference reads for128 aged records, not one whole scan per ancestor.
- Private foreign-company diagnostics make no reads of these providers. Preview leaves the native
  hash and clock unchanged. Save/load reproduces all128 native identities/blockers with the same
  counted admission, without a saved shadow index. Paused games and resumed twins remain identical.
- A command-authored first -> middle -> last DAG groups first/last together, contracting into
  an A/B cohort cycle.4 actual plans produce4 service reads. The held group retains both members;
  middle receives HistoryDependency. An otherwise eligible child depending on middle remains
  eligible, proving dependency direction is not reversed.
- Ordinary subsequent updates retire only the child, preserve the3 referenced actual records,
  and continue identically after save/load. Existing suites retain prior real physical projects,
  material consumption, provider mutation/fault, native/public recurrence and controller coverage.

## Clock and source evidence

World constructs GameClock once, SimGame reuses that same instance, and World.Tick is a read-only
projection of Clock.StepIndex. Current-time Sync remains embedded once by World. GameClock.cs
SHA256 remains339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31.
Local timestamps and work durations derive from that clock; no independent current-time field
was added. Draw/platforms, hosts/demos, OilFieldDays and Tycoon were untouched.

[P9_PLAN_HISTORY_CLOSURE_SOURCE_MANIFEST.json](P9_PLAN_HISTORY_CLOSURE_SOURCE_MANIFEST.json)
records functional edits, mechanical identity paths, unchanged ownership/proof dependencies and
document hashes. It is scoped working-tree evidence, not attribution of all prior dirty changes.
Prior A13.3 source manifest is retained as a historical checkpoint.

## Limits and continuation

The queue phase visits each rejected cohort and its actual members/edges once. This is counted
closure evidence, not certification that total diagnostics or updates have constant/linear CPU
or allocation cost. Initial/final native admission, grouping/newest sorting and provider-defined
read work retain their existing costs. Arbitrary cross-provider/session/static-state sandboxing,
active workflow revision, fleets/fuel/team feasibility, performance/allocations, replay/lockstep,
soak and full framework acceptance remain open. The objective is not complete.

The lifecycle skill aligned the staged design, ownership-preserving implementation, focused
evidence and master tracker; explicit repository/user rules kept verification to the two suites.

Final structural checks:140 source hashes and10 document hashes matched, with no duplicate
paths or source trailing whitespace. Stage/evidence links resolved; offline help parsed as
XML with html root. Scoped git diff --check passed (ordinary LF/CRLF warnings only).
One GameClock construction and embedded Sync remain; its digest above is unchanged. No
preceding2.183.0 current-build identity remains in C# sources. These checks are not runtime gates.

