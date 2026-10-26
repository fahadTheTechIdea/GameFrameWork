# P8-A1 — One contract-driven transformation runtime

Status: implemented and focused-verified,2026-10-05. Build2.75.0/save46; clean build;
144 checks/two exact suites (50 contracts;94 execution), zero failures/aborts.
[Evidence](../../docs/migration/P8_TRANSFORMER_CONTRACTS_VERIFICATION.md).
Prerequisite migration for P8, not its WIP/continuous/utility completion.

## 0. Problem

Production.cs, ProductionModule.cs and ProductionManagementView.cs dispatch/read concrete Producer
objects. External processors cannot participate in their commands, views or coordinator. The old
recipes/hours/continuous naming hides actual authored durations and repeating batch semantics.

## 1. Goals

1. Replace, do not wrap, Producer/RecipeDef/ProductionSystem with Transformer/TransformationDef/
   TransformerManager and migrate all standalone consumers/content/registrations.
2. Manager, commands and private views dispatch only ITransformer, with explicit named capabilities.
3. Each provider owns its queue, progress, operation/state and sole codec; manager owns no copies.
4. Master JSON authors transformations and explicit rational durations. Automatic repeating batch
   is named auto_repeat, not falsely represented as a continuous flow process.
5. Prove an unrelated public-only provider participates without a native manager switch.

## 2. Design

ITransformationDefinition/open TransformationDef publishes native Bills/result/duration/cost facts.
ITransformerDefinition publishes actual supported TransformationDef catalog aliases and optional
auto-repeat/automatic-supply declaration. ITransformer extends IControllableOperation with detached
ReadTransformation, pure queue admission, enqueue/cancel and Advance(TransformationExecutionContext).
The existing per-provider Sync owns all state; no manager queue/ledger or second update loop.
TransformationExecutionContext.CheckGates is the sole optional ITransformationGate evaluator,
reused by native and independent providers; pure refusal blocks before irreversible native start.

TransformerManager validates durations, actual catalog aliases, supply/economy/rollout requirements
before Initialize; scans live entity parts in entity/component order and calls the capability once.
It retains the existing logistics demand-owner alias and uses the actual provider snapshot for source
liveness. Command previews share its pure admission; named identity is required, never first-match.
Remove the arbitrary 50-entry command cap; actual checked collection representation remains a bound.
Pure provider reads/checks may not touch their entity. Explicit provider errors fault normal Update.

Transformer retains current physical batch execution in its own Advance, using existing native
transactions, rollout, economy and optional supply contracts. IEntity.Update alone commits state.
This migration does not invent WIP: prior batch consumption/cancellation behavior is an explicit
unfinished P8-A2 responsibility, not a complete accounting claim. No new input/output balances.
Native progress/blocker/phase/queue changes touch the entity; Entity.Update alone commits entity
state. An explicitly selected result home removed mid-work blocks completed delivery, never silently
substitutes another entity. Snapshot/checkpoint validation uses actual supported catalog aliases,
phase/progress/control facts; manager validates after restore and before writes without another codec.

## 3. API/content

TransformationModule sim.transformations owns transformer component, transformations section,
enqueue/cancel-transformation commands and TransformationManagementView. Commands require an entity
and operation capability, transformation ID/count/repeat/home or queue index. Duration uses explicit
numerator/denominator/unit, no legacy hours loader. Existing input/output Bills and entity_output
remain native identities. Open definition/processor contracts enable JSON provider subtypes.

## 4. Files

Move Production.cs/ProductionModule.cs/ProductionManagementView.cs to transformation counterparts;
add contracts; migrate StandardCommands, Logistics/Tuning, standalone tests/public proof/master JSON.
Update save/build/changed declarations, P8 parent/master/README/AGENTS and scoped evidence.

## 5. Errors

Wrong capability/company/content, unsupported definition, bad count/index/home or placement refuse
through identical preview/command rules. Missing/invalid duration/content/provider references fail
before tick one. Malformed snapshots/touched pure reads/checks or provider execution errors fault
Update; faulted partial state cannot checkpoint. No inferred provider or compatibility adapter.

## 6. Compatibility

Development revision, no compatibility. Delete old classes/sections/registrations. Build2.75.0/save46;
native transformer component3 and transformation-view2; distinct new command identities/payloads.

## 7. Verification

Build solution. Run exactly:

- `Transformations: contract dispatch, authored content and private management`
- `Transformations: timed native and independent providers with saved continuation`

Cover strict duration, private pure previews, named ownership, queue/cancel/repeat, no 50 cap,
independent provider, real native inventory/entity timing, control pause, saved twins/corruption,
session isolation and provider faults. Other suites migrate/compile only; no full/family gate.

Executed gates include native auto-repeat, an independent public-only timed entity emitter,
multiple named providers/shared gate, exact native material changes, disabled/saved progress,
output capacity/incompatible resources, immutable retained queue facts, removed result home,
invalid phases/progress/support, strict duration/cost/content and mutating check/read/gate faults.
Existing native inventory-provider cases run inside the execution gate, not extra suite names.
Broader automatic demand identity, WIP/result atomicity and full regression are not proven.

## 8. Risks

Changing named command payloads and module/component IDs is deliberately breaking. Native consumed
inputs are not recoverable WIP yet. Multiple named providers on one entity require provider-scoped
demand identities before automatic supply may support that combination; reject ambiguity explicitly.

## 9. Out of scope

WIP/cancellation recovery, distinct ports/catalysts/atomic multi-output, continuous rational processes,
utilities, arbitrary result providers, Oil/Tycoon/hosts/platform/Draw and broad acceptance remain P8.

## 10. Order

1. Migrate active identities/content, remove old path.
2. One contract-driven manager and provider-owned batch implementation.
3. Named commands/private view/persistence admission.
4. Public provider proof and two exact gates.
5. Evidence and linked trackers; keep full goal active.
