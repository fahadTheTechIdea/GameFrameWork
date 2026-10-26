# P4-G3c authored activity executors — scoped verification

2026-10-07. Canonical Beep.SimEngine only. Entire goal/plan ACTIVE. No commit/push.

## Actual invocation ledger

1. `dotnet build Beep.SimEngine.slnx --no-restore`: exit0,0 warnings/errors,10.74s.
   Live session24208 completed through its original handle, not restarted.
2. `dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Authored activity executors: custom physical phase, timed DAG and saved waits"`:
   exit0,47 checks/0 failures/0 aborted,420ms, first runtime attempt.

ONE build, ONE runtime invocation, ONE distinct exact NEW bounded case.111 older files/184 identity/
save assertion lines compile-migrate ONLY:105 build and102 save assertions,23 combined lines.
58 existing JSON files migrate175 nodes (including two instant timed nodes) and9 generated line
definition executor references.29 existing C# fixture files migrate22 inline node definitions and16
duration setter lines; four existing generated-node test call sites/families require explicit executor
references. These are source/compile migrations, NOT older runtime proof. Source-only malformed/
replacement-string fixtures and broader generated plan/line scenarios still need focused acceptance.
Existing older raw byte/layout matrices and unrelated stale expectations remain runtime-unverified.
No older/family/full/platform/stale runtime gate. Source/tests/content FROZEN after green; docs only.

## Sole ownership

ActivityExecutorDef/IActivityExecutor uses the existing immutable selected-content catalog/base aliases.
Staffed and timed declarations are explicit master-listed work-module JSON, alongside retry policy in
the existing shared work document; no module-generated default definitions or second executor catalog.
ActivityNodeDef requires executor and duration. Duration delegates to existing RationalDuration;
hour decimals migrate exactly to reduced rational values without reserializing unrelated JSON numbers.
Unknown/missing executor/duration/legacy hours fail; no old loader or role-inferred executor survives.
ActivityGraph captures the actual selected executor and its typed role contract through the same DAG.

Executor contracts own definition checks, phase composition validation, pure itinerary construction/
matching/re-approach and work-rate reading. WorkSystem/WorkBoard still own actual jobs, machine claims,
node progress, resource/cost bindings, physical material spending, completion, cancellation and Sync.
AdvanceOrder no longer constructs native GoTo/Work/LeaveDock itself; codec itinerary validation delegates
to the same selected executor. Generated LinePlan nodes explicitly use their authored LineDef executor.
No second scheduler, runtime graph, progress ledger, game clock or copied stock/reservation owner.

IWorkPhaseExecutor Purpose is immutable captured semantic metadata, validated before Initialize.
Purpose drives native worker state and dock/re-approach/departure checks, NOT algorithm dispatch.
Stable phase IDs still select the actual provider contract. Phase Pause delegates to existing policy
pause state and preserves job/node progress/materials. Ready RunJob reevaluates paused phases; AdvanceOrder
does not silently unpause the actual machine while a custom phase remains blocked.

ActivityNodeView.Executor exposes a detached stable authored ID, not a strategy/context/provider handle.
Build2.239.0/save139/WorkView79. Existing Board43/WorkSystem41 and other payload bytes remain unchanged;
save/build/view identity is strict. One unchanged GameClock, sole construction World.cs:96; SHA256
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.

## Actual proof

AuthoredExecutorProofModule is an engine-side content extension, not harness gameplay. Its delayed
productive phase uses ONLY actual order.CreatedTick plus sole session time and authored delay. It
calls the same engine-issued Pause/AdvanceProductiveWork operations, never mutates stock/progress,
introduces another timer/store, manufactures a worker or runs a host scheduler. Its explicit module
manifest/data definitions compose through the ordinary catalog. Native startup/navigation/work/
knowledge modules create all entities/resources. The test driver submits player commands/Update only.

- Reverse authored node order canonicalizes to operate then settle in the same DAG. Actual selected
  executor aliases and staffed/timed contracts are exposed; custom phase has productive purpose.
- Actual machine travels into the authored custom phase, waits until its90-minute order-relative
  clock admission, retains the same paused job/node, earns no productive work and spends no startup
  materials. The dependent timed stage remains blocked. Wait save/load and bounded continued wait
  are identical, with detached/private/pure executor views.
- Actual custom phase admits normal physical productive work. Native transaction20->12 consumes eight
  once; retained paused view remains unchanged. Partial productive save/load continues identically.
- Actual physical completion admits the five-minute timed successor with NO machine job. Real elapsed
  updates earn its progress; saved continuation is identical and never recharges or invents materials.
- A fresh execution gets its own authored delay. Normal cancellation during its wait releases only
  unspent work commitments, preserving12 stock and canonical saved state. New interleaved session starts20.
- Missing/unknown executor, executor/roles mismatch, missing duration, old hours, unknown/wrong-purpose
  physical phase and zero timed rate refuse without implicit defaults.

Every Reach is bounded900 Updates with current time/node/state/job/blocker diagnostics. This proves
one actual custom phase through native staffed executor content and a timed successor, NOT arbitrary
new executor types, every migrated workflow, all phases or broad provider purity.

## Source gates and remaining requirements

Scoped path/hash/link manifest accompanies this report, excluding its own hash. It is not whole-repo
or runtime acceptance. Diff check exit0 with existing LF/CRLF warnings. Historical evidence remains
historical. No OilFieldDays/host/demo/Draw/platform/business gameplay changes or broad tests.

Scoped inventory486 paths/82 Markdown/1487 local links/0 broken/hash mismatches. The
[generated manifest](P4_AUTHORED_ACTIVITY_EXECUTORS_SOURCE_MANIFEST.json) records this checkpoint only.

Remaining: new executor-family proof, arbitrary/multi-endpoint/team/process/haul strategies, all
generated workflows and authored template migration (G4), interruption/recovery/re-approach/dock/haul
phase coverage, malicious/mutating/retained invocation and global purity/fault matrices, richer progress/
ETA/cancel/replay views (G5), stress/CPU/full regression and all remaining P1–P12 requirements. Full G3
is not closed merely because staffed/custom-phase/timed execution is focused-green. No publication
until the FULL goal and plan are finished and verified.
