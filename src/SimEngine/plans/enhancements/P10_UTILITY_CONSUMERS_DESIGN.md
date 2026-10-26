# P10-A5 — contract-based resource utility consumers

Status: native focused slice verified,2026-10-06. Build2.141.0/save76/operations view9;
stateless utility signal schema1. Clean build;146 checks/two exact running-input suites
(75 contracts;71 execution), zero final failures/aborts.
[Evidence](../../docs/migration/P10_UTILITY_CONSUMERS_VERIFICATION.md). Full P10/P1-P12 remains open.

## 0. Problem and acceptance

Running inputs already physically consume native resources through Transformer and the transaction
owner, but their contract/facts are transformation-specific. A game cannot publish generic utility
demand/satisfaction without inventing another balance/controller. Resource utility supply is already
physical network/logistics delivery into the actual endpoint; do not replace it with free allocations.

1. Replace ITransformerOperatingInputs with named IUtilityConsumer, and replace its detached model
   with UtilityDemandSnapshot. No old contract/model/constructor alias survives. UtilityStore and
   ReadUtilityDemand(World) expose the existing provider and facts, never another consumption store.
2. Snapshot has explicit current resource identities, next native input vector, currently available
   contribution, actual cumulative consumed quantities, endpoint/version/elapsed running work, declared
   drawing state and existing range blocker. Satisfaction/shortage is derived, not saved separately.
3. Native Transformer implements the new contract over its existing rates/progress/stock. Reading
   neither acquires utility claims nor advances work/time. Whole-set execution remains the existing
   transaction commit; reservations, shortages, pause, output wait and cancel preserve those semantics.
4. Existing private OperationsView reads named consumers with pure guarded provider validation.
   Optional JSON utility_signal binds an exact consumer and publishes utility.ready plus requested,
   available and consumed native-resource metrics through existing signal/unit/rule/gate infrastructure.
5. Public-only independent consumer can implement the same contracts without touching coordinators.
   Missing/foreign/mutating/malformed consumers and invalid JSON refuse explicitly. Inactive demand
   is typed unavailable for signals, never fabricated satisfaction. Real shortages publish false
   readiness and numeric quantities; cumulative consumed is not mislabelled as current delivery.

## 1. Owners and staged implementation

Standard/UtilityConsumers.cs owns the generic contract, renamed immutable fact and pure validator.
TransformerOperatingInputs.cs retains actual rate/consumption implementation and sole state. Existing
TransformationContracts/TransformerFlow/manager/view references migrate in place. No other work loop,
allocation queue, inventory, reservation book, clock or utility codec is added.

Standard/UtilitySignals.cs declares optional stateless signal metadata/provider/module. It uses the
shared exact native quantity conversion from ResourceQuantitySignals, not a duplicate unit converter.
OperationsView adds named read and immutable ID discovery; its feature schema increments. Persistent
payloads/save format stay unchanged; exact build/content fingerprints refuse preceding content/builds.

1. Replace contract/model references; extend sole facts and native pure capture/validation.
2. Register optional stateless JSON source; add private existing-view discovery/read and shared conversion.
3. Extend exactly the two running-input suites with actual native/public consumer facts, shortages,
   multiple resources, promises, disabled/no-demand, units, pure/fault/private/save and physical supply.
4. Clean build then those two exact complete suites only; other suites compile only.
5. Update source-scoped evidence and master/parent/AGENTS/README/help.

## 2. Content/API and errors

utility_signal component: explicit id and consumer capability. Signals/resource_signals explicitly
name utility_signal/schema1 and their boolean/native quantity metrics and units. Master-manifest
module selection/document ownership is required; no discovery/default provider is injected.

UtilityContracts guards actual named provider/entity/endpoint identity and versions, actual catalog
resource IDs/native quanta and availability, bounds and current elapsed time. Invalid provider facts
fault. Missing consumer returns normal NotFound through the view; inactive demand has utility.inactive;
actual missing quantities have utility.shortage; exact conversion retains shared typed blockers.

## 3. Remaining scope

Follow-on [A6 authored network demand](P10_NETWORK_DEMAND_DESIGN.md) now implements native
cross-network resource delivery using this same consumer and the existing physical flow owner.
Its182/two network-solver gate does not rerun the146/two running-input gate below.

This stage completes generic resource-consumer demand/satisfaction, not electrical/thermal/pressure
physics, cross-network allocation policies or graph-independent providers. Utility work requirements,
priority editing, broader provider/global rollback/replay/stress/allocations/full P10/P1-P12 stay open.
No host/demo/Oil/Tycoon/platform/Draw changes, separate clock or compatibility path.

## 4. Completed tasks

- [x] Removed old consumer contract/model names; native and public callers use the one utility fact.
- [x] Pure named private consumer discovery/read validates actual endpoint/availability/units/identity.
- [x] Optional explicit JSON signal bridge reuses exact quantity conversion; no update/state owner added.
- [x] Native/public-only demand, partial/promise/mixed shortages, inactive/range/private/fault/unit/pure
  facts and saved isolated observations pass. Independent consumer is a stateless contract proof, not
  a claim of independent physical consumption. Native execution is the physical consumption proof.
- [x] Whole physical startup/running supply, timed consumption, controls/gates/cancel/output waits,
  shortage/claim release/saved twins/one-clock pause pass in the two complete affected suites.
- [x] Final clean build precedes the exact focused gate; source-scoped evidence/docs/tracker align.
- [ ] Generic utility work requirements and cross-network allocation/priority editing.
- [ ] Electrical/thermal/pressure physics and graph-independent utility providers.
- [ ] Broader provider/global rollback/replay/stress/allocations/full P10/P1-P12 acceptance.
