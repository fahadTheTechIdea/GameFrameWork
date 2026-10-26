# P4-G59 — Entity state definition contracts

Status: scoped verified on2.410.0/save154/replay1, FIRST25/FINAL29/0/0, FINAL1452ms. Full P2/P3/P4 and original221/155checked/66open remain unchanged/OPEN. [Evidence](../../docs/migration/P4_ENTITY_STATE_CONTENT_VERIFICATION.md).

## 0. Problem

IEntityStateSource, EntityStateBindings, default/initial state selection and reusable consumers require native EntityStateDef. A module cannot author its own state definition through the public content contracts, despite entity-owned Update.

## 1. Goals

IEntityStateDefinition carries authored identity/code/priority/default. Native and independent JSON definitions share one declaration; every consumer selects the actual catalog contract. Validate all loaded aliases before entity resolution/Initialize. Preserve original entity state/code/since/update order and exact priority/ID tie-breaking.

## 2. Design

EntityStateDeclaration owns the strict reader and actual owned metadata validation. EntityStateDef is normal native registration implementing the contract, not a mandatory authoring base. ResolvedContent uses the sole generic catalog for default/unique state code admission. EntityStateBindings and state source/construction/worker/operation/incident/motion/exploration/requirements consumers migrate in place. No state cache, copied state machine or compatibility return type.

Runtime identity/code/priority getters execute inside the original state-source/provider/entity/clock observation. Only observed local facts participate in original priority-descending/identity-ascending selection. The fallback initial-state getter uses the same original observation helper. A metadata getter changing the actual clock latches the first fault, publishes no healthy state transition prefix and cannot save. These local selection facts are not another authoritative state or catalog.

## 3. API

Breaking IEntityStateSource.GetState returns IEntityStateDefinition; construction/state metadata bindings follow. Engine identity2.410.0; save154 unchanged because original entity fields and provider payloads do not change.

## 4. Files

Shared state declaration in existing EntityStateDef.cs; mechanical type migration through framework/proof/quickstart callers; one independent non-friend definition/module; two explicitly listed JSON files; new exact native physical state proof. No Oil/renderer/platform/Tycoon changes.

## 5. Errors

Empty/throwing metadata, duplicate codes/defaults, missing default/references and unknown keys refuse before Compose/Initialize, unused included. Original provider/entity/clock observation and foreign-state identity rejection remain. No fabricated default or state substitution.

## 6. Compatibility

Breaking contract migration, no shim/native accessor path.

## 7. Verification

Compile completed slice and ONE NEW exact bounded native physical state case, not old/family/full tests. Moving/loading/working/stopped and initial default use only independently authored aliases; save/replay/conservation/retained facts prove original runtime ownership.

## 8. Risks

Custom private CLR metadata mutation/purity is not a sandbox guarantee. Interface migration affects existing external consumers; Oil migration remains outside this engine-only slice. Compile historical callers only.

## 9. Non-goals

New states scheduler, rendering/Draw/platforms, protected G19 files, all provider purity and broad lifecycle/full regression.

## 10. Order

Read actual consumers → shared declaration/contract → migrate all in-scope types → independent authored proof → compile/exact case/necessary repairs → evidence/master/audit. Full goal/plan must finish before commit/push.
