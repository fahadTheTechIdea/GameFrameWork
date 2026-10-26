# P5-A17 — Contract-based running costs and fine game-clock coordinates

## Scope and authority

Continue approved financial ownership migration; preserve the grid connection60-per-hour amount and
five-minute operating offset. Physical power/fuel/running state stays in Oil's engine assembly.
No adapter/Draw/client logic, parallel operation state, billing counter, account ledger or second clock.

## Stage 1 — Correct canonical time representation

P5-A15 incorrectly authored a ten-minute simulation quantum instead of a ten-minute game frame.
Oil's Power/Research/Campaign/Storyteller schedules require exact5/15/45/50-minute offsets. A ten-minute
quantum cannot represent them. All six time definitions now explicitly declare one-minute quantum and
ten updates per frame. The original GameClock and all schedule formulas are unchanged.
This is a correction, not a backward-compatibility reader or a second timing path.
Historical A15 fixture proof did not establish full Oil schedule composition.

## Stage 2 — General conditional periodic-cost contract

IEntityOperatingCostDefinition now has an explicit IOperatingCostCondition or null.
Every existing upkeep JSON row declares null for unconditional charging; missing fields refuse.
IOperatingCostCondition validates an actual target prototype and returns a typed eligibility blocker.

RunningOperationCostConditionDef authors a named operation identity. It validates the target's existing
IOperation capability and observes that original provider's OperationSnapshot through the shared guard.
Only OperationStatus.Running is eligible. It never infers running from fuel stock, enabled flags,
elapsed frames or financial state. Missing named operations refuse before initialization.

The existing EntityOperatingCostSystem remains the sole interpreter. It observes the condition, skips
ineligible due occurrences and charges only through original accounts/mode rules. No retry debt is
invented for an idle occurrence; a later eligible occurrence uses the same anchored schedule.
BusinessView8 publishes the condition blocker in detached cost views. System codec1 unchanged;
core2.542.0/save154/replay1. No new persisted decision/timer.

## Stage 3 — Move Oil power money data

business-operating-costs.json now contains38 unchanged unconditional upkeep policies and one conditional
grid_connection running policy: amount60, period1hour, offset5minutes, reasonledger.power, operationpower.
The PowerSourceDef cash field/reader, old ledger constant and PowerSystem/Fuel economy lookup/charge
are removed. Physical fuel commit and actual Running state remain on their original owners.
All other structure and cost data stays unchanged; all business configuration remains under one
optional Tycoon module in the master manifest.

## Stage 4 — Focused proof and actual admission boundary

One NEW exact public-input case uses a native continuous transformer operation to prove the generic
condition: idle versus running, disabled/resumed work, real physical processing, hourly billing,
saved twins and disabled/absent money. Its authoring projection retains canonical grid rates/timing;
it is not the actual Oil grid operation.
Six canonical mode/time profiles separately prove the exact fine-minute offsets and original frame size.

One public OilCoGame initialization preflight against actual manifest/data currently refuses unknown
section recipes in oil/production.json. No Oil world was initialized or advanced. Next migrate native
transformation/worker/storage/construction content, along with remaining sale/research business ownership.
Actual Oil generator/fuel/network behavior and full durability/privacy/provider/stress remain unverified.
No original acceptance checkbox closes. Focused tests only; commit/push waits for full goal AND plan.

[Evidence](../../docs/migration/P5_OPERATION_RUNNING_COSTS_VERIFICATION.md)
