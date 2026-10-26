# P8-F1 — File-authored Planet X production

Status: scoped verified on unchanged2.408.0; FINAL48/0/0 in4889ms, ONE NEW exact case only. Only original P8.acceptance.001 checked;221/154checked/67open. Eight builds/five attempts preserved; no older runtime. [Evidence](../../docs/migration/P8_PLANET_X_PRODUCTION_VERIFICATION.md). Full goal/plan remain OPEN; no staging/commit/push.

## 0. Problem statement

Original P8.acceptance.001 still requires new Planet X smelting and count-based assembler definitions from explicitly listed files. Existing ITransformationDefinition/ITransformer, native TransformerDeclaration/Transformer, resource ports, unit packing and physical supply owners already support such content; no separate crafting subsystem is appropriate. The P8 future mixed-port sketch is not accepted runtime JSON.

## 1. Goals

1. Load distinct new resource, recipe, logistics/work and world documents from one explicit master, using the current accepted contracts.
2. A fictional ten-kg ore batch physically earns six kg alloy and four kg slag after thirty seconds. A count-based three-blank/one-reusable-fixture assembly earns one finished part after sixty seconds.
3. Normal player queues, physical supply, WIP/tool holds, pause, full multi-output backpressure, ordinary export and cancellation use existing owners.
4. Checkpoints, private detached facts, twin hashes and replay preserve real progress/resources. No budget/Tycoon or platform logic.

## 2. Design

Add a production-only engine-side proof pack under tests/ExtensionProof/content/planet-x-*.json, with distinct planet-x-production-master.json. The existing planet-x-master.json/planet-x.json extraction proof remains a separate content composition using the same native owners. Master selects existing nine resource/process/motion/work/setup/access and PointModule contracts only. PointModule provides the already proven independent spatial provider, not a new production owner. WorldSetup authors one source, smelter, assembler, receiver and carrier. Resource-only mode has no optional properties/accounts/progression/controllers/knowledge. Units distinguish nondiscrete kilograms and discrete item counts; separate explicit capacity limits govern real stores/cargo/WIP. Every count resource explicitly measures one fictional kg per item; shared source/receiver stores admit sixty kg, carrier ten kg, smelter twenty-one kg including its reusable crucible. Assembly/WIP also use discrete count capacities. Quantums are five game seconds; durations and hourly movement rates are authored, never driver formulas.

Smelting has separate input/tool/product/waste port identities sharing the sole actual native store, explicit reusable crucible, and two outputs. Assembly uses discrete blanks/fixture/finished parts and its own count WIP. Existing automatic supply physically dispatches the ordinary carrier. Queue three smelts and two assemblies; the shared smelter store cannot accept the third raw batch while holding twenty kg of products plus the crucible. Original ten kg ore therefore remains upstream, never a partial result; ordinary ten-kg alloy hauling makes room for physical supply and the whole third conversion. Completed-WIP output blocking across separate stores is outside this slice. Cancel a partially worked third assembly and reuse returned raw inputs for a later successful batch. Drivers issue commands/read facts/advance Update only; no direct stock writes or result execution.

These are fictional authored gameplay yields, not engineering data. No assumption that every optional technology, fuel, catalyst or utility is required.

## 3. API surface

Existing ContentSet/GameFactory, seat commands/previews/results, TransformationManagementView, InventoryView, WorkView, immutable snapshots, Update/Clock, saves/replay. No new framework API, coordinator switch or protocol expected.

## 4. Files to change

Add explicitly owned master/resources/recipes/world/work/logistics documents and one focused PlanetXProductionTests case. Embed the pack and register only that case. Existing production sources change only for an evidenced owner defect. Update stage/evidence/parent P8/master/audit.

## 5. Error handling

Missing listed file, unit/resource/port or malformed recipe refuses before Initialize; no loader fallback. Fractional item quantities refuse; negative authored durations refuse. Positive durations round through the existing session converter; they need not be exact multiples of the quantum. Physical supply/output blocking preserves original WIP and holds, never inventing outputs. Cancellation follows return_inputs and retains physical position. Private queues/tools/ports do not leak.

## 6. Compatibility

Development only; no old reader, compatibility shim or parallel crafting/catalog owner.

## 7. Verification

Completed slice build plus ONE NEW exact bounded case only, same-case repair allowed. Physical stages bounded to512Updates; child30000ms/termination5000ms. Close P8.001 only if both named authored workflows genuinely load and execute; original broader P8 gates stay open. No historical/family/full/Oil/Tycoon/platform runtime.

## 8. Risks

Native tool holds stay in their actual source; WIP contains raw inputs, not an invented combined total. Shared product/waste ports must not double-count capacity. One-dimensional space uses one mobile carrier to avoid opposing-lane collision assumptions. Read declared public facts rather than assuming exact system settlement from command enqueue. Allocation/stress proof is not claimed.

## 9. Out of scope

Future mixed-port schema, remote/entity tools, continuous wear, fuel/utilities/technology prerequisites, arbitrary result providers/global failure/stress/full P8, protected G19 files, Oil/Tycoon/platform/rendering, staging/commit/push.

## 10. Execution order

Inspect current declarations/unit/capacity/result and public views; author explicit new pack; exercise simultaneous normal queues and physical saved workflows; fix only genuine existing-owner defects; preserve all attempts/final source identity; update original requirement text unchanged and scoped evidence.
