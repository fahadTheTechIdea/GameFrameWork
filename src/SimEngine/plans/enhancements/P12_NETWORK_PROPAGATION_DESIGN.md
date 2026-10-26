# P12-B3: Network-connected incident propagation — Design Document

## 0. Problem Statement

IncidentPropagationSystem already owns bounded scanning and delayed exposures, but built-in rules only test tile distance. A hazard cannot follow actual connected network topology or respect endpoint isolation without custom game logic. NetworkDiagnostics.ReadQuantity contains the authoritative connection admission but does not expose detached connection identity.

## 1. Goals

1. Expose company-private pure settled connection facts through INetworkDiagnostics and InfrastructureView, reusing one connection admission path.
2. Author a network propagation rule in explicitly master-listed JSON. Bind the actual INetworkDefinition and require its diagnostics provider before Initialize.
3. Select only endpoints of the same connected pool, respecting existing network gates at both observation and due-time revalidation.
4. Native and independent rules delegate one shared declaration/algorithm; retain original exposure/incident/work/resource/clock owners.
5. One new bounded case proves delayed spreading, disconnected components, source/target isolation after scheduling, physical response waits, saved twins/pause/replay/privacy and strict pre-world refusal.

## 2. Design

Extend original INetworkDiagnostics with ReadConnection(EntityId,string,PlayerId,out NetworkConnectionSnapshot). Snapshot contains the actual pool endpoint, company and version, no live handle. Refactor LineService's existing connection admission into one private observation path used by both quantity and connection reads. Preserve settled topology, declared connection, ownership, actual codec and geometry guards. Never rebuild topology or transfer stock during reads.

NetworkIncidentPropagationDeclaration owns strict network/policy JSON, resolution, provider/source binding validation and ordinary eligibility. NetworkIncidentPropagationRuleDef implements IIncidentPropagationRule on Def. Independent content may delegate the same public declaration without native inheritance. IncidentPropagationModule registers the section but does not globally require infrastructure: only selecting network-dependent content requires it. Observe uses INetworkDiagnostics, compares actual pool identities, then existing NetworkGateContracts and policy conditions. The existing propagation system re-observes when exposure is due, so isolation or a changed connection prevents stale delivery. No second graph, pool, incident queue or scheduler.

## 3. API surface

New immutable NetworkConnectionSnapshot and INetworkDiagnostics.ReadConnection; thin InfrastructureView.ReadConnection. New public NetworkIncidentPropagationDeclaration and native rule section network_incident_propagation_rules. Existing IIncidentPropagationRule/emitter contracts, commands and state codecs remain authoritative.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| Modify | Standard/NetworkDiagnostics.cs and Views/InfrastructureView.cs | 65 | medium |
| New | Standard/NetworkIncidentPropagation.cs | 100 | medium |
| Modify | Standard/IncidentPropagationModule.cs | 1 | low |
| Modify | Kernel/SaveGame.cs and current protocol expectation | 2 | low |
| New/modify | Existing master-listed proof composition, one JSON file, one new exact test | 220 | low |
| Modify | Master/phase/audit evidence | metadata only | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Missing/foreign network reference | Typed content refusal |
| Selected rule without INetworkDiagnostics | Before-Initialize content failure, no fallback topology |
| Source without actual declared connection | Before-Initialize content failure |
| Missing endpoint, undeclared/disconnected/pending connection | Existing typed connection blocker |
| Distinct settled pools | network.disconnected |
| Source/target isolated | Actual gate blocker; no exposure raise |
| Saved pending exposure becomes stale | Existing due-time re-observation refuses |
| Invalid provider read | Original observation fault; no healthy checkpoint |

## 6. Backward compatibility

Development only, no shim/legacy. Core2.400.0/save154/replay1. Connection view schema increases; existing persistent layouts unchanged.

## 7. Verification

Build completed slice and run ONE NEW exact case only: Network incidents: connected topology and isolation govern delayed saved exposures. Outer30000ms/owned termination5000ms/output30000chars; bounded per-stage updates with actual incident/connection diagnostics. Repair and rerun this same case only. No historical/family/full/Oil/Tycoon/platform runtime.

## 8. Risks

A rule demonstrates connectivity pressure, not a physical fire/chemical/weather formula. Native tile topology is still optional content, not a universal spatial default. Shared endpoint gates intentionally control this rule's network propagation as well as resource flow. Cross-company public networks and actual removal/reconnection stress remain broader follow-ups unless explicitly covered.

## 9. Out of scope

No fabricated weather, contamination amounts, incident severities, business rules, protected clock/Tycoon edits or platforms. Full P1-P12 compound gates remain open; publishing waits for full completion.

## 10. Sub-task execution order

1. Extend/refactor sole guarded connection reader.
2. Add shared authored eligibility and native registration.
3. Add independent delegate/master JSON and thin normal-command proof.
4. Build, run only new case and freeze input identity.
5. Reconcile tracker/evidence without weakening original requirements.

## P12-B3 delivery — scoped connected propagation

[Stage](P12_NETWORK_PROPAGATION_DESIGN.md), [evidence](../../docs/migration/P12_NETWORK_PROPAGATION_VERIFICATION.md). Core2.400.0/save154/replay1/infrastructure-view6. Sole guarded connection admission publishes detached actual pool identity and also serves original quantities. Native/direct master JSON propagation declarations use the same network algorithm; original exposure/incident/work/resource/clock owners retain state. Delayed spreading requires same actual connected component and open source/target network gates at scheduling and delivery. FINAL125/0/0 in823ms (PID40788 Exited0/985ms observed), seven clean builds final3.95s; six same-case attempts retained. Native content without its active topology provider refuses before Initialize; independent metadata without diagnostics remains source-reviewed, not executed. Saved pending/raised/isolation twins, pause/replay/privacy/stock conservation pass narrowly. Native startup geometry is not physical corridor construction proof. Cross-company/public/removed/reconnected topology, weather/chemistry, custom diagnostics/provider/purity/global/performance/full gates remain OPEN. Compound requirements/checkboxes unchanged; publication deferred.
