# P4-G39 — Direct work-effect implementations and worker subject safety

## 0. Problem statement

G38 added physical callback contracts, but seven native implementations and two public proofs still
inherit PhysicalWorkEffectDef; nonphysical examples still inherit WorkEffectDef. Rules are already
shared, so these concrete dependency bases are unnecessary. Condition completion authoring has no
shared declaration for direct independent worker effects. Subject resolution guards the order target,
but the existing CompletedBy worker can be distinct and needs its own actual codec observation.

## 1. Goals

1. Remove both obsolete effect bases; all active implementations directly use registered contracts.
2. Preserve the single physical helper, WorkBoard, resource/knowledge/condition/incident owners.
3. Share strict condition-effect JSON declaration and target/worker selection over existing graph facts.
4. Guard the actual completed worker while resolving a physical subject, without a new subject store.
5. Prove native/independent earned worker damage/repair, physical resource use and saved continuation.
6. Prove actual resolved-worker mutation faults in resolver/check/refused commit through normal Update.

## 2. Design

Migrate native/proof definitions to Def and IWorkEffect/IPhysicalWorkEffect. Callback plumbing delegates
to PhysicalWorkEffectContracts; no copied admission/completion algorithm or replacement base class.
ConditionEffectDeclaration alone reads/resolves source/kind/unit/amount. IConditionEffect exposes
typed immutable intent and whether the completed worker is the subject. ConditionEffectContracts
validates physical ancestry/worker stages and delegates actual changes to existing ConditionContracts.
WorkerConditionEffectDef specializes only subject metadata, not progress or execution.

Original ActivityNodeExecution.CompletedBy remains sole worker identity. During resolver observation,
guard that actual worker's scalar/Entity.Sync in addition to the original order target/provider/map.
Check and refused commit already guard the resolved actual subject. No world hash, rollback, timer,
state machine, stock/condition duplication or additional clock.

## 3. API surface

Remove WorkEffectDef and PhysicalWorkEffectDef. Add IConditionEffect, ConditionEffectDeclaration and
ConditionEffectContracts. Existing IWorkEffect/IPhysicalWorkEffect and public views/commands unchanged.
Direct providers require contracts/content only; scheduler does not branch on concrete types.

## 4. Files

Standard/WorkEffects.cs, EquipmentCondition.cs, CompanyKnowledge.cs, SpatialResources.cs;
public proof implementations/diagnostics that referenced removed bases; new independent worker
effect proof/JSON and one thin player case; registration/build/protocol; stage/evidence/master/audit.

## 5. Errors

Missing/wrong unit or invalid source/change/quantum fails composition. Worker effect requires its own
positive machine stage; unrelated parallel work cannot authorize it. Uncompleted/removed worker
refuses through the same existing effect boundary. Resolver/check/refused-commit mutation faults
before healthy time/save/hash; no fabricated repair or compensating rollback.

## 6. Compatibility

Development only. Delete concrete bases and migrate active framework/public proofs. No compatibility
reader/shim. External domain migrations remain their explicit P5 work; do not edit excluded Oil/Tycoon.
Save154/replay1 unchanged if persistent layouts are unchanged; build identity advances explicitly.

## 7. Verification

ONE NEW exact bounded case: Worker effect contracts: earned subject changes and scoped faults.
Native/direct independent damage/repair of actual CompletedBy, target unchanged, timed consumed/held
inputs, private previews, partial saves, sole-clock pause, twins/replay and interleaved isolation.
Fault the actual completed worker's private codec during resolve/check/refused commit. Strict invalid
authoring and timer-only/missing-worker node refusal before Initialize. Compile old callers only;
no historical/family/full/Oil/Tycoon/platform runtime. Freeze selected identities before execution.

## 8. Risks

Only order target and existing completed worker are observed during resolution. Arbitrary other
subjects/services, global rollback/accounting, every migrated effect category and all provider fault
matrices are not inferred from the new case. No sandbox for arbitrary CLR behavior.

## 9. Non-goals

Tycoon/G19 pending authority, Oil/hosts/demos/platforms/Draw, a new subject/progress/condition store,
full tests or publication before the full original goal and plan complete.

## 10. Sequence

Inspect active implementations; delete concrete bases/migrate providers; share condition declaration
and subject rules; extend scoped resolver observation; author independent provider/new player case;
compile/freeze/execute that case; update accurate scoped evidence and original tracker gates.

## 11. Status

Implemented and scoped-verified on2.377.0/save154/replay1. FIRST/FINAL155/0/0 in827ms.
231 selected executable/content inputs frozen2026-10-09 14:25:05 UTC before execution.
[Evidence](../../docs/migration/P4_EFFECT_IMPLEMENTATIONS_VERIFICATION.md).
Full P1-P12 remains OPEN; original compound gates remain unchanged. No publication.
