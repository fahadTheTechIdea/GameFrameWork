# P5-A24 — Canonical physical resource recovery

## 0. Problem statement

Oil scenarios explicitly disable recovery. Stocked cancellation/removal therefore keeps a typed
missing-recovery blocker; a full colony game needs real stock disposition and physical retrieval.

## 1. Goals

Select a file-owned native recovery profile in all six scenarios. Preserve existing stock through local
packing and ordinary timed hauling, without deletion, teleport to depot, new ledger or recovery manager.

## 2. Design

recovery.json declares oil.recovery in oil.tiles and one oil.recovered-stock marker prototype.
It selects the existing40-slot capacity policy, initially empty inventory, all authored resource phases,
private company stock, one oil.stock port, and nonblocking stationary1x1 presence. These are explicit
new game-content choices, not engine limits or copied stock. Manual evacuation is selected: recovered
material waits for actual player/logistics commands, never an invisible automatic return.

The master registers the file once and each canonical scenario selects oil.recovery. ConstructionService
remains the only packing/disposition owner, ResourceTransactions the only transfer/stock authority,
and Work/Logistics/Motion the physical hauling owners. Local packing follows the source placement;
the original container capacity/quantity rules split larger lots, not an unlimited bag.
No new system/clock/state source or compatibility fallback.

## 3. API surface

Existing IResourceRecoveryDefinition/ResourceRecoveryDeclaration/IResourceRecovery/IInventory/paired
resource ports and immutable ConstructionView/InventoryView/WorkView. Ordinary ConstructCommand,
HaulResourcesCommand and CancelConstructionCommand. Game decisions/time/state remain inside the engine.

## 4. Files

New canonical recovery.json plus master/scenario selection. Existing public integration project gains
one distinct focused recovery case and shared read-only file fixture access. No production harness/demo/
platform or business changes. Stage/evidence/master/audit updated in place.

## 5. Errors

Explicit null selection retains stocked sources with native missing-recovery refusal; no implicit
container. Packing requires actual spatial placement, compatible capacity/quantum and released claims.
Unsupported/unreachable/full/removed endpoints retain original engine refusal/recovery.

## 6. Compatibility

Development-only replacement of disabled scenario recovery; null remains a valid explicit game choice.

## 7. Verification

One new bounded actual Oil public-API case creates a real pad site, holds its grader, physically stages
six cement, cancels through native cleanup, saves stock/claims and retrieves recovered stock by timed
haul. An explicit-null fixture verifies retention instead of loss. Canonical modules/components are
not projected; fixtures use24x24, one native-minute update per frame, explicit manual supply for this
pad activity, and null recovery only in the negative case. No material or state is written live.
Not full-size/all-phase/multi-container/fault/congestion/all recovery acceptance.

## 8. Risks

Forty normalized slots are authored container balance, not physical volume. Local recovery packing
uses existing co-located stock mechanics, while depot delivery must be physically hauled.
Large lots/non-tile/complex claims and broader recovery durability remain open.

## 9. Out of scope

New recovery/stock/cargo/clock owner, fabricated stock/yields/refunds, instant depot return, full
regression, business/platform/Draw/host gameplay changes and Git writes.

## 10. Execution stages

1. Author native container/profile and select it in canonical scenarios.
2. Verify actual stocked cancellation/local preservation and physical retrieval, including saves.
3. Verify explicit null refusal retains stock, without weakening admission.
4. Record scoped evidence/tracker/audit; original acceptance gates stay open.
5. Continue whole Oil recovery/network/business/industrial/private/rival/durability acceptance.
