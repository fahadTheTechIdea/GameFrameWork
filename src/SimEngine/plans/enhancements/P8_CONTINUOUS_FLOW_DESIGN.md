# P8-C1 — Continuous native transformations

Current physical wear/accounting extension on unchanged2.723: [M75 original-owner
design](P8_CONTINUOUS_CATALYSTS_DESIGN.md), [actual supplied/blocked/refilled/stopped
evidence](../../docs/migration/P8_CONTINUOUS_CATALYSTS_VERIFICATION.md#current-m75-physically-hauled-continuous-catalyst-accounting--2026-10-22).
Two new clock variants9472/0/two full-final-hash replays, no native edit or all-provider
claim. Initial C1/shared-math records below retain their actual historical scope.

## Historical C1 design and shared math integration

Earlier shared-math integration: [P10-A2c network rates](P10_NETWORK_RATE_DESIGN.md) extends the
same cumulative QuantizedGameRates for larger receiver batch quanta. Transformer admission retains
native-unit checks and its existing progress/stock/transaction owners. Current network proof is86
checks/two exact suites on2.126.0/save69; these historical transformation suites compile only, not
rerun. No utility solver or full transformation/provider/durability acceptance is implied.

Status: focused native slice verified on 2026-10-05. 2.84.0/save54/transformer6/transformation-view7;
112 checks/two exact suites (46 contracts;66 physical), clean build. See
[evidence](../../docs/migration/P8_CONTINUOUS_FLOW_VERIFICATION.md). Full framework goal remains partial.

## 0. Problem

Native Transformer only finishes timed batches. AutoRepeat starts more batches; it is not continuous
input/output flow. The existing ResourceRates math handles elapsed fuel but not effective fractional
processing time or output-floor quantization. Reuse these owners instead of adding another process loop.

## 1. Goals

1. Explicit JSON `mode: batch|continuous` selects native transformation semantics, no inferred mode.
2. Continuous input/output rates use existing native resource/time contracts and actual fractional
   effective progress. Input cumulative ceiling and output cumulative floor never round per update.
3. Every successful update commits its complete input/fuel/output set through existing transactions.
   Shortage/backpressure/disabled/gates preserve earned progress, tools and time without spending.
4. Running requests remain actual named logistics demand; physical travel/handling still applies.
5. Immutable private facts and the sole processor codec survive pauses, cancellation and saved twins.

## 2. Design / owners

`Content/ResourceRateSpec.cs` is the sole JSON rate reader/resolver used by operating inputs and
continuous inputs/outputs. It resolves the existing catalog, not another rate registry. Batch uses
native Bill objects and positive duration. Continuous uses rate arrays, explicit duration:null,
entity_output absent and cancel_policy:stop. Batch retains return_inputs. Tools/operating inputs stay
explicit in both modes. Continuous requires a nonempty output rate list, and may explicitly author
empty inputs for a generator. Repeat is invalid for a never-finishing stream.

`ResourceRates.Accumulated/Between` directly replace the earlier whole-tick methods, with required
WorkProgress and ResourceRateRounding. One exact cumulative calculation owns both ceiling inputs and
floor outputs at actual endpoint quantum. No old overload/wrapper, per-tick round or new remainder
ledger. `WorkProgress.AdvanceUnbounded` adds checked fractional throughput for indefinite processing;
batch advancement keeps its actual stop-at-requirement semantics.

Transformer remains the only native executor. Existing Progress owns effective processing time;
existing running counter owns elapsed successful updates. They represent different facts, never
copied balances. Continuous startup owns tools through the existing WIP execution identity, with
empty raw WIP. Each update computes actual next progress, input/output deltas and elapsed fuel.
Whole input admission and output capacity admission precede startup/payment or actual input claims.
Temporary exact input claims use the existing execution-derived flow owner, consume within the same
whole ResourceConversion and never persist as a second promise ledger. Output capacity is protected
by that same conversion protocol before spending. No partial output/fuel success or compensation.

`ITransformerFlow` exposes actual input/output stores; required detached TransformationFlowSnapshot
reports actual cumulative processed inputs/outputs and next deltas. The shared contract validator
checks metadata/phase/current authored math on Update/checkpoint/load. Other providers explicitly
report None and cannot accept continuous definitions without the capability. Native source/destination
are the same actual local inventory in this stage; distinct/remote ports remain later contract work.

Working continuous input/fuel shortages request their combined actual next Bill through existing named
logistics. Backpressure withdraws unacquired intent, not acquired cargo. Successful updates also withdraw
that request. Disabled/gated work earns nothing. Cancellation stops the stream and releases actual tools;
already consumed native inputs/fuel are not refunded and fractional prospective outputs are not created.
Explicit stop semantics make that quantization consequence visible, not hidden material disposal.

Flow.NextInputs is process feed only; Operating.NextInputs is elapsed fuel. The engine combines both
for actual native admission/supply/conversion. Consumed/Produced describe the current execution's
cumulative earned rate facts, not current inventory or session totals. The existing journal alone
records actual historical consumption/production. Shared FlowFacts belongs to TransformationContracts,
not the native executor, so provider validation does not depend on a concrete processor implementation.

## 3. JSON / public API

```json
{"id":"game.refine","mode":"continuous","cancel_policy":"stop","tools":{},
 "operating_inputs":[],"duration":null,
 "inputs":[{"resource":"game.feed","quantity":2,"period":{"numerator":1,"denominator":1,"unit":"minute"}}],
 "outputs":[{"resource":"game.product","quantity":1,"period":{"numerator":1,"denominator":1,"unit":"minute"}}]}
```

Use existing named enqueue/control/cancel commands and management views. No new player/admin instant
resource grant, host timer, scheduler, queue, Draw or platform callback.

## 4. Files

Modify existing TransformationDef/Transformer/TransformerManager/contracts/Sync/view registration,
ResourceRates/WorkProgress and current fixtures/callers; add ResourceRateSpec.cs and TransformerFlow.cs
as authoring and another partial file of the existing native owner. Add JSON-only proof and two suites.
Update stage/master/parents/README/AGENTS/help/evidence together.

## 5. Refusal / fault policy

Invalid modes/shapes/periods/native quantities/capabilities/results/cancel/repeat refuse explicitly.
Ordinary whole shortage/capacity wait changes no physical stock or progress. Native representation
overflow reports a typed blocker without fabricated limits. Actual mutation after successful preflight
must verify through current protocols or fault Update; faulted state cannot checkpoint. No compensation.

## 6. Compatibility

Direct breaking migration. All current batch JSON authors mode:batch; old rate methods/constructor
layout are deleted. Build/save/component/view identities advance together. No old reader/default/shim.

## 7. Verification

Only these exact suite names may run (repair repeats allowed):

- `Continuous transformations: authored modes, quantization and private contracts`
- `Continuous transformations: physical flow, waits, cancellation and saved throughput`

Build Beep.SimEngine.slnx. Other suites migrate/compile only, no full/family/third suite. Prove fractional
rates, simultaneous products/fuel/tools, shortage/backpressure, changed targets, progressive physical
input supply, independent saved sessions and cancellation; bound physical waits with diagnostic snapshots.

## 8. Risks / exclusions

Output space must fit before consumption, matching the existing whole conversion's actual capacity
protocol. Do not borrow hypothetical future freed space. Native quantization intentionally pre-pays
input quanta and delays output quanta; stop never converts unearned fractions into stock. No arbitrary
worker requirement, remote ports, catalysts, utility solver, scientific oil yields, P5/Tycoon or platforms.
Full provider/fault/replay/stress/accounting and P1–P12 gates remain open.

## 9. Execution order

- [x] C1.1 Shared explicit rate authoring, fractional math and direct mode/caller migration.
- [x] C1.2 Native continuous whole conversion, private contracts and physical named supply.
- [x] C1.3 Exact two-suite verification and evidence/docs; full goal remains active.
- [ ] Later continuous providers/remote ports/catalysts/utility/priced admission and full durability gates.
