# P12-B1 incident propagation — scoped verification

Date: 2026-10-06. Runtime: Beep.SimEngine only. Build **beep-simengine-2.97.0**, save **62**.
Stage: [P12-B1](../../plans/enhancements/P12_INCIDENT_PROPAGATION_DESIGN.md).

## Ownership and clean migration

Optional `sim.incident-propagation` requires only `sim.incidents`. Master-listed rule definitions and
named emitter capabilities author positive rational interval/delay, target source/kind, company scope
and explicit source/target condition arrays. One new system at380 owns exposure cursors, observed
source epochs and pending target epochs/deadlines. It does not own incident state, response work,
stock, reservations, motion, phase completion or another gameplay loop.

Native tile rules read actual full-footprint Chebyshev separation, explicit selected space and a wide
nonnegative radius. Independent public-contract rule/emitter proof uses an actual readiness contract
with no source geometry or scheduler change. The fixture/provider lives in ExtensionProof; tests
install authored scenario entities and issue ordinary commands. No client-side spread/recovery rules.
Existing SignalGateCondition owns shared predicate authoring/resolution/validation/evaluation.

The explicit per-Update budget admits one candidate or due delivery per unit, with saved stable source
round-robin and captured scan high-water membership. It limits admission work, not world size, total
CPU or arbitrary provider cost. Source discovery/candidate lookup still enumerate actual entities.
Positive delays and pre-captured source admission prevent same-Update cascades. Delivery rechecks
actual source episode, target episode and current eligibility, then verifies existing incident Raise.
Existing incident response390/work400 remain the physical recovery owners. Isolation can stop spread
through the already authored phase-hold signal; no persisted safety-state mirror.

System codec1 alone saves/hashes the exposure book. Structural decode stages before replacing live
state; OnLoaded validates live lifecycle/history after WorkBoard restores. Stateless emitter codec1,
view1; existing incident/work/resource/spatial/network layouts unchanged. Save62 is the sole current
format; no old reader, compatibility alias, scheduler duplication or default environmental inputs.
Player views exclude foreign pending targets and global scan IDs; explicit observer diagnoses both.

## Commands run and final evidence

```
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Incident propagation: authored rules and bounded admission"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Incident propagation: delayed exposure, physical isolation and saved waves"
```

- Final build: zero warnings/errors, 4.65 s.
- Contracts/admission: **61 passed, 0 failed**, 494 ms, one exact suite.
- Physical/waves: **46 passed, 0 failed**, 733 ms, one exact suite.
- Total: **107 passed, 0 failed, 0 aborted suites**. Only these two exact names executed; earlier repair
  runs used these same names, not extra suites.35 other current identity assertion files were updated
  mechanically and compiled, **not executed**. Prior P12-A3 130/P12-A2 93 checks are historical only.

## Proven in these fixtures

- Explicit aligned positive durations, required scope/space/source/kind/conditions/budget and rejection
  of unknown keys, bad identities/missing metadata; no inferred legacy/default authoring.
- One-candidate budget, stable round-robin, captured scan membership and next-cycle late entities.
- Native geometry and independent rule/emitter on nonspatial sources through public contracts.
- Immutable private reads, observer diagnostics and no read-time state mutation.
- Staged negative epoch, wrong rule and duplicate pending decode rejection preserves prior live state.
- Malformed/mutating observations fault the owning phase, refuse target damage and forbid healthy saves.
- Signed wide timestamps, positive scheduling/delivery, multiple waves without same-Update chaining,
  no duplicate active-target Raise, physical native response enrollment and no distant/default spread.
- Real responder travel/material consumption; completed isolation cancels delayed exposure even across
  ordinary response cancellation/resumption. Real cleanup/restart clears pending causes/schedules.
- Independently repaired target epochs reject old exposure; removed identities are never replaced by
  new occupants. Same-company scope refuses nearby foreign targets; authored any-company pressure
  works without exposing foreign pending identities to private source views.
- Saves during scans, pending delivery, physical isolation, intervening target recovery and subsequent
  waves continue hash-identically when two sessions update interleaved.

## Open gates — not claimed complete

Native weather/terrain/network-topology propagation, arbitrary non-phased incident sources, probability,
severity, damage/condition/environment accounting, preparedness/maintenance, failed response policy,
network segment cuts and arbitrary operation migration remain open. Geometry movement re-evaluation
is source-only, not exercised here. Broader custom-provider purity/receipt faults, replay/lockstep/fuzz/
stress/deadlock/allocations/full regression and end-to-end P12/P1–P12 acceptance remain unverified.
The provider fixture has no source geometry but still uses the existing spatial Colony scenario; this
is not a new full mapless-session proof. No Oil/Tycoon/host/demo/platform/Draw changes. Development-
lifecycle guidance kept the stage, implementation, focused evidence and master tracker synchronized.
