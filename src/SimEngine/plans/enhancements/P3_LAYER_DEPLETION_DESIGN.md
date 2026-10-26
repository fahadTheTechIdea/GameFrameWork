# P3-S5g — Exact provider-owned spatial resource depletion

## 0. Problem statement

`Kernel/SpatialResourceValues.cs` checks legal quantity cells, but exposes only forward
conversion. `Map/TileMap.cs` and the independent public `ExtensionProof/PointLayers.cs`
own their actual values. An extraction source cannot safely debit native resource units
until it can prove the remaining value is exactly representable in that provider's store.
No copied inventory, rounded material, implicit creation, or compatibility path is allowed.

## 1. Goals

1. One optional `ISpatialResourceQuantity<TLocation>` contract supplies actual native
   UnitQuantity reads, pure withdrawal admission and fresh exact commit using the existing owner.
2. One shared calculation converts actual stored stock to native resource units, checks a
   positive exact amount and converts the remainder back without rounding or shape loss.
3. Native byte/int/fixed and independently authored point providers use that same calculation.
4. Refusals change no value, invalidation cursor, sparse allocation, save or hash.
5. Actual depletion persists through each provider's existing Sync, with no new payload/store.

## 2. Design

`SpatialResourceValues.CheckWithdrawal(ISpatialLayer, Fix stored, Fix nativeAmount)` is
stateless. A detached result contains typed status, conversion diagnostic and exact stored/
native before/after facts only for success. Default results refuse. Associated and unlinked
measurements never become extraction stock. Invalid live material is corrupt state, not zero.
Byte/int remainders must be whole and in range even when their authored unit is continuous.

`ISpatialResourceQuantity<TLocation>` extends the actual layer contract. `CheckWithdrawal`
checks the provider's actual bounds; `Withdraw` accepts a new amount/location, not a retained
preview. Native `MapLayer<T>` reuses its sole typed `Set` and atomic cursor preflight. Point
layers delegate to their actual PointSpace owner and its existing WriteLayer/invalidation.
The contract is engine-side, not included in host views or a player command.

This is a stock primitive, like a port's Take, **not a completed extraction workflow**.
Following stage: bind actual cells as non-entity IResourceOutlet/IResourceInlet endpoints,
embed existing ResourceReservationBook at the selected owner, validate unique cell bindings,
then invoke existing IResourceTransaction movement from positive-time PhysicalWorkEffectDef
work. Receipt origin must identify the actual cell; no Produced/Initialized grant substitutes
for deposit-to-inventory transfer. Claims must protect changes/ownership and survive saves.

## 3. API surface

`ISpatialResourceQuantity<TLocation>.ReadQuantity(TLocation)` returns actual native UnitQuantity;
invalid coordinates/nonquantity/corrupt cells throw rather than fabricate zero.
`ISpatialResourceQuantity<TLocation>.CheckWithdrawal(TLocation, Fix)` and
`.Withdraw(TLocation, Fix)` return detached `SpatialResourceWithdrawal`. No renderer,
host command, timer, scheduler or new content category. Existing master-listed layer JSON
remains the resource/unit/value-type/geometry authority.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | src/Beep.SimEngine/Kernel/SpatialResourceValues.cs | medium |
| Modify | src/Beep.SimEngine/Map/TileMap.cs | medium |
| Modify | tests/ExtensionProof/PointLayers.cs | medium |
| Modify | tests/Beep.SimEngine.Tests/SpatialResourceValueTests.cs | low |
| New | tests/Beep.SimEngine.Tests/SpatialResourceDepletionTests.cs | low |
| Modify | src/Beep.SimEngine/Kernel/SaveGame.cs and current build assertions | low |
| Modify | master/parents/AGENTS/README/help | low |
| New | docs/migration/P3_LAYER_DEPLETION_VERIFICATION.md | low |

## 5. Error handling matrix

| Case | Result |
|---|---|
| Associated/unlinked/absent layer | NotQuantity; no inferred material |
| Out-of-bounds provider location | InvalidLocation; no values |
| Nonpositive/invalid native quantity | InvalidAmount and original conversion diagnostic |
| Amount exceeds actual material | InsufficientQuantity |
| Remaining stored value is inexact/discrete-invalid/out of shape | Unrepresentable with original diagnostic |
| Corrupt existing material | CorruptStateException; no fabricated availability |
| Changed sparse invalidation overflows | Existing preflight throws before value commit |
| Retained readiness after another withdrawal | Fresh actual admission, no negative stock |

## 6. Backward compatibility

None. Build identity changes. Existing payloads/schemas remain because no saved state is added.

## 7. Verification

Only these two exact complete suites (no previous/family/full gate):

1. `Spatial depletion: exact native admission and atomic stored remainder`
2. `Spatial depletion: independent geometry, private facts and saved continuation`

Build: `dotnet build Beep.SimEngine.slnx --no-restore`.

## 8. Risks

- Depletion alone would lose material if misused as a gameplay workflow: no player command
  or free inventory creation is provided; receipt-based timed extraction remains mandatory/open.
- Preview is not a claim: every commit re-evaluates actual current stock.
- Provider write faults have scoped atomicity, not universal callback/session rollback.

## 9. Out of scope

Actual extraction receipts, inventory transfer, timed work dispatch, replenishment/hydrology,
reservoir science, 3D geometry, platform drawing, Oil/Tycoon, legacy readers, replay/stress/
allocation/full-regression gates. No artificial world extent limit.

## 10. Sub-task execution order

- [x] Shared typed contract, strict native reads and exact remainder calculation.
- [x] Native owner integration and independent public-provider integration.
- [x] Two named suites: 213 admission and 61 independent/saved checks; no failures/aborts.
- [x] Clean build, scoped evidence, tracker/parent/README/help alignment.

Focused-verified at 2.116.0/save65/map5/point3; payloads unchanged.49 previous build assertions
compile only. Full regression and the receipt/physical extraction follow-on remain open.
