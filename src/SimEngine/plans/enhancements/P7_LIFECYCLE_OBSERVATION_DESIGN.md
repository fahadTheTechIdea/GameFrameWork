# P7-C7: Direct inventory lifecycle observation

Status: implemented and focused-verified on2.600.0/save159;369 final checks.
[All attempts and scope](../../docs/migration/P7_LIFECYCLE_OBSERVATION_VERIFICATION.md).
Original compound P7 gate and full goal remain open.

## 0. Problem statement

`ResourceTransactions.PrepareInventory` invokes `IInventory.Accepts`, `CapacityPolicy`,
`Available` and `CheckIncoming`, but checks only `Entity.Version`. Direct callers can
therefore receive a healthy result after a provider changes saved private state or
the sole game clock without touching that version. Higher-level work observation is
not a substitute for enforcing the original transaction contract itself.

This addresses part of the original P7.acceptance.017 provider/fault requirement;
it does not close its compound conversion/spill/topology/stress gate.

## 1. Goals

1. Both EvaluateInventory and CommitInventory use the same guarded preflight.
2. Observe the actual inventory, whole endpoint entity, selected space and clock
   with existing EntityObservation; no new owner, ledger or observation subsystem.
3. Fault before a stock mutation or success event; preserve the original callback
   exception and reject further calls after the first fault, including invalid requests.
4. Prove private-state, clock and thrown-callback violations in each inventory
   callback; retain healthy physical workflow behavior with literal focused tests.

## 2. Design

Keep input/identity checks and optional settlements in PrepareInventory. Run its
inventory requirement/capacity evaluation through EntityObservation.ReadProvider
with the actual inventory/entity, selected space and observeTargetState enabled.
Require a healthy session before any early result. Remove the weaker version-only
purity check; retain the returned version for original commit validation.

Reuse AtomicInventory's original ResourceInventoryState and master-listed proof
module. Add explicitly JSON-authored hostile lifecycle read probes, armed only by
pausing after initialization. A single saved probe marker demonstrates changes
without Entity.Touch; callback counters are transient diagnostics. Update only that
optional proof component's codec from 1 to 2. Native save format remains unchanged.

## 3. API surface

No new production contract or command. Existing IResourceTransaction semantics
are enforced at their original owner. Resource shortage remains NoStock; removal's
ActivePromises code is not a lifecycle shortage reason.

## 4. Files to change

- ResourceTransactions.cs: original preflight guard.
- AtomicInventoryProof.cs / ConnectedResourcePortProof.cs: reused hostile provider.
- ResourceLifecycleObservationTests.cs / Program.cs: exact contract proof.
- ResourceLifecycleTests.cs: migrate stale physical assertions to current WIP,
  earned requirements, owned receipts and terminal live-view boundary.
- SaveGame.cs / current-engine-protocol.json: current build identity only.
- Master tracker and evidence: actual scoped outcomes, no broad gate closure.

## 5. Error handling

Ordinary invalid intents and stock/capacity refusals remain typed, mutation-free
results. Violating/throwing callbacks latch the existing observation fault, return no
result and permit no healthy save/hash/continued transaction. No rollback or substitute.

## 6. Compatibility

Development-only; no shim or old codec reader. Native payloads are unchanged.

## 7. Verification

Build the test project once per executable revision. Run only the new literal case
and the existing literal timed lifecycle workflow case, each with an external timeout.
Record red/repair/final attempts. No full/profile/family run or Git publication.

## 8. Risks

Whole-entity/space observation adds preflight cost; no performance claim is made.
This is scoped provider verification, not a CLR sandbox or all-session purity proof.

## 9. Out of scope

Tycoon/Oil/platforms/rendering; new resources/quality systems; broad original gates.

## 10. Execution order

1. Add and execute focused negative proof against current preflight.
2. Enforce original observation boundary and build.
3. Execute literal fault and physical cases; document every attempt and remaining work.
