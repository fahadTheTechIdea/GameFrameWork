# P12-D2 — Authored game-time condition wear

Status: historically focused verified, 2026-10-06; build2.100.0/save64, condition codec2/view2/system2, signal1.
**101 checks/two exact suites** (73 contracts;28 physical), zero failures/aborts, clean build.
[Evidence](../../docs/migration/P12_CONDITION_WEAR_VERIFICATION.md).34 other identity assertion files
and D1 fixtures/codec instrumentation migrate/compile only; previous suites not rerun. Full P12 remains partial.

## 0. Problem statement

D1 provides one condition value and physical repair, but no actual time-based degradation. D2 extends
that owner with an optional exposure clock and open JSON-selected wear rules. Automatic maintenance
posting/inspection remains D3: it must consume this source and the existing WorkBoard, not another scheduler.

## 1. Goals

1. Explicit wear:null/profile on native condition; selected master-listed profiles author exact units,
   positive quantity/period and explicit signal conditions. No assumed idle/operating/travel wear.
2. Shared exact cumulative rate arithmetic serves existing resources and condition; no per-tick rounding
   drift or saved fractional remainder. Whole exposure ages remain signed64; cumulative raw math is wide.
3. All eligibility observations precede all mutations in the existing condition system370. Decisions
   cannot cascade through a changed signal in the same pass. Each provider advances at most once per tick.
4. Native and independent authored rules share contracts and provider-owned exposure/condition Sync.
   Wear saturates at zero by the declared degradation rule, never invents negative condition.
5. Private wear diagnostics, pure/stale/dishonest-provider checks, no-map/no-work composition and physical
   service/save continuation are proved by exactly two named suites.

## 2. Design

IConditionWearDefinition extends existing definition metadata with an optional IConditionWearRule.
IConditionWearProvider exposes provider-owned ConditionExposureSnapshot and ApplyWear over an engine-issued
ConditionWearContext plus captured immutable decision. Rule definitions are pure existing-catalog content,
not a new registry. ConditionWearContracts validates actual binding, clock, pure rule observation and exact
condition/clock commit. The current EquipmentConditionSystem captures a complete pass before applying
decisions, then validates actual state; it remains the only condition coordination owner, with no backlog.

Native EquipmentCondition embeds exposure count/last accepted tick in its sole codec2. Repair changes
condition only; it never erases exposure rounding history. Blocked/offline/exhausted/no-rule reads earn no
exposure. Eligible zero-quantum steps advance exposure without fake condition loss. Native wear computes
floor(total after)-floor(total before) at actual unit quantum, explicitly limited by actual remaining
condition. This saturation is authored wear behavior, not clamping explicit player damage/repair requests.

Extract QuantizedGameRates.AccumulatedRaw from ResourceRates' existing arithmetic; ResourceRates delegates
to it and keeps catalog/capacity/Bill validation. Native wear uses the same raw calculator before narrowing
only its bounded delta. Periods need not align to ticks: exact rational rates intentionally accumulate.
Shared SignalGateCondition owns authoring/readiness; no new operation gate, signal parser or enabled value.

## 3. API surface

Engine: IConditionWearRule.Observe(context)/ValidateBinding(world,entityDefinition), provider ReadExposure/
ApplyWear, shared contracts. Hosts: existing EquipmentConditionView.ReadWear gives detached definition,
exposure ages, next bounded quantity and typed availability. No wear-edit or instant maintenance command.

## 4. Files

New Core/QuantizedGameRates.cs and Standard/ConditionWear.cs; revise ResourceRates, EquipmentCondition,
its module/view/identity. Add master-listed wear JSON, independent public rule and two named suites;
migrate required explicit wear:null/current identity assertions and update stage/master/parent/help/evidence.

## 5. Errors

Missing/wrong unit, profile/source/conditions, nonpositive rate/period, discrete quantum and content alias
violations fail explicitly. Unavailable signals preserve their source blocker. Negative/future/inconsistent
exposure refuses checkpoint/restore; stale context/dishonest reads/rules/commits fault actual Update.
Pure guards remain scoped to actual entity/tick/allocator, not every cross-service mutation or load rollback.
The same structural exposure predicate serves staged native decode and live contract observation.

## 6. Compatibility

Development-only direct change: build2.100.0/save64/native condition codec2/view2/system2. No absent-field
default, old codec/loader or compatibility hook. Other native owner payloads remain unchanged.

## 7. Verification

Build, then only the exact names below. Other assertions migrate/compile only; no full/family/third gate.

```
Condition wear: authored rates, exposure contracts and checkpoint guards
Condition wear: signal-gated degradation, physical service and saved continuation
```

## 8. Risks

Eligibility can depend on another condition: capture all before mutation. Repair must not reset cumulative
quantization. Wide cumulative loss must not narrow to Fix before subtraction. Invalid commit cannot resume
or create compensation; actual condition and work remain sole authorities.

## 9. Out of scope

Automatic inspection/service posting, operating/fleet policy commands, preparedness reserve jobs/materials,
native weather/chemical wear formulas, arbitrary operations, replacement/reference migration, contamination,
broad provider/replay/stress/performance/full P12/P1–P12 gates. No Oil/Tycoon/host/demo/platform/Draw edits.

## 10. Sequence

Shared rate arithmetic → wear metadata/rule/contracts → same provider/system/view/codec → authored JSON
and public proof → exactly two focused gates → scoped evidence/tracker. Full framework goal stays active.

## 11. Subsequent slice — D3, now separately focused verified

[Recurring inspection/service](P12_RECURRING_MAINTENANCE_DESIGN.md) implements posting through the existing
board; this D2 suite was not rerun. The following original handoff remains the design boundary:

Author inspection/service plans and threshold/cadence requirements in master-listed JSON. Extend existing
WorkBoard/plan policy and actual condition providers for deterministic posting, deduplication, pause,
cancel/retry and saved continuation. Ordinary machine travel, stock/tool/fuel claims and completion effects
remain the only service execution path. No private repair queue, instant restoration or host-side decision.
