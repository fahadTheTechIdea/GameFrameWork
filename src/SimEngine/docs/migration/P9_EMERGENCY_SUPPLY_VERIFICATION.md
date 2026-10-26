# P9-B4b2 — Emergency supply dispatch verification

Date: 2026-10-05. Current working source **2.68.0/save40/work18/work-view23/logistics4/logistics-view9**.
Existing work/native/logistics/transporter2/flow4/company-alert1 state layouts unchanged.
[Stage](../../plans/enhancements/P9_EMERGENCY_SUPPLY_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Exact focused gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Emergency supply: pure dispatch, native blockers and protected carriers"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Emergency supply: timed delivery, retained work and saved continuation"
```

Final source build: **2.78 seconds**, zero warnings/errors. Policy: **87 passed / 354 ms**;
physical: **86 passed / 474 ms**. **173 checks / two exact suites / zero failures / zero aborted
suites**. Summed reported suite time: 828 ms. Only these two distinct complete names ran/repeated.
No third/family/full suite. Eight older test identity literals and the affected source/offer calls
migrated and compiled only; historical results are not current broad regression evidence.

The first physical run found a fixture expectation error: an ordinary stock target and an active
response request are separate additive intents, not duplicate demand. The protected-haul proof now
asserts their truthful combined unready offer, then disables the extra stock intent via a real
command. Existing inbound claims satisfy the remaining response-only gap without another dispatch;
that intent edit never clears the acquired carrier/cargo/receipt.

## Ownership and scoped proof

- Required WorkCandidateMode on IWorkerDispatchSource.CopyOffers/TryAcquire migrates native and
  public-only proof sources in place. WorkerDispatchOffer constructor explicitly carries zero or a
  positive interrupted-job identity; ordinary interruption facts refuse. No old overload/shim.
- WorkSystem owns one generalized safety/profile preview and actual interruption protocol.
  Activity candidate selection and standing source acquisition use those same owners. Source facts
  must name the actual busy relation and pass that safety/willingness check; source providers still
  cannot submit derived alert/aging scores. Existing selected immutable content remains sole policy.
- LogisticsService's existing Preview/PreviewSource/BuildQueue accept transient mode/intent facts.
  No stock/worker/job/claim clone or saved takeover state. Real role/area/availability, export stock/
  holds/reserves, receiving/cargo native capacity, handling profile, dock admission and both route
  legs remain in that selector. Only prospective ordinary busy relation can be ignored. Native raw
  queue/direct haul previews stay idle-only; combined private offers expose potential InterruptedJob.
- Locked work, excluded logistics scope, profile veto, missing haul role, emergency ineligibility,
  source stock blocked by actual held inputs, closed route and outside area remain truthful unready
  offers. Maximum-priority ordinary stock cannot interrupt, even with an unconditional independent
  profile. Eligible real idle spare takes supply without interrupting ordinary activity.
- Pure combined/native/direct/private reads preserve hash, actual incumbent and next job ID. Foreign
  company sees no carrier/response/native takeover facts; retained detached offers remain unchanged.
  Invalid mode and malformed ordinary interruption facts refuse.
- Actual source TryAcquire re-resolves current intent/preview; WorkSystem rechecks and stops/reopens
  the actual ordinary activity. Fresh ordinary Acquire alone creates real haul/job/carrier/dock/native
  receipt ownership. A pure availability provider that refuses after the actual worker becomes idle
  faults normal Update: partial failed admission cannot save/hash. No compensating refund/rollback.
- Authored and independent profiles physically interrupt already-paid work: eight ordinary startup
  units remain spent, two unspent held tool units release, eight actual source units reserve for
  emergency hauling. Timed partial loading, travel, unloading and subsequent repair consume actual
  native inputs; destination receives no teleported material. Source ends with four units (twenty
  authored opening, eight ordinary consumed, eight transported). Original age/progress remain and
  resume without additional startup consumption after repair.
- Save before takeover, during partial loading, and continuation through delivery/repair/resumption
  are identical. Interleaved untouched sessions remain isolated. Direct emergency activity takeover
  also continues deterministically through the shared migrated protocol.
- Actual native hauls are protected both before loading and with partial cargo, even under an
  unconditional profile. New response pressure and stock-intent edits keep the same acquired carrier
  and inbound token. The physical shipment supplies the current response; saved continuation stays
  identical and exactly eight units leave source.

## Current boundary and remaining work

Build/save/work-view/logistics-view identities advance together; existing state codecs remain sole
owners. Existing strict master-listed preempt_emergencies/scopes govern willingness; proof fixture
uses the already master-listed JSON activity document, with automatic supply explicitly selected.
No new module discovery/default, scheduler/pass, claim/preemption ledger, compatibility reader,
Oil/Tycoon/host/demo/platform/Draw implementation.

B4b2 closes native standing emergency-supply takeover of ordinary activity work, not full B4b/P9.
Automatic interruption/cargo handoff of acquired hauls, other provider source protocols, ordinary
priority preemption, evacuation/return arbitration, complete provider/alias/fault matrices, replay/
lockstep/fuzz/soak/allocation/full acceptance and fleet/plans/areas/fuel/repair/congestion/replanning
remain open. Existing actual native holds may block prospective supply; preview does not invent
post-release availability. Only this scoped two-suite result is confirmed current.
