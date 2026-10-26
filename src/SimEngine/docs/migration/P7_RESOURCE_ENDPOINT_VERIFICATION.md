# Scoped resource endpoints — focused verification

2026-10-04. Working-tree **beep-simengine-2.38.0 / save 17**.
P7-C3b2b4a / P10-A2a is verified for the focused scope below, not complete P7/P10 or full regression.

## Single-owner change

- ResourceEndpointId owns receipt location: reserved `sim.entity` IDs or declared module-scoped positive
  64-bit IDs. Explicit entity conversion only; no entity-only overload, legacy reader or compatibility alias.
- IResourceInlet/IResourceOutlet carry the existing physical contract outside IEntityCapability.
  IInletPort/IOutletPort compose those same contracts. Every outgoing endpoint still exposes its inlet.
- Existing ServiceContainer discovers declared provider instances in composition order, aliases once.
  No second endpoint registry or global state. Composition rejects foreign worlds, unowned/duplicate/
  reserved scopes; resolution rejects invalid returned metadata. IDs are session-local references, not
  credentials. Providers must resolve purely and keep World/scope/identity/company stable.
- ResourceTransactions remains the sole protocol. Commit evaluates current receipts on the same resolved
  owners it will mutate, verifies exact quantities/deltas/versions, then publishes the existing fact.
  No separate pool transfer coordinator, compensation or fake lifecycle effect.
- Storage's stock and owned-reservation implementation moved into ResourceInventoryState. Storage delegates;
  there is no second stock list/book/version counter. Each embedding owner supplies metadata/capacity and
  embeds the same Sync codec in its existing persistence responsibility.
- The independent public-contract proof module authors four non-entity pools through a master-listed JSON
  document, including an ID beyond entity integer range. They start empty and receive actual stock through
  verified transfers. No host or harness implements stock, admission, timing or transfer rules.

## Breaking state layout

Engine save format 17 rejects old saves. Storage component/inventory view 4; supplier and independent
single-resource components 3; WorkBoard service 6; ResourceFlowFact event/journal/view 3. Transfer history
serializes scoped token IDs in the existing journal codec. Entity lifecycle intents remain entity-specific;
no generalized pool initialization or arbitrary endpoint lifecycle grant was added.

## Commands and final results

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- 'Resource endpoints: scoped providers, pure commits and durable inventory owners'
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- 'Resource transactions: haul phases, orphan saves and provider faults'
```

Build: zero warnings/errors, 2.74 seconds. Final exact suites:

| Suite | Passed | Failed | Runtime |
| --- | ---: | ---: | ---: |
| Scoped providers, pure commits, durable inventory owners | 74 | 0 | 137 ms |
| Existing haul phases, orphan saves, provider faults | 90 | 0 | 271 ms |

**164 checks / two exact named suites**, zero aborted suites. No broad/full test run.
Only those two suites were rerun after repairs; earlier topology/recovery/creation suites compiled,
but were not executed in this slice.

The scoped proof covers entity→pool, continuous resource admission, pool→pool, pool→entity, exact
conservation, native/shared-capacity refusal, wrong scope/token, repeat refusal, private history,
save with live receipts, restored execution, deterministic twin continuation, independent sessions,
invalid composition, resolver identity violation and endpoint identity codec validation.

The first haul run exposed outdated blind deletion in its missing-participant fixture. The production
removal guard was preserved. Tests now prove normal active-claim deletion refuses, then use an explicitly
internal fault injection to exercise missing-participant cleanup. This is not a gameplay deletion bypass.
The first reserved-scope fixture also narrowed its deliberately invalid entity-scope IDs so composition
scope rejection could be tested separately from ID-range validation. Both final suites pass.

## Still open

Actual LineService components still own raw network pool lists, and network Flow still uses primitive
Take/Receive. A2b must replace those lists with the shared owner, persist stable component IDs/versions,
protect live receipts across topology changes, and move flow/redistribution through real transactions.
Neutral public-pool actor ownership and timed network journal proofs are not implemented here.

No generic lifecycle grant to non-entity pools, utility solver, automation, incident propagation, activity
DAG, dock/tool participation, allocation/stress gate or full accounting closure is claimed. No oil,
Tycoon, demo, harness gameplay, Unity, Godot, Draw or adapter changes. The development-lifecycle skill
guided the explicit dependent-slice contract, ownership replacement and bounded evidence.
