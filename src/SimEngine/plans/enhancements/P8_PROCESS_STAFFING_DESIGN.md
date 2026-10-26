# P8-D4c — Process-bound automatic staffing lifetime

Current M159/engine2.776: process staffing now shares the contract-based JSON declaration
and original runtime with independent metadata. Queue IDs, posting budget, actual work
lifetime, sibling exclusion, saved departure and physical material semantics are unchanged.
Both native and independent process variants104/0 each and exact final replay hashes pass;
the corresponding facility variants100/0 each are separate affected checks.
[Current implementation and ownership](P8_PRODUCTION_STAFFING_DESIGN.md#current-m159--shared-staffing-metadata-2026-10-25),
[all four exact results/self-review](../../docs/migration/P8_PRODUCTION_STAFFING_VERIFICATION.md#current-m159-contract-authored-staffing--2026-10-25).
Focused-only acceptance; quality/lot deferred. Broader compound gates remain open.

## Historical D4c implementation

Status: scoped native automatic staffing implemented; FINAL102/0/0 in2790ms in one new bounded case.
Six builds (two repaired compile failures), final clean7.18s. Core2.315.0/save149/replay1;
transformer9/staffing2/WorkBoard45/transformation-view17/work-view88. Earlier cases not rerun.
[Evidence](../../docs/migration/P8_PROCESS_STAFFING_VERIFICATION.md). Whole P8/P1–P12/goal remain open.

## 0. Problem Statement

D4b reads independently posted facility work. It cannot distinguish two named processors using the
same recipe, or keep a physical team on duty for a queue entry's actual lifetime. Queue entries need
stable execution identities before material startup, and ordinary work needs an explicit lifetime
owner. No second worker scheduler, reservation book, progress store or game clock is appropriate.

## 1. Goals

1. Every queued transformation has a stable positive identity, allocated by its existing provider.
   Startup, WIP, cancellation, repeat, save/hash and private views use that same identity.
2. Gates receive the actual named ITransformer, not only its enclosing entity and recipe.
3. Explicit JSON process staffing posts ordinary WorkBoard orders bound to the exact named processor
   and queue execution. Facility staffing remains an explicitly authored different policy.
4. Existing productive jobs retain their physical workers after earning the authored minimum work
   while their execution owner needs service. Native parallel complete-team graphs are supported
   here; arbitrary dependent service graphs remain a later extension. Current work policy, reach, roles and requirements
   still govern each update. Ended/cancelled process service finishes through ordinary departure.
5. Batch, continuous, waits, saved continuation and sibling exclusivity run entirely in the engine.

## 2. Design

Extend TransformationQueueEntry and TransformationSnapshot in place; use the native existing
_nextExecution allocator for queue admission rather than material startup. Repeats allocate a new
execution. Extend the gate contract once to receive ITransformer. Add an optional contract-based
activity lifetime binding to the existing Order codec. WorkBoard remains the sole posting/job owner;
generic completion consults its declared named lifetime provider, never a concrete production part.

Normal process staffing/graph configuration is authored in process-staffing.json, listed by its
master JSON alongside shared roles/recipes. The player driver does not author its normal graph.
StaffedTransformationGate stays stateless. In explicit process mode it reads/posts native orders
through IActivityExecutionOrders; their bindings identify source, processor and execution. The
transformation manager prepares staffing before provider Advance. No gate/view read posts work.
An automatic activity is a parallel service graph: each node has one worker, positive minimum
duration and no completion effects/dependencies. All nodes are covered exactly by requirements;
one activity's nodes have the same authored count, representing that many complete teams. Unequal
counts use separate authored activities. This prevents ghost nodes or silently overstaffed graphs.

## 3. API surface

IActivityExecutionLifetime and IActivityExecutionOrders; immutable ActivityExecutionBinding and
service-order facts; original compact completion history keys include source/capability and preserve binding; WorkExecutionFacts exposes the original binding. ITransformationGate.ReadGate
receives ITransformer. Explicit staffing JSON mode plus processes for process mode. No compatibility
overload, implicit mode, adapter or second clock.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | Standard/TransformationContracts.cs, Transformations.cs and existing WIP/flow/operating partials | high |
| Modify | Standard/TransformationStaffing.cs, TransformationSignalGates.cs, TransformationModule.cs | high |
| Modify | Standard/Work.cs, WorkContracts.cs, WorkStart.cs, WorkModule.cs | high |
| New | Standard/ActivityExecutionLifetime.cs | medium |
| Modify | Views/TransformationManagementView.cs and existing engine-owned proof providers/content | medium |
| New | tests/Beep.SimEngine.Tests/ProcessStaffingTests.cs | medium |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Invalid queue ID/prefix/WIP binding or saved lifetime source | Explicit contract/corrupt-state failure |
| Missing process, activity, graph coverage or inconsistent team count | ContentException before Update |
| Worker absent, travelling, held or physically blocked | Existing staffing shortage; no resource/progress prefix |
| Disabled or resource/output-blocked process | Original queue execution and physical service remain owned |
| Process cancellation/completion | No staffing authorization; ordinary earned work/departure releases team |
| Explicit ordinary service-order cancellation | Native cancellation; live demand may post replacement work |

## 6. Backward compatibility

Development only. Migrate original contracts/content/codecs; no legacy reader or parallel subsystem.

## 7. Verification

Compile the solution without restore, then ONLY one NEW exact bounded process-staffing case.
Cover actual approach, stable queue IDs, same-recipe sibling contention, minimum-duration retention,
hold/release, disabled waits, saved continuation, completed batch, continuous stop and replay.
Older/family/full/Oil/platform runtimes remain unauthorized; no broader green claim.

## 8. Risks

Provider fault breadth and full allocation/stress remain separate open gates. The current tile
proof has no dock; dock departure/geometry and early withdrawal during approach are not proven. Completion must not
charge one-shot work costs twice, and running consumption must follow actual productive exposure
even once minimum work saturates. Source-owned bindings are provenance, not worker reservations.

## 9. Out of scope

Scoped physical supply/export, full provider/stress acceptance and whole-plan closure/publication.

## 10. Sub-task execution order

1. Migrate queue/gate identity in the original owners.
2. Add generic ordinary-work lifetime binding and integrate productive completion.
3. Add strict automatic process staffing and diagnostics through existing owners.
4. Build/run the single new bounded proof; update tracker/evidence and audit anchors honestly.
