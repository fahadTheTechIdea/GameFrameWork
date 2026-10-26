# P11-D3 — Exact spatial observer identity

2026-10-07. Build2.220.0/save124; native payloads/views unchanged. Shared SpatialLayerAccess recognizes
exact PlayerId.None, not arbitrary negative IDs. Native TileMap version/chunk/delta/snapshot and
public point-provider cursors use the same exact distinction; invalid negative viewers get no
changes. Existing World.ViewFor still rejects malformed/nonseated identities. No new knowledge/fog/
layer/stock/cursor owner, state table or clock. No compatibility alias or old save reader.

ONE build and ONE exact new bounded protocol scenario, first attempt:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Spatial visibility: exact observer identity and private cursor isolation"
```

Build0 warnings/errors3.83s. Scenario89 passed,0 failed,0 aborted suites313ms. No repair/rerun/stale
binary, older/family/full/platform gate or post-gate gameplay/test edit.93 older assertion files/
151 current identity lines compile-migrate only; no old suite result is claimed for current source.

The authored embedded `spatial-observer.json` supplies time, wide sparse tile space, native units,
resources and public/private layers. The existing SpatialLayerTests.Factory preloads its contents
under the logical master document `layers.json`; no new loader/default/provider registry. Existing
point-layer JSON/PointGame master/provider remain unchanged. Native assignment/value writes are
explicit internal layer-fixture hooks, NOT a new player discovery/write capability or physical
gameplay acceptance. Point setup uses ordinary owner/sample commands through the existing provider.

The single case checks invalid IDs -2,-7,int.MinValue on public/private live/detached native/public
contracts. It observes no invalid metadata, values, private version/chunk changes or full-invalidations.
Denied pages expose no count/version/page/region/type detail and clear old output. Native owner/
foreign/exact-observer views preserve actual access; real private writes do not affect foreign
cursors. Invalid point writes refuse without stock/sample mutation. Pure hash checks and saved
native/public twins confirm existing sparse codecs and Update continue with actual authorized values.
Fixed small loops and one-step continuation are bounded; no long acceptance workflow is run here.

Development-lifecycle/SOLID review revised existing contract/cursor owners, not a parallel subsystem.
No host/demo/OilFieldDays/platform/Draw/economics changes. This is NOT partial-cell discovery, typed
layer knowledge, public-hidden activity gating, positive nonseat direct-provider authorization,
relationship/event/ID/custom-feature/global purity, pressure/strategy/replay/stress/full acceptance.
Those remain open in P11 and the full tracker. Historical reports/inventories remain unchanged.

[Design](../../plans/enhancements/P11_SPATIAL_OBSERVER_IDENTITY_DESIGN.md),
[tracker](../../plans/enhancements/MASTER_TRACKER.md),
[scoped inventory](P11_SPATIAL_OBSERVER_IDENTITY_SOURCE_MANIFEST.json), excludes itself.
Read-only hash/link/clock/diff checks are not runtime gates. Full goal/plan remain active;
commit/push only after FULL completion and verification.

Scoped inventory241 unique files/37 Markdown documents; final hash/link checks require0 mismatches/
broken links. `git diff --check` exited0. Sole GameClock source unchanged:
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.
