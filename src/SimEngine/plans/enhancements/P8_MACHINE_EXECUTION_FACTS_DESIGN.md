# P8-D4a — Actual machine execution facts

Status: scoped implementation verified64/0/0 in ONE NEW exact bounded case on2.313.0.
Two builds, final clean5.32s after new-test command-property repair. Earlier cases not rerun.
[Evidence](../../docs/migration/P8_MACHINE_EXECUTION_FACTS_VERIFICATION.md).
Production machine-team admission/lifetime and full goal remain open.

## 0. Problem Statement

`src/Beep.SimEngine/Standard/Work.cs` resolves `Worker.GetState` by obtaining a mutable
`Job` from concrete `WorkBoard`. `WorkExecutionFacts` already supplies detached incumbent
facts to preemption, but no shared read contract exposes an actual current assignment.
Production must not infer a reservation from an idle candidate, role tag or entity state.
This is the observation prerequisite for entity tools, not their admission/lifetime completion.

## 1. Goals

1. The existing WorkBoard alone projects reciprocal live worker/job assignments through
   `IWorkAssignments`; no job handle, copied assignment ledger or acquisition is introduced.
2. Reuse and extend `WorkExecutionFacts` with exact order/node/plan, current authored phase,
   phase purpose/target and dock references. Retained values cannot change after Update.
3. Worker state resolution uses that contract, preserving paused/working/loading semantics.
4. A company-private WorkView method returns the same facts, with explicit null for an idle
   worker and refusal for absent, foreign or non-worker entities.
5. A new thin bounded player case verifies real approach, authored wait, productive work,
   cancellation, detached reads, saved continuation and replay; no direct world mutations.

## 2. Design

`IWorkAssignments.ReadAssignment(PlayerId viewer, EntityId worker, out WorkExecutionFacts? value)`
is bound to the already persistent WorkBoard. The method verifies current worker ownership,
reads its actual job identity through the existing entity observation guard and verifies the
reciprocal native job/worker/company relationship. Idle is `Code.None` with null, never a
fabricated ready actor. An invalid retained assignment is corrupt state, not idle fallback.
Phase facts derive from the current native itinerary and composed `IWorkPhaseCatalog`.
There is no cached result, new codec, JSON selector, second clock or separate lifecycle.
Existing JSON activities/executors/roles remain the sources for every projected identity.

## 3. API surface

New engine contract `IWorkAssignments`; expanded existing immutable `WorkExecutionFacts`;
`WorkView.ReadWorkerAssignment(EntityId, out WorkExecutionFacts?)`. Work view schema87,
build2.313.0; save148/replay1 and native codec layouts remain unchanged.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | `src/Beep.SimEngine/Standard/WorkContracts.cs` | medium |
| New | `src/Beep.SimEngine/Standard/WorkAssignments.cs` | medium |
| Modify | `src/Beep.SimEngine/Standard/Work.cs` | medium |
| Modify | `src/Beep.SimEngine/Standard/WorkModule.cs` | low |
| Modify | `src/Beep.SimEngine/Views/WorkView.cs` | low |
| Modify | `src/Beep.SimEngine/Kernel/SaveGame.cs` | low |
| New | `tests/Beep.SimEngine.Tests/WorkAssignmentFactsTests.cs` | medium |
| Modify | `tests/Beep.SimEngine.Tests/Program.cs` | low |
| Modify | `tests/Beep.SimEngine.Tests/testdata/current-engine-protocol.json` | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Missing/dead/private worker or non-seat observer | NotFound, no facts |
| Owned non-worker | NotWorker, no facts |
| Idle worker | None, null facts |
| Nonreciprocal or terminal retained assignment | CorruptStateException |
| Assigned/paused actual job | Detached actual facts, no claims or mutation |

## 6. Backward compatibility

Development only; no compatibility overload, legacy reader or fallback owner.

## 7. Verification

One compile build and only the new exact bounded `Work assignment facts` case. Earlier suites
compile only. Full regression remains pending human authority; no Oil/platform runs.

## 8. Risks

Read facts are not admission authority. Future entity-tool execution must freshly revalidate and
acquire through WorkBoard, retain physical assignments during output waits and release through
its native cancellation/departure paths. This stage deliberately claims none of those transitions.

## 9. Out of scope

Transformation worker/team acquisition, scoped supply/export, new schedulers, renderers, required
business/money and full-plan completion. Original P8 compound acceptance stays unchecked.

## 10. Sub-task execution order

1. Extend the existing facts and bind the actual assignment query.
2. Migrate worker state and expose the private immutable view.
3. Add a thin exact player proof; build once, repair only if needed, run that case.
4. Record exact evidence and remaining integration in the master tracker/completion audit.
