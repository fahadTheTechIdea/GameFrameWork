# P3-LC1 — Native dense layer checkpoint validation cost

Implemented2026-10-16 on2.640.0/save160/replay1. Two focused framework finals304/0;
canonical prepare24/0 and first day22/0, SECOND day outer timeout retained. This
optimization is verified; overall canonical performance/harbour acceptance is NOT.
[All attempts, measurements and current checkpoint](../../docs/migration/P3_DENSE_LAYER_CHECKPOINT_COST_VERIFICATION.md).

## 0. Problem and specification

P5-MC2's canonical marine continuation repeatedly exceeded its wall-time threshold.
A live stack sampled MapLayer<byte>.ValidateValues inside native spatial observation
hashing. Its current loop visits every nonzero classification cell solely to repeat
a quantity check that is inapplicable to a non-quantity layer. Full map hashing and
corruption checks must remain; no dimension caps, observer exemptions or state cache.

## 1. Goals

1. Measure the original dense native layer codec in the existing CanonicalStorage case.
2. Eliminate redundant per-cell material validation for non-material layers while
   retaining nonempty sparse chunks, correct shape, exact partial-edge padding checks,
   all quantity legality, complete serialization and full observation hashing.
3. Preserve byte/hash encodings and actual mutation detection; verify corruption,
   saved twins, generation/counter failure atomicity and wide sparse dimensions.
4. Do not claim this local optimization alone completes harbour or the full plan.

## 2. Design

MapLayer<T>.ValidateValues remains sole validation owner. For a non-quantity layer,
HasValues proves nonempty; only the padding of a partial edge chunk needs individual
zero checks. A complete chunk has no padding, so no other legality check applies.
Quantity layers retain their existing complete per-value legality loop. Sync still
passes EVERY value through the original IStateSync, including observation hashes.

Extend existing SpatialLayerTests.CanonicalStorage, not another engine/validator.
Use its own explicit96x96 codec fixture with byte/int/fixed/associated dense samples;
time256 whole native Sync hashes after warmup, record exact bytes and hashes. Runtime
timings are comparative evidence, not a flaky elapsed-time assertion or a substitute
for canonical marine acceptance. Existing edge/corruption/quantity proofs remain.

## 3. API surface

No new public contract or state. Engine build increments after implementation; save160
and replay1 layouts unchanged. Old-build native saves remain incompatible by design;
never rewrite their headers or add a migration shim. Keep original marine artifacts.
Actual updated-build scenario validation must explicitly start its same canonical
fixture because its older native save cannot load, not search for an easier world.

## 4. Files

Modify Map/TileMap.cs, Kernel/SaveGame.cs, current-engine-protocol.json and existing
SpatialLayerTests.cs. Correct existing SpatialResourceValueTests empty-section oracle
after its six identical observed failures. Design/evidence/master/P3/AGENTS/README track scoped results.
No Oil production/content, Tycoon, host, platform or drawing implementation changes.

## 5. Errors

Null/wrong-shape, empty, unsorted/outside chunks and nonzero edge padding still fail
normal writes, reads and hashes. Invalid material quantities still fail unchanged.
Do not cache validated hashes or skip provider/clock/entity/spatial observations.

## 6. Compatibility

Development only. No legacy reader, alternate state owner or compatibility path.

## 7. Verification

Targeted Release test build and same exact canonical-storage baseline/final case;
one exact existing spatial resource legality case. No full/profile/family run.
Then targeted actual consumer build; original marine preparation and bounded harbour
only if needed to measure the effect on canonical real work. Keep all failures and
original deadlines; report local codec and scenario evidence separately.

## 8. Risks

Other guarded work may dominate live Update or checkpoint Load. A sampled stack is
not full performance attribution. Do not call a codec speedup a completed RTS gate.

## 9. Out of scope

Changed game time, material amounts, durations, gameplay policy, hash coverage,
world dimensions, full suites, staging/commit/push before the whole goal and plan.

## 10. Execution order

1. [x] Extend existing exact codec proof and measure original implementation.
2. [x] Optimize sole native validator with all original invariants and full hashing.
3. [x] Run affected exact checks; measure canonical scenario (next day still timed out).
4. [x] Record identities, results and original remaining gates without broad claims.
5. [ ] Measure/resolve remaining canonical route/observation cost and finish original harbour workflow.
