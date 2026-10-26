# P4-G1 — Sole authored activity graph and named execution

Date: 2026-10-05. Current working-tree evidence, not committed-HEAD/full-regression completion.
Engine **2.45.0 / save20**, WorkBoard schema7, WorkView feature schema6.
No old content/array/save reader. No oil, Tycoon, host/demo, Draw, renderer or platform project changes.

## Final verification

`dotnet build Beep.SimEngine.slnx --no-restore`: zero warnings/errors, final build2.14s.
Runtime, non-friend public ExtensionProof, testing library and test project compile together.

Exactly two directly affected named suites ran, including correction reruns:

```powershell
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Activity DAG: canonical graph validation and strict authored admission'
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Activity DAG: named parallel physical work, private views and saved execution'
```

| Exact suite | Passed | Failed | Final runtime |
|---|---:|---:|---:|
| Canonical graph validation and strict authored admission | 31 | 0 | 186ms |
| Named parallel physical work, private views and saved execution | 87 | 0 | 333ms |
| Total | 118 | 0 | Two suites, neither aborted |

Counts describe the final run, not repeated assertions added together. No broad filter/full gate ran.
Affected older source/proof/JSON consumers compile; their complete suites were not rerun.
The first physical fixture placed workers at substantially different travel distances, so their actual
work periods did not overlap and the simultaneous-job assertion failed. Corrected spawn geometry proves
actual overlapping machine execution without changing engine travel rules. Intermediate compile errors
from migration/test qualification were fixed before the final run; no test used a failed-build artifact.

## One owner per responsibility

- ActivityDef replaces OrderDef in the normal content registry. ActivityNodeDef, IActivityNodeDefinition,
  IActivityDefinition and IActivitySource replace positional plan contracts. Only master-listed
  activities/nodes JSON is accepted. Existing command names retain their ordinary work-order meaning.
- ActivityGraph is the sole immutable compiler. Stable IDs and sorted dependency lists resolve
  arbitrary forward references. Kahn ordering uses ordinal identity tie-breaks; iterative cycle paths
  identify actual nodes. Empty/duplicate/missing/self/repeated edges fail explicitly. No authoring cap,
  recursive depth guard, runtime scheduler or second graph registry was added.
- Graph snapshots own their node data; named dictionaries alias the same nodes. Activity/construction
  definitions read/resolve once and discard authoring staging lists. Physical effects use the graph's
  dependency ancestry rather than the removed Index/AfterIndex compiler. Unrelated machines do not
  authorize a timer-only physical effect.
- WorkBoard owns existing orders/jobs and one ActivityNodeExecution per node. Named lookups alias
  those records; parallel state/job/progress arrays and numeric Job.Step are removed. WorkSystem still
  owns eligibility, toils, actual worker assignment/travel, wide progress and native input consumption.
- One native bill aggregates all eligible supplying nodes per order. LogisticsService replaces that
  owner's complete resource request, withdrawing stale resource lines without a new demand ledger.
  Each actual node bill is consumed once through the existing resource transaction coordinator.
- Jobs refer to NodeId. Successful completion clears pruned job links; cancellation frees owned jobs/
  demand and retains attained progress. Typed requirement pauses apply to machines and timers.
  Dependency/resource/worker/requirement blockers are recorded per node, not reconstructed by hosts.
- WorkBoard.Sync serializes those same named records/links for save/load/hash. Load rejects wrong
  counts/IDs, orphan/mismatched live jobs, invalid progress/dependency/state and worker/haul receipts.
  There is no old array reader or automatic recovery/default for corrupt state.
- WorkView.TryCopyNodes exposes immutable detached IDs/dependencies/state/job/progress/required time/
  blocker. Owner-private node/effect/requirement diagnostics remain pure. Summary views reuse the graph.
  Construction/demolition/line definitions, work effects/incidents and commands use the same contracts.

## Physical and extension proof

Existing Colony.Factory explicitly lists the added activity-dag.json in its single master content path.
The ordinary external requirement provider controls guarded work; no test/harness schedules gameplay.
The external retention component chooses whether the sole board keeps terminal records; it stores no
progress, jobs or graph. The test only submits player commands, reads facts and drives engine Update,
apart from explicit setup/fault instrumentation.

Proved: out-of-order authored joins; combined two-branch material demand; real hauling to an initially
empty input store; two physical machine jobs working concurrently; one-machine contention and eventual
sequential execution; dependency waits; provider-driven machine/timer pause; native joined effect once;
detached private node facts; saved supplying, working, paused, cancelled and completed continuations.
Stages have 500-tick failure limits with named state/progress/blocker diagnostics.
4096-node/deep-cycle compilation is iterative; it is not a stress/allocation/whole-session size guarantee.

Reordered documents produce identical topology and WorkBoard.Sync state/outcome. Their exact authored
content fingerprints may differ. No cross-content save compatibility or identical whole-game hashes
across differing document identity is claimed. Within each content identity, saved twins hash equally.
Corrupt-node tests use the sole direct codec or saved-session loading on disposable fixtures, not a
parallel parser or a claim that partial corrupt reads roll back.

## Remaining work

G2 all/any expressions, typed candidate selection, tools and atomic acquisition; G3 authored executors/
closed-toil replacement; G4 reusable construction/line/workflow templates; G5 prior-activity facts,
critical-path/ETA and additional durability; P5 oil workflow migration and P7–P12 larger game-control
capabilities remain open. Machine/role/native bill/nonconsuming providers are not every future category.
No seismic/logging prerequisite is imposed; oil was not modified or integration-tested.
No broad regression/replay/lockstep/stress/soak/allocation or full stage gate is claimed.

The lifecycle skill guided the recorded one-owner breaking migration and source-specific evidence.
Repository/user exact-suite limits override generic full-regression guidance. The single master tracker
records G1 focused verification, not complete P4 or the overall framework objective.
