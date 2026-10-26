# P7-D5: Preserve original creation failure through cleanup

## 0. Problem statement

`EntityStore.Create` currently catches a failed configure/space-spawn/OnCreate callback and executes `space.OnDespawn(e); e.Alive=false; throw;`. If cleanup throws, it replaces the original exception and skips marking the unregistered candidate dead. If cleanup triggers and catches a second engine failure, that secondary failure can become World's first fault before Spawn sees the primary exception.

## 1. Goals

1. The existing World fault owner observes the primary creation failure before any cleanup callback.
2. Cleanup errors cannot replace that original fault or thrown exception.
3. Candidate Alive becomes false even if cleanup fails; no candidate registration, identity allocation or healthy save/hash occurs.
4. Preserve normal cleanup attempts, legitimate healthy creation/load, actual original resource state and session isolation.

## 2. Design

Extend the original internal EntityStore.Create invocation with a required fault-notification Action supplied by World's existing `LatchFault("entity.create",error)` path. No new fault record, ledger, observer or creation coordinator.

In the existing catch: notify primary failure first; attempt the original spatial cleanup; route a secondary exception to the same first-fault owner without rethrowing it over the primary; set candidate Alive=false in finally; bare rethrow preserves the original exception and stack. World's outer Spawn catch remains necessary for constructor/registration and initializer failures and preserves first-fault precedence.

The immutable WorldFault continues to describe the first failure. No secondary fault history/store is introduced or promised. Cleanup is best-effort after a terminal failure, not rollback or resumability.

Reuse existing PointSpace negative geometry hook at configure/spawn/despawn boundaries and existing per-instance creation/tail probes. New master-listed `creation-cleanup.json` defines the actual prototypes. Prove direct Spawn and CommitCreation × new/restored × configure/OnSpawn/OnCreate primary failures × successful/throwing/nested-catching/nested-throwing cleanup. Hooks filter the exact test prototype and are not gameplay behavior.

## 3. API surface

Public signatures unchanged. Internal Create adds the original owner's fault callback, without compatibility overload. Build2.618.0; save159/replay1 and production codecs unchanged.

## 4. Files

- Modify: `Kernel/Entities.cs`, `Kernel/World.cs`, build identity/current protocol JSON, existing point proof hook, proof embedded-resource declarations and test registration, trackers/checkpoint docs.
- New: master-listed proof content, focused cleanup test, this design and verification report.
- Reuse: EntityStore/World original cleanup, allocator, fault owner, save/load and resource/clock owners.

## 5. Error handling

The original exception is rethrown, not wrapped with a later cleanup error. Cleanup failure is sent to existing first-fault-wins handling; no healthy state is returned. A cleanup-triggered Spawn must see the latched primary fault before any child allocation/callback. Healthy structural refusals remain outside the mutating lifecycle.

## 6. Compatibility

Development only, no alternate cleanup path, old overload, legacy reader or configurable fallback.

## 7. Verification

Build/run the new literal on2.617 to reproduce before production repair. After repair, build and run only new cleanup proof, existing creation lifecycle cutoff and timed entity-result literal. Keep all attempts, do not run a full/profile/family regression. A narrow cleanup repair does not complete broad P7.017.

## 8. Risks

Do not skip cleanup outright to preserve the exception. Do not reset the allocator/stock/map to manufacture rollback. A terminal first fault can cause a provider's cleanup to refuse further engine operations; that cleanup failure must not replace the original. Candidate death belongs in finally. Original exception identity and immutable first-fault identity are independent checks.

## 9. Out of scope

Multi-error diagnostic history, otherwise healthy initialization purity, reentrant successful creation, coupled conversion callbacks, full topology/stress/performance, Tycoon/platforms/Draw and publication.

## 10. Execution

1. Author explicit content and actual provider-bound cleanup reproduction.
2. Run literal baseline, then fix only original EntityStore/World error ordering and candidate finalization.
3. Bump build only; run three exact literals and record source evidence/limits.
4. Update master, keeping broad plan/goal open.

## Scoped completion

- [x] Old-engine proof600/180 reproduces primary exception replacement, late first-fault notification and missed candidate death.
- [x] Original World notified before cleanup; same first-fault owner receives secondary errors; finally marks candidate dead and bare throw preserves primary.
- [x] Explicit master-listed content and48 negative plus4 healthy contexts reuse actual point/creation owners.
- [x] Build2.618.0/save159/replay1; three exact finals780+218+88=1086/0.
- [x] [All attempts, ownership and limits](../../docs/migration/P7_CREATION_CLEANUP_FAILURE_VERIFICATION.md) recorded.
- [ ] Otherwise healthy commit purity/reentrancy/coupled providers/topology/stress and broad P7.017 remain open.
