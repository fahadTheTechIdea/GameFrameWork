# P11-B3 — Objective content contract verification

Date:2026-10-09. [Stage](../../plans/enhancements/P11_OBJECTIVE_CONTENT_CONTRACTS_DESIGN.md).

## Outcome and ownership

ObjectiveSystem now uses IObjectiveDefinition and IObjectivePredicate for entries, catalog lookup, prerequisite and predicate topology, selected bindings, evaluation and private snapshots. Native ObjectiveDef and LogicalObjectivePredicateDef resolve contract prerequisites/predicates/effects. The existing external gated objective effect also resolves IObjectivePredicate. Concrete native classes remain only content registrations/provider implementations, not runtime catalog gates.

The sole ObjectiveSystem composition validator checks actual owned/unambiguous definition/predicate/effect aliases and positive deadlines. Existing DependencyGraph validates missing/duplicate/cyclic dependencies. Selected beneficiary/prerequisite enrollment and provider bindings remain checked before Initialize. Evaluation and effect Check retain existing purity guards; same system commits lifecycle and Apply exactly once. No new objective state, scheduler, graph compiler, loader, timer, command family or outcome owner.

IndependentObjectiveDef, ForwardObjectivePredicateDef and IndependentObjectiveEffectDef implement the three contracts directly from Def without native objective inheritance. Their explicit objective-contracts.json is owned once in the master alongside reused objectives.json. The forwarding predicate uses existing captured child observations, not a parallel evaluator. The effect waits on an actual authored leaf predicate and notifies through the engine callback. Proof module only declares content and captures detached admission facts/Initialize instrumentation. Hosts/player drivers contain no gameplay implementation.

Core2.333.0/save154/replay1; ObjectiveSystem schema1, physical work/native schemas and sole GameClock unchanged. No old-save compatibility path.

## Build and bounded runtime

TWO clean builds:7.32s/4.45s, zero warnings/errors.

Only ONE NEW exact case executed:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Objective content contracts: independent graphs and effects use physical construction, private lifecycle and saved replay" --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

First:0 passed/1 failed,152ms; PID41176 exit1/observed315ms. The new fixture passed a document through the shared factory's point-owned additionalDocuments slot while declaring it objective-owned. Corrected that authored file input to the existing extraDocuments slot; no engine default or fallback.

FINAL:79 passed/0 failed/0 aborted,1043ms; PID15532 exit0/observed1191ms. Untruncated stdout/stderr, no observer error, timeout or forced termination. No other runtime cases or broad/family/full run.

## Proven scope

- Actual independent aliases link native prerequisite and logical predicate references, with immutable private views and pure observations.
- Native root completion activates independent dependent work only on a later Update pass. Authored content never grants completed objectives.
- Player enables ordinary machine policy and submits physical construction plans. Actual movement from position4 and two constructions consume16 of20 hauled inputs.
- Independent completion waits for a second physical structure through its effect gate; explicit blocker and activation timestamp survive saved continuation.
- No early notification or ghost structure. Effect applies once; loaded settled state does not reapply it. Native final objective activates later and its original effect owns outcome/loop termination.
- Foreign company does not earn another company's structures/objectives. Retained snapshots stay detached.
- Saved twins remain hash-identical. Physical/mixed graph/pending effect history replays. New sessions start without shared objective state.
- Independent deadline failure and native dependent failure use the one game clock and survive restore without touching inventory.
- Eighteen missing/wrong/duplicate/cyclic prerequisite, predicate, effect, child, gate, array, deadline and enrollment branches refuse before Initialize.

Executable freeze:2026-10-09 02:00:42 UTC; subsequent edits documentation/audit only.

## Limits

Historical objective/condition/environment/technology/startup/point/tile cases compile only, not runtime rerun on2.333.0. Alias-validation branches for detached forged providers, malicious code, broader purity/corruption/lockstep/stress matrices and full P1–P12 remain open. The scoped result does not prove all mods safe or the plan finished. No original acceptance checkbox/requirement changed.

No business/Tycoon expansion, mandatory money, Oil consumer, rendering/Draw/platform/adapters or second clock. No staging/commit/push until the FULL goal AND plan genuinely finish and are verified.
