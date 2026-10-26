# P4-HP1 — Carrier policy authority and cargo disclosure verification

Date2026-10-13. Core2.516.0/save154/replay1/logistics view45. Existing physical codecs and sole game clock unchanged.

## Original owners

Load/trip CheckConfigure requires the actual player and gates healthy/live/session identity/ownership/current sight before provider/settings access. The original provider algorithm remains private for physical Configure; hauling/load minimums/travel budgets and acquired cargo do not depend on player sight. Existing group selection uses that same player check and omits private Settings. Existing whole-set preflight and exact provider commits/fault-stop remain authoritative.

Public ConfigureHaulLoadPolicyGroupCommand and ConfigureHaulTripPolicyGroupCommand live in their respective existing command files. Their Sync canonicalizes detached identity lists; the existing logistics module registers them. LogisticsView typed group previews forward the original group owner, with no named-fleet requirement or duplicate state.

Current load/trip/service source/service window/trip-budget/cargo return/disposition reads gate actual carrier sight before private callbacks. Observer reads diagnose actual physical state; observer single-control previews retain actual owner admission. No compatibility overload, second scheduler, stock/receipt ledger, platform behavior or clock is introduced.

Seven older assertion files migrate only affected foreign private refusals: HaulLoadPolicyTests, HaulTripPolicyTests, FleetLoadTests, FleetTripTests, HaulContentContractTests, RetainedHaulTripTests and CargoDispositionObservationTests. Their registered cases were not run.

## Build and actual runs

One Release solution build:

```text
dotnet build Beep.SimEngine.slnx -c Release --no-restore
```

Exit0,10.02s,0warnings/errors. The solution maps its quickstart module to Debug; core/testing/proof/tests compile Release.

FIRST Release332/0failed/0aborted in1623ms, PID10480/process1877ms/Exited0. FINAL same-case Release332/0/0 in1550ms, PID45932/process1771ms/Exited0; streams untruncated/no observer error.

```text
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Carrier policy access: private grouped load trip and cargo views preserve saved physical delivery" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 24000
```

Between FIRST and FINAL only the explicit validation profile JSON gained the newly registered case. First profile-info refused an incomplete explicit assignment (exit1); after adding the case, metadata-only profile-info passed:716registered/included,0excluded/long markers,0runtime suites. This was not a profile/full executor run. No executable input changed after FINAL.

## Executed scope

Two authored minute/half-minute point flows use existing load/trip JSON profiles, native capability components, native cargo and the engine-side visibility module. A second identical authored carrier is physically held at position20, outside the route, with an earlier identity so a valid visible group prefix precedes the unseen carrier. No runtime stock, motion, assignment or policy writer exists in the driver.

Normal input starts an eight-unit timed haul from finite source20. The actual carrier loads cargo and exposes an actual trip budget without optional money. Hidden and foreign current load/trip/service/window/budget/cargo/return views return NotFound with no fabricated facts; caller return lists are cleared. Typed and ordinary single/group preview refusals agree, including null settings and malformed selection shapes. Hidden group settings are absent; visible prefix settings remain actual immutable values.

Eight actual owner/foreign single/group inputs are saved in the hidden pending inbox, loaded and executed. All refuse NotFound; neither the unseen carrier nor the earlier visible prefix changes settings. The original acquired job remains assigned. Physical delivery continues while unseen, with per-Update conservation, source12/receiver8/empty carrier, saved twins and no teleport or instant transfer.

Reveal restores public group controls for both carriers. Original load owner applies minimum5/emergency bypass, and original trip owner disables fresh trips. Retained original minimum4/trip-enabled facts remain immutable. Settled save/load, replay of actual commands/refusals/delivery and independent-session hash isolation pass. Physical waits are bounded512normal Updates; outer90000ms/termination5000ms/output24000characters.

## Remaining scope and authority

The native no-floor trip profile has no operating-fuel/service commitments; this is not a runtime proof of fueled funding, reserve floors, service queues, arbitrary external policies, disposal/cleanup execution or all recovery paths. Existing algorithms are source/compile preserved only for those matrices. Route/demand aggregate disclosure and the broad P4.014/P3.008/P11.033/P9/provider/fault/performance/global acceptance remain open.

Original221 gates remain193checked/28open; additional248. No compound gate closes. All12Tycoon sources and sole-clock hash unchanged. No Oil/platform/Draw/business-rule edits, staging, commit or push. The ONE authorized final full regression remains deferred until implementation completes.

## Post-pass audit

1878 source anchors,1065 selected executable inputs and13 quickstart anchors validate with no mismatch or post-pass executable drift. Inventory3036paths/1103Markdown/8802local links/0broken;18master rows/0malformed. Diff check0 with34existing LF/CRLF warning lines;3024dirty paths/0staged. Original221 literal gate records remain193checked/28open; additional248.

All12Tycoon and sole-clock hashes match the prior checkpoint. Driver boundary audit reports no raw World/resource/progress writes, TestKit or RunTicks. Nine existing production files changed, zero new production owners/files. Three new files were authored; eight existing source anchors and eleven existing selected executable inputs were added to the inventory, including the two unchanged consumed load/trip JSON documents. No broad pass, staging, commit or push is inferred.

