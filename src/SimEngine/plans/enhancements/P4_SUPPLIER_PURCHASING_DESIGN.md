# P4-I3 — One optional supplier-purchasing authority

Current checkpoint2.214.0/save118: selected ordinary work costs now acquire account-owned whole-set
holds, spend at actual productive progress and release unpaid holds through the existing lifecycle.
The SAME optional Tycoon account owns budgets, purchasing and income; no second monetary authority.
Saved/private diagnostics use account4/board41/work41/work view74/business view5. Final clean build
2.33s; ONE exact new scenario64 checks0 failures/aborts494ms. Three builds/three same-scenario
invocations including fixture ordering/compile repairs and an accidental stale-binary repeat are
recorded in [current evidence](../../docs/migration/P4_WORK_BUDGET_VERIFICATION.md).
[Budget design](P4_WORK_BUDGET_DESIGN.md). No older/full/family run. Broader provider/debt/range/
interruption/replay/stress/advanced acceptance remains open. One unchanged GameClock; no Draw,
platform, host/demo or OilFieldDays change. Checkpoint paragraphs below are historical, not current
runtime verification.

Current physical purchasing2.213.0/save117: native/finite paid trips use existing stock-policy
dispatch, movement, earned load/unload, same account settlement and stable preview/live wait codes.
Actual manufacture consumes travel-time funds; real production income funds the unpaid remainder.
Paid cargo survives payment/route waits and saved continuations. Resource-only hauling omits Tycoon.
One clean build5.66s and ONE exact new scenario98 checks0 failures/aborts484ms, first attempt.
[Physical design](P4_PURCHASE_HAUL_DESIGN.md),
[current evidence](../../docs/migration/P4_PURCHASE_HAUL_VERIFICATION.md). Older gates are historical;
persistent budgets, mixed legs, endpoint/fuel/dock/provider/replay/seller/advanced acceptance stay open.

Historical status: native synchronous settlement focused-verified, 2026-10-07; broader acceptance open.
Build2.212.0/save116/account3/business view4/supplier4.73 checks0 failures/aborts273ms;
final clean build0 warnings/errors2.77s. Five builds/three invocations of the SAME new scenario,
including two new compile repairs and two fixture startup repairs before final73.
[Evidence](../../docs/migration/P4_SUPPLIER_PURCHASING_VERIFICATION.md). Older suites compile only.
This extends existing P4-I3/H1/H2,
not a second economic module or a new game loop. Parent: [master tracker](MASTER_TRACKER.md).

## 0. Problem statement

Hauling and receipt transfer are caller entry points, not separate purchasing systems.
Previously `Logistics.Preview`, `ResourceTransactions.EvaluateTransferLeg` and supplier
callbacks called concrete `BusinessModeRules`; native and proof suppliers charged money
themselves. `PrepareTransfers` rejected multi-leg supply rather than admitting a complete
purchase. The desired boundary is one optional Tycoon-owned economic settlement with
the existing physical transaction owner retaining all stock, receipts and facts.

## 1. Goals

1. Physical systems depend on optional contracts, never interpret native economic flags.
2. One Tycoon account instance owns balances and complete-set purchase settlement.
3. Complete current-price/current-money admission precedes physical mutation. Individually
   affordable but collectively unaffordable purchases refuse without partial stock or payment.
4. A purchase debits once only after every actual physical receipt/stock delta verifies.
5. Raw supplier fulfillment requires a transaction-created synchronous scope; it is not a
   second purchase API. Partial receipts retain exact original tokens and unspent amounts.
6. Accounts-only, dormant and omitted economics retain normal physical work/transport.

## 2. Design

`Core/ResourcePurchasing.cs` declares `IResourceTradingRules`, `IResourceSupplyPrice`,
`ResourcePurchaseIntent`, `IResourcePurchaseSettlement`, `IResourcePurchaseCommit`
and the nonconstructible, transient `ResourceSupplyExecution` callback scope.
Existing JSON `supplier.sells` prices are denominated in the explicitly selected account
definition's currency; there is no guessed currency, resource-valuation fallback or foreign
exchange. `IResourceSupplyPrice` publishes only authored native-unit price, not balances.

`BusinessModeRules` implements the pure trading contract; immutable mode configuration
remains the only flag authority. `BusinessPurchasing.cs` is another file of the SAME
`BusinessAccounts` partial class. It validates/quotes all supply legs, aggregates monetary
charges per actual buyer, guards the account vector against impure pricing, checks range
and affordability, and returns a same-call-only commit. The commit checks every captured
balance before writing any debit and rejects stale or repeated use. No account, purchase
ledger, queue, codec, clock or system is added.

