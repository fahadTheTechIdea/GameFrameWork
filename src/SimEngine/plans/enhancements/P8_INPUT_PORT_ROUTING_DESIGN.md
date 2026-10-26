# P8-D2b — Native JSON-authored input-port routing

## Problem and goals

Before this stage, native batch input movement, continuous feed and cancellation assumed `Entity.Get<IInventory>()`.
D1 already provides named stock bindings; D2a supplies complete mixed movement admission. This stage
uses those same owners for explicitly authored input routes, not a second manufacturing scheduler.

1. Require `input_ports` for every batch input/continuous feed resource; reject missing/extra/unknown
   or incompatible routes before Update. No implicit local port, old parser or compatibility reader.
2. Admit all selected input stores plus existing local tools/first operating draw together. Batch
   inputs move into the existing protected WIP only after the complete set fits.
3. Persist only original resource/port/endpoint references for an active batch. Cancel returns the
   complete WIP to those endpoints atomically; capacity/missing/access waits retain actual WIP/tools.
4. Continuous feed uses each selected source's native quantum and aggregate actual outbound claims;
   full output or missing feed consumes no prefix or earned progress.
5. Publish immutable company-private per-port facts through the same management view/flow snapshot.
   Save/load/hash/replay use the existing execution and stock owners.

## Design and contracts

Generalize the existing output-only route/binding/snapshot into shared transformation resource-port
types. One guarded named-port resolver, per-endpoint bill grouper and rate calculator serve inputs
and outputs. `TransformationDef.InputPorts` is immutable JSON content, resolved against exact native
input identities. Batch/continuous definitions share this required mapping shape:

```json
"input_ports": { "parts.raw": "material.input", "process.water": "water.input" }
```

Explicit `inventory_port` aliases select the entity inventory in existing content; independent
engine modules may bind their own actual stores through `IResourcePortDefinition/IResourcePort`.
No new resource registry or stock implementation. The existing local tool/operating contracts remain
the next migration, rather than being silently described as named-port support.

`Transformer.SyncWip` also owns the active batch's original input endpoint references. These are
execution bindings, not balances or reservations. WIP validation checks their exact authored
resource/port shape and canonical identity; missing return endpoints cause typed waits, not fallback.
Continuous streams have no refundable input WIP and keep no original-source reference ledger.

`ITransformerFlow` loses its singular InputStore; flow facts expose InputPorts and OutputPorts through
the same shared immutable snapshot type. Inputs use cumulative ceiling/native packing at their
actual sources, outputs cumulative floor at their destinations. Existing `ResourceTransactions`
owns full claim acquisition/conversion and aggregate batch movement. Utility burns and tool holds
retain their original sole owners. No additional update loop, remainder counter, money requirement
or host-side gameplay.

Automatic logistics must not send a remote-port shortage to the entity inventory. Existing local
requests remain truthful; unavailable scoped-port supply reports an explicit blocker while retaining
the execution. General physical scoped automatic supply/export remains an open P8/P9 migration,
not a teleportation fallback or a newly declared final-state limitation.

## Files, errors and execution order

Update ResourcePorts, TransformationDef/contracts, native WIP/start/flow and validation, the existing
management view, module codec/view metadata, breaking EngineInfo identity and authored JSON.
Migrate prior consumers directly; do not add deprecated members or old save/schema translation.

Unknown/missing/mismatched routes: ContentException. Actual shortage: materials/flow-input blocker.
Return-full/missing/incompatible/not-owned: typed returning wait, no partial transfer or release.
Provider identity/purity/exact mutation violations: fault session; no resumable partial checkpoint.

Execution order: shared binding/authoring contracts; whole startup/WIP/return; continuous feed;
immutable views/durable validation; explicit JSON migration; one new bounded player-only proof;
tracker/evidence reconciliation. Build only for compilation and run only the new exact bounded case.

## Verification and remaining plan

Scoped implemented/verified on2.307.0/save145/replay1/transformer7/view11: one new bounded exact
case passed178 checks on its first runtime, zero failures/aborts. Final clean build1.85s. Original
source bindings survive a selected-port change and a full-store cancellation/save; material returns
only after all original destinations fit. Continuous feed/output waits do not consume a prefix.
Twenty existing JSON files now author22 exact input maps/19 explicit inventory aliases; one shared
port proof document owns common definitions rather than duplicating them. The one-time
AuthorTransformationPorts.ps1 authoring tool is not an engine loader or runtime compatibility path.
[Observed evidence](../../docs/migration/P8_INPUT_PORT_ROUTING_VERIFICATION.md).

Prove mixed solid/liquid sources, shortages, native timed batches/continuous flow, full outputs,
atomic returning waits, original source references, cancellation and saved/replayed continuation.
Tests issue commands/read views and call engine Update; they own no materials, progress or decisions.

Full P8/P1–P12 remains open: named tool/utility routes, catalysts/entity tools, scoped physical
automatic supply/export, dynamic provider recovery, global purity, accounting/stress/full gates.
No full suite, Oil consumer, rendering/platform changes or publication in this stage.
