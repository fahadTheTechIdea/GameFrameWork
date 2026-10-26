# P4-G21 — Contract-based physical processor declarations

Status: scoped verified, 2026-10-09; core2.356.0/save154/replay1. ONE NEW case96/0/0 thenFINAL98/0/0;
final10572ms;42 inputs frozen07:55:20UTC. [Evidence](../../docs/migration/P4_PROCESSOR_CONTENT_CONTRACTS_VERIFICATION.md).
Full P1–P12 remains OPEN, no publication.

## 1. Specification

Recipes are open contracts, but original Transformer runtime still inherits Component<TransformerDef>,
and original port/first-operating validation runs only for TransformerDef. Independent authored processor
metadata cannot reuse the same physical engine implementation. Remove these concrete dependencies.

Success: independent JSON component kind implements existing ITransformerWipDefinition and uses the
ORIGINAL Transformer runtime for physical batch/continuous production, timing, supply, tools, WIP,
operation/state/signals, save/load, cancellation and replay. Malformed native/independent metadata
must refuse before Initialize through the same existing owners.

## 2. Single-owner design/API

Extract ORIGINAL processor JSON reader/resolver into sole TransformerDeclaration. Native TransformerDef
delegates; no alternate loader or native fallback. Existing ITransformerWipDefinition owns necessary
resolved Content identity, authored States and AutoRepeat in addition to WipCapacity; inherited controls,
supported recipes and supply remain authoritative. Content is the actual immutable resolved session
definition catalog, not a second registry/store/World. Runtime Transformer uses this contract, not native
definition class. Original ResourceInventoryState still owns WIP; original Sync retains payload.

Move original port/packing/tool checks to existing TransformationContracts.ValidateDefinition and invoke
once from existing ResolvedContent component validation for every loaded entity/space, including unused
and unselected definitions. Whole WIP/context/runtime/alias/repeat checks share this content owner.
World composition retains only selected-session service/time/rollout/first-operating constraints. Generic first-operating draw check no longer native-only.
Metadata selection uses entity's actual named ITransformerDefinition, not runtime Component cast.
Supported aliases unique, WIP/runtime/metadata/context pairs valid, explicit repetition finite/supported.
No Makes helper/compatibility alias. Original support-owner loop handles actual alias membership.

## 3. Files/owners

Standard/Transformations.cs and TransformationContracts.cs; build/protocol identity.
Public ExtensionProof/ProcessorContentContractsProof.cs delegates declaration and creates original runtime.
Reuse existing master-listed capacity/recipe metadata and thin player authoring/commands, no copied
inventories or recipe/factory content catalog. Mechanical historical ContentOwnershipTests compilation
reference adaptation only; no historical runtime.

## 4. Verification

ONE NEW bounded exact case:
`Processor content contracts: independent declarations reuse native physical processing`.
Reuse thin player workflows with explicitly selected independent processor module/kind, not old case
execution. Physical90-second batch in two session quantums, continuous actual supply/draw/tool/operating/
output/cancel, saved twins/pause/accounting/replay. Additional early refusals for metadata/context/alias/
repeat/ports/capacity and one shared declaration lifecycle. Bounded stages diagnostic actual facts.
Build as needed; only this new case may be repaired/rerun. No old/family/full/Oil/Tycoon/platform runtime.

## 5. Limits and execution order

Existing recipe/processor declaration → interface runtime and central metadata validation → explicit
public module and new player proof → compilation/exact bounded case → evidence/freeze/master/audit.
No business/default/clock/platform/Draw/Oil/adapter changes. Save154/replay1/state/view schemas unchanged;
native processor codec9 retained, new proof component explicitly declares matching schema9.
Arbitrary CLR-provider callback mutation/faults, network/staffing/technology/knowledge breadth,
endpoint removal/mixed workloads/concurrency/lockstep/shuffling/fuzz/soak/performance/global/full open.
Resource catalog/type-only excluded Tycoon scope remains awaiting user authority.
Lifecycle skill guides staged single ownership; user/repository focused gates override full suite/early publish.
Commit and push only after genuine full goal AND plan completion and verification.

[Parent](P4_REUSABLE_MODULES_DESIGN.md), [processing](P8_TRANSFORMATIONS_DESIGN.md),
[prior timing](P4_TRANSFORMATION_TIMING_DESIGN.md), [tracker](MASTER_TRACKER.md).
