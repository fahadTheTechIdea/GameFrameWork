# P4-G72: Work-management observation lifecycle

## 0. Problem Statement

The original WorkView returns missing/private/empty work facts before consulting the
World's terminal fault in several reads. Plan edit/group previews have the same
shortcut. Candidate views pass caller lists into provider callbacks rather than
publishing a detached, wholly evaluated result.

## 1. Goals

1. A stopped session refuses these live work queries before ordinary shortcuts.
2. Out values are reset before refusal; candidate lists publish no failed prefix.
3. The original WorkBoard, worker selector, graph, stock and single clock remain owners.
4. Healthy physical work/hauling, saved continuation and replay remain engine execution.

## 2. Design

Add the existing World.RequireHealthyUpdate guard to original WorkView reads and
original WorkBoard plan-owner/group admission. Both candidate projections stage
local facts inside EntityObservation.ReadProvider over the actual IWorkerDispatch,
then publish after health verification. IdleWorkers verifies its complete observation.
No replacement selector, inferred readiness, compatibility path or extra state store.

## 3. API surface

No new production API. Same contracts and typed normal refusals, with terminal
fault precedence. Engine build2.446.0; save154/replay1 unchanged.

## 4. Files to change

Original WorkView, WorkPlans, WorkPlanGroups and build/protocol identities;
one new public-input test and engine-side negative probe. Existing independent
ContractWorker receives a negative-only callback, absent from persisted state.

## 5. Error handling matrix

Healthy missing/private/invalid inputs retain normal refusal codes. A stopped
session throws through the original first fault. A later worker-provider failure
leaves the external candidate list empty, never returns partial success, and
cannot produce a save or advance the sole clock.

## 6. Backward compatibility

None; current build identity is required, with no legacy save migration.

## 7. Verification

One solution compile attempt, then only needed same-case repair compiles.
ONE NEW bounded exact case, minute and half-minute authored time variants, physical
work/hauling, preview/command parity, private facts, partial/terminal saved twins,
pause, replay and session isolation. Negative queries cover original first-fault
precedence and late worker failures. All attempts retained in the evidence document.

## 8. Risks

Arbitrary private CLR mutation and exhaustive alternate providers are not proven.
These scoped fixes do not close the compound P4.014 or global durability gates.

## 9. Out of scope

Oil consumer, business/Tycoon, renderers/adapters, broad/full runtimes and publication.
Original221 requirement texts/checks remain unchanged unless full evidence warrants it.

## 10. Sub-task execution order

1. Inspect actual lifecycle/selector/admission ownership.
2. Fix original query/admission boundaries without a parallel subsystem.
3. Add one thin public-input case and negative engine-side instrumentation.
4. Compile, execute the new bounded exact case, retain every attempt.
5. Update master/current metadata and source identity; keep whole-plan completion open.

## Delivery status

VERIFIED-SCOPED: FINAL590/0/0 in840ms. All three attempts and four compile attempts
are recorded in [the evidence](../../docs/migration/P4_WORK_OBSERVATION_BOUNDARY_VERIFICATION.md).
Original221/180checked/41open unchanged. Four production files revised in their
original owners, no new scheduler/state/clock. Full P4.014 and P1-P12 remain OPEN.
Current1464 source anchors/740 selected inputs/13 quickstart anchors verified;
950 Markdown documents/8548 links/18 master rows and221 literal gates validate.
No post-FINAL executable drift, publication or broad runtime. Protected clock/G19
bytes unchanged; dirty checkout preserved,2694dirty/staged0.
