# P4-G2g — Complete productive work commitments

Historical G2g runtime, not rerun on2.291.0. [G2h](P4_MIXED_PRODUCTIVE_SCHEDULING_DESIGN.md)
now integrates native machines and acquired timers across orders through this same WorkBoard
commitment owner and existing dispatch ranking. Native mixed scheduling gaps are closed by that
focused slice; distributed/provider/fairness/accounting/performance/full gates remain open.

Status: native productive slice implemented/focused verified,2026-10-08. Full P4/P7/P9/P12/P6/P1–P12 open.
Build2.287.0/save141/replay1/WorkView82. ONE clean build13.77s; ONE new case first106/0/0 2145ms.
Source frozen; scoped [evidence](../../docs/migration/P4_PRODUCTIVE_FRONTIER_COMMIT_VERIFICATION.md).

## 0. Problem Statement

Standard/WorkStart.cs WorkBoard.AdvanceWork admits/consumes one node's startup receipts, running
delta and optional cost, then updates progress. Work.cs AdvanceOrder advances acquired timed nodes
one at a time. Feasible parallel paused nodes cannot obtain one complete next-quantum commitment.

## 1. Goals

1. One original WorkBoard admits distinct actual same-company productive node references freshly.
2. Aggregate running inputs, retained consume receipts and optional costs preflight before mutation.
3. Existing resource transaction acquires all running claims once; original per-node causes/history
   remain, with progress bound only after every exact commit verifies.
4. Timed Working/Paused frontiers share this owner; ordinary shortage permits canonical sequential
   progress, not deadlock. New starts earn no work until the next Update.
5. Machine productive work uses the same complete-set owner with one actual node; physical approach,
   departure and all worker gates remain in original WorkSystem.
6. Existing saved bindings/progress/productive steps own state; no new ledger/clock/progress codec.

## 2. Design

Replace internal five-argument AdvanceWork with
AdvanceWork(IReadOnlyList<(int Order,string Node)> nodes,PlayerId company,out Code blocker).
Actual order/definition/execution/worker/rate/cause are re-resolved, never accepted from caller copies.
Validate the entire identity/ownership/started shape before callbacks; reject duplicates, missing
orders/nodes, nonpositive duration, unbound resources/costs, unstarted/open machine jobs and foreign owners.
Machine refs require their actual assigned/paused job actor and current Productive itinerary phase.
Existing ExecutorRate and requirement evaluator supply fresh typed readiness.

Prepare transient per-node next progress/productive steps, consume receipts, running ResourceClaimIntents,
optional budget receipts/credit and original actual cause. Canonicalize refs by OrderId/NodeId.
Evaluate complete running claims, complete retained receipts and complete optional spend before mutation.
Acquire running claims once through existing IResourceTransaction; bind returned exact owner/endpoints
to transient rows, recheck complete receipts/budgets after acquisition, then spend/consume using existing
per-node causes. Missing/extra/duplicate provider receipts fault normal Update; no compensation.
Only after all commits verify bind original spent flags/progress/ProductiveSteps.
Transient prepared rows are synchronous proposals, not saved authority or another game model.

AdvanceOrder prepares acquired, dependency-ready timed nodes before its regular graph pass and calls
the complete set. On ordinary group shortage it uses the same owner for individual canonical nodes.
CompleteNode/effects/dependencies remain original; transient attempted IDs prevent a second graph
advance or effect attempt in that same Update. Not persisted, not another clock.
ExecuteProductive delegates one actual ref to the same owner; delete the old single-node mutation body.

## 3. API surface

Internal original WorkBoard productive API changes in place; no old overload.
Build2.287.0/save141/replay1/WorkView82. Existing persistent payload and sole GameClock unchanged.

## 4. Files to change

| Action | File | Risk |
| --- | --- | --- |
| Modify | Standard/WorkStart.cs, Work.cs | high |
| Modify | Standard/WorkModule.cs, Kernel/SaveGame.cs and current identity fixtures | mechanical |
| New | tests/Beep.SimEngine.Tests/ProductiveFrontierTests.cs, explicit proof JSON | medium |
| Modify | existing proof embedding/registration; tracker/parents/evidence/README/AGENTS/help | low |

## 5. Errors

Malformed/stale/foreign/duplicate/unstarted sets refuse without mutation. Aggregate running shortage or
unavailable optional budget returns the original typed blocker. Negative provider rate/purity/changed
claims/commit violations fault ordinary Update and forbid Save/Hash, with no rollback or prefix success.

## 6. Compatibility

None. Delete old productive method, migrate actual callers and identities; no inferred rate/cause.

## 7. Verification

Build completed slice; ONE exact NEW bounded case:
`Productive frontier: complete native quanta, paused continuation and saved physical work`.
No older/family/full/platform/stale runtime. Repair/re-run only observed failures in that same case.
Older identity migrations compile-only.2000 Update failure bounds per physical phase with diagnostics.
Freeze source/proof/tests/JSON after final green, then document every attempt and scoped inventory.

## 8. Risks

Completion must not double-apply effects or grant dependent timers an extra Update. Preserve per-node
historical causes and elapsed-vs-work normalized inputs. Scarce running inputs must allow existing
sequential physical execution. Full cross-order/mixed machine-timer productive scheduling remains open.

## 9. Out of scope

External distributed commitments, cross-company/cross-order fairness, arbitrary provider/stress/global
purity/full regression, Tycoon policy/source, Oil/siblings, hosts/demos, Draw/platforms or new time/state owners.

## 10. Execution

- [x] G2g.1 Fresh actual complete productive set admission/commit through original owners.
- [x] G2g.2 Integrate acquired timed frontier and original single machine productive path.
- [x] G2g.3 Explicit authored JSON and one exact new contract/public-player bounded gate.
- [x] G2g.4 Freeze source; record attempts, scope, tracker and literal SHA handoff.
- [ ] Full distributed/cross-order/mixed/fault/stress/authorized P6/P1–P12 and final Git publication.

Open decisions: none for existing native productive commitment ownership.
