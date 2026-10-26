# P4-G2b2a/G2c1 — Authored resource requirements and physical lifetime

Date: 2026-10-05. Working-tree evidence, not committed-HEAD or full-regression completion.
Engine **2.49.0/save24**, work/view9, resource-flow4. Old saves are rejected; no compatibility reader.
Only standalone SimEngine runtime/proof/tests/docs changed. No oil, Tycoon, host/demo, Draw or platform work.

## Final verification

`dotnet build Beep.SimEngine.slnx`: zero warnings/errors, final source build1.59s.
Runtime, public non-friend ExtensionProof, testing library and test project compile together.

Exactly these two directly affected named suites ran on final source:

```powershell
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Activity resources: authored scoped offers, native sets and private selection'
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Activity resources: physical consumption, held tools, interruption and saved ownership'
```

| Exact suite | Passed | Failed | Final runtime |
|---|---:|---:|---:|
| Authored scoped offers, native sets and private selection | 55 | 0 | 246ms |
| Physical consumption, held tools, interruption and saved ownership | 104 | 0 | 858ms |
| Total final-source checks | 159 | 0 | 1104ms |

Neither suite aborted. Counts are the final run, not sums of reruns. Other callers/schema assertions
compile but their suites did not run. No broad/full regression, oil, replay, lockstep, stress, soak or
allocation gate ran. Previous reports remain historical evidence, not passes for current source.
Targeted tracked diff checks and explicit whitespace scans of new resource-stage/runtime/proof files
pass; Git reports only LF/CRLF normalization warnings.

## Engine ownership and extension proof

- IResourceWorkRequirement extends the existing authored leaf family with immutable native Bill/use,
  pure CopySources and CheckSource. Master-listed target_resource_requirements and
  worker_resource_requirements register strict consume/hold JSON. Illegal quantities, missing units,
  empty bills, unknown references and zero-duration resource work refuse explicitly.
- WorkBoard remains the sole expression/source selector. Cost, ordinal scope and positive64 identity
  order actual offers; foreign private endpoints are omitted before private availability is exposed.
  Native stock observations are ResourceTransactions facts, not mixed-unit totals or balances.
  The existing iterative all/any evaluator validates every branch; retained previews never authorize
  mutation. Joint prefix-preflight finds a feasible alternative when two leaves compete for one pool.
- The public proof adds an unrelated connected-source requirement via normal registered content and
  a target capability listing actual scoped endpoints/costs. No scheduler modification or fake remote
  fallback is needed. Its pools embed existing ResourceInventoryState and receive stock by verified
  transfers. The proof authors an actual wrench/tool, machine and activity through JSON.
- ResourceTransactions acquires the complete native selected set before WorkSystem commits the
  worker.Job/Job.Worker relation. Existing endpoint books alone own live promises; node Resources
  retains immutable receipt links/facts, not a second reservation book. Owners use unambiguous named
  order/node/occurrence identities. Entity and wide scoped IDs follow the same protocol.
- Machine travel and reach precede consumption. Positive effective work consumes startup inputs once
  through exact ConsumeClaims; zero rate produces no progress/consumption/history. Hold protects real
  tools rather than spending them. Positive timed activities use the same acquisition/lifetime path.
- Pause retains tools/branch/progress. Interruption, loss and cancellation release surviving unspent
  promises only. Spent inputs remain spent, keep their authored branch, and never require the old source
  to remain alive. A new machine physically travels before continuing without repeat spending/refund.
- The existing WorkBoard codec saves exact occurrence/source/usage/receipt/spent facts. Restore checks
  authored branches/native bills, owners/tokens, live assignments and endpoint books. Entity and scoped
  orphan engine claims reject; providers push their existing books via pure InspectReservations,
  not a new registry. Views are detached and private, with actual commitment distinct from offers.
- Provider acquisition of unrelated promises faults normal Update. No partial-success history,
  compensation or resumable checkpoint is fabricated. All stock mutations use the existing coordinator.

## Focused outcomes and remaining scope

The two suites cover pure immutable offers, role/tool eligibility, private source omission, fractional
native units, aggregate shortages, feasible joint alternatives, actual exclusive claims, bounded
travel/work, held tools, pause, consumed-branch retention, reselected workers, before/after-start cancel,
timed work, lost claims, zero work rate, corrupt ownership/path/use/count/spent state, scoped/ordinary
orphan detection, provider fault and saved twin continuation/hash identity. Physical stages have a
500-tick bound with node/job/progress/blocker diagnostics, not an unbounded acceptance wait.

P4-G2c2 must migrate and delete the older Needs/cash/TrySupply early-consumption path and bring existing
supply/money consumers into the same authored requirement/commitment protocol. It is not complete in
this slice. No legacy translation or duplicate migration wrapper is intended. Operating-area/target
policies, continuous fuel/fluid, generic executors/templates, full joint money admission, DAG prior
facts/ETA, oil integration and P4/P7/P9/whole-framework acceptance remain open. Joint selection has no
hard content cap, but worst-case combination/performance budgeting and stress proof remain open.

Lifecycle guidance kept one owner and staged evidence. User/repository exact-suite restrictions govern;
focused verification is not completion of the full P1-P12 roadmap.
