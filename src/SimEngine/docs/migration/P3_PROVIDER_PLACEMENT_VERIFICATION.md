# P3-S9 provider-owned replacement — scoped verification

Date: 2026-10-04. Working-tree engine build **2.20.0 / save format 11**.

## Commands and scope

```powershell
dotnet build Beep.SimEngine.slnx --no-restore -v:q
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Provider placement: non-tile timed replacement and saved continuation"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Provider placement: refusal parity, changed geometry and initialization rollback"
```

Final verification: clean solution build, zero warnings/errors; **24 + 29 = 53 checks** in these
two exact suites (110 ms and 138 ms respectively), zero failures/aborted suites. Only these same suites were retried while correcting
fixture assumptions: the shared factory intentionally shares immutable definitions, and physical
cranes occupy their real approach tiles. No other suite, full/broad filter, oil, Tycoon or platform ran.

## Established behavior

- ISpatialPlacement is an optional capability of the selected spatial authority. Its immutable,
  provider-specific requests are ephemeral intent, not a second spatial model or registry.
- TileMap's direct spawn and replacement use the same geometry check/configuration. Preview ignores
  only the replaced entity's occupied footprint; it cannot overlap other structures or machines.
  Geometry sums use 64-bit bounds checks before constructing an accepted footprint.
- Upgrades remains the sole replacement coordinator. It does not reference tile contracts, TileMap,
  concrete inventory parts or entity-kind placement branches. Non-spatial entities need no location.
- IInventoryDefinition extends the existing inlet declaration with capacity. Both built-in and
  non-friend independent inventory providers implement it; outlet still inherits inlet unchanged.
  Existing stock cannot disappear into a removed, incompatible, filtered or insufficient inventory.
- Independent JSON-authored IReplacementGuard policy runs without adding a scheduler branch.
  Shared codec identity/runtime compatibility is checked; existing Sync carries non-spatial state.
  Spatial presence is configured by the provider, not copied after claiming the target footprint.
- PostOrder preview validates replacement and existing content access. Completion rechecks after
  releasing its jobs; changed geometry yields Failed/core.blocked while preserving the original.
  An active worker cannot be replaced while assigned elsewhere. No remote work or instant fallback.
- A non-tile source upgrades only after all six authored work ticks. Live pose is retained despite a
  different prototype pose; actual stock overwrites target initial stock and uses target capacity.
  Entity-owned state settlement, active saves, completed saves and twin hashes remain consistent.
- Same-factory immutable definitions are safely shared; separately resolved equal-ID definitions and
  foreign live entity objects are refused. Spatial/non-spatial conversion is explicitly unsupported.
- A tile replacement resizes the authoritative footprint and center, claims its added area, retains
  stock and drops old-ID inventory reservations. A completed save restores matching occupancy.
- A thrown initialization probe releases temporary occupancy, restores the original live identity and
  original geometry, consumes no new ID and leaves the persisted hash unchanged. The error propagates.
  State-read exception cleanup uses that same lifecycle path but is not independently fault-injected here.

## Boundary and remaining work

Runtime/proof changes are confined to Beep.SimEngine. The non-friend proof supplies registered
engine-side contracts/content; player scenarios drive public commands/Update. Framework tests use
explicit mutable-world instrumentation only for environmental change, pure boundary checks and fault
injection, not to implement work, placement, transfer or save rules in hosts.

Build identity changes to 2.20.0; existing saved layouts and format 11 stay unchanged. Exact build
matching continues to reject old saves/peers; no compatibility loader or legacy wrapper was added.

Initialization recovery is not a transaction for arbitrary external side effects in extension
callbacks, nor a full generic resource transaction protocol. P7 and P6 retain those broader gates.
Generated construction/site authoring, production rollout, infrastructure/salvage placement,
comprehensive lifecycle/layer/privacy checks and authorized full regression remain open. No whole
P3, full roadmap, oil integration or generic 3D/rendering completion is claimed.

The lifecycle skill guided the staged design, single-owner review and source-specific tracker/report.
User/repository instructions override its generic full-regression recommendation.
