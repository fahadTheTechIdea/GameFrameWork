# P12-E4 — Current spill backlog and objective integration

## 0. Problem

Finite spill material now exists in original spatial cells, but environmental metrics currently describe only committed resource flows. Treating current spill stock as cumulative loss or dividing it by elapsed time would misreport the backlog. Extend the existing metric contracts, not the journal or material ledger.

## 1. Goals

1. Distinguish flow from current-stock observations explicitly in the existing immutable metric contract/view.
2. Read file-authored actual spatial cells through original endpoint/layer owners; preserve native units and private ownership.
3. Current stock has an observation timestamp and a typed no-rate reason, never a fabricated throughput.
4. Drive spill-detected -> recovered -> treated objectives through the original objective system and ordinary physical work.
5. Prove native/independent spatial providers, blocked inputs, private reads, saved continuation and replay with literal focused gates only.

## 2. Design

IEnvironmentalMetricDefinition declares EnvironmentalMeasure (Flow/Stock) and nullable RateUnit. Existing flow definitions retain their authored rate/window semantics. EnvironmentalObservation publishes the measure, and its single Measured factory plus original service validation enforce flow rates versus rate-free current stock. Stock coverage is one current game-clock timestamp; no new history or timer.

SpatialResourceEnvironmentalMetricDeclaration authors unit and a nonempty distinct list of cell definitions. SpatialResourceEnvironmentalMetricContracts validates actual content/unit/cell/service aliases, then sums actual same-unit quantities under original endpoint ownership and content visibility. SpatialResourceEnvironmentalMetricDef directly implements contracts; independent definitions delegate to the same declaration and contract algorithm without native inheritance or a new evaluator/scheduler. Existing metric service guards spatial observations as well as entities. No native TileMap dependency.

One master-listed JSON add-on authors backlog metric, treated-material flow metric, predicates and objective prerequisites. The fixture supplies explicit scenario JSON enrollment and opts into these definitions; no test/driver calculates objective progress. A separate master-listed extension document authors an independent stock definition. Treatment uses the journal's actual Produced cause, not an artificial environmental event. Current backlog is not cumulative spilled volume; that separate history requirement stays open.

## 3. API/state

Required measure and nullable rate in metric definition/observation contracts; migrate all current public proof implementers directly. New spatial_environmental_metrics section. Existing state owners/layouts unchanged; engine2.596.0/save157/replay1 and environmental view contract2. No compatibility aliases.

## 4. Files

Standard/EnvironmentalMetrics.cs and module, new spatial metric declaration/definition in the same subsystem, actual proof implementations, authored environmental spill add-on, existing fixture composition/physical tests, protocol expectations, tracker/evidence.

## 5. Errors

Missing/duplicate cells, unequal units, missing selected capabilities and invalid measure/rate metadata refuse composition. Foreign private cells yield withheld unavailable facts, not zero. Stock observations never produce a rate. Callback mutation faults through original observation boundaries; no rollback fiction.

## 6. Compatibility

Development-only direct contract migration; no legacy/default measure or facade.

## 7. Verification

Targeted compile; exact current-stock admission/privacy case, native/independent extended physical spill cases, and one relevant existing flow contract case. Physical cases use a60-second process watchdog, other literals30 seconds, all with5-second termination deadlines and bounded stage loops. No full/profile/family runs.

## 8. Risks

Current stock is not history. Mixed units require explicit conversion content in a later requirement; no silent unit conversion. Multi-cell mixed-owner policies, arbitrary custom providers/fault injection, cumulative spill totals, continuous leaks, environmental science, disposal and global durability/performance remain broader work.

## 9. Exclusions

Oil/Tycoon/platform/renderer/Draw changes, duplicate pollution/material/journal store, global acceptance claims or Git publication.

## 10. Status

- [x] Explicit stock/flow contract, nullable rate unit and immutable typed observations, no compatibility alias.
- [x] Original-owner cell observations and native/direct external metadata through the same contract algorithm.
- [x] File-authored backlog/treatment metrics and prerequisite objectives driven by the existing physical workflow.
- [x] Four focused final literals865 successful checks; all prior failures/attempts retained in [verification](../../docs/migration/P12_SPILL_BACKLOG_OBJECTIVES_VERIFICATION.md).
- [x] Correct stale resource-only/mapless flow-test composition without enabling Tycoon or changing financial rules.
- [ ] Cumulative spill totals, continuous leaks, disposal/science, multi-cell mixed-owner and hostile-provider/global matrices; whole plan/goal remains active.
