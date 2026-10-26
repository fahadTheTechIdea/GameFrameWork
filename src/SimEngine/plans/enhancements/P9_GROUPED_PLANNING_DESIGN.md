# P9-A15 — Grouped planning control and truthful template admission

## 0. Problem Statement

The original P9 acceptance still requires grouped invalid/valid template diagnostics, pause, reorder, repeat and cancellation. These operations already belong to WorkBoard, but `WorkPlanTemplates.ReadTemplateProvider` verifies callbacks in finally, masking an original failure, and lacks transient reentry/faulted-query admission. `ExpandPlanTemplate` can return rejected intent alongside a failed admission.

## 1. Goals

1. Preserve the first unexpected template-provider failure, refuse reentry and faulted publication, and observe existing identity/clock/map facts without a second state owner.
2. Keep expected invalid content/overflow as typed per-entry refusals when observation remains healthy.
3. Publish expanded intent only on whole-group success; a rejected group never publishes a partial executable intent.
4. Verify all named group controls with authored dependency links, real hauling/construction, retained progress, private immutable facts, saved twins and ordinary replay.

## 2. Design

Revise the sole `WorkPlanTemplates.ReadTemplateProvider` observation boundary. A transient guard rejects recursive callback observation. Successful callbacks verify actual identity counters, clock/entity identity and selected tile-map version before returning. Expected authoring refusals verify observation before propagating to the existing typed per-entry evaluator. Unexpected errors latch the original failure and rethrow; finally clears only the transient guard. Template public entry points require healthy state even when a missing or malformed input would otherwise return early.

`ExpandPlanTemplate` uses a local candidate; publish its out intent only after existing `PreviewPlanGroup` admits the complete candidate. No alternate compiler, group membership store, plan queue, reservations, progress, timer or template execution subsystem.

Use the existing independent construction registration, sole construction declaration, master-listed template JSON and original native physical owners. A thin proof uses ordinary commands/views/Update; only explicitly armed negative callbacks live in the proof module.

## 3. API surface

No new gameplay subsystem or host API. Existing IPlanBoard template admission and group commands/views retain their contract; failed expansion now returns null intent. Persistent schemas remain unchanged unless actual state changes.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| Modify | Standard/WorkPlanTemplates.cs | 65 | medium |
| Modify | tests/ExtensionProof/ConstructionContentContractsProof.cs | 30 | low |
| Modify | tests/Beep.SimEngine.Tests/ConstructionContentContractTests.cs | factory only | low |
| New | tests/ExtensionProof/content/grouped-planning.json | 10 | low |
| New | tests/Beep.SimEngine.Tests/GroupedPlanningControlTests.cs | 240 | medium |
| Modify | protocol identity, proof embedding, case registration | 5 | low |

## 5. Error handling matrix

| Condition | Before | After |
|---|---|---|
| Unexpected provider error | finally may replace error | original first fault preserved |
| Reentrant preview | recursive callbacks | transient observation refusal |
| Invalid authored member | typed per-entry refusal | same refusal if state remains healthy |
| Group rejected after expansion | failed check plus nonnull intent | failed check, null intent |
| Faulted query with invalid input | may return ordinary refusal | healthy publication refuses |

## 6. Backward compatibility

Development only; no legacy or compatibility. Keep original plan records, graph links, physical execution and saved codecs.

## 7. Verification

Build completed slice and run ONE NEW exact bounded group-control case only. Runtime30000ms, owned termination5000ms, output30000 characters, stage256 Updates. Repair/rerun that same case if needed. No old/family/full/Oil/Tycoon/platform runtime. Evidence for a named original acceptance item must match that item's scope; aggregate/full gates stay open.

## 8. Risks

Template observations also execute during stored-template checkpoint validation; transient state must never be serialized and legitimate outer validations must remain usable. Invalid-input exceptions must not fault a healthy game. Group repeat remaps internal dependencies to new actual IDs and does not copy completion, resources or entities. Broad arbitrary CLR purity is not established by scoped identity/map observation.

## 9. Out of scope

New platform/Draw logic, Tycoon or protected G19 migration, duplicate plan/template/clock owners, arbitrary provider sandboxing, world-size caps, full suite and publication before full plan completion.

## 10. Sub-task execution order

1. Repair sole template observation and whole-group out intent.
2. Add authored linked template and ordinary-player grouped lifecycle proof with negative callback instrumentation only.
3. Build completed slice and execute one bounded case; preserve attempts.
4. Update stage, master and completion audit with truthful scoped evidence.

## P9-A15 delivery — original grouped-planning acceptance

[Stage](P9_GROUPED_PLANNING_DESIGN.md), [evidence](../../docs/migration/P9_GROUPED_PLANNING_VERIFICATION.md). Core2.406.0/save154/replay1/work-view96. Original WorkBoard template observation preserves first unexpected failure, rejects reentry/swallowed faults and verifies identity/clock/map facts; healthy authoring refusals remain typed. Whole expansion returns immutable intent only on success, exposed through existing WorkView delegation. Stored/selection/authored faulted early returns refuse. ONE NEW exact FINAL447/0/0 in1856ms (PID3916 Exited0/2007ms observed), outer30000ms; four same-case attempts/four clean builds retained. Native/direct linked grouped templates verify distinct invalid/valid entry results, no partial admission, whole pending reorder, partial physical pause/save/resume, cancelled/completed repeat with fresh actual links, pending and saved active-plus-pending cancellation, physical16 consumption/two structures and consumed8 retained12 on cancellation, private detached facts/replay/isolation.25 callback fault probes and healthy typed refusal pass. P9.acceptance.013 is checked with its original requirement text unchanged; only that checkbox changes, all other original gates retain state. Original aggregate/full acceptance and G19 authority remain OPEN; no publication.
