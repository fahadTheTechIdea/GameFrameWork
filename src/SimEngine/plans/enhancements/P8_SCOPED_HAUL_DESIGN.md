# P8-D5a — physical hauling through exact named resource ports

## Scope and ownership

Extend the existing `IHaulExecution`/LogisticsService and WorkBoard path. A physical pickup or
delivery site remains an entity for navigation, operating areas and docks, but its selected named
`IResourcePort` may resolve an actual scoped store. No new inventory, reservation book, scheduler,
clock, transfer algorithm or host gameplay is introduced. Normal manifests author the ports,
stores, vehicles, handling rates and scenario setup in JSON.

The explicit unnamed port selects the entity's inlet/outlet capability. A specified named port
never falls back to that inventory. Purchases continue to use the existing supplier contract;
this slice must not invent scoped purchase/settlement semantics.

## Implementation stages

1. Extend the existing haul contract/preview and add an ordinary player haul command. Admission
   resolves the named providers and applies physical-site ownership, export policy, native quantity,
   actual carrier eligibility, cargo capacity, travel and handling preflights. Preview is pure.
2. Store the two named-port identities on the original Job. Actual source/destination receipt
   endpoint IDs pin the original stores throughout the haul, even when current selection changes.
   Native non-purchase claims use the existing whole-set transaction owner before travel starts.
3. Pickup, earned handling, unloading, receipt validation and cancellation all resolve those same
   exact original stores. Surviving claims are released through the existing resource transaction
   owner; a missing/substituted port must not redirect stock or promises.
4. Migrate the current save/build/view/work schema identities without an old reader. Add ONE NEW
   bounded thin-driver case covering real travel/loading/delivery, explicit selection changes,
   saved continuation, private refusal and native cancellation/accounting.

## Open follow-on work

Automatic scoped supply demands, standing routes/exports and stock-policy management are separate
subsequent migrations of the same existing owners. This manual physical-port slice does not claim
those complete, nor generalize entity cargo to service-owned cargo, nor close provider/fault/stress,
joint machine-resource admission, P8, P1-P12 or the active goal. Commit/push remains gated on FULL
goal and plan completion. No old/family/full/Oil/platform runtime tests are authorized by this stage.
