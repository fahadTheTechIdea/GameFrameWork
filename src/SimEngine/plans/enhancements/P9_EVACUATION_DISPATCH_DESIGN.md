# P9-C3d — Authored evacuation through the sole assignment pass

Status: HISTORICAL focused verified checkpoint, 2026-10-07; not proof of the current working tree.
Current source uses the focused-verified [C3e recovery/cargo-return migration](P9_CARGO_RETURN_DISPATCH_DESIGN.md).
Evacuation-only policy/chronology and content were replaced directly with shared recovery contracts
and master-listed native/public JSON. Current C3e clean build and180 assertions are recorded
separately; broader acceptance remains open.
The C3d verification and source manifest below record the earlier checkpoint only.
Return-cargo arbitration and wider fleet acceptance remain open.
Build2.197.0/save105/board36/work35/logistics15/logistics maintenance2/logistics view26.
[Verification](../../docs/migration/P9_EVACUATION_DISPATCH_VERIFICATION.md).

## 0. Problem

LogisticsService.PlanEvacuations creates physical jobs after the shared WorkSystem assignment
pass, using Tuning.EvacuationPriority. Evacuation cannot participate in the ordinary engine
priority/age/alert/starvation rules and is not authored policy.

## 1. Goals

1. Optional master-listed evacuation policy is explicit JSON/contract content, with no default.
2. Evacuation competes through existing IWorkerDispatchSource and the sole WorkSystem pass.
3. LogisticsService owns only source/resource intent chronology; inventory/receipts/jobs remain
   sole physical owners. No copied quantities, destination promises, separate queue or clock.
4. Fresh acquisition recomputes actual source/destination/carrier admission atomically.
5. Private queue views expose truthful readiness/age, save/load preserves intent chronology,
   and material/time/exit behavior remains physical.

## 2. Design

EvacuationPolicyModule selects an immutable EvacuationProfileDef through explicit scenario
settings. IEvacuationPolicy declares enabled, priority and bounded attempts per pass. No selected
module means no automatic evacuation, not a fallback priority. The previous hard-coded priority
and separate posting path are deleted.

LogisticsService refreshes source/resource intents during PrepareDispatch. One embedded Sync
owns their company, creation and last-dispatch coordinates, all from GameClock. Original
chronology survives promises/partial trips; current inventory determines quantity and validity.
Existing BuildQueue/CopyCandidates/TryAcquire read these intents, select actual compatible ports,
and acquire through existing Preview/Acquire. No second dispatcher or balance store.

LogisticsDispatchOffer identifies evacuation as a distinct immutable offer kind. Shared worker
assignment target is the actual evacuation origin; destination is separately selected and can
be absent on refusal. Ordinary work/haul/emergency rules, profiles and physical gates all remain.
Maintenance only removes empty unreferenced evacuation containers, never assigns machines.
It must also pass the existing World.CheckDespawn guard, including other providers' promises.

## 3. API

IEvacuationPolicy, EvacuationProfileDef, native authored definition and optional module;
LogisticsDispatchKind.Evacuation and offer origin. JSON explicitly authors enabled/priority/
attempts_per_pass; scenario settings select profile. No new host/demo behavior.

Authoring requires the runtime module in GameDefinition, its master declaration/document,
the module in the scenario's modules list, and settings.sim.evacuation-policy.profile naming
the authored definition. These are normal composition requirements, not a runtime fallback.

## 4. Files

New Standard/EvacuationPolicies.cs and LogisticsEvacuations.cs; existing Logistics.cs,
LogisticsScheduling.cs, Tuning.cs and semantic metadata; two exact evacuation suites/public-only
fixture content/providers, tracker, documentation and manifest.

## 5. Errors

Missing/invalid selected profile fails composition. Blocked supply, stock floors, destination,
carrier, motion, handling and native claims retain typed refusals. Retained offers cannot acquire
removed or changed intent. Saves reject malformed/duplicate chronology and changed profile.

## 6. Compatibility

Direct current save/schema revision, no old codec or fallback path. Automatic evacuation is an
explicit selected capability; retained cargo return remains its current path for a later stage.

## 7. Verification

Build and only two exact new evacuation suites. Prove source-owned age/private purity, current
refusal, cross-kind priority, physical hauling/conservation/cleanup, saved pending/loaded twins,
and independent public profile/port composition. Other suites compile only.

## 8. Risks

Source enumeration remains bounded by current world content, not constant CPU proof. Existing
return-cargo bypass, broad fleet/congestion/replay/provider/failure/performance gates remain open.

## 9. Out of scope

Oil/Tycoon/platforms/Draw; new transport media, clocks, host logic, instant cleanup of stocked
containers or copied resource/worker reservations.

## 10. Stages

- [x] Explicit contract/content policy and sole-owner chronology.
- [x] Shared offer/candidate/acquisition path; remove separate posting and hard-coded priority.
- [x] Focused private/physical/save/public proof, clean build and tracker evidence:73 assertions
  across two exact suites (42 admission;31 physical), zero final failures/aborts. Other suites
  compile only; one unchanged GameClock.
- [ ] Return-cargo arbitration and broader fleet/congestion/replay/performance/full acceptance.
