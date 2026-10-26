# P4-G3c — Authored node executor contracts

## 0. Problem

G3b removed enum phase dispatch, but WorkSystem.AdvanceOrder still builds a fixed native itinerary
and WorkBoard.ValidateActivityItinerary checks that same fixed shape. ActivityNodeDef also infers
worker/timer execution from roles and accepts only hours. A new workflow must instead select its
executor explicitly through the existing immutable content catalog.

## 1. Goals

1. Every activity node has an explicit IActivityExecutor and rational GameDuration JSON. Remove the
   old hours/implicit-executor authoring path; directly migrate existing authored/generated nodes.
2. Native staffed/timed executor definitions come from master-listed JSON, not automatic defaults.
3. Executor contracts own node validation, pure itinerary creation/matching/re-approach and rate
   reading. WorkSystem/WorkBoard continue to own assignment, progress, resources, completion and Sync.
4. Phase semantic purpose drives worker state and dock handling without selecting algorithms by type.
   A custom productive phase can be referenced by authored staffed content and execute normally.
5. ONE new bounded native player/mod-content proof covers custom execution, deterministic pause,
   physical materials, timed dependencies, private views and save/load. No full suite.

## 2. Design

New Standard/ActivityExecutors.cs: IActivityExecutor, immutable ActivityExecutorContext and authored
ActivityExecutorDef family. Native staffed_activity_executors explicitly name travel/work/departure
phase IDs. timed_activity_executors explicitly author a positive rate. Existing content base aliases
are the sole registry. IActivityNodeDefinition.Executor is mandatory and captured by ActivityGraph.
Board validates exact selected-content alias and delegates definition checks; composition validates
phase IDs after the actual WorkSystem catalog is captured, before Initialize.

Node duration uses the existing JsonReader.RationalDuration schema {numerator,denominator,unit},
not a second time parser. ActivityNodeDef.Read requires executor and duration; Make also requires
executor. LineDef explicitly names the executor passed to its generated nodes. No legacy loader,
implicit timed/staffed choice, duplicate graph or second progress owner survives.

Create actual jobs only after a pure copied itinerary validates against the executor and composed
phase catalog. Codec validation delegates to the same executor; physical departure still requires
earned node progress. Executor-directed re-approach replaces node-specific phase decrement rules.
Timed and productive rate reads are pure and delegate advancement to existing WorkBoard.AdvanceWork.
An engine-issued phase Pause operation retains actual job/node progress and typed blockers.

IWorkPhaseExecutor declares semantic purpose (travel/productive/pickup/delivery/departure/other).
The catalog captures and validates this immutable metadata. Purpose informs state/dock consumers,
never a dispatch switch; Execute still resolves the actual stable phase ID. Native haul itineraries
remain their existing strictly validated physical protocol, not silently generalized by this slice.

ActivityNodeView adds the stable selected executor identity. WorkView schema79; build2.239.0/save139.
Board43/WorkSystem41 retain their bytes; identity breaks exactly, no old saves/content translation.

## 3. API

IActivityExecutor.Id/RequiresWorker/ValidateDefinition/ValidateComposition/CreateItinerary/
MatchesItinerary/ApproachIndex/ReadRate; ActivityExecutorContext actual session/order/node/worker;
IActivityNodeDefinition.Executor; Toil.Executor unchanged; phase Purpose/Pause; immutable view ID.
All rules remain engine/module-side; hosts send commands/read detached views only.

## 4. Files

ActivityExecutors, ActivityGraph, Work/WorkDocks/WorkScheduling/WorkModule/WorkPhaseExecutors,
Networks, WorkView, SaveGame; canonical engine JSON and compile-only inline fixtures/Make callers;
focused native mod proof/assets, Program; stage/master/AGENTS/evidence/scoped manifest.

## 5. Errors

Missing executor/duration/unknown references/invalid role shape/rate/phase purpose refuse content
or composition before Initialize. Invalid/mutated itinerary refuses before actual job creation or
restore. Provider refusal preserves progress and materials; invalid provider mutation faults, never
compensates or fabricates progress. Wider purity/fault/provider matrices remain open.

## 6. Compatibility

Direct development breaking migration, no compatibility path. Historical reports remain historical.

## 7. Verification

Read/migrate source, then build. ONE exact distinct NEW bounded player proof; repair/rerun only
observed failures. Record every invocation. Older fixtures compile-only are not runtime proof.
Freeze source/tests/content after final green; docs-only hash/link inventory afterward. Full gates
remain pending unless user explicitly authorizes them.

## 8. Risks

Source-phase purpose is not a global provider sandbox. Native staffed geometry remains a canonical
travel/work/optional-departure implementation, while new executor types can own other itineraries.
Broader team/multi-endpoint/process/transport executors and all native haul/dock save/fault matrices
need later gates. Mechanical JSON migration must preserve exact authored durations and unrelated data.

## 9. Exclusions

No OilFieldDays/hosts/demos/Draw/platform/business changes. No second clock/scheduler/claims/catalog.
No whole-G3/G4/G5/P1-P12 completion claim or commit/push until FULL goal is proven complete.

## 10. Order

- [x] Contracts/content and same-owner runtime/view migration.
- [x] Direct authored/generated/compile-only fixture migration and one bounded native proof.
- [x] Focused verification, evidence/trackers and source manifest handoff.

Verified2026-10-07: ONE clean build10.74s (live session24208 resumed), ONE exact NEW bounded case
47 checks0 failures/aborts420ms first runtime attempt. Source/tests/content frozen after green.
[Evidence](../../docs/migration/P4_AUTHORED_ACTIVITY_EXECUTORS_VERIFICATION.md). Full G3/G4/G5/P1-P12
remain open; this checkpoint is not full workflow/provider/platform/replay/stress acceptance.
