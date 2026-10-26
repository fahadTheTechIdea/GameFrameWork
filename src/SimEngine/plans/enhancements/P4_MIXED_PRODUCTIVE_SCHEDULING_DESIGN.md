# P4-G2h — Mixed cross-order productive scheduling

Status: scoped native scheduling implemented and verified,2026-10-08.
Build2.291.0/save141/replay1/WorkView82. Four clean builds and four attempts of ONE new
bounded case, final147 passed0 failed0 aborted1882ms. Executable inputs frozen after green.
[Verification and all attempts](../../docs/migration/P4_MIXED_PRODUCTIVE_SCHEDULING_VERIFICATION.md).
Continues G2f startup and G2g complete productive commitments; broader gates remain open.
Full P1-P12 remains open; no publication before full goal and plan completion.

## 0. Problem

WorkSystem.Update runs assigned jobs before AdvanceOrder. ExecuteProductive commits one machine
quantum immediately; AdvanceTimedFrontier then commits timers one order at a time. Shared running
inputs are consumed by loop/entity/order enumeration instead of the existing player-policy ranking.

## 1. Goals

1. Native machine and acquired timed work across orders share one company productive commitment pass.
2. Keep physical approach, provider phase invocation, gates, departure and job ownership unchanged.
3. Complete feasible running sets commit through existing WorkBoard.AdvanceWork; shortages sequence
   by existing RankOffer/CompareAssignments, never by implicit machine-first enumeration.
4. Node progress advances once per update; newly admitted work earns no same-update quantum.
5. Completion effects/dependencies/cancellation/private views/save/replay remain native authorities.
6. Prove shared mixed stock, opposite player priorities, pause/restart, saved twins, provider failure
   and physically timed construction with ONE new bounded runtime case.

## 2. Design

WorkSystem.Update holds a transient list of actual productive identities during its current call,
cleared in finally. ExecuteProductive keeps physical/readiness checks but submits only an identity;
it no longer mutates productive progress inline. External phase providers still invoke the original
single-use WorkPhaseContext. No context survives or bypasses provider invocation.

After job phases, gather acquired dependency-ready timers for every active eligible order into
that same list. Project actual node identities into existing WorkerDispatchOffer ranking facts
and reuse RankOffer/CompareAssignments. Productive travel cost is zero because physical approach
already completed; timers have no fabricated worker. Group by actual company. Try its complete
WorkBoard.AdvanceWork set once; on ordinary shortage use ranked actual single identities through
the same owner. Bind node/job blockers/states, productive itinerary transitions and native timed
completion only after real commitment. No second resource/budget/progress scheduler or ledger.

Return transient attempted identity set to AdvanceOrder, replacing per-order AdvanceTimedFrontier.
It avoids another productive quantum/effect attempt during the later graph walk, while existing
ready/dependency/effect/startup/plan logic remains. Starts still happen in final Assign.
No saved fields or clock changes; existing bindings/progress/cadence remain authoritative.

## 3. API surface

No public gameplay API change. Internal AdvanceOrder receives current attempted identities.
Delete AdvanceTimedFrontier and immediate singleton productive commit path.
Build2.291.0/save141/replay1; WorkView82 and existing persistent layouts unchanged.
New proof workflows are explicit JSON merged into the fixture's master-listed work document.

## 4. Files

| Action | File | Risk |
|---|---|---|
| Modify | src/Beep.SimEngine/Standard/Work.cs | high |
| Modify | Kernel/SaveGame.cs and old identity assertions | mechanical |
| New | tests/Beep.SimEngine.Tests/MixedProductiveSchedulingTests.cs | medium |
| New | tests/ExtensionProof/content/mixed-productive-scheduling.json | medium |
| Modify | existing Program/ExtensionProof embedding | low |
| Modify | stage/evidence/tracker/parents/README/AGENTS/help | low |

## 5. Errors

Invalid/lost requirements retain original reopen/failure behavior. Nonfeasible complete sets
have no productive prefix; ranked fallback may make actual independently feasible progress.
Provider acquisition/consumption violations fault Update and forbid valid Save/Hash; no rollback
or inferred success. Duplicate same-update proposal is an invariant failure, not double progress.

## 6. Compatibility

Development-only replacement of old scheduling paths, no compatibility branch or duplicate model.

## 7. Verification

ONE exact NEW bounded case:
`Mixed productive scheduling: ranked machine-timer quanta, shared inputs and saved physical work`.
Build only after complete edits; run only this case. Record every attempt, repair observed failures
in the same case only. No old/family/full/platform/stale runtime. Each physical wait <=2000 updates
with job/time/fault diagnostics. Freeze executable inputs after final green, then docs/inventory.

## 8. Risks

Trusted phase callbacks can still hang or mutate other providers; global purity/distributed atomicity
and concurrent admission remain separate open gates. Successful groups canonicalize commitments,
but scarce fallback follows existing ranking, not a new fairness policy. Existing work policy rank
semantics are retained rather than copied. This does not batch separate external dispatch owners.

## 9. Out of scope

Distributed external obligations, new Tycoon/gameplay policy, rendering/platforms/adapters,
new state or time owners, broad stress/regression, legacy support and premature Git publication.

## 10. Execution

- [x] Inspect existing productive/job/phase/rank/graph owners.
- [x] Replace split productive scheduling with the sole current-call pass.
- [x] Author explicit mixed workflows and one bounded player/protocol case.
- [x] Build/run scoped gate and record all attempts.
- [x] Freeze executable inputs and update linked tracker/evidence/current guides.

Open decisions: none for existing native productive scheduling.

