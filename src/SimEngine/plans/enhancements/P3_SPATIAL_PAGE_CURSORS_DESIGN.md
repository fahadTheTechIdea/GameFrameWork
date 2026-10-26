# P3-V3: Query-bound spatial page cursors — Design Document

Status: scoped implementation verified2026-10-08; full P1–P12 goal remains active.
[Evidence](../../docs/migration/P3_SPATIAL_PAGE_CURSORS_VERIFICATION.md).

## 0. Problem Statement

Kernel/SpatialContracts.cs SpatialLayerPageRequest.ExpectedVersion accepts a bare integer.
Map/TileMapView.cs and public ExtensionProof/PointLayers.cs validate only its numeric equality.
Matching viewer/session/provider counters can silently continue an unrelated layer/region.
This closes the paging ownership gap recorded in P3-V2; it does not complete P3 or the full plan.

## 1. Goals

1. Replace numeric continuation with an opaque cursor scoped to the actual query instance.
2. Bind each cursor to the exact layer and immutable provider-owned region identity.
3. Check sample authorization before cursor validation or publishing page facts.
4. Keep original provider versions, geometry, samples, save codecs and one GameClock.
5. Prove native/mod privacy, rejected stream substitution, pure reads, and physical saved work
   through one distinct bounded runtime case. Older probe/identity migrations compile-only.

## 2. Design

Extend original SpatialChangeScope.Capture/TryBaseline with an optional immutable string binding.
SpatialRegionCursor privately captures that binding; null remains the regional-change stream.
Each existing TileMapView/PointLayerView scope serves both protocols; binding prevents crossing them.
No separate page registry, version, scheduler, clock, history or persistence authority.

SpatialLayerPaging.CheckEnvelope validates offsets/capacity/long count without creating a cursor.
Providers then authorize the exact requested samples. Check validates scope/binding and current
version and creates detached successful page facts. Foreign scope/binding returns InvalidCursor;
owned stale/future numeric versions return Invalidated. Refusal clears destination and all facts.

Native tile binding uses invariant length-prefixed layer identity and X/Y/W/H.
Point binding uses invariant length-prefixed layer identity and Start.Raw/Spacing.Raw/Count.
Binding excludes offset/capacity, allowing variable page sizes within one exact regional read.
No kernel geometry switch, implicit template, platform map limit, or old integer overload.

## 3. API surface

- SpatialLayerPageRequest(offset,capacity,SpatialRegionCursor cursor=null); Cursor replaces ExpectedVersion.
- SpatialLayerPageResult.Cursor replaces Version.
- SpatialLayerReadStatus.InvalidCursor is an explicit refusal, separate from owned invalidation.
- SpatialLayerPaging.CheckEnvelope(total,request,out count) and
  Check(total,version,scope,binding,request). Geometry/authorization remain provider-owned.
- SpatialChangeScope.Capture(version,binding=null),
  TryBaseline(cursor,currentVersion,out previousVersion,binding=null).

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| Modify | src/Beep.SimEngine/Kernel/SpatialRegionChanges.cs | 15 | medium |
| Modify | src/Beep.SimEngine/Kernel/SpatialContracts.cs | 55 | medium |
| Modify | src/Beep.SimEngine/Map/TileMapView.cs | 15 | medium |
| Modify | tests/ExtensionProof/PointLayers.cs | 15 | medium |
| New | tests/Beep.SimEngine.Tests/SpatialPageCursorTests.cs | 220 | medium |
| Modify | Program.cs, old direct page probes, EngineInfo identity fixtures | mechanical | low |
| Modify | master tracker, original P2/P3/P6, README/AGENTS | scoped handoff | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Missing/hidden layer or unauthorized sample | Unavailable; no cursor/count/value |
| Invalid geometry/envelope/type | Existing typed refusal; no cursor/count/value |
| Another view/session/provider/restore/layer/region/protocol | InvalidCursor; no facts |
| Actual owned visible version differs | Invalidated; explicit restart required |
| Negative provider facts/null scope/binding | Programmer exception, no fabricated state |

## 6. Backward compatibility

Development-only direct replacement. No integer overload, alias, legacy reader or compatibility shim.
Build identity advances to2.280.0; save140 and all persistent fields remain unchanged.

## 7. Verification

Canonical dotnet build Beep.SimEngine.slnx --no-restore, then ONE exact NEW bounded case:
`Spatial page cursors: bound native and mod reads with physical saved discovery`.
Every attempt recorded; repair/re-run only this case on observed failure. No broad/old runtime.
Source/tests/proof/JSON freeze after final green; final documentation-only evidence/hash/link checks.

## 8. Risks

Scoped handoff:836 canonical paths/174 Markdown/2394 local links/0 broken/0 duplicates;
18 phase rows valid. Actual git diff --check exit0,31 existing LF/CRLF warnings.
179 prior-covered engine files:only paging contract/original scope/native query/build identity
changed;215 authored JSON files unchanged. Sole GameClock SHA unchanged. This is a scoped
inventory, not full regression or full-plan proof; final readonly checks in excluded manifest.

Binding must not collide or depend on current culture. Providers own canonical immutable strings.
Authorization precedes scope checks to avoid revealing private change/refusal information.
Conservative existing scoped versions may invalidate unrelated visible layers; no precision claim.

## 9. Out of scope

Precise mod regional history, global provider-purity proof, hostile provider audits, Oil migration,
replay/soak/allocation/full regression, Tycoon, hosts/demo/Draw/platform implementations and publishing.

## 10. Sub-task execution order

- [x] V3.1 Replace original page protocol and extend existing cursor scope in place.
- [x] V3.2 Migrate native/public point provider and old callers directly; no old runtime.
- [x] V3.3 Add one bounded native/mod case including real player-earned saved discovery.
- [x] V3.4 Two clean builds; same ONE new case74/0/0 final; green source frozen, attempts recorded.
- [ ] Precise mod history/global purity/hostile providers/full phases and whole-goal verification.

Open decisions: none within the approved ownership boundary. Full-plan gates stay open.
