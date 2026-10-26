# P12-C1 — Authored environmental accounting over verified resource history

Current M94/native2.732/save161/replay2: environmental view/source terminal preflights repaired,
with existing physical accounting and one complete initial-session replay2045/0 in one exact case.
No journal/resource/accounting rule or codec changes. [Current evidence](../../docs/migration/P12_ENVIRONMENT_CONTENT_CONTRACTS_VERIFICATION.md#current-m94-retained-environmental-queries-stop-at-the-original-terminal-fault--2026-10-22).
Original accounting status: focused verified2026-10-06; broader P12 remains partial. Parent: [P12](P12_INCIDENTS_ENVIRONMENT_DESIGN.md).
[Scoped evidence](../../docs/migration/P12_ENVIRONMENT_ACCOUNTING_VERIFICATION.md).

## 0. Problem

Standard/ResourceFlows.cs retains a bounded fact feed but discards old amounts. Environmental totals
and objective inputs cannot depend on whether the UI/history retention still contains a spill or
cleanup fact. Adding a second environmental resource balance or host counter would duplicate authority.

## 1. Goals

1. Extend the existing resource-flow journal, its sole event subscriber and codec with cumulative
   company/resource/kind/cause amount/count/timestamp summaries. These are history, never inventory.
2. Keep retained individual facts bounded by authored retention; whole-session sums use arbitrary
   precision without a game/world-size cap. Track the actual last evicted tick to certify time windows.
3. Optional master-listed environmental metric definitions explicitly select same-native-unit resources,
   actual lifecycle kinds/causes, cumulative or rational-window scope and rate time unit.
4. Derive immutable typed amount/rate observations through a public metric-source contract. Missing
   history/inputs/refusals stay explicit; no assumed emissions, oil formulas, zero substitute or grants.
5. Native objective predicates read that same metric source. Existing ObjectiveSystem owns all pacing,
   observations/effects/state. Timed incident cleanup effects remain native WorkBoard/resource requests.
6. Save/load/hash, private views and interleaved physical cleanup preserve history after feed eviction.

## 2. Design

ResourceFlowJournal replaces its company FeedRing value with one embedded partition containing that
same feed, cumulative grouped summaries and last-evicted tick. Record validates actual ResourceFlowFact
then updates the partition once in the event phase. No extra subscription/service/store or per-metric
counter. Immutable ResourceFlowSummary and ReadWindow extend IResourceFlowJournal. Decode stages all
partitions before replacement and verifies canonical unique keys, native units/quanta, positive amounts/
counts, time ranges, whole count=feed.Latest and dominance over retained facts. Full retained history
requires exact equality; evicted history cannot be reconstructed, so do not claim cryptographic proof.

EnvironmentalMetricDef resolves resources/units through existing catalogs and explicit lifecycle
kind/cause filters; native rules may classify real Lost/Destroyed/Consumed/Produced facts differently
per game. They do not create contamination inventory. All selected resources must use the exact same
native unit identity: no mixed-unit scalar or guessed conversion. Window:null means current cumulative
history; a positive exact authored duration selects [max(0,tick-duration),tick). Window reads refuse
Expired whenever the last evicted fact overlaps the requested range, even if that fact might not match
a metric. This conservative certification avoids silently incomplete rates. Cumulative reads remain
available after retention expiry. Rate is an exact reduced rational in quantity-units per selected
second/minute/hour/day, using actual session quantum; tick0 has explicit unavailable rate.

Optional sim.environment-metrics requires sim.resource-flows only. EnvironmentalMetrics aliases
IEnvironmentalMetricSource, holds only World/catalog references and no Sync/state/system. Private
EnvironmentalMetricsView and optional environmental objective predicate definitions read the same
source. Normal objective composition is independently selected, never an implicit module dependency.
Native metrics require the selected journal; future independent metric definitions override public
ValidateBinding/Observe through the same catalog/base/service without scheduler changes.
Public independent proof now compiles/executes through this seam. Objective predicates explicitly
author their threshold unit and validate its exact metric match at composition; do not read another
provider's unresolved metadata during Resolve or introduce type-specific resolution-order branches.
Cause filters use the existing nonempty Code contract, including native `activity:` plan-key causes;
they are not content-definition IDs and must not receive an incompatible identity parser.

## 3. API

IResourceFlowJournal.ReadSummary/ReadWindow; ResourceFlowAggregate/ResourceFlowSummary.
IEnvironmentalMetricDefinition/IEnvironmentalMetricSource, EnvironmentalQuery/Observation/Rate,
environmental_metrics, environmental_objective_predicates; company-private EnvironmentalMetricsView.
No instant cleanup/grant/player metric edit command, copied live balances or new environmental events.

## 4. Files

Extend Core/ResourceFlows.cs, Standard/ResourceFlows.cs, Kernel/Players.cs and Views/ResourceFlowView.cs.
Add Standard/EnvironmentalMetrics.cs, EnvironmentalMetricsModule.cs and Views/EnvironmentalMetricsView.cs.
Public proof content/provider and two exact suites; current identity migration and stage/master/parent/
evidence/README/help/AGENTS. Build2.98.0/save63/resource-flow-journal6/view6; event fact5 unchanged,
environmental view1/stateless service, other native incident/work/network/resource layouts unchanged.

## 5. Errors and risks

Bad resources/unit/kind/cause/window/rate/reference fail explicitly. Negative/future windows refuse.
Expired windows do not fabricate zero. Foreign private reads disclose no watermarks/amounts. Missing
source, invalid provider observations, overflow/unrepresentable bridges cannot become successful
measurements. Structural/cumulative/feed inconsistency rejects staged decode without losing originals.
Source purity validation is scoped; wider cross-service mutation and arbitrary-provider cost remain open.

## 6. Compatibility

Current contracts/state only. No legacy journal reader, default metric, parallel ledger or compatibility shim.

## 7. Verification

Exactly `Environment accounting: authored metrics and durable journal admission` and
`Environment accounting: physical cleanup, rates and saved objectives`.
Prove strict authoring, cumulative history past eviction, conservative windows, exact rates/large sums,
staged rejection/private immutable reads/provider extension, real machine travel/spending/cleanup,
objective input pacing and saved/interleaved continuation. Compile other identity assertions only.
No third/family/full gate without request.

Final focused evidence: clean solution build (0 warnings/errors;2.27 s), **109 checks / two exact
suites**:84 contracts/admission (444 ms),25 physical/continuation (483 ms), zero failures/aborts.
Strict native units/authoring/window/threshold admission, independent public provider and faults,
complete/expired/empty windows, exact rational rates, cumulative quantities beyond Fix range,
staged failed-decode preservation, immutable/private reads, no-map/no-work/no-objective composition,
real responder travel/spill/cleanup/material spending/cancellation/restart, objective next-step pacing,
wide timestamps and saved interleaved hashes pass.36 other current identity assertion files migrate/
compile only; prior suites not rerun. New journal6/view6 replaces old native layout directly; event5
and other incident/work/spatial/resource codecs stay unchanged. Build2.98/save63; no old reader.

## 8. Open scope

Contamination layers/native weather/chemical formulas, arbitrary source metrics, condition/maintenance/
preparedness, persistent environmental impact/cost policy, automation signal bridge, global provider/
purity/fault/replay/stress/allocations and full P12/P1–P12 acceptance remain open. This is verified
physical-history accounting, not scientific environmental certification. No Oil/Tycoon/host/platform/Draw.

## 9. Order

1. Add cumulative/window contracts and extend the sole journal/partition/codec.
2. Add optional authored metric service/private view and objective predicate over existing contracts.
3. Author non-oil physical cleanup/provider fixtures; run only two exact gates and repair them.
4. Record final focused evidence and synchronize tracker/parent/help/README/AGENTS without claiming full completion.
