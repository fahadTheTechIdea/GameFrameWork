# P8-D2d — Native operating ports and one multi-port utility contract

Status: implemented as a focused slice, not full P8 completion. Required maps, shared utility/port
contracts, whole processor startup/draw, inventory-free external processor, migrated worker/network
call sites and immutable private facts are landed. Core2.309.0/save146/replay1/transformer8/
transformation-view13/operations-view11; existing durable layouts and GameClock unchanged.
One NEW exact bounded case FIRST197/0/0 in578ms; two builds include repaired new-test compile errors,
final clean4.68s. Older/worker/network cases compile only, not rerun. Frozen18:20:14 UTC.
[Observed evidence](../../docs/migration/P8_OPERATING_PORT_ROUTING_VERIFICATION.md).

## Problem and goals

TransformerOperatingInputs consumes from one entity inventory; IUtilityConsumer.UtilityStore and
UtilityDemandSnapshot.Endpoint/Version encode that assumption in network demand, signals and worker
observations. Replace the shared contract in place, not with another processor-only utility model.

1. Require exact operating_ports JSON maps for processor operating rates, {} when empty.
2. Let each IUtilityConsumer resolve an actual named IResourcePort for each declared resource.
3. Generalize the existing resource-port route/binding/snapshot naming and share guarded lookup,
   immutable facts and native packing between process ports and utility consumers.
4. Whole startup combines feed/tools/first operating draw; each timed draw consumes all selected
   sources together through actual claims, never a successful ready prefix.
5. Continuous feed/operating draw/output use the same existing conversion and earned-time owners.
6. Private utility facts/signals/network demand validate actual per-resource endpoints and versions.
7. Remove the native processor's mandatory inventory when all ports are external; no fallback,
   duplicate buffer, cumulative ledger or implicit local utility endpoint.

## Contract and state design

IUtilityConsumer.ResolveUtilityPort(World,ResourceId) replaces its singleton UtilityStore. Existing
worker/motion operating providers expose their own explicitly named operating capability as a local
IResourcePort binding to the same inventory; no second component or local stock model is introduced.
Their phase/time/consumption owners remain unchanged. Independent providers implement the same port
contract. Processor selection follows exact head.OperatingPorts authoring.

UtilityDemandSnapshot replaces singleton endpoint/version with copied canonical resource-port facts.
Resources is derived from those facts; aggregate Consumed/NextInputs/Available remain projections of
the same existing demand, not balances. Shared ResourcePortContracts guards actual named lookup and
stock/endpoint/version observations. UtilityContracts validates identity, per-port quantities and
availability. NetworkFlow selects the consumer's actual port for the served resource, not one store.

Transformer keeps its sole running-time counter. Operating rates use each selected port's native
quantum through the existing cumulative ResourceRates calculator. Batch draws use complete
AcquireClaims/ConsumeClaims, continuous draws the original complete conversion. Shortage, disabled,
backpressure and cancellation consume no extra exposure; already burned utilities never refund.

Startup groups feed, reusable tools and operating amounts by actual endpoint before acquisition.
Remote automatic supply remains explicitly unavailable where current local logistics cannot serve
the selected endpoint. Native processors without inventory are supported when automatic local
supply is not selected. Full scoped physical supply/export remains the next existing-owner migration.

## Files, errors and verification

Modify Standard/ResourcePorts, UtilityConsumers, OperatingInputs, TransformerOperatingInputs,
TransformerFlow, Transformations/contracts, NetworkFlow, existing view metadata, engine identity,
proof consumers and compile call sites. Revise the one-time AuthorTransformationPorts script for
explicit operating maps/aliases. Migrate all authored processor JSON directly; no old reader.

Missing/extra/unknown/incompatible maps refuse before Update. Native complete draw shortage retains
actual sources/progress/tools/WIP. Misreported identity, endpoint/version, compatibility or stock
facts fault pure observation; a failed actual mutation cannot produce a checkpoint. Range overflow
uses the existing typed blocker. Unavailable remote supply never silently targets entity inventory.

Execution: shared resource-port primitives; one revised utility contract/consumer migrations;
native authored operating selection/whole draw/startup/continuous paths; views/identity/content;
one new bounded public-player proof; documentation/tracker/evidence reconciliation.

Prove mixed utility sources, shortage without prefix, native quantum/elapsed consumption, feed/tool/
utility aggregate startup, pause and output backpressure, no-refund cancellation, inventory-free
processor, private facts, saved twins/replay and malformed authoring. Build for compile correctness;
run only the new exact case. Existing worker/movement/network cases compile, not broad runtime proof.

## Remaining full goal

Catalysts/entity tools, scoped physical automatic supply/export, dynamic endpoint recovery, generic
provider/global purity, network breadth, accounting/stress/full P8/P1–P12 remain required. No platforms,
Draw, new clock, required budget/Tycoon or publication. Full goal remains active until all original
requirements are genuinely implemented and verified.
