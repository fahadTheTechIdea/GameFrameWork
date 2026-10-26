# P7-C3b2b3 / P10-A1 conserved network topology — focused verification

2026-10-04. Working-tree build **beep-simengine-2.37.0 / save 16**. Existing pool/service layout remains
schema 1; no compatibility reader. Exact build identity rejects previous simulations. This is a P7/P10
conservation prerequisite, not completion of the resource protocol, activity DAG or framework roadmap.
No oil/Tycoon, platform, adapter, rendering, host/demo gameplay or full-test changes/runs.

## Engine ownership

INetworkTopology is an optional built-in tile-network contract. InfrastructureModule binds the existing
LineService instance under that contract; it remains the sole graph/pool owner. CheckRemoval and Remove
are engine execution hooks for physical work providers. No instant player/admin removal command was
introduced. InfrastructureView.PreviewRemoval reads the same pure rule; a host does not calculate
resource disposition or topology itself.

BuildTopology constructs a temporary candidate without changing the map, ownership, graph or stock.
Redistribute is the sole split/merge rule: stable overlap ordering, exact policy-native quantum,
wide integer proportional products, authored receiving capacity, then bounded remainder placement
in largest-overlap/stable identity order. Discrete resources stay whole; continuous quantities keep
their representable fractions. Existing shared capacity is checked across the complete resource vector,
not independent per-resource totals or dimensionless free space.

Removing every tile of a stocked component returns sim.network.occupied. Insufficient remaining
capacity returns sim.network.insufficient-capacity. Foreign owned segments refuse before pool evaluation;
redistribution also refuses ownership reassignment of an occupied component. There is no authored
spill/recovery policy yet, so loss is never substituted. Empty components may disappear normally.

EvaluateRemoval privately resolves pending additions before evaluating removal. All networks sharing
the line layer must pass before Remove changes geometry/ownership and commits candidates. A retained
preview is not authorization: execution evaluates current stock again. Scheduled RebuildDirty also
prepares every dirty network before committing any candidate. A direct invalid layer mutation bypassing
the contract faults the engine before replacing original pools, and cannot create a healthy checkpoint.
Temporary candidates are not persistent authorities; no copied balance or alternate graph survives.

ValidatePool shares native resource/quantity/order/capacity checks across redistribution and Sync.
Malformed native quantities, unknown/non-carried resources, duplicate/out-of-order entries, nonpositive
stock and over-capacity vectors reject healthy serialization/restore. Save/load reconstructs components
through the same graph builder and existing pool codec. Current stock and geometry retain one owner.

## Exact verification

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Network topology: native splits, merges, capacity and removal refusals"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Network topology: saved multi-network ownership and current-state execution"
```

Final build: exit 0, zero warnings/errors, **1.76 s** reported build time.
Each full name selected exactly one suite; no broader filter or full suite was run.

| Exact suite | Passed | Failed | Elapsed |
| --- | --- | --- | --- |
| Native splits, merges, capacity and removal refusals | 30 | 0 | 172 ms |
| Saved multi-network ownership and current-state execution | 28 | 0 | 185 ms |
| Total | **58** | **0** | Two suites; no aborted suite |

Proved by the first suite: sole-owner binding, pure immutable preview/hash identity, five-to-four tile
split, whole steel remainder and fractional water shares, exact conserved totals, save/load after split,
ordinary stamped reconnection/merge, last-tile refusal, reduced-capacity refusal with unchanged hash,
pending additions evaluated without preview mutation, valid execution after added capacity, wide raw
quantity arithmetic whose overlap product exceeds Int64, and identical wide-stock checkpoint identity.

Proved by the second suite: foreign ownership refusal, invalid/duplicate/missing segment references,
current-state execution after changed secondary-network stock, no primary graph commit when a secondary
network refuses, multi-network saved continuation, exact-capacity stock conservation, ownership clearing,
empty complete removal, healthy reconstructed checkpoints, invalid direct-mutation fault retaining
both prior graph owners, refusal of a faulted save, fractional discrete and over-capacity pool save rejection.

The single added network-topology.json document supplies explicit policies, line/layers and two networks
through the existing master manifest/proof factory. Existing recovery JSON supplies shared time/resources/
machines rather than duplicate fixture definitions. Pool setup/draining is explicit test instrumentation
of the sole owner, **not a verified network transfer or player flow scenario**. Tests do not implement
topology calculation, scheduling, loss policy or gameplay in a host/harness.

An early large-value fixture multiplied its per-tile limit beyond the representable total, and correctly
failed capacity validation. Its authored limit was corrected to a representable total; the final stock
still requires a wider-than-Int64 proportional intermediate. Final evidence above is after that repair.

## Remaining and verification limits

- Raw network outlet.Take/inlet.Receive still bypass owned transfer receipts and the flow journal. They
  must move to the existing transaction owner, with explicit pool endpoint identities/reservations, not
  invented Consumed/Produced lifecycle facts or a second pool inventory.
- Stable persistent network component identities, queued network promises, explicit topology disposition,
  spills/recovery/isolation, pressure/solver/utility contracts and private throughput diagnostics remain open.
- These topology execution hooks do not provide a timed player removal workflow; future physical activity
  effects decide when to invoke them. No instant normal-scenario command was added.
- No source inventory/WIP/full-session conservation ledger or provenance/quality salvage proof is claimed.
- Valid restored stock uses the existing codec in both suites; individually byte-mutated network-save
  payloads were not fuzzed. Native/capacity rejection was directly exercised through healthy-save guards.
- Previous recovery (88), initial inventory (108), lifecycle (95), Standard line and generic lifecycle suites
  were not rerun. Their reports are historical source-specific evidence, not current regression claims.
- P4/P7/P8/P9/P10 and the full stage, fault/stress/allocation gates remain unfinished. Full/broad gates
  need explicit user authorization; this change ran only the two exact suites above.
- Development-lifecycle skill guided the engine execution contract, existing-owner revision, scoped
  verification and single master-tracker update; it did not close the phase or authorize broad testing.
