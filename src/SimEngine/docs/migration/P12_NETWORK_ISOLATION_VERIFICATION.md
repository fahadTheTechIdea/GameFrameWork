# P12-A3 — Shared gates and network connection isolation: scoped evidence

Date: 2026-10-06. Source `beep-simengine-2.96.0`, save61. New stateless signal_network_gate component1;
infrastructure-view3/transformation-view8. Existing incident/work/line/inventory/transformer state codecs
remain unchanged. The current build reader remains sole authority; no old API facade or save shim.

## Implemented ownership

- SignalGateCondition owns immutable strict JSON condition parsing, stable ordering, exact signal
  kind/unit/source resolution, boolean/range evaluation and detached condition projection once.
- ExecutionGateSnapshot/ConditionSnapshot/Evaluation replace process-specific fact names outright.
  ExecutionGateContracts owns stable named-provider ordering, actual capability/session binding,
  pure-read identity/version checks and complete condition/aggregate validation for both gate contracts.
- ITransformationGate remains typed to TransformationDef; INetworkFlowGate is typed to NetworkDef.
  Their callers validate actual catalog identity, not a guessed target string or another registry.
- INetworkPortsDefinition exposes the existing PortSpec metadata. NetworkPortsDef<TPorts> is the
  shared authoring owner; native PortsDef and independent ProofNetworkPortsDef reuse it. Runtime
  LineService.Flow continues to use INetworkPorts, never a concrete native Ports requirement.
- Native signal_network_gate explicitly names supported networks and compatible named conditions.
  IncidentSignal retains sole derived active/phase-hold meaning; no extra hold, valve, timer, operation
  enabled-state, progress codec, stock balance, network service or incident scheduler.
- Each actual connected port checks readiness before push/pull acquisition. Existing endpoint books,
  transactions, resource quanta, rates, ownership, pools and topology remain unchanged. Unselected
  connections proceed normally; a closed connection neither clears promises nor destroys stock.
- Physical work400 precedes flow420, so its completed isolation affects the ensuing network phase.
  Process350 retains next-update observation. Gate evaluation never advances work or runs effects.
- InfrastructureView.ReadGates returns owner/explicit-observer-only immutable facts from execution's
  same evaluator. Unknown/hidden/unsupported connection requests refuse explicitly.
- Infrastructure module requirements remain Work/Inventory/TileNavigation; no new mandatory operation
  dependency. That declaration is checked, but a wider no-operations network composition matrix was
  not executed in this slice. Existing P12-A2 mapless proof is historical, not rerun.

## Exact verification

Working directory: `C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine`.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Network isolation: shared gate contracts and strict admission"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Network isolation: physical phase closure and saved flow recovery"
```

Final build: zero warnings/errors,2.35s. Contract suite: **41 passed/0 failed**,402ms.
Physical suite: **89 passed/0 failed**,686ms. **130 checks/two exact suites**, no aborts.
Only these gates were run, repeated during compile/fixture refinement; no third/family/full regression.

Contracts check native/independent metadata, exact catalog aliases, stateless gate payload, shared
condition type, immutable pure private facts, explicit observer, unsupported/unknown connection,
unselected network applicability, saved identity and strict missing/duplicate/invalid JSON refusal.
Actual numeric signal work/control crosses the inclusive authored range with stable condition ordering.

Physical proofs cover positive-time flow before isolation, actual crane isolation, parallel blocked
cleanup, separate push and independent-provider pull closure, ordinary CancelOrderCommand/same-order resumption,
retained pool capacity receipt/endpoint/five-tile topology, complete steel100 conservation and actual
spare10→6 response consumption. Cleanup/restart restores timed verified transfers. Active emergency
policy closes immediately while an unrelated public-neutral network continues. Permanent response
failure remains closed. An unavailable sensor preserves probe.sensor-offline (not an invented range
blocker), saves identically and reopens only after real provider input. Interleaved saved twins match.

Six public-contract fault modes verify refusal before stock acquisition and sim.lines faulting:
entity mutation, null facts, forged capability, invalid inapplicable blocker, inconsistent aggregate,
and capability identity mutation without Touch. Faulted sessions cannot save or resume. These probes
do not prove arbitrary cross-entity/provider purity or the complete future fault matrix.

A bounded300-tick process reuse probe in the same physical suite starts a real batch, physically
isolates it, preserves progress and WIP2, and proves saved twins through the shared validator. It is
scoped refactor evidence, not a rerun of previous complete transformation/incident suites.

Initial compile attempts used a metadata interface without IComponentDefinition, token equality via
an unsupported operator, and a nonexistent runtime component removal method. Those are corrected:
the public metadata contract inherits IComponentDefinition, receipts use Equals, and the invalid
repair provider is explicitly absent in authored content. No runtime fallback/removal shortcut.

30 other current-build assertions migrate/compile only. Earlier P12-A1/A2, process, network topology,
visibility/knowledge/resource suites were not rerun; their historical counts are not current regression.

## Open scope

This is endpoint-connection isolation, not network component/segment geometry cuts. Other direct
endpoint users are not implicitly gated. Arbitrary non-transformation operation gates, propagation,
condition/maintenance/preparedness/environment, retry, broader providers/replay/stress/allocations and
full P12/P1–P12 acceptance remain open. No Oil/Tycoon/host/demo/platform/Unity/Godot/Draw work.

Development-lifecycle guidance aligned stage, code and tracker; the user's two-suite limit overrides
generic full-regression advice. Dirty unrelated work was preserved; no resets, commits or exports.
