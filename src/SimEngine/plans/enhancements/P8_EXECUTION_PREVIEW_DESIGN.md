# P8-F2 — Engine-owned production execution previews

Status: scoped verified on2.409.0/save154/replay1/transformation-view18, FINAL72/0/0 in1455ms. Two clean builds/two attempts; one new exact case only. Original221/154checked/67open unchanged; full goal/plan and full preview gate remain OPEN. [Evidence](../../docs/migration/P8_EXECUTION_PREVIEW_VERIFICATION.md).

## 0. Problem statement

TransformationManagementView exposes separate queues/ports/gates but no cohesive inputs/yields/rates/timing observation. Hosts must not reproduce production arithmetic or invent a guaranteed completion time while supply, gates or outputs are blocked. Original P8.acceptance.015 remains compound/open.

## 1. Goals

1. One contract-based immutable execution preview on the existing view reports authored bills/rates/tools/yields, actual controls/gates/head state and optional money facts.
2. Original WorkProgress owns exact remaining-update arithmetic; original session Time converts updates to game duration. No second progress/remainder/clock.
3. Batch throughput describes output bill per nominal productive cycle; continuous throughput exposes exact authored rates with current target multiplier. Neither promises net delivered throughput.
4. Conditional productive ETA exists only for actual working head with positive rate, healthy gates/state, enabled operation and unpaused clock. Other states give typed absence; no guessed supply/travel/output time.
5. Public player-driven partial production, controls, pause, saved twins/replay/privacy, rate rounding/range and retained faulted projections verify read purity.

## 2. Design

Add Views/TransformationExecutionPreview.cs with ITransformationExecutionView.ReadExecutionPreview and detached sealed facts/typed estimate reason. Existing TransformationManagementView implements it; no new view factory or feature registry. ReadRecipe captures the actual named ITransformer/entity/provider/clock scope, gathers original TransformationContracts.Read/ReadGates/OperationContracts.ReadControl, then publishes only after healthy verification. Early private/missing/empty paths must reject prior update faults.

Core/WorkProgress.TryRemainingUpdates(Fix rate,long required,out long updates) uses existing RemainingRaw and exact ceiling division with BigInteger. Out-of-range/zero rate returns unavailable, never overflow or fake zero. GameDuration.FromSteps and current session Time supply durations. Batch nominal cycle yields and unscaled continuous rates/current multiplier are explicit facts; quantized feed/stock/gates remain actual native owners. No admission callback requests supply or mutates queues during reads.

Productive completion step is conditional, not result settlement or travel/supply ETA. Supply, returning, completed-output waits, disabled/gated work, clock pause, zero rate and range have no predicted completion step. Stale actual Blocker remains reported; controls/gates are read at observation time.

## 3. API

ITransformationExecutionView.ReadExecutionPreview(EntityId,Code,string,out TransformationExecutionPreview). Obtain existing TransformationManagementView and use this interface; no parallel feature instance. Missing/private selection returns normal refusal and null facts. Existing recipe and management methods remain the authoritative command/port observations.

## 4. Files

Core/WorkProgress.cs; Views/TransformationManagementView.cs; new Views/TransformationExecutionPreview.cs; TransformationModule view schema; EngineInfo build; current-engine-protocol identity; existing fictional factory composition access; one new ProductionExecutionPreviewTests; Program registration. Staged evidence/tracker/audit.

## 5. Errors

Existing first-fault and EntityObservation guards reject faults/mutation; no caller prefix. Unknown/not-supported recipe returns typed refusal. No positive estimate on uncertainty or range failure. Read never advances RNG, progress, clock or requests supply.

## 6. Compatibility

Development-only; no legacy reader, compatibility alias or parallel production model. Build identity and view schema change; saved payload layout unchanged.

## 7. Verification

Completed-slice build and ONE NEW exact bounded case only. Same-case repair allowed. Bounded256Update stages and30000ms runtime/5000ms termination. No older/full/family/Oil/Tycoon/platform run. Do not close the entire original preview gate unless its complete provider/continuous scope is genuinely covered.

## 8. Risks

Conditional rate estimates do not predict future failures, queues or output settlement. Continuous rates need native-unit quantization at execution and are labelled authored throughput, not a measured history. Observe actual definitions/providers before publishing; use BigInteger for long progress/rate spans. No normal gameplay in test drivers.

## 9. Out of scope

Routing/travel ETA, independent provider rollback/purity, full mixed-port/result/fuel/staffing/cancellation/accounting acceptance, allocation/stress/global gates and protected G19 migration. No money requirement, rendering/platforms or commit/push before full goal/plan.

## 10. Order

Inspect actual contracts/Time/WorkProgress; implement exact math and one immutable view observation; exercise ordinary authored production/controls/checkpoints; fix actual owners only; preserve every attempt and frozen inputs; update original scope unchanged.

