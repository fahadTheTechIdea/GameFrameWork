# P10-A7 — Infrastructure service contracts

## 0. Problem Statement

`InfrastructurePlans.cs`, `LineEffects.cs`, `InfrastructureView.cs` and `LineSystem` still select concrete `LineService`. Native cursor inspection also escapes into the portable line declaration. This prevents a composed infrastructure implementation from satisfying existing consumers through contracts.

## 1. Goals

1. Construction declarations use `ILineConstruction`, including actual execution binding and identity.
2. Earned segment/final effects use `ILineWorkEffects`; removal requires only existing `INetworkTopology`.
3. The existing system invokes `ILineSimulation` at its unchanged order420. No second update loop.
4. Flow views use existing `INetworkDiagnostics`; no concrete service binding remains.
5. One new exact bounded physical proof covers contract aliases, removal/reconstruction, path/flow privacy, partial saves and replay. Historical cases compile only.

## 2. Design

Add focused service interfaces in `Standard/InfrastructureWork.cs`. Move the original marker binding/identity bodies from `LineDeclaration` into `LineService`; retain the sole path identity algorithm in the declaration. `Begin` returns its original posted `Order`, eliminating declaration-side cursor access. Convert original segment/finish implementations to instance methods using their own session. The same owner implements construction, effects, simulation, topology, diagnostics, endpoint and activity-source contracts and its original persistence codec. Service aliases register that instance once.

Simulation methods have no supplied World: the composed owner uses its actual session. `LineSystem` retains original Advance -> RebuildDirty -> Flow order. Flow diagnostics return a typed refusal, clear output and enforce original content/company visibility. No graph, cursor, map, stock, scheduler, state schema or clock changes.

## 3. API surface

Engine service contracts only; no HTTP/platform API. `ILineConstruction`: CheckIntent/Check/PlanPath/Begin/BindExecution/ExecutionIdentity. `ILineWorkEffects`: CheckSegment/CompleteSegment/CheckFinish/Finish. `ILineSimulation`: RebuildDirty/Advance/ValidateMarkers/Flow. `INetworkDiagnostics.CopyFlow` returns Code. Remove concrete `LineService` service registration without a shim.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| New | src/Beep.SimEngine/Standard/InfrastructureWork.cs | 45 | medium |
| Modify | Standard/Networks.cs, InfrastructurePlans.cs, InfrastructureModule.cs, LineEffects.cs, NetworkFlow.cs, NetworkDiagnostics.cs | 110 | medium |
| Modify | Views/InfrastructureView.cs | 12 | medium |
| Modify | historical tests using concrete service registration | mechanical | low |
| New | tests/Beep.SimEngine.Tests/InfrastructureServiceContractTests.cs | 160 | medium |

## 5. Error handling matrix

| Operation | Before | After |
|---|---|---|
| Segment/finish without provider | requires concrete LineService | requires ILineWorkEffects before Initialize |
| Removal without topology | requires concrete and topology | requires INetworkTopology only |
| Invalid cursor/graph/order | native declaration inspection | original corruption checks inside sole owner |
| Unknown/private flow content | view checks concrete owner | diagnostics returns UnknownDef with empty destination |

## 6. Backward compatibility

Development only; no compatibility registration, adapter or duplicate implementation. Historical native-owner tests locate the existing implementation through provider inventory, not a restored concrete binding.

## 7. Verification

Build completed slice once. Run only the new exact bounded case with outer30000ms, termination5000ms and output30000 characters. Repair/rerun that same case only if needed. Full/old/family/Oil/Tycoon/platform runtime remains unauthorized/open. Preserve actual attempts and executable inputs in evidence.

## 8. Risks

Changing alias registration must not duplicate the persistence codec or endpoint provider. Bound lifecycle methods must retain their original order and session. The focused proof does not establish arbitrary custom provider purity or broader non-tile infrastructure.

## 9. Out of scope

Tycoon and protected clock files, renderers, platforms, additional transport, non-tile corridor implementations, full regression, commits/push until the entire original goal/plan finishes.

## 10. Sub-task execution order

1. Define contracts and move existing bodies into their existing owner.
2. Migrate original consumers and remove concrete registration; mechanically migrate historical test inspection.
3. Add one ordinary-player proof and bounded diagnostic stages; build and run only that case.
4. Update stage evidence, master tracker and completion audit without closing compound original gates.
