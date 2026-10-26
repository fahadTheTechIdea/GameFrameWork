# P3-S5e — Private ownership and atomic layer invalidation

Status: implemented and focused-verified, 2026-10-06. Build2.108.0/save65/map4, native payloads unchanged.
Clean build;241 checks/two exact suites, zero failures/aborts.
[Evidence](../../docs/migration/P3_LAYER_INVALIDATION_VERIFICATION.md).
Continues the existing spatial-layer owner; full P3 remains partial.

## 0. Problem

`Map/TileMap.cs` assigns private ownership using `TouchAll(null)`, which invalidates every company,
including companies that cannot observe either owner or values. `MapLayer<T>.Set` and `Fill` commit
values before map cursor increments; overflow can therefore leave changed values with stale views.

## 1. Goals

1. Ownership transfer invalidates only the observer, previous owner and new owner.
2. Refused cursor increments preserve values, ownership and every map/layer cursor.
3. Keep exact sparse values, provider registration, JSON metadata, saves and query contracts intact.
4. Verify through two exact suites only; no full/family regression or platform work.

## 2. Design

Keep invalidation in `TileMap`. Preflight all affected counters before value/owner mutation.
`MapLayer<T>.Set` preflights after its same-value exit. `Fill` stages generation as before and
preflights again after callbacks, since callbacks can change other layers. Map touch operations also
preflight their own counters. This is scoped atomicity for counter refusal, not global rollback of
generator callbacks, allocation failure, provider faults or other gameplay state.

Private ownership uses the same existing viewer records: bump observer full invalidation and each
affected owner's full invalidation, never public or unrelated private records. New owners inherit the
current public cursor through the existing initializer. No saved cursor, secondary ledger or layer
store. Counters remain transient representational cursors, not authored map size limits.

Engine build changes to 2.108.0. Save format65 and native map schema4 remain unchanged; no old reader.

## 3. API surface

No new public gameplay API. Existing layer writes, owner assignment, immutable queries and deltas
continue through their canonical owners. Layer semantics remain JSON and public contracts.

## 4. Files to change

| Action | File | Scope / risk |
|---|---|---|
| Modify | src/Beep.SimEngine/Map/TileMap.cs | Preflight and scoped owner invalidation; medium |
| Modify | src/Beep.SimEngine/Kernel/SaveGame.cs | Current build identity only; low |
| Modify | tests/Beep.SimEngine.Tests/SpatialLayerTests.cs | Direct native fault/visibility proofs; medium |
| Modify | tests/Beep.SimEngine.Tests/*Tests.cs | Build assertions compile-only; low |
| New | docs/migration/P3_LAYER_INVALIDATION_VERIFICATION.md | Actual focused evidence |
| Modify | master/P3 stage/AGENTS/README/help | Current scope and remaining gates |

## 5. Error handling

Counter overflow throws before the requested value/owner/cursor mutation. Same-value and same-owner
writes remain no-ops, including at the representational cursor edge. Invalid session/seat/public
ownership still refuses through existing checks. Private reads remain unavailable to other companies.

## 6. Compatibility

Development-only clean revision. No legacy path, facade, fallback, duplicate model or old-save support.

## 7. Verification

Build `Beep.SimEngine.slnx`; run only:

- `Spatial layers: canonical sparse storage and generation`
- `Spatial layers: private ownership, detached views and saved sparse values`

Extend these suites with public/observer/owner counter faults; private assignment/transfer unaffected
third-company cursors and continuation; generator-induced counter changes; same-value/owner no-ops;
save/hash equality after refusal and restored continuation. Other suites compile only.

## 8. Risks

Framework test reflection injects transient counter exhaustion; modules never gain backing-store
access. Callback side effects are not compensated. Memory allocation failure is outside this proof.

## 9. Out of scope

Extraction/hydrology/3D, arbitrary provider faults, replay/stress/full acceptance, Oil/Tycoon,
hosts/demos, renderers/Draw/platform SDKs and changes to cursor representation.

## 10. Execution order

- [x] Revise canonical native preflight and private ownership invalidation.
- [x] Add tests; migrate identity assertions and build.
- [x] Run the two exact gates and review scoped atomicity.
- [x] Record evidence and update master/parent/guidance together.
