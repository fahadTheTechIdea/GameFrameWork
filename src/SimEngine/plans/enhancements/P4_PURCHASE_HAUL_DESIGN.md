# P4-I3 — Physical purchasing over the one game clock

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

Status: native/public-contract timed hauling focused-verified,2026-10-07; broader acceptance open.
Build2.213.0/save117; payloads unchanged. One clean build0 warnings/errors5.66s and ONE exact
new scenario98 checks0 failures/aborts484ms, first attempt. Older suites compile, not rerun.
[Evidence](../../docs/migration/P4_PURCHASE_HAUL_VERIFICATION.md).
Parent: [supplier settlement](P4_SUPPLIER_PURCHASING_DESIGN.md),
[master tracker](MASTER_TRACKER.md). This completes focused physical acceptance beneath that same
settlement owner; it does not introduce a second purchase system, host workflow or engine loop.

## 0. Problem statement

The2.212 supplier gate verified direct synchronous receipts, not timed player-driven hauling.
`Logistics.Preview` generated an enum-derived purchase blocker, while `WorkSystem.AcceptHandling`
used the stable `haul.payment-wait` code. Public previews/dispatch/live retry must agree. Real machines
must travel, progressively load only paid quantities, carry actual cargo, progressively deliver,
retain paid cargo across route/payment failures, and resume saved execution without repeat charges.

## 1. Goals

1. A public stock-policy command triggers engine-owned source matching, dispatch, travel and handling.
2. Purchase money changes only for actual earned load chunks, never at dispatch/travel/unload/restore.
3. Native authored creation and public-only finite stock use the same account settlement contract.
4. A real manufacturing action may consume funds while a truck travels; unpaid remainder waits at the
   supplier with existing receipt/progress/retry state. Real timed production income can fund resumption.
5. A broken route retains paid cargo and physically resumes when player intent restores access.
6. Unpriced survival stock hauling omits Tycoon/accounts yet uses the same physical engine protocols.
7. Every wait/phase is bounded and failures expose authoritative job/stock/position diagnostics.

## 2. Design

Reuse `LogisticsService`, `WorkBoard`, `WorkSystem`, `ResourceTransactions`, native accounts/income,
Transformations and the existing public-only point-space providers. No new gameplay provider, scheduler,
mutable job, receipt, account, clock or codec. `HaulHandlingCodes.PurchaseRefusal` maps typed purchase
admission to the same stable wait codes as `AcceptHandling`; no enum-name-generated blocker identity.
The same account owner prices/admits exact chunks when loading, not a retained preview quote.

All new scenario content is explicit JSON, including one master listing modules/documents, physical
machines/positions/handling, account/value/income policy and mode profiles. Native supplier prices are5;
eight actual loaded units cost40. Explicit opening cash100, manufacturing cost90, and separately
whitelisted timed funding generation6 units at5 exercise a shortage and its physical resumption. The
test submits existing player commands; it never directly charges/credits accounts, allocates jobs,
reserves receipts, calls loading callbacks or computes gameplay schedules. Assertions observe engine
views and actual provider stock/position only. Named finite-source/survival configurations change
authored content and real player seats, not runtime rules.

Normal admission has no long-lived monetary reservation yet. Actual loading uses current funds and
the existing typed retry policy. Competing budget guarantees belong to H2's SAME economic account
owner in a later stage; do not make dispatch invent account balances or claim future affordability.

## 3. API surface

No new host command or public gameplay API. Existing ConfigureStockPolicy, EnqueueTransformation,
point access intent, immutable Logistics/Work/Business views and Initialize/Update/save boundaries.
Stable purchase-refusal mapping is internal protocol translation, not another native economic policy.

## 4. Files to change

Standard/ResourceHandling.cs; Standard/Logistics.cs; Kernel/SaveGame.cs; tests/PurchasingHaulTests.cs;
public-only proof embedded-resource registrations; four master-listed purchasing-haul JSON files;
identity-only assertions in older scenarios; active plan/tracker and scoped verification evidence.

## 5. Error handling

Normal payment shortage is haul.payment-wait in both preview and execution. Existing authored retry
backoff preserves actual loaded stock, unspent receipts and earned progress. Disabled trading and
absent capability retain explicit typed blockers. Route loss uses existing unreachable retry. Settlement
range/invalid price is an explicit stable refusal, not silent free supply. Provider faults retain existing
deterministic engine fault boundaries; this stage does not add compensation or rollback.

## 6. Compatibility

Direct build/save identity migration; existing physical payloads unchanged. No legacy blocker aliases,
alternate scheduler, old save reader or simulated free economy.

## 7. Verification

One solution build, then only the exact new bounded scenario:
`Tycoon hauling: timed purchases, payment waits and physical continuation`.
Native/finite paid trips, partial load/delivery saves, manufacture-induced shortage, true production
funding, preview/live blocker parity, no unpaid job allocation, broken-route continuation and survival.
No older/family/full suite. Run again only to repair a demonstrated new compile/fixture/source failure.

## 8. Risks

Positive opening cash is not a reserved budget. A finite owned public stock source may be selected as
unpriced stock, so the purchase proof authors a different supplier company. Test content contains actual
machine capacities/world extent, not framework/platform size limits. Point-space movement proves public
contract portability, not all spatial implementations, road congestion, dock queues or platform support.

## 9. Out of scope

Persistent budget reservations, seller revenue/advanced markets, complete incident/endpoint/fuel failure
matrix, arbitrary provider-fault acceptance, replay/lockstep stress, full regression, hosts/demos/Draw.

## 10. Sub-task execution order

- [x] Inspect existing physical load/unload/retry ownership and replace inconsistent preview blockers.
- [x] Author physical/economic/mode/master JSON using existing native and public-only contracts.
- [x] Add player-only bounded physical scenarios with no fixture-owned economic/transport behavior.
- [x] Build and run only that exact scenario; first attempt passes, no repair/rerun.
- [x] Record current source evidence and reconcile master/AGENTS/README/active plans.
- [ ] H2 competing budgets, broader physical/provider/replay/durability acceptance.
