# P12-E5 — Cumulative scoped spill and recovery history

## 0. Problem

ResourceFlowJournal currently groups cumulative facts only by resource/kind/cause. A spill is a real StockTransfer, not Lost material. Its cell endpoint and direction must remain distinguishable from recovery and unrelated hauling after the retained feed evicts old facts. Do not create another environmental journal or relabel conserved material as loss.

## 1. Goals

1. Extend the original cumulative summary key with exact historical endpoint attribution and owner scope.
2. Use one immutable attribution value for committed facts, summaries, deterministic ordering and serialization.
3. Extend existing JSON environmental flow metrics to select stock transfers into/out of an authored cell; no parallel metric evaluator or hidden all-causes default.
4. Preserve lifetime totals after feed eviction, save/load and replay, while unavailable time windows remain explicitly unavailable.
5. Prove real native/independent physical spill/recovery/treatment with short retained history and the existing flow contract gate.

## 2. Design

ResourceFlowAttribution contains source, destination, lifecycle endpoint and owner scope, but no receipt or balance. Facts project it from their existing values. Original ResourceFlowAggregate and AggregateKey include it; the same original journal records, saves, validates and publishes cumulative rows. Existing coarse consumers sum rows as before. No duplicate coarse summary/cache is retained.

ResourceEnvironmentalMetricDeclaration adds explicit stock_transfer selection. This kind requires an empty causes array (actual transfer facts have no lifecycle cause) and a transfer object naming one actual cell plus deposit/withdraw direction. It cannot mix lifecycle kinds or omit its selector. Existing lifecycle metrics retain nonempty explicit causes. The same Matches algorithm filters retained facts and attributed cumulative rows.

An authored add-on declares lifetime deposited/recovered totals plus long/short-window transfer metrics and an objective using cumulative totals. The fixture authors a two-record journal policy and normal scenario enrollment before play. Original material movement and objective owners remain unchanged. Current Stock remains distinct from cumulative Flow.

## 3. API/state

Required attribution in aggregate construction; original journal schema7 and resource-flow view9. Engine2.597.0/save158/replay1. Existing fact event payload is unchanged because attribution is a derived projection. Direct replacement, no legacy reader, overload or second aggregation ledger.

## 4. Files

Core/ResourceFlows.cs and ResourceTransactions.cs; Standard/ResourceFlows.cs and EnvironmentalMetrics.cs; protocol expectations and existing public proof schema callers; authored spill-history document; original fixture/physical cases; tracker/evidence.

## 5. Errors

Malformed historical attribution, illegal transfer/lifecycle endpoint shape, noncanonical ordering/counts/amounts and inconsistent retained facts reject restore. Missing/invalid/incompatible cell selectors reject composition. Expired windows report HistoryExpired, never truncated amounts or a lifetime substitute. No financial/gameplay behavior changes.

## 6. Compatibility

No legacy path. Current in-repository contract callers and schema expectations migrate directly.

## 7. Verification

Targeted compile; two new exact physical history cases (60-second watchdog), one exact original journal/metric admission case (30 seconds), all with5-second termination deadlines. No full/profile/family run.

## 8. Risks

Attributed cumulative history has higher key cardinality; original journal remains the only owner and broad allocation/soak gates stay open. Historical attribution is not current endpoint availability or a material balance. General dynamic endpoint-selector authoring, arbitrary custom/private/fault matrices, continuous leaks/disposal/science and global acceptance remain broader requirements.

## 9. Exclusions

Oil/Tycoon/platform/host/Draw changes, material loss fallback, second journal/clock, full regression or Git publication.

## 10. Status

- [x] Extend original cumulative rows/key/codec with immutable exact endpoint attribution and owner scope.
- [x] Explicit cell/direction stock-transfer metric authoring through the existing flow declaration/evaluator.
- [x] Two-record authored history policy, cumulative/expired/quiet-window facts and original cumulative objective.
- [x] Native/independent physical scenarios, saved continuation/replay/private history and malformed cumulative attribution restore.
- [x] Three final literal cases954 successful checks; all attempts retained in [verification](../../docs/migration/P12_SCOPED_SPILL_HISTORY_VERIFICATION.md).
- [ ] Continuous/multi-cell leaks, disposal/science, dynamic selectors, high-cardinality performance and broader private/provider/fault/global acceptance; whole goal remains active.
