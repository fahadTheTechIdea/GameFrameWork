# P9-HI1 — Retain valid home approach intent during physical traffic

## Problem and measured baseline

The compatible2.667 native harbour at minute3553 has a loaded delivery truck
and a returning idle truck cycling through the same positions every18 updates.
The delivery retains12 units and phase2 after1200 additional game minutes.
Moving positions and a healthy hash are not delivery progress.

The generic NativeTrafficTests reproduction earns the returning worker's partial
route through four ordinary updates before the outbound player command. Both
workers remain moving after240 updates: baseline1 passed/2 failed. The returning
worker repeatedly changes its actual motion goal between two parking slots.
Starting both workers together clears; that earlier control is not the failing
baseline and must not be used as proof of a repair.

## Contract and sole-owner design

Extend existing IWorkDock with required, pure IsApproachingSlot. It compares the
slot with the actual active motion destination, not proximity or estimated cost.
Native TileWorkDock reads IMobility.Goal/Status. Independent PointDock reads its
actual PointMotion.Goal/Status. Negative admission provider refuses execution as
before. No default interface method, compatibility path or native concrete lookup
in WorkBoard; providers implement the same required contract.

ReadHomeParking retains its current actual-arrival-first pass. Before cost
ranking, consider a current approach only when:

- the actual provider identifies that motion destination;
- the worker's current role, job claims, actual vacancy and policy still admit it;
- the original structural route/operating-area preview remains valid.

Report Pending for this retained travel intent. Do not require a dynamically
clear route to retain it: temporary traffic is precisely why movement may wait.
Movement and Approach still enforce all actual occupancy, segment clearance,
time, operating input and speed gates. Occupied, incompatible, changed-policy
or structurally invalid targets fall back to existing deterministic ranking.
An eligible physical arrival still wins and stops superseded motion normally.

The original motion payload is the only retained destination. Add no home-slot
binding, reservation list, route cache, worker priority, scheduler or clock.
All new provider reads use existing ReadDock/EntityObservation guards over the
actual provider, full endpoint, carrier, selected space and game clock.

## Acceptance and bounded verification

1. [x] Observe actual native cycle without commands or host gameplay (48-update trace).
2. [x] Reproduce the same phase-dependent cycle generically before production edits.
3. [x] Extend sole dock/parking owners and all current implementations, with no legacy overload.
4. [x] Verify saved waiting/retarget/replay/isolation/physical limits, independent provider faults, and existing occupied/area recovery (three exact cases417/0).
5. [x] Continue current native harbour through actual delivery, finalization and the original physical chain within original bounds (all three completed at8111; next fleet).

Engine build2.668.0; save160/replay1 unchanged. Behavior identity changes, so old
2.667 saves are preserved as historical evidence, not header-rewritten or migrated.
New preparation is justified by the real engine correction, not by a failed test.
No full/profile/family run; no authored Oil content/business/platform/Draw changes.
This repair stage's selected acceptance is complete, not original P9.051 or the
whole plan. Current native b63abed3fb504a71ba4f7a1ea7b5c13b/checkpoint.json is
harbour_complete/next fleet at8111/hashDF87EA156D6CE938. Preserve this state.

## Operational and assurance boundaries

Reuse World.Fault for callback failures, existing public views for observations,
and the existing bounded runner/log files for diagnostics. No catch-and-success
path or independent fault owner. Broader Log/Audit/Notification foundations and
assurance gaps are explicitly mapped in the existing verification record below;
they are not declared complete by the presence of a fault object or replay.

## Risks and continuation

This proves one real oscillation and its selected independent-provider failure
boundary, not universal deadlock freedom, arbitrary docks, large fleets or native
performance. Structural Pending/budget cases and broader topology/endpoint/fuel
matrices remain original P9.051 work. Do not weaken dynamic collisions to improve
acceptance or infer full harbour completion from elapsed time.

[Every attempt, identities, current native state and handoff](../../docs/migration/P9_HOME_APPROACH_INTENT_VERIFICATION.md).
