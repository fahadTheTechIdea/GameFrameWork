# P4-H1 — Optional Tycoon module and authored default values

## Current M144: contract-defined valuation and value entries — 2026-10-23

Scoped verified155/0 across two exact final cases, mechanical migration only.
[Actual evidence and remaining gates](../../docs/migration/P4_PRODUCTION_INCOME_VERIFICATION.md#current-m144-direct-valuation-and-income-contracts--2026-10-23).
Existing2.768 primary-income baseline101/0
establishes the unchanged default-value/settlement behavior before this slice. Replace
mandatory BusinessValuePolicyDef selection with IBusinessValuePolicyDefinition and
concrete fixed-entry enumeration with IFixedResourceValueDefinition. Delete the former
specialized abstract base, not a compatibility wrapper. Native fixed policies/entries
and independent JSON definitions share ONE value-entry reader/linker and ONE fixed
catalog/quote algorithm. Retain exact unit validation, explicit zero/missing semantics,
duplicate checks, rational fixed-point product/range/remainder behavior and all prices.

Native2.769/save161/replay2; no state codec, balances, default mode flags, module ownership,
resource timing or financial-rule change. The paired production-income migration is owned
by [P4-H2](P4_PRODUCTION_INCOME_DESIGN.md). One new non-friend definition family reuses
the existing master/physical producer fixture and canonical declarations, never a second
quote/income implementation. One new exact case plus the original affected income case;
explicit excluded economic-profile row and metadata-only inspection, no full/profile run.
Fresh native compiled caller audit after the final build; retain broader original gates.

## Historical implementation checkpoints

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


Status: partial; last verified fixed-default foundation,2026-10-07, before in-progress H2 edits.
Build2.205.0/save109; business account codec1/view1; existing payload schemas unchanged.
Clean build;72 checks/one new exact named scenario,0 failures/aborts. Older suites compile only.
[Evidence](../../docs/migration/P4_BUSINESS_DEFAULTS_VERIFICATION.md). Advanced Tycoon providers,
automatic income and budget reservations are not proven by this gate. User clarification supersedes
blanket business-exclusion/removal.
Parent: [P4](P4_REUSABLE_MODULES_DESIGN.md). Next: [H2](P4_PRODUCTION_INCOME_DESIGN.md).

Current planning revision: [P4-I game modes](P4_GAME_MODES_DESIGN.md) owns shared mode enablement.
Values, accounts, income and monetary budgets/trading are independent opt-ins. The inspected source
has independent JSON accounts/rule-based optional composition at account3/view3. I3's current88-check
matrix is [separate evidence](../../docs/migration/P4_BUSINESS_MODE_COMPOSITION_VERIFICATION.md),
not a rerun of the historical account1/view1/72-check gate. H2 income preparation remains unwired;
unsupported income/trading/Tycoon activation refuses, not a functioning advanced provider claim.

## 0. Problem statement

The framework is a simulation/colony/RTS framework, not a mandatory business game. Optional business
support must serve both fixed-value games and explicitly activated Tycoon games without duplicate
accounts or mandatory trading. Survival games may have nobody to buy from or sell to.

Before H1, the then-current Standard/Economy.cs defined IEconomy and explicit FreeEconomy, not a complete shared business
module. Content/Resources.cs defines IResourceCatalog/IResourceDefinition with native quantity units,
not economic values. Kernel/Modules.cs owns composition. Tests/ExtensionProof/ColonyGame.cs has
TestLedger; it is test support, not a runtime economic implementation. The oil extension currently has
Companies : IEconomy in OilCo.Game/Company.cs and sales credit in Operations.cs. Audit their ownership
before promotion; this plan does not claim they already form a reusable Tycoon module.

## 1. Goals

1. Resource-only survival composes without the business module, money service, prices or traders.
2. Selecting the business module supplies only enabled capabilities: values without accounts,
   accounts without income/budgets, or explicitly activated advanced rules. At most one account owner.
3. Default values cover every selected resource by stable ID and quantity unit, including explicit
   zero values. Gold100000 per one authored gold unit is a content example, not a hard-coded constant.
4. No market actor is required for default production income. Hosts expose engine facts only.

## 2. Design

Reuse IEconomy for balances, affordability, charges and credits. Establish one runtime business module
owner, migrating any promoted generic responsibility rather than copying Companies or TestLedger.
When accounts are enabled, TycoonModule binds the same BusinessAccounts instance as IEconomy
and IBusinessAccounts; one codec owns balances/debt. Accounts alone must not require a valuation
catalog. IResourceValuation is separately supplied when values are enabled by a BusinessValuePolicyDef.
FixedResourceValuesDef selects an explicit catalog; independent FixedResourceValueDef entries come
from master-listed resource_values documents. Additional resource/value files enroll without scheduler
changes. No second mutable resource/value store is created; resolved definitions remain authoritative.
Add only the remaining settlement/budget contracts to that module. Physical inventory remains
IInventory; ResolvedContent remains IResourceCatalog; mode and values do not become inventory state.

Register module/content/settings through the existing master manifest and ContentTypes resolver.
JSON declares shared mode properties through P4-I and module provider parameters: account currency/
opening amounts, valuation policies, resource values and unit identities. Opening cash is an account
initialization parameter, not an automatic building budget or monetary-cost enablement setting.
No directory scanning, per-resource switch, host price table or guessed missing value. Module absence
means no economic behavior. Default means explicit authored data, never implicit FreeEconomy fallback.

Resolve all references and units before Initialize. A game without business need not supply a value
table or dummy ledger. Only effectively enabled monetary costs require accounts; disabled monetary
requirements do not disable real resource/machine/time requirements. Enabled costs without a provider
refuse composition. Trading-off refuses purchases, never makes supplier stock free.
Selection/mode is fixed for a session initially; save identity captures it. Live mode switching is deferred.

## 3. API surface

Retain IEconomy. Implemented IResourceValuation.Quote(ResourceId, Fix) returns immutable
ResourceValueQuote with status, resource, native unit/quantity, currency, unit value, value, mode and
policy identity. A default quote is Unavailable, not successful zero. Exact integer multiplication
refuses overflow and unrepresentable currency results, never silently rounds into zero income.
IBusinessAccounts publishes detached BusinessAccountSnapshot. BusinessView exposes quotes and
company-private account reads, never provider/world handles. BusinessValuePolicyDef.Create(World)
is the open registered content extension point; native fixed policies require matching scenario mode.
At most one provider per enabled contract and one ledger when money is enabled. No caller casts to
a Tycoon class; module selection alone is not an account/income/market enablement switch.

Native content uses ordinary sections. The document is explicitly listed under sim.tycoon in
the master manifest; resource and unit definitions are separately authored through the existing pipeline.

```json
{
  "fixed_resource_values": [
    { "id": "default-values", "catalog": "defaults", "currency": "unit.money" }
  ],
  "resource_values": [
    { "id": "gold.default", "catalog": "defaults", "resource": "gold", "unit": "unit.gold", "currency": "unit.money", "value": 100000 }
  ]
}
```

The historical H1 schema required policy, module-level mode and inline opening_cash. I3 removes those
readers directly. Effective mode properties determine enabled capabilities; module settings now name
valuation_policy/account_policy/income_policy. Independent business_accounts definitions author
currency/opening_cash through IBusinessAccountDefinition. No second module-level mode/enablement switch
survives. A new value entry can live in another master-listed document with the same catalog.
Advanced content registers its own BusinessValuePolicyDef/provider; activation must match central
Tycoon enablement, not a second mode authority. JSON cannot select an arbitrary CLR type.
Advanced rules remain authored engine-side contract implementations, not host logic or this native
fixed policy's implicit fallback. Resource-only scenarios omit sim.tycoon and its settings entirely.

## 4. Files to change

Current paths: Standard/Tycoon/TycoonModule.cs, BusinessValues.cs, BusinessAccounts.cs and Views/BusinessView.cs.
IEconomy now lives in Standard/Tycoon/Economy.cs; FreeEconomy is removed. Historical H1 reused the
existing GameModule/ContentTypes/master/resolver pipeline,
strict resolved UnitDef/ResourceDef identities and service codec/view registration. Runtime build
identity changed in Kernel/SaveGame.cs; save109 remains because module-owned codecs self-describe.
tests/ExtensionProof/content/business-*.json declare modules, scenarios, values, units and new resources;
BusinessDefaultsTests.cs observes/instruments contracts without implementing gameplay or a ledger.
Do not modify OilFieldDays in this planning request. Ownership migration needs its own scoped approval.

## 5. Error handling

Missing provider for enabled monetary requirements, duplicate ledger/value provider, invalid resource/unit,
missing monetized-resource value, ambiguous currency or overflowing amount: typed composition/quote
refusal. Explicit zero is valid. Business absence in a resource-only game is valid, not an error.

## 6. Compatibility

Development-only direct migration; no old loaders, wrappers, duplicate managers or compatibility shims.

## 7. Verification

Completed one build/one new named scenario:72 checks in237ms; build0 warnings/errors in5.59s.
Survival, default values, negative/missing/duplicate configuration, unit mismatch, exact precision,
account ownership/privacy/debt/overflow, saved twins, interleaved isolation and extra master-listed
resource/value files pass. A real mapless timed process completes through normal commands without
money/traders; monetary work without a provider refuses. Selecting tycoon with the fixed policy refuses
rather than falling back. This does not prove an advanced Tycoon provider or its market rules.
No full/family/previous scenario runs. The pending recovery-cost scenario compiled but was not run.

## 8. Risks

Do not confuse monetary value with capacity, route travel cost, fuel budget or resource quantity.
Do not assume that every simulation uses money, companies, buyers, sellers or oil-domain economics.

## 9. Out of scope

New market systems, leases, auctions, contracts, platform adapters, Draw, real financial guidance.
Existing advanced Tycoon behavior is reused only when activated, not expanded by this stage.

## 10. Sub-task execution order

- [x] Audit current IEconomy/TestLedger/oil Companies owners; declare the optional framework owner.
- [x] Fixed-default JSON values, authored active mode/opening budget and open valuation contracts.
- [x] Validate selected-module dependencies; resource-only games remain independent.
- [x] Immutable mode/value/account facts and self-describing account codec/content identity.
- [x] One affected composition/default-account gate and actual evidence.
- [ ] Independently compiled authored advanced/Tycoon valuation provider proof and provider-fault matrix.
- [ ] P4-I2/I3 shared mode configuration, values-only/accounts-only decoupling and actual disabled-money gates.
- [ ] H2 committed generation settlement and reserved building budgets; H3 advanced mode/durability.
