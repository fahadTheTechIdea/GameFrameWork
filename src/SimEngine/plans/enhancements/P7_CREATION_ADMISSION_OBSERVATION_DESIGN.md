# P7-D3: Original creation-placement observation

## 0. Problem statement

`Kernel/EntityCreation.cs:World.CheckCreation` calls external `ISpatialPlacement.CheckPlacement` with only source-version and next-ID comparisons. A callback can change the source's existing auxiliary codec payload, the one clock/pacing/random stream or the selected space and still return a healthy placement result. A direct callback throw also bypasses the standard observation fault owner. Original P7.017 entity conversion cannot rely on that incomplete boundary.

The existing entity-conversion execution literal additionally has obsolete test authoring: added tools lack explicit port routing, entity-only recipes retain unrelated routes, and its point recipe predates explicit port metadata. Its negative assertions try to read healthy history after terminal failure.

## 1. Goals

1. Observe placement admission through the original provider/source/whole-source-codec/selected-space/clock mechanism before returning either success or a typed refusal.
2. Direct `PrepareCreation` withholds its out handle on provider failure, and `CheckCreation` cannot advertise healthy admission after mutations or throws.
3. Preserve current structural refusals, sole entity allocator, ephemeral intents, Spawn/initialization ownership, actual resource completion and save schemas.
4. Revalidate actual timed entity results, saved output/placement waits, native point/tile rollout, entity-only queues, independent producers and actual provider failures.

## 2. Design

Replace the version-only placement block in `World.CheckCreation` with `EntityObservation.ReadProvider(this, provider, source, callback, space: Space, observeTargetState: true)`. Delete the old parallel partial validator. Keep all structural admission and actual `CommitCreation`/Spawn logic with their existing owners. No new state, codec, allocator, registry or compatibility API.

Add one literal to `EntityConversionTests` using the existing PointSpace `GeometryObservationFault` instrumentation and existing `SingleResourcePort.FaultServicePayload`. Test direct prepare/check, new/restored sessions and normally ready/blocked placement results. Mutations cover hidden source payload, clock pause, pacing speed, selected-space state and random advancement; payload+throw and plain throw preserve the original exception. Explicit original typed refusals remain healthy without a bad provider. Negative instrumentation is not host gameplay.

Repair existing test recipes with explicit native routes, including empty routes for entity-only work. Use native inventory-port components in the point fixture. After terminal failure, require live history/save/hash refusal and retain only detached last-healthy facts. Use independent worlds for independent negative operations; never continue a faulted world as though healthy.

## 3. API surface

Public signatures unchanged. Engine build increments to2.616.0 for stricter runtime semantics; save159/replay1 and original payload codecs unchanged. No legacy reader.

## 4. Files

- Modify: `Kernel/EntityCreation.cs`, `Kernel/SaveGame.cs` build identity, current protocol JSON build identity, `EntityConversionTests.cs`, tracker/checkpoint docs.
- New: this design and verification report.
- Reuse unchanged: `EntityObservation`, `ISpatialPlacement`, `IEntityCreation`, original Spawn/EntityStore, resource transactions, PointSpace/SingleResourcePort negative instruments.

## 5. Error handling

Healthy structural or placement refusals stay typed. Mutation or unexpected provider throw latches the original first fault and throws; no successful handle, registration or success events. Later calls stop before callback re-entry. Do not attempt physical rollback or return fabricated healthy snapshots after a fault.

## 6. Compatibility

Development only. No old path, mirrored observer or codec, optional strictness mode or compatibility shim.

## 7. Verification

First record the two existing literal baselines (prepared contracts76/0; timed execution fails missing tool port). Build the new proof and run its exact literal against the old engine before the production repair. Then build and run only the new admission literal and the two original entity-conversion literals with bounded processes. Record every attempt; no full/profile/family runtime.

## 8. Risks

Whole-source observation must use the original entity Sync, not version alone or a copied codec. The selected space has its own explicit codec and invalidation revision. Allow legitimate later creation/state changes in CommitCreation; this phase guards the pure placement callback only. A thrown callback's original exception must remain visible. Tests using one hostile world for two separate operations must now split those worlds rather than suppress terminal cut-off.

## 9. Out of scope

Arbitrary undeclared CLR state sandboxing, all creation initialization/commit purity, broad coupled conversion observations, topology/stress, platforms/Draw, Tycoon business rules and Git publication.

## 10. Execution

1. Author negative direct-admission proof and repair explicit fixture authoring.
2. Record exact old-engine reproduction.
3. Replace version-only callback observation with original guard; bump build identity only.
4. Run the three exact literals; record source/evidence/remaining scope. Original P7.017 stays open.

## Scoped completion

- [x] Pre-repair direct proof692/252 records actual observation gap.
- [x] Version-only placement block deleted; original whole-source/provider/space/clock observation reused.
- [x] Build2.616.0, save159/replay1 and original state schemas unchanged.
- [x] Exact finals1072 admission +76 prepared +88 timed =1236/0.
- [x] [All attempts, original-source test correction and limits](../../docs/migration/P7_CREATION_ADMISSION_OBSERVATION_VERIFICATION.md) recorded.
- [ ] Broader creation initialization/commit, coupled callbacks, topology/stress and original P7.017 remain open.
