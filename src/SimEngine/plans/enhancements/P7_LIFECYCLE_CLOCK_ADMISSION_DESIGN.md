# P7-D12 — Lifecycle clock admission

## Current M64: one completed-operation boundary

Native2.719.0/save161/replay2. [Current C8 design](P7_LIFECYCLE_COMMIT_DESIGN.md),
[all current attempts and evidence](../../docs/migration/P7_LIFECYCLE_COMMIT_VERIFICATION.md#current-m64-operation-admission).
The former internal World.RequireCompletedEntityLifecycle name is removed, not retained as an alias.
World.RequireCompletedOperations is now the single entry used by clock/checkpoint admission. It
retains original entity phases and reads frozen declared ISimulationOperationBoundary providers
through existing guarded observation. ResourceTransactions owns its own nesting depth; World stores
only provider references, never copied busy flags or another clock/lifecycle state machine.

New144-context proof covers actual resource mutations and nested normal refusal. Latest four selected
checks2887/0/two public replays are explicitly build-bound; inherited interface metadata is included.
Creation/removal evidence below and in M63 retains its original source/image; it was not blindly
rerun after a test-only change. No full/profile/family/soak or broad closure. Current user permission
is focused-only even at final validation. No compatibility or publication.

## Historical D12 entity admission design

Status: implemented2.624.0/save159; baseline1768/1160, five selected current-core
checks7041/0. [All attempts and limits](../../docs/migration/P7_LIFECYCLE_CLOCK_ADMISSION_VERIFICATION.md).
Original P7.017 and full goal remain open.

## 0. Problem Statement

`World.Step()` checks recursive stepping, but not unfinished entity creation or
capability initialization when creation started between updates. A provider can
therefore advance systems/entities and the sole clock over an unfinished entity.
`SimGame.UpdatePhase` can also run controllers or consume pacing budget first.

## 1. Goals

1. Refuse advancement before any controller, clock, system, entity or publication
   work when original entity creation/replacement/initialization is unfinished.
2. Reuse actual EntityStore mutation flags and World initialization depth; no new
   clock, lifecycle manager, state flag or saved format.
3. A handled pure refusal permits normal creation and saved continuation. A
   propagated error retains the original creation/initialization first fault.
4. Preserve valid nested child creation and ordinary post-creation updates.

## 2. Design

Extend the existing World initialization admission guard to cover original store
mutation state. Expose one internal store guard used by its Sync and the original
World guard. Invoke the World guard at Step before time work and UpdatePhase
before controllers/pacing/publication. Existing recursive-update semantics remain.
Use that same guard at existing persistence/hash entry points, before payload reads.

## 3. API surface

No public API addition. Internal original lifecycle admission only.

## 4. Files to change

| Action | File | Risk |
|--------|------|------|
| Modify | Kernel/World.cs, Kernel/Entities.cs, Hosting/SimGame.cs | Medium: lifecycle admission |
| Modify | Kernel/SaveGame.cs, current-engine-protocol.json | Low: build identity only |
| Modify | CreationCleanupTests.cs, EntityConversionTests.cs | Low: explicit protocol instrumentation |
| New | stage verification report | Low: evidence |

## 5. Error handling matrix

| Attempt | Outcome |
|---------|---------|
| Step/Update/elapsed/RunTicks during unfinished creation | Throw before advancement |
| Provider handles refusal | Creation completes normally |
| Provider propagates refusal | Original creation/initialization fault; no continuation |
| Ordinary completed parent/child | Original update and save/hash continue |

## 6. Backward compatibility

Development-only; no compatibility shim. Save payload unchanged.

## 7. Verification

One new exact bounded case: fresh/restored sessions, direct/prepared parents,
configure/spawn/OnCreate/parent-before/child/parent-after stages, four advancement
entry points, handled/propagated refusal. Observe actual time, budget, publication,
later initializer cutoff, identity, saved twins and independent session progress.
Run only selected existing initialization/allocation cases if needed after repair.
No full/family/profile runs.

## 8. Risks

This is protocol fault instrumentation, not a player acceptance scenario or CLR
sandbox. Existing store flags remain authoritative; no rollback after real faults.

## 9. Out of scope

Oil/Tycoon/platform/Draw/adapters; changing clock semantics; broad stress closure;
Git publication. P7.acceptance.017 remains open beyond this boundary.

## 10. Sub-task execution order

1. Add and run the one focused reproduction against current core.
2. Repair original lifecycle/update admission and update build identity.
3. Build for changed C#, run focused proof and adjacent checks; explicitly correct
   stale selected fixture composition/expectations using existing owners if needed.
4. Record all attempts/limits and update tracker.
