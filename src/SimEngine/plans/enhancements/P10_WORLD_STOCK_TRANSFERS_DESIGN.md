# P10-W1: World-owned network stock transfers — Design Document

> Superseded authority hypothesis, corrected by P10-TB1 on 2026-10-13: the pre-W1 ValidClaimCompany already accepted PlayerId.None. Ordinary Stock already permits world-to-world transfer under actual ownership checks. W1's claimed rejection of every ordinary None actor was incorrect; its negative fixture used a company-owned destination. WorldStock was redundant and has been deleted, without an alias. The sections below retain the historical design and scoped runtime record, not the current API. The shared line-effect dependency correction remains. See [current topology envelope design](P10_TOPOLOGY_MOVEMENT_ENVELOPE_DESIGN.md).

## 0. Problem Statement

NetworkService.Transfer submits ordinary company stock transfers even when redistributing pools whose actual owner is PlayerId.None. PrepareTransfers correctly rejects None as a company; public/unowned topology therefore cannot move its own conserved stock. Inventing a player would corrupt authority and history attribution.

## 1. Goals

1. Declare explicit WorldStock transfer kind, requiring None actor and actual world-owned source/destination.
2. Keep ordinary company and supply authority unchanged; world transfers cannot touch company-owned endpoints.
3. Reuse original transfer/version/receipt/journal/codec owners for split/merge of public networks.
4. Prove physical tile and non-tile public topology, conservation, saved continuation/replay and private world history boundaries.

## 2. Design

Extend ResourceTransferKind with WorldStock. Original PrepareTransfers accepts None only for that explicit kind; a company cannot claim world authority. EvaluateTransferLeg validates both actual endpoint companies are None before receipt callbacks, then uses the same stock transfer preflight/commit. Existing ResourceFlowKind.StockTransfer records actual None attribution; original journal already supports world partitions and observer-only reads. No world balance, fallback player or new journal schema.

NetworkService.Transfer chooses WorldStock for its actual world-owned pools, otherwise ordinary Stock. Both pools' actual ownership remains validated by the central transaction. Topology allocation/receipt refusal, physical graph construction/removal and the existing shared inventory/journal remain authoritative.

The runtime case also exposed native line-effect dependency dereference of intentionally absent segment/finish line references. One shared LineEffectContracts.ContentReferences supplies explicit empty cursor dependencies, mandatory actual removal line/layer/space and actual owner layer only when owned. Native and independent effects use that same helper; no missing removal reference is fabricated or tolerated.

## 3. API surface

One explicit enum value; existing transfer intent and methods unchanged. WorldStock requires None plus both physical endpoints world-owned. Ordinary Stock/Supply still require valid seats. Observer reads existing world partition; player-seat reads remain NotOwned. Build changes; physical save154/replay1/state/view schemas unchanged.

## 4. Files to change

Core transfer kind; original transfer shape/preflight/commit and native network dispatch; identity/protocol; new thin physical-world topology case/profile reusing existing authored network fixtures. Stage/evidence/master/audit updated.

Shared line-effect dependency declarations and independent forwarder; test content-authoring callbacks/explicit scenario selection and shared player wait use actual engine previews, with no copied travel/assignment rule. Proof diagnostics delegate read-only transaction/receipt inspection.

## 5. Error handling matrix

World kind with seated actor or company-owned endpoint refuses before mutation. Ordinary None remains invalid. Missing/foreign receipt/version/capacity rules unchanged. Dishonest provider or interrupted topology faults without healthy checkpoint/compensation. No resources are created or sold.

## 6. Backward compatibility

Development only; old builds rejected. No fallback actor, compatibility shim or duplicate transfer subsystem.

## 7. Verification

Completed focused compilation then one new bounded exact case, authored public/unowned tile and external point graph across minute/half time, physical split/reconnect, exact conserved stock, actual world journal attribution, saved twins/replay/private views. Retain failed attempts; full regression only at final implementation validation.

## 8. Risks

None is world ownership, not arbitrary invalid company. World transfer must validate both endpoint owners explicitly, not merely permit public access. Journal events must remain stock transfers, not stock purchases. Fixtures must author public/unowned topology rather than silently convert private/company data.

## 9. Out of scope

Full reservations/loss/spill/fault/global P10 matrix, Tycoon/Oil/platform/Draw/clock and Git writes.

## 10. Sub-task execution order

1. Declare and validate explicit world-owned authority centrally.
2. Connect actual world topology dispatch without fabricated actor.
3. Prove physical public graphs/accounting/durability/privacy using engine commands.
4. Record current evidence and tracker; compound gates remain open.

## Scoped stage outcome

Implemented and focused-runtime verified: FINAL165/0/0 across four public tile/point minute/half physical split/reconnect flows with exact conservation, saved twins/replay, world history/privacy and central authority refusals. Shared cursor/removal dependency bug corrected. Six clean focused builds, five failed same-case attempts and one refused patch retained. Full topology/resource/reservation/global gates remain open. See [verification evidence](../../docs/migration/P10_WORLD_STOCK_TRANSFERS_VERIFICATION.md).

