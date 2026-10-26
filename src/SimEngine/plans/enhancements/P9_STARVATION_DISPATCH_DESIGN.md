# P9-B4a — Authored starvation promotion in shared machine dispatch

Status: focused-verified, 2026-10-05. **147 checks / two exact suites** (71 policy; 76 physical),
build2.66.0/save38/work18/work-view21; zero warnings/errors/failures/aborts.
[Evidence](../../docs/migration/P9_STARVATION_DISPATCH_VERIFICATION.md). No third/family/full suite.
Parent [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md); complete framework roadmap remains partial.
Uses the existing B2/C3c/B3 shared assignment pass and C5a current job lifetime.

## 0. Problem

WorkScheduling.CompareAssignments orders emergency, effective priority, then original age. An
eligible low-priority activity or standing haul can wait indefinitely behind newer higher-priority
intent. Existing creation ticks already supply the sole required age state. No new queue is needed.

## 1. Goals

1. Optional sim.dispatch-policy selects one explicit master-listed dispatch_profiles definition
   through scenario settings.profile. No policy is implicitly injected into an unselected game.
2. Immutable IWorkerDispatchPolicy/DispatchProfileDef contracts declare participating scopes and
   a positive actual-session starvation threshold. Authored scope references are validated before
   Initialize; independent derived content works without editing the scheduler.
3. WorkSystem's same pure CopyAssignments pass computes detached starvation facts. Ready always
   precedes blocked; actual emergency always precedes ordinary. Within each class, eligible starved
   work precedes nonstarved work. Starved work uses oldest original creation tick before explicit
   effective priority; normal work preserves effective priority before age. Cost/stable IDs remain.
4. This lexicographic oldest-first starvation cohort prevents a newer high-priority stream from
   overtaking older eligible work in the same emergency class. It is not an unconditional guarantee
   under endless emergencies, no eligible workers, never-released claims or earlier blocked providers.
5. Original intent/job/stock age remains authoritative. Priority edits, alerts, previews, retries,
   saved continuation and dispatch recomputation never reset it or grant worker/resource claims.
6. Immutable WorkerDispatchOffer exposes nullable StarvationAfterTicks and Starved. Null explicitly
   means that no selected policy participates for the scope. Projection flags never come from source
   providers, mutate jobs or become serialized scheduling state.

## 2. Design

Standard/DispatchPolicies.cs owns immutable policy contracts/content only; DispatchPolicyModule.cs
declares the optional master-owned kind and binds that actual catalog alias as a stateless service.
WorkSystem.ValidateComposition validates alias identity, canonical declared scope references and
session rules before any initialization. WorkScheduling remains another file of that sole system.
It applies existing company alert priority and then selected starvation rules once before its existing
comparison/acquisition/rebuild. CopyQueue remains native raw-work diagnostics; CopyAssignments is
the authoritative combined ranking. No additional scheduling phase or persistent service is added.
Existing Dirty/source/fallback triggers retain sole pass cadence; threshold crossing affects the next
eligible shared pass, not an extra per-tick dispatcher.

JSON dispatch_profiles: id, scopes (explicit strings, including an intentional empty list), and
starvation_after (positive rational duration). Scenario settings.sim.dispatch-policy.profile selects
one definition. Only selected profiles act on a session; fingerprints/module manifests identify
the content. No old reader/compatibility/default threshold or arithmetic composite priority score.

For example, master-listed content belonging to the selected sim.dispatch-policy module:

```json
{
  "dispatch_profiles": [{
    "id": "game.dispatch",
    "scopes": ["sim.work", "sim.logistics"],
    "starvation_after": {"numerator": 30, "denominator": 1, "unit": "minute"}
  }]
}
```

The scenario explicitly selects the module and
`"settings": {"sim.dispatch-policy": {"profile": "game.dispatch"}}`.
The example duration is a game choice, not a default or framework limit. Scope references must
identify actual selected providers. Independent profiles may explicitly exclude an offer with null.

## 3. API

IWorkerDispatchPolicy: Id, DispatchScopes, ValidateTime(ISimulationTime),
StarvationAfter(ISimulationTime, WorkerDispatchOffer) returning nullable positive ticks.
DispatchProfileDef is its open content base. AuthoredDispatchProfileDef supplies JSON durations/scopes.
Existing WorkerDispatchOffer gains immutable derived StarvationAfterTicks/Starved fields;
source constructor offers have neither authority. Current WorkView version/build/save identity
advance together. WorkBoard/job/native/logistics/alert layouts remain unchanged.

## 4. Files

New policy/module, existing WorkScheduling/WorkContracts/WorkModule/SaveGame; public-only derived
profile proof, explicitly listed proof JSON/master and factory option; one new registered test file
with two exact suites; parent/master/P4/README/AGENTS/evidence. Current test identity literals migrate
only where directly affected; other suites compile, never count as fresh proof.

## 5. Errors

Missing selected settings/profile, duplicate/malformed/unknown scope or nonpositive session duration
refuse before Initialize. Independent policy returning zero/negative or undeclared-scope thresholds
faults normal evaluation, never substitutes a default. Sources cannot supply starvation rank. Private
views reject foreign company facts through the existing offer guards. Faulted sessions cannot save.

## 6. Compatibility

Development only. Breaking current content/semantics identity, no old reader or shim.

## 7. Verification

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Dispatch aging: authored thresholds, private ranking and strict composition"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Dispatch aging: physical fairness, retained age and saved continuation"
```

Only these two distinct names may run/repeat this stage. Bounded physical helpers report current
job/intent/worker/blocker facts on timeout. Older suites compile only; no third/family/full run.

## 8. Risks

Authored policy deliberately overrides normal priority only after its threshold. Actual emergency
class remains absolute. A starvation cohort orders actual eligible work, not impossible machines,
resources or routes. Shared definitions must remain immutable and pure across interleaved sessions.

## 9. Out of scope

Preemption, evacuation/return ranking, editable fleet/area/plans, alternate-endpoint recovery,
fuel/repair providers, congestion, provider/replay/lockstep/stress/allocation/full acceptance,
Oil/Tycoon, host/demo logic, adapters/renderers/platform/Draw. Full roadmap remains partial.

## 10. Execution

- [x] B4a.1 open strict master policy and pre-Initialize composition validation.
- [x] B4a.2 same-owner lexicographic ranking and detached view facts.
- [x] B4a.3 physical priority-stream/emergency/content-provider/saved isolation proof in two suites.
- [x] B4a.4 current linked evidence/tracker and explicitly open broader gates.
