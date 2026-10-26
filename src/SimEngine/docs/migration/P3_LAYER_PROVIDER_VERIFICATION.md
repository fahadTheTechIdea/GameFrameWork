# P3-S5b — Non-tile layers and strict provider admission

Date: 2026-10-05. Current working-tree evidence, not committed-HEAD/full-regression completion.
Engine **2.44.0 / save19**. Existing point-space codec schema2; tile map schema4 unchanged.
No old build/save reader. No oil, Tycoon, host/demo, rendering, Unity or Godot project was changed.

## Current verification

`dotnet build Beep.SimEngine.slnx --no-restore`: zero warnings/errors, final build1.90s.
Runtime, non-friend public ExtensionProof, testing library and tests compile together.

Exactly two directly affected named suites ran, including reruns during this slice's corrections:

```powershell
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Spatial providers: non-tile layers, strict admission and saved continuation'
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Spatial layers: private ownership, detached views and saved sparse values'
```

| Exact suite | Passed | Failed | Final runtime |
|---|---:|---:|---:|
| Non-tile layers, strict admission and saved continuation | 112 | 0 | 177ms |
| Private ownership, detached views and saved sparse values | 20 | 0 | 69ms |
| Total | 132 | 0 | Two suites, neither aborted |

Counts describe the final run, not a sum of repeated assertions. No broad family/full gate was run.
Earlier layer-catalog, motion/work/production/recovery/network proofs were not rerun as complete suites.

## Implemented responsibilities

- SpatialLayer is the sole live resolved metadata/owner binding used by MapLayer and the ordinary
  external PointLayer. It aliases SpatialLayerDef/catalog contracts, owns no samples or registry,
  and adds no codec. SpatialLayerSnapshot still owns detachment; providers own their real values/Sync.
- World.FinishComposition validates actual selected registrations instead of accepting a name alone.
  Missing/null/duplicate/unselected/wrong-space layers, wrong type/value/visibility/unit/resource/
  resource-measure/vertical data, changed or mutable metadata, invalid/public owners and incorrect
  company/observer visibility all refuse before systems initialize. Correct frozen metadata copies
  are allowed; units/resources must alias the sole catalog. Providers are trusted executable modules,
  not CLR-sandboxed code; composition validation is not continuous protection from a hostile module.
- PointSpace extends the existing non-tile provider/factory; no parallel world/spatial authority.
  Its explicit manifest-listed point-layers.json defines independent terrain, count-valued underground
  deposits and water height associated with a fluid. No framework semantic enum or scheduler changes.
- PointLayer embeds sparse fixed-coordinate samples. Sorted coordinates/IDs define its single save/
  load/hash traversal. PointSpace.Sync embeds ownership and samples alongside its actual access state;
  transient invalidation cursors rebase on load and are not saved gameplay epochs.
- PointLayerView implements ISpatialLayerQuery<PointRegion,Fix> with provider-owned fractional
  coordinates, exact signed physical properties, sparse defaults, explicit region refusal/overflow,
  immutable metadata and private authorization. No TileMap/NavService, raster resolution or dense
  world allocation. This proof supports fixed samples; other storage shapes explicitly refuse.
- Sample-write/private-owner commands implement all checking and mutation inside the engine-side
  provider. Previews are pure and match commands. Material samples respect native quantity legality,
  but do not create inventory or consume/extract deposits. Hosts only submit/read/Update.

## Proven boundaries

Two interleaved sessions reuse the same immutable definitions without sharing samples/ownership.
Cross-session mutation refuses. Private sample writes do not change another company's view cursor.
Ownership transfer invalidates both affected views without mutating retained metadata. Saved twins
execute transfer/write/Update identically. Real source inventory remains unchanged by deposit samples.

The sole point-space codec rejects wrong layer count/identity, invalid owners, negative/out-of-space/
duplicate coordinates, explicit zero sparse entries and fractional discrete quantities. Direct codec
fault injection uses disposable sessions; it does not claim rollback of a partially restored provider.
The existing tile suite still proves large sparse coordinates, native independent values, private
owner transfer and twin continuation through the shared metadata binding.

## Remaining acceptance

Generic generation/extraction and all-provider per-cell quantity policies, broad spatial queries/deltas,
environmental consumers, saved navigation/rollout budgets, lifecycle/hostile-provider/allocation/stress,
oil integration and the explicitly authorized stage regression gate remain open. This is P3-S5b,
not full P3, P4 DAG or the complete framework roadmap. Rendering/platforms remain outside SimEngine.
