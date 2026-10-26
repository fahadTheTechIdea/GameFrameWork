# P8-D2a — Complete multi-port movement admission

## Problem and goals

`Transformer.StartWip` and `ReturnWip` currently move one bill between two stores. The native
transaction's movement entry point likewise accepts one directed bill. Calling it repeatedly
would move a prefix before a later input or return vessel refuses. P8 mixed input/tool/utility
authoring must use one atomic envelope, not a second coordinator or compensating transfer.

This prerequisite extends the existing `IResourceTransaction` and `ResourceTransactions` owner:

1. Admit every source, existing receipt and destination capacity together before acquiring claims.
2. Aggregate common source demands and inlet vectors at their actual endpoint books.
3. Support converging/split same-resource movements with bounded uses of one owned receipt.
4. Refuse shortages, missing/private endpoints, aggregate overdraw/overflow and stale records without
   changing stock, promises, sequences, history or clock. Provider contract violations remain faults.
5. Canonicalize through existing claim/transfer ordering; saved ordinary commands use the same owner.

## Contracts and implementation

Add `EvaluateMovements(IReadOnlyList<ResourceMovementIntent>)` and `CommitMovements(...)` to the
existing public transaction contract. Single `EvaluateMovement`/`CommitMovement` calls delegate to
this same implementation. No separate services, loops, ledgers, clocks or persistent state.

One envelope names one company, positive native bills and explicit source/destination/owner identities.
Repeated owner/source/destination/resource rows refuse: combine those quantities in the bill.
Shared source or inlet claims normalize by direction/owner/endpoint, then use the existing complete
claim admission/acquisition. Existing outbound references are checked against actual books; aggregate
spending cannot exceed the referenced record. Transfer preparation allows one receipt to participate
in distinct routes only within its total held quantity. Duplicate receipt pairs or opposite-direction
reuse refuse. Exact provider mutations and success facts remain owned by `CommitTransfers`.

Only the existing engine-side `TransferProbeCommand` is extended to hold a movement array; it delegates
Check/Apply to this public protocol. Its old singular field is removed in-place and consumers migrated.
Tests issue player requests and inspect existing immutable views, not live `World` or stock writes.

## Files and errors

- Core/ResourceTransactions.cs — public whole-movement-set contract.
- Standard/ResourceTransferBatches.cs — sole preparation/acquisition/commit implementation.
- Kernel/SaveGame.cs — breaking simulation build identity; existing state/save codecs unchanged.
- ExtensionProof/ClaimProbe.cs — actual command array serialization and protocol delegation.
- Existing movement command callers — direct migration, no compatibility wrapper/parser.
- New exact movement test — mixed bills, convergence, split, refusal, saved continuation and privacy.

Malformed/overflow/duplicate route: InvalidRequest; stale/mismatched/overused receipt: InvalidReceipt;
missing endpoint: MissingEndpoint; incompatible resource/quantum: Incompatible; aggregate shortages:
NoStock or NoCapacity. Foreign endpoint: InvalidRequest. A provider mutation faults normal Update;
no rollback fiction, success history, save or hash of partially committed state.

## Verification and remaining execution

Build with `dotnet build Beep.SimEngine.slnx --no-restore`. Run only the NEW exact bounded case recorded
in this stage's verification document; no earlier/family/full/Oil/platform suite without authority.
Review complete-set admission and native claim/transfer reuse; update the master tracker and audit.

Focused verification:116 checks pass in ONE new exact bounded case after an initial97-check green
run and deliberate coverage extension. Two clean builds;2.306.0/save144/replay1/view10, no new state.
[Observed evidence](../../docs/migration/P8_MULTIPORT_MOVEMENT_VERIFICATION.md).

Next, migrate native authored input/tool/utility bindings and cancellation to this whole-envelope
owner. This prerequisite alone does NOT implement those routes, catalyst behavior, dynamic port
recovery, complete accounting, wider provider proofs or full P8/P1–P12 completion.
