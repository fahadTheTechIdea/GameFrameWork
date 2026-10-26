# P4-G18 — Open authored unit contracts

Status: scoped verified, 2026-10-09;87 checks/zero failed/zero aborted. Full P1–P12 remains open.

ONE NEW exact bounded case FIRST/FINAL87/0/0 in1279ms; PID22508 exit0/observed1472ms.
One clean16.83s build/zero warnings/errors; core2.350.0/save154/replay1/state-view schemas unchanged.
26-input source freeze06:19:42UTC. No historical/family/full/Oil/Tycoon/platform runtime.
[Evidence](../../docs/migration/P4_UNIT_CONTENT_CONTRACTS_VERIFICATION.md).

## 0. Problem

Resources, capacity policies, spatial layers, quantity signals, environmental metrics, equipment
condition and operation policies expose runtime IUnitDefinition but select concrete UnitDef.
UnitDef is sealed; an independently authored definition cannot satisfy their existing unit contracts.
This contradicts P4's actual content-alias authority and file/provider extension requirement.

## 1. Goals and authoritative state

IUnitContentDefinition combines actual immutable content identity with existing IUnitDefinition.
Runtime-only unit values used for pure conversion do not acquire fictitious content ownership.
UnitDeclaration owns the original strict reader, stable identity, exact positive reduced rational scale
and explicit discreteness. Native and independent definitions share it; UnitConversion remains the
sole runtime conversion math. No new registry, unit catalog, density, balance or clock.

## 2. Resolution

ResolvedContent resolves every unit content contract once before resources/capacity/other consumers.
Before any consumer or Initialize, validate actual alias ownership, identity, valid unit metadata and
canonical scale through the existing conversion rules. Even unused malformed units refuse.
Migrate engine non-Tycoon consumers and public proof consumers in place to that actual content alias.
Keep concrete native section registration; remove concrete selection and identity checks in scope.

## 3. Views and contracts

CopyUnits inventories actual authored contracts, uses the existing provider observation guard, sorts
copied facts by stable UnitId and returns no executable/mutable provider. Resource/capacity projections
retain the same exact quantity and measure owners. UnitDeclaration is not another live provider.

## 4. Files and authored proof

Core/Units.cs; Content/Resources.cs, ResolvedContent.cs, CapacityPolicies.cs, SpatialSpaceDef.cs;
Hosting/PlayerView.cs; original operation/signal/condition/environment unit consumers and public proofs.
Master-listed units-contracts.json supplies direct unit kinds with the existing IDs for one explicitly
authored proof session; native fixture entries for those IDs are absent, never overridden/merged.
Reuse capacity-contracts.json and original physical processes/stock/WIP through explicit composition.
Add ONE NEW thin player case and public composition diagnostics; no host gameplay or scheduler.

## 5. Errors

Missing/malformed scale/dimension/discreteness/identity, wrong kind or ambiguous ownership refuse before
Initialize. Repeated quantity dimensions and inexact conversions retain original typed refusals.
No fallback unit, rounded scale, compatibility alias or alternate JSON field.
Arbitrary CLR execution/CPU is not sandboxed.

## 6. Compatibility

Development-only breaking revision; native units remain a legitimate content kind, not a legacy path.
Optional Tycoon files are untouched; their concrete unit bindings remain an explicitly open migration
boundary, not claimed to support independent business units by this slice.

## 7. Verification

Build as needed; ONE NEW exact bounded case:
Unit content contracts: independent JSON units, exact measures and physical execution.
Prove owned aliases, canonical normalization, exact/wide/discrete conversion, engine inventory and WIP
binding, immutable metadata, physical hauling/manufacturing, paused/saved continuation, privacy/replay/
session isolation and early malformed/unused/ambiguous provider refusals.
Only repair/rerun this same new case; no historical/family/full/Oil/Tycoon/platform runtime.

## 8. Remaining gates

Independent resource definition catalog registration, optional Tycoon bindings, broader layer/
condition/automation/environment runtime, arbitrary provider faults, lockstep/content ordering,
mixed concurrency/stress/soak/performance and full P1–P12 remain open.

## 9. Non-goals

No domain/business expansion, money requirement, Draw, Unity/Godot/adapters, parallel authority or
publication. Stage/commit/push only after the full goal AND plan genuinely finish and are verified.

## 10. Order

Original source/tracker review → shared authoring/validation → original aliases/consumers/views →
master-listed direct proof/player case → focused build/runtime → evidence/master/audit reconciliation.

[Parent](P4_REUSABLE_MODULES_DESIGN.md), [resources](P7_RESOURCES_DESIGN.md),
[tracker](MASTER_TRACKER.md).
