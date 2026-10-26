# P7-C3b2a — Exact lifecycle batches and physical consumers

Date: 2026-10-04. Working-tree identity **beep-simengine-2.34.0 / save 16**.
Standalone engine and independently compiled proof providers only. No oil/Tycoon, host/demo gameplay,
renderer, Unity/Godot or adapter changes/runs. The full framework and P7 stage remain unfinished.

## Replacement and ownership

The existing `IResourceTransaction` / `ResourceTransactions` now owns `EvaluateInventory` and
`CommitInventory` alongside owned physical transfers. `ResourceInventoryIntent` contains one existing
endpoint, company/requester, immutable Bill, explicit effect kind and cause identity. Supported lifecycle
effects are Initialized, Produced, Consumed, Destroyed and Lost. Results report changed-resource count,
never a scalar sum of incompatible units. This intent contains no stock or promise authority.

Preflight checks every resource/native unit/amount, exact provider quantum, compatibility, available
unpromised stock and whole incoming capacity against existing inbound promises. Normal refusal mutates
nothing. Commit re-evaluates current state; snapshots are transient verification facts. Every physical
line must return exactly the intended quantity, change actual stock exactly and advance the endpoint
version. The complete post-batch stock vector, including unrelated resources, and every owned promise
must match. Inspection cannot secretly advance the version. Only after the whole batch verifies do
success facts publish. A provider violating preflight faults the tick without compensation or a safe
partial save. This is not a cross-frame WIP/tool/graph reservation protocol.

`IInventory.TryConsume` and both implementations were removed, not wrapped. Production input/output
and work-step material consumption migrate to the existing transaction owner. Previously their
TryConsume result was ignored; now refusal cannot advance a stage or charge money. A provider violating
the exact commit contract faults. Money remains at the existing economy owner; failure after successful
payment preflight is a protocol fault, not free progress or an invented refund. No new economy is required
for unpriced recipes/work.

Recipe and work-plan JSON continues to author bills, durations, dependencies and effect causes. Production
consumes at its existing start phase, advances through authored game time and produces only at real
completion/capacity/rollout. Work records actual consumed stage materials once; updates/completion do not
consume again. Spent inputs are not recreated during saves, cancellation or failure. Entity output still
uses the existing production rollout/completion path.

`ResourceFlowFact` is generalized in place: common requester/company/resource/native amount plus either
actual transfer receipts or an explicit lifecycle endpoint/cause. The former nested transfer Intent
property is removed. There is no fake transfer token or parallel event/history/ledger. The same optional
JSON-configured journal, company privacy, retention cursors, EventBus phase and single service Sync
persist both kinds. Retained page totals separate transfer legs/movement, stock purchases, production/
authored-source creation, initialization, consumption, destruction and loss. They are not whole-session
balances; expired history remains explicitly marked.

Journal restore validates the active payload kind, real endpoint, nonempty cause, native unit/quantity,
exact deltas and relevant versions, and rejects inactive transfer/lifecycle metadata. Integer enum values
are range-checked before narrowing so malformed values cannot wrap into a valid byte kind.

Event, journal service and flow view schemas are **2**; build **2.34.0**, save **16**. Existing exact save/
peer identity and codec manifests reject previous layouts. No old reader, duplicate bill consumer or
compatibility facade remains. Earlier affected tests migrate/compile but are not rerun as whole suites.

## Exact verification

Final build:

```powershell
dotnet build Beep.SimEngine.slnx
```

Exit 0, zero warnings/errors; MSBuild **2.18 s**.

Only these two full names were selected, one suite per invocation:

```powershell
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Resource lifecycle: exact batches, refusals and saved native accounting'
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Resource lifecycle: authored timed production, work and provider violations'
```

| Exact suite | Checks | Failures | Aborts | Final time |
| --- | ---: | ---: | ---: | ---: |
| Exact batches / refusals / native saved accounting | 56 | 0 | 0 | 137 ms |
| Timed production / work / provider violations | 39 | 0 | 0 | 154 ms |
| Total | 95 | 0 | 0 | 291 ms |

All invocations in this slice used these same two names. The first timed suite run found one incorrect
test expectation: a material-blocked stage is Supplying, not Waiting. It was corrected to the existing
authoritative state; no gameplay workaround was added. Final evidence above is clean. No broad/full run.

Covered outcomes:

- Master-listed count/mass resource bills, exact initialization, native-unit facts, actual stock and
  explicit causes. Preview/invalid owner/anonymous cause/wrong kind/missing stock/capacity/native or
  measured-quantum refusal changes no hash, stock or promises; a compatible first line never commits
  when a later line fails normal preflight.
- Existing inbound/outbound promises remain protected. Empty explicit bills emit no fake event. Batches
  report resource counts rather than mixed-unit totals; all five lifecycle kinds use the same owner.
- Saved twin batch execution, retained-window totals/gaps, real loss/destruction/production deltas,
  generalized save/hash and interleaved owned transfer facts through the same journal.
- Corrupt lifecycle restore: negative tick, truly missing cause with a valid remaining payload layout,
  integer kind wrapping, and negative endpoint identity all refuse.
- Mapless unpriced authored production: actual input quantity 3 at start, elapsed recipe time, output
  quantity 1 at completion, entity output, saved WIP/twin identity and exact input/output facts.
- Local work with a competing owned pickup cannot consume its stage bill or progress to Working.
  After release, actual bill quantity 2 is consumed once; authored time elapses to Done with saved twin
  identity, no repeated consumption and a plan/stage cause.
- Independently authored partial Remove and Add return values fault production/work through the actual
  engine execution phase. Failed batches emit no successful fact. A valid earlier input remains history
  if later output fails; no compensation or resumable partial save is created.

Physical waits are bounded (30 ticks) and engine tick/fault diagnostics are surfaced. Proof behavior is
inside the non-friend ExtensionProof engine module; tests issue player commands, read/advance or inject
contract corruption. No host gameplay was introduced.

## Remaining scope

C3b2b must migrate authored initial stock creation, raw mutation callers, entity replacement/despawn/
salvage/cargo disposition and network pool/topology loss through their existing owners. The new lifecycle
protocol is available, but does not automatically observe every primitive Add/Remove/Receive/Take or
independent provider OnCreate. Journal opening facts cannot yet be claimed complete for every scenario.

P4/P8/P9 WIP/batch transformations, multi-endpoint/tool/dock/graph acquisition; P10 topology accounting;
P12 environmental consequences; broader fault/deadlock/soak/allocation/privacy and authorized stage/full
regression gates remain open. Trusted extension contracts are not a sandbox against arbitrary CLR code.

The development-lifecycle skill guided the execution contract, sole-owner replacement and source-specific
evidence. The user's exact-test instruction overrides its broad/full regression step.
