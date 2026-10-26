# P4-G2e — Complete timed frontier acquisition

Historical scoped proof, not rerun on2.286.0. [G2f](P4_UNIFIED_NATIVE_ADMISSION_DESIGN.md)
retains this WorkBoard ownership but removes early AdvanceOrder timed acquisition and its
transient started-ID guard. Timed and staffed starts now share original ranked final admission.

Status: timed native slice implemented and focused verified,2026-10-08. Full P4/P7/P9/P12/P6/P1–P12 remain open.
Build2.285.0/save141/replay1; final83 passed0 failed/aborted1342ms in ONE new bounded case.
Five clean builds and all five same-case runs recorded in
[evidence](../../docs/migration/P4_WORKERLESS_FRONTIER_ACQUISITION_VERIFICATION.md).

## 0. Problem Statement

Standard/Work.cs AdvanceOrder's Ready branch acquires each positive-time workerless node separately.
The G2d complete native acquisition now exists, but authored parallel timed work can still bind an
earlier node before discovering a later aggregate shortage. Batched starts must not grant later
nodes an extra productive Update simply because their state changed earlier in the graph loop.

## 1. Goals

1. Same WorkBoard admits distinct actual dependency-ready timed nodes in a fresh complete set.
2. Reuse CheckWorkSet and the original resource/optional-budget acquisition, not another coordinator.
3. Whole refusal leaves nodes, stock, books, budget holds and state identities unchanged.
4. Feasible parallel timed nodes start together at zero progress; all first earn work next Update.
5. Scarce tools/materials still permit canonical sequential physical execution; no artificial deadlock.
6. Existing graph/node state, save/hash/replay, cancellation and public views remain authoritative.
7. One new JSON-authored bounded case proves contracts and public resource-only/funded physical work.

## 2. Design

Extend the existing WorkBoard partial Standard/WorkAcquisition.cs with internal
StartWorkerless(Order,IReadOnlyList<string>,out Code). Copy and canonicalize requested IDs; validate
actual active order/company/target/response/plan binding, distinct Waiting/Ready positive-time
workerless nodes, no jobs, current dependencies and fresh EvaluateRequirements. CheckWorkSet selects
joint existing resource offers, then same AcquireResources commits native claims and optional
authored budgets. Bind original node choices/state only after complete resource verification.
Delete unused single-node AcquireResources wrapper when this replaces its only consumer.

AdvanceOrder collects currently eligible timed nodes at the existing Ready branch. Try the complete
eligible set first; if joint admission refuses, use the same owner for the canonical current node,
preserving physical sequential competition. A transient started-ID set only prevents extra work in
this same AdvanceOrder call; it is not saved progress/time or another scheduler. Zero-time effects,
staffed jobs, dependency completion and paused already-acquired work retain original paths.

## 3. API surface

No new public state authority. Internal admission uses actual Order and stable authored node IDs.
Build2.285.0/save141/replay1; original persistent codecs and sole GameClock unchanged.

## 4. Files

| Action | File | Risk |
| --- | --- | --- |
| Modify | Standard/WorkAcquisition.cs, Work.cs, WorkResources.cs | high |
| Modify | Kernel/SaveGame.cs, older build assertions | mechanical |
| New | tests/Beep.SimEngine.Tests/WorkerlessFrontierTests.cs | medium |
| New | tests/ExtensionProof/content/workerless-frontier.json, workerless-frontier-costs.json | low |
| Modify | OptionalWorkTests.Factory explicit master assembly, ExtensionProof.csproj, Program.cs | low |
| Modify | tracker/parent stages/evidence/README/AGENTS/help | low |

## 5. Error handling

Stale/duplicate/missing/dependent/staffed/zero-time nodes refuse before mutation. Resource or optional
money shortages retain existing typed blockers. Provider violations fault normal Update, not rollback.
Pure successful admission never substitutes a completed/stale execution or funds disabled costs.

## 6. Compatibility

None. Replace old path in place, no legacy API/loader/codec or duplicate acquisition owner.

## 7. Verification

Build after completed implementation. ONE distinct NEW bounded case:
`Timed frontier acquisition: complete native commitments and physical saved continuation`.
Only observed same-case failures repaired/re-run. Older migrations compile-only; no old/family/full/
platform/stale runtime. Freeze source/proof/tests/JSON after final green, then documentation-only.
Each physical phase has2000 Update failure bound and job/node diagnostics, not watchdog/soak/CPU proof.

## 8. Risks

Batching must not accelerate later nodes by one Update. Joint refusal must not turn sequentially
usable tools into a permanent wait. Providers remain governed by existing native guards; global
purity/cross-provider distributed obligations and all frontiers combined remain open.

## 9. Out of scope

Tycoon policy/source, Oil/siblings, hosts/demos, Draw/platforms, new ledger/clock/scheduler, price defaults.

## 10. Execution

- [x] G2e.1 Complete fresh timed-node admission through original native acquisition owner.
- [x] G2e.2 Integrate batched starts without extra same-Update progression or scarcity deadlock.
- [x] G2e.3 Master-list JSON proof content; build/run the single bounded contract/player case.
- [x] G2e.4 Freeze source and record every attempt, tracker, source inventory and remaining gates.
- [ ] Full distributed/provider/stress/authorized P6/P1–P12 acceptance and final commit/push.

Open decisions: none for this existing DAG acquisition requirement.
