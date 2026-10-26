# P10-B8 — resource backpressure from actual receiving capacity

Status: native focused slice verified,2026-10-06. Build2.140.0/save76; inventory signal3/network
signal2. Clean build;447 checks/two exact suites (264 contracts;183 execution), zero failures/aborts.
[Evidence](../../docs/migration/P10_RECEIVING_ROOM_VERIFICATION.md). Extends existing P10 owners;
full P10/P1-P12 remains open.

## 0. Problem and acceptance

Stored quantity alone does not describe backpressure in a mixed-resource endpoint. Inbound promises
and other resources can exhaust shared capacity before the selected resource reaches its threshold.
Hosts must not calculate this or invent their own process/network controller.

1. Existing ResourceQuantitySnapshot requires explicit detached CapacityOffer receiving facts.
   No old constructor, implicit unlimited room or compatibility path remains.
2. ResourceQuantitySignals captures actual inlet Offer at its native quantum, including all held
   stock and inbound commitments through the same owner. Positive offers must pass actual CheckIncoming.
   Outbound promises do not free physical room. Observations acquire nothing and advance no time.
3. Explicit JSON inventory.receiving-room/network.receiving-room metrics reuse resource_signals,
   exact units, private views and existing gate/rule admission. Inventory schema3/network schema2
   replace the old declarations. Numeric room is per resource, never a dimensionless total.
4. Known full capacity is available zero. Invalid capacity observations withhold room through a
   typed blocker but do not invalidate otherwise valid stored quantities. Malformed/mutating providers
   fault; unsupported resource/dirty/private/disconnected topology retain existing typed refusals.
5. Authored normal network gates stop physical transfer under committed capacity pressure and resume
   after actual receipt release, without banking skipped flow. Existing transformation gates and
   deferred automation can consume the same facts. Pause and saved twins use the sole GameClock.

## 1. Owners and API

Standard/ResourceQuantitySignals.cs owns the extended detached fact constructor, guarded capture
and unit conversion. Existing INetworkDiagnostics.ReadQuantity aliases LineService and returns the
same facts. Standard/InventorySignals.cs and NetworkSignals.cs add their metric declarations only.
No graph, inventory, scheduler, controller, queue, state codec or clock is added.

ResourceQuantitySnapshot(..., CapacityOffer receiving) validates native facts plus known enum,
nonnegative amount and zero amount for non-Fits status. Receiving records actual capacity status;
Fits/LimitExceeded publish the offered quantity, other known statuses withhold receiving-room.
Capture retains entity/endpoint/policy versions and checks Offer purity. Receiving room remains a
read fact, not a promise; execution rechecks current capacity through the normal receipt protocol.

## 2. Files and staged implementation

1. Extend shared contract/capture/read and both registered signal schemas.
2. Migrate explicit current JSON provider references and callers, without aliases or defaults.
3. Extend exactly the two existing resource-signal suites with mixed-capacity/native/public-only,
   reservation/release, unit/quantum/invalid/private/dirty/pure and physically gated saved proof.
4. Clean solution build, then those two exact suites only; unrelated suites compile only.
5. Record evidence and update master/parent/AGENTS/README/help in this same slice.

## 3. Verification and errors

- Resource signals: inventory and network contracts and private facts
- Resource signals: inventory and network controls and saved continuation

Malformed room/unknown status/observation mutation throws. Known unrepresentable/out-of-range
capacity has resource.signal-capacity-unavailable with no value. Incompatible output conversion
uses the existing exact quantity/range blockers. Current explicit content references must match
new schema versions before play. Build identity changes; persistent layouts/save format do not.

## 4. Non-goals and remaining scope

This is the resource backpressure branch, not electrical/thermal/pressure utility satisfaction.
Generic utility allocation, graph-independent providers, broader rollback/replay/stress/allocations
and full P10/P1-P12 remain open. No host/demo/oil/Tycoon/platform/Draw change or full suite gate.
No game-size cap, separate clock, legacy loader or parallel authority.

## 5. Completed tasks

- [x] Extended sole shared quantity capture/contract/read; actual capacity Offer and positive admission.
- [x] Explicit metric/schema/current content and constructor migrations; old declarations refuse.
- [x] Mixed native/public-only inventories, inbound/outbound and cross-resource promises, exact units,
  malformed/mutating/unrepresentable facts, private topology and saved/pure reads.
- [x] Authored network gate stops/resumes actual flow without banking; paused/saved interleaved twins.
- [x] Existing deferred rules shut/reopen actual valve from room hysteresis through normal commands.
- [x] Clean build then exactly two complete affected resource-signal suites; scoped evidence/docs.
- [ ] Electrical/thermal/pressure satisfaction and graph-independent utility providers.
- [ ] Broader provider/global rollback/replay/stress/allocations/full P10/P1-P12 acceptance.
