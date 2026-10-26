# P11-D12 — Authored survey freshness and exact sample visibility

Current M86/native2.727/save161/replay2: original fresh/regional strategies replace local Capture/
Verify snippets with existing EntityObservation.ReadProvider over actual callback and selected
space. Actual old-engine304/38 reproduces direct callback errors/space mutation escaping healthy;
normal wrapped views were already protected. Corrected20-context398/0, finite point264/0, native
tile freshness122/0 and regional183/0: latest967/0 on one final image, three complete final-hash
replays. Only two strategies/EngineInfo change among450 native sources; no new owner/schema/clock.
[Current source responsibility, every attempt, actual replay scope and remaining gates](../../docs/migration/P11_KNOWLEDGE_FRESHNESS_VERIFICATION.md#current-m86-self-observed-spatial-coverage-strategies--2026-10-22).
[Earlier M85 independent finite authoring](../../docs/migration/P11_KNOWLEDGE_FRESHNESS_VERIFICATION.md#current-m85-independent-finite-spatial-readings--2026-10-22)
retains its actual2.726 evidence.
[M83 protected bulk composition](../../docs/migration/P11_KNOWLEDGE_FRESHNESS_VERIFICATION.md#current-m83-protected-bulk-changes-and-earned-freshness--2026-10-22)
and [M82 lookup observation](../../docs/migration/P11_KNOWLEDGE_LEDGER_DURABILITY_VERIFICATION.md#current-m82-observed-knowledge-provider-lookup--2026-10-22)
retain their original evidence. Original221/210source-recorded checked/11open/full goal active/no
original closure. Earlier D12 identities/counts below are historical, not current full acceptance.

2026-10-08. Scoped implementation verified:84 checks/ONE exact NEW bounded case0 failures/aborts,
460ms,FIRST attempt; ONE clean --no-restore build13.44s,0 warnings/errors. Full P1–P12 goal remains
active. [Evidence](../../docs/migration/P11_KNOWLEDGE_FRESHNESS_VERIFICATION.md).

## 0. Problem

The sole knowledge ledger retains physical observations and chronology, but a host must infer
whether a reading is stale. Permanent regional facts cannot express expiring exact survey samples.
No new knowledge table, fog map, timer, work queue or host-side rule is needed.

## 1. Goals and ownership

Extend CompanyKnowledgeService through IKnowledgeFreshness, aliasing that same instance.
Optional IKnowledgeFreshnessProvider supplies explicit per-subject lifetime; a provider without
the capability is unsupported, not presumed permanent. Native subjects require `fresh_for`:
null explicitly means permanent, otherwise a strictly positive rational GameDuration.
Finite lifetime requires LatestObservation retention. Unknown/unobserved/foreign queries refuse
without a reading. Historical TryRead/CopyKnown still return stale detached data.

KnowledgeFreshnessSnapshot retains the original immutable observation, authored duration,
converted duration steps, current age steps, exact elapsed GameDuration and IsFresh. Age derives
only from GameClock.StepIndex minus existing ObservedTick; normal time conversion rounds positive
lifetime upward to the session quantum. Fresh means age < converted lifetime. Observation time
is the existing accepted interval-start coordinate, including the interval in which work completes.
There is no second clock or guessed timestamp offset; subquantum lifetime can expire by the first
post-Update snapshot. Pause advances neither age nor lifetime.

## 2. Spatial contract and authoring

ISpatialKnowledgeSubjectDefinition declares its actual layer and CoversSample predicate. Native
TileReadingDef and independent PointReadingDef implement it directly and delegate the same
SpatialReadingDeclaration; obsolete SpatialReadingDef is removed, not a required mod base. Native
tile coverage is exactly one cell, never a wider containing footprint. M85 now qualifies finite
point coverage through the same native policy with actual timed work/saved twins/full replay;
broader independent geometry/provider/fault matrices remain open.

LayerFreshKnowledgeVisibilityDef is one authored IVisibilitySampleTarget in the existing selected
KnowledgeVisibility policy. Required stable reading IDs resolve to spatial subjects in the exact
target layer/session. Freshness AND exact sample coverage AND existing ownership/policy gates are
required. It grants access to current native values at authorized cells; the historical reading is
still the captured historical value, not substituted current data.

## 3. Invalidation and durability

Original map scope invalidation remains the only cursor owner. Fresh finite observations declare
their actual clock coordinate as a transient scope dependency while fresh. This deliberately
conservative invalidation changes during each advancing update, including the expiry boundary,
even though visibility refresh precedes clock completion. Current permission always evaluates the
actual clock. No clock completion is moved and a failed refresh cannot advance a failed Update.
Expired scopes disappear on the next refresh; redundant invalidation is allowed, stale permission
is not. Restore derives scopes from the restored actual clock and original ledger; cursors are
session-local derived versions, not checkpointed gameplay state.

Historical D12 introduced native authored provider3/view3 with ledger2/save140 on2.261.0. Current
M86 uses unchanged provider3/ledger3/view5/save161/replay2 on2.727.0; the development identity changes
for its coverage observation correction, not the finite-content schema. Strict JSON/current protocol remain;
there is no compatibility reader or fabricated default.

## 4. Changes

CompanyKnowledge.cs owns all age/freshness validation; CompanyKnowledgeModule aliases that owner.
KnowledgeView exposes private freshness. SpatialReadings.cs owns exact native sample coverage.
One new visibility definition and section extend the existing visibility policy. Native content
files add explicit freshness; PointReadingProof only implements the new required predicate.
One native JSON visibility document and one bounded new case prove the slice.
The new case selects only the fresh-deposit rule (other public layers remain explicitly unlisted),
so moving sensor coverage cannot masquerade as the proof of time-dependent cursor invalidation.

## 5. Refusals and risks

Malformed/omitted/zero/negative duration, permanent finite lifetime, missing/duplicate/non-spatial/
wrong-layer references refuse at content/composition. Session conversion overflow refuses before
first Update. Private queries return NotFound without facts. Scoped provider observations guard
normal purity/reentry, not global hostile-mod mutation rollback. Live sample access and historical
payload are distinct; time-sensitive scopes may conservatively invalidate more than once.

## 6. Verification

ONE exact new bounded case: `Knowledge freshness: physical survey expiry and exact saved visibility`.
Build once, repair/rerun only observed failures. Native machine travel, material-consuming survey,
partial checkpoint, fresh→stale boundary, exact/mixed/private sample denial, unchanged history,
paused saved twins, re-observation, current-vs-remembered sample and independent sessions.
Invalid JSON is included within the same case. Existing identity/JSON/API changes compile only.
Record every invocation; freeze source/tests/JSON after final green and finish evidence/tracker only.

## 7. Remaining scope

Broader exploration/sharing/forgetting/freshness strategy combinations, independent provider/fault
matrices, large-scope allocation/performance, wider replay/lockstep/stress,
full P11/P1–P12 acceptance remain open. No business/Tycoon, OilFieldDays, hosts, demos, Draw,
platforms, commit or push in this slice.
