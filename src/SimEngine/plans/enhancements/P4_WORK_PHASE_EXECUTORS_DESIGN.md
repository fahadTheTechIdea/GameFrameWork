# P4-G3b — Stable contract-driven physical work phases

## 0. Problem statement

`Standard/Work.cs:324` closes job phases over `ToilKind`; `RunJob` selects five physical algorithms
with an enum switch. Arbitrary activity graphs alone do not remove this executor bottleneck.
This is a direct migration of the existing job owner, not another scheduler or compatibility layer.

## 1. Goals

1. Replace enum phase identity with stable `Code` identities in actual Toil records and their Sync.
2. Dispatch through declared `IWorkPhaseExecutor` service providers; reject duplicate/empty identities
   before Initialize and unknown saved phases, without an implicit native fallback.
3. Keep existing native travel, productive work, progressive pickup/delivery, physical departure,
   policy waits, receipts and progress with the same WorkSystem/WorkBoard/resource owners.
4. Give providers a single-operation, engine-issued invocation that expires after execution.
5. Verify normal master-authored physical work, interruption and saved continuation in ONE new
   bounded scenario. This does not close authored node executor selection or full G3.

## 2. Design

`Standard/WorkPhaseExecutors.cs` defines `IWorkPhaseExecutor`, `IWorkPhaseCatalog`, stable native
`WorkPhases` identities and `WorkPhaseContext`. WorkModule declares the five native providers.
WorkSystem aliases IWorkPhaseCatalog, captures alias-deduplicated ServiceContainer providers during
composition validation, and publishes sorted immutable IDs. Its derived lookup contains no progress,
claims or provider state; service/module declarations remain the composition authority.

The engine invocation carries the actual session/job/worker/target/current Toil. Its five operations
delegate to the same native physical algorithms. It permits at most one operation, checks live
identity/current phase, and expires in finally. A retained context cannot advance another frame.
No host callback is required. Providers introducing state must declare their normal persistence;
arbitrary provider purity/fault matrices remain an explicit later gate.

Move switch bodies to named physical methods in the same partial WorkSystem. Native providers select
those methods by contract, not by enum/type switches. Re-approach recursion remains native travel
behavior. Native haul/activity itinerary validation remains strict; this foundation does NOT yet allow
an authored activity to introduce arbitrary itineraries. Subsequent G3 node executors must own that
validation through their contract and retire the fixed node-to-itinerary constructor in place.

`Toil.Executor` saves the stable ID as a string; every write/read verifies it against the actual
composed catalog. Native logistics/dock/state consumers use the same native identity constants.
Board schema becomes 43, save138, engine2.238.0; WorkSystem41 and WorkView78 retain their payloads.
No old byte reader, enum, fallback or compatibility alias survives.

## 3. API surface

`IWorkPhaseExecutor.Id/Execute(WorkPhaseContext)`; `IWorkPhaseCatalog.Ids/Contains(Code)`;
`Toil.Executor: Code`. These are engine module contracts, not player/platform gameplay APIs.
Native factories remain explicit native physical operations. JSON node `executor` and typed duration
remain unfinished G3 work; current JSON behavior is not silently reinterpreted.

## 4. Files to change

Work/WorkScheduling/WorkDocks/Logistics/WorkModule/SaveGame; new WorkPhaseExecutors; focused native
proof and Program registration; three existing phase API consumers and historical identity/save
assertions migrate and compile ONLY. Stage/master/AGENTS/evidence inventory accompany the checkpoint.

## 5. Errors

Empty/duplicate provider ID => ContentException before Initialize. Unknown saved phase =>
CorruptStateException. Expired/reused/foreign invocation => InvalidOperationException. Existing
physical/policy/material refusals retain their actual typed blockers and never fabricate completion.

## 6. Compatibility

Development-only direct breaking migration; no old reader or legacy dispatch.

## 7. Verification

One build after source completion; ONE distinct exact new bounded case. Repair/rerun only observed
failures. Record all invocations. Older compiled tests are not runtime evidence. Freeze source/tests/
content after green, then docs-only scoped hash/link inventory and diff check. No broad/full gates.

## 8. Risks

Native itinerary checks still limit mod phase authorship until the node executor migration. A provider
can violate other world contracts; invocation bounds are not a global mod sandbox. Native hauling,
all dock geometries and corruption matrices need later focused gates; no broad parity claim here.

## 9. Out of scope

Node executor JSON/typed duration, arbitrary custom itineraries, team work, full replay/stress/CPU,
OilFieldDays, business changes, hosts/demos/Draw/platforms. One unchanged GameClock. Entire P1–P12
goal remains active; commit/push only after FULL completion and verification.

## 10. Execution order

- [x] Contract identities/composition/invocation and native method dispatch.
- [x] Direct phase persistence/consumer migration; bounded player proof and compile-only metadata.
- [x] Exact focused gate and truthful evidence/tracker/manifest handoff.

Verified2026-10-07: TWO builds (11 errors/0 warnings1.96s, then clean4.93s); ONE exact NEW bounded
case42 checks0 failures/aborts388ms first runtime attempt.110 older files/182 identity/save lines,
three older phase API consumers and previous board-schema assertion compile ONLY. Source/tests/
content frozen after green. [Evidence](../../docs/migration/P4_WORK_PHASE_EXECUTORS_VERIFICATION.md).
G3 as a whole remains OPEN; this does not prove authored custom phase execution or all native phases.
