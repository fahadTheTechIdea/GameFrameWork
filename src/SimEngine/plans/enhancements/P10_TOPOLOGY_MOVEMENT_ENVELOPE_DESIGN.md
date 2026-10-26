# P10-TB1: Complete topology movement envelope — Design Document

## 0. Problem Statement

Topology currently reserves/transfers each redistribution leg separately. Network flow also retains a raw claim shortcut before calling the transaction owner. Later-leg failures can follow earlier successful legs. P10-W1 introduced redundant WorldStock: source inspection confirms ValidClaimCompany already supports None as the explicit world actor, and ordinary ownership checks enforce actual world ownership. Preserve W1 runtime results but correct its authority hypothesis and remove that redundant mode.

## 1. Goals

1. Topology submits all party/network/resource legs through the original complete movement owner.
2. Aggregate shared stock/inlet admission across parties before any acquisition; retain one full endpoint checkpoint through all parties' claims/transfers/releases.
3. Route network single-leg flow through the same movement owner, deleting raw reservation shortcut.
4. Keep None as existing world actor and actual ownership/privacy checks; no fabricated seat, duplicate kind, ledger or compatibility shim.
5. Prove multi-party shared stock refusal, deterministic complete commits, physical owned/world topology and late-leg faults.

## 2. Design

PrepareMovements returns an explicit owner-to-company mapping, rejecting ambiguous owner attribution. Capture all endpoints first, validate held records per actual party, prepare fresh claims by stable party groups, then run the same extracted CheckClaimVectors over complete outbound/inbound totals. Native PrepareClaims delegates that same vector helper.

CommitMovements acquires stable party groups against the original complete checkpoint, then calls original CommitCapturedTransfers once for all legs. Requested participant releases use their actual owner-party map against the same checkpoint; success facts still defer until all exact mutations verify.

NetworkService groups admitted topology quantities by actual source/destination, submits one complete envelope across all changes, then checks retired owners/allocations and geometry as before. Single-leg flow delegates CommitMovement; raw CanReserve/Reserve calls are deleted. None already represents world authority; remove WorldStock and its special-case shape checks. Ordinary actual source/destination ownership continues to forbid None-to-company transfers.

## 3. API surface

Public movement/transfer intent signatures unchanged. Mixed-party engine envelopes use each explicit Company; normal player command wrappers still enforce their one seat. No extra runtime model. Build changes; save154/replay1/journal/view layouts unchanged. Trusted proof-module system envelope instrumentation only, not new player/admin transport.

## 4. Files to change

Core kind consolidation, original movement/claims/transfer owner, native network topology/flow helper; existing proof diagnostics/codec/world owner fixture validation and old world test callers; identity/protocol; one new thin case/profile/new authored endpoint JSON. Stage/evidence/master/audit and W1 explanatory correction updated.

## 5. Error handling matrix

Ambiguous owner-party mapping or invalid company refuses before callbacks. Complete shared-stock/inlet overcommit refuses before any promise. Actual ownership remains required per party. Late dishonest callback faults without partial success history/checkpoint or compensation. Retired world/company pools must still empty exactly.

## 6. Backward compatibility

Development only; delete redundant WorldStock, migrate existing proof references. No legacy load/enum alias or duplicate acquisition algorithm.

## 7. Verification

One new bounded exact case after completed focused build: actual world/company mixed envelopes, aggregate refusal, saved/replayed commits, explicit releases and late peer faults; owned and public tile/point physical split/reconnect across minute/half time. Retain attempts; full regression only at final validation.

## 8. Risks

Shared public stock can be requested by multiple parties; independent per-party admission is insufficient, so native vector admission must be global. Held tokens cannot masquerade under another party. Topology may include several network/owner components; actor/order stability is required. World identity is None, not a fallback player.

## 9. Out of scope

Complete loss/spill/topology/provider/fault/stress/lockstep/global gates, Tycoon/Oil/platform/Draw/clock changes and Git writes.

## 10. Sub-task execution order

1. Consolidate world actor semantics and shared vector admission.
2. Add explicit mixed-party owner mapping, complete acquisition/transfer/release.
3. Delete network raw claim path and use one topology envelope.
4. Prove contracts/physical workflows/faults; record corrections/evidence without narrowing gates.

## Scoped stage outcome

Implemented and focused-runtime verified: FINAL 347/0/0 in 10370 ms, with eight owned/world tile/point minute/half physical workflows, separate joint-stock/inlet refusals, fresh/held mixed-party commits/releases, ordinary world Stock, saved twins/replay/private facts and late-peer fault checks. Intermediate 339-pass attempt retained; observed builds 10.09/1.39 seconds clean, intervening build observation unavailable. Full original topology/resource/reservation/global acceptance remains open. See [verification evidence](../../docs/migration/P10_TOPOLOGY_MOVEMENT_ENVELOPE_VERIFICATION.md).

