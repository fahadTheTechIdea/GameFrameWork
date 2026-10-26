# P3-SC1 — Spatial checkpoint callback allocation

Status: scoped implementation measured/verified on2.664.0/save160/replay1.
Four exact cases445/0. Same9216 measured reads allocate69.38% less than verified
pre-edit2.663 inputs; timing is mixed, NOT a general CPU/performance victory.
[All evidence, identities and limits](../../docs/migration/P3_SPATIAL_CODEC_CONTEXT_VERIFICATION.md).

## 0. Problem and specification

MapLayer<T>.Sync allocates a captured quantity selector for every fingerprint,
even when its resource-cell binding set is empty. SpatialResourceCellBindings
creates staged-publication capture/enumeration overhead on writes that never
publish a restore action. Empty sparse layers also allocate empty key arrays.
These costs repeat inside unchanged full spatial observation intervals.

## 1. Goals

1. Eliminate captured quantity samplers via one generic value-context StageSync
   on the existing SpatialResourceCellBindings; migrate all known native,
   independent tile and point callers directly, no compatibility overload.
2. Always encode/check the real binding count. For actual zero bindings, no
   sampler/enumeration/book action is needed; reads return a stateless empty
   publication, writes/hash return null as before. Real cells retain all checks.
3. Allocate captured publication only when reading nonempty books. Reuse the
   shared immutable zero-length key array for actual empty native chunk sets.
4. Prove exact empty-count bytes/hash/refusal; measure unchanged native/independent
   route samples; verify actual incoming/outgoing cell books and physical spill
   recovery through original owners.

## 2. Design

Kernel/SpatialResourceCells.cs: replace StageSync(IStateSync,Func<long,Fix>) with
StageSync<TContext>(IStateSync,TContext,Func<TContext,long,Fix>). Validate Count
before the empty branch. One nonempty core retains the original geometry/version/
actual StageSync/commit-shape checks. A separate publication-action factory is
called only for reading; no capturing closure in the write/hash method.

MapLayer<T>.Sync passes (layer,candidate chunks) to its static sampler. Existing
SensorSampleLayer and PointLayer pass their actual staged/current sample stores
through equivalent stateless readers, preserving point read/write distinctions.
No dictionary snapshot, book copy, alternate codec, cached hash or new state owner.
Native OrderedKeys returns Array.Empty<long>() only for an actually empty store.

## 3. API and state

Public helper API migrates directly; ISpatialResourceCellGuard.StageSync and all
actual cell books/layouts remain unchanged. Runtime2.664/save160/replay1. No legacy
signature, fallback, arbitrary platform limit or old save-header rewrite.

## 4. Files

Original Kernel/SpatialResourceCells.cs, Map/TileMap.cs and build identity/protocol;
two independent proof providers mechanically migrate callers. Existing spatial
cell test owner gains a separate empty-codec case. Reuse the SAME existing entry
measurement method/profile without copying a benchmark or scenario factory.
Stage/evidence/master/README/AGENTS and allocation-stage cross-reference updated.

## 5. Errors

Bad saved counts/geometry, provider version changes, missing read commit and
unexpected writer commit still refuse. No sampler or book callback can be
silently skipped for a nonempty binding set. Decoded sample/book state is still
published only after original complete staging succeeds. First faults unchanged.

## 6. Compatibility

Development only; migrate every known caller, remove old API in place.

## 7. Verification

Current pre-edit Tests/core binary hashes match recorded P7-NG2 final2.663 inputs;
reuse that actual measurement baseline instead of needlessly rerunning it. After
one completed target build, run separately: new exact empty-codec proof, unchanged
entry measurement, existing bounded incoming cell commitments, independent physical
spill accounting. Timeout30000/termination5000, caps14000/12000. No family/profile/
full/stress driver or consumer/long-day run. Preserve any failed attempts.

## 8. Risks

Count bytes/order and read-action nullness must stay exact. Static samplers hold
no session; tuples reference the actual candidate, not prematurely published data.
An empty action is the exact zero-book operation after validated count, not a
fabricated recovery default. Remaining full-map hashing/validation cost persists;
measured allocation does not prove universal speedup/soak/harbour completion.

## 9. Out of scope

Reduced guards, cached world hashes/samples, changed units/quantities/timing/
resource policies, business/Oil/Draw/platform/host logic, broad gate closure or
Git publication.

## 10. Execution

1. [x] Inspect all actual StageSync callers and confirm current measured baseline.
2. [x] Migrate shared helper and all known producer callbacks in place.
3. [x] Await target build and run four separate exact checks under existing bounds.
4. [x] Record source/binary/measurement evidence and original unchanged audit.
