# P4-G70 — Transporter cargo binding and removal contract

## Problem and scope

IInletPort requires IEntityRemovalGuard, but ITransporter does not. A custom
transporter may use an actual cargo inventory on another entity; removing its
worker can orphan that binding while its stock/receipts remain live. The original
private LogisticsService binding reader also has bypasses in policies/views/work.

## In-place implementation stages

1. Require IEntityRemovalGuard on ITransporter. Native and independent providers
   delegate to one TransporterContracts.CheckRemoval implementation. No optional
   fallback, shim, new removal registry or extra state owner.
2. Move the original binding algorithm to TransporterContracts.ReadCargo, delete
   the private reader and migrate every engine cargo binding read in place.
   Observe actual provider/entity; refuse null, foreign, dead or nonactual cargo.
3. When cargo is the worker's own actual IInventory, its existing inventory guard
   retains ownership, including original recovery's explicit guard resolution.
   When cargo belongs to another entity, observe that inventory and refuse active
   promises or undisposed stock. Never silently recover or drop it.
4. Migrate independent BoundCargoDef to the actual ITransporterDefinition contract
   and keep its original explicit JSON binding/context/saved payload.
5. Verify one new exact bounded public-player case for native and bound cargo,
   partial pickup/delivery, waits/cancellation/packing/removal, saves/private facts,
   replay and session isolation. Repair the same case only if needed.
6. Record all attempts, freeze sources and update tracker without broad closure.

## API and ownership

ITransporter now includes CheckRemoval(World). TransporterContracts.ReadCargo and
CheckRemoval are engine-side contract helpers over actual World/entity providers;
no host mutable-world escape. World.CheckDespawn remains the sole removal owner,
WorkBoard the sole graph/job scheduler, inventory/transactions the sole receipt
owners, ConstructionService the sole physical packing owner. Explicit recovery
acts on the actual inventory entity's authored placement, never invents attachment
geometry for a separate cargo entity.

Engine2.441.0/save154/replay1; no payload or clock change. No compatibility reader,
duplicate cargo ledger/reader or native provider type branch in engine systems.

## Acceptance and limits

Loaded/promise-bound remote cargo blocks worker retirement and survives physical
interruption/removal waits. Explicit inventory recovery and cargo-emptying allow
later retirement. Native recovery can still resolve its own attached inventory
guard. Removed/cancelled workflows do not discard handling prefixes, physical
position or stock. Saved continuation/replay match actual public command streams.

No complete arbitrary provider purity, every cargo owner topology, lots/business/
incident/accounting/global/performance/stress/full P4/P7/P1–P12 claim. Existing
broader gates remain open. Protected G19/clock/Tycoon sources unchanged; no Oil,
Draw/platform or gameplay in drivers. Commit/push conditional on full plan finish.

[Evidence](../../docs/migration/P4_TRANSPORTER_REMOVAL_CONTRACT_VERIFICATION.md)

## Verified scoped checkpoint

FINAL477/0/0 in15308ms; PID37760 Exited0/process15491ms. Eight native/bound
minute/half partial-transfer physical retirement/recovery flows. All four same-case
attempts and four terminal compiles retained in evidence; no full/old/family run.

Read-only closure on 2026-10-11 14:48:14 UTC: 1425source anchors,
13quickstart anchors and 712selected executable inputs; zero source/quick/input drift.
2677workspace paths, 938Markdown files, 8496local links: zero broken links.
18master rows have valid shape; zero driver-boundary matches; git diff --check
exit0 with 34existing line-ending warnings. All221 original literal requirements and
checkboxes unchanged (178checked/43open); 204line anchors shifted6. Additional165.
Four protected clock/Tycoon hashes unchanged; 2665dirty entries, 0staged.
Fourteen previously selected executable inputs changed; two newly authored inputs
and two previously existing relevant inputs added to freeze. Ten existing production
files changed. No executable drift after FINAL; no runtime repeated by metadata
checks. Goal complete=false; nothing staged, committed or pushed.
