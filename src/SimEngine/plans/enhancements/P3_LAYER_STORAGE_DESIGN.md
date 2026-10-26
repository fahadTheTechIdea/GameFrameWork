# P3-S5d — One canonical sparse tile-layer owner

Status: implemented and focused-verified, 2026-10-05. Build2.89.0/save58, native layouts unchanged.
159 checks across exactly two named suites; clean build, zero failures/aborts.
[Evidence](../../docs/migration/P3_LAYER_STORAGE_VERIFICATION.md). The full roadmap remains partial.

## 0. Problem statement

`Map/TileMap.cs` repeats the sparse dictionary, indexing, writes, generation and checkpoint loop in
ByteLayer, IntLayer and FixedLayer. Their restore checks reject empty chunks but accept nonzero
padding outside a partially filled edge chunk. Their `Get` methods also fabricate zero for invalid
coordinates, despite the framework's explicit coordinate-refusal rule.

## 1. Goals

1. Give every native typed tile layer the same storage, mutation and checkpoint implementation.
2. Keep JSON-authored semantics, resource/unit metadata, visibility and provider geometry unchanged.
3. Reject invalid coordinates and noncanonical sparse checkpoints for every value representation.
4. Failed generation leaves the previous layer values and invalidation cursors intact.
5. Prove private saved continuation and int-max-sized sparse geometry without a full regression run.

## 2. Design

Introduce `MapLayer<T> : MapLayer` in the existing file. It alone owns the dictionary, zero-cell
elision, checked indexing, `Set`, `Fill`, nonzero tile enumeration and `Sync`. ByteLayer, IntLayer and
FixedLayer retain only typed construction and value encoding. These are actual typed storage
representations, not wrappers around obsolete implementations. Delete the former duplicate stores
and out-of-bounds `Get` fallback APIs; use the strict existing indexers.

Keep the byte/int/fixed wire encodings unchanged. One checkpoint loop validates sorted unique chunk
coordinates, positive sparse occupancy and zero padding outside the authored dimensions on read,
write and hash. Read into a transient candidate dictionary; replace the live dictionary only after
the complete layer validates. This candidate is not another store, codec or saved transaction.

Fill builds the same transient sparse representation before committing. Refuse same-layer mutation,
recursive generation or checkpoint traversal during the generator callback. A thrown callback leaves
original values/version untouched. Different-layer/provider-wide callback purity is not claimed.

The shared metadata facade, TileMap owner/registration codec, private versions, world lifecycle and
regional/paged views remain authoritative. Zero is only an unallocated in-bounds cell. No raster,
layer count, world-area or platform size restriction is added. Other providers retain their own
storage contracts; this does not impose native tile storage on them.

Engine build advances to 2.89.0. Save format58, map schema4 and view schemas remain unchanged because
the layout does not change; exact build matching already rejects earlier saves. No legacy reader.

## 3. API surface

Native byte/int/fixed layers inherit strict typed indexers, Set and Fill from MapLayer<T>. Remove their
Get(TileCoord) methods. No new commands, host gameplay, rendering or platform interfaces.

## 4. Files to change

| Action | File | Scope / risk |
|---|---|---|
| Modify | src/Beep.SimEngine/Map/TileMap.cs | Replace duplicate storage in place; medium |
| Modify | src/Beep.SimEngine/Kernel/SaveGame.cs | Current build identity; low |
| Modify | tests/Beep.SimEngine.Tests/SpatialLayerTests.cs | Direct sparse invariants; medium |
| Modify | tests/Beep.SimEngine.Tests/*Tests.cs | Current build assertions only; low |
| New | docs/migration/P3_LAYER_STORAGE_VERIFICATION.md | Actual focused evidence; low |
| Modify | master/P3 stage/README/AGENTS/help | Scope and ownership guidance; low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Out-of-bounds read/write | ArgumentOutOfRangeException, no zero substitution |
| Recursive Fill/Set/Sync during same-layer generation | InvalidOperationException |
| Generator throws | Original values and cursors unchanged |
| Empty/duplicate/unsorted/outside/padded checkpoint chunk | CorruptStateException |
| Missing/hidden layer or invalid public query | Existing typed view refusal, cleared output |

## 6. Compatibility

Development-only clean redesign. No compatibility facade, old store or fallback path. Keeping the
existing value encoding is not a compatibility feature; there is still one current codec/build.

## 7. Verification

Build the standalone solution. Run exactly these two named suites:

- Spatial layers: canonical sparse storage and generation
- Spatial layers: private ownership, detached views and saved sparse values

Cover all three native value types, edge padding, zero removal, stable ordering, full signed tile
dimension range, failed fill/read atomicity, strict coordinate reads, private invalidation, whole and
paged queries, save/load/hash and twin continuation. Other suites compile only. Broader provider,
extraction, replay/stress and full acceptance gates stay open.

## 8. Risks

Public typed inheritance changes reflection shape; compile every project, but do not claim all older
behavioral suites ran. Transactional Fill temporarily holds old/new allocated chunks; it still does
not allocate the whole map. Full Fill remains explicit O(area) generation, not a per-update operation.

## 9. Out of scope

Extraction/hydrology/3D semantics, general provider purity, Oil/Tycoon, hosts/demos, Draw/platforms,
full regression, multiple spatial authorities and new layer value types.

## 10. Execution order

1. Replace duplicated native layer storage and delete coordinate fallbacks.
2. Add direct canonical/generation proofs; retain the existing private-save proof.
3. Build and run only the two named suites; repair within those same gates if necessary.
4. Record current evidence and update the existing master/parent docs without closing the roadmap.

All four steps are complete for this slice. Other suites compile only; the wider gates remain open.
