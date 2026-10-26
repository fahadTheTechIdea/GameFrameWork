# P9-C3c — Shared machine arbitration verification

Date: 2026-10-05. Current working tree **2.59.0 / save32 / work14 / work-view15 /
logistics4 / logistics-view8**. Policy components1 and resource-flow4 unchanged.
[Stage](../../plans/enhancements/P9_SHARED_MACHINE_DISPATCH_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Exact current gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Shared machine dispatch: private cross-source ranking and declared JSON providers"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Shared machine dispatch: physical contention, incident chain and saved sessions"
```

Final build: **zero warnings / zero errors**, 1.48 seconds. **130 checks / two exact suites /
zero failures / zero aborted suites**. Admission 76 passed / 229 ms; physical 54 passed / 271 ms;
reported suite time 500 ms. Only these two suite names ran. Earlier development invocations passed
110/117 checks before additional stale-offer/cancellation proof; those are not the final gate.
Adding cancellation proof caused one CS0246 test compile error (missing Commands import), corrected
without a runtime change. The following stale-binary suite pair is excluded from current evidence.
After a successful corrected build, physical proof had 50 passed / one failed assertion: it incorrectly
expected eight unused destination units after cancelling response work. The actual incident remained
active and correctly posted replacement work, which consumed incoming cargo through real repair.
Corrected proof now asserts distinct replacement execution, preserved original requester/native inbound
claims, once-only consumption and incident completion. No engine shortcut or outcome was substituted
to make it pass. Final gated build and both exact suites pass as reported. Historical C3b/C3a/B2 were not rerun.
SessionTimeTests schedule assertions were migrated and compiled only, not tested or counted.

Final scoped source checks pass: tracked-slice `git diff --check` (only line-ending conversion notices),
explicit trailing-whitespace scan of 24 edited/new files, both new JSON documents parsed, current
build/save/work/view/alias declarations inspected. Word-boundary searches confirm old LogisticsSystem,
DispatchQueue and duplicate CompareDispatch/CompareLogistics methods are absent from src/tests.
An initial overbroad search matched the distinct CopyDispatchQueue diagnostic API; corrected exact
method search passes without deleting useful work-only facts. Docs-only handoff triggered no additional
build/test run. Existing dirty/unrelated work was preserved; no commit/reset/host/platform changes.

No full/broad/third suite, Oil/Tycoon, hosts/demos/platform/rendering, replay/lockstep/fuzz/soak,
allocation or whole-provider fault matrix ran. This scoped gate does not close P9/P4/full framework.

## Actual architecture change

Previously WorkSystem claimed workers at order400 before LogisticsSystem independently dispatched
standing hauls at450. Separate correct internal queues therefore gave ordinary activities a phase-order
advantage over emergency supplies. WorkSystem.Assign now owns one arbitration pass over existing
work-job offers and declared IWorkerDispatchSource services. It runs after physical jobs and activity
orders advance, before idle cargo/home behavior. Board Dirty/assignment fallback or a declared source's
schedule wakes it; every participating source then competes, including between periodic visits.

WorkerDispatchOffer is immutable detached current ranking, with canonical scope/positive long identity/
entry, company, target, actual offered worker, readiness/blocker, emergency, exact priority, original
age, provider cost and per-pass attempt envelope. No offer is serialized or a retained authorization.
Readiness → emergency → priority → original creation tick → cost → stable target/company/scoped identity
is one comparison reused by native work/logistics projections and actual combined selection.

WorkSystem uses current job admission in TryAssign; source TryAcquire resolves current intent again.
Every successful acquisition or work cancellation rebuilds all offers after real machine/stock/capacity
claims. Original scoped intent identities count per-pass attempts; a changed budget during a pass faults,
not an unbounded repeated attempt. Pure blocked offers do not grant machines or cause repeated rebuilds.
WorkSystem validates foreign/duplicate source scopes/schedules before Initialize and current duplicate/
private offer identities, age/company/target/actual idle-worker facts before selection. ServiceContainer's
existing Providers owns discovery and alias deduplication; no source registry, copied job/claim/priority
store, closed source enum or second scheduler is introduced.

LogisticsService aliases IWorkerDispatchSource and keeps the same demand/route/latch/source/receipt owners.
PrepareDispatch prunes expired intent and refreshes native stock bands. Stable destination/resource or
route identities do not change when contributing requester priority changes. TryAcquire resolves a
fresh native queue offer then uses existing atomic source/cargo/destination/carrier acquisition.
DispatchQueue and the old LogisticsSystem type are deleted, not wrapped. LogisticsMaintenanceSystem
at450 retains only physical evacuation maintenance and complete-restoration native validation.
IWorkDispatchQueue.CopyQueue remains specifically open work diagnostics; CopyAssignments/private
WorkView exposes actual cross-provider arbitration through that same owner.

Changed scheduling is build2.59.0 and WorkView15. Existing saved layouts remain save32/work14/logistics4;
the normal module/system manifest captures scheduling. No old build reader or saved derived queue.
Job.HaulDemand and current source classification remain the actual provenance/emergency authority.
All motion, loading, delivery, repair, resource consumption and entity-state transitions stay in engine
owners; no host, proof-provider or harness loop calculates progression or copied outcomes.

## Focused proof

Admission proves low-priority emergency supply beats older maximum-priority ordinary activity on one
multi-role machine; unavailable emergency cannot take a ready ordinary worker. Pure private/observer/
foreign-session facts, original intent budgets, actual native receipt ownership and fresh busy-carrier
recomputation pass. Ordinary cross-kind priority, age before cheaper routes, provider travel cost,
canonical final ties, inaccessible supply and dirtied dispatch between periodic source visits pass.
Disabled native intent and newly disabled external worker refuse retained ready offer identities without
jobs or claims. Undefined external identities refuse without mutation.

A public-only engine-side DispatchProofSource declares a master-listed JSON shipment/profile/interval
through an ordinary module. Its scope and identity beyond int.MaxValue participate without selector
changes. It owns no job, balance, promise, movement, stock-demand or saved priority table: native
IHaulExecution acquires/progresses its fixed ordinary shipment. Service aliases deduplicate; duplicate
scope and foreign-world providers refuse composition. Invalid identity/worker/age/cost/attempt constructor
facts refuse explicitly. Initial profile intent exists at new-game tick zero; completion derives from
actual destination stock/inbound promises, not another saved workflow record.

Physical proves one actual inspect/haul machine first loads/delivers incident material, then switches
roles into positive-duration emergency repair before waiting ordinary activity. Only actual completed
bound repair consumes eight supplied units and clears the incident. Loaded saved twins continue identically
through both claim decisions, consumption/completion and eventual ordinary work. An interleaved session
borrows no stock/jobs/incidents. Dry emergency repair beats ordinary stock hauling and is followed by
actual saved delivery. Two-machine work-versus-haul contention reselects the remaining carrier, partially
fills sixteen units in real loads, preserves source totals and releases actual inbound receipts. JSON
external shipment saves loaded cargo, delivers once, then ordinary work completes with identical hashes.
Cancelling a loaded response requester retires old urgency but preserves original cargo/carrier/source;
the still-active incident posts replacement work using the existing inbound receipt, repairs physically
and only then releases pressure for ordinary activity. Saved twins remain identical with no extra pickup,
refund or incident-resolution shortcut.
All physical waits are bounded to 360 Updates with actual job/role/state/blocker diagnostics.

## Remaining gates

Company alerts, starvation promotion, automatic preemption, evacuation/return policy ranking, editable/
all-dispatch areas, fleet/service/fuel, dock occupancy/timed amount-scaled loading/congestion, persistent
plans/templates, provider purity/fault/cancellation/replacement breadth, replay/stress/performance and
full framework acceptance remain open. Existing evacuation/return behaviors are not falsely represented
as authored demand identities. Strict priority/oldest-first is not a starvation guarantee. More frequent
dirty-triggered standing-haul dispatch is deliberate shared arbitration, not independent periodic
acquisition retained for compatibility.
