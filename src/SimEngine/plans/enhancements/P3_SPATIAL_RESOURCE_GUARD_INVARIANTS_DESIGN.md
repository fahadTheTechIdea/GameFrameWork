# P3-S39 — Pure spatial resource guard invariants

## 0. Problem

SpatialResourceCellBindings verifies a guard's quantity/version/owner during pure admission, but silently changing the game clock or unrelated entities is not detected. Such a preflight can run before publication and return or throw without a world fault.

## 1. Goals

1. Bind every actual resource-cell guard to its immutable session through a required contract, never a guessed world.
2. Observe pure guard clock/random/identity/entity and quantity/version/owner facts using original engine scopes.
3. Keep ordinary unchanged refusals healthy; preserve the original exception and latch a fault when mutation accompanies refusal.

## 2. Design

ISpatialResourceCellGuard declares World. Original SpatialResourceEndpoint exposes its existing readonly world. SpatialResourceCellBindings validates the actual session/space on binding and keeps that session reference, not another state owner.

SpatialMutation reuses one internal scoped-facts capture for publication and pure Check. Successful guard callbacks verify world/entity and original cell facts; silent mutation faults spatial.guard. A thrown callback is rethrown unchanged: only mutated scoped facts make it terminal. Existing material guards and expected refusal semantics remain authoritative.

## 3. API/state

One required immutable guard metadata property, migrated directly. Build2.589.0/save157/replay1; payloads unchanged. No default/legacy world fallback.

## 4. Files

Kernel/SpatialMutation.cs and SpatialResourceCells.cs; original SpatialResourceEndpoint; focused guard/publication probes and engine/protocol identity.

## 5. Errors

Wrong session/space binding refuses. Unchanged argument, capacity, promise and overflow refusal remains healthy. Silent scoped mutation or mutation-plus-error is terminal, preserving the first error. No preflight mutation rollback or second fault state.

## 6. Compatibility

Required contract migration and existing owner refactor in place; no compatibility reader or alternate guard engine.

## 7. Verification

Two targeted builds clean7.02s/4.71s. Six exact focused cases pass572 checks: guard modes130, publication96, atomic refusal56, independent extraction89, native extraction92 and actual native/point receipt admission109. [Evidence](../../docs/migration/P3_SPATIAL_RESOURCE_GUARD_INVARIANTS_VERIFICATION.md). No full/profile/family run.

## 8. Risks

Arbitrary private component/service memory, malicious metadata getters, all guard/owner/codec callbacks and per-entity capture budgets remain wider open gates. Physical stocked recovery/utility/lifecycle/global acceptance remains open.

## 9. Exclusions

Oil/Tycoon/platform/host changes, invented world/defaults, rollback, full regression and Git publication.

## 10. Status

Required session contract and scoped guard verification complete. Other callback/private-memory/performance and physical stocked recovery/utility/lifecycle/global gates remain open. Full goal active; no original broad checkbox closes.
