# P9-C3b — Emergency supply verification

Date: 2026-10-05. Working tree **2.58.0 / save32 / work14 / work-view14 / logistics-view8**.
Logistics4, work_policy1, logistics_policy1 and resource-flow4 unchanged.
[Stage](../../plans/enhancements/P9_EMERGENCY_HAUL_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Current scoped gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Emergency hauls: current source facts, private priority and eligible native offers"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Emergency hauls: physical incident supply, paused cargo and retired saved provenance"
```

Final build **0 warnings / 0 errors**, 1.68 seconds. **82 checks / two exact suites / zero failures /
zero aborted suites**: admission 45 passed / 215 ms; physical 37 passed / 306 ms. Reported suite time
totals 521 ms. Only these two suite names ran. No broad/full regression, third suite, Oil/Tycoon,
host/demo/platform/rendering, replay/lockstep, provider fault matrix, stress/fuzz/soak or allocations.
Earlier gates are historical, not rerun. Updated C1/C2/C3a/B2 assertions compile but their suites did
not run. Development changes used the existing service TryGet(out ...) and one work-state codec;
no failed test was hidden or counted as a passing gate.

Final scoped source checks passed: `git diff --check` on the tracked slice, and an explicit
trailing-whitespace scan of 27 edited/new source, test, content and handoff files. Git emitted only
line-ending conversion notices. Source searches found no implicit `LogisticsDemandSourceCheck.Active()`
classification or copied `job.Emergency` path. Verified current save/work/view versions, the service
alias to the existing LogisticsService, and requester serialization in the existing WorkBoard codec.
Documentation-only handoff did not trigger another build or test run.

## Current source ownership, not copied urgency

LogisticsDemandSourceCheck adds explicit Emergency for Active checks only; expired/invalid facts
cannot carry emergency. Ordinary stock/producer owners explicitly return false. WorkBoard returns
true only for a current valid bound response episode. Priority, role, activity name and numeric source
scope do not manufacture urgency. An ordinary high-priority order remains ordinary.

ILogisticsDemandSources aliases existing LogisticsService; InspectSource resolves through current
declared ILogisticsDemandOwner services and validates facts/company. No additional registry, intent
table, priority score, incident record, emergency inventory or copied classification is introduced.
The same inspection supplies demand projections, contributing-source ranking, haul preview/acquisition
and assigned physical eligibility. A public-contract JSON mod reports its own explicit classification
through its normal source-owner contract, with no scheduler changes. New/ordinary proof source JSON
requires emergency explicitly. Incident/component profiles and response DAG requirements come from
master-listed emergency-hauls.json and existing module registrations.

Ready emergency offers precede ready ordinary demand/routes, then current explicit priority/original
age/provider cost/stable IDs. Readiness still precedes urgency: unavailable emergency intent cannot
monopolize a truck that can perform ordinary work. Within a combined store/resource group, emergency
contributors take precedence over ordinary contributors; disabled/quiet/expired rows never elevate it.
The native combined delivery target and actual receipts remain in their existing owners.

Shared CheckAssignment receives current classification before any source/cargo/destination promise.
A preferred but emergency-ineligible carrier cannot win incident supply. A less-preferred eligible
carrier can, while ordinary supply may still use the ordinary-preferred machine. The existing native
matching/acquisition/recomputation and physical travel/load/unload are unchanged owners.

## Physical provenance and lifetime

Job.HaulDemand stores optional original LogisticsDemandId in the sole work codec. There is no saved
Emergency bool or mirrored incident episode/worker/source-state object on a haul. WorkBoard.IsEmergency
derives current haul urgency through its declared requester; physical CheckJob and private WorkJobView
reuse it. Work queue incident classification also delegates to this owner instead of a separate test.

Ordinary/manual route, evacuation and loaded return have no requester linkage; request-driven stock,
work, production and mod shipments retain the originating workflow. Non-haul/route/return records cannot
claim requester provenance. Undefined owner/identity/company refuses source inspection and checkpoint;
after full restore, ValidateHaul checks physical links and requester binding together. Expired workflow
identity remains lawful shipment history, not an instruction to release reservations or cancel cargo.

Restrictive emergency_eligible edits pause an acquired shipment and actual motion with the saved
work.policy-emergency-ineligible blocker. Source/cargo/destination receipts, worker.Job, loaded material
and current toil remain protected. Re-enable resumes the same physical execution. Retirement or episode
replacement makes an old shipment ordinary while preserving its original source ID and all real cargo;
it can then finish even if the carrier is emergency-ineligible. A renewed response uses existing inbound
promises and delivered stock rather than booking a duplicate load.

Save32/work14/work-view14/logistics-view8 break together without old readers. Logistics4 still owns
requests/routes and native claim books still own promises. Providers restore first; source liveness is
checked only against complete restored session state. Entity.Update alone commits visual-neutral states.
No alternate loop, Draw, teleport, refund or host-calculated decision.

## Scoped proof and remaining work

Admission covers native source aliases, explicit ordinary/emergency classification, malformed facts,
private pure queue/job views, current episode binding, same-store contributing priority, readiness before
urgency, preferred ineligible versus eligible carriers, real requester identity, unknown/fabricated
checkpoint provenance, explicit saved layouts and immediate retirement/stale-episode exclusion.

Physical covers one actual contested truck, timed pickup/delivery, eligibility pause while loaded,
unchanged tokens/toils/stock, saved pause/resume/hash identity, interleaved isolation, retired loaded mod
shipments, a real incident's supply followed by timed DAG repair/native consumption/incident completion,
and replaced episodes reusing actual incoming cargo. Every physical wait is bounded to 320 Updates with
actual job/state/blocker diagnostics. The mod and incident provider compile through public contracts only;
test setup uses normal engine execution contracts, not a test-owned matcher or gameplay loop.

Company alerts, starvation promotion, preemption/global work-versus-haul competition, evacuation/return
policy ranking, fleet/service/fuel, timed docks/congestion, persistent plans/templates, editable/all-dispatch
areas, complete provider fault/purity/cancellation/replacement matrices, replay/stress/performance and full
framework acceptance remain open. No full-stage or overall roadmap completion is claimed.
