# P11-B1 — Optional authored objective chains and deadlines

Current ownership revision: [P11-B3 objective content contracts](P11_OBJECTIVE_CONTENT_CONTRACTS_DESIGN.md) migrates the same ObjectiveSystem to independent contract definitions/predicates/effects. Same objective-state1 and physical owners. NEW exact case79/0/0 on2.333.0/save154; [evidence](../../docs/migration/P11_OBJECTIVE_CONTENT_CONTRACTS_VERIFICATION.md). Earlier B1 runtime is historical, not rerun on2.333.0. Full P11/P1–P12 remains open.

Status: focused native objective slice verified on 2026-10-05. Full P11/P1–P12 remains active/partial.
Build2.88.0/save58/objective-state1/view1; other native layouts unchanged.90 checks/two exact suites,
zero failures/aborts, clean build; [evidence](../../docs/migration/P11_OBJECTIVES_VERIFICATION.md).

Historical B1 evidence, not rerun on current HEAD. Current shared condition/operational extension
is [B2](P11_CONDITION_OBJECTIVES_DESIGN.md): same objective lifecycle/codec1 and private view2,
typed conditions independently selected without controller; no second objective store/evaluator.

## 0. Problem

P11_PROGRESSION_KNOWLEDGE_CONTROLLERS_DESIGN requires objectives/deadlines. The standalone runtime
has no objective owner. Hosts must not infer victory from inventories or implement a campaign loop.
World.End is the existing sole outcome owner; native work/construction/technology remain fact owners.

## 1. Goals

Explicit master-listed objective definitions, all/any predicate graphs and prerequisite chains;
explicit per-company scenario selection; deterministic timed activation/deadlines; once-only terminal
effects; detached private progress/blockers; one saved/hashed state owner. Prove real
construction drives an objective chain and final outcome entirely inside the normal Update lifecycle.
Technology observations use the existing knowledge contract; combined physical research proof
remains a separate gate.

## 2. Design

ObjectiveDef/IObjectiveDefinition references ObjectivePredicateDef/IObjectivePredicate and optional
ObjectiveEffectDef/IObjectiveEffect aliases in the existing catalog. DependencyGraph validates both
predicate and objective topology. Conditions are reusable registered providers, never CLR strings.
Native predicates observe learned technology or real owned entity prototypes; logical all/any consumes
typed child observations. Technology service is required only for selected technology predicates.

ObjectiveSystem/IObjectives owns scenario-bound objective lifecycle records only. No copied stock,
worker/work/research state, host campaign scheduler or compulsory money/spatial/technology dependency.
System order800 observes after native work/process phases. Each Update captures all ready observations
before terminal effects; dependency activation uses the pass-entry states, so effects/completion cannot
feed another objective in the same pass. States: Locked → Active → Completing → Completed, or Failing
→ Failed. Completion/failure effect admission may wait with its typed blocker; successful Apply commits
once then settles. Provider violation faults the normal engine session, not a compensation path.

Deadlines are explicit rational elapsed durations or null, measured from activation with existing
session time; Tick >= deadline expires before predicate success. Once Completing/Failing, the earned
terminal intent waits for its effect without reevaluating predicates or extending/resetting activation.
Failed prerequisite propagates typed
dependency failure. Invalid observation fails rather than interpreting unavailable as satisfied.
Native outcome effect uses World.End; no new global victory ledger. Private snapshots retain only
observed progress, lifecycle/timestamps/blockers and declared prerequisite IDs.

## 3. API

IObjectives.CopyObjectives / Observe; ObjectiveView exposes detached facts/pure current observations.
Ordinary player commands do research/building; no instant objective-complete command. Scenario JSON
settings.sim.objectives.companies explicitly lists company/objective IDs, including empty arrays.

## 4. Files

Standard/ObjectiveContracts.cs, Objectives.cs, ObjectiveModule.cs; Views/ObjectiveView.cs;
public external proof provider/content and two exact suites; identity/master/stage/evidence/help.

## 5. Errors

Missing/cyclic/duplicate refs, empty logical graphs, missing bindings/prerequisites, invalid
deadlines, unsupported selected providers refuse before Initialize. Unknown/private queries expose no
foreign lifecycle facts. Malformed state/timestamps/prerequisite chronology refuse save/load/hash.

## 6. Compatibility

Direct current identity/schema only; no old objective code, parallel owner, default victory or shim.
Oil/P5 adoption remains separate under the user's standalone-only scope.

## 7. Verification

Build and exactly two suites: Objectives: authored graphs, bindings and private contracts;
Objectives: physical chains, deadlines and saved continuation. No third/family/full run.

## 8. Risks

Pure observation is a provider contract; global mutation/fault coverage remains a later matrix.
Objective effects must not introduce free materials or replace the real work scheduler. Whole-pass
snapshotting is required to prevent same-tick feedback. No implicit objective enrollment.

## 9. Out of scope

Pressure/controller/fog, population/combat/economy defaults, oil/platform/Draw, objective repeat/edits,
arbitrary resource/network/incident predicate libraries and full acceptance.

## 10. Stages

- [x] B1.1 Open contracts, strict JSON and sole dependency validation.
- [x] B1.2 Native lifecycle/deadlines/effects/private view and sole Sync.
- [x] B1.3 Public provider/physical proof, exact gates, tracker/evidence.
- [ ] Combined technology-research objectives and broader resource/network/incident predicates.
- [ ] Provider purity/fault/corrupt-state/replay/lockstep/stress/full P11 acceptance.
- [ ] General knowledge/controllers/pressure/P5 and full P1–P12 roadmap.
