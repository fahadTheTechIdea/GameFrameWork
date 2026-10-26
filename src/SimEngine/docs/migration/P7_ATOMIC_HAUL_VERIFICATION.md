# P7-C2a — Atomic carrier and exact-quantity hauling checkpoint

Current owner update: [M66 compound logistics admission and actual focused results](P9_ATOMIC_HAUL_BASELINE_VERIFICATION.md#current-m66-compound-logistics-operation-admission--2026-10-22)
on2.721/save161/replay2. Original logistics/transaction/job/book owners remain; the direct supplier
claim/release group is now scoped and verified through those owners. No business rules changed.
The2.31 record below is historical, not current contracts or current runtime certification.

Date: 2026-10-04. Working-tree build **beep-simengine-2.31.0**, save format **15**.
Standalone framework only; no oil, Tycoon, host, demo, renderer or platform edits/runs.
This report proves one focused slice, not P7 or the full framework roadmap complete.

## Implemented ownership

- `ICapacityPolicy.Quantum` owns the exact native-resource step derived from authored units/measures.
  The existing policy's `Offer` reuses it; `CapacityQuantum.Combine` computes an exact checked LCM.
  No decrement search, guessed precision, second catalog or capacity balance.
- The existing `LogisticsService` owns `PreviewHaul` and `AcquireHaul`. Preview selects an eligible
  actual carrier, reachable endpoints and common source/cargo/destination quantity. Acquire re-evaluates
  current state before mutation. Normal refusals consume no IDs, promises, stock or worker state.
- `WorkSystem` implements `IWorkerDispatch`; registration binds the same system instance. Existing
  `WorkBoard.Assign` / `worker.Job` acquires the selected worker, not a separate fleet-token ledger.
  Successful acquisition includes source, cargo capacity and destination owned receipts. No Open haul
  promises resources while waiting for an unspecified carrier. Non-haul work retains normal scheduling.
- Physical pickup consumes source and cargo-admission receipts, then protects actual carried stock with
  a cargo-outbound receipt. Delivery consumes exactly its own cargo/destination receipts. Unreserved
  consumers cannot steal cargo. Interruption releases surviving claims before freeing the worker;
  standing engine policies/routes replan. Actual cargo remains physical and returns through the same path.
- `ISupplier` removes anonymous `Supply`. Finite supply delegates owned commitments to its existing
  inventory. Authored unlimited supply owns fulfillment promises only, not fictitious stock. Payment
  happens at physical pickup, not planning. Refusal leaves the receipt available for normal cleanup;
  no partial refund or fabricated compensation path remains.
- `LogisticsView.PreviewHaul` exposes detached authorized facts from that one engine evaluator.
  JSON still owns resources, units, capacity limits, roles, prices and prototypes. Runtime commitments
  belong to existing state owners. Tests submit normal stock/access commands and instrument engine-side
  contracts/setup; no hauling planner or delivery logic was moved into the harness.

Breaking declarations: WorkBoard schema **5**, built-in Supplier schema **2**, independently compiled
finite-supplier declaration **2**, logistics view **2**. Existing inventory records remain their sole
stock/receipt authority. New cargo/source receipt references save/hash with the same jobs and validate
against surviving endpoints and worker relations. No old-format reader or compatibility overload.

## Exact verification

Final source build:

```powershell
dotnet build Beep.SimEngine.slnx
```

Exit 0, **zero warnings / errors**, **2.30 s** reported by MSBuild.

Only these two complete names were selected, each invocation running one suite:

```powershell
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- 'Atomic hauling: common quantities, all-participant refusal and exclusive carrier acquisition'
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- 'Atomic hauling: protected cargo, route interruption, paid supply and saved physical workflows'
```

| Suite | Checks | Failures | Aborts | Final suite time |
| --- | ---: | ---: | ---: | ---: |
| Common quantity / all-participant refusal / exclusive carrier | 39 | 0 | 0 | 125 ms |
| Protected cargo / interruption / paid supply / saved physical workflows | 47 | 0 | 0 | 282 ms |
| Total | 86 | 0 | 0 | 407 ms |

Coverage includes:

- Authored independent point-space source/cargo/destination steps of 3/2/5 native units; exact common
  step 30, request 35 -> 30, request 29 refusal. Overflow/default/unknown resource steps refuse.
- Held or busy carrier, exhausted stock and competing acquisitions leave all state/hash unchanged on
  refusal. Two actual carriers own separate source/cargo/destination receipts and physically deliver.
- Private preview authorization and detached retained facts; format-15 acquired jobs save/continue
  with the exact same workers and hash.
- Normal stock command, real travel, protected actual loaded cargo, interrupted engine route,
  complete claim cleanup, stranded-cargo save, physical return/replan and conservation/twin hashes.
- Finite paid supplier: requests 20 + 20 acquire only actual stock 20 + 5; no payment before pickup,
  exact total payment 175, physical destination stock 25 and saved continuation identity.
- Directly affected existing construction/production/infrastructure/cancellation physical scenario
  runs inside the second exact suite. It does not select an additional suite or entire old family.

Physical waits are bounded with stage/tick/fault diagnostics. Initial runs aborted because the new
authored JSON was missing from explicit embedded resources. The next run exposed unsettled fixture
spawns before dispatch/save. Resource packaging and fixture lifecycle were corrected; both final runs
above are green. These were test setup defects, not reasons to fabricate runtime fallback behavior.

## Still open

- P4/P9 authored tool/dock participant requirements and capacity/queue rules; no dummy participants.
- P7-C3 formal transaction phase/receipt checks, provider versions, malicious partial-provider fault
  injection, orphan saved-claim validation and resource-flow journals/accounting diagnostics.
- Exact provider preflight is a contract guarantee. A violating provider faults the session; current
  tests do not prove arbitrary hostile cross-provider commits are transactional or roll back.
- Capacity/logistics scale/deadlock/allocation/soak gates, network topology spill/conservation and
  generic construction/activity-DAG completion. Later P8–P12, oil migration and final release gate.
- Prior C1/B/other named suites compile but were not rerun as a whole. No broad/full suite was run;
  stage closure requires its authorized source-specific gate, not extrapolation from 86 checks.

The development-lifecycle skill guided the staged single-owner API migration, explicit schema break
and evidence boundaries. The user's exact-test policy takes precedence over broad/full stage runs.
