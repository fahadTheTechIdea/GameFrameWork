# P12-B1 — Deterministic authored incident exposure and delayed propagation

Status: focused verified, 2026-10-06; broader P12 remains partial. Parent: [P12](P12_INCIDENTS_ENVIRONMENT_DESIGN.md).
[Scoped evidence](../../docs/migration/P12_INCIDENT_PROPAGATION_VERIFICATION.md).

## 0. Problem

IncidentResponseSystem owns physical recovery, but no framework owner evaluates or delivers incident
exposure. P12 requires bounded deterministic propagation without same-tick cascades or lost pending
work. Incident/work state must not be copied into another hazard or response scheduler.

## 1. Goals

1. Master-listed rules and named emitter bindings use public metadata/runtime contracts.
2. One optional system owns exposure scan cursors, due times and pending deliveries only.
3. An explicit positive per-update work budget bounds candidate evaluation/delivery, not world size.
   Exhaustion keeps saved pending scans/deliveries; stable round-robin scheduling prevents starvation.
4. Positive authored delays prevent same-step chains. Delivery rechecks current source episode,
   target episode and actual eligibility before invoking the target's existing IIncident.Raise.
5. Physical isolation/source recovery, target recovery/removal/movement and missing inputs prevent
   stale delivery. WorkBoard still owns all response activity/time/material/tool state.
6. Private immutable views and saved/interleaved runs expose actual owned backlog/blockers.
   Global scan identity cursors are observer-only; private views return None for those two fields.

## 2. Design and ownership

IIncidentPropagationRule extends IContentDefinition with explicit interval/delay, target named source,
target kind and company scope. ValidateBinding and Observe are public engine-side provider seams;
adding a rule needs content/module code only, no scheduler or host changes. Providers receive an
engine execution context and actual phased lifecycle facts, never mutable propagation records.

IncidentPropagationRuleDef is the registered immutable rule base. Native tile-radius rules explicitly
name their selected tile space and a nonnegative wide tile radius. They read actual full-footprint
Chebyshev separation using wide arithmetic; no terrain/weather defaults, forced map or map-size cap.
Other geometries/environment/connectivity checks implement the same Observe contract. Required
capabilities validate explicitly; missing live inputs return typed eligibility blockers.

Native incident_propagation is a stateless named binding: id/source/rule. It requires exact named
IPhasedIncidentResponse metadata/runtime on its entity. Generic base authoring supports independent
emitter components. Phased target snapshots provide the exact epoch needed to reject old exposure
after a target is repaired or replaced; generic non-phased migration remains a future gate.

Source/target condition arrays are explicitly authored, including empty arrays when no constraint is
wanted. Reuse SignalGateCondition for parsing/resolution/evaluation, not another predicate interpreter.
An incident phase-hold signal can stop spread after actual isolation; readiness re-evaluates on delivery.

IncidentPropagationSystem at380 owns one record per actual emitter: observed source epoch, next cycle,
saved bounded scan cursor/high-water entity identity, pending target epochs/due times and typed last
blocker. Stable source round-robin cursor is system-owned. Each budget unit handles at most one due
delivery or one candidate. Scan membership is a captured identity high-water; later entities join the
next cycle. Duplicate pending target deliveries are not created. Interval advances from authored due
time, not wall time. All timestamp arithmetic is checked64-bit. No copied incident active flag/progress.

Deliveries are due strictly after scheduling. They recheck exact current source/target and Observe,
then use the target's existing Raise contract and verify its resulting episode/timestamp. The target
joins its ordinary response system at390; newly active emitters cannot affect already captured source
admission during this Update. Source resolution cancels stale causes with a typed blocker; failed or
cancelled recovery does not itself erase an active source. Same-company/any-company scope is explicit.

System Sync alone owns exposure state; staged structural decoding validates before replacement.
OnLoaded checks actual live bindings/lifecycles after the existing WorkBoard state restores. No extra
service codec/incident/job ledger. These are candidate/delivery admission counts, not a bound on total
CPU or custom-provider internal work: source discovery and stable candidate lookup enumerate entities.
Module sim.incident-propagation requires sim.incidents only. Scenario settings explicitly select budget.
No automatic emitter/rule enrollment, compulsory spatial/operations/environment module or fake inputs.

## 3. API

IIncidentPropagationRule, IIncidentPropagationEmitter/Definition, IncidentExposureObservation/Context,
detached IncidentPropagationSnapshot/PendingExposureSnapshot, optional private propagation view.
JSON tile_incident_propagation_rules and incident_propagation; public modules register other rule bases.
No player command creates spread progress or instant repair. Existing incident trigger/recovery remain owners.

## 4. Files

Add Standard/IncidentPropagationContracts.cs, IncidentPropagationSystem.cs, IncidentPropagationModule.cs,
Views/IncidentPropagationView.cs. Extend SignalGateCondition to named explicit condition arrays.
Add public non-oil provider proof, master-listed JSON and two exact suites. Update identity and scoped
evidence/master/parent/README/help/AGENTS. New system codec1/view1/stateless emitter1; save62/build2.97.0.
Existing native incident/work/spatial/network/operation layouts remain unchanged.

## 5. Errors and risks

Invalid source/rule/IDs/durations/budget/space/condition binding fails before ticking. Malformed/mutating
provider observations and refused dishonest Raise fault normal Update. Stale/removed/incompatible
targets produce typed blockers, not substitute targets or fabricated weather. Saved invalid epochs,
timestamps, duplicate pending rows and wrong bindings refuse. Per-pair native scans are bounded by
budget, but arbitrary custom provider internal execution cost/purity needs separate verification.

## 6. Compatibility

Clean current content/state contracts only; no alias/shim/fallback reader.

## 7. Verification

Exactly two suites: `Incident propagation: authored rules and bounded admission` and
`Incident propagation: delayed exposure, physical isolation and saved waves`.
Prove strict JSON, native/provider substitution, budget deferral, cycle/delivery ordering, positive delay,
source/target stale epochs, isolation, physical response, private views and saved/interleaved hashes.
Build and compile other current identity assertions only; no third/family/full suite.

Final focused evidence: clean solution build (zero warnings/errors), **107 checks / two exact suites**:
61 contracts/admission (494 ms), 46 physical/waves (733 ms), zero failures/aborts. Positive aligned
durations, required settings/IDs/space/kind/capabilities, bounded round-robin and captured membership,
staged codec rejection, independent nonspatial-source provider, dishonest observation faults,
company-private immutable diagnostics, wide timestamps, delayed multiple waves, physical responder
travel/material consumption/isolation/cancellation/recovery, stale target episodes, removed identities,
same/any-company scope and saved interleaved hashes pass.35 other current identity assertion files
migrate/compile only; no preceding/family/full suites rerun. New codec1/view1/emitter1; build2.97/save62.

## 8. Open scope

Native weather/terrain/network-topology formulas, arbitrary non-phased sources, probability/severity/
damage/condition/environment accounting, maintenance/preparedness, failed-response retry, segment
cuts/arbitrary operation migration, broader provider/fault/purity/replay/stress/full P12/P1–P12 remain open.
No Oil/Tycoon/host/demo/platform/Draw changes.

## 9. Order

1. Add immutable rule/emitter contracts, strict native tile rule and condition reuse.
2. Add bounded durable exposure owner/module/private view and staged persistence.
3. Author public provider/non-oil proof and two exact gates, repair scoped failures.
4. Record verified evidence and update all tracker surfaces without claiming full P12 completion.
