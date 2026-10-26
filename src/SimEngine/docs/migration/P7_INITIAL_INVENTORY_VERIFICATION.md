# P7-C3b2b1 authored initial inventory — focused verification

2026-10-04. Working-tree identity **beep-simengine-2.35.0 / save 16**. Event/journal/view schemas remain
2; stock and service layouts are unchanged. Exact engine identity rejects previous simulation builds;
there is no compatibility reader. Standalone framework/proof sources only; no oil/Tycoon, platform,
adapter, renderer or host/demo gameplay implementation. The full roadmap and P7 stage remain partial.

## Ownership and creation

Kernel `IEntityInitialization.Initialize(World)` executes in authored capability order after the
new entity is registered, before World.Spawn returns. Existing OnCreate initializes local fields
before registration. Load and state-preserving replacement do not execute new-creation effects.
This is an engine lifecycle contract, not a host callback, render phase or second entity-update loop.

`IInventoryDefinition.InitialStock` exposes one immutable Bill. Storage's `initial_stock` is read from
the existing master-listed JSON. Missing stock means an empty inventory, not fabricated gameplay
resources. Generated buffers are explicitly empty. Resolution checks resource IDs, native quantity,
filters, exact policy quantum and whole-bill capacity before play; independent single-resource content
also validates native quantity/quantum. Built-in/external providers start empty, then delegate to
the existing ResourceTransactions.InitializeStock/CommitInventory owner. Actual stock remains in
the one existing provider; no second balance, creation ledger or journal exists. Proof depot stock is
authored in JSON, not raw worldgen Add; the specialized proof endpoint no longer assigns its balance
in OnCreate. Test fault fixtures author a zero-input/zero-initial-stock recipe to isolate output failure.

Only a completely verified batch publishes Initialized facts, with actual endpoint, unit/delta/version,
`sim.inventory.initial` requester and entity-definition cause. The existing startup boundary dispatches
them before tick-zero save. Runtime spawn retains deferred-event/save/hash rules. Invalid initial
provider execution faults `entity.initialize`, emits no success, refuses further Spawn/Update/save,
and never returns a healthy partial startup. No compensating stock is invented.

Replacement restores existing live inventory through its shared codec, ignoring target starting grants;
load restores stock/history without reinitialization. This verifies no extra creation, **not** complete
relocation/removal accounting. Company privacy, detached views and bounded retained history are unchanged.
Retained opening facts are not whole-session accounting and still expire explicitly.

## Exact verification

Only these two full suite names were selected; each invocation runs one suite:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Initial inventory: authored creation, private checkpoints and replacement"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Resource lifecycle: authored timed production, work and provider violations"
```

Build: exit 0, zero warnings/errors, MSBuild **1.78 s**. Focused proof:
**69 + 39 = 108 checks / two exact suites**, zero failures and zero aborted suites.
Final suite times: creation **157 ms**, timed production/work **152 ms**; both exits 0.
Creation covers mixed-native bills, content refusals, immutable definitions, public external capability,
private opening history, tick-zero/runtime checkpoints, no load/upgrade grants, empty creation,
optional diagnostics, interleaved sessions and hostile initial providers. Timed proof retains bounded
30-update waits, saved production/work continuation, competing stock promises and partial provider faults.

The first creation run failed before checks because the test request omitted its second scenario seat.
The fixture request now explicitly selects both human seats; the same suite passed after repair.
No engine default seat behavior was changed. Only the same two suites were used for verification.

Older flow assertions were source-migrated for opening facts, actual sequence IDs and runtime event
checkpoint boundaries. They compile but were **not rerun**. The prior 2.34 batch suite and prior broader
proofs likewise have no current full-regression claim. No broad/family/full test run was performed.

## Remaining work

Despawn/salvage/raw/network callers, cargo disposition and complete relocation/conversion/spill/topology
accounting are open. The coordinator does not automatically observe arbitrary Add/Remove or malicious
mod code. Generic multi-endpoint/tool/dock/DAG/transform integration, stress/allocation and authorized
stage/full regression gates remain open. This completes C3b2b1 only, not C3b2b or the overall plan.

The development-lifecycle skill guided the execution contract, single-owner migration and source-specific
tracker/evidence; repository/user instructions restrict verification to these exact affected suites.
