# P5-A7: Company strategy contracts and construction-site facts

## 0. Problem statement

Oil Autopilot.cs uses retired stock/route/spatial APIs, guesses construction from a site: prefix, and caches the first company's catalogue/resources for later companies.

## 1. Goals

Use original engine commands and views; author policies explicitly; remove prototype-name inference and cross-company cached knowledge. Expose actual site facts from existing contracts. Retain incomplete industrial/Oil gates.

## 2. Design

Existing PlaybookDef implements ICompanyPlaybookDefinition with read-only collections and immutable CompanyBuildTarget. Explicit selected identity replaces first-entry fallback. Existing LogisticsDemandSettings/StockPolicyEntry own stock policy metadata; nine previous quantities become explicit equal minimum/desired/maximum bands with enabled=true, priority0, protected reserves. Route uses explicit enabled=true/batch60/priority0. No scalar-policy compatibility path.

Autopilot uses LogisticsView stock/route reads and previews, InventoryView compatibility, ordinary ConfigureStockPolicyCommand/AddRouteCommand, and TileSpatialSnapshot/TilePlacement arguments. No stock, travel or placement execution is copied. Each company refreshes catalogue/resource observations. Unsupported spatial facts are not converted. Scoring uses long intermediate arithmetic.

Original ConstructionView.TryReadSite returns ConstructionSiteSnapshot (site ID, target prototype, original order ID). Owned observation and target content visibility precede publication; actual IConstructionSiteDefinition is validated through ConstructionBindingContracts.ReadSite, and the actual provider is observed through existing EntityObservation. Refused outputs clear; provider disagreement faults. No new project/state/codec/queue owner. Core2.534.0/construction-view5; save154/replay1 unchanged.

Autopilot collects those facts per company and counts actual target definitions; no site: convention remains. Existing cash-reserve strategy remains pending optional-business migration, not silently changed.

## 3. API surface

ConstructionSiteSnapshot and ConstructionView.TryReadSite(EntityId, out ConstructionSiteSnapshot). Oil strategy contract ICompanyPlaybookDefinition; immutable CompanyBuildTarget. Missing/hidden/non-owned/non-site/undisclosed-target reads return false. No host/rendering API.

## 4. Files

Framework: Views/ConstructionView.cs, Standard/ConstructionModule.cs, Kernel/SaveGame.cs. Oil: Autopilot.cs, Settings.cs, content/oil/playbooks.json. New ConstructionSiteViewTests.cs, existing Program.cs/protocol baseline, tracker/evidence.

## 5. Errors

Invalid authored policy/target settings refuse; missing strategy never chooses arbitrary content. Hidden facts do not disclose order/target IDs. Refused previews never fabricate demand or delivery. Actual provider binding disagreement faults through original engine observation.

## 6. Compatibility

Development revision only: retired APIs removed in place, no shim or duplicate subsystem.

## 7. Verification

One focused framework test-project compile; ONE NEW exact case30/0/0 in629ms/outer912ms. Two scoped Oil compiles fail70 errors (2.28s,2.33s), reduced from85. Structural JSON/source checks do not prove Oil content loading/gameplay. No old/family/full runtime.

## 8. Risks

Oil still does not compile. Complete build/cancel/recovery, independent malicious providers, hidden target policies, faulted retained queries, replay/lockstep/stress and rival recovery require further focused validation. The new case covers ordinary native point geometry, not all geometry/providers.

## 9. Out of scope

Tycoon business-rule changes, clock changes, Draw/platform/Unity/Godot, host gameplay, full regression, old sibling engine, publication or commit/push before complete goal and plan.

## 10. Execution and outcome

Strategy/contracts and original engine views migrated; native site projection added; naming inference removed. Framework compile16.57s clean. Case30 passed/0 failed/0 aborted; Oil70 errors remain, zero Autopilot.cs diagnostics in final retained output. Nine stock policies/ten target entries structurally valid. No original gate closure.

[Evidence](../../docs/migration/P5_OIL_STRATEGY_CONTRACT_VERIFICATION.md).
