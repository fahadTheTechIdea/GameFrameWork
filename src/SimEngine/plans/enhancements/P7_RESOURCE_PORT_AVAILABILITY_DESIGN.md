# P7-PA1: typed resource-port availability — Design Document

## Current M153 — Validate final port quantity publication

Native2.774/save161/replay2/inventory view11. Keep original typed availability and
first binding validation. The existing final Facts projection previously read Amount
again without validation, publishing negative-as-empty or fractional whole stock.
Actual2.773 reproduction20/8; fix the same helper with the shared quantity/capacity
validator, borrowing the original immutable resource alias in its internal binding.
No new stock/content owner, public API, schema, fallback, rounding or host logic.

New84/0 plus existing scalar491/0/physical connected-port366/0 =941/0. Earlier10/1
test-sequencing failure is retained; views must be cached before the intended fault.
Three targeted builds; native/proof identical between final build2/3; no unchanged
green reruns. [Every command/result/image and remaining scope](../../docs/migration/P7_RESOURCE_PORT_AVAILABILITY_VERIFICATION.md#current-m153-validated-final-port-quantities--2026-10-23).
Fresh450 source/PDB/138typed-call/14primitive-state-write/8layer-write census, zero
unknown/unmapped/indirect. Profile metadata1107/1086included/21excluded/zero bodies.
Original221/210 source-recorded checked/11open/full goal active. No full/profile/
family/soak, Oil rebuild/business/platform/Draw/adapter change or Git publication.

## 0. Problem Statement

Before P7-PA1, ResourcePorts.cs returned a live outlet or null. Store accepted null only when allowMissing and an explicit original were supplied; consumers disagreed about whether temporary unavailability was normal gameplay or a provider fault. That implementation assumed live bindings throughout manufacturing. P7-PA1 replaced it with the typed availability contract below; M153 additionally validates final quantity publication through the shared quantity/capacity rules.

## 1. Goals

1. One explicit ResourcePortResolution describes actual ready storage or typed unavailable state; malformed results remain provider faults.
2. Every existing named-port provider and engine consumer uses that contract, with no null/default compatibility mode or substitute inventory.
3. Blocked bindings expose no fabricated zero-stock observation and cannot acquire claims, consume inputs, earn manufacturing time or commit outputs.
4. Existing original tools, WIP, receipts and cumulative costs survive interruption, save/load and cancellation until physical/original access becomes valid.
5. Actual authored public-command proof demonstrates unavailable originals, restoration and no resource/progress duplication; one exact bounded case, no broad runtime.

## 2. Design

Change IResourcePort.ResolveStore to ResourcePortResolution. Existing ResourcePortContracts Store/Bind owns shape/identity/company/packing validation and pure provider observation. Remove allowMissing. Internal binding retains actual typed blocker plus declared capacity quantum for nominal previews when unavailable; no second stock or receipt store. ResourcePortSnapshot includes explicit blocker/availability and null stock for an unavailable observation.

All manufacturing startup/tool/operating/continuous/output admission checks binding blockers before physical mutation. Nominal rates still come from authored metadata; no stock is inferred from absence. Utilities, work, supply matching, network demand and port diagnostics handle the same result. Cancellation returns WIP only through exact original accessible port bindings; no current substitute.

Migrate native InventoryPort/OperatingInputs and independent proof providers in place. Existing selection command preserves selected references and validates ready actual stores through the same owner; no new selector or workflow scheduler.

## 3. API surface

ResourcePortResolution.ReadyStore(actual outlet)/Unavailable(nonempty Code), Ready/Store/Blocker. IResourcePort.ResolveStore result changes in place. ResourcePortSnapshot.Blocker/Ready distinguish actual stock from unavailable observation. No new mutable host API, command stream, clock or parallel models.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---:|---|
| Modify | Standard/ResourcePorts.cs | 120 | high |
| Modify | manufacturing startup/flow/operating/WIP/delivery consumers | 90 | high |
| Modify | utilities/work/logistics/network and immutable views | 90 | medium |
| Modify | native/proof port providers and facts | 50 | medium |
| New | authored public availability acceptance content and driver | scoped | medium |
| Modify | engine identity, protocol, stage evidence/master | scoped | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Ready actual outlet | Original identity/company/packing checks |
| Typed unavailable current/original | Gameplay blocker, no substitute or false stock |
| None blocker plus null store, or blocker plus store | Terminal malformed provider result |
| Wrong actual endpoint/company/policy | Original terminal fault |
| Missing original during active manufacturing | Preserve tools/WIP/progress; no draw/output |
| Cancellation with unavailable original | Returning wait until original restoration, no teleport/refund |
| Provider callback mutation/throw | Original first fault; no healthy save or replay success |

## 6. Backward compatibility

Development only. Replace the old return shape and remove allowMissing. No overload, legacy resolver, exception-based gameplay fallback or duplicate port owner.

## 7. Verification

One solution compile and necessary targeted repairs only; one NEW exact bounded public case with all attempts retained. Actual JSON/master, seat commands/views/Game.Update, physical supply where required, saved twins/replay/isolation. No old/family/full runtime, and no broad original gate closure from this proof.

## 8. Risks

Absence must not fabricate empty stock. Historical consumed/produced quantities remain derived from earned progress rather than disappearing when a binding is unavailable. Exact original endpoint identity must survive all holds and returning phases.

## 9. Out of scope

New dynamic network capacity/ports, arbitrary private CLR/fault/stress matrices, Oil/platform/Draw/business expansion and publication.

## 10. Sub-task execution order

1. Replace the port result and observations in the existing owner.
2. Migrate every consumer/provider without compatibility paths.
3. Author and verify normal unavailable/restored manufacturing and logistics behavior.
4. Update evidence and whole-goal tracker without narrowing open requirements.

## Current scoped result

Core2.471.0/save154/replay1/inventory-view7/transformation-view19/operations-view12; original state owners/codecs retained. FIRST/FINAL458/0/0 in9172ms: twelve native/point minute/half physically supplied batch completion/cancellation/continuous flows preserve original tools, WIP, progress and cumulative draws through typed unavailability and restoration; no alternate return or repeated conversion; private facts/saved twins/replay/isolation. Unavailable observations carry null stock/-1 version, not fabricated zero.

One solution compile failed five old return-shape/ReferenceEquals callers7.35s; one necessary targeted compile clean8.45s. No solution rerun or executable edit after FIRST/FINAL; older/family/full runtime remains unrun. [Evidence](../../docs/migration/P7_RESOURCE_PORT_AVAILABILITY_VERIFICATION.md). Original221/185checked/36open unchanged; additional197. Fresh dynamic/network/malformed/custom-provider/quality/paid-cost/fault/stress/performance/global/full and publication gates remain OPEN.

Metadata closure at 2026-10-12 02:12:12 UTC:1656 source anchors/13 quickstart anchors/915 selected executable inputs, zero SHA drift;2844 paths/1002 Markdown/8630 local links, zero broken;18 master rows/zero malformed; thin driver boundary matches0;git diff --check exit0 (34 warning lines),dirty2832/staged0. Original221/185checked/36open unchanged; additional197. Protected GameClock/Tycoon hashes unchanged, goal_complete false. Twenty-four prior selected inputs changed; seven newly frozen inputs comprise two new JSON/driver files and five existing consumer/helper/caller files. Seventeen existing production sources changed (two previously unfrozen), zero new production file; four source anchors added. Old nullable resolver/allowMissing paths0, typed provider implementations6. Inventory/transformation/operations views7/19/12. No executable edit after FIRST/FINAL or original gate text/checkbox/line-anchor change; no older/family/full runtime or terminal-solution success claim.

