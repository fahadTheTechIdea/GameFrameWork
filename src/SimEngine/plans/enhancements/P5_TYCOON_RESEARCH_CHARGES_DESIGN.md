# P5-A25 — Optional Tycoon-owned content action charges and research costs

## 0. Problem statement

Oil physical technology definitions own cash, and StartResearchHandler evaluates/charges that money.
Paused physical progress is also used as payment history. Financial ownership must be optional Tycoon,
not physical research or hosts.

## 1. Goals

Move all16 original research amounts unchanged into optional JSON charge definitions. Preserve the
existing manufacturing-cost enablement category and charge only once per company/study across
switching, save/load and resumption. Resource-only research must require no money or fake ledger.

## 2. Design

IContentActionChargeDefinition declares an action, actual target-content reference, positive amount,
category, reason and once-per-company policy. Native ContentActionChargeDef is the sole JSON reader;
independent providers can implement the same contract. ContentActionCharges selects immutable metadata,
validates duplicate action/target bindings and performs existing BusinessModeRules admission.

The existing BusinessAccounts remains the sole balance and persistent financial owner. A partial
source file adds sorted once-charge receipts, validated against selected definitions on load. No new
ledger/persistent service. Disabled or unconfigured monetary intent does not create an account.
Read-only BusinessView exposes an authorized detached charge/cost/already-settled fact.

Oil tech.json contains physical points/prerequisites/unlocks/effects only. business-research-costs.json
belongs solely to optional Tycoon in the master. StartResearchCommand keeps physical checks and delegates
optional monetary admission/settlement to IBusinessContentCharges; Research owns progress only.
IsPaid and physical Cash/ledger reason are removed; TechView.Cost is nullable module-owned metadata,
not a fabricated zero-price or compatibility Cash property. Old hosts/test callers need separate API
migration and are not claimed compiled or runtime-verified.

Resource-only resolution also requires optional market ownership: the unchanged supply_town prototype,
ten supplier prices and placement distances live in Tycoon-owned business-supply-towns.json.
IOilSupplyTownDefinition owns only placement intent. Mandatory Oil rules no longer name a market.
The same optional document attaches the town visibility rule to the existing policy through
IKnowledgeVisibilityRuleBinding. One visibility owner merges canonical rule lists, refusing missing
references and duplicates; there is no copied policy or missing-reference fallback.

Core2.545.0/save155/replay1, account codec5 and BusinessView9 reflect the new persistent receipt layout.
Independent protocol header updated from its stale2.537 baseline; other codec expectations remain
historical/unvalidated, not silently claimed current coverage.

## 3. API surface

IContentActionChargeDefinition/IBusinessContentCharges, ContentActionChargeView, BusinessView.ReadActionCharge.
IKnowledgeVisibilityRuleBinding and IOilSupplyTownDefinition keep optional authoring references
selected with their provider. Existing public research commands/views and engine account aliases;
Oil research delegates financial admission/settlement only. No direct host state writes or new
physical scheduler/time mechanism.

## 4. Files

New Tycoon charge declaration/service and existing-account receipt partial; existing account/module/
business view/engine protocol marker; original visibility policy plus one rule-binding reader.
Oil research/command/view/tech/master plus optional cost/market JSON, placement contract, rules/visibility.
One new focused public Oil case in the existing public integration project. Stage/evidence/master/audit.

## 5. Errors

Missing target, duplicate bindings, nonpositive/invalid metadata and bad receipt identity/order refuse.
Enabled costs without accounts retain unavailable-capability refusal; unavailable funds refuse without
charge. Foreign/private views remain cleared/refused. No invented account or zero-price default.

## 6. Compatibility

No compatibility state reader or Cash API shim. Old save/peer identities are not accepted as current.

## 7. Verification

One new bounded actual public Oil case covers two first-start charges, switching/resume without a
second charge, saved paid receipts/twin continuation, unaffordable refusal, private views and resource-
only research, optional market presence/absence and missing/duplicate visibility binding refusal.
FINAL49/0/0 in7942ms/observer8115ms. Two earlier failures and an intermediate43/0 are retained in
[evidence](../../docs/migration/P5_TYCOON_RESEARCH_CHARGES_VERIFICATION.md).
Canonical domain modules/components retained with only explicit small fixture inputs.
Not completed physical research/technology effects, all16 runtime studies, arbitrary mod/fault/replay/
lockstep/whole-engine/old-host acceptance.

## 8. Risks

Account receipt persistence needs strict target-definition/canonical ordering. Physical command
failure after successful payment remains under original command fault semantics; comprehensive fault/
transaction guarantees stay open. Research category remains Manufacturing intentionally, not a new
business rule. Sales/power and whole Tycoon ownership are not declared complete by this stage.

## 9. Out of scope

Oil sales migration (next), full regression, hosts/demos/platforms/Draw, advanced Tycoon rules, changed
amounts, duplicate ledger/clock/research progress or compatibility paths and Git writes.

## 10. Execution stages

1. Add generic optional action-charge contracts, actual metadata and native account receipt persistence.
2. Migrate all16 fees and physical API callers, preserving amounts/category/once behavior.
3. Remove mandatory physical catalog market references, select optional town/rule-binding authoring.
4. Compile and run only the new affected public case; retain every repair/failure.
5. Update evidence/protocol/tracker/audit without closing original broad gates.
6. Continue sales consolidation, older paid-supply/host API callers and full physical/private/durability
   acceptance through engine commands/views.
