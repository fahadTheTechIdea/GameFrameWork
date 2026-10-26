# P7-C12: Explicit optional paid-receipt composition

Status: implemented/focused-verified on unchanged2.611.0/save159/replay1.
Four exact finals282/0. Original P7.009 and subordinate C2b are now reconciled with
C11's participant matrix. [Evidence and limits](../../docs/migration/P7_OPTIONAL_PAID_RECEIPTS_VERIFICATION.md).

## Problem and acceptance

The original compound transaction gate remains open because PartialReceiptTests.Paid
requests IEconomy from the default physical-only survival colony. That composition
correctly omits money. Do not restore a mandatory account or free-payment fallback.

## Design

Author a separate JSON document listing the paid mode (money/trading only), native
account policy/currency and explicit creation supplier. List it in the existing
master under Tycoon, and select that module/mode/settings only in PaidFactory.
Use IBusinessAccounts and the same underlying native account owner. Keep authored
finite price7, creation price10, actual partial quantities3/7 and initial finite25.
Direct balance/receipt setup is diagnostic protocol instrumentation, not a public
player-workflow acceptance claim. Preserve the physical default and its lack of money.

Repair old postfault history assertions to require refusal and retain detached prior
facts. No new engine coordinator, codec, state, business rule or compatibility path.

## Verification

Run the existing paid/scoped/provider-fault literal, plus native partial receipts if
affected and the directly relevant explicit optional-business mode boundary case.
Use bounded exact names only. Reconcile original P7.009 against C11's actual matrix
and this missing evidence; do not close unrelated disposition/stress/quality gates.

## Stages

- [x] C12.1 Reproduce and inspect authoritative optional monetary contracts.
- [x] C12.2 Author explicit content/composition and migrate the sole proof in place.
- [x] C12.3 Focused verification and original tracker reconciliation.
