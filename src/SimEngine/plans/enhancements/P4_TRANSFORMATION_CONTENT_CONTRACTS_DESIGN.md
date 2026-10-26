# P4-G20 — Actual transformation recipe contracts

Status: scoped-verified actual recipe migration, 2026-10-09; core2.353.0/save154/replay1.
One NEW exact case final50/0/0 in1048ms after named proof-query repair;29 inputs frozen07:10:18UTC.
[Evidence](../../docs/migration/P4_TRANSFORMATION_CONTENT_CONTRACTS_VERIFICATION.md). Full P1–P12 open.

## 0. Historical pre-G20 problem / specification

Standard/TransformationContracts.cs already defines ITransformationDefinition, but transformer
catalogs, admission/queue/execution, gates, WIP/tools/flow/operating inputs, dispatch, knowledge/
technology and management views select TransformationDef. Independent JSON recipe definitions
cannot participate in the original physical processors. No Tycoon TransformationDef consumers were
found in the inspected excluded subtree; this migration can proceed without its pending resource bindings.

## 1. Goals / design

Extend the EXISTING ITransformationDefinition with immutable IContentDefinition ownership, category
and existing duration conversion. Extract original recipe parser/resolver to sole TransformationDeclaration.
Native TransformationDef and independent Def providers delegate all common authored metadata to it.
Original ResolvedContent aliases/lookup/fingerprint and TransformerManager own composition/execution.
Validate actual recipe ownership/nonempty coherent metadata before components and Initialize.

Migrate all original engine/public-proof concrete recipe consumers to actual interface, retaining only
the native class and native section registration. No native casts/fallback/shim/second queue/catalog,
copied WIP/resource/rate math or alternate JSON loader. Existing optional cost field/behavior unchanged;
no business expansion, mandatory money, Tycoon files, Oil/platform/Draw/adapter work.

## 2. Original owners / API

Standard/Transformations.cs / TransformationContracts.cs / TransformationModule.cs;
TransformationsWip.cs / TransformationsTools.cs / TransformerFlow.cs / TransformerOperatingInputs.cs;
ResourcePorts.cs / TransformationStaffing.cs / TransformationSignalGates.cs / TransformationDispatchEffect.cs;
Technology.cs / KnowledgeVisibility.cs / Views/TransformationManagementView.cs / Content/ResolvedContent.cs;
public ExtensionProof/TransformerProof.cs. Actual recipe aliases feed these same providers.
Breaking build identity changes, original state/view formats remain unless actual persisted shape changes.

## 3. Verification

ONE NEW exact bounded case: `Transformation content contracts: authored independent recipe and physical manufacturing`.
Explicit master-listed recipe JSON is the sole definition of the chosen ID; native fixture recipe
entry absent. Reuse existing capacity/physical setup and engine-owned processing/hauling.
Prove actual independent alias/ownership/resolution, ordinary command/preview, physically supplied WIP,
elapsed duration/tool return/entity output, saved phases/pause/replay/private facts and malformed unused
content/incorrect references before Initialize. Bounded stages and child process diagnostics.
Only this NEW case may be repaired/rerun. No old/family/full/Oil/Tycoon/platform runtime.

## 4. Execution order

Shared reader/contract → existing consumer migration/central metadata validation → explicit independent
JSON/public composition facts/thin player proof → compilation/new exact runtime → source freeze/evidence/
master/audit reconciliation. User/repository focused gates override skill full-regression recommendations.

## 5. Remaining acceptance

[P4-G20a](P4_TRANSFORMATION_METADATA_DESIGN.md) subsequently consolidates resolved startup/tool/rate/route
metadata validation for native and independent unused recipes. Its111/0/0 checks are metadata proof,
not direct continuous production or arbitrary callback safety. G20 runtime was not rerun on2.354.0.

Continuous/direct-provider/tool/operating-port/network/result/technology/knowledge physical breadth,
arbitrary provider faults, cancellation/removal/concurrency/lockstep/fuzz/soak/performance/full gates open.
Resource catalog migration and three-file Tycoon scope decision remain separate and pending.
Commit/push only AFTER genuine full goal AND plan completion and verification.

[Parent](P4_REUSABLE_MODULES_DESIGN.md), [transformations](P8_TRANSFORMATIONS_DESIGN.md),
[tracker](MASTER_TRACKER.md).
