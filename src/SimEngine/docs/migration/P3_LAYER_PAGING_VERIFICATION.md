# P3-S5c — Scoped layer paging verification

Date: 2026-10-05. Scope: standalone Beep.SimEngine only.

## Current identities

Engine build `beep-simengine-2.73.0`; save format44 unchanged. TileMap codec4 and PointSpace codec2
remain the sole layer stores. Tile view declaration3; public-only point view declaration2.
Work21/work-view26/logistics4/logistics-view9/construction-view3 and native resource layouts unchanged.
Ten existing test build-identity literals migrated mechanically; those suites were not executed.

## Executed gates

Working directory: `C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine`.

`dotnet build Beep.SimEngine.slnx` succeeded: zero warnings/errors, 2.13s final build.

`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Spatial layers: paged tile and non-tile reads without area caps"`

87 passed, zero failures/aborts, one suite, 183ms.

`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Spatial layers: private ownership, detached views and saved sparse values"`

20 passed, zero failures/aborts, one suite, 73ms.

Total: **107 checks across exactly two suite names**. Earlier iterations repeated only these names.
All other suites compiled only. No full run or family filter.

## Proven behavior

- Optional generic paging contract: one pure envelope checker, no cursor registry/cache/codec.
- Long totals/offsets, bounded actual count, exact end/empty reads and typed envelope refusal.
  Generic envelope also handles long.MaxValue totals without offset-plus-capacity overflow.
- Sparse tile world int.MaxValue × int.MaxValue admitted without area allocation; final cell and
  row-crossing pages preserve byte/integer/fixed values. Small paged traversal equals whole reads.
- Non-tile provider reads long-count, raw-fractional coordinate regions through its own geometry,
  without TileMap/NavService conversion. Whole queries refuse only their single-buffer representation.
- Invalid geometry, overflowing point arithmetic, wrong representation, missing/private identity,
  bad pages and stale versions refuse explicitly; output is empty and total/version facts are absent.
- Same-value writes preserve cursors. Visible changes invalidate continuations; foreign-private
  writes do not. Ownership revocation returns Unavailable before version checks.
- Pure successful/refused queries leave hashes unchanged. Metadata keeps immutable authored unit/
  resource/vertical aliases. Saved point/tile values and private ownership restore with fresh cursors;
  saved twins continue identically. Independent tile sessions share no live samples or page state.

## Boundary and remaining work

Paging is not a durable snapshot transaction. Keep layer/region unchanged for continuation and discard
versions on restore/new session; their existing owner resets/rebases transient invalidation. Any
visible-layer change may conservatively invalidate a page, not just its selected channel. Read/update
use the existing single-threaded engine boundary. No platform-size policy is imposed; coordinate and
collection representation remain explicit provider/runtime bounds.

No extraction/generation, new 3D model, alternate resource balance, host gameplay, Oil/Tycoon,
platform adapter or Draw implementation. Custom-provider fault breadth, extraction/invalidation
stress, allocations, replay/full acceptance and the remaining P1–P12 roadmap are unverified/open.
The lifecycle skill guided stage-first design and explicit evidence; it did not trigger broad testing.
