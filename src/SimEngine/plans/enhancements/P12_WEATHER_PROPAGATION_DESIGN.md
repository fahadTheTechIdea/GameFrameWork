# P12-B6 — Authored weather eligibility on the one game clock

## 0. Problem Statement

Standard/IncidentPropagationContent.cs owns common tile/network eligibility but has no selected weather dependency. Existing tile footprint and connected-pool rules already re-observe due exposures; hosts must not supply a weather flag or a second timeline.

## 1. Goals

1. Add optional contract-based weather content with stable authored phases, explicit conditions/durations/repetition and no fabricated weather or clock.
2. Native and independent metadata reuse one WeatherTimelineDeclaration; current weather is derived from existing clock/content, with no independent persistent state or update scheduler.
3. Common incident policy optionally selects an actual weather provider and allowed authored condition IDs. Require selected reader/provider before Initialize, unused loaded definitions included.
4. Both scheduling and due delivery use original common policy. Tile distance/footprint and network topology/isolation still govern their existing rules.
5. One exact new bounded case proves native/direct weather, tile/network delays, changed weather, saved continuation/pause/replay/privacy/isolation, malformed authoring and scoped provider faults.

## 2. Design

Weather.cs defines IWeatherDefinition, detached WeatherObservation/WeatherSnapshot, WeatherContext, one strict timeline declaration and actual-content WeatherContracts. Each phase has a stable ID, condition code and positive exact GameDuration. Ordered phase durations are validated against the selected quantum before Initialize; duplicate IDs, empty cycles, impossible durations or unknown JSON refuse. Repeat is explicit. A nonrepeating exhausted timeline returns weather.exhausted, never holds or invents a final condition.

WeatherReader is a stateless selected service and composition validator. It observes immutable actual provider identity, declared conditions, result shape and original clock/provider/map scopes; reads reject recursion/faults, publish detached facts only after success and respect content visibility. WeatherModule registers native JSON and WeatherView. No new world/entity/episode/resource state, clock, gameplay Update loop or codec.

IncidentPropagationPolicyDeclaration reads optional weather {provider,conditions,blocker}; absence means no weather dependency, not a weather default. Present selection is explicit/nonempty/unique, resolves actual IWeatherDefinition and validates the selected IWeatherReader. CheckConditions uses the source owner's private reader after existing signal conditions. Existing propagation owner calls it at initial scan and delayed delivery. Existing tile/network algorithms remain authoritative.

## 3. API surface

IWeatherDefinition.ConditionIds/ValidateBinding/Observe(WeatherContext); IWeatherReader.Read(provider,viewer,out WeatherSnapshot); WeatherView.Read. Public WeatherTimelineDeclaration.Read/ValidateBinding/Observe supports independent metadata without native inheritance. JSON weather_timelines has repeat and phases [{id,condition,duration}]. Shared policy weather selection refers to its actual provider and allowed declared conditions.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| New | src/Beep.SimEngine/Standard/Weather.cs | 260 | Medium |
| New | src/Beep.SimEngine/Standard/WeatherModule.cs; Views/WeatherView.cs | 45 | Low |
| Modify | src/Beep.SimEngine/Standard/IncidentPropagationContent.cs | 70 | Medium |
| New | tests/ExtensionProof/WeatherProof.cs and content/weather-incidents.json | 95 | Low |
| New | tests/Beep.SimEngine.Tests/WeatherIncidentPropagationTests.cs | 220 | Medium |
| Modify | build/protocol, case/content registration and checkpoint metadata | mechanical | Low |

## 5. Error handling

Missing/foreign/undeclared provider or condition, missing selected reader, invalid phase shape and nonexact duration refuse before Initialize. Unknown/hidden public reads withhold facts. Exhausted authored timeline and disallowed current weather return typed blockers. Provider exceptions/mutation/malformed observations latch the original fault, publish no facts and prevent saves. Same original exposure receipt is removed when due-time weather refuses; later favorable weather earns a fresh full delay.

## 6. Compatibility

Development only; no compatibility or fallback path. Existing rule documents without a weather selection remain intentionally weather-independent. Build advances to2.427.0; save154/replay1/episode2/exposure1 unchanged.

## 7. Verification

One stage solution compile, ONE NEW exact case: Weather incidents: authored clock phases govern tile and connected delayed exposures. Outer60000ms/owned termination5000ms/output30000 characters; physical waits256 Updates. Observe handles to terminal, retain every same-case repair build/attempt. No historical/family/full/Oil/Tycoon/platform runtime. Do not check compound P12.003 until the actual covered scope proves it; broad gates remain open.

## 8. Risks

Authored categorical weather is a deterministic game policy, not a scientific atmospheric solver. Late-phase arithmetic must not overflow future coordinates; observations carry elapsed/remaining steps rather than an invented global timestamp. Scoped guards are not arbitrary CLR purity. Independent providers must use their actual immutable aliases; no mutable cross-session gameplay state.

## 9. Out of scope

Atmospheric chemistry, rendering/platforms, default maps/weather, OilFieldDays, Tycoon, protected files, a second propagation/clock/resource owner and broad regression/publication before the full goal and plan finish.

## 10. Execution order

- [x] Implement sole weather declarations/read boundary/view/module.
- [x] Extend common policy with selected weather and strict admission.
- [x] Author native/direct proof through master JSON and ordinary public inputs.
- [x] Compile and run one exact bounded case; retain all attempts.
- [x] Refresh actual tracker/audit/evidence and remaining requirements.
- [ ] Full regression remains unauthorized and unproven.
