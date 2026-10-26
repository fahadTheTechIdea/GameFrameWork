# P12-D1 — Authored equipment condition and physical service

Status: historically focused verified, 2026-10-06; build2.99.0/save63. **128 checks/two exact suites** (94 contracts;
34 physical), zero failures/aborts, clean build. [Evidence](../../docs/migration/P12_EQUIPMENT_CONDITION_VERIFICATION.md).
Other33 current build assertions migrate/compile only; earlier suites not rerun. Full P12 remains partial.
Current condition codec/authoring is extended by [P12-D2 wear](P12_CONDITION_WEAR_DESIGN.md);
D1 fixtures/codec instrumentation now compile against required wear:null and codec2, but D1 was not rerun.

## 0. Problem statement

P12 has incident episodes, physical response and environmental accounting, but no equipment-condition
owner. Maintenance cannot observe or repair actual damage without a game inventing another balance.
Implement one optional named condition capability, using existing content, work, signal and entity state owners.

## 1. Goals

1. Public condition-definition/provider/change contracts work with native and independent providers.
2. Explicit JSON selects unit, maximum, initial value and damaged/failed entity states. No inferred health scale.
3. Authored damage/repair effects require positive machine work through PhysicalWorkEffectDef. WorkBoard
   retains timing, supplies, choices, cancellation and once-only completion; no instant player repair command.
4. Existing typed signals/gates read real condition, allowing shutdown and physical restart without a new gate.
5. Private detached views and staged condition Sync retain exact values and signed64 change ticks across saves.

## 2. Design

Standard/EquipmentCondition.cs supplies IConditionDefinition, IConditionProvider and IConditionChange.
ConditionSnapshot is immutable native value/maximum/unit/change-time metadata, not a damage ledger.
ConditionContracts is the sole live binding/read/admission/commit verifier. Changes carry an explicit
direction and positive UnitQuantity; bounds refuse rather than clamp, overflow or discard requested change.
Providers own one value/Sync. Native EquipmentCondition also publishes condition.value/maximum through
the existing signal schema (explicit unit.condition). Independent providers may use other declared units.

EquipmentConditionModule has no required work, map, inventory, money, operations or population module.
Definitions register condition_effects/condition_requirements under existing effect/requirement ancestry.
Unused content does not create work. Selected effects require the actual WorkBoard through native admission.
The physical effect base's protected hooks become CheckEffect/CommitEffect, removing its resource-only
terminology directly in existing implementations; no compatibility aliases or second completion protocol.

Condition requirements author source/unit/inclusive range/blocker and run inside the existing requirement
evaluator. Condition changes do not copy enabled state, gates, jobs, resource holds or episode history.
Entity.Update alone settles authored damaged/failed states; condition mutations only Touch the entity.
One optional stateless EquipmentConditionSystem at370 validates all actual provider facts during Update,
before checkpoint writes/hashes and after full restore. It has an empty payload; it does not schedule
maintenance or duplicate condition state. Native Sync stages structural quantity/chronology checks;
future timestamps require that semantic session validator. Failed fresh-world load is not rollback of
all arbitrary service state. Multiple named native providers and independent alternative-unit owners pass.

## 3. API surface

Engine-side ReadCondition(), CheckChange(World, ConditionChange), ApplyChange(World, ConditionChange).
ConditionContracts.Read/Check/Commit accept the actual provider and current session. Hosts only read
EquipmentConditionView.Read/CopyIds; normal activity commands execute authored service graphs.
No host-accessible mutable provider or condition-edit command is introduced.

## 4. Files to change

New Standard/EquipmentCondition.cs, Standard/EquipmentConditionModule.cs, Views/EquipmentConditionView.cs;
revise existing PhysicalWorkEffectDef hooks/KnowledgeEffectDef; add independent public proof, master-listed
equipment-condition JSON and two named test suites. Update current identity, tracker, parent, README/help/evidence.

## 5. Errors

Missing provider, foreign ownership/session, wrong unit/quantum, nonpositive amount and impossible bounds
refuse without mutation. Wrong provider metadata, dishonest reads/checks/commits fault normal execution.
Malformed checkpoint values/timestamps reject before replacing native condition. Pure-read protection is
scoped to actual condition entity/tick/allocation; wider cross-service purity is not claimed.

## 6. Compatibility

Development-only clean contracts. Build identity advances; existing save layouts remain unchanged,
new native component codec1/view1. No old hooks, loader, fallback, health default or repair shim.

## 7. Verification

Build solution; execute only these two exact names, repeating them for repairs if necessary:

```
Equipment condition: authored ownership, providers and checkpoint admission
Equipment condition: physical service, shutdown and saved continuation
```

Prove strict JSON, contract-only extension, private reads, malformed/pure/commit faults, no-map/no-work
composition, real travel/material/tool service, cancellation, no early repair, shutdown/resume and saved twins.
Other identity assertions compile only. No third suite, family filter or full run.

## 8. Risks

Repair may compete with later damage: recheck current exact bounds at completion; never clamp/refund.
Unresolved effects retain native retry/refusal semantics. Stale incident service uses existing binding guard.
Equipment condition is not a physical resource and must not become an inventory or environmental ledger.

## 9. Out of scope

Automatic wear/inspection scheduling, operating/fleet service policies, preparedness reserves, arbitrary
operation gates, contamination/weather formulas, full provider/replay/stress/performance regression,
Oil/Tycoon/host/demo/platform/Draw work and full P12/P1–P12 acceptance remain open.

## 10. Sequence

Contracts/sole provider and Sync → native effect/requirement/signal/view → public independent provider
and JSON → exactly two focused gates → scoped evidence and tracker updates. Full goal stays active.
