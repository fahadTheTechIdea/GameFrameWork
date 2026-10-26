# P10-A2c — exact elapsed-time network port flow

## Current M136 — original-owner observation at game-update phases

Native2.767/save161/replay2. NetworkService alone owns native flow timing and its
transient execution marker. Observation fingerprints use the actual next interval
only while flow or the same game Update is in progress. Normal saves/hashes/loads
retain settled timestamp/order/quantity/endpoint checks and interrupted Flow cannot
checkpoint. No persisted phase or second clock. The existing rate factory optionally
selects a master-declared read-only event listener through the generic ProbeModule;
it performs no gameplay writes. New2904/0 and affected Contracts68/0/physical45/0,
plus actual topology/spill caller and budget qualification, retain precise scopes.
[Current self-review, failures and next work](../../docs/migration/P7_TOPOLOGY_ACCOUNTING_REVALIDATION_VERIFICATION.md#current-m136-native-flow-observation-and-physical-contention--2026-10-23).
Broader P7.017/P10.022 and whole goal remain open; no full/family/profile/soak runs.

## Historical implementation record

Status: historical native slice focused-verified on2026-10-06, build2.126.0/save69/LineService3/view4;
not rerun. Current [A4 solvers](P10_NETWORK_SOLVERS_DESIGN.md) adds required JSON solver selection
on2.128.0/save70/service4/view4;114 focused solver checks do not rerun the rate suite.
Preceding [A2d arbitration](P10_NETWORK_ARBITRATION_DESIGN.md) adds explicit open flow_policy
and component-owned successful history on2.127.0/save70/service4. It replaces tick-alias rotation
below;78 checks/two focused suites prove native same-group contention and saves, not broader global
fairness or rerun of the entire historical rate suite.
Clean build;86 checks/two exact suites (41 contracts;45 physical), zero failures/aborts.
[Evidence](../../docs/migration/P10_NETWORK_RATE_VERIFICATION.md). Previous suites compile only.
No full P10/P1-P12 completion claim. Lifecycle guidance kept the existing owners and scoped gates.

## Problem and goals

Networks.cs Flow rounds one scalar hourly allowance independently every ten minutes. Rates below
the combined endpoint quantum disappear forever. This also conflates native resource units, forces
a ten-minute session schedule and permits a pull to use goods pushed during the same interval.
Replace that path, not preserve it behind another solver or compatibility reader.

1. Master-listed networks author per-resource `rates` through existing ResourceRateSpec/IResourceRate.
   Each quantity is native and each period explicitly rational. Carried resources derive from rates;
   no duplicate resources list or scalar port_rate survives.
2. Flow runs in the existing LineSystem Update phase, once per actual session quantum. Connected,
   gate-ready, stocked/capacity-ready exact-quantum legs accrue elapsed eligibility. Sub-quantum
   time earns eventual whole transfers; disconnected/blocked periods earn nothing or catch-up.
3. Capture all leg readiness before any transfer. Goods newly pushed this Update cannot satisfy
   a pull's past-time eligibility. This historical slice retained deterministic clock-residue
   rotation. A2d now replaces it through an explicit policy contract; native same-group successful
   round-robin is focused-proven. Broader contention fairness/priority remain open.
4. Existing LineService owns sparse per-network entity/direction/resource elapsed cursors alongside
   its graph. Source/destination identity and combined quantum bind each cursor. Rebinding or a
   readiness gap starts a new interval. Cumulative quantization uses QuantizedGameRates only;
   no separate credit quantity, stock, promise, solver, scheduler or rate arithmetic owner.
5. Actual stock still moves only through verified owned resource transactions. Unused whole-step
   allowance is not banked into a later burst. Overflow returns a typed range blocker, not fake flow.

## Contracts and durability

Keep INetworkPorts/INetworkPortsDefinition as the provider seams, including independent providers.
Reject duplicate network connections and push/pull resources during common content resolution.
NetworkDef.Rates exposes existing typed immutable resource rates. Detached NetworkFlowSnapshot
exposes actual binding, elapsed eligible ticks and next quantized allowance through InfrastructureView;
private company flow history never becomes public merely because its network is public.

LineService schema3 embeds elapsed cursors in its existing Sync. Validate stable order, actual authored
resource, endpoint identities, positive quantum and bounded past-time cursors. Save69/build2.126.0,
infrastructure view4; other owner payloads unchanged. No previous save/content reader.

## Implementation sequence and verification

Revise NetworkDef/common port validation; extend the sole quantizer for rates whose authored quantity
is smaller than a receiver's exact batch quantum (native-rate callers retain their own unit checks).
Revise existing Flow/Sync and phase; expose detached private facts; migrate explicit JSON and prior
assertions; author tests using only engine execution contracts and Update. Record current evidence
and align parent/master/AGENTS/README/help.

Only two complete exact suites, including repair reruns:

- `Network rates: authored native periods, contracts and private progress`
- `Network rates: slow physical legs, blocked intervals and saved continuation`

Prove whole/discrete and fluid rates, different native quantities/periods, combined receiver quantum,
no same-step feed-through, pauses, gates, stock/capacity gaps, rebinding, native/independent ports,
physical journal conservation and saved interleaved identity. Earlier suites migrate/compile only.
No utility solver, generic spatial corridor, utility allocation, global rollback/replay/stress/full
regression or roadmap-completion claim is included.

The network's EveryTick schedule accepts a ninety-second quantum without converting ten minutes.
Actual mixed-scenario execution is proven with a five-minute quantum. A ninety-second mixed scenario
refuses composition at the unrelated LogisticsMaintenanceSystem exact five-minute schedule; that
owner is unchanged, not weakened to manufacture an arbitrary-quantum acceptance claim.
