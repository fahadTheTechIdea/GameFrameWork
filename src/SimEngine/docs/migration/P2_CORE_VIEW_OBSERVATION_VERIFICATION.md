# P2-F4 — Core view observation verification

2026-10-08. Focused engine-boundary evidence; full goal and P1-P12 are unfinished.
No staging/commit/push. Publication waits for full goal AND plan completion/verification.

## Implemented ownership

SimGame.World was already internal and remains internal. The existing EntityObservation
now supplies ReadProvider with the provider's original codec and target/world-scalar
scope. PlayerView uses it for entity/content visibility, inspection, entity presence
and selected world-space capture. TileMapView checks visibility before guarded geometry
and uses the same explicit-world entity projection. Core list copies accumulate privately;
callback failure leaves a cleared destination, not a partial successful list/panel.

ViewFeatureRegistration remains the only factory/declaration owner. Known direct mutable
engine authorities refuse as declared contracts and actual returned objects; custom
interface aliases cannot conceal IPersistentService at return. Null, throwing or mutating
factories fault and cache no feature. Genuinely unregistered optional features remain absent.

SimGame.PublishSnapshot prepares observer and all seats before committing any projection.
The original world-scalar scope also guards the complete preparation. A first provider
fault is immutable; snapshot.capture never replaces an earlier observation.provider cause.
Fault metadata republishes each viewer's original retained immutable projection with
empty deltas and the old facts Tick. Game.Tick remains the actual completed clock coordinate.
Initial failure has empty viewer-specific facts; seats never borrow observer projections.
Fault publication does not invoke any failed provider again. Update.Time is settled after
publication, so Updated sees all streams faulted and cleared interpolation/pacing.

No new saved view state, hash format, loop, clock, registry, rollback or compatibility
overload. Build2.288.0/save141/replay1; existing module/view schemas unchanged. EngineInfo
and153 older fixture paths migrate build identity only, compile-only. Two new explicit
master-listed proof JSON documents. No Tycoon/Oil/sibling/host/demo/Draw/platform changes.

## One exact new case

`Core view observations: guarded callbacks, private facts and atomic fault publication`

FIRST runtime attempt:179 passed,0 failed,0 aborted,3525ms.

- Twelve known mutable contract kinds reject before Initialize; actual persistent objects
  returned behind an otherwise unknown interface reject. Declared null factories fault.
- Engine-owned private entities, catalogue/resources/units/capacity absence, tile area and
  occupant reads, detached selected-space facts and lazy read models preserve hash/time.
  Hidden inspector callbacks are not invoked. Repeated reads earn no work.
- JSON-authored update advances actual values/versions. Retained facts remain immutable;
  saved twins through pause and12 real Updates match; canonical replay and interleaved
  independent session match.
- Five inspector failures: own persisted value mutation without Touch, entity Touch,
  clock pause, allocation of another actual entity, and thrown exception. Partial facets
  never publish; first cause remains; no retry after fault; Save/Hash refuse partial state.
- Five visibility failures: actual provider codec or target mutation and content
  mutation/exception. Earlier successful entity/catalogue/resource rows never leak as
  a partial result. Fault publication never invokes the visibility callback again.
- Three lazy factory failures: clock mutation, real allocation, exception. Failed views
  are not cached, and a repeated read does not re-enter the poisoned callback.
- Last-seat projection throws AFTER observer/first-seat preparation. One game step is
  genuinely complete; every viewer publishes the same next fault revision with its own
  previous safe facts/deltas, not partial current versions. Updated sees all streams
  faulted and Alpha zero. Repeated fault Update advances no time and retries no provider.
- Public physical construction preview/command, loaded undelivered hauling, saved real
  travel/material delivery/machine construction, fifty-minus-thirty steel conservation,
  and canonical replay all pass through existing work/logistics/resource owners.

The proof module is a separate non-friend public-contract assembly. JSON supplies its
time/calendar, values/rates, space/prototypes, mode and startup geometry. Its ordinary
engine-side system owns update rules; test clients only drive decisions/time and inspect.
Fault delegates/counters are explicitly transient instrumentation, not alternate gameplay.
Normal physical acceptance uses public commands, views, lifecycle, save/load and replay.

## Every build and run

| Attempt | Command/scope | Result | Elapsed |
|---|---|---|---|
| Build1 | dotnet build Beep.SimEngine.slnx | 0 warnings,2 CS1061 errors in NEW proof test: IGameClock has no Pause | 6.16s |
| Build2 | same solution build after two test-call repairs | 0 warnings,0 errors | 8.86s |
| Runtime1 | exact NEW case above, --no-build | 179 passed,0 failed/aborted | 3525ms |

The test instrumentation now casts the actual public GameClock for deliberate pause
mutation; no clock contract or engine runtime repair was required. No runtime rerun.
No older/family/full/platform/stale runtime. All executable inputs frozen immediately
after first green. Subsequent operations are documentation and read-only inventories.
Each physical phase has a2000-Update failure bound with job/time/fault diagnostics.
These are not process-watchdog, CPU, allocation or soak limits/proofs.

## Limits and next work

Current expanded inventory covers1759 repository-visible/prior-referenced paths,
including all357 engine files,572 Markdown and3378 local Markdown links.18 phase
rows have valid five-column shape;both new help references exist. Actual git
diff --check exits0 with31 existing line-ending warnings.224 previously covered
authored JSON files remain unchanged;two new proof documents were added.
The older manifest was sparse:only183 engine files, with six changed in that
comparison. The original ViewFeatureRegistration also changed but had no hash
in that prior manifest. This report does not invent a prior baseline for the
174 other engine files; their actual current hashes are now captured. This is
an evidence-coverage correction, not another source implementation or rerun.
Ignored/hidden build artifacts are not a runtime acceptance inventory.

This is scoped codec/entity/clock/identity observation, NOT a whole-world purity or
security sandbox. Arbitrary custom returned-view methods, foreign-service mutation,
state omitted from provider codecs, malicious mutable spatial snapshots, hostile
IList implementations and all external space/presence/Matches fault combinations
remain unproved. Additional hashing cost is not performance/soak validated.
Faulted batched RunTicks pacing diagnostics and initial-capture fault cases need
their own focused proof; the new fault-publication acceptance uses normal Update.

Cross-order/mixed productive scheduling, distributed obligations, full accounting,
provider stress, lockstep/fuzz/soak/allocation, Oil migration, authorized broad P6 and
full P1-P12 acceptance remain open. Prior stage numbers are historical, not rerun.

See [stage](../../plans/enhancements/P2_CORE_VIEW_OBSERVATION_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md), and
[source manifest](P2_CORE_VIEW_OBSERVATION_SOURCE_MANIFEST.json).
