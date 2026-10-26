# P3-S5f — Legal resource quantities in spatial layers

Status: focused implementation verified,2026-10-06. Parent: [spatial layers](P3_SPATIAL_LAYERS_DESIGN.md).
Clean build;192 checks/two exact suites, zero failures/aborts.
[Evidence](../../docs/migration/P3_LAYER_RESOURCE_VALUES_VERIFICATION.md).
This is an extraction prerequisite, not extraction completion. Full framework remains partial.

## 0. Problem

SpatialLayerDef resolves quantity units and resource dimensions, but native MapLayer.Set/Fill/Sync
accept negative, fractional-discrete, inexact or overflowing resource values. PointLayer has a local
conversion check, duplicated outside the native path, and validates samples only while restoring.
World resource availability cannot be trustworthy while its authoritative cells violate the catalog.

## 1. Goals

1. One stateless contract-based validator admits resource quantity values in their actual authored unit.
2. Exact conversion to the linked resource's native unit must succeed, without rounding/clamping.
3. All native byte/int/fixed writes, generation, reads and checkpoints use the same quantity rules.
4. Independent point provider uses those rules and validates/stages its existing checkpoint owner.
5. Refusal preserves values and invalidation; associated/nonresource channels retain signed semantics.
6. Strict corrupt-state proof, private queries and saved continuation pass exactly two named suites.

## 2. Design

Kernel/SpatialResourceValues.cs adds CheckValue(ISpatialLayer,Fix) and ConvertQuantity(ISpatialLayer,Fix)
using existing UnitConversion and IUnitDefinition/IResourceDefinition aliases. Only explicit quantity
metadata invokes material legality; names, vertical offsets and associated units infer nothing. Valid
quantity values are nonnegative, obey both stored/native discrete units, and convert exactly within
native Fix range. Converted zero remains real empty availability, not fabricated stock.

MapLayer<T> remains the sole native value/checkpoint owner. Its type specializations convert their
stored scalar to Fix exactly. Set preflights before sparse allocation/version/value changes; Fill
validates transient generated values before commit. Strict reads/checkpoints reject malformed live
quantities. Restore remains staged through the existing dictionary candidate. No second value store.

Public PointLayer deletes its private conversion formula. It uses the shared rules in admission,
reads and both checkpoint directions, staging its existing sample dictionary before publishing restore.
No public provider needs tile geometry. Observer/company-private invalidation owners remain unchanged.
Point same-value writes return before invalidation preflight; actual changed values still preflight
the existing counters before sample commit. No counter or value owner is duplicated.

Build2.115.0/save65/native map5/public point space3; byte/int/fixed/sample payloads unchanged.
Existing view descriptors/payload layouts and every work/logistics owner remain unchanged.

## 3. API

SpatialResourceValues.CheckValue returns existing UnitConversionStatus. ConvertQuantity returns the
existing exact UnitConversionResult in the linked native resource unit; non-quantity metadata refuses
with InvalidUnit. Provider checks are pure; executable providers own geometry, values and Sync.

## 4. Files

Add Kernel/SpatialResourceValues.cs; extend Map/TileMap.cs, Kernel/SaveGame.cs and public proof
PointLayers.cs/PointSpaceProof.cs. Add master-listed layer proof JSON and two exact suites. Mechanically
migrate prior build/map/point identity assertions and compile only. Update tracker/parent/evidence/
README/AGENTS/help with scoped proof; no Oil/Tycoon/host/platform/Draw changes.

## 5. Errors

Negative quantities, fractional discrete stored/native values, inexact conversion and native overflow
return the existing precise conversion status. Native invalid write/generation throws before commit;
point commands retain their existing typed InvalidArgument refusal. Corrupt live/serialized quantities
cannot read/checkpoint/hash. Associated water/elevation metadata can retain negative signed values;
it never becomes resource quantity by inference. Failed staged restore preserves the prior layer.

## 6. Compatibility

Development-only clean validation change; no legacy decoder, alternate writer, zero fallback or shim.

## 7. Verification

Build then only these complete exact names; same-name repair/final reruns permitted:

- Spatial resource values: catalog legality and atomic native generation
- Spatial resource values: provider checkpoints, private queries and saved continuation

Prove byte/int/fixed actual writes, source/native quantum, exact scaling, unrepresentable/overflow,
signed associated channels, private invalidation, failed generation, staged corrupted codecs, guarded
reads/hashes, public point commands and interleaved saved sessions. No third/family/full gate.

## 8. Risks

Quantity layers deliberately reject invalid existing values, not translate them. Associated signed
channels must not inherit nonnegative material constraints. Fill callbacks can mutate unrelated layers;
scoped failure retains this layer only, not global rollback. Native wide arithmetic must not impose
new geometry limits. Point restore scratch is not a second authoritative sample store.

## 9. Out of scope

Physical extraction/depletion/receipt provenance, hydrology, generation science, voxel/3D geometry,
arbitrary external provider purity, full replay/stress/allocations/framework gates remain open.

## 10. Execution

- [x] Shared quantity contract rules and actual native/public value/checkpoint integration.
- [x] Explicit master-listed content and two complete suites.
- [x] Build/run only declared names; record source fingerprints.
- [x] Align single tracker and guidance; leave extraction and broader gates open.
