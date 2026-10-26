# P4-SD1 — Demand-owner disclosure contracts

## 0. Problem
A visible destination is not enough authority to publish a demand: its authored workflow or recipe may be undisclosed. Current quantities/ages/classification and combined stock/offer/diagnostic totals can leak through a visible stock requester even when another requester is private.

## 1. Goals
1. Every ILogisticsDemandOwner must declare source disclosure through a required pure method.
2. Existing activity catalog and transformation execution disclosure own their respective metadata closure; logistics delegates, never reconstructs workflows.
3. Individual rows and whole aggregated offers/stock targets/diagnostics with private participating sources are withheld.
4. Original physical CheckSource, DemandActive and supply/matching/progression remain independent of player disclosure.
5. Prove native work and native/independent recipes with visible destinations/resources, revoked knowledge, visible stock co-demand, actual hidden supply/consumption/completion, save/load/twins/replay/isolation.

## 2. Design
Add CheckSourceDisclosure(viewer,source,company,destination) to original demand-owner contract and source service. Add CheckDemandDisclosure(viewer,destination,resource,endpoint?) on the same service to visit all matching original demand rows. Guard owner callbacks, accept only None/NotFound, never introduce copied demand/visibility state. Stock delegates current entity sight; work delegates IActivityCatalog.CanDiscloseActivity; transformer delegates TransformationContracts.CanDiscloseExecution; external proof uses public authorized entity/catalog/resource facts only.

Existing LogisticsView demands check owner and resource metadata before source facts. Original queue selection remains physical; whole derived offers with undisclosed contributing sources are omitted, not reselected. Rich diagnostics and explicit stock targets check whole matching-demand disclosure before publishing quantitative facts. Route resource metadata uses existing content sight. No changes to physical scheduling or game clock.

Runtime proof exposed accepted recipes stuck Supplying/UnknownDef after actual delivery because per-update access reused metadata admission. Original ContentAccessPolicy now binds a segregated IContentOperationAccess and funnels both purposes through one CheckCore; only metadata projection differs, real basis/rules remain evaluated. Accepted native transformation execution uses operation eligibility; fresh player queues retain metadata admission. No waived gameplay rule or instant fallback. Completion diagnosis adds an explicit company observer query on existing WorkView/history rather than relying on pruned terminal orders.

## 3. API
Required demand-owner/source-service methods; small IContentOperationAccess on the original policy owner separates operating eligibility from player metadata. Company-qualified completion query uses original history; normal reader delegates the same evaluator. Core2.518-to2.519/logistics47-to48/work102-to103, save154/replay1/physical codecs retained. No default implementation, second state owner or compatibility shim.

## 4. Files
Original demand contracts/service/work/transformer/views/modules/content-access/identity/protocol, existing external demand proof implementation and explicit embedded resource registration, JSON-only recipe fixture extension, new authored disclosure document/thin driver/profile entry and trackers. No new production state class.

## 5. Errors
Private/unknown demand owners and metadata are unavailable, not zero demand or an invented empty snapshot. A provider returning another code faults the observation contract. Foreign or unseen destinations refuse before the owner callback. Observer diagnosis retains actual physical facts.

## 6. Compatibility
Development-only direct contract revision; all implementers migrate, no fallback.

## 7. Verification
One completed-slice Release build and bounded new exact case; same-case repairs only. All rules remain engine/framework/module side; driver only JSON, normal inputs, Update and immutable observations. No full regression until implementation finishes.

## 8. Risks
Visible co-demand must not expose another requester's quantities. Revocation must not stop original physical materials/manufacturing or reset request ages. Do not count retained tools twice or infer complete private/global gates from this slice.

## 9. Non-goals
All indirect unselected candidates/endpoint/control/content matrices, arbitrary owner faults/source providers, external masked dispatch, utility/incident/disposal/fuel/lockstep/performance/global/full acceptance, Tycoon/Oil/platform/Draw.

## 10. Order
Extend original required contracts/implementers; wire aggregate disclosure; author work/recipe revocation fixture; build/exact case; refresh evidence/master and preserve broad open gates.

## 11. Scoped result

FINAL Release394/0/0 in4450ms/PID23692/Exited0; FIRST PASS386/0/0, eight failed same-case attempts retained. Solution17.05s/nine focused repairs clean. Six minute/half work/native/independent recipe flows prove private combined supply facts, actual hidden supply/consumption/completion and new-intent restrictions/saves/twins/replay/rediscovery/isolation. Core2.519.0/save154/logistics48/work103. Original221/193checked/28open unchanged/additional251; profile-info719/0runtime/no full executor. [Evidence](../../docs/migration/P4_DEMAND_DISCLOSURE_VERIFICATION.md). Broad provider/fault/real technology policy/indirect disclosure/performance/global/full goal remains open; final regression deferred.

