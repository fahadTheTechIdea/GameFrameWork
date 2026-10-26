# P4-G49: Authored line definitions — Design Document

## Current M67: topology is not material stock — 2026-10-22

Native2.722/save161/replay2. Shared LineDefinitionContracts.ValidateMetadata now rejects quantity
layers for topology AND owner channels before Initialize. Associated resource links remain valid;
they describe a layer, not physical quantities. One validator covers native/direct declarations and
geometry providers; there is no parallel native-only check or material conversion through stamping.
The eight-configuration focused case reproduces prior8/8 admission failures and passes16/0 after
repair; existing native/direct physical plans222/0/two public replays remain valid. [Full current
evidence and source/accounting scope](../../docs/migration/P3_SPATIAL_PUBLICATION_FAULTS_VERIFICATION.md#current-m67-spatial-publication-ownership-and-material-semantics--2026-10-22).
No original broad gate closes; no financial/platform/consumer change or full test run.

## Historical G49 design

## 0. Problem Statement

Every network, corridor service, work cursor, plan binding and infrastructure view required concrete LineDef. Independent line content needed native inheritance or a duplicate corridor implementation. Line metadata, portable intent and template authoring must be one engine-owned responsibility.

## 1. Goals

1. Expose ILineDefinition and migrate all engine consumers to actual content aliases.
2. Extract one LineDeclaration containing strict metadata, plan/template intent and the existing prototype binding.
3. Admit every loaded line, unused included, before Initialize.
4. Retain original physical DAG/work/material/map owners and one game clock.
5. Prove native/direct construction parity through normal player commands and immutable views.

## 2. Design

ILineDefinition extends IContentDefinition, IPlanTemplateDefinition and IActivityWorkflowBinding. It declares layer IDs/aliases, optional ownership, medium/value, explicit activity and terminal binding, material envelope, work capacity, completion effects and CreateIntent. Native LineDef is only normal JSON registration. Direct Def implementations delegate LineDeclaration; no abstract compatibility base or native-only consumer overload.

LineDeclaration combines the previous metadata and InfrastructurePlans partial implementation. It owns Read/Resolve, plan identity, path intent, submission/activation, execution identity and portable template capture/placement. LinePlan remains the original per-cursor immutable geometry binder: authored parallel dependencies stay intact, subsequent roots wait on the prior terminal, and completed tile prefix derives from actual node states. WorkBoard, WorkSystem, LineWork, TileMap, worker inventories, claims, effects and sole GameClock own all persistent gameplay. No second scheduler, progress counter, stock, map or catalog.

LineDefinitionContracts centralizes the original segment admission rules. It checks actual Id/Module/catalog aliases; byte line and optional same-space integer owner layers; positive value/known medium; actual non-orderable unrestricted workflow; plan/final/supply identity; machine ancestry and joined terminal; segment/final effects; original material envelope and capacity admission. Full validation checks the complete one-segment prototype preserves authored executors, requirements, durations, multipliers, effects, roles and bound dependencies. All loaded definitions validate after resolved lifecycle and before freeze; original infrastructure and line-service composition revalidate frozen aliases under existing provider/clock observations before Initialize.

Selected CheckIntent/Check use the original scoped provider/clock observations via LineDefinitionContracts.Read. It can capture a selected entity codec when supplied, but this proof exercises clock/exception faults only. No global rollback, private CLR sandbox or comprehensive callback-purity claim.

Network declarations, generated cursor definitions, topology APIs, execution/effects, work plan catalog and immutable infrastructure views resolve ILineDefinition. Native line-effect and port metadata remain separate unfinished seams; tile-only physical implementation does not imply non-tile topology support.

## 3. API surface

Normal lines JSON retains activity, segment_node, work_capacity, layer, optional owner_layer, owned, value, medium and finish_effect. All IDs are explicit and raw validated before lookup. Independent modules register their own section and master-listed JSON file using the same declaration. Public LayLineCommand, SubmitPlanCommand, template commands, cancellation, clock/Update and immutable WorkView/TileMapView remain the player boundary. No rendering/Draw/platform adapter.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| New | Standard/LineDefinitions.cs | medium |
| Modify | Standard/Networks.cs, InfrastructurePlans.cs, InfrastructureModule.cs | medium |
| Modify | Standard/NetworkDefinitions.cs, Capabilities.cs, Views/InfrastructureView.cs | medium |
| Modify | Content/ResolvedContent.cs | medium |
| New | tests/ExtensionProof/LineDefinitionContractsProof.cs | low |
| New | tests/ExtensionProof/content/line-definition-contracts.json | low |
| New | tests/Beep.SimEngine.Tests/LineDefinitionContractTests.cs | medium |
| Modify | tests registration, embedded resource and authored-segment factory | low |

Core paths are relative to src/Beep.SimEngine. Historical line/network test references migrate mechanically and compile only. Engine build/protocol fixture become2.392.0; original state layout remains save154/replay1.

## 5. Error handling matrix

| Input/read | Result |
|---|---|
| Missing/empty/unknown reference or extra legacy flat field | Typed ContentException; no fallback |
| Null/foreign alias, bad identity/value/medium/supply envelope | Refusal before Initialize, unused included |
| Wrong terminal, unjoined DAG, wrong prototype graph | Original shared graph/segment validator refuses |
| Live selected layer getter throws/pauses clock | Original observation fault; save/hash refuse |
| Cancellation after earned segment | Original claims released; earned prefix/material accounting retained |

## 6. Backward compatibility

None required. No legacy flat duration reader, native-only overload, duplicate model or compatibility shim. Native registration delegates the same authoring implementation. Exact build/content identity remains authoritative; no old-build translation.

## 7. Verification

THREE clean builds16.20s/7.13s/6.54s, zero warnings/errors. ONE NEW exact case, THREE attempts:22 passed1 failed865ms;160 passed2 failed1519ms; FINAL164/0/0 in1388ms. Final child PID43256 Exited0, observed1523ms. [Actual evidence](../../docs/migration/P4_LINE_DEFINITION_CONTRACTS_VERIFICATION.md).
No old/family/full/Oil/Tycoon/platform runtime; full regression stays unauthorized and OPEN.

## 8. Risks

Scoped observations do not cover every plan/template/route/topology/effect callback or nonparticipating private state. Actual codec helper support is not proof that every consumer supplies its target. Wider ownership, operating-area, route-rule, congestion, utility, promise, lockstep, fuzz/soak/performance and full acceptance matrices remain OPEN. Native byte tile layers and line effects remain existing dependencies, not provider-neutral transport support.

## 9. Out of scope

Ports/line-effect authoring-contract migrations, non-tile infrastructure, all callback/property purity, unused bad DAG breadth, global rollback, compatibility, platform/Draw/Oil changes and full original P1-P12 acceptance. G19 authority unanswered; protected Tycoon and GameClock untouched. Historical network proofs not rerun on2.392.0.

## 10. Sub-task execution order

1. Extract contract and single metadata/intent/template declaration; migrate every consumer.
2. Centralize actual alias/segment/prototype validation and scoped selected admission reads.
3. Master-list direct independent JSON definitions; one thin player proof shares existing fixture factory.
4. Build/run only the new exact bounded case; repair proof assumptions and strengthen prototype admission.
5. Preserve every attempt, post-green hashes and original gate text/states; update tracker/audit.

## 11. Delivery status

Scoped verified, not whole-plan completion. Native/direct machines physically move and execute parallel inspect/prepare then installation on each of three contiguous tiles. Stock40 remains40 while tools are held, becomes38 after one segment and34 after the corridor. No early topology. Sole-clock pause, partial/completed saves, twin continuation, replay and shared-factory isolation succeed. Portable paused plans/templates contain intent only. A second corridor cancellation preserves its first earned tile, no second tile, stock32 and exact save/load. Twenty-seven unused malformed definitions refuse before Initialize; two live layer read faults refuse save/hash.

Selected378 executable inputs hashed AFTER final green at2026-10-09 19:05:17 UTC, not pre-run/full transitive closure. No executable edits afterward. Original221 compound gates keep their text/checkboxes; full goal stays active. No staging/commit/push until the entire original goal and plan are actually complete.
