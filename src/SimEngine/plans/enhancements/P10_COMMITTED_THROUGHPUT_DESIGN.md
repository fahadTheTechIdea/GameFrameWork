# P10-B11: committed throughput — Design Document

## 0. Problem Statement

P10.acceptance.025 remains open: network elapsed-service/next-allowance facts are not actual delivered throughput. The sole ResourceFlowJournal already records verified transfer/lifecycle facts and cumulative history, with explicit master-listed JSON retention. EnvironmentalRate and LogisticsThroughputSnapshot separately reduce quantity/time fractions. A shared measured-rate value and a filtered journal query can supply actual throughput without adding a ledger, timer or host calculation.

## 1. Goals

1. Extend existing IResourceFlowJournal and ResourceFlowView with private pure filtered committed throughput.
2. Use native resource units, explicit half-open windows and the sole session quantum; no nominal allowance, wall clock or missing-history zero.
3. Move existing environmental fraction logic into one general MeasuredRate value and use it for journal/environment/haul outputs.
4. Preserve original hauling query semantics and history ownership while eliminating its duplicate aggregation.
5. Verify network delivery/backpressure, utility satisfaction and actual deferred operation state through public engine facts, saves, replay and independent sessions.

## 2. Design

Core/ResourceFlows.cs gains immutable ResourceFlowQuery (company/resource, copied explicit kinds, half-open coordinates, rate unit and optional exact endpoint/cause/owner-scope filters) and ResourceFlowObservation. Query fields are observer input, not gameplay defaults/content. Existing master-listed resource_flow_journals remains sole authored retention policy. Native journal ReadThroughput queries existing ReadWindow, validates actual resource and sums matching verified facts only; expired or future windows publish typed refusal with no amount/rate.

Core/ResourceRates.cs owns MeasuredRate, migrated from EnvironmentalRate rather than wrapped or duplicated. It reduces arbitrary-precision fractions and converts only through the original ISimulationTime. Native-unit and raw-Q32 observation paths share one calculator. EnvironmentalObservation uses that value; LogisticsThroughputSnapshot retains its original raw-Q32 public facts but obtains them from the shared calculator. Existing ReadIncomingThroughput uses the journal's filtered original haul-scope transfer facts, not a second aggregation.

ResourceFlowView observes the original service and cuts off faulted/private reads before calling it. Refused queries expose no native unit, amount, facts or rate. No new runtime owner, state codec, scheduler, clock, economic requirement or compatibility path. Build2.430.0/save154/replay1; resource-flow view registration advances6→7 because the public query shape changes.

## 3. API surface

IResourceFlowJournal.ReadThroughput(ResourceFlowQuery) and ResourceFlowView.ReadThroughput(ResourceFlowQuery) return detached ResourceFlowObservation with typed status, native quantity unit, nullable exact amount/rate and fact count. Zero-age complete windows return real zero amount and typed no-elapsed-time rate refusal. Query kinds are mandatory and copied/read-only; endpoint/cause filters refer to historical facts and need not fabricate current live entities.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---|---|
| Modify | ../../src/Beep.SimEngine/Core/ResourceFlows.cs | 95 | medium |
| Modify | ../../src/Beep.SimEngine/Core/ResourceRates.cs | 38 | medium |
| Modify | ../../src/Beep.SimEngine/Standard/ResourceFlows.cs | 30 | medium |
| Modify | ../../src/Beep.SimEngine/Standard/EnvironmentalMetrics.cs | 25 | medium |
| Modify | ../../src/Beep.SimEngine/Standard/LogisticsDiagnostics.cs | 30 | medium |
| Modify | ../../src/Beep.SimEngine/Views/ResourceFlowView.cs | 15 | medium |
| Modify | Protocol/registration and pure network factory clock/retention authoring | 15 | low |
| Modify | ../../tests/ExtensionProof/ExtensionProof.csproj | 1 | low |
| New | ../../tests/Beep.SimEngine.Tests/CommittedThroughputTests.cs | 230 | medium |
| New | ../../tests/ExtensionProof/content/committed-throughput-metrics.json | 5 | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Invalid query identity/kinds/window/unit/filter | ArgumentException before any observation |
| Foreign company | NotOwned, no private amount/unit/facts/rate |
| Unknown native resource | UnknownResource, no fabricated unit |
| Future window | CursorAhead, no invented rate |
| Evicted overlapping window | Expired, never partial quantity or false zero |
| Complete quiet positive window | Exact zero quantity and zero rate |
| Complete zero-age window | Zero quantity; no-elapsed-time rate blocker |
| Existing first fault or callback failure | Refuse cached queries/save, preserve original first error |

## 6. Backward compatibility

None. EnvironmentalRate is removed in favor of the general value, not kept as a shim. Existing raw hauling facts retain their original units/meaning and use one calculator.

## 7. Verification

ONE NEW exact bounded case: `Committed throughput: physical flow utilities and controls report private durable measured rates`. FIRST/FINAL373/0/0 in2798ms; PID43976 exit0/observed3022ms, untruncated/no observer error. Outer60000ms/termination5000ms/output30000 characters, physical waits256 Updates. One clean solution compile10.75s observed terminal before runtime; no repairs or historical/family/full/Oil/Tycoon/platform runtime.

Literal P10.acceptance.025 is matched: all four satisfaction/throughput/backpressure/control facts are pure/private and engine-derived in the current case. Original requirement text remains unchanged; only that checkbox changes. This does not close broad network provider, topology, corridor, stress or full P10/P1-P12 requirements. [Evidence](../../docs/migration/P10_COMMITTED_THROUGHPUT_VERIFICATION.md).

## 8. Risks

Window observation scans retained history; allocation/performance budgets remain open. Retention is a content policy, never an engine world-size limit. Endpoint identities label exact historical transfers, not current stock or topology; no implicit merge/rebinding. Arbitrary provider-private CLR/global mutation detection is not claimed.

## 9. Out of scope

Independent network solver/provider runtime, split/merge stress, arbitrary spatial corridors, broad regression/lockstep/soak, platform drawing/adapters, Oil/Tycoon expansion and publication before entire goal AND plan completion. Protected G19 permission remains pending.

## 10. Sub-task execution order

1. [x] Review actual journal, environmental and hauling rate owners.
2. [x] Extend contracts/journal/view and consolidate the exact rate calculator.
3. [x] Author one bounded public physical/durability/refusal proof.
4. [x] Observe one solution build and exact runtime; first attempt passed without repairs.
5. [x] Reconcile master/audit/evidence and source metadata:1362 source anchors/13 quickstart headers,677 selected inputs, zero post-FINAL executable drift; all221 literal original requirements unchanged, only P10.025 checkbox changes,172checked/49open, additional149. Paths/links/master columns/thin-driver/protected owners verified; publication condition remains unsatisfied.
