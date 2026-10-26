# P7-C4b — Bounded native receipt consumption

Status: focused-verified, 2026-10-05. **167 checks / two exact suites** on build2.63.0/save35;
zero warnings/errors/failures/aborts. Parent: [P7](P7_RESOURCES_DESIGN.md).
[Evidence](../../docs/migration/P7_BOUNDED_RECEIPTS_VERIFICATION.md). Historical native prerequisite
gate only; progressive handling/partial phases were then open. The follow-on
[P9-C4c](P9_PROGRESSIVE_HANDLING_DESIGN.md) now implements those same owners on build2.64.0/save36.
Only C4c's two suites ran this turn; the original native report remains historical. No broad/full/third suite ran.

## 0. Problem

Before this stage, `ResourceTransactions.Evaluate` required both receipt quantities to equal the
transfer quantity; `ReceiveReserved`, `TakeReserved` and `SupplyReserved` consumed the entire record. Timed
partial loading cannot legitimately move a smaller quantity while retaining its unspent commitment.
Adding a second cargo/promise ledger would violate the engine's one-owner design.

## 1. Goals

1. The existing reservation book consumes an explicit positive bounded quantity, retaining the same
   token/owner/resource/direction and exact remaining quantity until final consumption removes it.
2. Existing inlet/outlet/supplier primitive contracts require that explicit quantity. Old signatures
   are removed, not wrapped. Whole-set work consumption passes its actual full receipt quantity.
3. The existing transaction coordinator supports bounded unequal remaining source/inlet receipts;
   the transfer must fit both and satisfy the common native endpoint quantum. No retained preview
   authorizes subsequent chunks; every commit rechecks current state.
4. Generalize the current ephemeral claim-endpoint checkpoint for both claims and transfers. Verify
   all endpoint stock, remaining records, unrelated claims/resources, identity and versions across
   each callback. No additional persistent ledger or compensation path.
5. Suppliers charge only actual transferred quantity. Finite supply decreases existing stock; explicit
   authored creation stays a distinct origin. Failures spend no stock/money/receipts when preflight
   refuses; provider violations fault the normal Update and cannot checkpoint.
6. Current books/Sync and ResourceFlowFact own saved remainders and actual chunk history; no new
   event/feed/state store. Saved continuation, private facts and interleaved sessions stay deterministic.

## 2. Design

`Core/ResourceReservations.cs`: `ResourceReservationBook.Consume(endpoint, owner, token, direction,
amount)` is the sole authoritative remaining-record mutation. It removes only a fully consumed
record, otherwise replaces the immutable row in place without allocating a sequence or new claim.

`Core/ResourceEndpoints.cs` and `Standard/Capabilities.cs`: require `Fix amount` in the current
reserved mutation primitives and supplier payment check. `ResourceInventoryState` and independent
public-only providers validate native/capacity quantum and bounds before stock/record mutation.
Storage delegates to its same embedded owner. All old callers migrate to the explicit contract.

`Standard/ResourceTransactions.cs`: admission accepts `amount <= both remaining receipts`, verifies
the reported full records against actual books, checks source/destination native quantum, and uses
the generalized existing endpoint checkpoint to preflight and verify all exact expected deltas.
Commit calls explicit bounded primitives, changes transient expected stock/remaining rows, then
checks both endpoints after each mutation before publishing the existing flow fact. Claim-set
consumption reuses that same checkpoint and passes its exact remaining receipt amount.

The native record layout is unchanged: Amount always describes the outstanding promise. Build/save
identity advances without an old reader. Storage, work, flow/view schemas remain unchanged because
no new state/view fields are introduced. This stage supplies the native protocol prerequisite;
P9 still needs authored progressive transfer behavior and actual partial job-phase migration, not
just a timer percentage or a second stock model. Existing timed hauls still commit authored whole batches.

## 3. API surface

- `ResourceReservationBook.Consume(..., Fix amount)`.
- `IResourceInlet.ReceiveReserved(..., Fix amount)`.
- `IResourceOutlet.TakeReserved(..., Fix amount)`.
- `ISupplier.CheckSupply(..., Fix amount)` and `SupplyReserved(..., Fix amount)`.
- Existing `ResourceTransferIntent.Amount` now selects a bounded native transfer; no second intent API.
- Existing immutable reservation/history views report real remaining/actual quantities.

## 4. Files

Core/ResourceEndpoints, ResourceReservations; Standard/ResourceInventoryState, Inventory,
Capabilities, Logistics, ResourceTransactions; current public-only endpoint/supplier/fault proofs;
existing direct-call tests (compile migration only), new exact partial receipt suites/registration;
SaveGame identity; stage/evidence/master/P7/P9/P4/README/AGENTS updates.

## 5. Error handling

Zero/negative/over-remaining/wrong owner/wrong endpoint/non-native amounts refuse before mutation.
Released/missing receipts are typed refusals. Mutated/fabricated preflight, wrong actual delta, changed
unrelated stock/claims or unadvanced versions fault without rollback or fabricated success history.
The same token may fund successive bounded commits while remaining positive; it is invalid after
full consumption. Tokens are requester references, not exactly-once command IDs; lockstep/replay
ordering remains the existing command boundary's responsibility.

## 6. Compatibility

Development only. No overloads retaining old primitives, default quantities, old save reader or shim.

## 7. Verification

Clean solution build; only two directly affected exact suites:

```powershell
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Partial receipts: exact native remainders, refusal and saved identity"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Partial receipts: paid sources, scoped endpoints and provider faults"
```

No broad/full/third suite. Compile migrated callers, do not reuse historical suite counts as current.

## 8. Risks

Partial native quantities must honor endpoint packing quantum, not only resource units. Callback
failure may leave changed state; normal Update faults and refuses save/hash rather than inventing
compensation. Full-vector transient checks cost more than one-resource checks; allocation/soak gates
remain open. Live work/haul owners must update their expected receipt amounts before using partial
commit; this native stage does not silently alter those workflow semantics.

## 9. Out of scope

P9 progressive load/unload and partial cargo phase migration, fleet/plans/areas, dock/route collision,
all supplier/economy/provider fault matrices, multi-leg transform batches, full regression, Oil/Tycoon/
platform/host/demo/Draw.

## 10. Execution order

- [x] C4b.1 explicit bounded book/endpoint/supplier contracts and all standalone direct caller migration.
- [x] C4b.2 one generalized native checkpoint and current-state partial transaction admission/commit.
- [x] C4b.3 two exact native/supplier/scoped/fault suites with saved continuation (78/89).
- [x] C4b.4 scoped evidence and linked tracker/instruction updates; keep progressive P9 gate open.
