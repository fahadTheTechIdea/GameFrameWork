# P3-V3 — Query-bound spatial page cursors verification

Date2026-10-08; canonical Beep.SimEngine only. Build2.280.0/save140.
[Stage](../../plans/enhancements/P3_SPATIAL_PAGE_CURSORS_DESIGN.md),
[tracker](../../plans/enhancements/MASTER_TRACKER.md),
[source inventory](P3_SPATIAL_PAGE_CURSORS_SOURCE_MANIFEST.json).

## Outcome and ownership

Scoped implementation verified, full plan/goal active. Bare ExpectedVersion and page Version removed;
request/result use the original opaque SpatialRegionCursor. Its existing scope now checks immutable
provider binding, not another page store/history/counter or live provider reference. Null cursor starts
an explicit new read. Native tile and public point provider reuse their original viewer versions.
Binding identifies exact layer/geometry using invariant length-prefixed identity and integral/raw
coordinates. Offset/capacity are per-call limits, not world limits or part of regional identity.

The same SpatialLayerPaging owner validates envelope, then provider checks actual sample visibility,
then validates cursor and publishes page facts. InvalidCursor identifies a foreign query/view/session/
provider/load/layer/region/protocol. Actual owned stale/future version returns Invalidated. Refusal
clears destination and reveals no count, offset, cursor or values. Known prefix plus unknown gap
returns no partial values. Existing conservative viewer version may invalidate other visible layers;
this does not claim precise per-layer histories or complete hostile-provider/global-purity proof.

No new command, money requirement, clock, scheduler, saved state or JSON authoring change. Public
non-tile provider uses only public kernel contracts. All normal physical drivers issue player commands,
read detached views and call Update/clock controls; native negative protocol setup is explicitly
instrumentation of actual native provider/knowledge, not a normal-player visibility grant.

## Every build/runtime attempt

1. `dotnet build Beep.SimEngine.slnx --no-restore`:exit0,7.07s reported/7.8277053s tool,0 warnings/errors.
2. ONE exact NEW case FIRST runtime:15 passed1 failure/one suite abort158ms;1.8502888s tool,exit1.
   Native diagnostic setup wrote9/4 to a fixed layer linked to an authored discrete count resource.
   Original provider correctly rejected FractionalDiscrete. Corrected only this NEW test to whole9.
3. Same build:exit0,3.94s reported/4.619681s tool,0 warnings/errors.
4. SAME ONE exact NEW bounded case:74 passed0 failures/aborts1352ms;3.0273675s tool,exit0.

Exact runtime command:
`dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build --
"Spatial page cursors: bound native and mod reads with physical saved discovery"`

Source/proof/tests/JSON frozen immediately after final green. Nine old page probes and146 identity
paths (EngineInfo+145 older fixtures) compile-only. Two old changed-region expectations now directly
use stricter InvalidCursor semantics; no old numeric compatibility overload. No old/family/full/
platform/stale runtime. Physical-stage bound2000 Updates includes diagnostic snapshot on timeout;
not a CPU/allocation/stress budget. No timeout occurred in the final case.

Read-only preparation errors: PowerShell brace-list parse; guessed TestRunner path missing (located
with rg); oversized JSON inspection truncated at524288 bytes (replaced with rg JSON matching lines).
One identity patch verification refused already-migrated caller because slash normalization differed;
no files changed by that refused patch. Normalized exact paths and applied non-overlapping identities.
These were not build/runtime attempts, did not reset data or grant gameplay fallbacks.

## New exact case coverage

- Shared long.MaxValue envelope; stale/future/foreign identity/binding; invalid provider facts/envelope.
- Native authored70000-square query, authorized one-sample page; no huge allocation/world cap.
- Actual sample authorization precedes cursor metadata; unknown gap yields no prefix; hidden same-chunk
  writes preserve owned readable page, visible exact writes invalidate. Different rectangle/layer/
  value representation/authorized viewer/change protocol/restore refuse typed, detached empty results.
- Retained page facts immutable; successful/refused reads preserve authoritative hash.
- Ordinary native timed site, actual moving state and loaded haul, saved cargo twins, sole pause,
  delivered construction and exact50−30=20 steel conservation.
- Ordinary work-policy/physical mod discovery, partial progress does not expose knowledge; partial
  work saves and resumes twin-identically; exact8 supplies consumed. Earned company-private visibility.
- Public mod paging variable capacity, exact start/count/fractional spacing binding, layer/protocol/
  original/restored/other-query/provider refusals; exact saved5/2 quantity. Interleaved idle session pure.

## Review and remaining gates

Scoped handoff:836 canonical paths/174 Markdown/2394 local links/0 broken/0 duplicates;
18 phase rows valid. Actual git diff --check exit0,31 existing LF/CRLF warnings.
179 prior-covered engine files:only paging contract/original scope/native query/build identity
changed;215 authored JSON files unchanged. Sole GameClock SHA unchanged. This is a scoped
inventory, not full regression or full-plan proof; final readonly checks in excluded manifest.

SRP: scope validates detached identity only; paging validates envelope/freshness only; providers own
geometry/visibility/values. OCP/DIP: public contracts admit other authored spaces without kernel shape
switches. ISP: paging remains optional. LSP: native and public proof implement same refusal guarantees.
No parallel models or legacy/shims; resource invariants, existing state owners and one clock retained.

P3-V2 paging ownership follow-up is closed for native/public point implementation. Precise mod regional
history, arbitrary/hostile provider purity, load/destruction breadth, Oil migration, replay/soak/fuzz/
allocation/full regression and whole P1–P12 acceptance remain OPEN. User prohibits broad runtime
without explicit authorization; focused evidence cannot close those gates. No commit/push until the
full goal AND plan finish and are verified. Lifecycle skill organized stage/tracker/evidence, not broad runs.
