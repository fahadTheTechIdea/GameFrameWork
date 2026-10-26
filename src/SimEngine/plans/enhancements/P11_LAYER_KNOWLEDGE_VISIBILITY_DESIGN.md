# P11-D4 — Authored whole-layer discovery and private invalidation

2026-10-07. Historical scoped implementation verified:70 checks/one exact new scenario; final clean build.
Three build invocations (initial missing import failure), two SAME scenario invocations (initial
duplicate fixture document abort); all recorded in evidence. Full P11/framework goal remains open.

## Responsibility and authored content

CompanyKnowledgeService remains the sole learned-fact ledger. KnowledgeVisibility remains the sole
selected IVisibility policy. LayerKnowledgeVisibilityDef extends the existing typed target contract
for SpatialLayerDef. Master-listed `layer_knowledge_visibility` records require exact definition,
all/any mode and nonempty resolved fact IDs. Missing targets/facts refuse composition; no inferred
knowledge, compatibility alias, alternate fog store or layer registry.

SpatialLayerAccess.CanRead is ownership/identity only, including detached retained metadata.
CanSeeDefinition validates actual session/provider/layer identity, real seats/exact observer and the
selected content policy on the layer and its linked unit/resource/vertical-unit definitions.
CanObserve combines this with ownership. Views filter both metadata and samples; hidden and missing
reads share Unavailable, before geometry/type/page details. Point player writes/assignment use the
same visibility gate; an ownership hook is not a discovery command. Engine-only observer hooks
remain explicit setup/authority, not a host API or instant discovery feature.

## Update and cursors

Optional ISpatialVisibilityInvalidation belongs to the actual spatial provider. World calls it after
new-game systems settle, after systems/entities/CompleteUpdate in normal Update, and only after ALL
services/system OnLoaded callbacks restore. No new loop, clock or scheduled gameplay system. The
end-update fault phase is spatial.visibility; a fault cannot checkpoint a partial update.

TileMap extends existing VisibleChanges records with transient readable layer IDs. PointSpace extends
its existing per-company layer cursor records. These are membership invalidation caches, NOT
authoritative knowledge or saved values. Reads/hash/save do not refresh/mutate them. Membership
changes bump only the affected company's version once; native tiles issue full re-read and clear
old chunk history without enumerating newly revealed hidden chunks. Stable membership causes no
invalidation. Value writes advance only currently authorized company cursors; hidden PUBLIC layer
writes do not leak through aggregate versions/chunks. Exact observer retains the actual global
cursor. Invalid negatives and positive nonseats receive no provider cursors/data.

Counter changes preflight before value/ownership publication. Membership refresh preflights all
changed records before publishing. Ownership preflights companies whose visible access changes.
Cursors remain transient/discard-on-load; native payloads, knowledge schema1 and point payloads
are unchanged. Existing no-policy ownership/public semantics remain on the same provider, not a
second implementation or fabricated policy. Build2.221.0/save125 rejects older semantic identities.

## Scoped verification

Reuse native SpatialLayerTests.Factory and physical TechnologyTests.DiscoveryFactory through their
existing ContentSet/master pipelines. Embedded layer-knowledge.json and layer-discovery-facts.json
are preloaded under existing logical visibility.json/knowledge.json; existing point-layers.json content
uses UNIQUE master-listed discovery-layers.json (the existing construction helper already uses logical
point-layers.json for work fixture content). Tile-layer-knowledge.json is explicitly master-listed; spatial-observer.json
continues under existing logical layers.json. No loader/mod scan/default fallback.

ONE completed-slice build, then ONE exact NEW bounded scenario:

`Layer knowledge: authored discovery, private cursors and saved continuation`

The point workflow uses normal work-policy/order/sample commands, a real worker, elapsed game time,
eight consumed materials, partial-work save, private reveal, actual samples and queued saved command.
Native tile/knowledge setup is explicitly internal protocol instrumentation, not earned gameplay.
Queries, hidden cursors, all/any matching, metadata links, known pages/chunks, invalid IDs and saved
continuation are scoped proofs. Physical waits bound900 steps with order/state/blocker diagnostics.
No older/family/full/platform suites. Repair only observed failure; record every build/run invocation.

## Remaining work

- [x] Typed JSON layer/fact targets, sole knowledge/visibility owners, actual-provider authorization.
- [x] Native/public provider private write/membership cursors, after-restore/end-update refresh.
- [x] Focused physical and explicitly labeled native protocol proof, saved continuation and purity.

- [x] Authored partial-layer regions/private single-sample queries and cursors (scoped D5 below).
- [x] Exact native bulk-write privacy via scoped D6;132 checks/ONE new protocol case, NOT D4 rerun.
- [ ] Dynamic cell exploration/sensors, arbitrary mod/protected-cell bulk matrices and stale remembered observations.
- [ ] Arbitrary-provider mutation/fault/overflow/performance matrices and allocation budgets.
- [ ] Relationship/event/custom-feature/ID information-flow audit and direct authority hooks.
- [ ] Scenario pressure/strategy, replay/lockstep/stress and full framework acceptance.

Full goal stays active. No host/demo/OilFieldDays/Draw/platform/economics changes; one unchanged
GameClock. Commit/push only after the FULL goal and plan finish and are verified.

Current [D5 regional discovery](P11_REGIONAL_LAYER_DISCOVERY_DESIGN.md) extends SAME visibility/region/
knowledge owners with JSON sample scopes/private cursors.102 checks/ONE new bounded case and final
clean2.228.0/save132 evidence; D4 older runtime gate NOT rerun. Authored regional discovery is now
scoped-verified, but dynamic sensors/stale samples/exact bulk Fill privacy and wider gates remain open.

[Parent](P11_PROGRESSION_KNOWLEDGE_CONTROLLERS_DESIGN.md), [tracker](MASTER_TRACKER.md),
[verification](../../docs/migration/P11_LAYER_KNOWLEDGE_VISIBILITY_VERIFICATION.md).
