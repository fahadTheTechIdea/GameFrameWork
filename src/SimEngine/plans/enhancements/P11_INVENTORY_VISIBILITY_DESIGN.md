# P11-IV1 — Inventory observation follows current entity visibility

## Current M148 — selected quantity-provider/original-binding fault boundary, 2026-10-23

Scoped verified491/0 in one new exact case; native2.772/save161/replay2/inventory view11
is unchanged from M147. [Actual selected provider/binding matrix, attempt and limits](../../docs/migration/P11_INVENTORY_VISIBILITY_VERIFICATION.md#current-m148-selected-scalar-provider-fault-and-original-binding--2026-10-23).
Extend the existing AtomicInventory lifecycle probe with paused scalar reads and one
explicit negative-quantity mode, retaining its original ResourceInventoryState owner.
One new exact case tests healthy local/named/exact-original and stale-original/private/
unknown refusals, saved idle twins and pause/continuation. Then minute/half-minute and
local/named contexts exercise private persisted mutation without Touch, clock mutation,
first callback exception and invalid negative amount. Require no successful scalar,
terminal cached own/foreign/observer/missing reads, unchanged first fault, no callback
retry/save/hash/update, and an isolated peer. Negative live access observes only existing
probe counters; normal reads remain public. No new provider, inventory, state, clock or
gameplay owner. Run only the new60000ms literal; do not repeat M147's green physical cases.

Current M147/native2.772/save161/replay2/inventory view11: detached TryGetAmount uses
the same local/named/original resolver and whole-provider observation as capacity/receipts.
Visible zero is available; hidden/foreign/missing/unknown clears output and is unavailable.
Existing cached-hidden physical-save case399/0 now checks local quantities before hiding,
while paused/hidden and after reveal; its proof requires exact view11, no fallback.
[Actual attempts/image boundaries and broader limits](../../docs/migration/P10_CONTROL_CONTENT_CONTRACTS_VERIFICATION.md#current-m147-reusable-resource-controls-and-detached-quantities--2026-10-23).
Arbitrary quantity-provider faults/private purity/named-original matrices remain open.

Current M134 leaves native2.765/save161/replay2 unchanged and qualifies original receiver
selection with area-gated private service only on a discarded far candidate. Known serviced
truck admission and actual two-unit evacuation stay native, with saved twins/finite19stock.
[Focused171+240=411/0 and every fixture failure/diagnosis](../../docs/migration/P9_RECOVERY_DESTINATION_POLICY_VERIFICATION.md#current-m134-area-gated-evacuation-receiver-dependencies--2026-10-23).
No free fuel, collision exemption, changed128update bound or gameplay in the driver. Six final
replay verifiers pass; prior native census is reused after exact source/DLL/PDB preservation,
not freshly run. Original broader gates remain OPEN; next audit returns to P7.acceptance.017.

Earlier M114/native2.748/inventory view10/logistics view51 closes whole receipt/nested carrier
resource disclosure through the original metadata authority. CanSeeResources moves the existing
quantity-row loop to PlayerView, no duplicate visibility policy. Receipt copies publish only
after every actual record is authorized; per-resource ports remain safely scoped and unchanged.
Exact sight-proof view requirement migrates to10, same JSON visibility/codec/query-only probe.
New mixed369 plus directly affected165+246 =780/0, four actual full verifiers. Eleven older
entity callers remain compile-only. [Every attempt, physical proof and open provider matrices](../../docs/migration/P7_CARGO_DISPOSITION_OBSERVATION_VERIFICATION.md#current-m114-mixed-resource-carrier-and-receipt-disclosure--2026-10-23).

## Historical P11-IV1 design — 2026-10-12 (not rerun for M112)

## 0. Problem statement

InventoryView.ReadResourcePort, TryGetCapacity and TryGetReservations check entity existence/ownership but omit PlayerView.CanSee. A previously obtained entity ID can bypass a legitimate mod visibility policy after sight is lost. ReadResourcePortChoices already applies that policy.

## 1. Goals

1. Use one original visibility authority for all inventory entity observations.
2. Refuse hidden cached IDs before stock/capacity/receipt/provider reads and retain default/empty output.
3. Preserve authorized observer behavior, current ownership refusals for visible targets, and detached prior facts.
4. Prove ordinary visibility commands, physical manufacture/cancellation, hidden save/load and replay without driver-side gameplay.

## 2. Design

One private InventoryView.Visible helper delegates to the existing viewer's PlayerView.CanSee. No second fog policy, visibility cache, entity state or stock owner is created. Existing first-fault handling and immutable provider snapshots remain authoritative. Inventory view schema7 becomes8; engine2.508.0/save154/replay1.

A selected independent proof module reads its target/initial mask from scenario JSON, implements IVisibility and IPersistentService, and changes its own sight state only through an ordinary command. Both service aliases share one bool codec. The new driver hides the actual production destination after physical work begins, tests cached/private/observer queries, saves hidden twins, pauses/resumes and reveals the same surviving entity. Existing physical helper default behavior remains unchanged when its optional checkpoint is absent.

## 3. API surface

No production method signature change; hidden port reads return NotFound and boolean reads return false/default/empty. Visible ownership checks remain unchanged. The existing inventory view identity is version8. Draw/platform adapters remain outside scope.

## 4. Files to change

Existing InventoryView, InventoryModule view descriptor and engine/protocol identity; existing ResourceContentContractTests helper checkpoint; new engine-side InventorySightProof module and new public player driver; test profile/registration and tracker/evidence.

## 5. Error handling

Hidden entities never invoke inventory providers and expose no prefix, capacity or tokens. Invalid visibility callbacks remain governed by the original observation/fault boundary. Sight state, stock, promises and time continue to use their original owners; no rollback/default substitution.

## 6. Compatibility

Development-only protocol identity change; no shim or old view registration.

## 7. Verification

One solution build and one new bounded exact case (90seconds), with same-case repairs only if needed. Four minute/half-minute completion/cancellation workflows prove physical supply/WIP/tools, hidden saved continuation, pause/reveal/private delta retention and replay. No older case/family/full executor is invoked.

## 8. Risks

Visibility is broader than ownership; an optional game may deliberately hide an owned entity. A hidden panel must refuse even though its owner has retained prior immutable facts. This is not a complete metadata/fog/private activity or allocation/stress proof.

## 9. Out of scope

Tycoon rules, Oil content, quality/lot gameplay, other public-view disclosure gaps, platform/Draw/adapters and final full regression. Conditional lot selection was inspected but no lot module is selected; its broader original gate is not claimed by this inventory slice.

## 10. Execution order

1. Apply current visibility to all inventory entity observations.
2. Add a selected ordinary engine-side visibility module and player-driven physical checkpoint.
3. Build/run the one new exact case; preserve any failure diagnostics.
4. Update evidence and tracker while leaving whole P3.008/P11.033/full goal open.

## Post-pass metadata verification

After FINAL,1825 source anchors,1020 executable inputs and13 quickstart anchors validate without hash mismatch or executable drift. Inventory:3008paths/1085Markdown/8777local links/0broken;18master rows/0malformed. All221 original gate literals/anchors validate unchanged (192checked/29open). Diff check exits0 (34existing LF/CRLF warning lines);2996dirty paths/0staged. All12Tycoon source files andsole clock implementation match the previous checkpoint. No full regression, staging, commit or push occurred.
