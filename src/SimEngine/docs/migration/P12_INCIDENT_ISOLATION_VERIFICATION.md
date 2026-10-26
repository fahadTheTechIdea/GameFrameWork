# P12-A2 — Incident signals and physical process isolation: scoped evidence

Date: 2026-10-06. Source build `beep-simengine-2.95.0`, save61. New stateless `incident_signal`
component codec1/signal-schema1 has no saved payload. Existing incident component1/system1/view3,
native work and transformation layouts remain unchanged. No legacy reader or compatibility path.

## Implemented boundary

- Public `IPhasedIncidentDefinition` describes the existing named workflow. JSON explicitly binds
  incident source, exact workflow and phase; missing/incompatible references fail authoring.
- Native `IncidentSignal` implements existing typed automation contracts. `incident.active` reflects
  the episode; `incident.phase-hold` requires an active episode and all selected native phase nodes
  completed. Both are boolean, unit.boolean, schema1; no independent hold/progress state.
- `IncidentPhases` remains the pure lifecycle reader and shared completed-node query. WorkBoard owns
  all actual response progress, effects and history. The phase view uses the same query.
- Existing `signal_transformation_gate` and process execution checks alone enforce startup,
  ongoing-work and completion readiness. No duplicate operation gate, scheduler or enabled-state mirror.
- Transformation order350 precedes response390/work400. Physical isolation completed at400 gates
  the next process update; it does not undo prior processing or create same-tick feedback.
- Cancellation and permanent response failure cannot release the phase hold. Actual whole-response
  resolution releases it. Manual enable cannot bypass readiness; manual disable survives resolution.
- Incident registration adds no mandatory OperationsModule or spatial provider. Actual selected
  signals/gates require their declared modules, not fabricated defaults.

## Exact commands and final results

Working directory: `C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine`.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Incident isolation: authored phase signals and gate admission"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Incident isolation: timed process shutdown and saved restart"
```

Final build: zero warnings/errors, 1.93s. Contract suite: **41 passed/0 failed**,467ms.
Physical suite: **52 passed/0 failed**,997ms. **93 passed across exactly two suites; no aborts**.
These same gates were repeated during fixture repairs; no third/family/full suite was run.

Contract checks cover exact named aliases, no signal payload, immutable/private pure observations,
foreign-session refusal, invalid IDs/source/workflow/phase/schema/metric/unit/kind, no hidden enabled
field, and structurally equal but wrong workflow refusal. Mapless/no-operations composition uses
ordinary native positive-duration responder work and saved twins; no spatial/economy dependency.

The master-listed non-oil JSON proof uses a native batch/continuous processor and the existing timed
leak response. Tests cover immediate active-signal startup refusal, physical phase-gated interruption,
preserved WIP/tools/progress, cancellation with same-order resumption, parallel cleanup waits,
permanent failure retention, actual stock/output conservation, manual intent, physical restart,
detached captured facts and interleaved save/load hash identity. Stage waits are bounded to300 ticks
with process/response diagnostics on timeout.

Intermediate test authoring incorrectly used Bill's indexer with a ResourceId and an unsupported
worker-role category field. The fixture now uses AmountOf and strict existing JSON schema; no runtime
fallback was added. Failed intermediate attempts are not counted as successful evidence.

29 other build-identity assertions migrate and compile only, including the preceding incident-phase
suite. Previous P12-A1's110 checks and visibility/knowledge/response/resource gates were not rerun;
their historical evidence is not current regression proof.

## Remaining scope

Network disconnection, arbitrary non-transformation operation isolation, propagation, condition/
maintenance/preparedness, environmental accounting, failed-response retry policy, independent phased
provider matrix, broader replay/fault/stress and full P12/P1–P12 acceptance remain open. Reusable rules
stay inside SimEngine; no Oil/Tycoon/host/demo/platform/Unity/Godot/Draw changes.

Development-lifecycle guidance aligned design, implementation and tracker evidence. The user's exact
focused-gate limit overrides the skill's generic full-regression suggestion.
