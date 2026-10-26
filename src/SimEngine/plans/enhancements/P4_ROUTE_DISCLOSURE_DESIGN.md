# P4-RV1 — Standing routes and logistics disclosure

## 0. Problem
LogisticsView.CopyRoutes/CopyDemands use company ownership without current endpoint sight. CopyQueue exposes selected private endpoints/carriers; operational diagnostics include all company carriers and shipments. StockTarget substitutes zero for absence. Original CheckRoute/CheckRouteSettings reach native providers before player sight; route edits lack typed previews.

## 1. Goals
1. Original route owner gates player add/edit before native binding and keeps physical binding validation sight-independent.
2. Owned cancellation remains possible for unseen or removed endpoints, retires future intent only, and shares a typed preview.
3. Aggregate rows with private endpoint/carrier references are withheld whole, not redacted or reselected.
4. Rich operational diagnostics with private references are unavailable whole; no fabricated reduced totals.
5. Replace StockTarget with explicit ReadStockTarget status and actual quantity.
6. Verify authored minute/half source/destination/carrier masks, saved refused edits, owned cancellation, physical delivery, twins/replay/isolation.

## 2. Design
Existing LogisticsService.CheckRoute/CheckRouteSettings use original PlayerView.CanObserveOwned for both endpoints before CheckRouteBinding. AddRoute and load validation keep private physical binding. CheckRemoveRoute checks healthy owned intent; absent/foreign is NotFound but no endpoint sight is needed. LogisticsView adds typed PreviewRouteSettings/PreviewRemoveRoute forwarding existing checks and gates route/demand row references. Original queue selection remains authoritative; projection withholds a whole private offer, never picks a replacement carrier. Operational snapshots are all-or-unavailable when their embedded references are private.

## 3. API
Remove StockTarget; ReadStockTarget(EntityId,ResourceId,out Fix) returns None only for actual visible owned store and valid resource. A real zero demand is distinguishable from NotFound. Core2.516-to2.517/logistics45-to46; save154/replay1 unchanged. Typed route edit/remove previews delegate original owner.

## 4. Files
Existing LogisticsScheduling, Logistics, LogisticsView, LogisticsModule, SaveGame/protocol. Mechanical affected old tests only; new thin native proof and explicit profile entry. No new production state owner.

## 5. Errors
Private route add/edit targets refuse NotFound before null settings/resource/port/capability access. Shape and native provider errors remain original. Own cancellation survives hidden/deleted endpoints; foreign/missing intent indistinguishable. Hidden snapshots are absent rather than empty/zero facts.

## 6. Compatibility
Development-only direct revision; no legacy overload, adapter or compatibility mapper.

## 7. Verification
One completed-slice Release build and one bounded exact new case; same-case repairs only. All existing physical selection/binding/timing/stock/receipt owners retained; JSON and normal player inputs drive the proof. Final full regression stays deferred.

## 8. Risks
Do not apply visibility to automatic dispatch, native endpoint validation or acquired deliveries. Do not change supply selection to hide a private winning candidate. Owned cancellations cannot revoke existing shipment receipts.

## 9. Non-goals
Complete content/workflow-source metadata authorization, arbitrary endpoint/provider/fault/fuel/recovery/disposal/network/private CLR/performance/global acceptance, Oil/Tycoon/platform/Draw. Their compound gates remain open.

## 10. Order
Revise original route admission and typed previews; revise private aggregates and explicit stock target; author physical save/replay proof; build/exact case; refresh evidence/master without broad closure.

## 11. Scoped result

FIRST/FINAL Release706/0/0 in2256ms/PID44512/Exited0; one clean solution8.17s. Six authored minute/half source/destination/carrier masks prove actual route/stock intent and acquired job/cargo, private referenced rows/whole diagnostics and saved refused controls, owned unseen cancellation, physical delivery/conservation/twins/replay/isolation. Core2.517.0/save154/logistics46. Original221/193checked/28open unchanged/additional249. Profile-info717included/0runtime; no executable edits after FINAL. [Evidence](../../docs/migration/P4_ROUTE_DISCLOSURE_VERIFICATION.md). Indirect private candidate/eligibility information and source/content closure, removed-endpoint/provider/fault/performance/global/full goal and final full regression remain open.

