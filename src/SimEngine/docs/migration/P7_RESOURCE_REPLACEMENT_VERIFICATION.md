# Entity replacement and resource ownership — focused verification

2026-10-05. Working-tree **beep-simengine-2.40.0 / save 18**.
P7-C3b2b5a is focused-verified. P7, entity-reference migration and the overall roadmap remain partial.

## Authoritative implementation

Upgrades remains the sole replacement coordinator. Resource endpoint components, including inventory
and supplier books, are excluded from component-byte state carrying. Actual stock travels through
existing owned receipts and ResourceTransactions. Successful legs emit existing StockTransfer facts
with sim.replacement requester and real old/new endpoint identities, amounts, units and versions.
No fake destruction/production/initialization pair, second inventory or replacement journal exists.

EntityStore privately prepares the target while the original remains live and owns its geometry.
Configuration and local OnCreate run, but IEntityInitialization never runs, so target starting stock
is not granted. Preparation/normal admission refusal consumes no entity ID or world geometry.
After current-state preflight, the same operation releases source geometry, claims target geometry,
registers the actual empty target, transfers stock with both endpoints resolvable, and retires the
empty source. No Update or event dispatch runs between those steps. Spatial rules belong to the
selected provider, whether non-spatial, point or tile; no tile fallback enters the coordinator.

Preflight covers active port and supplier promises, busy workers, provider geometry/replacement guards,
non-resource removal guards, native stock, complete target capacity/filters/quantum, actual empty
target state, entity endpoint/company metadata and pure receipt admission. Resource capability/provider
types or component keys may differ where the authored receiving inventory definition admits the whole
stock vector. No concrete Storage/SingleResourcePort dependency exists in engine replacement logic.
Final validation requires the exact target vector, empty source and consumed promises before retirement.
Catalog-based source reading also handles ports without IInventory; the executed source proofs use
built-in and independently compiled IInventory providers. Receiving capacity still requires the authored
IInventoryDefinition contract; this slice does not prove every possible inlet-only extension shape.

IEntityReplacementState is an optional multi-part engine hook for provider-owned auxiliary state.
It is not a second codec or capability-slot authority. A provider may retain service/calibration facts,
but may not change its source, stock or promises. Matching non-resource components otherwise retain
their existing compatible codec. Resource components without the hook retain target-local auxiliary
state, never the old endpoint's book or balance bytes. Each live provider still uses its one normal
Sync for save/load/hash. The independent SingleResourcePort proof retains its service counter and uses
component schema 4 in every normal proof registration; no old reader is added. Runtime stock/receipt
payload layout is unchanged, so save format remains 18 while the engine build changes.

An interrupted committed replacement or provider preparation violation faults entity.replace.
EntityStore keeps its incomplete-operation checkpoint guard; World.Sync also refuses every faulted
write/hash, matching the existing save refusal. No balance/geometry compensation or resumable partial
checkpoint is fabricated. Local target initialization failure before commit leaves the original healthy.
Future arbitrary CLR side-effect/security isolation is not claimed: providers are trusted engine modules.

## Commands and final evidence

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- 'Resource replacement: native stock, real receipts, private history and saved identity'
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- 'Resource replacement: timed geometry, auxiliary state, refusals and fault checkpoints'
```

Final build: zero warnings/errors, **1.88 seconds**. Final exact suites:

| Exact suite | Passed | Failed | Runtime |
| --- | ---: | ---: | ---: |
| Native stock, real receipts, private history and saved identity | 78 | 0 | 156 ms |
| Timed geometry, auxiliary state, refusals and fault checkpoints | 112 | 0 | 246 ms |

**190 checks / two exact named suites**, zero aborted suites. Only these two names were executed;
the second reuses the directly affected bounded ProviderPlacementTests.Timed method, not a broad
family filter or the full provider-placement suites. The new master-listed resource-replacement.json
uses the existing PointGame factory/points.json manifest entry. Tile proofs reuse Colony.Factory.
There is no second factory, scheduler, world state model or host-side game rule.

The proof covers fractional native mass and count stock, exact receipts and emptied old owners,
target initial-grant suppression, native quantum/capacity/filter/removal refusals, both promise
directions, supplier-only promises, private detached history, changed provider/key, retained auxiliary
state, empty/no-map replacement, unchanged interleaved sessions, saved identity and continuation,
ordinary timed order execution, provider position/entity state, expanded tile occupancy, preparation
failure without ID/geometry mutation, and actual provider admission refusal before commit.

Seven authored fault probes cover retained/partial source receipts, partial receiving, mutating
preflight, absent version advance, cloned resource state and source mutation in auxiliary retention.
Their failed single resource leg creates no success fact and cannot save/hash/update further.
Initial execution exposed two hash-guard failures for preparation faults: no registered partial entity
or pending event existed to trigger the older hash guards. World.Sync's faulted-session write guard
now closes that gap, and the same two exact suites pass on final source.

Earlier InitialInventoryTests expectations were source-migrated for real relocation history; their
suite was not run. Other endpoint/network/recovery/haul suites and proof registrations compile but
were not executed. The latest full regression remains historical, not current. No oil, Tycoon, host,
demo, Unity, Godot or rendering work was done; no platform/world-size limit was introduced.

## Remaining work

C3b2b5b: incident/cargo disposition and remaining caller/resource-reference audit through their
existing owners. Company/home/operation/order references beyond the current timed-order path still
need explicit retention/rebinding policies; successful resource relocation does not prove all such
entity relationships survive a new identity. Full opening/conversion/spill/environment accounting,
tools/docks/DAG/batch integration, broader hostile/stress/allocation proofs and authorized stage gates
remain open. The lifecycle skill guided the existing-owner staged contract, implementation, tracker
and scoped verification; the user's no-full-suite instruction took precedence over its broad gate.
