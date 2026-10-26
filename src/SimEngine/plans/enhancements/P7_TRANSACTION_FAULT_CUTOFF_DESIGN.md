# P7-C9: Complete resource transaction terminal-session cutoff

Status: implemented/focused-verified on2.602.0/save159; three distinct literals1219
checks. [All attempts and scope](../../docs/migration/P7_TRANSACTION_FAULT_CUTOFF_VERIFICATION.md).
Complete current interface cutoff is proven; original compound/full gates stay open.

## 0. Problem

The original IResourceTransaction has23 methods. Lifecycle preflight/commit now
refuse a stopped session, but claims, movement, transfer and conversion entry points
can validate arguments, inspect providers or even release/acquire receipts without
first checking the original World fault. An extension catching a terminal failure
must not use another method of the same owner to continue changing that session.

## 1. Goals

1. Every current IResourceTransaction entry point checks the original session's
   health before arguments, caller collections, content or endpoint callbacks.
2. Keep the first fault, existing stock/books, clock and API ownership unchanged.
3. Prove all interface methods reject default and hostile-collection inputs after
   a real provider fault, plus actual valid reads/releases/acquisitions.
4. Prove new/restored sessions, held-receipt integrity and unaffected live sessions;
   retain the existing atomic-haul and direct lifecycle literal checks.

## 2. Design

Use World.RequireHealthyUpdate at original public transaction entry points across
the same three partial-class files. No facade, secondary fault flag, transaction
implementation, compatibility wrapper or copied resource state. Existing internal
admission/commit algorithms and normal refusal semantics remain.

Reuse the existing AtomicInventory proof factory and stock setup helpers rather
than introducing another catalog/provider fixture. A real clock-changing commit
latches the original fault with valid inbound/outbound reservations already held.
The test enumerates IResourceTransaction methods so future additions are included.
Caller collection probes count/throw on access; they contain no simulation data.
Additional typed calls use actual existing receipts to prove stopped release and
acquisition cannot mutate books. Repeat after save/load, and genuinely advance
unaffected/reference sessions while the stopped session remains stopped.

## 3. API

No new production API. Document the uniform terminal-session rule on the existing
interface. Current23 methods are verified, not merely a named subset.

## 4. Files

- Core/ResourceTransactions.cs: existing interface contract documentation.
- Standard/ResourceTransactions.cs, ResourceTransferBatches.cs, ResourceConversions.cs:
  entry guards only, same owner and algorithms.
- ResourceLifecycleCommitTests.cs: share existing test factory/helpers internally.
- ResourceTransactionCutoffTests.cs / Program.cs: one literal boundary proof.
- SaveGame.cs / current protocol: build identity only; no state payload change.
- Tracker/evidence/README/AGENTS: current actual outcomes and open requirements.

## 5. Errors

A stopped session throws before even invalid-intent short circuits or caller list
access. The first WorldFault remains authoritative. No provider retry, receipt
release, success fact, game-time progress, healthy hash or checkpoint is allowed.
Healthy ordinary refusals remain typed and do not become terminal faults.

## 6. Compatibility

Development-only; no legacy behavior/shim. Save/replay layouts unchanged.

## 7. Verification

New literal RED against current engine; build, implement guards, then new literal
and existing exact atomic-haul/direct-commit cases. External bounded watchdogs only;
no full/profile/family run. Retain every attempt and classify evidence narrowly.

## 8. Risks

This enforces an already latched fault across the entire transaction interface.
It does not prove every provider violation in every transaction method is itself
detected/latched, nor provide a CLR sandbox or all-session isolation proof.

## 9. Out of scope

Business/Tycoon/Oil/platforms/hosts, new resource/media/ledger/clock, global gates,
performance/soak or publication. Original compound P7 requirements remain open.

## 10. Order

1. Reuse real faulty-provider setup and execute full-interface cutoff proof RED.
2. Guard every original entry before its first work; retain original algorithms.
3. Run selected literals and update evidence/master without broad gate closure.
