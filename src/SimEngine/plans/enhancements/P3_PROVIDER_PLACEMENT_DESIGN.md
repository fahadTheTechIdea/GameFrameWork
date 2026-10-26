# P3-S9 — provider-owned replacement placement

## Scope and ownership

Extend the selected `ISpatialSpace` with optional `ISpatialPlacement`. An immutable
`SpatialPlacement` is an ephemeral intent, not a position store. The provider captures a live
entity's placement, checks the target definition against its own geometry (ignoring only the
entity being replaced), and configures the new presence before the existing spawn callbacks.
Tile and independent point providers implement the same contract. Non-spatial replacement
requires neither a provider nor fabricated coordinates. Spatial/non-spatial conversion is refused.

`Upgrades` remains the sole replacement coordinator. It validates current-session identity,
provider placement, compatible component codec identities, inventory capacity/acceptance and
optional capability `IReplacementGuard` policies before changing the entity store. Shared
non-resource components carry state through their existing Sync; spatial state is placed by the
provider, never blindly copied over a changed footprint. Removed or incompatible inventories
with stock are refused. Reservations do not migrate to a new endpoint ID.

Current resource ownership follows [P7-C3b2b5a](P7_OWNED_RESERVATIONS_DESIGN.md), not a duplicate plan:
resource endpoint state is never codec-copied. Actual old-to-new stock uses owned transfers; optional
IEntityReplacementState retains auxiliary fields only. EntityStore prepares privately before ID/
geometry mutation, registers the target with the source still live, and retires the emptied source
after exact transfers. Local preparation failure leaves original geometry/identity untouched;
interrupted commit faults and cannot checkpoint. This is not a transaction/security sandbox for
arbitrary side effects inside malicious extension callbacks.

## Commands and physical execution

Post-order preview validates a declared replacement and its existing content access policy.
Completion rechecks access and replacement after releasing that order's jobs; a changed footprint
or capacity returns a typed failed-order blocker, never an instant fallback or silent stock loss.
Replacement validation occurs before the completion effect; effects must not invalidate their
own replacement prerequisites. Workers assigned elsewhere cannot be replaced while working.

## Verification and remaining work

Historical P3-S9 gate: build the standalone solution and run only two exact replacement suites: non-tile authored timed
orders/save continuation, and spatial/inventory/guard refusals plus initialization rollback.
Keep all waits bounded with fault diagnostics. No full regression gate is authorized. Current
P7-C3b2b5a runs only its two exact resource-replacement suites; its second reuses the directly affected
bounded non-tile timed proof. Neither full P3-S9 suite was separately executed on current source.

This slice does not migrate tile-authored generated construction sites, production rollout,
infrastructure or salvage. Their sole-owner migrations remain open under P3/P4/P8, rather than
introducing temporary parallel project or placement models. No oil, Tycoon, host or platform edits.
