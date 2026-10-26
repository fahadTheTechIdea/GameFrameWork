# Network pools and physical transfers — focused verification

2026-10-05. Working-tree **beep-simengine-2.39.0 / save 18**.
P7-C3b2b4b / P10-A2b is focused-verified. This is not full P7/P10 or roadmap completion.

## Authoritative implementation

LineService remains the graph/pool owner and aliases IResourceEndpointProvider in the existing bindings.
Each NetComponent embeds ResourceInventoryState. Pool is now a read-only stock projection; raw pool list
mutation, TakeFrom and raw port Take/Receive/compensation paths are removed. The authored capacity evaluator
still owns arithmetic; a component wrapper supplies its actual tile multiplier without new balances.

Flow floors the common source/destination quantum, respects real unpromised stock and whole-vector
admission, acquires endpoint-owned receipts, and commits through ResourceTransactions. Each exact leg
publishes the existing ResourceFlowFact with actual scoped IDs, native deltas and owner versions. No
fake Initialized/Produced/Consumed effects represent movement. Networks author their resources, positive
port_rate, capacity policy and explicit public-neutral access through the master-listed JSON document.
Private foreign-company stock remains inaccessible; public-neutral legs are attributed to their real actor.

The ten-minute flow schedule now waits for ten minutes to actually elapse. The prior modulo-at-tick-zero
path granted a full interval during the first Update. There is no extra loop, clock or host timing rule.
Sub-quantum rates round down at each declared flow interval; they do not fabricate fractional native units.
Accumulated fractional rate budgets and advanced solver/throughput policies are not implemented here.

## Topology and persistence

- Pure temporary candidates contain geometry and allocations, not stock owners or allocated IDs.
- Identical owner/tile membership retains the same component instance, stable endpoint ID and live receipts.
- Changed components refuse outstanding claims. Every affected network must pass before commit.
- Changed components receive new empty endpoints at commit; old→new stock travels through verified receipts.
  Retired endpoints are removed only after actual stock/promises empty. No copied pools or fake losses.
- Direct geometry/owner-layer writes are detected through existing transient layer version cursors, not
  only Stamp calls. Invalid changes fault before replacing original pool owners.
- Sync owns the next endpoint ID, component identity/version/owner/membership, pending-geometry status and
  embedded inventory codec. Load reconstructs the saved settled graph cache without redistribution or grants.
  Dirty checkpoints preserve the current map plus prior pool membership until the declared Update phase.
- Layer invalidation versions are transient and not saved as game-state epochs. Load rebases those cursors
  and preserves the pending flag; healthy saved continuations remain identical. Saving/hashing never rebuilds.
- Native/capacity/identity/version/ownership/tile order/connectivity validation refuses malformed state.
  An interrupted topology commit cannot serialize as healthy; there is no compensating rollback.

New-game OnNewGame settles empty startup graphs. LineService service schema 2 and InfrastructureView
schema 2 use save 18; no old graph/pool reader or compatibility API remains. Immutable NetworkPoolView
reports actual endpoint/version/stock/promises/capacity/pending-geometry facts with company/public filtering.

## Commands and final results

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- 'Network transfers: timed quantum, public ownership, private history and faults'
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- 'Network transfers: durable topology identities, live claims and exact redistribution'
```

Final build: zero warnings/errors, 1.74 seconds. Final exact suites:

| Suite | Passed | Failed | Runtime |
| --- | ---: | ---: | ---: |
| Timed quantum, public ownership, private history, provider faults | 62 | 0 | 217 ms |
| Durable topology IDs, live claims, exact redistribution | 60 | 0 | 229 ms |

**122 checks / two exact named suites**, zero aborted suites. No broad/full run. Earlier endpoint,
hauling, topology and recovery suites compiled, but were not executed. The old topology fixture was
source-migrated from direct pool-list edits to the shared owner and admission refusal expectations.

The proof covers waiting before flow, discrete/continuous/common capacity quantum, actual source/pool/
destination conservation, capacity promises/backpressure, actor/private history, explicit public-neutral
access, dishonest retained receipt faults, save/load before flow and with active claims, dirty saves,
unrelated component identity retention, split/merge journal legs, pure previews, all-network refusal,
stocked removal protection, direct layer bypass, read-only pool projection and corrupt owner codecs.

Initial failures exposed the real tick-zero flow bug and the need to keep transient layer versions out
of persistent state. Both production paths were corrected. Proof setup explicitly selects the optional
journal, and public-pull assertions now read after opening-stock cursors rather than mislabeling startup.
The final same two suites passed after these corrections.

## Remaining work

State-preserving replacement still needs an ownership/relocation accounting review in Upgrades.TryReplace;
incident destruction, authored pool recovery/spill policies, cargo disposition and complete lifecycle
accounting remain open. Full generic solver/spatial-provider contracts, utilities, automation, activity
DAG/tool/dock integration, throughput/backlog diagnostics, rate accumulation, allocation/stress gates and
authorized regression are not proven here. The full framework remains partial.

No oil/Tycoon, demo/harness gameplay, Draw, renderer, Unity, Godot or platform adapter changes.
The development-lifecycle skill guided the existing-owner rewrite, staged contract and focused proof.
