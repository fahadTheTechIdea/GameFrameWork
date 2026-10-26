# P7-GA1 — One observed resource-generation admission

Status: implemented and scoped verification complete; full P7.017/P8.017/goal open.
[All attempts, results, identities and limits](../../docs/migration/P7_GENERATION_ADMISSION_VERIFICATION.md).

## 0. Problem Statement

ResourceTransactions.PrepareGeneration calls the optional generation participant
without observing its own persistent state. Endpoint checks alone cannot detect
private participant state changes. Continuous Transformer.CheckFlow also calls that
participant directly instead of using the sole resource preflight owner.

## 1. Goals

1. Observe CheckGeneration/PrepareGeneration and refused-result validation through
   the existing EntityObservation provider mechanism before exposing any commit.
2. Put continuous generation preflight in EvaluateConversionClaims, with an explicit
   required cause ID and exact requested consumed/produced bills.
3. Delete the direct duplicate continuous callback and migrate the sole caller;
   no compatibility overload, new transaction path or business rules.
4. Prove callback faults/refusals publish no stock/receipt/history/commit prefix and
   preserve first cause, saved-session isolation and normal production semantics.

## 2. Design

ResourceTransactions remains the sole physical transaction/admission owner. Its
existing PrepareGeneration observes the actual optional participant's original
codec/clock/space, then returns verified refusal/ephemeral commit. No observer wraps
the legitimately mutating Commit as though it were a pure read.

EvaluateConversionClaims gains required Code cause, validates it and prepares the
same generation context from requested claim deltas/output bills after physical
admission. Transformer.CheckFlow supplies the actual transformation ID and handles
SettlementRefused's typed blocker; its direct settlement call/version-only check
is removed. No policy becomes mandatory when the optional participant is absent.

Add an independent JSON-authored diagnostic admission module in ExtensionProof,
with one original persistent marker/commit counter and transient negative hooks.
It has no currency, prices, accounts, sales, quotes, balances or business defaults.
Its sole purpose is proving the public optional participant contract, not another
resource ledger. Existing TransformerTests.Factory/master loading composes it.

## 3. API surface

IResourceTransaction.EvaluateConversionClaims now requires an explicit cause. The
old signature is removed, not retained as an overload. No save/content/view schema
changes. Engine build and expected identity advance2.631.0→2.632.0; save159/replay1.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | Core/ResourceTransactions.cs | Medium: exact contract migration |
| Modify | Standard/ResourceGeneration.cs | Medium: pure participant observation |
| Modify | Standard/ResourceConversions.cs | Medium: complete claims preflight |
| Modify | Standard/TransformerFlow.cs | Medium: remove duplicate direct admission |
| Modify | Kernel/SaveGame.cs + expected protocol JSON | Low: behavior identity |
| New | ExtensionProof generation probe + explicit JSON document | Low: independent diagnostic module |
| New/modify | focused test/registration/resource link/design/evidence/trackers | Medium: coverage |

## 5. Error handling matrix

| Input/callback | Required result |
|---|---|
| Participant absent | Physical resource operations remain available |
| Valid participant | Pure admission; one original participant commit on real generation |
| Authored typed refusal | SettlementRefused/blocker, no stock/state prefix |
| Silent persistent mutation | Existing provider fault before publication |
| Mutation then throw | Original exception identity/first cause retained |
| Empty/missing cause | InvalidRequest before participant callbacks |

## 6. Backward compatibility

Development-only; old API removed, no shim/fallback or old-build compatibility.

## 7. Verification

Targeted build/new exact callback proof baseline. After repair, targeted build and
new proof plus selected existing continuous physical flow and optional production
income acceptance, after inspecting scope. No full/family/profile or broad bundle.

## 8. Risks

Consumed claim deltas are not whole held receipts; aggregate the actual requested
quantities. Preserve physical endpoint observations around callbacks. Do not move
financial rules into core or treat a real mutating commit as pure admission.

## 9. Out of scope

Tycoon/business rules/amounts, all unrelated/unbound provider effects, full P7.017/
P8.017/P6 gates, Oil/platform/Draw/adapters and publication.

## 10. Sub-task execution order

1. Compose actual optional public-contract probe; retain current failing evidence.
2. Observe original helper and migrate complete claim preflight/continuous caller.
3. Verify selected exact cases; record all attempts and keep broad gates open.

Completed: sole observed generation helper, required-cause claim preflight and
continuous caller migration; independent JSON-authored proof; focused411+101+66=578
checks pass. Native coupled-store/continuous authored fixtures corrected without
relaxing physical capacity or content validation. No original acceptance gate closed.
