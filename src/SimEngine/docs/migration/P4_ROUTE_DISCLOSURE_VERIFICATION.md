# P4-RV1 — Standing route and logistics disclosure verification

Date2026-10-13. Core2.517.0/save154/replay1/logistics view46. Physical codecs and sole clock unchanged.

## Original owners and revised public surface

Original LogisticsService.CheckRoute and CheckRouteSettings gate healthy/live/owned/current endpoint sight before resource/settings/native port callbacks. Commands and typed route previews delegate those same checks. AddRoute's engine-side physical commit uses the original private CheckRouteBinding, as do loaded route validation and physical execution. No player visibility enters automatic supply selection or acquired shipment progression.

Original CheckRemoveRoute requires healthy owned standing intent but does not require endpoint sight/existence; cancellation retires future dispatch only. Foreign/missing intent refuses NotFound. New LogisticsView.PreviewRouteSettings and PreviewRemoveRoute forward that original owner; observer previews use actual owner admission, not privileged input.

Existing CopyRoutes withholds entire rows referencing unseen/dead/missing endpoints. CopyDemands checks actual owned current destination sight before demand-owner facts. CopyQueue derives through the original guarded selector and then withholds whole offers whose explicit endpoint/carrier/recovery/route references are private. It does not choose a substitute visible carrier. Rich ReadDiagnostics snapshots are unavailable whole when their embedded offers/shipments/carriers/returns contain private references; no reduced totals or fabricated empty carrier list is returned.

StockTarget is removed rather than retained as a shim. ReadStockTarget returns explicit status and actual quantity: visible valid store with no demand is None/zero; missing/private target is NotFound; visible invalid resource is InvalidArgument. Four older caller files and two route-private assertion files migrate directly. No old registered case ran.

The aggregate projection uses original engine-derived facts under observation guards; it does not prove that unselected invisible candidates cannot affect indirect blocker/eligibility information. Arbitrary demand-source content/requirement closures remain unverified. Complete private-disclosure/fault/performance gates remain open.

## Build and runtime

One Release solution:

```text
dotnet build Beep.SimEngine.slnx -c Release --no-restore
```

Clean8.17s/0warnings/errors. The solution maps its unused quickstart module to Debug; core/testing/proof/tests compile Release. One source apply attempt failed on a guessed view-registration spelling before any file changed; inspecting the actual original registration repaired the source edit. No failed build or runtime attempt occurred.

FIRST/FINAL Release706passed/0failed/0aborted in2256ms, PID44512/process2438ms/Exited0; untruncated streams/no observer error. No executable input edits after FINAL.

```text
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Route disclosure: private aggregates and route controls preserve physical delivery and cancellation" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 24000
```

Profile-info metadata only717registered/included,0excluded/long markers,0runtime suites. Full executor not run.

## Executed proof

Six minute/half-minute point flows independently hide source, destination or carrier. The JSON-authored native worker and existing engine-side visibility module use real endpoint storage, stock policies, standing route, job/reservation books and travel. Thin proof only authors JSON, sends normal inputs, calls normal Update and observes public immutable facts.

Actual visible zero demand and private/missing intent differ by status. A normal player holds the carrier, adds an enabled eight-unit standing route, and declares an eight-unit destination stock band. Actual route/demand and stock intent are observed before the worker physically acquires cargo. The native derived diagnostic contains the actual carrier and acquired shipment.

After hiding, route/demand aggregates omit only private referenced rows; rich target diagnostics with private source/carrier/target return NotFound/null. Private stock availability precedes malformed resource. Route-offer rows are absent for unseen route endpoints even when the physical candidate refuses. Foreign aggregate queues are empty.

Typed and normal route add/edit/remove admissions match. Endpoint-hidden edits/adds refuse before null resource/settings/port details; carrier-hidden endpoint edits remain admissible. Owned cancellation remains admissible while unseen, foreign cancellation refuses. Actual owner/foreign refused inputs are saved in the pending inbox, loaded and executed; route identity/settings remain unchanged.

Owned normal cancellation removes the standing route without revoking the original acquired job. The existing stock demand and physical shipment continue under the sight mask: per-Update conservation, finite source20-to12, receiver8, empty carrier, saved twins and no instant transfer. Reveal restores complete diagnostics; retained original route/demand/stock facts remain detached. Final save/load, replay of actual controls/refusals/delivery and independent-session isolation pass.

Physical waits512normal Updates, outer90000ms/termination5000ms/output24000characters. Removal of actual endpoints, arbitrary native/external ports, fueled service/recovery/disposal and all indirect private candidate/control/content cases are source-only or unverified, not claimed runtime coverage.

## Remaining goal and authority

Original221 gates remain193checked/28open; additional249. No compound gate closes. Broad P4.014/P3.008/P11.033/P9/private-source/fault/performance/global/full acceptance remains open. All12Tycoon sources and sole-clock hash unchanged; no Oil/platform/Draw/business-rule edits, staging, commit or push. ONE final full regression remains deferred until implementation finishes.

## Post-pass audit

1882 source anchors,1069 selected executable inputs and13 quickstart anchors validate with no mismatch or post-pass executable drift. Inventory3039paths/1105Markdown/8805local links/0broken;18master rows/0malformed. Diff check0 with34existing LF/CRLF warning lines;3027dirty paths/0staged. All221 literal original gate lines/checkboxes remain unchanged (193checked/28open), additional249.

All12Tycoon and sole-clock hashes match the prior checkpoint. New driver has no raw World/resource/progress writes, TestKit or RunTicks. Five existing production sources revise original owners; no new production class/state owner. Three new files; one existing source and three existing executable inputs newly selected/frozen. No full regression, staging, commit or push occurred.

