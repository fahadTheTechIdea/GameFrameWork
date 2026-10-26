# P3-S5c — Bounded, provider-owned spatial layer reads

Status: implemented and focused-verified, 2026-10-05. Build2.73.0/save44; tile-view3/point-view2.
107 checks across the two exact suites below, zero failures/aborts; clean solution build.
[Evidence](../../docs/migration/P3_LAYER_PAGING_VERIFICATION.md). This continues P3-S5;
it does not close the full framework roadmap.

## 0. Problem

`TileMapView.ReadLayer` correctly rejects regions exceeding one IList's representation, but offers
no bounded traversal of those otherwise valid regions. Hosts should not reconstruct coordinate,
visibility or version rules. The non-tile proof uses the same regional contract but currently
narrows its authored query count to int. Layer metadata/storage remain with their existing owners.

## 1. Goals

1. Read any representable provider region in caller-sized pages, without materializing its area.
2. Preserve exact sparse values, stable provider order, typed refusals and private visibility.
3. Detect visible changes between pages using the existing viewer-local invalidation version.
4. Prove tile and non-tile implementations via public contracts, save/load and isolated sessions.

## 2. Design

- Extend `Kernel/SpatialContracts.cs` with optional
  `ISpatialLayerPagedQuery<TRegion,TValue>`, immutable `SpatialLayerPageRequest` and
  `SpatialLayerPageResult`. Offsets and totals are signed 64-bit; capacity and delivered counts are
  bounded per-call integers, not world dimensions. First reads may omit the expected version;
  continuation submits the returned version and unchanged layer/region. Versions are session-local
  transient cursors, not saved ages or transferable cross-session tokens.
- `SpatialLayerPaging.Check` alone validates the generic page envelope. Providers own geometry,
  order, authorization and value reads. Refusal clears output and publishes no total/version/value.
  Add typed `InvalidPage` and `Invalidated` statuses; no guessed zeros or silent restart.
- `TileMapView` shares its existing layer/region resolver between whole and paged reads. Whole reads
  retain their explicit single-buffer refusal. Paged reads compute row-major coordinates with wide
  arithmetic directly from the requested offset, including near the last tile of an int-max space.
- Extend existing `PointRegion`/`PointLayerView` in the public-only extension proof. Count becomes
  long, coordinates use checked raw fixed-point arithmetic, and the same provider region validator
  serves whole and paged reads. No tile conversion, raster requirement, new game loop or second map.
- Existing TileMap and PointSpace versions enforce conservative viewer-local consistency. Any
  visible change can invalidate a continuation; foreign-private changes cannot. Revalidate access
  first, so revoked ownership yields Unavailable, not a leaked version or extent.
- No values, ownership, cursor, page cache, registry or Sync are added. Native map schema4, point
  schema2 and save44 stay unchanged. View declarations change to tile3/point2; build identity advances
  to2.73.0. Retained numeric cursors must be discarded on load/new session as before.

## 3. API surface

`ReadLayerPage(string layerId, TRegion region, SpatialLayerPageRequest request, IList<TValue> destination)`
returns detached status, successful offset/next offset/total/count/version and HasMore. Positive
capacity is required even for an empty result. Offset equal to total is a valid empty terminal read;
outside offsets, nonpositive capacity and negative expected versions refuse. No platform API.

## 4. Files

Kernel/SpatialContracts.cs; Map/TileMapView.cs; Content/SpatialSpaceDef.cs; Kernel/SaveGame.cs;
existing ExtensionProof/PointLayers.cs and PointSpaceProof.cs; focused tests; stage/master/README/evidence.

## 5. Error handling

Missing/hidden layer: Unavailable. Wrong representation: ValueTypeMismatch. Invalid provider geometry:
InvalidRegion. Invalid page envelope: InvalidPage. Changed visible version: Invalidated. Oversized
whole result: BufferCapacityExceeded. Null destination remains a programmer argument exception.

## 6. Compatibility

Development-only clean revision. No old API alias, compatibility reader, second query/storage path,
invented fallback or legacy concern governs the design. Whole and paged reads are different bounded
query operations over one owner, not parallel data authorities.

## 7. Verification

Build the standalone solution; execute only these two exact suites:

- `Spatial layers: paged tile and non-tile reads without area caps`
- `Spatial layers: private ownership, detached views and saved sparse values`

Cover oversized regions, wide last offsets, row boundaries, byte/int/fixed values, non-tile fractional
geometry, empty/final pages, refusals/output clearing, same-value writes, ownership changes, foreign
private changes, saved data and pure reads/session isolation. Other suites compile only; no full run.

## 8. Risks

Visible versions conservatively invalidate unrelated visible-layer writes; explicitly documented.
Queries assume the existing single-threaded engine update/read boundary. Sessions/load reset transient
cursors; they are not durable read transactions. Per-call capacity belongs to the caller; no hard page
ceiling is imposed. Representational bounds remain explicit, not gaming-platform size limits.

## 9. Out of scope

Extraction/generation/physics, provider-fault breadth, 3D geometry, multi-space worlds, arbitrary new
storage types, rendering/Draw/platform adapters, Oil/Tycoon/hosts, broad stress and full acceptance.

## 10. Order

1. Add generic immutable paging contracts and shared pure envelope validator.
2. Migrate native tile resolver and expose row-major pages/view version3.
3. Extend existing point geometry/provider/view version2 through public contracts.
4. Run the two named gates, record evidence and update master/stage/README together.

All four tasks are complete for this slice. The lifecycle skill guided this staged implementation
and evidence boundary. Provider-fault breadth, extraction, stress and full acceptance remain open.
