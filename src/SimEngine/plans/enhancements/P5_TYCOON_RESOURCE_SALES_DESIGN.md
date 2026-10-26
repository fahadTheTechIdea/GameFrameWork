# P5-A26 — Optional Tycoon resource sales and sole revenue ownership

## 0. Problem statement

Oil Operations.cs owns BuyerDef prices and CommerceSystem stock removal/credit; Companies.cs
owns financial revenue history. This duplicates financial ownership outside optional Tycoon.

## 1. Goals

Move both unchanged buyer price tables to selected Tycoon JSON. Sell only actual available owned
stock through original resource transactions. Store revenue only in existing BusinessAccounts.
Retain physical sale statistics, daily closing, first-sale notifications and normal operation gates.
Resource-only games keep stock and run without accounts or buyers.

## 2. Design

IResourceSaleDefinition binds actual inventory target, native-unit prices, eligibility, period/offset
and reason. IResourceSaleEligibility delegates domain checks, not monetary decisions.
A Tycoon schedule interpreter selects due targets in stable definition/entity order.
ResourceFlowKind.Sold and IResourceSaleSettlement integrate prepared account commits into the
existing ResourceTransactions inventory commit, not direct Take/Credit calls.
Production and sales ephemeral participants share IResourceInventoryCommit; their typed settlement
contracts remain separate. Sold is a distinct outgoing flow, not pollution/consumption or production
income. Resource-flow event6/view8, BusinessAccounts codec6, BusinessView10, Oil companies codec4/
CompanyView3 and core2.546.0/save156/replay1 explicitly identify the changed contracts/state.
BusinessAccounts keeps balance, total/current/previous reporting-window revenue and one selected
JSON reporting policy. Sale facts publish after successful physical/financial commit.
Immutable exact periodic cost/sales/report schedules are compiled once from selected content.
Not-due rules no longer perform needless per-minute observation; due eligibility/money/stock
checks remain live. This is a session-local derived schedule cache, not another clock or saved timer.
Oil subscribes only to physical quantities/first-sale presentation; financial views/campaign read
the actual nullable account sales snapshot. Existing clock/stock/project/machine ownership remains.

## 3. API surface

IResourceSaleDefinition, IResourceSaleEligibility, IResourceSaleSettlement/IResourceSaleCommit,
BusinessSalesSnapshot, IBusinessSales.ReadSales and immutable BusinessView sales policies/statistics.
No new host logic, account or fallback. Report-period/offset and sales-period/offset are authored.

## 4. Files

Framework Tycoon sale definitions/system and account partial, resource transaction/flow contracts,
existing journal/view/module/protocol. Oil Operations/Companies/Views/Campaign/Module and optional
business-sales.json/master, one focused public case. Tracker/audit/evidence.

## 5. Errors

Unknown references, duplicate targets, wrong inventory, invalid price/schedule/eligibility or missing
enabled account refuse before play. Unavailable stock/trading and settlement overflow refuse before
mutation. Changed or duplicate prepared commits fail-stop through existing engine fault semantics.
Foreign sales/account facts are cleared/refused. No quantity or payout invented.

## 6. Compatibility

No Buyer/Cash shim, legacy price reader, old financial state or save migration.

## 7. Verification

Compile affected assemblies and run one bounded exact public case using canonical Oil content.
Verify both authored tables, actual sale timing/payout/stock, saved twins, private facts, money-off
stock retention, disabled operation and invalid authoring. FINAL92/0/0 in261513ms/observer261643ms.
Three outer timeouts and one internal staged failure are retained in
[evidence](../../docs/migration/P5_TYCOON_RESOURCE_SALES_VERIFICATION.md). Full suites remain prohibited.

## 8. Risks

Prepared commits must preserve account identity and stock promises; original resource commit must
own all changes before sale events. Report boundaries must retain original sale-before-day-close.
Full fault/provider/durability acceptance remains open; narrow tests do not prove the whole plan.
The focused daily report test advances the actual24-hour boundary through ordinary Update.
It is divided into35-second staged windows with tick/state/progress diagnostics and a360-second
whole-case bound; no artificial shortened day, clock jump, instant sale or narrowed report assertion.

## 9. Out of scope

Markets/leases/auctions/advanced pricing changes, platforms/Draw/adapters, host implementation,
full-size/full-regression/old-client validation and Git publication before full goal completion.

## 10. Execution order

1. Add native optional definitions, eligibility and original-account revenue/settlement.
2. Integrate Sold disposition through original resource authority and generic scheduled sales.
3. Remove Oil buyer/financial history and subscribe to committed physical sale facts only.
4. Migrate unchanged JSON tables, compile and run only affected focused public proof.
5. Record all attempts and update tracker/audit; leave broad gates open.
