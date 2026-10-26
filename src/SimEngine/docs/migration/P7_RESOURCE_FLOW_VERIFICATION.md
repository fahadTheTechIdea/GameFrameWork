# P7-C3b1 — Committed resource-flow journal checkpoint

Date: 2026-10-04. Working-tree identity **beep-simengine-2.33.0 / save 15**.
Only standalone SimEngine and its independently compiled engine-side proof providers changed.
No oil/Tycoon, host/demo gameplay, renderer, Unity/Godot or adapter changes/runs.
The framework roadmap and P7 stage remain partial.

## Ownership and contracts

`InventoryModule` declares `ResourceFlowFact` / `sim.resource-flow` schema 1. The existing
`ResourceTransactions` publishes only after exact physical receipt consumption, quantities, stock
deltas and entity versions pass. `ISupplier.SupplyKind` explicitly distinguishes ExistingStock from
AuthoredCreation. `SupplyStock` references the finite supplier's existing same-entity outlet or is
null for authored creation. Finite purchases now verify actual source deltas centrally, not merely
the returned quantity/version/consumed receipt. Native quantities and company identities preflight
independently of whether diagnostics are selected.

Facts retain tick, requesting owner/company, resource/native unit, consumed endpoint receipt IDs,
actual stock before/after, transfer origin and actual endpoint versions. Authored creation reports
no invented source stock. Historical facts never authorize future mutation or own balances.

Optional `sim.resource-flows` requires `sim.inventory`. Its master-listed JSON declares exactly one
`resource_flow_journals` definition:

```json
{
  "resource_flow_journals": [
    { "id": "diagnostics.resource-flows", "retained_records_per_company": 2 }
  ]
}
```

The proof retention of two is authored diagnostics policy, not an engine/world/platform size limit.
Missing/ambiguous selected policies and nonpositive retention refuse. Omission of the module adds
no journal service, view or persistent payload. The master lists its own JSON document explicitly;
there is no auto-loader, fabricated runtime default or duplicate content registry.

One `ResourceFlowJournal` implements `IResourceFlowJournal` and persistent service schema 1. It
subscribes at the existing deterministic event phase, reusing `FeedRing<T>` for retention and the
same per-owner Sync for save/load/hash. Company partitions have independent sequences; reads never
create partitions. `ResourceFlowView` refuses foreign-company queries before lookup, returning no
facts or watermarks. An observer selects a company explicitly. Returned pages/entries are detached
and immutable; expired/future cursors are typed rather than silently claiming complete history.

Page totals are derived from retained records in native Q32 raw amounts using BigInteger. They count
physical transfer legs, not logical jobs. Stock purchases and authored creation are distinguished;
these are not financial totals or a second resource ledger. The page states its retention window.

`FeedRing.Sync` validates authored capacity, count/next sequence and contiguous chronological IDs,
rebuilding one canonical layout on restore. Sequence exhaustion refuses before mutation. Journal
Sync validates resource/unit/native quantity, company, tick, kind, deltas, versions and partition order.
General presentation feeds remain unpersisted unless their owning state codec explicitly calls Sync.

Events are deferred. World.Sync rejects pending events before saving/hashing, instead of losing a
successful transfer that has not reached its subscriber. Normal Update drains them. There is no
second synchronous journal path or persisted pending-event queue. A failed partial tick remains
non-resumable; earlier verified transfer legs are not erased when later work faults.

No core byte layout changed: format remains 15. Selected journal payload/schema joins the existing
module-owned service codec manifest. Build 2.33.0 breaks earlier semantics through the existing exact
save/peer identity check; no compatibility reader exists.

## Exact verification

Final build:

```powershell
dotnet build Beep.SimEngine.slnx
```

Exit 0, zero warnings/errors; MSBuild **1.86 s**.

Only these two full names were selected, one suite per invocation:

```powershell
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Resource flows: authored retention, private history and durable checkpoints'
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Resource flows: physical legs, supply origins and rejected provider facts'
```

| Exact suite | Checks | Failures | Aborts | Final time |
| --- | ---: | ---: | ---: | ---: |
| Authored retention / privacy / durability | 71 | 0 | 0 | 210 ms |
| Physical legs / supply origins / provider facts | 64 | 0 | 0 | 214 ms |
| Total | 135 | 0 | 0 | 424 ms |

All invocations in this slice used these same two names. Earlier transaction/haul/ownership suites
compiled but were not rerun as whole suites. No broad/full run. Focused proof covers:

- Preview/refusal/repeated receipt produce no facts; exact commit queues one event. Pending save/hash
  refuses; Update records it. Actual source/destination balances, consumed receipts, unit and versions
  match the fact. Retained immutable pages do not change after ring wrap.
- JSON retention, explicit expired/future cursors, independent company histories and private refusal
  with zero leaked sequence/count. Queries preserve the hash. Optional omission has no hidden journal.
- Save/load/twin continuation before pickup, during actual loaded travel, and after delivery. Pickup
  and delivery are two correctly identified physical legs at different game ticks, with real cargo.
- Ring restore/canonical layout, capacity mismatch, corrupt count/sequence and sequence exhaustion.
  Journal restore rejects negative/future fact ticks, invalid company and substituted native unit.
- Finite paid supply: pure cash refusal then actual stock 25→10 / quantity 15 / payment 105.
  Authored supply reports creation into actual destination stock without a fictitious source balance.
- Six independently authored port violations add no success record for the failed leg. A prior verified
  pickup survives a later admission fault. Malformed event payload faults before a partition is created.
- An independently authored finite supplier consumes its receipt, advances its version and charges,
  but deliberately omits stock removal. Central source-delta enforcement faults that physical tick;
  no successful fact, fabricated compensation or resumable partial save is produced.

Physical waits are bounded (30/200 ticks for point hauling, 2,880 for paid-provider failure), with
stage/tick/fault diagnostics. Host/test actions submit/read/advance or instrument contracts; executable
provider behavior lives in the engine-side ExtensionProof assembly using public framework contracts.

## Remaining work

- C3b2 full lifecycle accounting: initialization, consumption, destruction, transformations, spill/loss,
  network topology changes and their later P4/P8/P10/P12 owners. This journal covers committed reserved
  transfer legs only, not every unreserved Add/Remove or every resource mutation in the engine.
- P4/P8/P9 batch/graph/tool/dock participation, broad hostile-provider permutations, stranded-cargo
  accounting and capacity/logistics stress/deadlock/allocation/soak proofs.
- Complete operational-knowledge privacy outside this company-scoped journal. Engine-side providers
  are trusted contracts, not a sandbox against arbitrary CLR code or fabricated valid event payloads.
- Later stages, oil extension migration and the authorized full regression/stage gate.

The development-lifecycle skill guided the staged execution contract, sole-owner implementation and
source-specific evidence. The user's exact-test instruction overrides its broad/full regression step.
