# P4-G27 condition/wear bindings — scoped evidence

2026-10-09, core2.362.0/save154/replay1; original equipment/provider/view state schemas unchanged.
Full P1-P12 and active goal OPEN. No staging/commit/push.

## Original owners and contracts

ConditionDeclaration is the sole condition identity/unit/band reader/resolver, shared by native and
existing independent proof metadata. EquipmentConditionDeclaration extends that metadata with
explicit optional wear and original immutable state bindings. IEquipmentConditionDefinition exposes
those authored contracts. Original EquipmentCondition is Component consuming that metadata rather
than EquipmentConditionDef; the same value/change-time/exposure/update-time fields and codec2 remain.

Removed redundant ConditionWearRuleDef. Native and proof rules implement IConditionWearRule directly;
native authored and independent rules reuse sole ConditionWearDeclaration, original signal gates and
QuantizedGameRates. Resolution, live rule lookup and composition validate actual interface-owned
catalog/unit aliases, not native inheritance. All loaded entity condition definitions are validated
for actual named identity/unit/bands/quantum/provider shape before Initialize, including unused ones.
Equipment states alias original catalog entries; capability shape validation uses contracts.

ConditionContracts.Check replaces manual scalar-only admission checks with original actual provider
observation. ConditionWearContracts guards complete exposure/metadata/rule/decision reads through the
same codec/entity/one-clock boundary and stages its out context until all guards succeed. Actual rule
aliases are verified even when exhausted/already updated. Original pass capture, context freshness,
exact mutation checks, saturation, exposure accumulation and physical service remain authoritative.
Condition ID views stage validated detached IDs. No duplicate condition/exposure state, scheduler,
clock, rules cache, provider selection store, host gameplay logic, compatibility or default profile.

## Thin player and independent extension

IndependentConditionDef creates the ORIGINAL EquipmentCondition runtime. IndependentWearDef implements
the public rule interface directly. Both use shared declarations; no copied parsing or rate evaluator.
Existing PolicyBindingTests authoring helper is extended in place, explicitly selecting master modules
and documents. Original independent condition proof now uses ConditionDeclaration and has a transient
pure-admission callback probe. Probe modules own deliberate callback/codec fault injection only.

Public JSON/commands/Update/views workflow proves three minute updates earn exactly one wear unit,
fractional saved continuation, one-clock pause, eighteen-minute wear reaching safety shutdown,
disabled exposure holding, compatible machine policy, physical spare/tool hauling and thirty-minute
repair, three spares consumed and two tools conserved, retained cumulative exposure, explicit disable/
latch acknowledgment, saved disabled twins, re-enabling earned wear, replay and settled save identity.
No test-side condition/resource/timer/assignment mutation implements healthy gameplay.

Four native/independent condition x wear metadata combinations physically advance discrete native
quantum: two updates accrue exposure without damage, the third loses exactly one after saved continuation.
Explicit wear:null stays disabled; no inferred/default profile. Views remain company-private and retained
facts detached. Missing-provider and three missing-session boundaries are explicit.

80 metadata/rule observation combinations across wear view, direct admitted context, condition view and
ID enumeration plus four independent change-admission faults (84 total) exercise exception, clock pause,
entity identity allocation and hidden mutation of original persisted fields. Outputs/contexts/ID lists
withheld; first observation.provider fault retained; no healthy hash/save/Update/callback retry.
Sixteen malformed actual condition/wear variants, including unused metadata, refuse before Initialize.

## Actual build and bounded runtime

Four clean compilation builds:
dotnet build Beep.SimEngine.slnx --no-restore.
Actual elapsed:7.28s,7.91s,3.98s,5.77s; zero warnings/errors.
Build2 compiled strengthened checks; build3 compiled the corrected direct-context proof before execution.
Build4 removed concrete runtime shape validation in favor of capability contracts.

Only ONE NEW exact bounded case was executed, three times:

~~~text
dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build -- --bounded-exact "Condition bindings: independent metadata and wear govern physical service" --timeout-ms 30000 --termination-timeout-ms 2000 --output-chars 24000
~~~

1.518 passed/0 failed in1696ms; pid42424, exit0, observed1846ms.
2.743 passed/0 failed in1838ms; pid31428, exit0, observed1988ms: added direct out-context,
  native/independent discrete combinations, null wear, and independent mutable admission faults.
3.Final743 passed/0 failed in1873ms; pid27320, exit0, observed2025ms: contract-based provider shape validation.

Zero aborted cases; no truncated output or observer errors. Final85 executable/fixture inputs frozen
2026-10-09 09:48:58 UTC and retained unchanged after final. No executable edits or runtime after final.
This is scoped proof, not a full regression or completion certificate.

## Remaining acceptance

G26 and all older runtime evidence historical, not rerun on2.362.0. Full capability decomposition,
whole CLR/content/codec sandbox, cross-provider mutation outside declared scopes, all mutating
condition/wear/resource transactions, spatial intent deep immutability, every external codec,
multi-lane/network/continuous/technology/removal/concurrency/lockstep/fuzz/soak/performance and
all P1-P12 acceptance remain unproved. Pending excluded Tycoon catalog type-binding authority unchanged.
No business/default/domain/platform/Draw/clock changes; no full/family/historical/Oil/Tycoon/platform
runtime, staging, commit, push or publishing.
