# P4-I3 — Engine rule enforcement and optional economics

## Current M144: valuation and income content contracts — 2026-10-23

The next migration identified in M143 is implemented: native and independent JSON
definitions bind IBusinessValuePolicyDefinition, IFixedResourceValueDefinition and
IProductionIncomePolicyDefinition. Sole shared declarations retain original validation,
quote arithmetic and primary-only eligibility. No prices, amounts, default mode flags,
financial rules, state codecs, physical work or account ownership change. Native2.769/
save161/replay2. Baseline101/0; final two exact cases155/0 and one complete replay;
actual Oil compile-only boundary and fresh450source/PDB/138call census are recorded in
[current evidence](../../docs/migration/P4_PRODUCTION_INCOME_VERIFICATION.md#current-m144-direct-valuation-and-income-contracts--2026-10-23).
No full/profile/family execution. Original221/210 source-recorded checked/11 open and
broader provider/accounting/durability gates remain; M143 evidence below is historical.

## Current M143: direct account-definition contract — 2026-10-23

Scoped verified86/0 on2.768/save161/replay2. Earlier2.767 reproduces1/1: a non-friend JSON Def implementing existing
IBusinessAccountDefinition is loaded correctly, but TycoonModule requests the abstract
BusinessAccountsDefinition and refuses colony.accounts. This contradicts the existing
open account contract; no account/business-rule defect is inferred beyond composition.

Mechanically select IBusinessAccountDefinition, delete the mandatory abstract base and
let native BusinessAccountPolicyDef implement the existing interface directly. Keep
the same validation, currency, opening amount, optional mode flags, sole BusinessAccounts
instance/aliases/codec and all charge/income/trading rules. No compatibility class,
adapter, new account service or state model. Bump native build2.768.0, save161/replay2
unchanged; older native saves remain strictly incompatible, never header-rewritten.

Extend the existing composition proof with one explicit exact case and a tiny external
content-only definition: public JSON reader/linker and metadata, no balances or gameplay.
Reuse the existing master/factory/account data; no new fixture ownership or copied flag
definitions. Verify real public policy/balance/currency/privacy, absent-money modes,
saved twins/isolation and invalid currency/opening definitions. Only the new exact case
and existing physical budget case; explicit profile row metadata, no full/profile run.
The account-only proof does not qualify the remaining valuation/income mandatory-base
paths or all financial provider/fault breadth; those remain separate actual follow-up.

Final independent account22/0 and existing physical budget64/0 span explicit test/proof
images with identical repaired native code. All five runs/four test builds/one reference
compile are retained. The budget fixture needed its explicit staffed executor plus a
noncrossing second-worker startup60 instead of0; no amounts, speed, durations or bounds
changed. Do not reintroduce the deleted account base or rerun those green cases after
unrelated test/docs work. Next migrate valuation/income selection through content contracts.
[Current evidence and limits](../../docs/migration/P4_BUSINESS_MODE_COMPOSITION_VERIFICATION.md#current-m143-direct-account-metadata-contract--2026-10-23).

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


## Authoritative ownership revision,2026-10-07

All native economics belong to ONE optional `TycoonModule` (`sim.tycoon`), including basic
fixed values/accounts and advanced activation. Directly replace the old module identity;
no alias, fallback economy or second module. Move native supplier and monetary requirement
declarations out of LogisticsModule/WorkModule. These physical modules retain no dependency
on Tycoon. Native economic authoring selects its owning module; all-off selected Tycoon is
dormant, not a free ledger. Unpriced physical games still omit it entirely.

Haul purchase admission and direct supply-receipt execution are callers, not two economic
systems. Both use the same Tycoon-owned trading rule; disabled trading cannot reserve a new
haul or generate purchased goods. Preserve the single existing supplier receipt book,
resource transaction owner, ledger and real physical execution. Advanced activation and
production settlement remain separately unfinished until their actual integrations are proven.

Consolidation gate: one exact bounded ownership/physical scenario, one completed-slice build;
prior reports remain historical, not automatically revalidated by file moves.

Date: 2026-10-07. Status: partial; native classified work/recipe gates implemented/focused-verified.
Current2.209.0/save113/board40/work40/work view73/transformation view9/account3/business view3.
One clean build/one exact new scenario105 checks0 failures/aborts364ms.
[Current evidence](../../docs/migration/P4_MONETARY_MODE_GATES_VERIFICATION.md).
Historical composition2.208.0/88 checks was not rerun. Native work costs require explicit categories;
disabled positive costs have no account requirement/binding/payment. Materials, machines and game time
remain real. Priced trading, ongoing monetary rates/budgets, public provider/debt/fault/cancellation
matrices, mobile construction/hauling and H2/H3/full I4 remain open. Unsupported income/trading/Tycoon
activation currently refuses. Global scenario-profile migration remains.
One additional exact scenario proves unpriced timed resource work/manufacturing with no selected
business/Tycoon service:27 checks,0 failures/aborts. [Evidence](../../docs/migration/P4_GAME_MODE_CONFIGURATION_VERIFICATION.md).
That earlier unpriced gate is historical; it does not prove current priced-supplier or income/advanced-provider tasks.
Parent: [Game modes](P4_GAME_MODES_DESIGN.md). Depends on [I2](P4_GAME_MODE_CONFIGURATION_DESIGN.md).
Integrates existing [H1](P4_BUSINESS_DEFAULTS_DESIGN.md), pending [H2](P4_PRODUCTION_INCOME_DESIGN.md)
and [H3](P4_TYCOON_MODE_DESIGN.md). Next: [I4](P4_GAME_MODE_VERIFICATION_DESIGN.md).

## 0. Problem statement

Work and recipe costs now use the shared effective rule at their existing actual transaction boundaries.
Priced supplier pickup remains pending. Disabling a UI field or not
constructing accounts is insufficient when selected content still contains monetary requirements.
Conversely, turning every monetary requirement into a free purchase would fabricate physical stock.

## 1. Goals

Work/manufacturing availability is NOT controlled by business/Tycoon mode. Only explicitly authored
monetary portions are affected by the economic flags. Physical materials, tools, machines, utility
inputs, movement, recipe/work dependencies and time stay under their independent simulation owners.

1. Survival construction/manufacture executes physical requirements over time with monetary gates off.
2. Values-only mode has quotes but no ledger; accounts-only has balances but no automatic income.
3. Enabled monetary requirements use the same actual account/requirement/transaction authorities.
4. Disabled trading cannot generate supplier goods, accept monetary orders, or mimic free accounts.
5. Preview, actual commands, autonomous decisions, views and load use the same effective rules.

## 2. Design

### One interpretation, existing execution owners

Implemented `IBusinessModeRules` and one immutable `BusinessModeRules` projection over
`IGameModeConfiguration`. It validates the eight known economic settings and prerequisite matrix
once. It contains no accounts, stock, reservations, policy tables, event handlers or update loop.
No generic scheduler branches on `ModeId`, concrete Tycoon classes, or raw JSON flags.

`TycoonModule.Compose` binds providers only for explicitly enabled capabilities:

| Effective configuration | Composition and runtime behavior |
|---|---|
| All economic flags false; module omitted | No economic service, currency coverage, buyer or seller required |
| All economic flags false; module explicitly present | No ledger, valuation, income or advanced systems; dormant parameters may be absent |
| Values true, money false | Required valuation policy/catalog; quotes only, no IEconomy/IBusinessAccounts |
| Money true, income/costs/trading false | One explicitly configured account owner; no automatic charges/rewards |
| Income true | Same accounts plus actual eligible-production settlement policy and transactions |
| Cost feature true | Same accounts; only corresponding explicitly authored costs apply |
| Trading true | Actual selected purchase/sale providers and money; no invented trader |
| Tycoon true | Actual advanced rules/provider via existing contracts; no fixed-policy fallback |

The eight flag IDs are defined in the parent document. Boolean prerequisites are authored in
property definitions; semantic provider requirements are also validated by the responsible module.
Money requires explicit account initialization/currency. Values require explicit unit-correct policy.
Accounts alone must not require a valuation catalog: decouple `BusinessAccounts` construction and
snapshot provenance from a compulsory `BusinessValuePolicyDef`. Account currency/account policy
are separate from optional resource valuation; do not invent a dummy valuation to create money.

Refactor current business settings in place. They retain provider parameters such as account
currency/opening cash, valuation policy and income-policy identity, **not another set of enabled bits**.
Selected profile configuration is the sole enablement authority. Valuation provider identity selects
fixed defaults versus advanced rules; central `business.tycoon.enabled` must agree with that provider.
Remove the overlapping module-level `mode` switch when replaced. No legacy aliases or dual reading.
Current native composition removes that switch and inline account parameters. `business_accounts`
definitions implement IBusinessAccountDefinition through the open BusinessAccountsDefinition base;
BusinessAccountPolicyDef reads explicit currency/opening_cash. Settings name `account_policy` and
`valuation_policy` independently. Account snapshots/codec identify the account definition, not a
valuation mode. BusinessView distinguishes provider presence and unavailable quotes/mode.

Dormant optional settings may be omitted. If authored, validate their JSON shape; do not resolve
inactive resource catalogs/providers as mandatory dependencies. A true flag with missing parameters
refuses composition. A module's settings cannot secretly override a false mode property.

### Monetary gates must not change physical requirements

Extend existing `IWorkCostRequirement` interpretation/admission, not the activity DAG scheduler.
An explicitly disabled monetary requirement is reported as **not applicable under the selected rule**;
it is not a paid cost, successful budget reservation, zero cash amount, or acquired IEconomy handle.
Other material, fluid, tool, technology, area, provider, site, machine and dependency requirements
continue normally. Work progress, real movement and completion are unchanged.

Map cost categories explicitly. Construction costs and manufacturing costs are separate properties.
Native work_cost_requirements now require category: construction, manufacturing or running, exposed
through IWorkCostRequirement.CostCategory. Unclassified/unknown categories refuse, never guessed from
mode name. Source audit established that existing credit:true means debt-funded spending via Charge,
NOT a reward; that debt permission is preserved and obeys the same cost-category gate. No work reward
capability currently exists. Any future reward needs explicit content/contracts and cannot be inferred
as resource production income. Running-category work remains an authored startup obligation; it is not
a new per-time monetary rate. Ongoing-rate integration remains separate and unimplemented.

BusinessModeRules.EvaluateCost is the single decision entry point for requirement, recipe and view.
It uses the immutable bound rule contract when selected, or reads the same typed frozen configuration
when business is omitted; no copied flag table, fake provider or mode-name fallback. No JSON is read
per Update. MonetaryCostDecision reports authored amount/category and NotAuthored, DisabledByMode,
Enabled or UnavailableCapability. A disabled decision has no effective payable amount.
WorkBoard binds/spends only
applicable choices using its existing cost bindings and account tokens. There is no duplicate budget
queue, free-money provider or balance copied into a job. When enabled, refusal/spend/cancellation timing
and future reservations follow H2's account-owner design. Mode disablement does not refund existing
obligations because modes are immutable for a session.

Process `TransformationDef.Cash` is a manufacturing-cost declaration. Existing TransformerManager
uses the same typed effective monetary-cost rule for check and commit. Running monetary costs, if
present in selected content, require their own declared category and gate. Physical fuel, water,
power, upkeep materials and worker service intervals remain active independently.

### Priced supply is not production

Current `SupplierPort` checks/charges money at real pickup. Trading-off excludes priced purchase
offers and refuses explicit buy/sell commands before money/stock mutation. It never strips a supplier's
price and then consumes limitless vendor inventory. Owned/unpriced physical hauling remains legal.
An authored supplier component can remain dormant and unavailable to selection; composition must
validate its content without requiring an economy solely for disabled trading. If a selected workflow
requires that unavailable supplier, expose the typed blocker; no substitute provider or instant delivery.

Income-off binds no production settlement. Income-on is not considered implemented until H2 wires
the existing resource commits and proves exact eligibility/account effects. Current primary-only
income scaffolding does not prove consumed-input processes, mined/transferred stocks or recycle
policies. Never enable an advertised flag whose actual commit boundary does nothing.

### Broader extension boundary

Each future capability registers its property schema/content and consumes the same configuration
contract through its responsible engine provider. A new pressure profile can be data-only; a new
mechanic still requires an engine-side implementation and proof. No central genre switch statement.
Do not expose unimplemented fuel-free, instant-build, technology-unlock, damage-free or weather
toggles as supported. Mode values are not permissions to bypass conservation/safety contracts.

## 3. API surface

Proposed `IBusinessModeRules` exposes immutable typed enablement/monetary category decisions and
provider requirement diagnostics. Its sole implementation resolves the configuration, not JSON per
Update. Actual balances retain IEconomy/IBusinessAccounts; values retain IResourceValuation;
resource commits retain IResourceTransaction; workflow checks retain IWorkRequirement contracts.

Existing views gain explicit `DisabledByMode`, `UnavailableCapability` and `Enabled` facts where
applicable, plus authored versus effective costs. Do not show no-account as a zero balance or a disabled
budget as successfully spent. No direct public flag setters. Previews use the same engine decision.

## 4. Files to change

| Action | Existing/proposed owner under `src/Beep.SimEngine` | Risk |
|---|---|---|
| New | `Standard/Tycoon/BusinessModeRules.cs`: one contract projection/semantic validation | medium |
| Modify | `Standard/Tycoon/TycoonModule.cs`, `BusinessAccounts.cs`: optional composition/decoupled account parameters | high |
| Modify | `Standard/WorkRequirements.cs`, actual WorkCost definitions, `WorkStart.cs`, `Work.cs` | high |
| Modify | `Standard/Transformations.cs`: effective manufacturing cost admission/commit | high |
| Modify | `Standard/Logistics.cs`: trading-disabled offer/pickup/command enforcement | high |
| Modify | Existing business/work/transformation/logistics views | medium |
| Modify | H2 resource commit owners only for actual income integration | high; H2 dependency |
| New/Modify | Framework public proof JSON and one exact monetary-mode scenario | medium |

Audit actual cost/reward providers before editing. Do not invent a parallel WorkCost manager or modify
OilFieldDays/hosts/tests to implement these gameplay decisions.

## 5. Error handling matrix

| Condition | Engine outcome |
|---|---|
| Monetary building/manufacturing gate off | No monetary check/charge; other real requirements still enforced |
| Income/budget/trading true but money false | Configuration refusal, not dependency auto-enable |
| Enabled accounts with missing currency/opening parameters | Composition refusal |
| Enabled values/income/Tycoon with unavailable policy | Composition refusal, no fabricated fallback |
| Trading off with explicit purchase | Typed disabled refusal, stock/account unchanged |
| Enabled monetary cost, insufficient balance | Existing cash/budget blocker; no partial physical acquisition |
| Provider changes state during pure preview or violates exact commit | Existing deterministic fault boundary |

## 6. Compatibility

Direct JSON/settings/schema migration; delete superseded enablement switches/readers. Preserve
unrelated dirty changes. No fake FreeEconomy for survival, zero-value catalogs, wrapper economy,
duplicated value tables or legacy fallback path.

## 7. Verification

Executed one exact new bounded scenario: `Game modes: survival physical work and independent money rules`.
105 checks pass using ordinary public orders/views for timed local material work and actual machine
manufacturing. All three native work categories: disabled without accounts, enabled shortage, actual
one-time payment and saved continuation. Accounts-only work never automatically charges. Recipes
preserve positive authored cash, actual WIP/resources/time/entity output and saved twins with costs
off/on; cash shortage has no WIP/output. Real material shortage still blocks with money off; missing
or unknown categories refuse. One clean build, one run, no repairs/reruns.
Broader hauling/mobile construction, public cost-provider/debt/fault/cancellation matrices, ongoing
monetary rates/budgets, trading-off supplier refusal and H2/H3 gates remain separate and open.
No full/family/older suite run; no broad batch disguised as focused verification.

## 8. Risks

Most important: a disabled-money flag can accidentally create free supplier goods, bypass physical
resources, or leave a hidden cash check. Audit both composition and actual check/commit paths.
Do not confuse monetary budgets with stock reserves, vehicle fuel budgets or route travel costs.

## 9. Out of scope

New economic mechanics, automatic selling, live mode switching/refunds, free stock, instant work,
platforms, Draw, extra Update loops or clock owners.

## 10. Sub-task execution order

- [x] I3.1 Centralize optional-business rule interpretation; JSON account definitions decoupled from values;88-check composition gate.
- [x] I3.2 Migrate module enablement to resolved configuration; old `mode`/inline account reader removed; dormant/values/money matrix verified.
- [x] I3.3 Native cost classification and applicable-work gating;105-check native proof. Credit is debt spending, not reward; broader public/debt/fault matrix open.
- [ ] I3.4 Manufacturing startup gate historically verified105; trading-off gates currently verified81 after Tycoon consolidation. Enabled trading and ongoing monetary rates/budgets remain.
- [ ] I3.5 Historical provider-presence88/current effective-cost diagnostics105 verified; broader hauling/mobile construction/public provider matrix remains.
- [ ] I3.6 Integrate H2/H3 when their own transaction/provider gates are complete; do not imply completion.
