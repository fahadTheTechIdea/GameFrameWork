# P8-D2c — JSON-authored reusable tool ports

## Problem and goals

Input/output routing uses actual named stores, but TransformationsTools.ToolStore, startup admission
and saved WIP validation still assume one local tool inventory. Replace that path in place.

1. Require exact tool_ports maps ({} when empty); resolve named compatible capacity/packing metadata.
2. Admit complete feed/tool/first operating demand together through the sole ResourceTransactions.
3. Preserve original tool resource/port/endpoint bindings for each active batch or continuous stream.
4. Read actual endpoint-owned receipts through the existing transaction, not a second resolver/book.
5. Complete/cancel releases tools at their original endpoints; full returns retain all commitments.
6. Save/hash/replay, queue checks and private immutable facts validate the same original bindings.

## Design and API

Extend ITransformationDefinition/TransformationDef with ToolPorts, using the same required-map reader,
exact resource matching, named port resolver and packing validation used by inputs/outputs. Generalize
the existing transient claim-bill grouping for feed, tools and local operating demand.

Replace ITransformerTools.ToolStore with immutable ToolSources references. Transformer.SyncWip owns
the selected reference list with the existing execution identity, not quantities/promises. Detached
TransformationToolsSnapshot contains Sources, actual Receipts and Held; remove singleton endpoint/
version properties. Active snapshots use original bindings even if a current port is rebound.

IResourceTransaction.ReadClaims(company,endpoint,owner) exposes a detached ResourceClaimSnapshot from
the original guarded ClaimAccess/reservation checkpoint. Typed missing/access/compatibility status,
endpoint version and actual records; no live stock handle, mutation, substitute endpoint or new owner.
Tool lifecycle uses existing whole ReleaseClaims/conversion. ValidateWipClaims accepts only the WIP
and original tool endpoints/resources; exact tool validation rejects missing/extra/foreign claims.

## Files and errors

Modify Core/ResourceTransactions, Standard/ResourceTransactions, ResourcePorts, TransformationContracts,
Transformations, TransformationsWip, TransformationsTools, module/save/view identities and private view.
Migrate authored JSON directly through the one-time AuthorTransformationPorts tool; no old parser or
compatibility API. Update compile consumers. Use the existing shared port proof and explicit master
JSON for a new scoped player-only workflow rather than copying common resource/stock definitions.

Missing/extra/unknown/incompatible tool maps refuse before Update. Complete startup shortage waits
without acquiring a prefix. Reusable tools cannot be spent by unrelated movement. Lost active actual
receipts or corrupt saved bindings fault; no recreated/free tool or synthetic compensation. Cancellation
blocked on input return retains tools until the complete return commits, then releases them once.

## Execution and verification

Implement shared contracts/read-only claim observation; authored validation; native startup/original
tool references, complete release and checkpoint validation; immutable views/identities; explicit JSON
migration and compile consumers; one new bounded exact test; tracker/evidence reconciliation.

Prove remote tools, same-resource feed/tool aliases, complete shortage, original binding changes,
timed processing, blocked output/cancellation retention, saved batch/continuous continuation, private
facts and replay. Driver issues ordinary commands/views/Update only; no direct stock or progress rules.
Build for compilation and run only the new case; no full/broad/Oil/platform runtime.

## Remaining full plan

Scoped implemented/verified on2.308.0/save146/replay1/transformer8/view12. The same new bounded case
passed154 checks initially,156 after complete authored-capacity refusal coverage. Final clean build
5.34s. Three builds include one corrected test import error; no failed runtime or old/full suite rerun.
Twenty-one JSON files migrated24 tool maps/20 native aliases; two added recipes reuse the existing
input master/common content. Original tools persist through selected-port changes and saved full
returns, then release from actual books once. [Evidence](../../docs/migration/P8_TOOL_PORT_ROUTING_VERIFICATION.md).

That historical tool checkpoint left operating inputs local. [D2d](P8_OPERATING_PORT_ROUTING_DESIGN.md)
now revises the same generic utility contract for named multi-endpoint demand, not a parallel model.
Catalysts/entity tools, scoped physical automatic supply/export, dynamic endpoint
recovery, provider/global purity/accounting/stress/full P8/P1–P12 gates remain required. No publication
until the full goal AND plan are actually finished and verified.
