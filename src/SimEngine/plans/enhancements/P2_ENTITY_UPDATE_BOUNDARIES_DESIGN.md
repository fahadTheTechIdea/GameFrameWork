# P2-F3 — Entity update authority and state observation boundaries

Status: focused-verified, 2026-10-06; broader P2/P3/framework remains partial.
Parent [P2](P2_EXTENSION_CONTRACTS_DESIGN.md), lifecycle [P3](P3_TIME_AND_WORLD_DESIGN.md).
[Evidence](../../docs/migration/P2_ENTITY_UPDATE_BOUNDARIES_VERIFICATION.md).
Prerequisites: [F1](P2_IMMUTABLE_FAULT_BOUNDARY_DESIGN.md), [F2](P2_EVENT_FAULT_CUTOFF_DESIGN.md),
[B3b](P2_CONTENT_LIFECYCLE_DESIGN.md).

## Problem and goals

Entity.Update calls all IEntityStateSource.GetState callbacks before World checks its existing fault
latch. An extension can swallow a nested initialization fault, allowing later sources and state
commit to proceed in that already-terminal update. Contexts currently bind only a World, not an
entity or issued game-update coordinate; a retained context can invoke another entity's lifecycle.
Standard.OperationPolicyContracts.Pure already guards scoped observations, but entity state reads
have no such boundary and the existing guard does not detect direct state/timestamp changes.

1. Game.Update/World.Step remains the only entity-update issuer. Each context binds its actual entity
   and sole-clock coordinate. Default, foreign, stale, reentrant and repeated same-update calls refuse.
2. Each state callback is independently checked for first-fault cutoff, exact content references and
   scoped entity/identity/time immutability. No later callback, entity or completion phase runs afterward.
3. Move the existing scoped observation guard into the kernel once, extending its checks rather than
   duplicating it. Directly migrate callers; no old helper facade or compatibility context constructor.
4. Healthy deterministic state priority, normal physical work, pause, saved continuation and session
   isolation remain intact. Exactly one GameClock; permission timestamps only derive its coordinate.

## Design and API

EntityObservation.Read(World, Entity, Func<T>) becomes the sole internal scoped observation guard.
It captures actual entity identity/liveness/owner/version/state/state-since, next entity identity and
game-clock coordinate/pause/speed; reads must not mutate those facts. A readonly Scope holds these
transient scalar facts. Entity.Update captures/verifies it directly, without a per-source delegate.
It does not hash the whole world per read
or claim to sandbox arbitrary private/global provider state. Standard callers migrate directly from
the deleted OperationPolicyContracts.Pure method, preserving existing responsibility.

EntityUpdateContext's internal constructor requires World and actual Entity. Its internal permission
check verifies target/session/current clock coordinate and an active game update. Public Clock/World/
Tick remain facts of the same clock. Entity embeds transient reentry/completed-coordinate guards,
not serialized gameplay time or a second loop. World constructs the bound context per live entity.

Entity.Update checks existing World.RequireHealthyUpdate before/after each source. The source's
frozen component key/slot and entity identity produce stable diagnostic boundaries. Exceptions latch
through World.LatchFault, preserving any earlier nested fault. Only successful final state selection
commits the entity's state, once. Invalid external context calls refuse before mutation; recursive or
repeated calls during an active update make that partial update terminal, even if swallowed.
World.Step also refuses recursive kernel advancement before starting another update, preserving the
same first-fault latch. UpdatingEntities closes in the existing Step finally path on every outcome.

## Files and sequence

Kernel/Entities, EntityLifecycle, World and the moved observation helper; existing standard/view
observation callers; current build/save identity; public-only fault probes/JSON fixtures; two focused
suites; stage/evidence/master/AGENTS/README/P2/P3/help. No engine observer registry, codec, state copy,
clock, scheduler or platform layer. First inspect → replace sole owners → proof → build/two suites → docs.

## Strictness, compatibility and scope

Context construction changes directly; no old overload. Existing authored states and component
definitions remain the same catalog/JSON owners. The actual binding does not grant gameplay mutation
authority to state observations. Invalid cross-catalog state references remain faults. Partial update
save/hash refusals remain existing World rules; no rollback, diagnostic checkpoint or recovery fallback.

Global provider private-field mutation, other-entity mutation without native version signals, all
protocol/startup/load/fuzz/replay/soak/allocation matrices and full P2/P3 acceptance remain open.
No Oil/Tycoon/host/demo/platform/Draw changes. No wall-clock reads or world-size limits.

## Verification

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Entity boundaries: issued updates, scoped observations and first-fault cutoff"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Entity boundaries: authored states, physical updates and saved continuation"
```

Only these two exact names, including repairs. Older affected suites migrate/compile only, not full run.

## Execution

- [x] Inspect actual entity callbacks, first-fault latch and existing scoped observation owner.
- [x] Replace update authority/observation owner and migrate callers directly.
- [x] Build and verify two exact contracts/physical suites:366 +149 assertions, zero final failures/aborts.
- [x] Record evidence/update sole tracker; leave broader framework gates open.

Build2.168.0/save95; existing gameplay layouts unchanged. Clean build.30 standard/view helper-owner/
caller files migrate directly;82 older metadata files and three old context/diagnostic expectations
compile only. Only the two declared suites ran, including missing-proof-resource repair reruns.
Public state providers, seven fault modes, nine scoped mutation injections, invalid content references,
retained/foreign/reentrant/repeated contexts, deterministic priorities, physical native/public response
work, private policy observations, saved continuation/session isolation/pause pass. One GameClock unchanged.
