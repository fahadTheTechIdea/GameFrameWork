# P3-V2: Provider-owned regional changes — Design Document

Status: scoped implementation verified2026-10-08, full phases and goal ACTIVE/OPEN.
[Evidence](../../docs/migration/P3_SPATIAL_REGION_CHANGES_VERIFICATION.md).

- [x] Generic contract/result/scope, native replacement and public point implementation.
- [x] Six prior probe migrations/two pure factory visibility helpers;145 identity paths compile-only.
- [x] Final clean build3.19s0/0, ONE new bounded case81/0/0 1342ms after observed test corrections.
- [x] Source frozen after green; all three build attempts/two same-case runtimes recorded.
- [x] Paging ownership follow-up implemented in [P3-V3](P3_SPATIAL_PAGE_CURSORS_DESIGN.md);74/0/0 focused.
- [ ] Precise mod history/global purity/hostile providers/full acceptance.

## 0. Problem Statement

Map/TileMapView.cs exposes TileChunkDelta and ChangedChunksSince(int). Other spatial
providers cannot implement that tile-specific surface through a kernel contract. Bare
integers silently accept another session/viewer/load baseline when counters happen to match.
TileMap already owns privacy-scoped changed chunk timestamps; retain that sole owner.

## 1. Goals

1. Generic ISpatialRegionChangeQuery<TRegion>, detached immutable regional results.
2. Opaque query-scoped cursors reject other viewer/session/provider/load streams.
3. Native TileMapView uses existing map invalidation only; no second history/counter.
4. Public non-tile proof uses original scoped version and explicit full invalidation.
5. ONE new bounded case proves privacy, timed work, retained queries and restored streams.

## 2. Design

SpatialRegionCursor captures a version and opaque SpatialChangeScope only. Scope has no
world/provider reference, counter, values, codec or update. Each viewer query owns one scope;
loaded/new views create another. Null requests initial full invalidation. Non-null cursors
must belong to that exact scope and not be newer than the existing provider version.
Refusal exposes InvalidCursor with no cursor, regions or full signal.

SpatialRegionChanges<TRegion> defensively copies regional values. Regions are detached
provider-authored geometry, not mutable live space. Full invalidation intentionally does
not enumerate a potentially huge space. Stable regional order belongs to the provider.
Kernel has no tile/point switch or platform-size rule.

TileMapView.ReadChanges delegates to original TileMap.ChangedChunkKeys, converts exact
chunk keys to clipped TileRects as before, and attaches a scoped cursor. Remove old native
TileChunkDelta/ChangedChunksSince/InitialVersion API directly. Existing timestamp/visibility/
ownership/restore rules remain authoritative. PointLayerView implements the same contract:
original version changed means full invalidation, unchanged means empty incremental result.
It owns no new location change log; absence of precise history is declared, not invented.

## 3. API surface

ISpatialRegionChangeQuery<TRegion>.ReadChanges(SpatialRegionCursor previous = null).
SpatialRegionChanges<TRegion>: Status, Cursor, FullInvalidation, ChangedRegions.
SpatialChangeScope: Capture(version), TryBaseline(cursor,currentVersion,out previousVersion).
Only providers possess their private scope; cursors contain no live provider/session state.
No new player commands or gameplay rules.

## 4. Files to change

| Action | File group | Risk |
|---|---|---|
| Add | Kernel/SpatialRegionChanges.cs | medium |
| Replace API | Map/TileMapView.cs | medium |
| Extend | ExtensionProof/PointLayers.cs | medium |
| Migrate | six old query probes, strict identity fixtures | compile-only |
| Add | one new bounded provider/normal-player test | medium |
| Update | master/P2/P3/P6/docs/source evidence | low |

## 5. Error handling matrix

| Input | Result |
|---|---|
| null initial cursor | Full, empty regions, actual scoped cursor |
| owned prior version | Actual incremental regions or explicit full |
| other scope/future cursor | InvalidCursor, no version or regional facts |
| invalid provider version/null result input | ArgumentException family, no fabricated result |

## 6. Backward compatibility

Dev only; replace old tile-specific API without wrappers/readers/shims. Build2.279.0,
Save140 and all original persistent codecs unchanged. Query cursors are never saved/hashed.

## 7. Verification

Canonical completed-slice build, ONE exact NEW bounded case:
Regional change contracts: scoped native and mod cursors with physical saved work.
Native JSON setup/visibility/timed construction and non-tile ordinary sample/work commands,
private mutation isolation, hidden region invalidation, immutable retained regions, foreign
cursor rejection including load/new/viewer/provider, query purity and saved continuation.
At most2000 Updates per physical stage with immutable public diagnostics.
Record every build/run; repair only observed same-case failures. Old probes and identity
migrations compile-only; no old/family/full/platform/stale runtime. Source freeze after green.

## 8. Risks

External providers must publish detached region values and use their scope correctly.
This is a trusted contract, not hostile-mod/reflection sandboxing. Full invalidation for
providers without precise history is an explicit capability outcome, not fine-grained proof.
Existing paged layer-query integer cursors are outside this change, and remain a separate
follow-up ownership audit. No claim of complete spatial-query protocol coverage.

## 9. Out of scope

Rendering/Draw/platforms, extra history/clock/state/save owners, transport serialization/
compression, Oil/Tycoon business source/hosts/demos, full release/regression/soak/allocation gates.
Full goal ACTIVE, commit/push only after full plan and goal completion/verification.

## 10. Sub-task execution order

1. Generic immutable result and opaque scope over existing provider cursors.
2. Replace native API and extend public point provider; migrate old probes directly.
3. ONE new bounded normal-player/privacy/session proof.
4. Build/exact gate and observed repairs; freeze source after green.
5. Update tracker/evidence and readonly source/link/hash checks.
