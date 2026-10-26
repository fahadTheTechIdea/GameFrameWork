# P5-A26 verification — Optional Tycoon resource sales

## Result

Core2.546.0/save156/replay1; BusinessAccounts codec6/BusinessView10, Oil companies codec4/
CompanyView3, resource-flow event6/view8. FINAL92/0/0 in261513ms, observer261643ms,
PID46412, exit0, stdout/stderr untruncated. One new affected public Oil case only.
No family/full/older suites, staging/commit/push or executable edits after FINAL.
[Design](../../plans/enhancements/P5_TYCOON_RESOURCE_SALES_DESIGN.md).

## Authoritative implementation

The original export_terminal/harbor_terminal tables (seven prices each) move unchanged from
BuyerDef into optional Tycoon-owned business-sales.json, listed once in the master.
Hourly minute30 sales, ledger.sales reason and daily minute30 reporting are explicit JSON.
Physical terminal definitions have no buyer/price component.

IResourceSaleDefinition/IResourceSaleEligibility bind actual immutable target/resource/rate and
domain operation checks. ResourceSalesSystem interprets schedules only; it owns no money or stock.
ResourceTransactions owns Sold disposition and prepares the account participant before stock changes.
BusinessAccounts owns balance settlement and total/current/previous revenue.
Production and sale participants share IResourceInventoryCommit.
No direct Oil Take/Credit sale path, second ledger, legacy Buyer reader or compatibility money state.

Sold is distinct from production income, consumption, destruction and pollution.
The existing resource-flow journal accepts outgoing facts and exposes SoldRaw in detached totals.
This public Oil case does not select the journal module; journal fault/replay acceptance remains open.

Oil owns physical sale quantities from declared committed events and the original first-sale note.
Events dispatch after systems: quantities on a reporting minute join the already-closed window,
preserving old sale-before-day-close behavior. Company/campaign revenue reads actual optional sales
snapshots. Missing money is absent data, never zero revenue or unlimited income.

Immutable cost/sale/report schedules compile once from selected content. Not-due rules skip needless
observation; due affordability, operation and stock checks remain live. No additional clock/calendar/
saved timer, changed formula or instant operation. No performance-budget completion claim.

## Focused command and coverage

```powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Oil public sales: optional account ownership actual terminal timing and saved continuation" --timeout-ms 360000 --termination-timeout-ms 5000 --output-chars 100000
```

Actual canonical Oil modules and both original terminals/prices execute. Explicit fixture inputs:
24x24 frontier/sandbox, one human, terminal as authored founding HQ, no starting vehicles,
founding radius100 and starting kit two crude/five gas. The power negative adds actual power_use
without a source. Normal placement previews/FoundCompanyCommand create the terminal/initial stock.
No direct World/stock/balance write, fabricated production or instant construction fallback.
This is not full physical terminal-project construction proof.

Both terminals verify: initialized stock produces no income; no sale before minute30; exact stock
removal; original crude60/62 and gas18 give210/214 payout minus unchanged scheduled terminal upkeep;
saved twin identity before/after sale and continuation; no second sale from empty stock; first and
next actual24-hour closes preserve revenue/physical windows; foreign facts refused/cleared.
Resource-only Sandbox retains both stocks, has no money/sales snapshot, and saves identically.
Unpowered terminals retain stock, earn no revenue and report actual no-power blockers.
Duplicate targets and missing eligibility metadata refuse before play.

Five daily-advance stages target400/700/1000/1300/1480 on the original minute clock, each with
35-second bounds and tick/state diagnostics. No shortened day/clock jump/narrowed report assertion.
The FINAL whole case completes within360seconds; this is not a throughput benchmark.

## All attempts and repairs

Core compile initially failed two type/API references,4.84s; event string identity and common
commit out type were corrected. Subsequent public builds compiled framework dependencies.
Public builds:1 failed missing empty Def.ReadCore,3.71s;2 clean1.91s;
3 failed two incorrect public lookup names,3.23s;4 clean1.67s;5 clean2.96s;
6 clean2.80s;7 clean1.42s;8/FINAL clean1.28s. Clean builds zero warnings/errors.
No full solution/older host/test-project claim.

Same affected case only:
1 timeout,PID32944,90062ms,exit-1, no terminal test report.
2 timeout,PID32324,90043ms,exit-1; diagnostics reached first sale/saved continuation.
3 timeout,PID25772,90039ms,exit-1; same daily advance.
4 internal timeout at tick540/target600, world Running:27/1 in44781ms,
observer44909ms,PID2944,exit1.
5/FINAL92/0/0 in261513ms,observer261643ms,PID46412,exit0.
All observer outputs untruncated. No abandoned live process or green inference from timeouts.
The no-sale-definitions guard changed while attempt2's old binary ran; rebuilt before later attempts.
Only documentation/audit changed after FINAL.

Two failed apply_patch validations used inaccurate schedule/document line contexts; neither changed
files. Some read-only diagnostics used incorrect paths/truncated output; no result inferred.
Duration objects were corrected to the existing rational JSON schema before runtime1.

## Remaining acceptance

Source inspection finds no Oil IEconomy implementation, direct sales Credit/Charge or buyer reader.
Physical power fuel remains resource-owned; optional power/upkeep charges remain Tycoon.
Paid supplier, advanced Tycoon, provider/fault/reservation/journal/replay/lockstep and full-size/
project/private/rival/old-host acceptance remain open. Prior slices are historical focused evidence,
not revalidated here or a whole-business-module green claim.
Original221 texts/checkboxes/line anchors retain193recorded checked/28open; additional290.
No original gate closed. Full goal/plan remains unfinished.

## Final metadata

2095 source anchors,13 quickstart anchors,1223 selected executable inputs: zero hash drift.
8692 local links: zero broken;18 master rows valid; external thin-driver boundary search empty.
All221 original texts/checkboxes/line anchors match. Git diff --check exit0 with34 existing
diagnostic/warning lines;3182 dirty framework paths, zero staged.
GameClock, supplier algorithm, valuation and account-definition policy hashes remain unchanged.
Authorized account/revenue and stock settlement changes are explicit in their current contracts/
codecs; no executable drift after FINAL. Full goal/plan remains open.
