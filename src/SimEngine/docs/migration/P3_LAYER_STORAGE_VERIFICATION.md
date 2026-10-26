# P3-S5d — Canonical sparse layer verification

Date: 2026-10-05. Repository: Beep.SimEngine only.

## Current source and ownership

Engine build2.89.0, save format58, map schema4, tile-view3, point-view2. Existing byte/int/fixed
encodings and other native layouts are unchanged. Exact-build loading rejects older builds; there
is no old reader, compatibility facade, extra layer registry or storage fallback.

MapLayer<T> is now the sole native sparse dictionary/indexing/write/generation/checkpoint owner.
ByteLayer, IntLayer and FixedLayer contain only typed constructors and scalar/run encodings.
Former duplicate storage loops and out-of-bounds Get(TileCoord) zero-substitution APIs are deleted.
SpatialLayer still owns resolved authored metadata; TileMap still owns registration/ownership and
viewer-local invalidation. External providers retain their own coordinate/storage contracts.

Generation stages actual nonzero values before commit. Throws and same-layer reentrant Set/Fill/Sync
leave original values and cursors unchanged. Read stages a complete layer before replacing it; the
sole codec rejects duplicate/unsorted/outside/empty/padded/truncated sparse chunks. Writes and hashes
validate the same canonical padding/occupancy rules. Staging is temporary, not another saved store.

## Commands and results

~~~powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests\Beep.SimEngine.Tests -- "Spatial layers: canonical sparse storage and generation"
dotnet run --no-build --project tests\Beep.SimEngine.Tests -- "Spatial layers: private ownership, detached views and saved sparse values"
~~~

- Final solution build: succeeded, zero warnings/errors,1.83s.
- Canonical storage/generation:139 passed,0 failed,91ms.
- Existing private ownership/saved sparse values:20 passed,0 failed,75ms.
- Total:159 checks across exactly two distinct named suites, zero failures/aborts.

The canonical suite was rerun after strengthening its write/hash/private-generation fault proofs.
The private-save suite ran against the same final runtime implementation; the subsequent edit added
tests only. Other suites had current-build assertions updated and compiled, not executed. No third
suite, broad family filter, full regression, Oil/Tycoon, host/demo, renderer, Unity or Godot work.

## Scoped proof

- All three native storage shapes use one implementation and preserve signed/raw fractional values.
- Invalid reads/writes throw; zero represents only a valid unallocated coordinate.
- Same-value writes do not invalidate; clearing final values removes sparse chunks.
- Failed and reentrant generation preserves exact storage/cursors; successful fill commits once.
- Private fill invalidates its owner only; failed private fill invalidates no company.
- Whole-layer decode refusal keeps prior values even after a valid prefix was decoded.
- Corrupt live padding/empty chunks refuse save traversal and hashing through the same codec.
- Existing private ownership transfer, detached metadata, resource/unit aliases and saved continuation.
- Int-max by int-max tile spaces use allocated chunks only; bounded last-cell queries and saved twins
  preserve byte/int/fixed values without an artificial area/dimension cap.

## Remaining gates

Full P3/P1–P12 acceptance remains partial. General provider/cross-layer callback purity, extraction,
hydrology/3D semantics, multiple spatial authorities, alternative value types, replay/lockstep/fuzz,
allocation/stress and broad regressions are not proved here. Fill remains an explicit O(area) operation
with temporary old/new allocated sparse values, not bounded per-tick world generation.

The development-lifecycle skill kept design, one-owner implementation, focused evidence and master
tracking aligned. It did not authorize a full regression contrary to the user's scoped-test rule.
