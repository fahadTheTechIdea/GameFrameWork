# P3-S5 spatial layers — current focused proof and historical checkpoint

Superseded baseline: the subsequent **2.44.0/save19** P3-S5b non-tile/provider-admission slice has
its own [current report](P3_LAYER_PROVIDER_VERIFICATION.md), 132 checks in two exact suites. This
document retains 2.43.0/catalog evidence; its full two-suite set was not rerun in S5b. Non-tile proof
listed as open below is now provided by S5b; extraction/generation/full stage gates remain open.

## P3-S5a current evidence — 2026-10-05

Working-tree build **2.43.0 / save18**. State payload/map schema4 unchanged; build identity breaks
old saves/peers explicitly. No compatibility reader. No committed-HEAD or full-regression claim.
The tile view declaration is schema2 for the typed catalog/vertical metadata contract.

`dotnet build Beep.SimEngine.slnx --no-restore`: zero warnings/errors, final build 3.16s. Runtime, public
ExtensionProof, testing library and test executable compile together. Exactly these tests ran:

```powershell
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Spatial layers: catalog links, authored metadata and explicit regional reads'
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Spatial layers: private ownership, detached views and saved sparse values'
```

| Exact suite | Passed | Failed | Runtime |
|---|---:|---:|---:|
| Catalog links, authored metadata and explicit regional reads | 48 | 0 | 71ms |
| Private ownership, detached views and saved sparse values | 20 | 0 | 70ms |
| Total | 68 | 0 | Two suites, neither aborted |

The old malformed-content assertions now execute within the directly affected catalog/query suite;
no broad family filter or additional suite was run. Prior cargo/work/network suites were not rerun.

### Implemented and proven

- SpatialLayerDef/ISpatialLayer resolve immutable IUnitDefinition and optional IResourceDefinition
  aliases in the sole existing catalog. Unknown/blank references refuse before initialization.
- A resource link explicitly declares quantity or associated. Quantity units must share the resource's
  native dimension; associated water height retains length units rather than being inferred as volume.
  Existing exact UnitConversion demonstrates tonnes/kilograms without rewriting stored values.
- A vertical domain requires vertical_unit; a mass-valued deposit's -1200 metre offset is independent
  of its value unit. Missing/unknown vertical units and unbound vertical metadata fail explicitly.
- Shared layer definitions reject repeated Read/Resolve. Semantic types remain open authored codes.
  Standalone authored layer consumers declare their real units; no implicit unit was added.
- A sparse 70000×70000 world (4.9 billion authored cells) uses small regional queries over independent
  terrain, stratum, elevation, underground deposit and lake channels. No new side cap or whole-map
  materialization. Buffer capacity refusal is an actual IList representation bound, not a world cap.
- Missing/hidden/type/region refusals clear results. Private ownership/transfer and nested immutable
  metadata are enforced in engine views. Detached snapshots do not expose live spatial storage.
- Save/hash restores ownership, exact independent values and large chunk coordinates; saved twin
  Update/ownership continuation matches. Layer-only worlds need no fake entity.

### Still open

Non-tile layer-provider proof, generation/per-cell resource legality, physical extraction, environmental
layer consumers, general route/placement/lifecycle completion, allocation/stress and the authorized
stage regression gate. Spatial resource metadata is not stock, a reservation or a transaction. No
new resource balance, resource grant, platform drawing, Godot/Unity, host/demo or OilFieldDays change.

## Historical build-only checkpoint — superseded by current evidence above

Date: 2026-10-04. Working-tree evidence, not a committed-HEAD or runtime acceptance report.

## Current evidence

`dotnet build Beep.SimEngine.slnx --no-restore -v:q`

Result: solution compilation succeeds, zero warnings and zero errors. Runtime, public ExtensionProof,
testing library and test executable compile together. No test runner, full-suite run, filtered run,
oil integration, renderer, adapter, or platform project was executed for this slice.

`git diff --check` is the whitespace check, not runtime verification. Historical test counts predate
this spatial migration and do not establish current correctness.

## Implemented source responsibilities

- One `spatial_layers` JSON section through the existing manifest/content registry. Selected space
  registers authored layers; no code-created layer fallback remains.
- `ISpatialLayer` / `ISpatialLayerProvider`: semantic type, deterministic value shape, visibility,
  session ownership, units, resource code, vertical domain/reference/offset and immutable metadata.
- TileMap: sparse byte/integer/fixed-point chunks; map codec schema 4 saves owners and chunk coordinates.
  The same Sync owns save, load and hash. Layer registration closes after composition validation.
- Lines resolve value/owner layer definitions; networks resolve the line. Shape/space validation and
  wrong-space refusal replace inferred owner suffixes and automatic layer creation.
- `SpatialLayerSnapshot`: detached metadata, never a live storage object exposed to hosts.
- `ISpatialLayerQuery<TRegion, TValue>`: provider-owned regional queries. TileMapView implements
  integer/fixed-point reads with typed refusals and one authorization/region validation path. Hidden
  and unknown layers both return Unavailable, and produce no substitute values.
- Proof/test fixtures migrated to explicit spaces/presence/layers or genuinely nonspatial spawning.
  Removed APIs are not retained as aliases.

The one-buffer IList count constraint is an actual output representation bound, not a map-size rule.
Callers can request smaller regions of the same world. Coordinate and allocation bounds still exist;
the framework does not promise infinite numerical range or memory.

## Authored proof cases — not executed

Three suites in `tests/Beep.SimEngine.Tests/SpatialLayerTests.cs` compile:

- `Spatial layers: authored metadata and explicit regional reads`
- `Spatial layers: private ownership, detached views and saved sparse values`
- `Spatial layers: malformed authored definitions refuse composition`

They target a 70000×70000 authored tile space with sparse terrain, elevation, geology, underground
resource and water-level channels. They cover large coordinates, storage shapes, query errors, empty
regions, private unassigned/assigned/transferred ownership, immutable nested metadata, registration
freeze, sparse zero removal, save/hash and twin continuation. These are planned behavioral assertions,
not passing results. Their execution and the P3 stage regression gate remain pending.

## Recovery and remaining boundaries

The obsolete uncompiled root copies `Content/SpatialSpaceDef.cs`, `Kernel/SpatialContracts.cs`, and
`Map/TilePresence.cs` were moved without overwriting any existing archive to:

`C:\Users\f_ald\.codex\recovery\Beep.SimEngine\spatial-source-20261004-dc2549740f6d40eb8839608a7a536af0`

They are recoverable, outside the repository and compilation. Active implementations remain under
`src/Beep.SimEngine`; no compatibility implementation was retained.

Resource/unit links currently remain opaque stable codes. Existence/dimension validation must use P7's
sole catalog, not another spatial catalog. Non-tile layer-provider proof, optional standard-module
composition, generic route/placement contracts, incident/environment consumers, and behavioral
acceptance remain open. All simulation logic remains engine-side; rendering is a later platform concern.
