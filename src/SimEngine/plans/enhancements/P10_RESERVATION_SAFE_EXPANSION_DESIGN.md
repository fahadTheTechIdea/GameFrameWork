# P10-A19: reservation-safe connected-network expansion

## Problem and scope

Earned corridor work currently stamps a new vertex before the next topology rebuild. If the stamp changes a pool with active resource receipts, the original redistribution correctly refuses endpoint replacement, but geometry has already changed and the session faults. Removal already preflights this condition.

This stage adds expansion admission to the existing NetworkService and physical completion path. It also authors a target-connected-network resource requirement using the existing activity requirement, diagnostics, claim and endpoint contracts. No second inventory, promise registry, graph scheduler, clock or host rule is introduced. Tycoon, oil content, platforms and rendering are excluded.

## Ownership and implementation

1. Extend INetworkTopology with pure CheckAddition. Validate actual catalog line, selected space, company and physical placement through existing contracts.
2. Project proposed vertices into the original transient BuildTopology read-set. Reuse reciprocal-edge validation, company partitioning, exact unchanged endpoint retention and Redistribute. Refuse affected active promises before any spatial mutation; allow unrelated unchanged pools.
3. Call admission only at earned segment completion. Plans remain valid intents; a retryable typed blocker retains earned work and original paid resources. Existing authored retry policy controls delay and retries.
4. Register target_network_resource_requirements in InfrastructureModule. Resolve the authored network and native bill before initialization; use original INetworkDiagnostics for the target's actual connection and IResourceWorkRequirement for actual claims. Retained receipt lookup must match the original endpoint exactly, never substitute a different connection.
5. Use one new exact public-player proof: JSON/master definitions, ordinary work/plans, physical flow, Game.Update, immutable views, pending-completion save/load, release through ordinary cancellation/completion, replay and session isolation. No direct world, pool, stock or layer edits in the driver.

## Failure and durability rules

Missing/foreign content, invalid placement or incompatible native quantities refuse through current contracts. Changed connections become typed unavailable requirements. Active promises return sim.resource.active-promises. Provider faults stay terminal through existing observer/commit guards. No implicit retry defaults, forced release, instantaneous work or compatibility reader. Save/replay schemas and existing topology codec remain unchanged unless new authoritative state is actually required.

## Verification

Scoped verified on core2.468.0/save154/replay1: FINAL137/0/0 in25017ms; four owned point minute/half cancellation/natural-settlement flows, real held receipts, unrelated versus adjacent construction, earned completion waits, once-only materials, private immutable facts, saved twins/replay/isolation. One early unknown-network content refusal. Existing topology/cursor/view codecs unchanged.

One solution compilation failed with two new call-signature errors; five necessary focused compiles include one fixture failure and four clean results, FINAL9.93s. All four same-case runtime attempts retained. No solution rerun, historical/family/full runtime or executable edit after FINAL. [Evidence](../../docs/migration/P10_RESERVATION_SAFE_EXPANSION_VERIFICATION.md).

Original221/185checked/36open unchanged; additional194. P10.001/native expansion/all-topology/arbitrary-provider/fault/stress/performance/full P1-P12 remain OPEN. No Oil/platform/Draw/Tycoon/G19/clock edit or publication.

Metadata closure at 2026-10-12 00:36:06 UTC:1639 source anchors/13 quickstart anchors/883 selected executable inputs, zero SHA drift;2827 paths/996 Markdown/8624 local links, zero broken;18 master rows/zero malformed; thin driver boundary matches0;git diff --check exit0 (34 warning lines),dirty2815/staged0. Original221/185checked/36open unchanged; additional194. Protected GameClock and Tycoon hashes unchanged, goal_complete false. Ten prior selected executable inputs changed, three new executable inputs (one production selector), five original production files changed; no executable edit after FINAL. Original requirement literals/checkboxes/line anchors unchanged. All four attempts retained; no full-runtime or terminal-solution success claim.
