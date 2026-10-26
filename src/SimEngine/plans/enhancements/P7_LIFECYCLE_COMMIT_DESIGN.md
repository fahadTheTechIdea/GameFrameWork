# P7-C8: Inventory lifecycle commit integrity

## Current M66: shared scope primitive, independent operation owners — 2026-10-22

Native **2.721.0/save161/replay2**. [Current logistics boundary and complete evidence](../../docs/migration/P9_ATOMIC_HAUL_BASELINE_VERIFICATION.md#current-m66-compound-logistics-operation-admission--2026-10-22).
Kernel's single internal SimulationOperation primitive replaces ResourceTransactions' private depth/
scope implementation. Its ten entries still scope the original transaction owner's own instance,
including actual nested CommitMovement/CommitTransfers/AcquireClaims refusals. LogisticsService uses
its own instance through existing ISimulationOperationBoundary, now required by IHaulExecution.
World's frozen provider-reference index and existing guard are unchanged; no copied permission state,
new service, native base-class requirement, persistent phase or second resource/clock model.
Shared transaction144-context exact case2104/0 passes on captured build5; no subsequent change to
the transaction or Kernel scope implementation. Final logistics-specific postcondition/tests are
captured separately on build6, not represented as one full-image regression. Earlier evidence below
retains its actual native version. Full plan/C3 accounting remain open.

<a id="current-m64-contract-owned-whole-operation-admission--2026-10-22"></a>

## Earlier M64: contract-owned whole-operation admission — 2026-10-22

Native **2.719.0/save161/replay2**. [Current implementation and all evidence](../../docs/migration/P7_LIFECYCLE_COMMIT_VERIFICATION.md#current-m64-operation-admission).
The original2.601 design/results below are historical, not the current API. The same transaction
owner now protects unfinished resource operations at the existing game-loop/checkpoint boundary.

### Requirement and observed gap

Resource mutations had provider/delta/fault checks but did not announce an unfinished operation to
checkpoint or clock admission. Old2.718 reproduction permits a save in a real pre-delta commit
callback. Source inspection shows the same missing boundary through the actual delta/publication
interval; a separate old-image post-delta runtime was NOT executed. The new proof explicitly checks
both sides of real native Add/Remove. A checkpoint must not escape before stock, claims and facts
form one fully verified operation, and no new game-time update may run over that unfinished work.

### One state owner, reusable contract

- Kernel/Services.cs declares `ISimulationOperationBoundary.OperationInProgress`, a pure transient
  permission contract. IResourceTransaction extends it; independent implementations need not inherit
  the native class. This is intrinsic engine protocol, not a JSON option that disables atomicity.
- The original ResourceTransactions owns one transient depth and allocation-free scoped disposal.
  All ten mutating entries acquire it before caller/provider work; nested Commit/CommitTransfers,
  movement and ExtendClaim/AcquireClaims paths cannot release an outer operation's permission.
  No stock, promise, journal, clock or persisted phase is copied.
- World freezes only the actual declared provider references through the existing ServiceContainer
  catalog (aliases deduplicated). It caches no live permission values. The renamed internal
  `RequireCompletedOperations` reuses entity phases and observes each operation-status read through
  existing EntityObservation. A true status refuses outside that observer, so a handled normal
  in-progress refusal does not fault a healthy session. A throwing/mutating status read retains the
  existing provider fault path. No resource-specific scheduler branch or new lifecycle manager.
- Existing World Step/Sync/hash/breakdown and SimGame ordinary/elapsed/batch admission use that
  one guard before time, controllers, payload or publication. Original first-fault behavior remains.
- Public build identity changes; production codecs/save161/replay2 remain unchanged. No legacy reader,
  fallback overload or parallel API remains for the renamed internal guard.

### Focused proof and finite coverage

The same ResourceLifecycleCommitTests partial and AtomicInventory's original state/provider are
reused. Hooks are explicit transient negative instrumentation, not host gameplay. Only the new
variant selects the existing optional resource-flow module through scenario JSON; old factory defaults
stay unchanged.144 new/restored × add/remove × before/after × nine checkpoint/clock entries ×
handled/propagated contexts check real deltas, a real nested movement coordinator's normal refusal,
outer permission retention, exact success facts, terminal prefixes, saved twins and session isolation.
The nested movement is a typed invalid-intent refusal, not claimed physical movement acceptance.

Compiled interface-map audit matches24 entries (including inherited boundary metadata) to original
source; all ten mutations have scopes and every entry has a health guard. This is source/assembly
coverage, not runtime proof of every endpoint shape or all raw resource-mutator callers. Cutoff test
discovery now includes inherited interfaces; it cannot silently omit the new getter.

Latest selected evidence: new2104/0, original lifecycle306/0, coupled provider139/0/two actual public
replays on final native/proof build5; inherited cutoff338/0 on build6 with identical native/proof.
Total2887/0 spans two explicitly captured test images, not a final full regression. Only cutoff was
rerun after its test-only enrollment edit. Other repeated focused checks followed a production change
to observe status reads, not a full/profile/family workaround. All18 build/runtime logs are retained.

Original P7.017/full goal remain open. Next finish the actual material-mutator caller/accounting and
typed reference-owner matrices, then relevant bounded stage stress. No Oil/business/platform/Draw,
consumer execution, full tests or staging/commit/push. Current focused-only permission includes final.

## Historical C8 initial implementation

Status: implemented, focused-verified on2.601.0/save159. Four distinct literal cases,
1099 checks; no broad gate closure. [All attempts and limitations](../../docs/migration/P7_LIFECYCLE_COMMIT_VERIFICATION.md).

## 0. Problem statement

The original ResourceTransactions.CommitInventory verifies stock and reservation
deltas but does not itself latch commit exceptions or observe allowed-mutation
clock/entity/space facts. Direct module callers could catch an invalid provider
commit and continue or save partial state. The success-publication loop also calls
the provider's Amount getter again after the vector has already been verified.

This continues original P7.acceptance.017 fault/transaction work. It does not close
the compound spill/conversion/topology/stress gate or redefine the full plan.

## 1. Goals

1. Violating or throwing direct commits latch the existing first-fault boundary.
2. Observe original clock/random/identity/entity/space facts while permitting only
   the expected inventory version changes; no rollback, duplicate stock or clock.
3. Guard pre/post inspection as pure reads of the actual provider and endpoint.
4. Publish facts from verified local values, with no new provider callback between
   verification and publication. Preserve healthy saved-twin behavior and real work.

## 2. Design

Keep the existing method, whole-vector validation, promises and settlement owner.
Use EntityObservation scopes with allowed entity-version change during mutations;
use ReadProvider for actual pure inspection and existing CaptureSpace for selected
space. Wrap the method in a first-fault catch that rethrows the original exception.
Settlement may change its own optional finances but not the verified inventory,
entity, space or clock. No financial rule or Tycoon implementation changes.

Compute each fact's after amount from the exact already-verified native delta,
instead of calling Amount again during publication. These are transient verified
facts, not another balance or persistent ledger.

Reuse AtomicInventory's original ResourceInventoryState and file/master binding.
Explicit hostile commit metadata activates only while paused after initialization.
No saved probe payload change is required. Test actual Add/Remove and post-commit
inspection failures, clock changes, short/no-op commits and unrelated promises.

## 3. API surface

Unchanged IResourceTransaction/IInventory interfaces. New behavior enforces their
existing terminal contract for extension violations, including direct engine callers.

## 4. Files

- Standard/ResourceTransactions.cs: original commit implementation.
- ExtensionProof/AtomicInventoryProof.cs: original independent hostile provider.
- ExtensionProof/ConnectedResourcePortProof.cs: stateless OnLoaded session-context
  rebinding for those original components; no gameplay Update or copied inventory.
- ResourceLifecycleCommitTests.cs / Program.cs: literal kernel contract proof.
- SaveGame.cs / current-engine-protocol.json: build identity only.
- Master/README/AGENTS/evidence: current scoped status and all attempts.

## 5. Error handling

Normal typed refusals stay healthy. Provider violations and exceptions preserve the
first error and make partial state unsaveable. No success prefix, compensation or
substitution; terminal faulty providers are not retried.

## 6. Compatibility

Development-only; no legacy readers or shims. Native save/replay payloads unchanged.

## 7. Verification

New literal RED before fix, then targeted build and final new literal plus previous
direct-preflight and existing physical-lifecycle literals. External bounded watchdogs
only, no full/profile/family test run. Record every build/run including repairs.

## 8. Risks

Observation cost is not a performance claim. Arbitrary CLR behavior cannot be
sandboxed. No cross-session or unbound third-party object purity promise is inferred.

## 9. Out of scope

New stock/lot systems; unrelated transfer protocols; Oil/Tycoon/platforms/hosts;
full regression or Git publication. Original broad gates remain open.

## 10. Order

1. Add hostile direct-commit proof; demonstrate the actual gap.
2. Enforce original transaction commit/inspection/publication boundary.
3. Run bounded literal checks, review ownership and update authoritative tracker.
