# P5-CF1 — Active client fact provenance

Status: verified for original P5.acceptance.006; [current source mapping, exact checks
and limits](../../docs/migration/P5_CLIENT_FACT_PROVENANCE_VERIFICATION.md). Full goal remains open.

## 0. Problem Statement

Original P5.acceptance.006 remains open after historical P6-TC1 verified thin host
ownership on2.623. Current2.628 must substantiate the exact operation/placement/work/
route facts actually consumed by every active Oil client, not assume an allowed
namespace proves correct behavior or create another client checker.

## 1. Goals

1. Read the complete active five-file console client inventory and project references.
2. Map each gameplay fact/decision to its engine view/preview/result producer.
3. Build current client artifacts and reuse the original compiled client-boundary
   exact case and the public thin-client parity exact case.
4. Keep source review, compiled restrictions and runtime parity distinct; close only
   the original fact-provenance gate if their combined evidence is sufficient.

## 2. Design

Active clients are CommandShell, TextClient, FieldPlayer, LiveDemo.Program and
Harness.Program. Input selection, parsing, I/O, display formatting and engine clock
requests are client concerns. Placement validity, route search, work progress,
operation state, money availability, production, objectives and outcomes are engine facts.

Inspect actual calls to PlanningView, InfrastructureView, WorkView, CompanyView,
ResearchView, ExplorationView, entity snapshots, Preview and engine results. Trace
placement and route producers to existing contract owners, not local algorithms.
Use the existing OilCo.Tests PurityTests and OilCo.PublicProof ThinClientTests,
without moving/duplicating their ownership or weakening any assertions.

If evidence reveals a missing engine fact or local gameplay rule, stop closure and
correct the authoritative engine path. Do not fabricate a default or silently edit
the read-only consumer. This stage presently anticipates verification, not new APIs.

## 3. API surface

No planned API, protocol, renderer, adapter or Draw changes.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| New | stage evidence report | Low: coverage and limits |
| Modify | P5 design/master/README/AGENTS | Low: original requirement status |

Production/test sources remain unchanged unless a demonstrated issue requires a
separately described repair. No parallel source/binary verifier is added.

## 5. Error handling matrix

| Finding | Required behavior |
|---|---|
| Missing engine fact/local gameplay formula | Keep original requirement open; repair engine ownership |
| Banned compiled owner or client wall clock | Existing compiled gate fails, no exception expansion |
| Shell/direct parity mismatch or stalled handoff | Retain failure and diagnostics; no instant fallback |
| Optional financial capability absent | Explicit unavailable fact, not invented cash |

## 6. Backward compatibility

Development-only; no compatibility paths or old engine support.

## 7. Verification

Targeted Release builds for OilCo.Tests (normal dependencies include all three
client artifacts) and OilCo.PublicProof. Run exactly:

```text
Clients: only the public surface is used
Oil thin clients: native shell input view purity saved state and engine automation handoff
```

Each bounded60s/5s with diagnostic output. No full/family/profile or demo/campaign
horizon. Observe any live handle to terminal without duplicate process restarts.

## 8. Risks

A source read is necessary because a permitted type may still be misused. A binary
namespace restriction is not dataflow proof. Focused runtime parity complements
complete source provenance; it is not every CLI branch/device/path or full scenario.

## 9. Out of scope

Oil/client/business source edits, full/profile tests, renderer/Draw/platform/adapters,
global completion or staging/commit/push. Original P5.005/007/008/009 remain separate.

## 10. Sub-task execution order

1. Read all active sources/references and inspect existing checker/runtime coverage.
2. Trace every consumed operation/placement/work/route fact to engine authority.
3. Build targeted artifacts and run the two existing exact cases.
4. Record all evidence/limits; update only the proven original requirement.
