# P7-D11: Complete entity initialization before checkpointing

## 0. Problem statement

EntityStore.Create ends its local allocation phase after registering the candidate. World.Spawn then runs capability initializers. Those may legitimately create children, but can also Settle and obtain a save/hash while later parent initializers have not run. Clearing a child phase must not certify its still-initializing parent.

## 1. Goals

1. Reproduce premature Save, Hash, HashBreakdown and direct World.Sync hashing during parent-before-child, child and parent-after-child initialization.
   After that reproduction, also verify direct loading refuses before advancing the original valid saved-state reader.
2. Keep valid direct/prepared child creation and exact sequential identities.
3. A handled checkpoint refusal remains healthy and later completed initialization can checkpoint normally; propagated refusal retains original entity.initialize first fault and stops later callbacks.
4. One World-owned transient nesting phase guards the original persistence entry points, with no new lifecycle, clock, allocator, save field or arbitrary gameplay limit.

## 2. Design

Reuse existing authored creation-cleanup.json and direct/prepared creation helpers. Extend the proof-only tail initializer with a transient callback. Each attempt explicitly Settle()s before checkpointing, proving pending registration alone is insufficient. A subsequent real Entity.Touch changes authoritative version after the attempted checkpoint; leaked hashes/saves must not be accepted as complete initialization.

World.Spawn owns a nested capability-initialization depth, incremented before its original initializer loop and decremented in finally. A single original World guard rejects checkpoint traversal while depth is nonzero. Apply it at World.Sync and whole/breakdown hash entry points, so neither direct Sync nor hashing's implicit Settle bypasses it. Do not forbid valid child creation or random initialization behavior. Do not add a serialized Initialized flag to each entity.

## 3. API surface

No public signature or persistent state layout change. Behavior change increments engine identity to2.622.0; save159/replay1 unchanged. Existing unfinished lifecycle checkpoint calls now explicitly refuse.

## 4. Files

World.cs, engine/protocol build identity, existing CreationProbe/CreationCleanupTests/EntityConversionTests registration, this stage/evidence and active tracker. Reuse current JSON/master and creation contracts.

## 5. Error handling

Checkpoint guard itself is a pure InvalidOperationException refusal. A caller handling it can finish initialization. Propagated errors use the existing World.Spawn entity.initialize first-fault catch. The transient phase always unwinds, but a faulted partially initialized world never becomes resumable. No rollback of already registered entities or initialized children is fabricated.

## 6. Compatibility

Development only; no old reader, shim or alternate lifecycle.

## 7. Verification

Build and reproduce the new exact literal before production changes. Then build/run only initialization checkpoint, existing creation allocation, and timed entity-conversion literals. Each literal uses60s timeout/5s termination; no full/profile/family run. Record every attempt and keep P7.017 open.

## 8. Risks

Parent phase must survive nested child finally blocks. Capability initialization must still mutate legitimate state and spawn children. Only whole-world checkpoint traversal is gated; scoped component observation remains available. This stage does not redefine fault-diagnostic breakdown behavior outside the active initialization phase.

## 9. Out of scope

Arbitrary callback purity, a second state machine, business, hosts/demo/platforms/Draw, full provider sandboxing, global stress or P7.017 completion.

## 10. Execution order

1. Add nested handled/propagated protocol proof; build/reproduce.
2. Guard the original World initialization/persistence boundary; bump behavior identity only.
3. Run the three exact gates, review source, update evidence/tracker without Git publication.
