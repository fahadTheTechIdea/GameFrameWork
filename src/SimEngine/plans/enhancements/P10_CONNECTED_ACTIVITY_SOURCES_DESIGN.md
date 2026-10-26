# P10-A20: connected activity resource sources — Design Document

## 0. Problem Statement

Standard/NetworkResourceRequirements.cs currently hardcodes target selection in its native resource requirement; generic requirements cannot reuse its strict JSON/network binding without copying the algorithm. Initial unavailable connections are collapsed into an empty candidate list rather than exposing the original typed connection blocker. Workflows cannot author a worker-connected source.

## 1. Goals

1. One strict network source declaration resolves JSON and actual carried resources; native or independent requirement contracts reuse it without scheduler changes.
2. Target and worker sources resolve their actual connection through original INetworkDiagnostics. A disconnected source blocks without creating stock or choosing another endpoint.
3. Existing retained claims require the exact original endpoint. Pauses, cancellation, physical removal, save/load and replay retain original work/resource ownership.
4. New exact public proof exercises real target claims protecting native/point physical corridor removal, plus worker source selection and disconnected target blockers. No driver state writes.

## 2. Design

Extract NetworkResourceSourceDeclaration within existing Standard/NetworkResourceRequirements.cs. It owns Read/Resolve/ValidateBinding/CopySources/CheckSource and typed connection evaluation. Native NetworkResourceRequirementDef delegates to it and the existing ResourceRequirementDeclaration through ResourceWorkRequirementDef; TargetNetworkResourceRequirementDef selects context.Target, WorkerNetworkResourceRequirementDef selects context.Worker and declares IWorkerWorkRequirement. Target guard returns original typed connectivity blockers; worker selection remains candidate-driven so it does not require an already assigned worker during global admission.

InfrastructureModule registers worker_network_resource_requirements alongside the current target section. Independent non-friend JSON requirement implements existing IResourceWorkRequirement and uses both shared declarations, not native inheritance. Original WorkBoard scheduler, NetworkService, receipt books, geometry, resources, Game.Update and clock remain untouched owners.

## 3. API surface

Shared NetworkResourceSourceDeclaration Read(JsonReader), Resolve(ContentResolver,IResourceWorkRequirement), ValidateBinding(World,IResourceWorkRequirement), Evaluate(context,source), CopySources(context,source,destination) and CheckSource(context,source,endpoint). Worker requirement JSON uses the same explicit network/use/native bill fields as target requirements. No command, host gameplay, runtime registry or saved schema is added.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---:|---|
| Modify | Standard/NetworkResourceRequirements.cs | 100 | medium |
| Modify | Standard/InfrastructureModule.cs | 1 | low |
| Modify | Kernel/SaveGame.cs and independent protocol identity | 2 | low |
| New | ExtensionProof independent source metadata, actual JSON/master declaration | 90 | medium |
| New | public connected-source acceptance driver | 200 | medium |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Unknown network or uncarried resource | ContentException before initialization |
| Missing selected diagnostics or wrong space | Composition refusal, no default geometry |
| Unconnected target/worker | Original network.disconnected or source unavailable blocker; no stock claim |
| Pending topology | Original network.topology-pending |
| Retained endpoint changes | Typed missing source, never substitute new receipt authority |
| Physical removal affects live pool receipts | Original active-promises refusal before geometry change |
| Provider faults | Original terminal observer/commit behavior, never a normal fallback |

## 6. Backward compatibility

Development-only; no compatibility path, legacy source parser, alternate state or second source-selection algorithm.

## 7. Verification

One solution build, necessary targeted compile repairs only. One NEW bounded exact case with all attempts retained, public commands/views/Update, saved twins/replay/isolation and native/point minute/half geometry. Older/family/full suites remain unauthorized; no broad claim from this proof.

## 8. Risks

Worker source availability can change during physical travel; reevaluation must use actual location and original receipt identity, not a retained connection approximation. Unavailable candidates must never create stock. Keep requirement/global versus candidate validation responsibilities distinct.

## 9. Out of scope

Named dynamic network resource ports, arbitrary custom topology/fault/stress breadth, Oil content, business/Tycoon, platforms/rendering and publication.

## 10. Sub-task execution order

1. Extract shared source declaration; add worker selection and target typed evaluation.
2. Author independent requirement and actual master-listed content, without engine scheduler edits.
3. Verify native/point physical claim waiting, ordinary release, worker selection and unavailable sources.
4. Update evidence/master/current audit; preserve original gates unless their full literal scope is proven.

## Current scoped result

Core2.469.0/save154/replay1; FIRST2/1, then59/3,137/1,137/1, FINAL176/0/0 in35341ms. Four native/point minute/half target flows preserve live claims and earned physical removal, release through ordinary cancellation/natural completion, conserve split quantities and expose typed disconnected blockers. Two moving-worker flows prove actual connected claims and disconnected refusal; independent metadata uses shared declarations without native inheritance. Saved twins/private facts/canonical completion history/replay/isolation pass.

One clean solution8.47s/four necessary targeted compiles5.50s/4.34s/4.56s/6.39s; all attempts retained, no solution rerun or executable edit after FINAL. [Evidence](../../docs/migration/P10_CONNECTED_ACTIVITY_SOURCES_VERIFICATION.md). Original221/185checked/36open unchanged; additional195. Named dynamic network ports/all-topology/elapsed-fluid/arbitrary-provider/native-worker/fault/stress/performance/global/full gates and publication remain OPEN.

Metadata closure at 2026-10-12 00:59:52 UTC:1645 source anchors/13 quickstart anchors/887 selected executable inputs, zero SHA drift;2833 paths/998 Markdown/8626 local links, zero broken;18 master rows/zero malformed; thin driver boundary matches0;git diff --check exit0 (34 warning lines),dirty2821/staged0. Original221/185checked/36open unchanged; additional195. Protected GameClock and Tycoon hashes unchanged, goal_complete false. Seven prior selected executable inputs changed, four new executable inputs, three existing production files changed/no new production file; no executable edit after FINAL. Original requirement literals/checkboxes/line anchors unchanged. All five attempts retained; no older/family/full runtime.

