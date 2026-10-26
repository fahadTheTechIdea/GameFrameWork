# P4-G2c5: Explicit activity input rate periods

## 0. Problem

G2c4 measures funded productive game duration, but elapsed input quantities still use the selected
node duration as their rate denominator. Reusing a requirement on a longer activity therefore changes
its fuel rate. The engine already owns exact rational resource rates and their JSON reader; reuse them.

## 1. Goals

1. Elapsed requirements author nonempty `rates` arrays through existing ResourceRateSpec/IResourceRate.
2. Each input has an independent positive rational game-time period, unrelated to work duration.
3. Preview, automatic supply, actual whole-set consumption and detached usage share the same rates.
4. Funded exposure stays in the existing node; save/hash/clock owners and layout do not change.
5. Missing/old quantities-only elapsed content, conflicting fields and invalid rates refuse before play.

## 2. Design

IResourceWorkRequirement exposes Rates alongside Quantities. For elapsed inputs, Quantities is a
derived native reference bill of those rates, not another authored bill. Nonelapsed requirements retain
their explicit quantities and have no rate intent. ResourceWorkRequirementDef delegates elapsed
authoring/resolution to ResourceRateSpec. WorkBoard scales the same native reference quantities and
rates with the existing requirement multiplier. Immutable candidate/source choices carry those exact
scaled rates; their codec resolves them from the manifest-bound requirement, not another saved ledger.

Work-normalized cumulative consumption retains its bounded work formula. Elapsed consumption instead
delegates to QuantizedGameRates.AccumulatedRaw using productive exposure and the session quantum.
No rate period is rounded to a game update or to the node duration. Existing cumulative differences,
whole-set transactions, cost/tool binding and logistics demand remain the execution authority.
WorkResourceUsage exposes immutable rates so hosts do not infer the rate from node duration.

## 3. API / JSON

Elapsed leaf example:

```json
{"id":"drill.fuel","use":"elapsed","rates":[
  {"resource":"fuel","quantity":1,"period":{"numerator":2,"denominator":1,"unit":"minute"}}
]}
```

Consume/hold/continuous retain `quantities`; elapsed refuses that old field rather than supporting it.
Periods smaller than or unaligned to the session quantum remain exact rational rates.

## 4. Files

Core/WorkResourceSelection; Standard/WorkRequirements, ResourceWorkRequirements, WorkContinuous,
WorkResources, WorkStart, WorkModule; Kernel/SaveGame build identity; current build assertions;
ContinuousWorkTests/ResourceRequirementTests; existing master-listed elapsed-work.json;
stage/evidence/master/parent/README/help/AGENTS.

## 5. Errors

Empty/missing/duplicate rates, unknown resources/units, zero/negative periods, illegal native quantities,
extra quantities and provider bill/rate disagreement fail explicitly. Missing stock remains a typed wait;
no partial resource/payment/progress commit. Native representational overflow refuses, never clamps.

## 6. Compatibility

Breaking content/contract/view/build identity in place. No old elapsed loader, alias or compatibility path.
Existing node/receipt layouts remain unchanged; old builds reject through existing save identity.

## 7. Verification

Clean solution build, then only these two complete named suites:

- Activity resources: authored scoped offers, native sets and private selection
- Activity resources: physical consumption, held tools, interruption and saved ownership

Extend existing probes for independent periods, duration reuse, exact fractional periods, multipliers,
native rounding, immutable/private views, no clock advance during preview, actual hauling, pause,
shortage and saved twins. No third/family/full suite.

## 8. Risks

Cumulative quantization intentionally prepays native units. Quantity per authored period must itself
be legal in the resource's native unit. Endpoint-specific coarse quanta and broader providers remain
separate open acceptance; this stage does not claim them.

## 9. Out of scope

Standby/idle/travel fuel, additional clocks, new scheduler/store, Oil/Tycoon/hosts/demos/platforms/Draw,
full regression/replay/stress and completion of the full P1-P12 roadmap.

## 10. Execution

- [x] Replace elapsed content and contracts with exact independent rate intent.
- [x] Reuse shared cumulative rate math in all existing admission/advancement/view paths.
- [x] Extend the two existing gates using real JSON, normal commands and actual resource flows.
- [x] Clean build, focused gates, evidence and tracker/docs update.

Focused result: build2.149.0/save77/WorkBoard24/WorkView41; clean build,485 checks/two exact suites
(140 admission;345 physical), zero final failures/aborts. No third/family/full gate.
[Evidence](../../docs/migration/P4_ACTIVITY_RATE_PERIODS_VERIFICATION.md).
G2c5 independent productive-input periods are closed; standby/provider breadth and full roadmap remain open.
