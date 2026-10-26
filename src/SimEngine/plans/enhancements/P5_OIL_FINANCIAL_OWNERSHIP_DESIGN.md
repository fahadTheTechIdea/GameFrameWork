# P5-A15 — Sole financial accounts and authored Oil modes

## Problem, scope and approval

OilModule still bound its own Companies ledger or a deleted FreeEconomy fallback. This contradicted
optional, engine-owned Tycoon accounts and caused the last OilCo.Game compiler error.
The human explicitly authorized financial ownership migration, preserving authored amounts.
This expands the earlier optional-cost-binding permission; it does not authorize changed pricing,
free resources or new financial formulas. No compatibility layer, platform, Draw or client gameplay.

## Stage 1 — Remove duplicate balances

Companies is now only an IPersistentService for Oil standing and physical/statistical history.
Its IEconomy implementation, Cash balance, Spent ledger and charging/crediting methods are deleted.
OilModule no longer binds an economy or initializes cash. Companies codec becomes3; no old-save shim.
Core build2.540.0, save154/replay1. The sole account/budget provider remains optional Tycoon BusinessAccounts.

CompanyReport contains an optional actual BusinessAccountSnapshot; Cash is nullable and derived.
No account is not a zero balance or infinite money. CompanyView enforces the same private company gate.
Campaign cash measurements are nullable when no account exists. Insolvency is inactive without money.
Autopilot cash-reserve conditions do not block physical work in money-off modes.

## Stage 2 — File-owned game modes and accounts

game-modes.json declares eight Boolean capability properties, six game modes and six time definitions.
All defaults are explicit false. Monetary modes select money, ordinary construction/manufacturing/running
costs and trading; they do not silently install advanced Tycoon or production-income formulas.
Sandbox and gulf have no financial capability, no Tycoon selection and no account policy.

business-accounts.json belongs to optional sim.tycoon through the master manifest. Original openings:
frontier6000000, adventure3000000, continent6000000, showcase8000000.
The old settings.oil.start.cash and free_economy fields/readers are deleted.
Each scenario explicitly selects its game mode/time and, if monetary, its Tycoon account policy.
The original ten-minute update quantum is authored as GameDuration data, not a second clock.

## Stage 3 — Route existing callers to selected contracts

Research admission/spending uses the existing BusinessModeRules manufacturing-cost gate and existing
IEconomy; a changed payment after successful admission is fail-stop. Existing research amounts remain.
Power fuel is still physically consumed through IResourceTransaction; its cash cost and daily upkeep
are gated by the existing running-cost decision. Buyers credit only when ordinary trading is selected.
The redundant fixed-price interface/wrapper is removed; existing per-buyer authored amounts remain.

These are intermediate domain callers, not completion of all-business-in-Tycoon. Buyer/upkeep component
definitions, sale settlement/statistics, research monetary annotations and power monetary annotations
still require Tycoon-owned authoring/operation bindings. Move them in the next consolidation stages,
preserving actual physical outputs, amounts, timing and the original transaction owners.

## Stage 4 — Focused validation

One NEW exact test reuses the prior native fixture compiler and links canonical Oil mode/account JSON.
It checks all six mode selections, original opening amounts, actual currency/policy identities, normal
budget capability, absence of advanced Tycoon substitution, a supplied physical road, original1500
charge once, saved twins, no leftover receipts and isolated sessions. It is not the full Oil module.

Targeted OilCo.Game and framework test builds must compile. No older/family/full test suites run.
Full Oil strict entity/work/production/settings authoring, actual initialization, unsurveyed drilling,
company views/campaign/autopilot financial behavior and broad durability remain open.
Keep all original acceptance checkboxes open where previously open; commit/push waits for full goal.

[Evidence](../../docs/migration/P5_OIL_FINANCIAL_OWNERSHIP_VERIFICATION.md)

## Time representation correction — P5-A17

The ten-minute quantum described above was incorrect for complete Oil composition: existing5/15/45/50-minute offsets require minute coordinates. Canonical definitions now use one-minute simulation quantum and ten updates/frame on the same clock. A15's fixture evidence did not prove these schedules. See [correction](P5_OPERATION_RUNNING_COSTS_DESIGN.md).

## Current ownership consolidation — P5-A25

The research intermediate caller described in Stage3 is now removed: optional Tycoon action-charge
JSON/IBusinessContentCharges and the original account's once receipts own every monetary decision.
All16 amounts/category/reason are unchanged; physical technology/progress has no payment history.
The original market prototype/prices/placement intent and its composed visibility rule are selected
with optional Tycoon; resource-only Oil startup creates neither market nor account.
FINAL49/0/0 proves actual small-world paid switching/saved resume, money-off study, insufficient cash,
private charge facts and optional visibility-binding refusal. Sales/power/paid-supply/host migration
and broad acceptance remain open. See [stage](P5_TYCOON_RESEARCH_CHARGES_DESIGN.md) and
[evidence](../../docs/migration/P5_TYCOON_RESEARCH_CHARGES_VERIFICATION.md).

## Current sales consolidation — P5-A26

Both unchanged terminal price tables and original hour/day offsets are now Tycoon-owned JSON.
The old Buyer/sale Take-Credit path and Oil revenue state are deleted. Existing ResourceTransactions
owns Sold disposition; original BusinessAccounts owns settlement and financial reporting; Oil keeps
only committed physical quantities and notes. Nullable views/campaign reference actual sales data.
FINAL92/0/0 proves both canonical terminal components/prices through authored starting scenarios,
actual timing, saved twins, next daily closing, private facts, money-off stock retention and no-power
refusal. Three outer timeouts/internal stage failure are retained; bounded stages now use actual time.
Power/upkeep charges already reside in optional Tycoon; physical fuel stays in the resource owner.
Paid-supply/provider/fault/journal/old-host/full physical/private/rival/durability acceptance remains
open. See [stage](P5_TYCOON_RESOURCE_SALES_DESIGN.md) and
[evidence](../../docs/migration/P5_TYCOON_RESOURCE_SALES_VERIFICATION.md).
