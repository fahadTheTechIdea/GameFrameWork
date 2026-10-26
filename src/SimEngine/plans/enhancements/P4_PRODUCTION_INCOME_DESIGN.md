# P4-H2 — Committed production income and building budgets

## Current M144: contract-defined production-income policy — 2026-10-23

Scoped verified155/0 across the new independent policy case and original native income
case. [Actual evidence, replay and remaining gates](../../docs/migration/P4_PRODUCTION_INCOME_VERIFICATION.md#current-m144-direct-valuation-and-income-contracts--2026-10-23).
Replace mandatory ProductionIncomePolicyDef with
IProductionIncomePolicyDefinition; native PrimaryProductionIncomeDef implements it
directly. Move the existing strict JSON reader and primary-only evaluator into ONE
canonical declaration used by native and independent definitions. Preserve sorted unique
explicit causes, primary_only, no consumed-input rewards and the original produced bill.
BusinessAccounts/ResourceTransactions remain the sole once-only monetary/resource owners.
The companion valuation/value-entry migration is [P4-H1](P4_BUSINESS_DEFAULTS_DESIGN.md).

One independent non-friend metadata family, normal native timed generation, saved partial
work/settled twins, explicit zero values, recycling/cancellation non-reward, optional income
and module-off physical generation, whole live replay and malformed metadata are checked
through the existing production-income test owner. No test-side pricing, eligibility,
payment, resource or update implementation. Original affected native case101/0 baseline
is separate; final exact cases retain existing60000ms bounds. No new financial amounts,
state codec or business rules; build2.769/save161/replay2. No full/profile/family/soak run.

## Historical implementation checkpoints

Current account-owned budgets2.214.0/save118: IMonetaryBudgets aliases the SAME optional Tycoon
account. Selected ordinary work costs acquire whole-set receipts at real assignment, pay once at
first productive progress and release unpaid holds through existing resource cancellation/reopen
lifecycle. Paid work keeps provenance but has no live hold or cancellation refund. Manufacturing
and supplier admission use unreserved money; purchase commits guard the hold revision. Existing
account4/board41/work41 codecs and work view74/business view5 expose exact saved/private diagnostics.
One exact new bounded player-command scenario64 checks0 failures/aborts494ms; final clean build
2.33s. Three builds/three same-scenario invocations, including fixture ordering/compile repair and
an accidental stale-binary repeat, recorded in [evidence](../../docs/migration/P4_WORK_BUDGET_VERIFICATION.md).
[Budget design](P4_WORK_BUDGET_DESIGN.md). Older metadata/signature edits compile only; no full/family
gate. Wider provider/purity/debt/range/orphan-corruption/interruption/replay/advanced acceptance
remains open. One unchanged GameClock; no platform/Draw/host/demo/OilFieldDays changes.

Historical physical purchasing2.213.0/save117: public stock/production/access commands drive existing
engine matching, real machine travel, partial paid loading/unloading and authored payment/route retry.
The same optional Tycoon account owns purchase and income settlement. Actual manufacture spends
funds during travel; actual timed resource generation funds the unpaid remainder without test-side
credits. Saved travel/load/wait/route/unload continuations are identical; survival moves real stock
without any economic service. Preview/live shortages share stable typed wait codes. Payloads remain.
One clean build0 warnings/errors5.66s and ONE exact new scenario98 checks0 failures/aborts484ms, first
attempt; no older/family/full suite. [Physical design](P4_PURCHASE_HAUL_DESIGN.md),
[current evidence](../../docs/migration/P4_PURCHASE_HAUL_VERIFICATION.md). Competing persistent budgets,
endpoint/fuel/dock/recovery/provider/mixed-leg/replay acceptance and seller/advanced rules stay open.
One unchanged GameClock; no Draw/platform/host/demo/OilFieldDays changes. Older checkpoints below
are historical, not current-source runtime proof.

Historical supplier purchasing2.212.0/save116: one optional trading contract aliases existing immutable
rules; complete supply settlement aliases the SAME Tycoon account owner/codec. Suppliers no longer
charge money. ResourceTransactions admits all prices/money before mutation, verifies actual physical
receipts, then settles once. Exact transient execution scope prevents a raw callback becoming another
purchase API. Fixed supplier purchasing needs neither advanced Tycoon nor resource valuation. Account3
and physical payloads remain; business view4/supplier4. One exact new scenario73 checks0 failures/aborts
273ms; final clean build0 warnings/errors2.77s. Five builds/three invocations of that SAME scenario
(new compile/fixture repairs), no older/family/full suite. [Design](P4_SUPPLIER_PURCHASING_DESIGN.md) and
[current evidence](../../docs/migration/P4_SUPPLIER_PURCHASING_VERIFICATION.md). Enabled timed hauling,
persistent budgets, seller/market/advanced rules, custom provider/replay and full acceptance remain open.
One unchanged GameClock; no Draw/platform/host/demo/OilFieldDays changes. Older checkpoints below are
historical, not current-source runtime proof.

Historical H2 production integration2.211.0/save115: optional explicit JSON income policy is enrolled
through TycoonModule's SAME account owner/codec. Existing ResourceTransactions preflight generation,
verify real output, settle once, then publish resource facts. Primary-only policies exclude input
recipes; initialization, transfer, cancellation return, preview and load never pay generation rewards.
Batch, inventory, multi-endpoint and continuous generation use existing physical/time owners. Continuous
preflight now checks the same optional settlement contract before input acquisition. Native full-capacity
and account-range refusals block with no partial stock/money. Account3/view3 and physical payloads remain.
[Current evidence](../../docs/migration/P4_PRODUCTION_INCOME_VERIFICATION.md):101 checks0 failures/
aborts355ms in ONE exact bounded new scenario; final clean build0 warnings/errors4.32s. Four build/
scenario invocations (two fixture repairs; intermediate88 pass; source-detected continuous gap fixed).
No prior/family/full suite run. H2 budget reservations, custom provider/commit fault matrix, broader
producers/recovery, enabled trading, advanced rules and full acceptance remain open. Previous
checkpoints below are historical, not rerun. Income-on now works with actual explicit providers;
trading/advanced-on continue to refuse until their own integration. One unchanged GameClock.


## Active commit integration,2026-10-07

Reuse the prepared IResourceProductionSettlement/IResourceProductionCommit contracts and the
same Tycoon-owned BusinessAccounts. Wire both verified Produced inventory batches and actual
conversion outputs; aggregate a whole conversion's consumed/output bills, excluding released tools.
Native primary-only policies whitelist causes and exclude consumed-input recipes. Initialization,
stock transfers, purchases, cancellation returns, previews and restore never settle generation.
Income-enabled composition requires an explicit frozen policy plus real accounts and valuation;
income-off binds no settlement provider, regardless of dormant policy authoring.

Admit resource/capacity/quote/account arithmetic before physical mutation. Actual output is verified
before committing the ephemeral settlement once, and both complete before publishing resource facts.
No history subscription, poller, clock, second ledger or journal-derived payout. A prepared-provider
violation after irreversible output uses the existing deterministic fault boundary; no fake rollback.
Settlement refusal has a typed resource status/blocker and leaves material/WIP/account facts untouched.
Existing account Sync owns income policy, sequence, counts and earnings; no duplicate state protocol.

Gate this slice with one exact bounded public-command production scenario (gold100000, income off/
omitted, primary versus recycled input, full capacity, saved continuation and arithmetic refusal), plus
explicitly labeled direct transaction contract probes. Budget reservations and advanced rules remain
separate unfinished tasks; do not mark H2 or the full plan complete from this gate alone.

Historical preceding Tycoon consolidation2.210.0/save114: native economics are owned by one optional
`TycoonModule` (`sim.tycoon`), with implementation under `Standard/Tycoon`. Native supplier and
monetary requirement declarations moved from Logistics/Work; FreeEconomy and the old module
class/identity are removed. Basic flags do not activate advanced rules. Native monetary authoring
selects its owner even with costs off; dormant selection needs no accounts, valuation or currency
file. Unpriced timed work/manufacturing omit the module entirely. One shared trading-off gate
serves haul admission, native vendor fulfillment and direct resource receipts; no free stock.
[Current evidence](../../docs/migration/P4_TYCOON_OWNERSHIP_VERIFICATION.md):81 checks,0 failures/
aborts,407ms,one exact scenario. Final clean build0 warnings/errors2.82s. Three build invocations
(one new test API compile repair) and two invocations of that same scenario (one unused account-file
fixture repair); no older/family/full suite. Enabled trading, income, advanced rules and broader
budgets/provider acceptance remain open. Checkpoints below are historical and were not rerun.


Historical source scaffolding: compiled in2.208.0, not integrated/runtime-verified at that checkpoint.
Depends on [H1](P4_BUSINESS_DEFAULTS_DESIGN.md) and [P4-I2/I3 mode configuration](P4_GAME_MODES_DESIGN.md)
for final shared enablement.
Next: [H3](P4_TYCOON_MODE_DESIGN.md).

H1 now supplies TycoonModule/IBusinessAccounts/IResourceValuation and JSON value definitions on
build2.205.0/save109. Its72-check gate proves quotes/accounts only: quoting gold does not credit money,
and no automatic generation-income settlement or budget reservation was installed. Reuse these owners.
Current source has production-settlement contracts/primary-only policy/account2/view2 additions;
actual ResourceTransactions/ResourceConversions do not invoke settlement yet. No new runtime proof.
I3 native accounts now use their independent JSON account definition (codec3), not valuation identity.
Preparation compares quotes to the actual valuation provider. Native income enrollment was removed
with the duplicate business mode reader; income-on explicitly refuses until actual H2 commit integration.

## 0. Problem statement

User clarification: income and building budgets are optional independently. A game may omit the
business module, select values only without accounts, or select accounts with no income policy.
The final P4-I configuration separately selects money, production income and monetary cost categories.
Omitted/null income_policy supplies no provider; an enabled income flag without a provider refuses.
Monetary costs apply only when effectively enabled AND explicitly authored. A supplied policy must
not override a disabled central feature, and absence must not create a dummy provider.
Do not implement mandatory income, a zero-budget dummy account or automatic construction costs.

Default resource value must become money only through actual engine-owned generation, not a trader,
UI calculation, preview or transfer. Construction may require both physical resources and money
when that monetary category is selected; business module presence alone is insufficient. Survival
continues building/manufacturing with real resources/time even when authored money gates are off.

Current Core/ResourceTransactions.cs and Standard/ResourceConversions.cs own conversion admission
and committed output. Views/ResourceFlowView.cs exposes the existing IResourceFlowJournal. Existing
WorkBoard and TransformerManager own physical progress; IEconomy owns money. Reuse these authorities.

## 1. Goals

1. With values, money and production income explicitly enabled and gold valued100000/unit,
   committed eligible production of1 unit credits100000;
   another2 units credits200000, exactly once to the actual resource-producing owner.
2. Without business or with income disabled, the same production creates physical gold only;
   survival needs no currency or trader, accounts-only receives no implicit reward.
3. Hauling, inventory moves, starting stock, reservations, previews, cancellation and save/load
   produce no duplicate income. Failed or uncommitted output produces none.
4. Building spends authored resources and, only when selected/authored, a monetary budget through
   existing admission/job progress. Money never replaces physical materials, machines or game time.

## 2. Design

One settlement responsibility within the shared module consumes verified committed production
facts through the existing resource transaction/completion protocol. Do not add production logic
to tests, journal views, hosts or a second scheduler. Extend the existing commit protocol if needed
so output and settlement have coherent failure semantics; do not assume event delivery is transactional.

Credit quantity times the module's active unit-correct value. Record a stable settlement identity
and policy/value provenance with the existing owning save codec. Checkpoints retain the processed
commit position/identity needed for exactly-once continuation; never recompute income from stock.
Keep account balances in IEconomy only, physical quantities in IInventory only.

Generation income is a production reward in default mode, not a sale or physical removal of output.
Default valuation does not require a buyer. Later sale/transfer of already credited output cannot
credit the same default reward again. Tycoon mode selects its settlement policy through the same
contract; no simultaneous default-plus-Tycoon payout. Recycled/looped transformations require authored
reward eligibility/net-output rules to prevent free-money loops; resource valuation still covers all resources.

Reuse P4-I3's classified effective construction/activity monetary requirements and actual IEconomy
admission. Money-off means no account, not a FreeEconomy. If budget reservations
are required, the account owner owns them once; planners/jobs reference tokens, not copied balances.
Specify charge timing, cancellation refunds and acquired-budget release before implementation. Resource
requirements and execution timing remain authoritative in the existing DAG/job/transaction owners.

## 3. API surface

Extend H1 valuation/settlement/budget contracts only where needed. Publish immutable earned-income,
remaining budget, charge/refusal and policy provenance through existing private view registration.
Proposed contracts are not already implemented. Commands/previews share real admission.

## 4. Files to change

Core/ResourceTransactions.cs; Standard/ResourceConversions.cs; Standard/Tycoon/Economy.cs; actual shared
business module; relevant existing construction/work requirement and immutable-view owners; JSON fixtures.
No second TransformerManager, ledger, resource journal, GameClock or job store.

## 5. Error handling

Insufficient budget: typed refusal with no resource/job mutation. Missing quote, overflow or invalid
owner: typed refusal before commit. Provider violation after irreversible mutation: existing deterministic
fault boundary, not a fabricated success or hidden compensation. Duplicate settlement: no second credit.

## 6. Compatibility

Direct current-schema migration if payloads change; no legacy reader or replay shim.

## 7. Verification

One focused bounded production/build scenario: business absent; gold example; partial/multi-output
generation; full destination; transfer; cancellation; resource shortage; insufficient money; saved twins;
repeat commit/replay and recycle-loop refusal. Build only if needed at completed slice; no broad reruns.

## 8. Risks

Resource facts can represent initialization, production, transfer or destruction: only verified eligible
generation qualifies. Finite journal retention cannot silently lose settlement; consumer ownership and
saved continuation must be proven. No system can read mutable balances to invent a production event.

## 9. Out of scope

New market mechanics, host gameplay, automatic selling, instant construction, platform implementation.

## 10. Sub-task execution order

- [x] Native primary-generation facts, synchronous payout timing, counters and failure semantics; broader custom-provider/recovery matrix remains open.
- [x] Adopt resolved mode gates for native income and monetary categories; no module-settings duplicate. Broader rates/budget/provider matrix remains open.
- [x] Integrate the sole Tycoon account settlement with verified inventory/conversion commits; continuous admission uses the same contract.
- [x] Native ordinary work budget admission/assignment/first-progress payment/cancellation through the same account; wider custom/debt/interruption acceptance remains open.
- [x] Persist/hash native budget provenance and publish immutable diagnostics; adversarial corruption/replay proof remains open.
- [ ] Prove the gold/survival/transfer/save cases with the one affected gate.
