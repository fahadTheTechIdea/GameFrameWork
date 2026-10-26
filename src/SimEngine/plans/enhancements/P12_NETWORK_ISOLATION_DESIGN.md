# P12-A3 — Shared gate facts and incident-controlled network connections

Status: focused verified, 2026-10-06; broader P12 remains partial.
Parent: [P12](P12_INCIDENTS_ENVIRONMENT_DESIGN.md).

Build2.96.0/save61; new stateless signal_network_gate codec1, infrastructure-view3 and
transformation-view8; existing state layouts unchanged. Clean build; **130 checks/two exact suites**
(41 contracts;89 physical), zero failures/aborts.
[Scoped evidence](../../docs/migration/P12_NETWORK_ISOLATION_VERIFICATION.md).

## 0. Problem

Standard/Networks.cs LineService.Flow moves physical stock through INetworkPorts without a safety
check. P12-A2 only gates transformers. Copying TransformationSignalGates.cs condition parsing and
TransformationGates.cs fact validation into networks would create competing implementations.

## 1. Goals

1. One signal-condition authoring/evaluation owner serves process and network gates.
2. One detached gate fact/aggregate validator serves their distinct typed execution contracts.
3. Explicit JSON selects actual network connections and named signal sources. Incident active or
   physically completed phase signals block both push and pull before acquisition/spending.
4. Closing a connection retains real pool/entity stock, promises, topology and endpoint identities.
   Actual response resolution restores normal timed flow; cancelled/failed work cannot unlock it.
5. Private immutable diagnostics and saved twin runs use the same readiness as native execution.

## 2. Design

Cleanly rename process-specific condition/fact types to SignalGateCondition, ExecutionGateSnapshot,
ExecutionGateConditionSnapshot and ExecutionGateEvaluation; remove old names, no facade or reader.
Keep ITransformationGate's typed operation contract and add INetworkFlowGate for NetworkDef targets.
ExecutionGateContracts owns stable capability ordering, pure-read checks and complete fact validation.
Typed callers validate their actual catalog target, then delegate aggregation to that sole owner.

SignalGateCondition owns strict immutable condition parsing, kind/unit resolution, named source
validation and boolean/range evaluation. Existing SignalTransformationGateDef selects supported
transformations; new SignalNetworkGateDef selects networks. Both use the same condition owner.

Add INetworkPortsDefinition metadata exposing existing PortSpec connections; native PortsDef
implements it. Network gate authoring validates exact supported NetworkDef references through that
contract, never a concrete Ports lookup. New stateless signal_network_gate component implements
INetworkFlowGate. No operation enabled-state mirror, stock/progress/connection-state codec, extra
network service or incident scheduler. Infrastructure does not gain a mandatory operations dependency.

NetworkPortsDef<TPorts> is the single shared authoring owner for native and independent port components;
PortsDef is its native leaf. Public proof runtime substitutes INetworkPorts without concrete Ports.

LineService.Flow evaluates all matching gates before either direction's reserve/commit path.
Blocked ports are skipped normally; other ports/networks continue. Network pools and geometry stay
with LineService/ResourceInventoryState. Work400 precedes infrastructure420; a completed physical
isolation node affects the ensuing flow phase. This differs deliberately from transformation350,
which observes work completion on its next update. No gate advances the incident or performs effects.

InfrastructureView.ReadGates exposes owner/explicit-observer-only detached facts for actual supported
connections, using execution's same evaluator. Hidden/unknown definitions and unsupported connections
refuse explicitly. Native topology and external endpoints remain authoritative.

## 3. API

INetworkPortsDefinition.Connections; INetworkFlowGate.ReadGate(World, NetworkDef);
shared ExecutionGate* facts; InfrastructureView.ReadGates(EntityId, string, out ExecutionGateEvaluation).
JSON signal_network_gate requires id, nonempty unique networks, and nonempty unique conditions.
Signals require exact provider schema/kind/unit as before. No instant shutdown/restart command.

## 4. Files

Refactor Standard/TransformationSignalGates.cs and TransformationGates.cs into shared helpers without
retaining duplicate implementations. Update typed process contract/view and public proof callers.
Extend Standard/Capabilities.cs, Networks.cs, InfrastructureModule.cs and Views/InfrastructureView.cs.
Add Standard/NetworkSignalGates.cs, master-listed non-oil proof JSON and two exact test suites.
Build identity2.96.0/save61; new stateless component codec1, infrastructure-view3 and transformation-view8;
existing state layouts unchanged. Update stage/evidence/master/parent/README/help/AGENTS together.

## 5. Errors and risks

Missing/wrong/duplicate IDs, networks or providers fail authoring. Runtime unavailable signals preserve
their typed blockers, never grant readiness. Foreign catalog/entity identities and malformed or mutating
gate providers fault rather than silently transfer. Immutable diagnostics cannot create receipts.
Do not imply this connection valve splits network geometry or blocks unrelated direct endpoint users.

## 6. Compatibility

Clean current contracts only; remove former process-specific fact names. No legacy aliases/shims.

## 7. Verification

Build and run only:
`Network isolation: shared gate contracts and strict admission`;
`Network isolation: physical phase closure and saved flow recovery`.
Exercise strict JSON, deterministic condition order, private/pure detached facts, external network-port
metadata, both directions, selected-vs-unrelated networks, stock/promise/topology conservation,
cancel/failure retention, actual physical response and saved twins. Include process-gate reuse checks
inside these two gates after the shared contract refactor; earlier suites compile only, not rerun.

## 8. Risks and non-goals

Network component/segment topology cuts, arbitrary non-transformation operation migration, propagation,
condition/maintenance/preparedness/environment, retry, broad provider/purity/replay/stress/full P12
remain open. No Oil/Tycoon/host/demo/platform/Draw changes; no platform size cap.

## 9. Execution order

1. Extract the sole shared condition/fact owners and migrate their current callers.
2. Add strict typed network metadata/gate plus native flow check/private view.
3. Prove physical incident closure/recovery and independent provider substitution through public contracts.
4. Run the two exact gates and build; record scoped evidence without claiming broad completion.

All four slice steps are implemented and focused verified. An unavailable real sensor retains its typed
blocker through save/load and resumes flow after actual provider recovery. Six dishonest gate modes
(mutation, null facts, forged identity, invalid applicability, inconsistent aggregate, identity mutation)
fault the actual line execution phase before stock acquisition. Earlier incident/process/network suites
compile only;30 build identity assertions migrate without rerunning their suites. No broad completion claim.
