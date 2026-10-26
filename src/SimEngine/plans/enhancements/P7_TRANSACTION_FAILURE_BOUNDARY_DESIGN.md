# P7-C10: Original transaction failure ownership

Status: implemented/focused-verified on2.603.0/save159; four distinct literals1443
checks. [Every attempt and scope](../../docs/migration/P7_TRANSACTION_FAILURE_BOUNDARY_VERIFICATION.md).
Original compound requirements and full goal remain open.

## 0. Problem

P7-C9 stops every API after a latched fault, but a direct module caller can still
catch an exception from several claim/transfer/conversion methods before World has
latched it. A malformed provider may already have changed stock or promises. The
transaction owner, not an optional caller's Update wrapper, must make that terminal.

## 1. Goals

1. Every IResourceTransaction implementation owns exceptions from its execution,
   preserving the first fault and rethrowing the original exception.
2. Keep normal typed refusals and original algorithms, owner/books/clock unchanged.
3. Prove actual malformed providers in direct query/acquire/consume/release/movement/
   conversion calls cannot leave a healthy checkpoint after an irreversible prefix.
4. Keep terminal-cutoff and real atomic physical-haul cases green with focused gates.

## 2. Design

Wrap original public method bodies in ordinary try/catch after their existing health
guard. A private method on the same class owns fallback latching. No delegate-based
facade, hot-call closure allocation, renamed legacy implementation or new class.
Existing lifecycle commit's specific fallback phase remains; earlier observation
faults retain precedence. Never roll back resources or pretend a violating provider
is ordinary capacity pressure.

Reuse EndpointGame, original ResourceClaimTests seed helper and ClaimFaultOutlet
over ResourceInventoryState. Probes exercise mutating checks, extra promises, short
consumption/receipt and ignored release. New/restored sessions arm faults only after
actual healthy setup. Structural reflection verifies every current interface target
has an exception boundary, alongside behavioral cases; structure alone is not a
claim that every possible fault in every algorithm is tested.

## 3. API

Same IResourceTransaction; document direct-call failure responsibility. One private
fallback-latching method, no secondary fault state or compatibility surface.

## 4. Files

- Standard/ResourceTransactions.cs, ResourceTransferBatches.cs, ResourceConversions.cs:
  same algorithm bodies, explicit fault boundaries.
- Core/ResourceTransactions.cs: existing contract documentation.
- ResourceClaimTests.cs: share healthy seed helper internally.
- ResourceTransactionFailureTests.cs / Program.cs: one focused proof.
- ResourceTransactionCutoffTests.cs: share argument helper; all23 healthy invalid
  requests additionally prove typed refusals with no state/fact change.
- Current build identity, tracker and evidence; native save/replay payload unchanged.

## 5. Errors

Ordinary refusals remain healthy typed results. Unexpected callbacks/invariant
violations latch first fault and rethrow, publishing no unverified success prefix.
Partial physical state remains terminal, not refunded or checkpointed.

## 6. Compatibility

Development-only; no legacy reader, wrapper service or duplicate implementation.

## 7. Verification

New literal RED against current code, targeted build then new literal and existing
cutoff/atomic-haul/direct lifecycle literals. Bounded exact cases only, record all
attempts; no full/profile/family regression or Git publication.

## 8. Risks

Exception phase ownership becomes the actual transaction rather than a surrounding
system when no earlier fault exists. This is intentional, not compatibility emulation.
No arbitrary CLR sandbox, exhaustive provider-fault or allocation/soak claim.

## 9. Out of scope

Oil/Tycoon/hosts/platforms, new gameplay/resource models, compensation, broad gate closure.

## 10. Order

1. Exercise real violating direct providers before the fix.
2. Add allocation-free ordinary exception boundaries at the sole original owner.
3. Run focused literals and document actual scope/remaining original requirements.
