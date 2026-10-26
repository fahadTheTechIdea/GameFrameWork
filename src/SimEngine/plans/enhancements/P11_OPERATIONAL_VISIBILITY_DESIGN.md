# P11-OV1 — Current operational facts follow entity sight

## 0. Problem statement

OperationsView's current-fact Owned gate, MotionView's five read methods, and LogisticsView's live diagnostics/throughput gate rely on ownership but omit current entity visibility. Cached IDs can expose private physical activity after sight changes.

## 1. Goals

1. Route current owned observations through the existing PlayerView authority, with health/seat/alive/owner/sight validation.
2. Withhold operation identities/control/schema/utility facts, motion/navigation/arrival/operating/route facts, and logistics/throughput objects before providers run.
3. Preserve authorized observer, visible ownership refusal semantics, company policies, historical intent and command-admission behavior.
4. Prove real worker/processor visibility loss/reveal during physical production and hidden saved continuation/replay.

## 2. Design

PlayerView.CanObserveOwned is an internal convenience over the existing CanSee policy, not another fog or ownership service. OperationsView.Owned and MotionView's current read selector use it. Logistics diagnostics preserve their explicit visible-owner refusal and add the same current sight check before reading facts. The native view descriptors become operations13/motion12/logistics43; engine2.509.0/save154/replay1.

The existing selected InventorySight module and original physical helper supply JSON settings, normal mask commands, actual stock/movement/WIP/tools and saves. Eight authored configurations mask the active processor or worker separately across minute/half-minute completion/cancellation. No mod gameplay is copied into the new driver.

## 3. API surface

Production signatures unchanged. Hidden current facts return false/default/empty or NotFound/null. Observer reads remain authorized. ReadRoute refuses a hidden entity before interpreting geometry. Navigation/control admission previews are not changed in this slice; their broader shared-evaluator/disclosure gates remain open.

## 4. Files to change

Existing PlayerView, OperationsView, MotionView, LogisticsView andtheir module view descriptors; engine/protocol identity; one new public driver/profile/registration; tracker/evidence. No new gameplay service or state owner.

## 5. Error handling

First-fault guard runs before private current-read shortcuts. Hidden queries produce no staged identity prefix or live provider facts. No visibility cache, rollback or invented default result is introduced.

## 6. Compatibility

Development-only identity/version change; no compatibility shim.

## 7. Verification

One solution build and one new bounded exact case (90seconds); same-case repairs if required. Eight physical fixtures exercise hidden cached queries, authorized observer, hidden save/load under equal local pacing, retained prior facts, reveal deltas and full ordinary-command replay. No older case/family/full registry.

## 8. Risks

Company-owned intent/history and command admission are not automatically the same as observing current physical facts. Do not alter those rules during this read-boundary fix. Whole private metadata/fog/mod/provider/stress matrices remain unproven.

## 9. Out of scope

Other views, command/control admission policies, Tycoon/Oil/platform/Draw, quality/lot gameplay, full regression and complete P3.008/P11.033 closure.

## 10. Execution order

1. Extend the original PlayerView current-owned observation boundary.
2. Migrate three current live-read view families andtheir descriptors.
3. Drive actual physical worker/processor hiding through public commands and saved views.
4. Build/run the new bounded case andupdate evidence without claiming whole privacy or full goal.

## Post-pass metadata verification

After FINAL,1829 source anchors,1022 executable inputs and13 quickstart anchors validate with no hash mismatches or executable drift. Inventory:3011paths/1087Markdown/8779local links/0broken;18master rows/0malformed. All221 original gate literals/anchors remain unchanged (192checked/29open). Diff check exits0 (34existing LF/CRLF warning lines);2999dirty paths/0staged. All12Tycoon files andsole clock implementation match the previous checkpoint. Three new files were created; the previously unanchored existing MotionModule was added to source/input inventories. No full regression, staging, commit or push occurred.
