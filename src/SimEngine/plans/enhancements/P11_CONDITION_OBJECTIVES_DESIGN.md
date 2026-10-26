# P11-B2 — Shared private game conditions and operational objectives

Status: scoped B2 verified,2026-10-07. Build2.226.0/save130/controller view4;
objective view2/system1/native layouts unchanged. ONE clean build9.82s0 warnings/errors;
ONE exact NEW bounded case82 checks0 failures/aborts483ms, first attempt. Earlier gates historical,
not rerun; full goal active. [Evidence](../../docs/migration/P11_CONDITION_OBJECTIVES_VERIFICATION.md).

## 0. Specification

Objective predicates currently cover technology/entity counts and public extensions, but cannot natively
reuse typed operational/private signal conditions. Those conditions are authored under the controller
module, unnecessarily coupling non-AI games to autopilot composition. Promote the SAME condition
contracts/provider into independently selectable generic game conditions. No second signal interpreter,
condition state/registry/clock or objective lifecycle. No compatibility names/readers.

## 1. Goals

1. IGameCondition/GameConditionDef/SignalGameConditionDef replace controller-specific names in place.
   Optional GameConditionsModule declares signal_conditions; selected conditions require no controller.
2. Native ConditionObjectivePredicateDef references the SAME content condition and uses company-private
   guarded observations. Existing objective graph/deadline/effect owner controls progress and completion.
3. Objectives/strategies share SignalGateCondition's exact typed Boolean/range evaluation and real
   inventory/incident signals; invalid references/provider kind/unit fail composition, unavailable waits.
4. Guard existing predicate/effect-check observations with scoped engine authority; malformed negative/
   nonseat objective snapshot viewers are not observers. Exact observer stays explicit.
5. Real player-raised incident, machine travel/material-consuming timed repair, stock20→12 and private
   signals complete an objective chain; public condition works without scheduler/objective edits.
   Saved partial/pending/twin state and once-only native terminal effect remain authoritative.

## 2. Design/API

Move existing conditions from PlayerStrategyConditions.cs into GameConditions.cs, leaving playbook
facts/contracts in PlaybookDiagnostics.cs. Delete old source file and old type/section/method names.
GameConditionsModule has content declarations only, no system/view/Sync/state service.
Controller/context/diagnostic consumers use IGameCondition and ObserveCondition, SAME guard/evaluator.

ConditionObjectivePredicateDef resolves GameConditionDef by alias, validates selected binding and
reads actual private company view through GameConditionContracts; binary progress0/1, no fabricated
partial signal progress. LogicalObjectivePredicateDef composes all/any as before, ObjectiveSystem
keeps its sole DAG/enrollment/lifecycle/effect/Sync. Add scoped predicate/effect-check guards there.

Public GameConditionProofModule owns existing elapsed condition/dial declaration, removed from
PlayerControllerProofModule. Split old branch condition sections into actual playbook-conditions.json
declared once by the selected condition module; migrate old branch fixtures only, not rerun.
New objective documents separate condition and objective module ownership. No duplicate loader.

## 3. Sequence/files

1. Design/tracker, promote existing contracts/content/guard and update consumers.
2. Native objective bridge/private observation guards, module/view metadata.
3. Public content module, authored operational objective proof and direct old fixture migrations.
4. Current identity assertions compile only; build then ONE exact NEW bounded scenario.
5. All invocations recorded, final green source/test freeze; tracker/docs/current source inventory.

## 4. Errors/compatibility/exclusions

Missing condition/section/provider/kind/unit/mode: ContentException before Update. No-target/unavailable/
wrong actual operational state are typed waits, not false/zero substitution. Scoped guards detect
tracked clock/entity-identity mutation; fault stops terminal success/effect Apply and prevents save/hash.
This does not prove arbitrary provider/global purity; effect admission can already be in Completing
when Check faults. No rollback/silent fallback claim. One existing observer identity only.
No rendering/adapters/business/OilFieldDays changes. No universal privacy/CPU sandbox, broader provider/
fault/corruption/replay/lockstep/soak/full acceptance or complete industrial rival strategy claim.

## 5. Verification

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Objectives: shared private conditions and material-consuming incident recovery"
```
ONE distinct NEW bounded case; actual failures only permit repair/rerun, every invocation recorded.
No older/family/full/platform/stale run. Native payloads unchanged; public view identities advance.
No source/test edit after final green. Full goal stays active.

[Parent](P11_PROGRESSION_KNOWLEDGE_CONTROLLERS_DESIGN.md), [master](MASTER_TRACKER.md).

