# P9-D5j: Lost original named-port access during a loaded haul

Status: native slice focused-verified; full P9 and P1-P12 remain OPEN.
FINAL172/0/0 in1513ms; FIVE clean builds, final5.21s.
[Evidence](../../docs/migration/P9_ORIGINAL_PORT_RECOVERY_VERIFICATION.md).

## 0. Problem statement

ResourcePortContracts.Store permits ResolveStore(original) to return null for a lost explicit
original binding. WorkBoard.ValidateHaul currently faults on that legal state, EarnedHandling
dereferences it, and LogisticsService.TryReplanDestination refuses when the physical site survives.
An acquired receipt must remain attached to its actual endpoint, not the port's new selection.

## 1. Goals

1. Distinguish physical site loss from unavailable original named-port access through IHaulExecution.
2. Require an explicitly authored reroute blocker for each cause; never implicitly grant recovery.
3. Validate surviving original receipts through the existing transaction owner even without access.
4. Preserve actual cargo/job/carrier and wait for a complete replacement; cancellation releases the
   original book, while restored original access resumes the same physical delivery.
5. Keep earned handling validated against the named port's immutable authored capacity policy.
6. Prove partial-unload residual recovery, unavailable-capacity saves, restoration and cancellation
   with normal JSON, player commands, immutable views, bounded Update stages and replay.

## 2. Design

Add CheckDestinationBinding(Job) to the existing IHaulExecution; LogisticsService uses its original
PhysicalPort resolution, returning core.not-found or haul.original-destination-binding-lost.
WorkSystem consults it before executing loaded delivery. TryReplanDestination distinguishes absent
geometry from missing access, preserves physical dock departure when geometry exists, and retains
the existing DestinationUnavailable wait if no complete replacement can be admitted.

WorkBoard uses that same observation to validate the existing wait and explicit policy permission.
Missing physical access does not invalidate a surviving endpoint receipt: ReadClaims still proves
the exact owner/token/resource/amount. A present but foreign/mismatched binding still faults.
EarnedHandling uses the exact named definition capacity policy when original access is unavailable;
there is no invented quantum, earned time, stock, default endpoint or new persisted state.

The existing external PoolPort provider gains required JSON original_access: retained/current_only.
All current documents explicitly migrate to retained. New normal content authors current_only and
changes selection through the existing ordinary SelectPoolPortCommand, never a driver fault switch.
This models access loss, NOT deletion of the backing store/provider.

ValidateLoadedDemands preserves legally disconnected original intent and validates its immutable
packing through CheckStockCapacity, shared with current admission rather than a copied calculator.
The same focused case found/fixed SyncRecoveries restoring absent cargo-return SourcePort as Code.None.
Core2.326.0/save154/logistics24/WorkBoard50/WorkSystem43/work-view92/logistics-view38;
existing saved layouts unchanged. All old pool ports explicitly migrate original_access to retained.

## 3. API surface

IHaulExecution.CheckDestinationBinding(Job); HaulRecoveryCodes.DestinationBindingLost.
No new command, claim owner, scheduler, stock store, clock or compatibility reader.

## 4. Files

Standard/WorkContracts.cs, HaulResourcePorts.cs, Work.cs, LogisticsRecovery.cs, semantic protocol
identities and views; existing external port provider/content; one normal master-listed scenario,
one new exact player proof; evidence, existing parent plans, master tracker and completion audit.

## 5. Errors

Missing original access is a typed gameplay blocker; provider mutation/wrong identities remain
faults. No current-selection substitution. Whole replacement claims precede original release.
Too-small demand cannot steal a partial capacity promise or silently discard residual cargo.

## 6. Compatibility

Development migration only. Required authoring and semantic identities migrate in place; no legacy
reader, overload, shim or parallel recovery path.

## 7. Verification

Compile when necessary. ONE NEW bounded exact case only; repair and rerun only that case. Each
physical stage is bounded and prints job state on failure. Save/load twin identity and replay use
the same normal commands and Game.Update. Historical/family/full/Oil/platform runs stay closed.

## 8. Risks

Partial unloading must preserve its already-delivered prefix while replacing only real remaining
cargo. Surviving original claim release cannot depend on physical access. Dock egress must retain
geometry rather than treating access loss as physical demolition.

## 9. Out of scope

True provider/store destruction, arbitrary corruption, dock/fuel/emergency stress breadth, global
acceptance, adapters/Draw, Oil/Tycoon changes and commit/push before the full goal and plan finish.

## 10. Execution

Revise original binding admission/validation/recovery; migrate explicit authoring; add the normal
scenario and thin exact proof; compile and verify once ready; repair only its demonstrated failures;
record actual evidence and update tracker/audit without claiming phase or goal closure.
