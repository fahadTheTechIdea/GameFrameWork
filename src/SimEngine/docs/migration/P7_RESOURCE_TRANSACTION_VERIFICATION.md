# P7-C3a — Owned physical transaction protocol checkpoint

Date: 2026-10-04. Current working-tree identity **beep-simengine-2.32.0 / save 15**.
Standalone SimEngine only; no oil, Tycoon, host, demo, renderer or game-platform edits/runs.
The full framework roadmap and P7 stage gate remain unfinished.

## Single-owner replacement

`InventoryModule` binds one `IResourceTransaction` implementation, `ResourceTransactions`, for engine-
side modules. It stores no balances, promises, persistent transaction list or new clock. Existing
inventories own stock and endpoint books own records. `ResourceTransferIntent` references those owned
source/destination receipts; it never mirrors resource ownership.

`ResourceReservationBook.Inspect/Check` is the shared typed receipt lookup. `CheckReservedIn`,
`CheckReservedOut` and `CheckSupply` are pure provider preflights, reused in the providers' mutations.
The actual provider owns compatibility, capacity, stock and price/payment rules. Other modules retain
their own reservation scopes; the engine does not reinterpret them as haul jobs.

`Evaluate` validates exact receipts/resource/quantity, current endpoint ownership and provider checks,
returning detached actual entity versions. Query mutation, fabricated readiness and invalid check
statuses refuse/fault explicitly. A retained check cannot authorize later mutation.

`Commit` evaluates current state before either participant mutates. Normal invalid/removed/stale/
stock/capacity/payment refusal changes no stock, receipt or money. Success requires exact source and
destination quantities, actual owned-stock deltas, consumed receipt tokens and advanced endpoint
versions. A source must not change its destination; a destination must not change its source.
A successful-preflight violation throws into the physical engine phase and faults that tick; no refund,
invented stock, compensation transfer, rollback or resumable partial save is manufactured.

`LogisticsService.Load/Unload` removes its separate mutation ordering and delegates to that one
transaction owner. The existing `WorkSystem` still owns movement and execution. Pickup also validates
the final destination before consuming source stock. Normal refusal ends the complete commitment
and releases surviving claims; physical cargo is not teleported or recreated.

## One authoritative haul phase

`WorkBoard.ValidateHaul` checks the existing itinerary, loaded quantity, carrier/job relationship and
source/cargo/destination receipt phase. There is no added phase field or parallel state machine.
Physical calls require the current Load/Unload toil and actual arrival.

| Current checkpoint | Actual quantity | Required owned receipts |
| --- | --- | --- |
| Travel to source / pickup | Loaded = 0 | Source outbound, cargo inbound, destination inbound |
| Travel to destination / unload | Loaded = exact amount | Cargo outbound, destination inbound; source/cargo-in spent |
| Return already-carried stock | Loaded = exact amount | Cargo outbound, destination inbound; no new source pickup |
| Ended job | No worker/receipts; completed loaded phase cleared | None |

After all service payloads restore, `WorkSystem.OnLoaded` invokes the board's validation and checks
orphan `sim.haul` promises/dangling worker assignments. Surviving participant capabilities and records
must match exactly. Removed endpoints or carriers may remain referenced only so the next engine
update can release surviving promises; no substitute provider or fake completion is introduced.

No persistent payload was added: save format stays **15**, with existing WorkBoard schema **5**,
Storage schema **3** and independent port schema **2**. Changed simulation semantics have build
**2.32.0**; saves/peers from earlier builds refuse by the existing exact identity check. No legacy reader.

## Exact verification

Final build command:

```powershell
dotnet build Beep.SimEngine.slnx
```

Exit 0, **zero warnings/errors**, **1.83 s** reported by MSBuild.

Only these two full names were selected; each invocation ran exactly one suite:

```powershell
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- 'Resource transactions: pure receipt checks, atomic refusals and saved exact commits'
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- 'Resource transactions: haul phases, orphan saves and provider faults'
```

| Exact suite | Checks | Failures | Aborts | Final suite time |
| --- | ---: | ---: | ---: | ---: |
| Pure receipt / refusal / saved exact commits | 40 | 0 | 0 | 154 ms |
| Haul phases / orphan saves / provider faults | 84 | 0 | 0 | 225 ms |
| Total | 124 | 0 | 0 | 379 ms |

All focused invocations in this slice used these same two names. No broad/full suite or previous named
suite was run. Prior C2a/C1 and other counts remain preceding-source evidence, not current regression.

Verified cases:

- Pure source/admission/supplier checks; foreign/wrong/stale/consumed receipts, exact-quantity
  substitution, ownership and missing destination refuse without changing stock/hash/sequences.
- Intervening compatible destination stock/version changes do not make a retained preview permission:
  Commit re-evaluates, then transfers exactly once. Existing module records and stock save/hash/twin-
  continue with no added transaction ledger. Repeated commit cannot duplicate physical resources.
- Finite paid supply: unavailable cash preflights before consuming any stock or admission; funded
  exact commit takes quantity 15 from actual stock 25 and charges 105 once, with saved twin identity.
- Current-toil/arrival guards prevent early load or teleporting unload. Saves reject invented loaded
  quantity, absent pickup, altered toil quantity, terminal live promises, wrong worker and resurrected
  spent pickup. Orphan haul records reject; unrelated module records remain loadable.
- Normal physical pickup and delivery conserve quantity and saved continuation; successful completion
  clears the loaded phase. Removed final destination preserves source stock and releases surviving
  source/cargo claims. Removed acquired carrier releases both surviving endpoints. Both cleanup paths save.
- Cash exhausted during actual travel is a normal physical pickup refusal, with no cargo, stock loss,
  partial payment, refund or retained source/cargo/destination claims; saved twin hashes match.

The independently compiled port authors six explicit hostile behaviors in the existing master-listed
`haul-quantum.json` proof content. They share that port's one stock/book, not a second balance authority:

1. partial pickup return after actual source consumption;
2. claiming success while retaining the source receipt;
3. changing endpoint version during a pure receipt query;
4. reporting Committed as a preflight status;
5. changing actual stock/receipts without advancing endpoint version;
6. admitting the full quantity but reporting a partial delivery receipt.

Every probe verifies its specific protocol fault message and `sim.work` phase, sticky fault/tick,
non-resumable save refusal and no fabricated rollback. The probes are deliberately invalid engine-
side extension providers, not gameplay in a player harness. All physical waits are bounded with
stage/tick/fault diagnostics. Initial focused runs passed; later reruns strengthened exact-fault coverage.

## Open work / proof boundary

- C3b resource-flow facts/journals/accounting diagnostics through existing event/view ownership,
  never an independent stock ledger. Author creation/destruction/conversion and topology losses.
- P4/P8/P9 graph/batch/transformation/tool/dock acquisition and physical timing integration. The current
  protocol transfers one exact already-reserved resource; it is not an all-resource transformation engine.
- Comprehensive arbitrary-hostile-provider guarantees, ownership/version namespace composition,
  general module-scope orphan validation, stress/deadlock/allocation/soak and fault permutations.
  Extensions must still honor pure queries and notify their existing entity of mutation; this is not
  sandboxing against arbitrary CLR code. Six focused faults do not prove every malicious behavior.
- Generic construction/activity DAG, later P8–P12, oil migration and authorized final regression gate.

The development-lifecycle skill guided a staged sole-owner replacement, explicit identity break and
source-specific evidence. The user's exact-test policy overrode the skill's broad/full regression step.
