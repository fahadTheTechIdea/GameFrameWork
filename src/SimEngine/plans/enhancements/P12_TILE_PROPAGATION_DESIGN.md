# P12-B5 — Layered tile incident eligibility

## 0. Problem Statement

Standard/IncidentPropagationContracts.cs embeds tile rule authoring in TileIncidentPropagationRuleDef. Distance and existing signal gates cannot select the same ITileConditionDefinition predicates used by navigation/construction across actual source and target footprints. Delayed exposures must re-observe earned map changes, not cached initial terrain.

## 1. Goals

1. One TileIncidentPropagationDeclaration owns native/direct JSON, actual space/radius, policy and footprint eligibility; original propagation/episode/work/resource/map/clock owners retain state.
2. Required source_tiles/target_tiles objects select explicit all/any footprint coverage and actual typed condition references; no optional fallback or fabricated weather.
3. Reuse the existing TileConditionReferences reader and TileConditionContracts observation boundary. Reject missing, duplicate, unknown, foreign-space and invalid coverage authoring before Initialize, unused included.
4. Prove real machine-earned layer change affects due-time eligibility; native/direct registrations have identical chronology and partial saves.
5. Physical response still requires real material supply and timed machine work through ordinary commands. No gameplay in drivers.

## 2. Design

Extract TileIncidentPropagationDeclaration to Standard/TileIncidentPropagation.cs. Resolve references first; native/direct ValidateResolvedCore delegates Validate only after all definitions resolve, preventing registration-order dependence. Native TileIncidentPropagationRuleDef delegates; independent registrations delegate the same declaration, not copy algorithms. ValidateSource/ValidateBinding retain actual selected tile space and policy requirements.

TileFootprintConditionDeclaration in Standard/TileLayerConditions.cs reads match and conditions, resolves actual aliases, requires the selected map/space, and evaluates every footprint cell through existing TileConditionContracts.Matches. All means every footprint cell satisfies every referenced predicate; any means at least one cell satisfies every referenced predicate. Explicit all+[] is unconditional, any+[] refuses. Existing nonempty navigation/construction reference semantics remain.

Tile rules retain existing signal/presence/distance admission, then source and target footprint predicates. Existing IIncidentPropagationState/IncidentPropagationView gains ReadEligibility returning detached per-target facts through the same original observation. It reads only authorized source/target entities, never advances exposure records, and clears facts on refusal. Incident propagation view schema becomes2; persistent exposure schema stays1. New typed source-terrain/target-terrain blockers are truthful. Existing propagation scanning and due delivery both call the same Observe; no exposure codec or journal change.

## 3. API surface

Public TileIncidentPropagationDeclaration.Read/Resolve/Validate/ValidateSource/ValidateBinding/Observe and TileFootprintConditionDeclaration.Read/Resolve/Validate/ValidateBinding/Matches. Native JSON requires source_tiles and target_tiles, e.g. {match:all,conditions:[]}. Existing IIncidentPropagationRule and ITileConditionDefinition remain authoritative. IIncidentPropagationState.ReadEligibility(source, capability, target, viewer, out IncidentEligibilitySnapshot) and IncidentPropagationView.ReadEligibility return typed admission and detached episode/rule/clock/current eligibility facts. No platform/rendering API.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| New | Standard/TileIncidentPropagation.cs | 100 | medium |
| Modify | Standard/IncidentPropagationContracts.cs | replace native authoring | medium |
| Modify | Standard/TileLayerConditions.cs | 100 | medium |
| Modify | Standard/IncidentPropagationSystem.cs, IncidentPropagationModule.cs, Views/IncidentPropagationView.cs | 70 | medium |
| Modify | historical incident-propagation.json | explicit selectors | low |
| New | tests/ExtensionProof/TileIncidentPropagationProof.cs | 100 | medium |
| New | tests/ExtensionProof/content/terrain-incidents.json | 60 | medium |
| New | tests/Beep.SimEngine.Tests/TileIncidentPropagationContractTests.cs | 220 | medium |
| Modify | kernel build/protocol and case registration | mechanical | low |

## 5. Error handling matrix

| Operation | Before | After |
|---|---|---|
| Footprint conditions | unsupported | actual predicate aliases over settled map |
| Missing/foreign selection | distance-only | content refuses before Initialize |
| Queued terrain change | not considered | due-time original observation rechecks |
| Predicate mutates/throws | existing guarded consumers | same actual map/clock observation, first fault preserved |

## 6. Backward compatibility

Development only; no compatibility path or legacy reader. Update the one historical native JSON fixture explicitly; its cases compile only, not run.

## 7. Verification

Build the completed slice. Run ONE NEW exact case only, outer30000ms/owned termination5000ms/output30000 characters, physical stages bounded256 Updates with actual exposure/order/stock/layer diagnostics. Repair/rerun the same case if required. No historical/family/full/Oil/Tycoon/platform runtime. Preserve all attempts/builds and executable hashes.

## 8. Risks

Any must mean one cell satisfies the whole conjunction, not different cells satisfying different predicates. Layer changes are earned by original work; delayed receipts remain original episode guarded state. Observations are scoped protection, not an arbitrary CLR sandbox. Callback cleanup must not mask the first exception.

## 9. Out of scope

Climate/weather generation, scientific spread chemistry, line-of-sight/intervening corridor sampling, private layers, alternate spatial providers, business/default money/Tycoon, rendering/platforms, new clocks/stores/schedulers, broad runtime and publication before full goal/plan completion. Protected resource-catalog authority remains pending.

## 10. Sub-task execution order

1. Extract sole rule declaration; add shared explicit footprint selectors and actual context admission.
2. Migrate native registration/historical JSON and add direct content registration.
3. Use master-listed JSON and ordinary commands to prove physical map change, delayed exposure, supplied response, saved twins/pause/replay/privacy and invalid unused authoring.
4. Update stage/evidence/master/audit without changing original compound requirements or claiming full completion.

## Delivery — scoped, not aggregate completion

[Verification evidence](../../docs/migration/P12_TILE_PROPAGATION_VERIFICATION.md). Core2.404.0/save154/replay1/incident-propagation-view2; original exposure schema1 and episode/work/map/resource owners unchanged. ONE NEW exact case FINAL272/0/0 in4654ms; three same-case attempts and four clean builds retained. Native/direct all/any footprints, actual cut/reconstruction, fresh delayed receipts, real water/timed response, per-target private facts and saved twins/replay are narrowly verified. Resolution-order validation and original first-fault cutoff are fixed in the existing paths. Climate/private/non-tile/custom/global/performance/full gates remain OPEN; protected Tycoon authority remains pending. No executable edits after the final pass; no publication.
