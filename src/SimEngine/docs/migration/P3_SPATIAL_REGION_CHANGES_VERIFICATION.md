# P3-V2 — Provider-owned regional changes verification

Date2026-10-08; canonical Beep.SimEngine only. Build2.279.0/save140.
[Stage](../../plans/enhancements/P3_SPATIAL_REGION_CHANGES_DESIGN.md),
[tracker](../../plans/enhancements/MASTER_TRACKER.md),
[source inventory](P3_SPATIAL_REGION_CHANGES_SOURCE_MANIFEST.json).

## Outcome and ownership

Scoped implementation verified. Generic ISpatialRegionChangeQuery<TRegion> and detached readonly
SpatialRegionChanges<TRegion> replace tile-only API. Cursors bind exact query scope, rejecting another
viewer/session/provider/restored query even when version numbers match. Null explicitly starts full
invalidation; refusal supplies no cursor, regions or full signal. Opaque scope contains no live world,
provider, version counter, history, codec or updater. It only validates identity/current provider version.

TileMapView uses the SAME TileMap.ChangedChunkKeys/timestamps and clipped region conversion; old
TileChunkDelta/ChangedChunksSince/InitialVersion removed with no alias/reader/shim. Native visibility,
sample authorization, ownership, persisted layer values and transient restore rules unchanged.
PointLayerView uses its SAME scoped version and declares full invalidation on actual change, empty
incremental otherwise. It does not fabricate exact regional history or install a second change log.
Full invalidation never enumerates the entire world. No kernel concrete shape switch or platform cap.

## Every build/runtime attempt

1. Canonical `dotnet build Beep.SimEngine.slnx --no-restore`:12.00s,0 warnings/5 CS0103 errors.
   Live session35214 returned after10.001412s, polled ONCE (terminal exit1). One old LayerKnowledge
   migration had inserted baseline in the first similarly named block instead of native block. Removed
   the unused first insertion and added baseline at the actual native pre-write site. Engine/proof compiled.
2. Same build: exit0,5.37s reported/5.9808641s tool,0 warnings/errors.
3. ONE exact NEW bounded case FIRST runtime:54 passed1 aborted2446ms,4.1551668s tool,exit1.
   New completion wait queried active OrderView after engine had retired completed order. Bounded
   timeout at2003 reported ready entities/no active orders; no engine fault or divergent twin.
   Corrected NEW driver to existing persistent activity-completion fact, matching exact plan/target/order.
4. Same build after this test-only correction: exit0,3.19s reported/3.8027388s tool,0 warnings/errors.
5. SAME ONE exact NEW case:81 passed0 failed/aborted1342ms,2.8717424s tool,exit0.

Exact runtime command:
`dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build --
"Regional change contracts: scoped native and mod cursors with physical saved work"`

Source/proof/tests/JSON frozen immediately after final green; following edits documentation only.
145 mechanical build identity paths (EngineInfo+144 older fixtures), six old query probe migrations,
two RegionalDiscovery factory visibility helpers compile-only. No older/family/full/platform/stale
runtime. Save140/all persistent payloads unchanged; one original GameClock unchanged.

## New case coverage

- Scope identity/current-version guards, future/negative invalid provider facts, defensive-copy
  readonly regions and no persistent scope authority.
- Native initial full signal in actual70000-square authored space, no enormous chunk enumeration.
- Native hidden writes and unknown cell in SAME chunk disclose no timing/location; learned visibility
  requests full without hidden-region enumeration. Actual known write emits one original native region.
- Invalidation never grants sample authorization inside coarse region; foreign viewer remains unchanged.
- Exact other-viewer/new-session/query-instance/provider/load and fabricated matching-integer refusals.
- Pure reads/cursors, retained native/mod results, original saved layer/knowledge continuation.
- Normal native construction as physical site; machines move, loaded cargo travels, pause/partial save
  twins and completion consume real game time.50 steel opening minus30 construction leaves20.
- Non-tile ordinary player work policy/discovery takes productive machine time, earns regional visibility
  only on completion and consumes8 real supplies. Partial-work save/twins and company-private knowledge.
- Normal ownership and known private/public sample commands invalidate through same mod contract;
  unauthorized company stays unchanged and unknown gap preview refuses. Exact fractional sample retained.
- New/load query streams rebase explicitly without serialized cursor state; interleaved session isolated.

Native protocol privacy setup explicitly uses actual native layer/knowledge contracts for negative
instrumentation, not normal-player discovery or free gameplay. Physical drivers use normal commands,
immutable projections and bounded Update; test/harness owns no completion/visibility/scheduler logic.
Each physical stage bounds2000 Updates with public entity/order/time diagnostics on timeout.

## SOLID and remaining gates

Provider owns geometry/invalidation; scope validates identity only; generic result copies detached
facts; query contract is optional. No new native value or history owner. External point provider
implements contract without scheduler/kernel changes. Trusted provider must supply immutable region
values and keep its scope private. This is not hostile-mod/reflection sandboxing or precise mod-region
history. Existing paged-query integer cursors are a separate pending ownership audit. Other shapes,
pagination/transport serialization/compression, provider purity, replay/lockstep/CPU/allocation/stress/
Oil and full P2/P3/P6/P1-P12 acceptance OPEN. Historical proofs not rerun on2.279.0.

No Tycoon business implementation/Oil/sibling/host/demo/Draw/platform changes. Full goal ACTIVE;
commit/push only after FULL goal AND plan completed and verified. Nothing published.

## Source checks

Final scoped file hashes/links/phase rows/removal searches/unchanged clock and actual diff check
recorded in self-excluded source inventory. Counts are scoped evidence, not full completion.
Read-only discovery of a guessed WorkPolicyContracts/ActivityDef path and rg Windows wildcard
failed before writes; actual definitions were located/read via rg. No destructive operation.

Scoped832 selected canonical paths/172 Markdown/2373 local links/0 broken/0 duplicate paths.
Actual git diff --check exit0,31 existing LF/CRLF warnings, no whitespace errors.18 five-column
phase rows valid; removed native API symbol hits0.178 prior-covered engine files:two changed
(native query and Build identity), plus one new kernel contract/result/scope file.215 authored
JSON files unchanged against preceding manifest. Sole GameClock SHA unchanged:
339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31.
Final readonly hash/removal/row/diff checks appended only to self-excluded source manifest.
