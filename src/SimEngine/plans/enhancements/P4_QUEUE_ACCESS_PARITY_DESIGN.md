# P4-QP1 — Private queue preview/command parity

## 0. Problem statement

Management previews hide foreign queues with NotFound while ordinary commands return NotOwned. Owned queues hidden by a selected visibility policy can still be admitted and read. The shared evaluator gate must cover both access and queue arguments, not only successful owned cases.

## 1. Goals

1. Enqueue/cancel checks in the original TransformerManager own one healthy/live/owner/current-sight access rule.
2. Management and ordinary command previews return identical private refusal before inspecting queue arguments or providers.
3. Actual rejected commands change no queue, WIP or reusable receipt; physical work may still advance normally.
4. Hidden recipes/execution projections remain private and saved physical continuation/replay agrees.

## 2. Design

CheckEnqueue/CheckCancel perform health and same current-owned sight checks before capability/recipe/index/provider reads. Missing/dead/foreign/hidden queues all return NotFound. This intentionally removes the ownership-specific foreign queue leak; no compatibility result mapping is introduced.

TransformationManagementView.Owned delegates to existing PlayerView.CanObserveOwned for current facts. Its two admission previews call ITransformationQueue directly with the actual viewer. Explicit observer diagnosis uses the actual entity owner but grants no command-submission privilege. Original account/provider guards, native queue/stock/receipt/timing and completion/cancellation owners remain unchanged.

## 3. API surface

Signatures unchanged. Foreign queue command refusal becomes NotFound, matching private management previews. Transformation view22 becomes23; engine2.510.0/save154/replay1. Existing five old fixture expectations migrate mechanically, without executing their registered suites.

## 4. Files to change

Existing TransformerManager, TransformationManagementView, module view descriptor and engine/protocol identity. Five existing foreign-refusal expectations only. One new public player driver/profile/registration, tracker/evidence. Existing selected InventorySight module and physical helper are reused unchanged.

## 5. Error handling

Terminal health check precedes missing/private shortcuts. Invalid queue arguments cannot disclose a hidden queue. Refused normal commands publish their original typed refusal andleave physical queue/material/receipts unchanged; this is not rollback of a running simulation tick.

## 6. Compatibility

Development-only contract change. No NotOwned compatibility mapping or legacy queue path.

## 7. Verification

One solution build andone new exact90second case. Four minute/half completion/cancellation physical fixtures compare typed andordinary previews for valid/invalid/foreign/missing/hidden access, execute four rejected commands, save hidden state, preserve actual WIP/tools andreplay. No older/family/full registry.

## 8. Risks

The new private foreign refusal is intentional. Observer diagnostic preview uses original owner authority, not a privileged input path. Current physical progress can advance during rejected-command Updates; do not incorrectly assert that all simulation state is frozen.

## 9. Out of scope

Other command families, complete P4.014/P3.008/P11.033 or full-provider/stress/global gates, Tycoon rules, Oil/platform/Draw, and final full regression.

## 10. Execution order

1. Add private access to the original queue evaluator.
2. Route management previews to that evaluator andcurrent projections to existing sight.
3. Mechanically migrate obsolete refusal expectations.
4. Build/run one new physical parity case andupdate evidence without broad completion claims.

## Post-pass metadata verification

After FINAL,1832 source anchors,1024 executable inputs and13 quickstart anchors validate with no hash mismatch or executable drift. Inventory:3014paths/1089Markdown/8781local links/0broken;18master rows/0malformed. All221 original gate literals/anchors remain unchanged (192checked/29open). Diff check exits0 (34existing LF/CRLF warning lines);3002dirty paths/0staged. All12Tycoon files andsole clock implementation match the prior checkpoint. Three new files were created; existing TransformationCancellationTests was explicitly added to the input freeze after its mechanical refusal migration. No full regression, staging, commit or push occurred.
