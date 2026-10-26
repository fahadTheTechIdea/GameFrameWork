# P8-B2 — Elapsed-time operating inputs

Current [P10-A5 generic resource utility consumers](P10_UTILITY_CONSUMERS_DESIGN.md) replaces the old
operating-input contract/fact with IUtilityConsumer/UtilityDemandSnapshot. Same running-input state/
stock/consumption owner, now explicit current demand/available/satisfaction/private signals. Build
2.141.0/save76/operations view9;146 checks/two exact expanded running-input suites. Evidence below
is preceding source, not another current count. Utility allocation/physics/full framework remain open.

Current shared-math integration: [P10-A2c network rates](P10_NETWORK_RATE_DESIGN.md) extends the
existing cumulative QuantizedGameRates for receiver batches larger than the authored native rate
quantity. Existing process/native rate admission still validates its own units; no second math,
resource or progress owner. Current network proof is86 checks/two exact suites on2.126.0/save69;
these historical process suites compile only, not rerun. Full process/provider/durability gates stay open.

Status: focused-verified on2.83.0/save53/transformer5/transformation-view6:98 checks/two exact suites
(44 contracts;54 physical), zero failures/aborts; clean build, zero warnings/errors,2.52s.
[Evidence](../../docs/migration/P8_RUNNING_INPUTS_VERIFICATION.md). Full P8/P1–P12 remain partial.
Manager2/work22/logistics5/work-view27/logistics-view10/flow5 and inventory layouts unchanged.
No compatibility path; this is local elapsed-time consumption, not complete utilities/continuous output.

## 0. Problem

`Transformer.AdvanceBatch` advances work without drawing running resources. Signal gates describe
readiness, not consumption. WorkBoard's continuous inputs are total work-normalized budgets;
reusing that budget as fuel/time would incorrectly make faster machines burn the same total fuel.

## 1. Goals

1. Every transformation explicitly authors `operating_inputs`, including [] for none.
2. Native processors consume exact resource/time rates during successful running updates only.
3. Complete next-draw admission precedes startup/payment and earned progress; ordinary shortage
   spends nothing and preserves real WIP/tools/progress.
4. Existing named logistics intent supplies running shortages physically, without a second ledger.
5. Detached private facts and the sole processor codec retain elapsed running time through saves,
   controls, output waits and cancellation. Burned inputs never return with raw WIP.

## 2. Design and owners

`Core/ResourceRates.cs`: immutable `IResourceRate`/`ResourceRate` holds the actual catalog resource,
positive native quantity and positive rational period. `ResourceRates.Consumed/Next` is the sole
stateless cumulative calculation: ceiling(elapsed session seconds * native rate / endpoint quantum)
times that quantum. BigInteger intermediates preserve rational precision; no per-update rounding,
saved fractional fuel or inferred units. Representational overflow has a typed process blocker.

`TransformationDef` resolves explicit unique rows through the existing resource catalog. Native
definition validation checks actual local inventory acceptance/quantum and complete startup plus
first-draw capacity. No new section, rate registry, catalog or provider fallback.

`ITransformerOperatingInputs` exposes the actual operating store. `TransformationSnapshot` adds
explicit immutable running facts (endpoint/version/elapsed/consumed/next/blocker); providers without
this capability report None and cannot support rate-bearing definitions. Shared contract validation
feeds execution/checkpoints/private views, never a second progress owner.

`Transformer` embeds one running-tick counter in its existing Sync. One successful working Update
earns one session quantum independently of target work rate. Startup checks combined raw/tool/first
draw availability before acquiring WIP or charging. Each draw uses the existing whole-Bill
`IResourceTransaction.EvaluateInventory/CommitInventory` lifecycle; actual available stock excludes
tools and other promises. No new fuel claim ledger. Progress and elapsed advance only after commit.
Pauses, gates, shortages, completed-output waits, Returning and idle consume no running inputs.
Completion/actual return reset the counter; cancellation never refunds burned resources.

Named automatic supply requests contain the next actual draw, not a fabricated whole-batch fuel
budget. TransformerManager accepts Working intent only when that selected provider has nonempty
next operating demand. Existing logistics rows, jobs, stock, receipts, travel and handling stay sole
owners; expired intent never destroys acquired cargo. Disabled/cancelled/completed intent expires.

## 3. API / JSON

```json
"operating_inputs":[{"resource":"fuel","quantity":1,
  "period":{"numerator":3,"denominator":1,"unit":"minute"}}]
```

Quantity uses the selected resource's native unit. [] means no running requirement. Existing
enqueue/cancel/control commands remain the player API; no host timer or additional Update loop.

## 4. Files

Modify Transformations.cs/TransformationsWip.cs/TransformationContracts.cs/TransformationModule.cs,
engine identity and existing proof JSON/callers; add ResourceRates.cs and TransformerOperatingInputs.cs.
Add two focused test suites and a JSON-only public proof through the existing master/module loader.
Update parent plans/master/README/AGENTS/help/evidence together.

## 5. Refusals and faults

- Missing/duplicate/unknown/non-native/zero rows: content refusal before Initialize.
- Whole resource shortage: typed operating-input blocker, unchanged progress/elapsed/stock/payment.
- Overflow: typed rate-range blocker, no spending/progress or fabricated quantity.
- Lying/mutating provider or failed committed lifecycle: ordinary Update faults; no resumable save/hash,
  no compensating stock or success prefix. Reuse current resource protocol guards.
- Wrong company/source metadata or malformed elapsed phase: checkpoint refuses.

## 6. Compatibility

Dev-only direct migration: bump build/save/native component/view identities together. All current
transformations explicitly author []. No old loader/constructor/codec/shim survives.

## 7. Verification

Only these exact suites may run for this slice (repeats for repairs allowed):

1. `Transformation running inputs: authored rates, admission and private saved facts`
2. `Transformation running inputs: physical supply, controls, cancellation and timed consumption`

Build `Beep.SimEngine.slnx`; other suites compile/migrate only. Bounded physical waits include actual
phase/blocker/jobs; test rational/discrete/wide math, changed rates, protected tools, mixed draws,
startup shortage, saved twins, private reads and fuel-only physical supply. Full gate remains open.

## 8. Risks / non-goals

Cumulative ceiling can purchase one quantum early; it never repeats that purchase on pause/resume.
The final earned update consumes a whole running quantum even when work finishes partway through it.
This is an explicit discrete Update model, not sub-tick wall-clock simulation. No power-network solver,
idle/travel fuel, remote ports, continuous output, catalysts, new scheduler, Draw, platforms or oil rules.

## 9. Execution order

- [x] B2.1 Rate contracts/content + direct fixture migration.
- [x] B2.2 Native running draw, lifecycle/supply/checkpoint/private facts.
- [x] B2.3 Exact two-suite gate and evidence/docs; broader gates remain open.
