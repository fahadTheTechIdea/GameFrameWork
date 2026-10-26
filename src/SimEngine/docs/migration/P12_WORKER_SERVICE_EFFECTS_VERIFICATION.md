# P12-D5 physical worker-scoped condition service — focused verification

Date: 2026-10-06. Runtime: Beep.SimEngine only.
Build **beep-simengine-2.154.0**, save **81**, WorkBoard **25**, WorkSystem **16**, WorkView **45**.
Native condition codec2/public condition proof codec1 and the game clock are unchanged.
[Stage](../../plans/enhancements/P12_WORKER_SERVICE_EFFECTS_DESIGN.md).

## Authoritative ownership

ActivityNodeExecution.CompletedBy extends the existing node record and embedded Sync; no additional
receipt store or service scheduler exists. CompleteNode captures its actual final job actor before
release only after positive authored machine work, full progress, complete physical itinerary and
actual owned IWorker assignment. Full progress while still leaving the bay cannot repair early.
Timer and zero-time nodes have no completing machine. Aggregate completion cannot author a worker effect.

Checkpoint validation rejects negative/future/nonworker/unearned identities and missing completion
identities on settled positive-time machine nodes. A removed historical actor may remain a fact;
live effects still require the actual owned provider. This is not a contributor ledger or a
player-selected fleet-service subject. The receipt is retained only by its authoritative node/order;
ordinary terminal-order pruning removes it. No shadow history or retention workaround was added.
Embedded node decode and existing whole-board validation remain the save authority; arbitrary decode
atomicity, historical forged-alternate-actor detection and global provider tamper matrices are unproven.

PhysicalWorkEffectDef.CheckSubject validates the original target/order/response and resolves the effect
subject through existing scoped pure guards. Apply captures that accepted subject once and commits
to the same entity through existing phase checks. ConditionEffectDef remains the sole range/unit/
change implementation; WorkerConditionEffectDef only selects the recorded finisher and restricts
authoring. WorkerConditionRequirementDef reuses the same condition-range evaluator with context.Worker
and the existing IWorkerWorkRequirement candidate mechanism. Target-scoped providers remain legitimate
capabilities, not compatibility paths. Explicit master-declared worker_condition_effects and
worker_condition_requirements JSON sections select scope; no inferred subject or instant repair command.

Immutable company-private ActivityNodeView exposes the same receipt. Public-only WorkerServiceProofModule
reuses the existing ProofConditionDef, demonstrating an independent condition owner without another
balance, workflow, scheduler or host rule. JSON authors real spares, held tools, damaged-candidate
ranges, bay work and repair amount. Existing jobs/motion/docks/resources/condition owners do the work.

Exactly one GameClock remains. World.Tick is its read-only update-index projection, not another timer.
Paused Update(0) changes neither gameplay nor clock. GameClock source/construction is unchanged.

## Final commands and results

```
dotnet build Beep.SimEngine.slnx
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Worker service: authored subjects, candidate requirements and completion ownership"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Worker service: physical bays, materials, repair and saved continuation"
```

- Clean build: **0 warnings, 0 errors**, MSBuild **4.86 s**.
- Authored subjects/candidate requirements/completion ownership: **28 passed, 0 failed**, **314 ms**.
- Physical bays/materials/repair/saved continuation: **77 passed, 0 failed**, **447 ms**.
- **105 passed, 0 failed, 0 aborted suites**. Only these same two exact names ran in this stage.
- Mechanical identity migration touched70 test source files:64 build assertions,61 save assertions,
  26 board-schema assertions and2 work-system-schema assertions. Other suites compile only, not rerun.
- No source change after the final build/gates; subsequent changes are documentation only.
- Scoped document whitespace and worker-service links pass; Git reports line-ending notices only.

Contract evidence: native/public damaged-candidate selection, healthy rejection, pure/private/frozen
views, foreign-company refusal, no admission/travel receipt, early receipt checkpoint rejection,
timer/zero-time/aggregate worker-effect authoring rejection and durable valid continuation.

Physical evidence: native and public-contract machines travel to an actual bay, earn12 game minutes
of service, consume3 real spares and hold/release2 real tools. Worker condition remains4 during work
and departure, then becomes10 exactly once; facility condition remains4. Policy pause preserves
progress, consumed stock and condition. Sole-clock pause, cancellation after earned work, saved partial
work/departure/completion twins, interleaved session isolation and invalid receipt guards pass.
Public ignored-commit/future-change faults stop the normal update and reject saving the faulted session.

An explicitly authored roleless settling node keeps the actual service node available while testing
its completion receipt; finishing that node restores normal ended-order pruning. It is test content,
not an engine delay/cache/history rule. Physical waits are bounded at200 game updates with clock,
order/node/blocker diagnostics. Tests drive player input and existing Update, with explicit fault
instrumentation; no service gameplay lives in a test host.

## Same-gate repairs

Initial test compilation guessed view/candidate API names. Tests now use the existing TryCopyNodes
and candidate.Result.Blocker; no aliases were added. An initial post-completion view assertion ran
after normal order pruning. The fixture now explicitly authors the settling node and proves pruning
after it completes; no extra retention path was introduced. Final source review replaced double
subject resolution with a single accepted subject and existing pure guards. The clean build and
both same-name suites above are the final evidence after these repairs.

## Source fingerprints (SHA-256, framework-relative)

```
E8344F23CA0C3689277D50CB03719AD33176C620B3D43A394ADF451E83259F0D src/Beep.SimEngine/Standard/Work.cs
025AAFC28E33C7408BA9D94F620D8F72B74DF95BDC35C4D18C96A649D83FDA51 src/Beep.SimEngine/Standard/WorkEffects.cs
AA6EE7FF0F7A8592FF0FE92D42B9BFC9011ECF87AA5EA42DB38369AD9FE845DC src/Beep.SimEngine/Standard/EquipmentCondition.cs
586B10D0F64E2C7295DC70B745E4DF26235FEC9D112A59CBBD15824929480108 tests/Beep.SimEngine.Tests/WorkerServiceTests.cs
C3DF024A015C0744E23A0605F083C33C8F4D1E020C184B148FAC8F57AE066121 tests/ExtensionProof/content/worker-service.json
339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31 src/Beep.SimEngine/Hosting/GameClock.cs
```

## Open acceptance

This is a physical service prerequisite, not full fleet orchestration. Explicit selected service
subjects, facility selection, cadence dispatch, repair teams, preparedness, inspection science,
fuel/traffic/congestion/preferred routes, broader providers/global purity/replay/stress/full framework
remain open. P10-B9, earlier recurring/condition/dock/area suites were not rerun. No full gate or
OilFieldDays/Tycoon/host/demo/platform/Draw work was performed. The full implementation goal stays active.
