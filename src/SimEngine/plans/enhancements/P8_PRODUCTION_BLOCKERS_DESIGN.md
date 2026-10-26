# P8-F3 — Physical production blockers

Status: scoped verified on2.409.0/save154/replay1, FINAL77/0/0 in25684ms. Original P8.acceptance.012 verified;221/155checked/66open, full goal/plan OPEN. [Evidence](../../docs/migration/P8_PRODUCTION_BLOCKERS_VERIFICATION.md).

## 0. Problem

The original acceptance requires supply, full output, disabled operation, fuel and worker blockers to preserve actual progress and owned WIP. Earlier standalone/shared-store proofs do not establish their combined physical behavior.

## 1. Goals

1. Master-listed JSON composes existing processor, resource, work, tile motion and original scoped vessels.
2. Player inputs physically supply ore, a reusable tool and limited fuel; staffing is an actual engine-created service order.
3. Disabled operation, held bound worker and fuel exhaustion retain partial work and WIP through saved continuation.
4. Full independent product/waste vessels retain completed raw WIP; draining one cannot publish a partial conversion.
5. Physical draining of both releases the whole result, with conservation, privacy and replay.

## 2. Design

Use existing Transformer, WorkBoard, ResourceInventoryState, LogisticsService and session clock. Reuse original EndpointProofModule/OutputPortProofModule for independent stores. No production, supply or timing owner in the driver. Two-dimensional authored tile geometry prevents the one-dimensional fixture's opposing-machine occupancy trap; no teleport/crossing fallback. Production remains native contract-selected execution.

## 3. API

No new runtime API. Existing enqueue, operation, worker hold, hauling commands and immutable transformation/work/stock views.

## 4. Files

New production-blockers master/document and ProductionBlockerTests driver; existing explicit resource/recipe/work/logistics/world documents are reused without alteration. Register exactly one case and two embedded documents.

## 5. Errors

Bound every stage to256 Updates with actual blocker/progress/job diagnostics. Preserve failed attempts. Missing physical supply never becomes fabricated stock or an instant fallback.

## 6. Compatibility

No compatibility path or duplicate owner.

## 7. Verification

One NEW exact bounded case only; current solution compile and that case. No old/family/full runtime. Per-update actual raw/result/WIP/tool conservation, saved twins, fuel totals and public replay.

## 8. Risks

Native work/dock interaction may expose a real integration defect; repair the responsible original owner only if evidenced. Fixture geometry and quantities are explicit content, not engine limits.

## 9. Non-goals

Tycoon, platforms, protected G19 files, arbitrary provider purity, universal congestion/stress and full regression.

## 10. Order

Author composition → public physical driver → compile → one exact case/needed same-case repairs → evidence and original tracker. Commit/push wait for the whole goal and plan.
