# P4-OP1 — Private operation and safety-policy verification

Date2026-10-13. Core2.514.0/save154/replay1/operations view14/condition view4; physical codecs and sole clock unchanged.

## Original authoritative owners
ConfigureOperationHandler.Check now gates healthy/live/owned/current entity sight before operation ID, target, provider or safety checks. Its existing check executes under the original whole-target observation guard; both command and typed OperationsView preview use the same evaluator, with the real viewer or observer’s actual-owner diagnosis. No parallel permission/refusal mapper or control state is created.

OperationPolicyContracts.CheckConfigure requires player/entity/policy authority. The original provider-level settings check stays private in the same owner and is used by physical Configure; wear/shutdown/restart evaluation still uses its existing physical methods rather than player permission. EquipmentConditionView uses existing PlayerView.CanObserveOwned for condition, wear, policy facts and identity menus, and forwards typed preview to the same policy admission.

Four older files migrate mechanical private assertions: OperationTests, OperationPolicyTests, LiveSensorTests, and PolicyContentContractTests (prior work-policy consistency). Their registered cases were not executed. Signal-read ownership semantics are unchanged; this revision concerns operation/policy controls and current condition projections. No compatibility accessor or duplicate state owner is added.

## Builds and retained outcomes
One Release solution attempt:

```text
dotnet build Beep.SimEngine.slnx --no-restore -c Release -v minimal
```

Failed8.77s/exit1/0warnings/2errors: the new test used an unsupported WorkProgress equality operator at two assertion sites. Production projects compiled. No runtime was launched from that failed build.

Same-case focused compile after using the existing value Equals contract:

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -c Release -v minimal
```

Clean5.96s/0warnings/errors. New FIRST/FINAL exact case:539passed/0failed/0aborted in1946ms; PID23956/process2114ms/Exited0, both streams untruncated/no observer error. No executable changes after FINAL.

```text
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Operation access: private control and condition policies preserve physical WIP pause and saved continuation" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 24000
```

## Executed proof
Four authored minute/half-minute completion/cancellation flows reuse the original point resource/sight/manufacturing helper and native condition/policy JSON components. Condition unit and thresholds are copied from the existing authored documents before initialization; no live condition adjustment or alternate simulator is added.

Visible operation shutdown and policy updates go through normal command preview/execution. The disabled process retains original input WIP, reusable tool receipts and progress while the game clock advances. Current condition remains the authored actual value; no fabricated condition or safety transition is claimed.

While the processor is hidden, condition/wear/policy reads and ID menus expose no prefix or substituted facts. Valid and malformed control/policy requests, from owner and foreign seat, produce identical typed/ordinary NotFound before IDs, target bounds or nullable settings. Observer diagnosis preserves the actual hidden owner’s refusal.

Four real re-enable/policy-change inputs are saved in the hidden pending inbox, loaded and executed. They change no enabled state, policy criticality, condition, material/tool receipts or stalled progress. Reveal restores ordinary admission; re-enable resumes original physical completion or cancellation. Retained control/policy/WIP facts remain immutable. Existing helper verifies finite source/receiver accounting, real delivery/manufacture, WIP/tool settlement, saves, paused clock, replay and interleaved session hashes.

Native wear/shutdown/restart method ownership is source/compile preserved; arbitrary wear/policy/automation provider matrices and environmental incidents are not claimed executed by this case. The unchanged condition fixture is intentional, not a default fallback.

## Scope and limits
Original221 gates remain193checked/28open; additional246. No compound gate closes. Broad P4.014/P3.008/P11.033/P10/provider/fault/performance/global/full goal remains open. No older/family/full runtime ran. The only new runtime is one bounded exact case, process90000ms/termination5000ms/output24000characters.

Metadata-only profile info:714registered/included,0excluded/long markers,0game suites. Command: `dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --profile-info tests/Beep.SimEngine.Tests/testdata/framework-validation-profile.json`. This is not a full executor pass.

All12Tycoon and sole-clock files remain unchanged. No Oil/platform/Draw/business-rule changes, new clock or state owner, staging, commit or push. Authorized ONE final full regression remains deferred until implementation finishes.

## Post-pass audit

1861 source anchors,1047 selected executable inputs and13 quickstart anchors validate without mismatch or post-pass executable drift. Inventory3030paths/1099Markdown/8796local links/0broken;18master rows/0malformed; diff check0 with34existing LF/CRLF warning lines;3018dirty paths/0staged. All221 original gate lines/literals/checkboxes remain unchanged (193checked/28open), additional246.

Eight pre-existing production files changed inside original owners; no new production file, policy/control/state/clock store or compatibility entry point. Three new files were authored. One pre-existing changed test is newly source-anchored, and two pre-existing changed tests are added to the selected executable freeze, plus the new driver. Existing condition/policy authoring documents were already frozen and are reused unchanged. All12Tycoon andsole-clock hashes match the prior checkpoint. Driver audit finds no raw World/resource/progress writes, TestKit or RunTicks. No full regression, staging, commit or push occurred.


