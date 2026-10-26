# P9-C8h — Shared physical dock queue verification

Current M71 selected repeated dock-contention/allocation proof: [two exact public
physical cases, final metrics and scope](P7_CAPACITY_LOGISTICS_STRESS_VERIFICATION.md#current-m71-repeated-finite-dock-contention--2026-10-22),
22,475/0/two replays on unchanged native2.723/save161/replay2. This is a finite
one-source/one-destination workload with native docks and two inventory providers,
not a rerun of every cross-company/shared-geometry/provider case below. Initial
C8h implementation records retain their actual historical image and limits.

Date: 2026-10-06. Framework-only scope;
[stage](../../plans/enhancements/P9_SHARED_DOCK_QUEUE_DESIGN.md).

## Source identity and exact gate

Build **beep-simengine-2.123.0**. Save67/WorkBoard23/work system13/work view39/native map6/public
point space4 and all existing owner schemas/payloads unchanged. Exact build admission excludes
preceding saves/peers; there is no old reader, compatibility overload or alternate queue path.

From the framework repository:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Shared dock queues: fair admission, eligible waiters and geometry isolation"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Shared dock queues: physical promotion and saved continuation"
```

Final build: **0 warnings / 0 errors**, MSBuild elapsed **4.68 seconds**.
Final admission suite: **48 passed / 0 failed**, 1,357 ms.
Final physical suite: **39 passed / 0 failed**, 1,164 ms.
Total: **87 checks / exactly two complete suites**, zero failures or aborted suites.

Only these names ran, including same-name repairs.56 preceding test files/56 build identity lines
migrated mechanically and compiled only; their suites were not rerun. Existing service/route/claim
fixture helpers were reused, not their gates. Scoped tracked-source whitespace checks passed;
Git emitted line-ending conversion warnings only. Pre-existing dirty source was preserved.

Repairs corrected the fixture's PlanIntent key to the actual `activity:` content identity and retained
the real worker entity ID for a later observation after completed jobs clear their assignment link.
No gameplay behavior was moved into tests or weakened to accommodate those fixture mistakes.

## Existing owners

WorkBoard.QueueBlocker replaces endpoint-local ArrivedBefore. It scans existing live jobs and uses
original CreatedTick then stable job ID for the existing FIFO discipline. Only assigned, arrived,
currently eligible waiters with no future retry deadline compete. Their actual waiting slot, CheckJob,
request compatibility and ServiceAvailable/DockJourney admit real future service. Paused plans,
disabled/unavailable workers, forbidden routes/policies and away waiters cannot head-of-line block
an unclaimed bay. This is not ordinary dispatch re-ranking or active bay preemption.

The existing guarded Overlap method serves claims and queue arbitration through the selected
ISpatialOverlap provider. Queue comparisons additionally guard the competing worker. Only usable
service geometry that overlaps the candidate bay can block it; unrelated bays remain available.
Unsupported comparison is refused explicitly, not assumed disjoint. These participating-entity/
tick/allocator guards are not universal spatial mutation tracking or global rollback.

SelectDock applies the same queue rule to new/replacement service proposals. Retained claims skip
queue order; a paused service remains exclusive. ApproachDock applies that rule per candidate bay
for waiting promotion, replacing the old all-bays endpoint-level veto. No queue records, arrival
timestamps, spatial index, route copy, copied eligibility formula, stock or new scheduler is added.
Existing jobs/receipts/progress/motion/Sync/views retain ownership.

Fixtures author canonical JSON through existing explicit master composition. An actual idle worker
with explicit empty allowed roles occupies a bay. Normal player home commands move that machine
physically after the work phase, creating a genuine newly vacant bay checkpoint while old waiters
remain arrived. Tests observe engine decisions, not test-owned queue or movement rules.

## Direct proof

- Native tile and independent public point geometry both protect an arrived older work job against
  new foreign-company admission at a differently named facility sharing the bay. The caller receives
  its actual available waiting place, not a stolen bay or another facility's committed wait.
- Pure previews preserve hashes and entity/job allocators. Foreign private job rows stay hidden;
  newly vacant derived queue state round-trips without another saved owner.
- A second non-overlapping service bay remains admissible even while the shared first bay yields
  to an older eligible waiter. Paused plan, disabled policy and physically distant old waiters do
  not block an unclaimed shared bay. No queued work earns service remotely.
- A point provider that mutates the competing worker specifically during coincident service
  comparison cannot publish usable preview admission; the shared guard throws.
- Physical point execution queues two companies at different facilities, promotes older work first,
  retains younger waiting geometry and allows no overlapping earned work. Pausing acquired service
  preserves its claim/progress; normal resume, actual departure and later younger promotion finish
  both DAG orders. Saved interleaved continuations retain equal hashes and unchanged stock.
- Physical native execution mixes an older haul with foreign ordinary work sharing service geometry.
  Both travel to actual different waiting tiles; older hauling promotes first and earns loading while
  queued work earns zero. Subsequent travel/delivery/work/departure complete normally. Sampled twins
  have identical hashes and at most one machine on the shared service tile. Source32→24 and
  destination0→8 are single real debit/delivery; completed checkpoints restore identically.

## Unproven and still open

This is scoped arrived FIFO, not full P9/framework acceptance. No alternate authored queue discipline,
emergency service preemption, unconditional fairness under infinite/ineligible work, moving facilities,
overlapping hulls, future traffic/road reservations, fleet/deadlock recovery, global provider rollback,
replay/lockstep/stress/soak or allocation budget was tested. Retry-deadline and route eligibility
branches are integrated/source-reviewed, not exhaustive direct fault matrices. The distinct-bay proof
is pure admission, not every multi-bay physical topology. Earlier suites compile only. No Oil/Tycoon,
host/demo/Unity/Godot/Draw changes or engine world-size cap.

## SHA-256 of final compiled/gated source

| File | SHA-256 |
| --- | --- |
| src/Beep.SimEngine/Kernel/SaveGame.cs | D640EB4429769F1BCA9096A57E2404AC225965A1AE3E07178608BCD5B8E46DF6 |
| src/Beep.SimEngine/Standard/WorkDocks.cs | F430D5AC484F571FCBC37DD14817BEA72784A0B2ED5C09734D15B7E3B400EBB9 |
| tests/ExtensionProof/PointSpaceProof.cs | D899E4314E4A39A4746FFFF061445F06AE39E558E45D6C9656CA8B2BC9EFF33F |
| tests/Beep.SimEngine.Tests/DockGeometryClaimTests.cs | 43CD2515560E49DCFDBE48A9D1C72CA7C71D6AE319F1AE3E9C25366CB640E297 |
| tests/Beep.SimEngine.Tests/SharedDockQueueTests.cs | C0F503DD9A4B3100887936F0D92C0D6BC80B92484C4C316D4EC6FE3C548FBC3E |
| tests/Beep.SimEngine.Tests/Program.cs | B3B70A8DFA2F330FBF807A8A33575ED212A8A8BFD1D6162B336A83BFD6625CF1 |
