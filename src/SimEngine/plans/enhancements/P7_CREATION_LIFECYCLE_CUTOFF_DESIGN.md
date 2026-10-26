# P7-D4: Creation lifecycle first-fault cutoff

## Current M63: complete the original removal lifecycle boundary

Native **2.718.0/save161/replay2**. This extends the same entity lifecycle owner; it does not add a
second removal coordinator, scheduler, clock, fault store or persisted lifecycle model. The original
creation repair below is historical. [Current implementation/evidence](../../docs/migration/P7_CREATION_LIFECYCLE_CUTOFF_VERIFICATION.md#current-m63-removal-lifecycle).

### Observed gaps and requirements

On2.717, World.Despawn checked removal guards and directly invoked EntityStore.Destroy. A spatial
OnDespawn exception could escape without latching World.Fault. A callback could also save/hash/load
or advance the game while an entity was already marked dead but retirement was unfinished. Existing
creation/replacement checkpoint and clock admission did not cover removal. Both gaps were reproduced
by literal bounded tests before changing native code.

1. Healthy guarded refusal remains pure and nonterminal; protected stock is never discarded.
2. Unexpected retirement failures latch/rethrow at World's existing first-fault owner. A provider that
   catches a nested initialization failure cannot return outer removal as healthy.
3. Actual removal callback depth belongs to EntityStore. The existing completed-lifecycle guard must
   refuse checkpoint encoding/decoding and all clock advancement until the OUTERMOST removal returns.
4. Preserve legitimate nested removals and creation; no new global ban, substitute allocator or fake
   rollback. Keep the real terminal prefix and prevent faulted checkpoint/hash/advancement.
5. Verify ordinary public physical retirement and cargo recovery without bypassing collisions or claims.

### Changes in existing owners

EntityStore.Destroy retains its signature/order/registry. A transient, finally-unwound depth covers
its original spatial callback. RequireCompletedLifecycle checks that depth alongside the original
creation/replacement phases. Depth is not game time and is not saved; a boolean would incorrectly
release the outer boundary after a nested removal. World.Despawn keeps normal guard refusal before
its mutating try/catch, then uses existing LatchFault("entity.remove",error) and RequireHealthyUpdate
after the callback. Existing first-fault precedence retains an earlier nested cause. Updated may
still report a zero-step fault observation; no notification contract was changed.

The SAME CreationCleanupTests owner now supplies two explicitly negative lifecycle cases over its
existing master-listed content/probes. Four direct/nested-failure new/restored contexts and108 nested
admission contexts cover save, hash, breakdown, direct Sync, decode, World.Step, ordinary Update,
elapsed Update and engine fast-forward, with handled/propagated refusal and outer-before/nested/
outer-after phases. Healthy nested work completes; refused advancement changes no sole-clock budget.

The existing PermanentRemovalTests public-player scenario exposed obsolete setup assumptions: two
valid actor bays did not imply bay31, and its unfiltered outer loading bay trapped a retiring crew
behind a deliberately paused truck in one-dimensional geometry. Its authoring now uses existing
resource/work-role slot filters to separate inner hauling from outer work; assertions accept only the
two authored actor service positions. Diagnostic snapshots retain physical positions/state. No engine
routing/collision rule, timeout, money, resource balance or physical execution was weakened.

### Scoped completion and next work

Two new literals60/0 and984/0 plus existing shared creation allocation612/0 pass on build4. Existing
physical-retirement literal311/0/six actual replays passes on build6 after ONLY test authoring/oracle/
diagnostic edits. Total1967/0 spans those two tests images, not one final regression. All use native
2.718 built at build3. No old green reruns after test-only edits, full/profile/family/soak or consumer
execution. All15 build/runtime attempts, failures and exact images are recorded in existing evidence.

The internal Destroy caller census finds one native owner (World) and eight explicitly negative test
injections, with contextual source review. This is NOT an audit of every resource mutation or runtime
qualification of those old negative cases. Original P7.017 and the full goal stay open. Next continue
the finite C3 creation/resource transaction caller/fault matrix, then typed references and relevant
stage stress. User permission remains focused-only, including final. No staging/commit/push.

## Historical D4 creation repair

## 0. Problem statement

`World.Spawn` invokes `Entities.Create` outside its initialization try/catch. Unexpected configure/OnSpawn/OnCreate failures therefore need not latch the original world fault when called directly. It also does not check health after a capability initializer returns. `EntityStore.Create` similarly continues lifecycle callbacks after a callback catches an already-latched failure. This permits later callbacks, registration or an apparent successful return after terminal failure.

## 1. Goals

1. Unexpected creation/initialization failures latch the existing first fault for direct Spawn and CommitCreation calls, without depending on an outer Update or resource transaction wrapper.
2. After every configure, space spawn, component OnCreate and capability initialization callback, stop before the next lifecycle step if the existing world fault has latched.
3. Preserve the original allocator, registration order, candidate cleanup and initialization ownership. Never pretend partially executed work was rolled back.
4. Preserve healthy argument refusals before mutation and ordinary healthy creation/save/load behavior.

## 2. Design

`World.Spawn` starts with existing `RequireHealthyUpdate`. Structural definition/space validation stays before creation begins. Put the existing `Entities.Create` invocation and initialization loop under one phase-aware try/catch: phase `entity.create` until creation returns, then existing `entity.initialize`. Preserve first-fault precedence through `LatchFault`; rethrow the actual exception.

`EntityStore.Create` receives the existing world health check as an internal required `Action`, invoked after each callback before registration. Replace its unused optional restore callback (only caller is World.Spawn; load decodes through original EntityStore.Sync). This is an invocation gate, not another fault owner or observer. `World.Spawn` checks the same health owner after every initializer.

New focused test uses actual existing creation probes plus a diagnostic tail component. Transient per-instance hooks/counters are negative instrumentation, not saved gameplay state. Test configure/OnCreate/Initialize throws, caught nested real initialization faults and later callback cutoff in new/restored sessions through direct Spawn and CommitCreation. Verify positive exactly-once callbacks and that load never reruns creation. An inner creation can leave an actual pending prefix; faulted sessions are not advertised as resumable and no fake rollback is asserted.

## 3. API surface

Public signatures unchanged. Internal EntityStore.Create replaces its unused restore parameter with required health invocation. Build2.617.0; save159/replay1 and original codecs unchanged. No compatibility overload.

## 4. Files

- Modify: `Kernel/World.cs`, `Kernel/Entities.cs`, engine/protocol build identity, existing external creation probe and endpoint-module declaration, `EntityConversionTests.cs`, stage tracker/checkpoints.
- New: this design and verification report.
- No new allocator, spawn coordinator, clock, fault state, gameplay model or codec.

## 5. Error handling

Unexpected lifecycle exceptions fault the actual phase. If a callback swallowed an earlier failure, health checks stop continuation while preserving that first fault record. Faulted Spawn refuses before invalid-definition/provider callbacks. Healthy structural argument/placement refusals outside the mutating lifecycle retain their existing semantics. Cleanup exception masking and arbitrary auxiliary-state mutation during otherwise healthy initialization remain separate audits, not silently promised here.

## 6. Compatibility

Development only, no old overload or fallback. Original replacement preparation is a separate lifecycle and is not changed by this stage.

## 7. Verification

Build the new focused proof against2.616 first and record its failure. After repair, run that exact literal, the existing timed entity-conversion literal, and the original first-fault Update-boundary literal whose phase semantics must remain unchanged. No full/profile/family runtime. Record every attempt and leave broad P7.017 open.

## 8. Risks

Do not forbid healthy candidate-local initialization or freeze map state across legitimate registration. Avoid a second fault flag or duplicate callback dispatcher. Nested failures can consume real IDs; retain the actual terminal prefix rather than resetting the allocator. Keep harmless structural refusal distinct from unexpected failure after creation starts.

## 9. Out of scope

Whole-source/map purity across arbitrary creation commits, coupled resource callbacks, cleanup secondary exception handling, all-module lifecycle/performance, topology/stress, Tycoon/platforms/Draw and publication.

## 10. Execution

1. Add explicit negative lifecycle proof and record old-engine reproduction.
2. Move the original creation call inside World.Spawn's phase-aware fault owner and gate original callbacks through existing health checks.
3. Bump build identity only; build/run the three exact cases.
4. Record evidence/limits and update existing master; full goal remains open.

## Scoped completion

- [x] Old-engine proof166/46 exposes direct unlatched failures and continued callbacks after swallowed faults.
- [x] World.Spawn owns both original creation and initialization failures; existing first fault remains authoritative.
- [x] Existing EntityStore callbacks and capability initialization stop through the original health gate; unused restore callback removed.
- [x] Build2.617.0/save159/replay1; three exact finals218+88+78=384/0.
- [x] [All attempts, ownership mapping and limits](../../docs/migration/P7_CREATION_LIFECYCLE_CUTOFF_VERIFICATION.md) recorded.
- [ ] Secondary cleanup exception preservation, whole commit purity, broader P7.017/topology/stress remain open.
