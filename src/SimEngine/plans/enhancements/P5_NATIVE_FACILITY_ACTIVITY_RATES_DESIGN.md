# P5-A31: Native facility-supported activity execution

## 0. Problem

Removing Oil's point scheduler requires native activities to receive capacity from multiple operating
facilities. Existing timed nodes had a fixed rate; worker nodes had one assigned machine. A new scheduler
or copied research progress would violate the single-owner boundary.

## 1. Goals

1. JSON and contracts declare named facility rate pools, sources and allocation.
2. Existing WorkBoard owns all execution and prevents counting a pool's capacity once per consumer.
3. Existing controls, signal gates, entity Update/state and codecs own source availability.
4. Native technology completion and saved progress work without money or any Oil gameplay system.

## 2. Design

`ICompanyActivityRateDefinition`/`ICompanyActivityRateSource` describe source metadata and pure
capacity facts. `ICompanyActivityExecutor` describes the pool selected by an authored node.
`CompanyActivityRateDef`/`CompanyActivityRateSource` specialize the existing ControlledExecution
reader, gates, controls, state resolver and codec. There is no second enabled-state or condition model.

WorkModule registers `company_activity_rate` component schema1 and `company_activity_executors`.
Each executor declares `pool` and explicit `allocation: "equal"`. Sources declare positive nominal
`rate`, pool and the existing operation/conditions/disabled-state metadata. No guessed rate/allocation.

The existing WorkBoard captures pool rates after plan promotion, before productive execution in its
original WorkSystem update. Owned, live, ungated sources contribute; foreign sources do not. Capacity
is divided equally among dependency-ready, live, unpaused plan nodes in that pool/company. Requirement-
blocked consumers conservatively retain a share; this does not allocate their unused capacity twice.
Allocation is fixed for the update, including completion/cancellation effects; the next update observes
changed consumers and sources. This prevents within-update count changes from oversubscribing time.

The pool map authorizes exact order/node identities captured at update start: newly dependency-ready
nodes cannot borrow another consumer's share. Pool observations use company then ordinal pool ordering.
This map is derived update-local data on WorkBoard, cleared in WorkSystem's finally block. It owns
no saved resource, progress, work assignment or knowledge. Original WorkProgress, productive admission,
requirements, cancellation, clock and TechnologyService/CompanyKnowledge completion remain authoritative.
Q32 division can leave a fractional remainder unused; capacity is never rounded up.

## 3. Contracts and JSON

```json
{"company_activity_executors":[{"id":"research.capacity","pool":"research","allocation":"equal"}]}
```

A normal entity's `company_activity_rate` component:

```json
{"pool":"research","rate":12,"operation":{"id":"research-rate","enabled":true,
 "target":1,"minimum":0,"maximum":2,"unit":"unit.work-rate"},
 "conditions":[],"states":{"disabled":"stopped"}}
```

An ordinary native technology/activity node uses that executor, explicit empty roles, and an authored
duration. Requirements, dependencies, material costs and final effects remain the existing activity DAG.
The nominal rate is a dimensionless work multiplier; authors choose durations to express actual throughput.

## 4. Files

- `Standard/CompanyActivityRates.cs`: source/executor contracts and original WorkBoard capacity slice.
- `Standard/WorkModule.cs`: normal JSON registrations.
- `Standard/Work.cs`: original update observation lifetime, cleanup on faults.
- `Kernel/SaveGame.cs`: engine build2.548→2.549, save format156 retained.
- Existing authored protocol test metadata: mechanical build identity update, not a runtime baseline run.
- Oil `OilCo.PublicProof/CompanyActivityRateTests.cs`/`Program.cs`: public native-engine proof only.

## 5. Refusals

Unknown allocation, missing selected pool source and nonpositive rates refuse before play. Mismatched
runtime/metadata/named aliases and malformed/negative source facts refuse through existing observation
guards. No source capacity produces no progress, not a fallback timer. Independent provider fault breadth
is not yet proven. Callback exceptions preserve the original first-fault boundary.

## 6. Compatibility

No compatibility path. New facility control codec1 reuses the original generic implementation;
existing WorkBoard, WorkSystem and knowledge codec schemas do not change. Engine build2.549.0 prevents
old saves/peers from claiming identical simulation semantics.

## 7. Verification

One exact native public case FINAL44/0,433ms; bounded observer563ms, no truncation/abort.
Core/proof builds clean. [Detailed attempts and boundaries](../../docs/migration/P5_NATIVE_FACILITY_ACTIVITY_RATES_VERIFICATION.md).
No full/family regression. Original221/194recorded checked/27open unchanged.

## 8. Risks

Equal sharing is explicitly authored, not an emergency/priority allocator. Blocked shares may reduce
utilization. Dynamic providers, faults, power/fuel/material supply, destroyed facilities, large worlds,
non-tile worlds and complicated parallel graphs need additional focused proof. No new world-size limit.

## 9. Out of scope

Oil lab/point scheduler migration, Oil fees or modifiers, full physical construction, Tycoon, platform,
Draw/adapters/host gameplay and full industrial/durability acceptance are not complete here.

## 10. Stages

- [x] Add generic JSON facility source/executor contracts on existing operation/work owners.
- [x] Prove equal sharing, source controls, cancellation, saved progress and native research completion.
- [x] Migrate Oil technology JSON into existing TechnologyDeclaration/ActivityDeclaration, bind lab
  operation conditions, and remove its point scheduler/active-progress owner in favor of WorkBoard.
- [ ] Preserve optional authored research charges and domain effects; prove the actual Oil scenario.

P5-A32 completes the structural migration and focused fee/plan proof. Canonical constructed/powered
lab and full domain-effect/industrial parity remain open; see [migration](P5_NATIVE_OIL_RESEARCH_MIGRATION_DESIGN.md).

Use dev-lifecycle-standards for staged design/evidence/tracker; user's focused-only verification rule
overrides the skill's generic full-regression guidance. Full goal and plan remain open.
