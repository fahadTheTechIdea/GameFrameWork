# P4-G27 — Contract-owned condition and wear bindings

## 0. Problem

EquipmentCondition uses native EquipmentConditionDef metadata, and wear resolution/validation
selects ConditionWearRuleDef rather than IConditionWearRule. Exposure/decision/admission reads
use incomplete manual observation checks; public condition-ID enumeration publishes live getters.

## 1. Goals

1. Original condition runtime accepts independent JSON metadata with the same four-field codec.
2. ConditionDeclaration and EquipmentConditionDeclaration are the sole readers/resolvers.
3. Wear rules resolve through their actual IConditionWearRule catalog identity, not inheritance.
4. Original condition/wear owners guard metadata, exposure, decisions and admission with existing
   provider-codec/entity/one-clock observation; publish outputs only after successful validation.
5. A new thin-player proof exercises earned wear, physical repair, saved continuation, replay,
   private detached views, malformed declarations and callback faults.

## 2. Design

IEquipmentConditionDefinition extends existing wear/signal metadata with authored state bindings.
EquipmentCondition becomes Component consuming that contract. Native and independent declarations
share original parsing, unit/quantity validation and state resolution. No new clock, condition state,
wear scheduler, repair path, resource owner or save model. Existing ConditionWearContracts captures
decisions before commits, verifies the actual rule/unit alias, and stages out contexts until successful.
ConditionContracts.Check and condition-ID enumeration reuse existing observation and detached reads.

## 3. API

New public metadata contract and declarations; existing commands/views and codec2 unchanged.
Wear extension implements IConditionWearRule directly; redundant abstract rule definition removed.

## 4. Files

EquipmentCondition.cs, ConditionWear.cs, EquipmentConditionView.cs; existing proof condition/wear
definitions; new ConditionBindingProof.cs and ConditionBindingTests.cs; reused PolicyBindingTests
authoring helper; Program/current protocol; stage, evidence, master and completion audit.

## 5. Errors

Missing references, malformed values/state links/units and invalid actual aliases refuse before
Initialize. Pure callback exceptions or scoped mutation latch the original observation fault and
withhold outputs. No default profile, substitute provider, partial checkpoint or compatibility path.

## 6. Compatibility

Development only; remove concrete dependencies in place. No legacy readers or duplicated runtime.

## 7. Verification

Build when compilation needs checking. ONE NEW bounded exact case only:
Condition bindings: independent metadata and wear govern physical service.
Repairs rerun only that new case. Historical/full/family/Oil/Tycoon/platform gates remain unrun.

## 8. Risks

Scoped original provider observation is not a world/CLR sandbox. All mutation transaction breadth,
all provider codecs, network/stress/performance/global acceptance remain separate open gates.

## 9. Out of scope

Tycoon/catalog authority, business/default economics, renderers/platform/Draw, publication.

## 10. Sequence

Shared declarations/native contract; original guarded binding/evaluation owners; detached views;
independent extension and thin physical proof; actual evidence and tracker synchronization.

Status: narrowly verified, core2.362.0/save154/replay1; NEW exact bounded case final743/0/0 in1873ms.
[Actual evidence](../../docs/migration/P4_CONDITION_BINDINGS_VERIFICATION.md).
Three executions of this same new case (518/0/0, strengthened743/0/0, final743/0/0);
no failed runs, historical/family/full runtime, executable edits after final, staging/commit/push.
Full P1-P12 and active goal remain OPEN; pending Tycoon type-binding authority unchanged.
