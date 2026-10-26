# P7-NG2 — Allocation-free callback contexts at tile entry boundaries

> Subsequent [P3-SC1](P3_SPATIAL_CODEC_CONTEXT_DESIGN.md) on2.664 removes per-codec quantity/publication capture overhead using the same unchanged measurement method/profile. [Four focused cases/current measurements/limits](../../docs/migration/P3_SPATIAL_CODEC_CONTEXT_VERIFICATION.md). The2.663 figures below are the verified pre-edit baseline, not current universal performance or full-suite acceptance.

Status: scoped implementation measured/verified on2.663.0/save160/replay1.
Exact microbenchmark60/0 before and after; final fault48/0/physical82/0.
Measured112–568 fewer managed bytes/read, with all observation guards retained.
[All measurements, identities and limits](../../docs/migration/P7_ROUTING_CALLBACK_CONTEXT_VERIFICATION.md).
Extends [P7-NG1](P7_NAVIGATION_OBSERVATION_ALLOCATION_DESIGN.md); does not repeat
its broad fleet driver or replace its original scoped evidence.

## 0. Problem and specification

TileConditionContracts.Matches still creates a captured Func<bool> for each tile.
NavGrid.EntryCost creates another captured Func<int> per cost read. Both are hot
inside route probes and layered navigation. Existing EntityObservation already
has the value-context overload and cached invoker introduced by P7-NG1; duplicating
observation or weakening full-space verification is unnecessary and incorrect.

## 1. Goals

1. Measure allocation/time of actual condition, structural and live entry reads
   on existing authored native/independent layered worlds BEFORE runtime edits.
2. Remove only those captured callbacks by using existing ReadProvider<TContext,T>
   and static stateless functions; preserve exact provider, target, space, context
   admission, metadata/minimum/occupancy/clearance validation and first errors.
3. Keep all guards, hashing bytes and gameplay decisions. Do not cache layer
   values/world hashes or infer a whole-scenario speedup from a microbenchmark.
4. Verify selected condition faults and physical saved-routing behavior separately.

## 2. Design

TileConditionContracts passes (definition,world,map,tile) to its static reader.
NavGrid.EntryCost passes (grid,map,actor,tile,structural) to a static reader holding
the original callback body. Original bounds/origin checks still run before that
same observer. No new service, provider, pathfinder, codec or public API.

Extend the existing NavigationObservationAllocationTests owner with a separately
registered partial measurement case. Reuse LayeredNavigationTests.Factory and
IndependentTileWorkTests.Factory and their master-listed JSON. One embedded test
profile authors sample coordinates/IDs, iterations, warmup and repeats. Timed
current-thread samples exclude assertions/query setup/output. They report bytes
and elapsed timestamps; timing is not a flaky hard pass gate. Values/hash/time/
save/isolation must still pass. The original all-tier driver is NOT invoked.

## 3. API and state

Existing internal observer only; no schema/layout/public contract changes.
Advance runtime identity2.663/save160/replay1 after measurements; no compatibility
or old save-header rewrite. Static delegates hold no session state.

## 4. Files

Standard/TileLayerConditions.cs, Standard/Navigation.cs; build identity/protocol;
existing allocation test registration/same-owner partial/explicit test profile;
stage/evidence/master/README/AGENTS and original allocation-stage cross-reference.

## 5. Errors

Keep original callback exceptions, invalid cost checks, healthy early refusals and
terminal provider faults. An observation still verifies all original state before
publishing a result. No provider-specific fast path, swallowed callback or cached
validity assumption.

## 6. Compatibility

Development only, in-place call-site migration. No obsolete callback path kept.

## 7. Verification

One new exact benchmark on pre-edit and final source, outer30000/termination5000/
output14000. One awaited target Tests build per completed executable change.
Then exact condition-fault and independent physical-routing cases separately.
No original all-tier allocation driver, stress-family/profile/full suite, native
long-day or consumer rebuild. Retain every attempt and actual measurements.

## 8. Risks

Context value tuple must preserve original actual map/actor and callback order.
Warmup/JIT/runtime influence timings, not expected values or exact callback object
allocation removal. Remaining observation/hash allocation and route cost remain;
current-thread .NET measurements are not all-thread/process/FPS/soak guarantees.

## 9. Out of scope

Observer/codec guard changes, world-derived caches, different cost/route/dock/fuel
rules, broad original gate closure, Oil/Tycoon/business/Draw/platform changes and
Git publication.

## 10. Execution

1. [x] Record existing actual-world baseline before runtime edits.
2. [x] Migrate both hot call sites to the original value-context boundary.
3. [x] Await build, measure final and run two affected exact cases separately.
4. [x] Record evidence/identities/limitations and unchanged original gate audit.
