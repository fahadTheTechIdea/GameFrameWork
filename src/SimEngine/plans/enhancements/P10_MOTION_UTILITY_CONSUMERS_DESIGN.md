# P10-A8 — Motion operating inputs as utility consumers

Status: focused verified, 2026-10-06. Clean build2.144.0/save76;169 checks/two exact motion suites
(96 admission;73 physical), zero failures/aborts. [Evidence](../../docs/migration/P10_MOTION_UTILITY_CONSUMERS_VERIFICATION.md).
Full P10 and P1–P12 remain open.

Follow-on [A9 shared-stock utility demand groups](P10_UTILITY_DEMAND_GROUPS_DESIGN.md) now supplies
combined native motion inputs without counting shared stock per consumer. Its network gate does not
rerun this historical motion gate or complete arbitrary global utility arbitration.

## 0. Problem statement

`Standard/MotionOperatingInputs.cs` already owns authored active-attempt resource calculations,
exposure admission and one whole-set transaction. Its separate resource snapshot prevents generic
utility views, signals, network demand and activity requirements from observing those same facts.

## 1. Goals

1. Native and public-only motion input providers implement the existing `IUtilityConsumer` contract.
2. One calculation uses actual authored rates, exposure, session time and native inventory quantum.
3. Motion snapshots embed generic utility facts, not a second resource-demand model.
4. Actual movement continues to commit one aggregate resource draw and exact exposure changes.
5. Generic private views/signals agree with physical waiting, resupply and saved continuation.

## 2. Design

Extend `IMotionOperatingInputs : IUtilityConsumer`. Add public stateless
`MotionOperatingContracts.ReadDemand(World, IMotionOperatingInputs)` so independently registered
providers reuse the engine calculation rather than implementing fuel arithmetic. Native and proof
providers expose their actual existing `IInventory`; no additional inventory or consumption state.

Replace `MotionOperatingSnapshot` with `MotionUtilitySnapshot(Exposure, Demand)`. Exposure retains
the last accepted game-clock coordinate needed for exact commit. `UtilityDemandSnapshot` alone
contains named identity, endpoint/version, resources, consumed/next/available quantities and blockers.
Aggregate motion admission still checks the complete draw, including competing named providers.
Provider demand must match the actual authored motion rates and exposure before movement can commit.

Motion utility demand is readiness for the **next accepted active attempt**, including while idle;
it is not evidence of current motion, another clock, predicted distance, or guaranteed future fuel.
`Drawing` denotes an authored input obligation. Explicit empty rates expose inactive utility demand
and still permit non-consuming movement. Disabled, idle, zero-rate, unreachable and shortage behavior
remains governed by existing movement execution. Reads, supply and pauses never advance exposure.

## 3. API surface

The existing private `OperationsView` discovers/reads motion consumers; existing explicit JSON
`utility_signal` binds their actual names. Existing utility requirements/network policies need no
coordinator switch. Remove old motion-only resource properties/type, without aliases.

## 4. Files

MotionOperatingInputs.cs; UtilityConsumers.cs documentation; MotionModule.cs; existing public-only
MotionOperatingProof.cs/PointSpaceProof.cs; MotionOperatingTests.cs and its embedded master-listed
utility signal JSON; current build identity/assertions; stage/master/parent/evidence/README/help/AGENTS.

## 5. Errors

Foreign/missing providers, metadata, endpoint or inventory identities and malformed/mutating reads
refuse explicitly. Authored next-draw overflow remains `motion.operating-range`. Generic shortage
uses `utility.shortage`; aggregate motion shortage remains `motion.operating-inputs`. Native quantity
and promise guards are reused. Faulting providers do not imply general transactional rollback.

## 6. Compatibility

None. Native/public motion component semantic schema2 and motion view5 replace earlier identities.
Exposure payload remains two longs; save76 and movement5/work13 remain unchanged. Build2.144.0.

## 7. Verification

Clean `dotnet build Beep.SimEngine.slnx`, then only these two exact existing suites:

- Motion operating inputs: authored contracts and whole resource admission
- Motion operating inputs: timed expenditure, physical resupply and saved continuation

Build failure stops tests. Physical stages remain bounded. No third/family/full regression gate.

## 8. Risks

Per-provider readiness can be true while their combined draw exceeds shared stock; preserve whole-set
admission and test this distinction. Ceiling prepayment can make a next bill empty while resources
remain declared; do not fabricate consumption or inactive state. Scoped purity is not a security sandbox.

## 9. Non-goals

No extra clock, game loop, fuel balance, auto-refuel scheduler, platform/Draw, Oil/Tycoon/host/demo
gameplay, distance/idle consumption, electric physics, global priority, full replay/stress acceptance.

## 10. Execution

- [x] A8.1 unify contracts/calculation and actual physical admission
- [x] A8.2 public extension/JSON/private generic views/signals and focused proof
- [x] A8.3 clean build/two exact suites and evidence/master/help alignment
