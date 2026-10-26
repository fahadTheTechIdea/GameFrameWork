# P9-C4b — Physical haul dock verification

Date: 2026-10-05. Working source **2.62.0/save34/work16/work-view18**;
transporter2/logistics4/logistics-view8/flow4 and optional company-alerts1/view1 unchanged.
[Stage](../../plans/enhancements/P9_DOCK_QUEUES_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Exact focused gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Haul docks: authored admission, atomic commitments and checkpoint guards"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Haul docks: physical waiting, fair service and saved continuation"
```

Final build: **2.20 seconds**, zero warnings/errors. Admission: **52 passed / 237 ms**;
physical: **56 passed / 229 ms**. **108 checks / two suites / zero failures / zero aborted suites**.
Summed reported suite time: 466 ms. Each complete unique name selected exactly one suite, confirmed
by runner output. Commands ran independently in parallel against the same already-built binaries.
No broad/full/third suite ran. Prior suites were compiled only, not rerun.

Initial build found two CS1061 errors in the new public-only proof (JsonReader has no Path and
PointSpace has no runtime Extent); corrected to explicit JSON context and the existing resolved
presence definition alias. First clean build: 2.56s; first two-suite gate: 49/50 passed (215/218 ms).
Shared definition admission/purity/result guards and extra route/blocked-departure checks were then
added. Intermediate build found two CS0311 constraints: IHaulDockDefinition must inherit the existing
IComponentDefinition. Corrected before the final clean build and both final exact suite runs above.
No stale binary or intermediate count is used as the final gate.

Final source-only checks covered the 25 edited/new stage paths: scoped `git diff --check` and trailing
whitespace search reported no findings (Git emitted ordinary LF/CRLF policy notices only). Explicit
point-docks.json parsed successfully. Later edits were documentation only; no additional test suite
was run or needed to characterize the already-built final runtime gate.

## What was proven

- Public-only non-tile dock provider binds immutable registered JSON slot definitions through
  IHaulDockDefinition/IHaulDock. Common HaulDockRules owns content admission; proof owns geometry,
  resolved native resources, actual carrier occupancy and PointMotion approach, no job/queue codec.
- Both endpoint slots participate before job/native source/cargo/inlet/carrier mutation. Service/
  waiting capacity is authored physical locations, not an arbitrary engine world/fleet limit.
  Pure preview, stale/far-end/full/closed-route refusal allocate no job or partial receipts.
- WorkBoard's existing Job fields alone own source/destination slot commitments; same native
  stock/receipt transaction and WorkSystem dispatch/timed handling owners remain in place.
- Distinct physical waiting locations, zero queued handling time, actual service outside endpoint
  center, oldest arrived promotion, paused service non-preemption, source claim until physical
  departure, loading/unloading conservation and no tested service-location overlap.
- Final LeaveDock keeps actual worker ownership after committed unload. Authored departure places
  move carriers out of service over game time. Occupied exits retain service/typed blocker with
  no teleportation or repeated stock delivery; empty-cargo departure saves/restores exactly.
- Private immutable slot/wait facts; content refuses empty/duplicate/overlapping/undefined/missing
  service/departure geometry and unknown resources. Checkpoints refuse missing/duplicate slot
  claims and invented waiting progress; no compatibility reader is introduced.
- Interruption releases only that job's slot/native claims. Released actual bay occupancy still
  blocks promotion until a player's ordinary home command physically clears the idle carrier.
- Normal removal protects active commitments. Explicit test-only participant destruction exercises
  next-update cleanup of surviving native/job claims, not a player bypass/removal shortcut.
- Saved physical waiting, policy pause/resume, loaded destination queue, departure, blocked exits,
  interrupted occupied geometry and endpoint-loss continuation preserve twin hashes. Interleaved
  sessions share no process-global queue. All physical test stages are bounded at 300 Updates with
  job/toil/slot/progress/blocker/position diagnostics; no harness advances gameplay state itself.

## Source and ownership review

Runtime additions: Standard/HaulDocks.cs public definition/geometry contracts and common validator;
WorkDocks.cs another file of the same partial WorkBoard. Work.cs owns saved slot fields/current
itinerary, waiting entity-state facts, service reach, actual departure and codec validation.
Logistics.cs owns current preview/acquisition/physical commit, now including slot facts/guards.
WorkView/WorkModule and EngineInfo migrate view/build/save identities. No separate queue scheduler,
reservation book, stock balance, location registry, host timer or legacy fallback was added.

Public-only ExtensionProof adds PointDockProof, explicit point-docks.json and registration/resource
listing. HaulDockTests submits normal commands/drives Updates/reads facts; internals are used only
for admission/commit/checkpoint fault instrumentation consistent with existing framework tests.
TimedHandling/CompanyAlert/SharedMachineDispatch metadata assertions were updated for the new build/
save/work schema and compiled only. README/AGENTS/master/P9/P4/current stage were reconciled; C4a's
report is labelled historical rather than reused as current verification.

## Not proven / still open

No claim of whole-framework completion or current broad regression. Generic repair/fueling/service
docks, fleet policies, editable/all-dispatch areas (including dock-location containment), persistent
plans/templates, incremental subquantity native receipts, starvation/preemption, automatic evacuation/
return policy ranking, all provider purity/fault matrices, cross-dock geometry exclusivity, route-wide
collision/congestion, replacement/topology breadth, replay/lockstep/stress/soak/allocation and complete
P4/P7/P9 acceptance remain open. Pure guard code is compiled; adversarial purity-provider breadth was
not separately exercised. Both-endpoint slot reservation is deliberately conservative, not a maximum
throughput guarantee. Paused/occupied bays and finite occupied departure places produce typed waits.

No Oil/Tycoon/old engine/host/demo/harness gameplay/platform/adapter/rendering/Draw changes. Dirty
working-tree changes were preserved; no commit/reset/Git initialization/package publishing occurred.