`TycoonModule.Compose` aliases the trading contract to the existing rules and, only when
trading is enabled, aliases settlement to actual accounts. It no longer rejects fixed-price
supplier trading. Advanced Tycoon activation still refuses; this slice introduces neither
markets nor seller accounts/revenue. Native supplier quantities/prices remain JSON-authored.

`ResourceTransactions` remains the one physical transfer owner. Pure optional trading
admission is shared with hauling. After all exact receipts/compatibility validate, it admits
the entire supply vector through the settlement contract. Commit repeats current admission,
creates one transient exact callback scope, verifies all real supplier/inlet mutations,
closes the scope, commits payment, checks endpoints, then publishes existing resource facts.
Mixed stock/supply legs share this protocol; ordinary stock legs never invoke settlement.
Any provider violation after irreversible physical mutation faults; no fabricated rollback.

`ISupplier.CheckSupply` is physical-only. `SupplyReserved` requires the admitted scope and
contains no charge/account lookup. Native `Supplier` and public-only `FiniteSupplier`
implement the price contract. Haul preview checks the same settlement on the actual
quantized candidate load; actual loading repeats transaction admission, never trusts preview.
Money is NOT reserved across travel yet; changing funds while a vehicle travels must use
existing typed refusal/retry. Long-lived budget reservations remain H2 work.

## 3. API surface

New optional contracts above; required scope parameter replaces the old raw fulfillment
signature directly, with no legacy overload. `BusinessView.HasPurchasing` reports installed
capability; absence does not masquerade as zero balance. No host market or rendering API.

## 4. Files to change

Core/ResourcePurchasing.cs; Standard/Capabilities.cs; Standard/ResourceTransactions.cs;
Standard/ResourceTransferBatches.cs; Standard/Logistics.cs; Standard/Tycoon/{BusinessModeRules,
BusinessPurchasing,Suppliers,TycoonModule}.cs; Views/BusinessView.cs; Kernel/SaveGame.cs;
public-only proof supplier and authored purchasing fixtures; one new bounded runner scenario.

## 5. Error handling

Off trading: TradingDisabled. Absent module/settlement/pricing: UnavailableCapability.
Insufficient aggregate money: NoPayment. Unrepresentable/under-quantum price: SettlementRefused.
Bad receipt/amount/neutral buyer: typed ordinary refusal. Negative/mutating price, missing scope,
stale/repeated prepared payment, false physical success: deterministic provider contract fault.
No ready settlement advertised without an actual installed provider before initialization.

## 6. Compatibility

Development-only direct contract/build/schema migration. No old signatures or save readers.

## 7. Verification

At completed slice: one solution build and ONE exact new bounded scenario:
`Tycoon purchasing: sole settlement and aggregate physical admission`.
Cover one-owner aliases, aggregate shortage, caller order, multiple accounts, partial receipts,
native creation/finite stock, off/dormant/omitted independence, save/load, range/stale admission
and an explicitly named provider fault. Earlier scenarios compile, not rerun. No family/full
suite or per-edit gate. Timed enabled hauling, competing travel obligations, custom provider
matrix, replay and advanced sale/market behavior remain distinct pending acceptance.

## 8. Risks

Account admission is synchronous, not a persistent budget guarantee. Endpoint callbacks
cannot retain execution scope. Module-owned price validation is not arbitrary mod sandboxing.
Do not claim direct contract probes prove physical truck travel or full fault recovery.

## 9. Out of scope

Foreign exchange, seller revenue/market exchange, auctions/leases/contracts, reserved travel
budgets, advanced Tycoon activation, hosts/demos/Draw/platforms and full roadmap completion.

## 10. Sub-task execution order

- [x] Replace concrete purchase checks with optional contracts and retain one immutable rule owner.
- [x] Move all supply payment out of endpoints into the sole account owner.
- [x] Admit complete purchase vectors through the existing physical transaction owner.
- [x] Require exact transient fulfillment scope and expose actual capability in views.
- [x] Add explicit authored fixtures and a new bounded contract/independence scenario.
- [x] Build and run only that exact scenario; record invocation chronology and actual results.
- [x] Reconcile master, AGENTS, README and affected active plan status with scoped evidence.
- [x] Native/finite timed haul, real money change/funding, route retry and survival focused proof:98.
- [ ] Broader endpoint/fuel/dock/provider/replay, adversarial pricing/commit and full acceptance.
