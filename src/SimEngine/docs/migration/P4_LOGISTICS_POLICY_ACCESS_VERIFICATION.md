# P4-LP1 — Private stock and outlet policy verification

Date2026-10-13. Core2.515.0/save154/replay1/logistics view44; physical codecs and sole clock unchanged.

## Original authority and physical owners
ConfigureStockPolicyHandler.Check is the single player admission path for commands and typed view previews: healthy/live/owned/current sight precedes resource/port/settings and the existing physical stock service check. Startup ConfigureStockPolicy and automatic matching remain in the original service and are not visibility-gated.

LogisticsPolicyRules.CheckChange requires player authority; its original provider algorithm is the private CheckProviderChange used by physical Configure. Group admission calls the same authority and withholds hidden Current and Next. Existing ConfigureGroup preflights all members before original exact commits. It now forwards the selected named or unscoped port consistently. A public ConfigureOutletPolicyGroupCommand/handler in the existing command file and PreviewOutletPolicyGroup expose that existing owner without named fleets or another state model.

Current outlet/stock view reads share existing CanObserveOwned. Observer typed diagnosis uses the actual owner and does not grant privileged commands. Hidden/missing/dead/foreign controls refuse NotFound before resource, endpoint, port or nullable change details. No duplicate policy, demand, material, promise, receipt or clock ledger is added.

Six older private assertion files migrate mechanically: AllDispatchPolicyAcceptanceTests, RouteRecoveryPolicyAcceptanceTests, ScopedStockPolicyTests, LogisticsPolicyTests, LogisticsPolicyContentTests and FleetOutletTests. Their registered cases were not executed. The direct physical stock-service foreign-owner refusal and unrelated foreign work-intent refusal remain unchanged; no compatibility remapping is introduced.

## Actual build and runtime
One Release solution:

```text
dotnet build Beep.SimEngine.slnx --no-restore -c Release -v minimal
```

Clean7.35s/0warnings/errors. The solution maps its unused quickstart module to Debug; core/testing/proof/tests compile Release.

NEW FIRST/FINAL:496passed/0failed/0aborted in1905ms; PID32684/process2071ms/Exited0, both streams untruncated/no observer error. No executable edits after FINAL.

```text
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Logistics policy access: private stock export and grouped reserves preserve physical delivery" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 24000
```

## Executed proof
Four minute/half-minute point flows use existing resource, logistics-policy and sight documents. The authored initial destination stock policy is disabled explicitly in the JSON fixture. Its native startup is applied through the existing physical policy owner, not simulated player commands. A normal player stock-band command enables the existing automatic demand/assignment path for eight units from finite source20; source reserve4 stays physically protected.

During real acquired pickup/delivery, either the source or destination is hidden. Current stock/outlet reads return no invented empty policy. Valid/malformed resource/port/settings requests from owner/foreign seats have matching typed/ordinary private refusal. Mixed group Current/Next withholds only the private member and retains real visible metadata.

Six real reserve/stock/group inputs are saved in the hidden pending inbox, loaded and executed; none changes visible or hidden settings. Original automatic physical delivery continues under the visibility mask, with per-Update conservation, source12/receiver8/empty carrier, saved twins and no instant transfer.

Reveal restores ordinary public group reserve control. Reversed input identities canonicalize through the command Sync/selection owner and commit both native policies. Retained reserve facts stay immutable; final save/load, replay of policy/private refusals/delivery, and separate-session hashes pass. Physical waits are bounded512normal Updates; outer90000ms/termination5000ms/output24000characters.

The selected named-port group parameter is source/compile forwarded but this new runtime uses unscoped stores. Full named-port, arbitrary provider, load/trip/route/aggregate and fault/performance matrices remain open rather than inferred from this proof.

## Scope
Original221 gates remain193checked/28open; additional247. No compound gate closes. Complete P4.014/P3.008/P11.033/P9/provider/fault/performance/global/full goal remains open. No older/family/full runtime ran.

Profile metadata only:715registered/included,0excluded/long markers,0game suites. Command: `dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --profile-info tests/Beep.SimEngine.Tests/testdata/framework-validation-profile.json`. No full executor pass is inferred.

All12Tycoon and sole-clock sources remain unchanged. No Oil/platform/Draw/business-rule implementation edits, new state/clock owner, staging, commit or push. The authorized ONE final full regression stays deferred until implementation finishes.

## Post-pass audit

1867 source anchors,1053 selected executable inputs and13 quickstart anchors validate without mismatch or post-pass executable drift. Inventory3033paths/1101Markdown/8799local links/0broken;18master rows/0malformed; diff check0 with34existing LF/CRLF warning lines;3021dirty paths/0staged. All221 original gate lines/literals/checkboxes remain unchanged (193checked/28open), additional247.

Six existing production files revise only original command/policy/view/group/module/identity owners; no new production file, policy/demand/stock/receipt/clock owner or compatibility path. Three new files were authored. Two existing changed source files and one unchanged consumed policy JSON are newly anchored; four existing changed executable inputs and that policy JSON are newly selected/frozen, plus the new driver. All12Tycoon andsole-clock hashes match the prior checkpoint. Driver audit finds no raw World/resource/progress writes, TestKit or RunTicks. No full regression, staging, commit or push occurred.


