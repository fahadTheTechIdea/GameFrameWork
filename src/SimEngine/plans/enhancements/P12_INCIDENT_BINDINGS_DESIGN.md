# P12-A5 — Contract-owned reusable incident episode and signal bindings

Status: scoped verified121/0/0, ONE NEW exact bounded case. Original P1–P12 gates remain open.
[Verification](../../docs/migration/P12_INCIDENT_BINDINGS_VERIFICATION.md).

## 0. Problem Statement

`src/Beep.SimEngine/Standard/IncidentWorkflows.cs` binds `Incident` to `Component<IncidentDef>`;
`IncidentSignals.cs` binds `IncidentSignal` to `Component<IncidentSignalDef>`. Independent JSON
component definitions cannot reuse these existing episode and signal owners without native inheritance.
The earlier workflow/completion contracts do not remove these two remaining dependencies.

## 1. Goals

1. Independent JSON episode and signal component definitions implement focused contracts and reuse
   the original runtime components, schemas, work graph and clock.
2. One strict declaration per binding owns JSON parsing and reference resolution. No copied parser,
   native-definition fallback, parallel episode state or signal cache.
3. Every loaded binding, including unused entity definitions, validates actual owner, runtime,
   workflow, phase, source and state aliases before Initialize.
4. Public ordinary incident, work and automation commands prove real travel, material consumption,
   elapsed response, interruption, private phases, saved continuation and subsequent episodes.

## 2. Design

Add `IIncidentEpisodeDefinition : IPhasedIncidentDefinition` for the original episode runtime's
explicit state bindings and pure composition validation. Do not force these fields onto arbitrary
custom `IPhasedIncidentDefinition` implementations. Add `IIncidentSignalDefinition` for exact named
source/workflow/phase metadata. Native definitions and independent module definitions delegate
to `IncidentEpisodeDeclaration` and `IncidentSignalDeclaration`.

`Incident` and `IncidentSignal` derive nongeneric `Component` and expose their actual typed contract
definition. They keep the existing original episode codec2 and stateless signal codec1. The original
`IncidentResponseSystem` also validates loaded bindings after composition, not a new system/service.
Actual metadata and lifecycle reads use the existing scoped `EntityObservation`; response completion
still invokes the existing board-bound final effect. No alternate scheduler, phase progress or clock.

## 3. API surface

Focused content contracts and shared declarations only. Ordinary commands/views remain unchanged.
JSON fields remain episode `id/workflow/states.active` and signal `id/source/workflow/phase`.
Independent component kinds and signal-provider schemas are explicitly declared by the module/master.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---:|---|
| New | src/Beep.SimEngine/Standard/IncidentBindings.cs | 170 | medium |
| Modify | src/Beep.SimEngine/Standard/IncidentWorkflows.cs | 65 | medium |
| Modify | src/Beep.SimEngine/Standard/IncidentSignals.cs | 65 | medium |
| Modify | src/Beep.SimEngine/Standard/Incidents.cs | 40 | medium |
| New | tests/ExtensionProof/IncidentBindingsProof.cs | 65 | low |
| New | tests/Beep.SimEngine.Tests/IncidentBindingTests.cs | 150 | medium |
| Modify | tests/Beep.SimEngine.Tests/ControlContentContractTests.cs | 5 | low |
| Modify | tests/Beep.SimEngine.Tests/Program.cs | 1 | low |

## 5. Error handling matrix

| Invalid input/provider | Result |
|---|---|
| Missing/wrong workflow, source, phase, state or undeclared schema | Authoring/composition refusal before Initialize |
| Wrong runtime/definition pairing or fabricated alias | Composition refusal, never native substitution |
| Pure callback mutates clock/entity/provider facts or throws | Existing observation fault cutoff; no checkpoint or compensation |
| Cancelled current response | Original same-episode work resumption; no repeated material grant |

## 6. Backward compatibility

Development only. No legacy reader, compatibility shim or duplicate subsystem.

## 7. Verification

Build when the complete slice is ready. Run only ONE NEW exact bounded incident-binding case;
repairs may rerun that same case. Do not run earlier incident/environment/Tycoon/Oil/platform families.
Record actual attempts, terminal process status and limitations before marking this slice verified.

## 8. Risks

Actual metadata aliases must remain immutable. Scoped provider checks are not a CLR sandbox or global
rollback guarantee. Native and independent runtime parity needs physical evidence, not class searches.

## 9. Out of scope

Tycoon scope decision G19; Oil/platform/Draw changes; propagation breadth, exhaustive private-codec
faults, scale/performance/lockstep/fuzz/soak/full acceptance gates. No publication until full goal/plan finish.

## 10. Sub-task execution order

1. Replace concrete episode/signal bindings and share strict declarations.
2. Integrate all-loaded binding validation in the existing response system and scoped lifecycle reads.
3. Author independent module bindings and one thin public-player physical proof.
4. Build and run only the new bounded case; record evidence and update the authoritative tracker.

## 11. Current evidence

Core2.383.0/save154/replay1. Four clean builds; four attempts of only the same new case.
Final121 checks, zero failures/aborts in969ms; PID42676 observed Exited, exit0,1104ms,
no truncation or observer error. Earlier attempts repaired proof-only state-code/explicit probe
authoring mistakes; no old/native family runtime was rerun. Native and independent declarations
have identical earned episode/phase timing, stock20→12→4 and physical worker4→20.
Private signals/phases, original same-episode cancel/resume, paused clock, saved raised/partial/
interrupted/resolved continuation, new episodes, replay and session isolation are narrowly proven.
Seventeen unused-definition/JSON/alias/runtime-pair/pure-callback refusals occur before Initialize.
Broader actual live metadata/private-codec faults, global rollback, propagation, performance,
lockstep/fuzz/soak/full gates remain open. No staging/commit/push.
