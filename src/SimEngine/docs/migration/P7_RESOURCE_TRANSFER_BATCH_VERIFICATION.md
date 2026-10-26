# P7-C5a / P8-A2a — Owned native transfer-set verification

Date: 2026-10-05. Standalone Beep.SimEngine only. This prerequisite is complete within its stated
scope; recoverable transformer WIP, atomic process conversion/results and the full roadmap are not.

## Identities and one owner

Engine beep-simengine-2.76.0/save46. Existing resource, inventory, endpoint, work, logistics,
transformation and event/view codecs are unchanged. Build identity rejects prior semantics without
a compatibility reader. The public-only proof's existing transfer-probe command now authors a set
directly; its singular payload is deleted, not wrapped or retained as a legacy path.

IResourceTransaction/ResourceTransactions alone own EvaluateTransfers/CommitTransfers. The new
ResourceTransferBatches.cs is another file of that same partial class, not another service/queue or
codec. Single Evaluate/Commit enter the same whole-set admission and mutation implementation.
Actual ResourceInventoryState/ResourceReservationBook providers alone hold stock and promises;
temporary shared ClaimEndpoint objects contain exact verification checkpoints only.

## Executed gates

Working directory: `C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine`.

`dotnet build Beep.SimEngine.slnx`: zero warnings/errors, final source build6.49s.

`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Resource transfer batches: whole-set admission, native ownership and canonical commits"`

**166 passed**, zero failures/aborts; one suite,444ms.

`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Resource transfer batches: saved continuation, shared endpoints and provider fault boundaries"`

**119 passed**, zero failures/aborts; one suite,637ms.

Total **285 checks/exactly two suite names**. Repair iterations repeated only those names. Initial
test fixtures were corrected to respect the existing one-owner/resource/direction reservation rule
and use the actual request owner when releasing a receipt; the endpoint ownership rule was not
weakened. Directly affected pre-existing native quantum/fractional and paid/creation single-leg cases
execute inside these gates; their full suites did not run. Other suites, including Transformers and
automation, compile only. Current build identity literals migrated mechanically; no full/family run.

## Focused proof

- Pure set admission is detached; null/empty/malformed/invalid-kind requests refuse before stock or
  claims change. Duplicate source/destination tokens refuse; same-token fragments are not implicit.
- Mixed discrete steel/fractional water, common native packing quantum, entity/scoped endpoints and
  positive64-bit identities retain their authored units. No mixed-unit aggregate result is invented.
- Exact source/destination claim ownership, company access, private foreign endpoints, missing
  endpoints, incompatible resource identity, overspending and invalid native quantities refuse.
- Whole-set refusal preserves actual stock, native books, versions, token sequences and history,
  including a removed later receipt after a previously ready preview and current provider capacity/
  stock blockers. Earlier valid legs do not commit on these ordinary refusals.
- Existing inbound promises protect aggregate measured capacity; this protocol acquires no claims
  or future capacity. The two shared scoped destinations resolve once per set, not once per leg.
- Canonical endpoint/resource/receipt ordering produces identical native state and event history
  with shuffled caller order. Partial source/inlet tokens retain real remainders/unrelated promises.
- Shared intermediate endpoints have one verification checkpoint across both roles, start with
  their own protected outbound stock and record their then-current actual quantities in history.
- Source and destination stock conserve each native resource separately. Facts are StockTransfer,
  not fake creation/consumption. Company-private journal views retain their existing ownership checks.
- Pending ordinary set commands, partial continuation and settled outputs save/load through existing
  codecs; interleaved twins retain identical hashes. Independent sessions remain unchanged.
- Single-leg API and set API give identical owned receipt changes, native amounts and event history.
  Existing finite-paid and explicitly authored-creation sources pass their actual partial payment,
  refusal, receipt-remainder and saved continuation checks through the common protocol.
- Ten provider fault modes run through ordinary Game.Update: mutating own/cross-endpoint pure
  checks, short take/receive, erased remainders, unrelated stock/receipt mutation, silent versions and
  mutation of a prior participating endpoint. Exact whole-set checkpoints fault the session; no
  earlier-leg success facts or accepted command completion are published. Save/hash are forbidden.
- Faulted state retains real earlier mutations. This is not a claim of rollback/ACID compensation;
  malicious mutation cannot be represented as a resumable successful batch.

## Boundaries still open

The native Transformer still consumes startup inputs; its cancellation does not recover WIP yet.
This slice must not be advertised as solving that behavior. Atomic combined inbound/outbound claim
acquisition, real WIP endpoints/execution identities/codec, authored cancellation/scrap disposition,
catalysts/tools, process resource conversion plus entity result commitment, distinct ports,
continuous accounting and utility semantics remain required. Multiple paid/creation Supply legs
explicitly refuse until aggregate payment/creation admission exists; single Supply remains supported.

The protocol does not supply a carrier, travel, handling time or player shortcut. Existing physical
owners call it only through their own authoritative execution gates. No map/world-size restriction,
parallel stock/reservation/transaction authority, legacy reader, host/demo/harness game logic,
Oil/Tycoon/old sibling or platform/Draw implementation was added. Broad hostile-provider matrices,
allocation/fuzz/replay/lockstep/stress/soak/full regression and P1–P12 acceptance remain open.

The lifecycle skill guided the stage-first design, shared owner and focused evidence/reporting.
