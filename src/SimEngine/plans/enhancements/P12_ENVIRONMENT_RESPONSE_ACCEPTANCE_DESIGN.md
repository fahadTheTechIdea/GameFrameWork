# P12-A8: physical environmental accounting and durable rates

## 0. Problem statement

Original P12.acceptance.006 requires environmental totals to account for losses/recovery and derived rates to survive saved continuation. Existing EnvironmentalMetrics.cs classifies committed ResourceFlowJournal facts and computes rational rates from the sole game clock. Verify this through actual native physical execution, not driver-authored journal entries.

## 1. Goals

1. Positive-time physical isolation commits one stock loss; cancellation/resume does not duplicate it.
2. Finite waste WIP remains paused under unsafe operation and resumes once after physical response; native recycling converts two waste into one reusable steel and one actual residue.
3. A physically staffed eight-minute activity destroys that actual residue once, not on command submission.
4. Lifetime quantities/counts and exact rational rates remain correct through pending/partial/blocked/completed saves, pause, twins and replay under minute/half-minute clocks.
5. Authored finite journal eviction preserves cumulative totals, refuses incomplete windows, and later certifies a complete quiet zero-loss/rate window. Kind/cause filters and company privacy remain exact.

## 2. Design

Use existing Standard/EnvironmentalMetrics.cs, ResourceFlows.cs, native inventory effects, transformations, Incident/ResponseWork/WorkBoard, resource transactions and safety gate. No additional gameplay owner or ledger. Master-listed environment-response-acceptance.json defines metrics, real loss/disposal effects, timed waste-recovery recipe and staffed disposal activity. PhysicalRecoveryScenario selects them through an optional pure authoring flag; all defaults remain unchanged. Existing cleanup work uses a held real steel tool, not consumption of waste already held in recycling WIP.

EnvironmentalResponseAcceptanceTests drives ordinary player production/incident/haul/cancel/work commands and detached observations. It verifies full stock+WIP+produced=initial+removed conservation, actual journal eviction, exact units per game time and saved observations. It never writes a journal fact, world, actor position, progress or claim. Existing A6 observation helpers are reused.

## 3. API surface

No production API/schema change planned. Existing EnvironmentalMetricsView, ResourceFlowView, WorkView, transformation/lifecycle facts and normal commands. All authoring uses current JSON contracts/master content selection.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| New | tests/ExtensionProof/content/environment-response-acceptance.json | 20 | Medium |
| New | tests/Beep.SimEngine.Tests/EnvironmentalResponseAcceptanceTests.cs | 130 | Medium |
| Modify | tests/Beep.SimEngine.Tests/PhysicalRecoveryScenario.cs | 15 | Low |
| Modify | tests/Beep.SimEngine.Tests/Program.cs and tests/ExtensionProof/ExtensionProof.csproj | 2 | Low |

## 5. Error handling

Unavailable/incomplete history has typed refusal, never invented zero. Zero elapsed time has NoElapsedTime rather than a rate. Every physical stage bounds512 Updates with native phase/operation/job diagnostics; outer90000ms/termination5000ms/output30000 characters. Observe actual build/runtime handles to terminal, never restart solely on observer timeout.

## 6. Compatibility

Development only. No shim, duplicate environmental stock/clock or old execution path. Existing scenario flag defaults preserved.

## 7. Verification

One solution compile then ONE NEW exact case: `Environment acceptance: physical loss recycling disposal and durable exact rates`. Same-case repairs only; no historical/family/full runtime. [Evidence](../../docs/migration/P12_ENVIRONMENT_RESPONSE_ACCEPTANCE_VERIFICATION.md). Original compound .006 checked only after the complete physical and durability proof succeeds.

## 8. Risks

Lost stock cannot be recovered by granting it back: recovery must consume actual independently retained waste through the native recipe. Feed eviction invalidates windows but not cumulative amounts. Fixture physics/fuel/declared quantities remain explicit, never bypassed to satisfy a check.

## 9. Out of scope

OilFieldDays, Tycoon/business, combat/rendering/platforms, arbitrary custom environmental providers, propagation/weather/world-layer admission and full goal/publication.

## 10. Execution order

- [x] Review native environment/journal/inventory/response/production owners.
- [x] Author one master-selected physical recovery/disposal/metric document and thin driver.
- [x] Compile and run one bounded case, retaining every attempt.
- [x] Refresh original tracker, audit anchors/hashes, evidence and exact remaining scope.
- [ ] Full regression unrequested and unproven.
