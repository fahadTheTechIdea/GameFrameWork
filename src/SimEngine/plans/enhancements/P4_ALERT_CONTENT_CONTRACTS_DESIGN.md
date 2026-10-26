# P4-G11 — Open company alert profile content

Status: scoped verified70 checks; full P1–P12 remains open. [Evidence](../../docs/migration/P4_ALERT_CONTENT_CONTRACTS_VERIFICATION.md).

## Problem and goals

Before this slice, CompanyAlerts.cs declared ICompanyAlertProfileDefinition, but CompanyAlertPolicies, CompanyAlertModule and CompanyAlertControlActionPolicyDef still selected CompanyAlertProfileDef. A game could not supply alert policy JSON through the actual contract without native inheritance.

1. Make the existing profile contract an IContentDefinition and resolve every consumer through actual owned aliases.
2. Extract the sole original JSON reader/rule ordering/priority adjustment into CompanyAlertProfileDeclaration; native and direct extension definitions delegate to it.
3. Validate all selected profile identities, explicit scopes and actual dispatch bindings before Initialize; guard profile observations and priority decisions.
4. Preserve CompanyAlertPolicies as the sole company settings/chronology/reserve owner and the existing scheduler, automation command, graph, claims, resources, physical work and GameClock.
5. Prove independent master-listed profile JSON drives normal alert commands, deferred automation, emergency priority, real travel/material-consuming response, private immutable views, save/load and replay.

## Design and files

- Standard/CompanyAlerts.cs: contract identity, public stateless declaration, guarded contract admission/priority/composition. Company settings and service schema3 unchanged.
- Standard/CompanyAlertModule.cs: initial contract binding; existing command/view registration unchanged.
- Standard/CompanyAlertControlActions.cs: content dependencies resolve actual profile contracts; normal action still creates ConfigureCompanyAlertCommand.
- ExtensionProof: direct Def implementation delegates shared declaration; content-only module extends existing proof registration and adds no gameplay system.
- One new thin player case reuses existing explicit ControlContentContractTests authoring inputs, not its historical runtime suite.

## API and errors

ICompanyAlertProfileDefinition extends IContentDefinition. CompanyAlertProfileDeclaration.Read(JsonReader), DispatchScopes and PriorityAdjustment retain existing semantics. Missing level arrays, duplicate rules, malformed scopes, missing adjustments and unknown selected dispatch scopes refuse explicitly. Runtime wrong-kind profiles/foreign company commands retain typed refusals. No default, alternate priority evaluator, compatibility loader or legacy path.

## Verification and execution order

1. Shared declaration and actual contract consumers; review original semantics.
2. Master-listed independent profiles and thin public player proof.
3. Build Beep.SimEngine.slnx; run only ONE NEW bounded exact case and repair/rerun that same case if needed.
4. Record builds/attempts, freeze executable inputs, update tracker/audit anchors and preserve original acceptance text/checkboxes.

## Risks and limits

Composition must inspect dispatch providers after their declared preparation, not from an unrelated early module Compose. Profile checks remain pure; priorities do not authorize instant repair, resource creation or cargo movement. JSON executable behavior implements a registered contract, not arbitrary CLR names. Arbitrary hostile CLR recursion/CPU is not sandboxed by this engine.

## Out of scope

Oil, business/Tycoon, mandatory budget/money, Draw/platform adapters, a second clock/scheduler, historical/family/full runtime regression, and publication before full goal/plan completion.
