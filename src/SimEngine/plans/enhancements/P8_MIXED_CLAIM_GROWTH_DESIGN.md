# P8-D3b1 — Whole-set acquisition of fresh and growing resource claims

Status: focused implementation/verification complete; prerequisite to continuous catalyst replenishment,
not full P8. Core2.311.0/save148; native codecs/views/clock unchanged. Two builds (new-test import
repair), final clean3.81s. ONE NEW exact case FIRST231/0/0 in295ms, PID7436 exited0, observed436ms,
untruncated/no observer error. Inputs frozen18:57:20 UTC; older cases compile only, not rerun.
[Observed evidence](../../docs/migration/P8_MIXED_CLAIM_GROWTH_VERIFICATION.md).

## 0. Problem statement

`Standard/ResourceTransactions.AcquireClaims` admits fresh claims together, while `ExtendClaim`
independently admits and grows one original receipt. A continuous processor must replenish its
original catalyst charge together with feed/utilities. Sequential calls would allow a ready prefix
to grow while another source is unavailable. D3a already supports bounded consumption retaining
the original token; this stage supplies the missing complete acquisition operation.

## 1. Goals

1. One existing claim intent explicitly describes either fresh acquisition or increase of one exact
   actual outbound receipt. No implicit merge, replacement token, copied book or source fallback.
2. All request shapes, endpoints, exact original receipts, native packing and aggregate free stock/
   inbound capacity are admitted before any acquisition or increase mutates actual state.
3. The existing `ExtendClaim` delegates to this same whole-set acquisition owner.
4. Returned increase receipts carry the same original token and full updated protected amount.
5. Public preview/Update, saved pending commands, interleaved sessions and replay preserve this
   operation. Full P8 continuous wear remains required in the next execution stage.

## 2. Design

Extend `Core/ResourceTransactions.ResourceClaimIntent` with explicit `ResourceClaimKind`,
`OriginalReceipt`, and `ResourceClaimIntent(ResourceReservation receipt, Bill quantities)`.
Fresh constructor remains the fresh operation, not a legacy compatibility alias. Increase has one
positive additional quantity matching the original resource. A malformed original never becomes fresh.

`PrepareClaims` first validates detached shapes and duplicate endpoint/owner/direction/resource
demands. It captures the complete endpoint set before admission callbacks, validates original full
receipts with `PrepareClaimReceipts`, then checks additions through the existing aggregate vectors.
Fresh demands cannot overwrite existing same-owner/resource claims. Explicit increases cannot
duplicate an original receipt, grow inbound capacity, fabricate ownership/amount, or overflow.

`AcquireClaims` applies canonically sorted fresh `ReserveOut`/`ReserveIn` and original
`IncreaseReservedOut`, updates its ephemeral captured records and checks every participant after
each operation. No rollback fiction: a violating provider faults and cannot publish a checkpoint.
`ExtendClaim` is a one-intent delegation, removing its separate admission/mutation implementation.

## 3. API surface

No host API or platform code. Existing `IResourceTransaction.EvaluateClaims/AcquireClaims` accept
the revised intent. Existing detached `ResourceClaimResult.Receipts` returns exact full current
records. Native endpoint books remain the only authority. Outlet/inlet inheritance is unchanged.

## 4. Files to change

| Action | File | Approximate lines | Risk |
| --- | --- | --- | --- |
| Modify | src/Beep.SimEngine/Core/ResourceTransactions.cs | 20 | medium |
| Modify | src/Beep.SimEngine/Standard/ResourceTransactions.cs | 90 | high |
| Modify | tests/ExtensionProof/ClaimProbe.cs | 30 | medium |
| Modify | src/Beep.SimEngine/Kernel/SaveGame.cs | 2 | medium |
| Modify | tests/Beep.SimEngine.Tests/testdata/current-engine-protocol.json | 2 | medium |
| New | tests/Beep.SimEngine.Tests/MixedClaimGrowthTests.cs | 180 | medium |
| Modify | tests/Beep.SimEngine.Tests/Program.cs | 1 | low |

## 5. Error handling

Malformed/duplicate/multi-resource increase: typed InvalidRequest. Wrong/stale/forged/inbound
original: InvalidReceipt. Missing/foreign endpoint: existing typed access refusal. Insufficient
aggregate stock/inlet capacity: NoStock/NoCapacity with no changed claim prefix. Invalid native
quantum/range: Incompatible/InvalidRequest. Provider mutation during any pure callback or inaccurate
actual increase: existing native fault boundary, no successful save/history.

## 6. Backward compatibility

Development-only direct migration; no old payload reader or compatibility path. Build/save identity
changes for the pending proof command format. Native resource/processor state layouts stay unchanged.

## 7. Verification

Build for compilation only, then ONE NEW exact bounded public-player case for mixed fresh/increase
requests, contention/aggregate shortage, stale/full metadata, native units, privacy, preserved token,
pending-command saves, partial consumption/replenishment, immutable facts and canonical replay.
No old/family/full/Oil/platform runtime without renewed human authority.

## 8. Risks

Capture all participants before callbacks to detect cross-endpoint mutation. Require explicit kind
so invalid receipts never silently become fresh. Validate full original amount, not just the requested
addition. Additions aggregate with fresh demands and existing other-owner holds.

## 9. Out of scope

This acquisition prerequisite does not claim continuous authored rate wear, entity tools, scoped
physical supply/export, all provider stress or full P8/P1–P12 completion. Those remain required.

## 10. Execution order

1. Revise the existing intent and sole acquisition implementation; remove separate growth algorithm.
2. Directly migrate saved proof commands and current independent protocol identity.
3. Add the thin player proof using existing master-listed resources and engine endpoint module.
4. Compile, run only the new exact case, record all attempts and update the authoritative tracker.
5. Continue continuous catalyst authoring/execution through this path; no independent replenishment owner.
