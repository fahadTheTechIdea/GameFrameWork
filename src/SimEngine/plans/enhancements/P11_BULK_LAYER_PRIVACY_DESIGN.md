# P11-D6 — Exact bulk layer invalidation

2026-10-07. Scoped implementation verified:132 checks/ONE exact NEW bounded case0 failures/aborts
234ms, first attempt. ONE clean build0 warnings/errors9.14s. Full P11/framework goal remains active.

## 0. Problem

`MapLayer<T>.Fill` currently calls `TileMap.TouchAll`, so an unrelated hidden sample change
invalidates every company able to observe any region of the layer. It also invalidates an identical
replacement. This contradicts the sample-private semantics introduced by regional discovery.

## 1. Goals

1. Regeneration compares actual old/new sparse values, including erased and newly allocated cells.
2. Unchanged generation changes no layer/map/company cursor and remains legal at exhausted counters.
3. Bulk changes advance each affected company once, recording ONLY its actually observable chunks.
4. Single and bulk value writes share one preflight/commit owner; no duplicated cursor authority.
5. Generator/material/counter refusal preserves original layer storage before commit; save/continue
   twins and independent private/company regions retain exact values without hidden invalidation.

## 2. Design

MapLayer owns the only sparse candidate comparison. It enumerates the sorted union of actual old and
candidate chunk keys, comparing native samples without scanning or allocating a second world grid.
Existing MapLayer.Set and Fill use the SAME TileMap value-write preparation/commit path.
Its short-lived prepared write contains derived affected chunks/actual expected versions only; it is
not saved state, a retained preview, a gameplay reservation or a second invalidation store.

TileMap preflights actual layer/map/public and affected viewer counters before value publication.
Ownership/session/identity and selected sample visibility stay existing contracts. Commit advances
actual map/layer once and each affected company's existing version once; existing chunk dictionaries
receive only authorized changes. Restoration uses an explicitly named full cursor invalidation path,
not the old bulk value-write fallback. Native resource-cell guards remain the same preflight/callback
owners. No fabricated rollback of independently committed generator callbacks.

## 3. API

Public Set/Fill signature unchanged. No new host/admin command, gameplay generation rule, service,
clock or state codec. Actual native implementation details are engine-owned; JSON regions and native
byte/int/fixed layers retain the existing master content/visibility pipeline.

## 4. Files

Map/TileMap.cs; engine identity and directly affected compile-only assertion metadata; one new native
protocol scenario and master-listed bulk-layer JSON; tracker/parent/README/help/evidence/inventory.

## 5. Errors

Failed/recursive/same-layer-mutating generator, bad quantity or current counter overflow refuses
before generated replacement. Whole empty/identical change is no-op. Prepared-write identity/owner/
cursor drift refuses explicit commit. Arbitrary provider callback mutation/rollback remains a wider
fault-isolation obligation; this gate must not claim global transactional purity.

## 6. Compatibility

No legacy PreflightTouch/Touch/TouchAll value-write paths. One native preparation/commit responsibility.
Save/view identities migrate directly; no old reader or fallback generation mode.

## 7. Verification

ONE new exact bounded scenario: `Bulk layers: exact sample-private generation and durable cursors`.
Native generator/knowledge/stock/overflow fixtures are explicitly protocol instrumentation, NOT a
physical survey or player terrain command. Validate actual master JSON, byte/int/fixed generation,
same-chunk hidden changes, disjoint private regions, exact once-only/chunk updates, no-op, refused
generators/counters, ownership, retained pages and saved continuation. One build at completed slice;
repair/rerun only an observed failure; record all invocations. Older/family/full gates remain pending.

## 8. Risks

Generator callbacks may commit independent other-layer writes; no compensating rollback. Native
generation still visits authored map coordinates and has no platform cap; sparse diff visits only
allocated chunk unions. Large-scale CPU/allocation/provider-fault matrices remain open.

## 9. Out of scope

Dynamic sensors/stale observations, rival strategy/full acceptance, external mod bulk protocols,
business/Tycoon/OilFieldDays/hosts/demos/Draw/platforms. One unchanged GameClock.

## 10. Execution

- [x] Replace duplicate/conservative value invalidation with exact native preflight/commit.
- [x] Add master JSON and new bounded native protocol case.
- [x] Verify, freeze gameplay/tests after green and update tracker/evidence/source inventory.

Build2.229.0/save133/native tile view5; public point layer view3 and all owner payloads unchanged.
101 older assertion files/166 identity lines migrate/compile ONLY, not runtime proof. Native resource
quantity legality is verified; bound receipt/provider-callback matrices remain compile-only/open.
Dynamic sensors/stale observations/arbitrary mod bulk protocols/global provider/privacy/CPU/fault/
replay/soak/allocation/full acceptance and industrial rival strategy remain open. No per-slice publication.

[Parent](P11_PROGRESSION_KNOWLEDGE_CONTROLLERS_DESIGN.md), [regional discovery](P11_REGIONAL_LAYER_DISCOVERY_DESIGN.md),
[tracker](MASTER_TRACKER.md), [verification](../../docs/migration/P11_BULK_LAYER_PRIVACY_VERIFICATION.md).
