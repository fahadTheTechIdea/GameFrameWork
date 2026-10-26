# P9-C3a — Unified demand/route dispatch verification

Date: 2026-10-05. Current working tree: **2.57.0 / save31 / logistics4 / logistics-view7**.
Work/work-view13, work_policy1, logistics_policy1 and resource-flow4 unchanged.
[Stage](../../plans/enhancements/P9_LOGISTICS_DISPATCH_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Final scoped gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Logistics dispatch: native route admission, private queue and deterministic intent ordering"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Logistics dispatch: contested physical carriers, refreshed offers and saved route policy"
```

Build: **0 warnings / 0 errors**, 3.16 seconds. **120 checks / 2 exact suites / 0 failures /
0 aborted suites**. Final admission suite: 70 passed, 228 ms. Final physical suite: 50 passed,
211 ms. Reported suite runtime totals 439 ms. Only these same two suite names ran during development;
no broad/full regression or third suite was run. Earlier reports are historical, not rerun.
Existing capability/C1/C2 references compile against new command/layouts; their suites were not run.

Development corrections: the test initially used nonexistent disposable/stream state-codec overloads
and assigned a wide CommandResult to int without checked conversion; both were corrected before
running. The first physical scenario timed out because route-delivered stock had no retained stock
policy and legitimately became exportable to another demand. The final scenario declares its intended
manual-only reserve band, preserving actual stock conservation rather than disabling legal logistics.

Final read-only handoff checks: scoped tracked-file `git diff --check` and explicit trailing-whitespace
scan of new/edited dispatch source, tests, JSON and stage documents passed. Git emitted only the
checkout's LF-to-CRLF normalization notices. Source searches confirmed the old PlanDemand/PlanRoute/
FindSource, fixed RoutePriority and c.Batch/r.Batch paths are absent. Build/save/service/view constants,
queue service alias and ConfigureRoute registration match this report. Documentation-only handoff
changes did not trigger another build or test run.

## Authoritative responsibilities

ILogisticsDispatchQueue aliases the existing LogisticsService. LogisticsScheduling.cs is a partial
file of that same owner, not a second scheduler or queue store. CopyQueue derives immutable offers
from current demand/route records, native stock/receipts, worker policy and selected-space providers.
Player-private LogisticsView delegates to it. Views never advance refill, prune intent, assign workers,
create jobs or mutate promises. Foreign queues and route settings expose no operational facts.

One current demand group combines actual active target quantities and identifies the highest-priority
contributing source, then oldest source/stable scoped ID. Disabled/expired/quiet-band rows cannot
promote another request. Original request rows remain authoritative; the temporary grouping dictionary
is not a durable intent/balance/claim table. Already promised inbound stock removes its real gap.

Ready offers rank ahead of waits, then explicit priority, original creation tick, checked provider
pickup-plus-delivery cost, stable destination/resource/scoped source/route identity. Costs are estimates,
not ETAs or elapsed-time scheduling. Export priority/owned-before-priced source selection and machine
preference remain the existing candidate policies beneath this cross-intent ordering.

PlanDemand/PlanRoute insertion-order passes are deleted. The normal declared LogisticsSystem cadence
refreshes bands and invokes the sole queue. Each actual Acquire rechecks current shared native/policy
admission and atomically owns the same source/cargo/destination receipts and worker.Job. Every successful
acquisition rebuilds remaining offers. Two independent pure offers may name the same carrier, but cannot
both acquire it. Partial refill recomputes quantity after actual inbound receipts instead of creating
another full load. The existing per-demand per-pass load bound remains computational work, not world
size. Routes attempt one acquisition per pass, with physical capacity/worker claims controlling further
shipments over time; this does not impose a one-live-shipment route cap.

## Route policy and durability

LogisticsRouteSettings is immutable explicit enabled/native batch/priority intent. AddRouteCommand
requires settings; ConfigureRouteCommand admits edits through CheckRouteSettings/ConfigureRoute.
One current route record owns settings and original CreatedTick. The old Batch command/state field
and fixed RoutePriority are removed. JSON mod source/worker profiles come from explicit master-listed
documents. Player-authored routes are normal engine commands; no host matcher or settings defaults.

The shared route admission checks real ownership/endpoints, resource acceptance and exact common native
batch quantum before allocation. Pure checks cannot consume route IDs. Missing/foreign/invalid settings
refuse explicitly. Priority permits the representable nonnegative range, without an arbitrary ceiling.
Disabled routes retain detached diagnostics and age. Edits/removal affect future acquisition only,
never revoke a live cargo/inbound receipt or alter a loaded shipment's real itinerary/amount.

The same logistics codec4 saves route settings/age. Duplicate/invalid IDs, counters, negative/future
age, invalid priority/native quantum refuse checkpoints. Native current endpoint ownership/compatibility
validates on writes and after all service restores; removed endpoints may lapse at the next normal pass.
Save31/view7 replace old layouts, without a compatibility reader. Loaded jobs/claims remain in their
unchanged work/inventory codecs. No saved queue, mirrored carrier, fake inventory or derived cost cache.

## Focused proof and scope

Admission proves actual command/preview parity, native batch refusal, ID allocation purity, immutable
settings/age, private pure views, changed authored order, demand-versus-route priority, readiness before
blocked maximum priority, cost/age/stable ties, disabled routes, JSON mod contributing-source identity,
quiet-band nonpromotion, immediate source retirement and corrupt route codec refusal.

Physical proof verifies the last carrier goes to current priority; route can beat demand and vice versa;
actual timed movement/loading/delivery; two-carrier recomputation and exact partial load promises;
oldest-versus-cheaper newer request; priority edits after retained previews; disabling/removing loaded
routes preserves physical delivery; finite stock accounting; typed unmet supply; JSON mod retirement
with live receipts; partial loaded save/continue hash identity; and independent interleaved sessions.
Every physical stage is bounded to 320 Updates with actual job/state/destination/blocker diagnostics.

This is ordinary demand/route ordering, not company alerts/emergency haul provenance or eligibility,
starvation promotion, preemption or global work-versus-haul arbitration. Evacuation/loaded return retain
their existing physical owners and are not yet authored entries in this ranking. Fleet/load-wait/home/
fuel/service, timed docks/congestion, editable/all-dispatch areas, persistent plans/templates, replay/
lockstep/provider fault matrices/stress/fuzz/soak/allocation and full framework acceptance remain open.
No Oil/Tycoon/host/demo/renderer/platform or Draw changes. No full-stage/roadmap completion is claimed.
