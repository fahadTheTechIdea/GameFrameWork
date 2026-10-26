# P4-H3 — Optional Tycoon activation and durability

## Current M81: saved shuffled optional resource commerce

Unchanged native2.725/save161/replay2/all450 native sources, native/proof DLLs and JSON. The SAME
PurchasingHaulTests.ResourceCommerce selects standalone or the existing native Pair transport; no
second physical scenario, transport implementation or economic rule. Six actual owner commands
plus six actual foreign refused commands execute canonically through delayed/reversed frames. Both
original peers save and reattach a second Pair at real physical/sale checkpoints. Each primary has
12 inputs/129 checkpoints and identical canonical replay bytes/final hash3CFB440FF2F89A4C; standalone
retains its original six-input hash13AFA09A56DB4910. Two exact finals15072/0/two native verifier runs,
not full module/provider/fault/stress acceptance. [Current evidence and remaining original gates](../../docs/migration/P6_NETWORKED_MOD_DURABILITY_VERIFICATION.md#current-m81-shuffled-native-resource-commerce-and-saved-authority--2026-10-22).

### Retained M80 implementation

Unchanged native2.725/save161/replay2/all450 native sources/DLL. The original Supplier promise book,
ResourceTransactions, ResourceSalesSystem and sole BusinessAccounts now have an engine-only physical
acceptance path for cancelled unloaded supply, progressive paid handling, actual delivery and
JSON-scheduled sale/reporting. The same original factory/test owner is extended; the only added
game-module declaration is pure actual-inventory sale eligibility, not an account or sales service.
One exact FIRST/FINAL2040/0/one complete129-checkpoint replay, with the resource-only no-Tycoon
delivery helper reused for the changed factory. [Actual caller review, content, scoped proof,
input identities and remaining gates](../../docs/migration/P4_TYCOON_OWNERSHIP_VERIFICATION.md#current-m80-optional-native-resource-commerce-accounting--2026-10-22).
No native business rule, default price, consumer, host/demo logic or clock changes. Earlier records
below retain their historical images; original broad requirements/full goal remain open.

## Earlier checkpoints

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

Current physical purchasing2.213.0/save117: public stock/production/access commands drive existing
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


Authoritative ownership revision,2026-10-07: the sole native economic module is `TycoonModule`
(`sim.tycoon`). It contains basic values, accounts, costs, income and priced suppliers as well
as advanced rules. There is no separate BusinessModule. Module selection and advanced
`business.tycoon.enabled` activation are distinct; basic-only and dormant selection are valid.
Physical work/manufacturing/transport never require it. No old module alias or FreeEconomy.
All native monetary content declares this owner; legacy authored content must migrate directly.

Status: planned,2026-10-07. Depends on [H1](P4_BUSINESS_DEFAULTS_DESIGN.md)
and [H2](P4_PRODUCTION_INCOME_DESIGN.md). Parent/master phase IDs remain unchanged.
Shared enablement/configuration is [P4-I](P4_GAME_MODES_DESIGN.md), not another Tycoon game loop.
Current I3 composition2.208.0 removes the duplicate business `mode` setting. Tycoon activation explicitly
refuses until an actual advanced provider is implemented; fixed values/accounts are not its substitute.

## 0. Problem statement

Business module selection and Tycoon mode activation are different choices. Survival games omit
business entirely; fixed-value games select it without advanced market rules; Tycoon games explicitly
activate those rules. All enabled modes use the same module, accounts and economic contracts.

## 1. Goals

1. Survival initializes/updates/saves with physical resources and no economy/trading dependencies.
2. Fixed defaults use immutable authored values; production income occurs only when separately
   enabled with accounts and a real settlement policy, without requiring buyers or sellers.
3. Tycoon mode alone activates authored advanced economic rules through the same valuation,
   settlement, affordability and budget contracts. No parallel balance or double credit.
4. Saves, hashes, replay and session isolation include selected modules, mode, policy and values.

## 2. Design

Master JSON explicitly lists profiles/properties and optional business documents. Existing scenarios
select modules and a P4-I profile with permitted overrides. Central `business.tycoon.enabled` controls
advanced activation, while module settings supply actual provider parameters, not duplicate enabled
bits or another `mode` switch. Absent module means no economics; false flags may leave a selected
module dormant. No auto-enable on encountering gold/resources. Require economic capability only
for effectively enabled monetary content; money-off is not unlimited dummy accounts.

The active valuation/settlement policy is selected once at composition when its capability is enabled;
values-only and accounts-only are valid independent choices. Generic construction,
production and logistics consume contracts without Tycoon switches. Advanced systems register only
under explicitly activated Tycoon rules. Reuse existing Tycoon rules rather than invent new market
features or move domain-specific oil economics into generic schedulers.

Persist canonical module versions, authored mode, value/content identity, account and settlement state
in the existing save framework. Incompatible/missing mode policies refuse restore before Update.
Use one GameClock and declared deterministic system order; no independent market timer or host loop.
Live mode toggling is out of this first implementation, avoiding undefined outstanding obligations.

## 3. API surface

Immutable business-enabled/mode/value provenance and private budget/income facts through existing
view contracts. Module absence is visible capability absence, not a zero balance posing as an account.
No new host-facing market API or platform adapter in this stage.

## 4. Files to change

Kernel/Modules.cs; existing ContentTypes/master/scenario pipeline; Kernel/SaveGame.cs and owning
module codec; existing private view registration; affected authored fixtures and focused tests.
Current consolidation supplies TycoonModule, IResourceValuation and BusinessValuePolicyDef as the selected
JSON/contract boundary, plus one IBusinessAccounts/IEconomy owner. Native fixed policies are implemented;
no advanced Tycoon provider/rules are installed or proven by H1's72-check fixed-default gate.

## 5. Error handling

Unknown mode, missing activated policy, duplicate ownership, monetary requirements without business,
or mismatched saved content: typed composition/restore refusal. Normal resource-only game: success.
Advanced systems cannot silently run in fixed-default mode or fall back to guessed market values.

## 6. Compatibility

No legacy mode aliases, duplicate registries, compatibility facades or old-schema adapters.

## 7. Verification

At stage completion run only affected bounded mode/save/session scenarios: survival, fixed-default
gold100000, Tycoon activated, inactive advanced-system evidence, module refusal, saved continuation,
twin hashes and interleaved isolation. No full/family suite or per-edit reruns without user request.
This plan revision runs no tests and reports no new runtime completion.

## 8. Risks

Do not equate business-enabled with traders-present, and do not make money mandatory for resources.
Existing supplier charges are physical purchases, not resource-generation payouts; explicit content
and the shared module must distinguish them. Pure movement/stock transport remains independent.

## 9. Out of scope

Adding buyers/sellers to survival, new leases/auctions/contracts, live mode switching, Draw or platforms.

## 10. Sub-task execution order

- [ ] Select/validate optional module and authored active mode through the master pipeline.
- [ ] Use P4-I resolved enablement; delete overlapping module-mode selection and forbid silent provider substitution.
- [ ] Gate existing advanced systems while retaining one shared economic contract/account owner.
- [ ] Persist mode/value/settlement identity and publish capability-aware views.
- [ ] Run the affected bounded mode/durability gate once and record real evidence.
- [ ] Reconcile master/AGENTS/specification/README; whole implementation plan stays open.
