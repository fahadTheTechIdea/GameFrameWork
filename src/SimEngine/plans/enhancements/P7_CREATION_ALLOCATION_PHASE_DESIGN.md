# P7-D6: Original allocator creation phase

## 0. Problem statement

`EntityStore.Create` computes the candidate ID before configure/OnSpawn/OnCreate, but advances the allocator only afterward. A callback that successfully creates another entity reuses that same ID, and the outer registration collides after the nested entity has already committed. Original save/hash also has no guard for this unfinished pre-registration phase.

## 1. Goals

1. The original entity-store allocator refuses reentrant allocation while a local candidate awaits registration, before any child constructor/callback/ID change.
2. Original initialization may create a child after the parent has registered; parent/child get distinct sequential IDs and normal lifecycle behavior.
3. Checkpoint/hash refuses the unfinished pre-registration phase, without a shadow candidate, second allocator or saved phase flag.
4. Preserve original cleanup/first-fault cutoff and clear the transient allocation phase on every success/exception path.

## 2. Design

Add one transient `_creating` phase flag to the existing EntityStore, beside its existing `_replacing` flag. Create refuses if either mutation phase is active, then sets `_creating` inside a try/finally covering actual candidate construction through registration. Finally clears the flag before World.Spawn begins capability Initialize, so legitimate post-registration child creation remains allowed.

Replace refuses while local creation is active, before allocating a replacement candidate. Existing preparation/irreversible replacement semantics remain otherwise unchanged. EntityStore.Sync refuses while `_creating` or `_replacing`; do not serialize a phase flag or candidate. No compatibility overload, new scheduler, fault owner or world hash.

New literal reuses the existing cleanup fixture/composition/probes. Add an explicitly authored nonspatial child prototype to the same master-listed file. Prove parent Spawn/CommitCreation × child Spawn/CommitCreation × new/restored × configure/OnSpawn/OnCreate × caught/propagated nested refusal. Separately prove healthy child creation from Initialize, and caught save/hash refusal during local callbacks without poisoning an otherwise healthy creation.

## 3. API surface

Public contracts unchanged. Build2.619.0; save159/replay1 and original payload codecs unchanged. Phase state is transient protocol protection, not a second persisted simulation state.

## 4. Files

- Modify: `Kernel/Entities.cs`, build identity/current protocol JSON, existing cleanup proof content/tests and literal registration, trackers/checkpoint docs.
- New: this design and verification report.
- Reuse unchanged: World.Spawn/CommitCreation, original allocator/registration/cleanup, World first-fault owner, spatial provider and original Sync.

## 5. Error handling

Nested public creation reaches original World.Spawn's failure owner and latches the same first fault even if its callback catches the refusal. Outer candidate cleanup remains original and receives no successful child prefix. Save/hash refusal during a handled diagnostic attempt is not itself a gameplay mutation or permission to fault a healthy session. All finally paths clear local creation phase. No allocator reset or fabricated rollback.

## 6. Compatibility

Development only, no old allocation path or configurable legacy behavior.

## 7. Verification

Build/run the new exact literal against2.618 first. Then repair the original owner and run only new allocation-phase proof, existing cleanup proof and timed entity-result literal. Record all attempts; no full/profile/family run. Broader replacement reentrancy and post-registration initialization checkpoint purity are not claimed.

## 8. Risks

Do not reject all nested Spawn calls globally: initialization after parent registration is valid. Do not move allocator advancement early, which would create partially allocated IDs on local failure. The phase flag must clear on constructor, callback, registration and cleanup exceptions. Original replacement preparation has different fault semantics; preserve them.

## 9. Out of scope

Whole source/map purity during healthy initialization, all replacement preparation reentrancy, callbacks issuing nested game updates, post-registration initialization checkpointing, coupled conversions/topology/stress/performance, Tycoon/platforms/Draw and publication.

## 10. Execution

1. Add original-owner proof for collisions/checkpoints and valid initialized children; record baseline.
2. Implement transient phase exclusion in existing EntityStore with unconditional finally release; bump build only.
3. Build/run three exact literals, update evidence and master, keep original P7.017/full goal open.

## Scoped completion

- [x] Old-engine proof348/264 reproduces actual reused child/parent ID and premature checkpoints.
- [x] Original EntityStore excludes overlapping local allocation, releases its transient phase in finally and blocks unfinished Sync.
- [x] Valid initialized children retain distinct IDs/ordinary lifecycle; handled local checkpoint refusals do not poison healthy creation.
- [x] Build2.619.0/save159/replay1; three exact finals612+780+88=1480/0.
- [x] [All attempts, ownership and limits](../../docs/migration/P7_CREATION_ALLOCATION_PHASE_VERIFICATION.md) recorded.
- [ ] Broader constructor/initialization/replacement/coupled/topology/stress and original P7.017 remain open.
