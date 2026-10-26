# P10-A2d — authored successful-service network arbitration

Status: historical native focused slice verified,2026-10-06, build2.127.0/save70/LineService4/view4;
not rerun. Current [A4 solvers](P10_NETWORK_SOLVERS_DESIGN.md) requires explicit JSON solver selection
on2.128.0; existing save70/service4/view4 payloads remain unchanged. Its114 focused checks are not
a rerun of the78 arbitration checks below.
Clean build;78 checks/two exact suites (28 contracts;50 physical), zero failures/aborts.
[Evidence](../../docs/migration/P10_NETWORK_ARBITRATION_VERIFICATION.md). Previous suites compile only.
Subdivision of P10, not a new master, utility solver or full framework completion. Lifecycle guidance
kept existing owners, staged scope and directly affected verification gates.

## 0. Problem and measurable goals

Standard/NetworkFlow.cs rotates pull enumeration by world.Tick modulo total port count. An exact
rate whose whole transfer is due at the same residue can starve other compatible consumers forever.
Pushes always use entity enumeration order. Neither rule is authored or replaceable through a contract.

1. Each NetworkDef explicitly references a JSON-authored NetworkFlowPolicyDef implementing
   INetworkFlowPolicy; missing/wrong references refuse before tick one, with no implicit policy.
2. Built-in round-robin ranks due candidates after the last actual successful participant, never
   after tick number or a failed attempt. Stable identity resolves equal custom ranks.
3. Existing NetComponent records the last successful entity/tick per resource and push/pull direction.
   Idle, gated, dry, full and unsuccessful attempts never rotate this fact. A changed graph component
   starts a new history; unchanged components retain history through existing topology identity rules.
4. Actual receipts remain the only stock movement. Existing elapsed eligibility still accrues before
   any transfer; no banked unused allowance, teleport, stock duplication or new queue is introduced.
   There is one game clock. A tick is its internal fixed-step index; local elapsed work and historical
   timestamps derive from that clock, never independently advancing clocks.
5. Save/load/hash/interleaved sessions preserve arbitration. Ordinary public-only extension content
   can change ranking without editing LineSystem/LineService. Detached facts contain no World/entity
   handles; invalid or mutating participant reads fault normal execution and cannot checkpoint.

## 1. Design and API

Add Standard/NetworkFlowPolicies.cs with immutable NetworkFlowCandidate (entity/company/resource,
direction, requested amount, combined quantum, eligible ticks), INetworkFlowPolicy.Id/Rank(candidate,
lastServed), open abstract NetworkFlowPolicyDef and built-in RoundRobinNetworkFlowPolicyDef.
Lower signed64-bit rank wins; ties use stable entity ID. Rank is pure immutable content behavior,
not stock allocation or permission. JSON network_round_robin_policies defines explicit named native
policies; networks require flow_policy referencing a resolved open base definition.

Existing LineService Flow captures readiness and advances elapsed cursors as before. Build transient
due batches keyed by actual component/resource/direction. Rank once per candidate, validate participant
versions/liveness/metadata around callbacks, then sort and recheck actual current capacity/stock before
verified transfers. Batches use stable network/component/direction/resource ordering. Successful
commit alone updates that component's sparse history. No tick-based rotation survives.

Existing NetComponent embeds sparse FlowTurn records; existing LineService.Sync embeds their codec.
Validate stable resource/direction order, carried resource, positive historical identity/tick and
past bounds. Historical winners may legitimately be removed; no live-reference fallback or substitution.
Build2.127.0/save70/LineService4. Infrastructure view4 and other owner payloads unchanged.

## 2. Content

```json
{
  "network_round_robin_policies": [{"id":"field.fair-flow"}],
  "networks": [{"id":"field.pipe-network","flow_policy":"field.fair-flow"}]
}
```

The network snippet shows only changed fields; existing line,capacity_policy and native rational
rates remain required. Documents are explicit master-manifest entries owned by their declaring
module. Other registered content sections may supply derived NetworkFlowPolicyDef implementations.
No closed strategy enum, coordinator type switch, legacy reader or fabricated default.

## 3. Files and execution order

1. Add this design and tracker entry; inspect actual owner/contracts.
2. Revise NetworkDef/InfrastructureModule registration and explicit proof JSON.
3. Extend existing flow/component Sync; remove tick arbitration, not preserve a parallel path.
4. Add public-only policy/fault proof and two exact bounded tests; migrate prior schema assertions
   and compile only. Build completes before either test runs.
5. Align parent/master/AGENTS/README/help and current verification evidence.

## 4. Verification and limits

Only these two complete exact suites, including repair reruns:

- Network arbitration: authored contracts, pure ranking and durable history
- Network arbitration: competing physical legs and saved fair continuation

Prove sparse discrete supply with due-time/tick aliasing, push capacity contention, native and
independent ports, zero-budget/failed attempts, idle/gated intervals, save before/after contention,
topology retention/reset, equal-rank stable IDs, custom policy and mutation faults, private views,
native conservation and twin identity. Tests are bounded inputs/Update/observe, not copied scheduling.

Fairness means native successful-service round-robin for the same component/resource/direction
among repeatedly due compatible candidates and adequate actual supply/capacity. Unequal budgets,
cross-resource shared-capacity fairness, multi-network conflicts, runtime player policy edits,
utilities/provider-neutral graphs/global rollback/replay/stress/full P10/P1-P12 remain open.
This slice removes the specific tick-alias starvation; it does not claim unconditional scheduling
fairness, arbitrary-provider determinism or a utility allocation solver. No host/demo/platform/Draw work.
