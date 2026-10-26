# P8-D3b2 — JSON-authored continuous catalyst wear and original-source replenishment

## Current M75: physically hauled continuous catalyst accounting — 2026-10-22

[Actual physical evidence, all attempts and exact scope](../../docs/migration/P8_CONTINUOUS_CATALYSTS_VERIFICATION.md#current-m75-physically-hauled-continuous-catalyst-accounting--2026-10-22).
Native2.723/save161/replay2/all450 sources/DLL unchanged. Existing continuous
flow/tools/operating/transaction/logistics/clock owners already implement the rules;
no new wear ledger/scheduler/default or engine defect is claimed.

New file-authored proof extends the original ContinuousTransformationTests owner
and Factory. Explicit finite hauling supplies an empty disabled processor, saves
loaded transit, earns real feed/wear/fuel and fills an actual eight-unit store.
Saved output waits burn nothing; physical coproduct clearing and finite donor refill
preserve the same execution/catalyst receipt. Slower resumed work earns the next
native wear quantum; stop releases unused charge without refund/duplication. Every
observed Update balances physical stock/cargo/WIP and classified cumulative facts;
history eviction/privacy/saved twins/isolation and full-final-hash replay are checked.
All named ports share one original local inventory, not scoped/independent stores.

Two new literal finals9472/0/two complete replays at minute/half-minute quanta; original
32MiB peak/1GiB cumulative per-tier Update budgets pass. Two clean target builds;
initial768/3 exposed missing JSON product handling, repaired explicitly with the
original nonzero loading/unloading durations, not an engine fallback. No old green
case/profile/full/default/family/soak run. Broader P8.017/C3/every-provider obligations
remain open; no original acceptance status changes or publication.

## Historical D3b2 implementation and protocol proof

Historical focused slice implemented/verified; full P8/P1–P12 remain open. Core2.312.0/save148/replay1/
transformer8/transformation-view15/operations-view11. Two builds (test-only syntax/Json API repair),
final clean3.31s. ONE NEW exact case FIRST222/0/0 in788ms; PID22488 exited0, observed927ms,
untruncated/no observer error. Frozen19:13:08 UTC. Five JSON files/7 continuous arrays migrated.
Older cases compile only, not rerun. [Evidence](../../docs/migration/P8_CONTINUOUS_CATALYSTS_VERIFICATION.md).

## 0. Problem statement

`TransformationDef` refuses nonempty continuous tool consumption. `TransformerFlow` acquires fresh
feed/utility claims under a separate flow owner and consumes each full receipt. D3a provides bounded
receipt disposition; D3b1 provides whole fresh/increase acquisition. Connect those existing owners
to native authored continuous rates without a new catalyst ledger or scheduler.

## 1. Goals

1. Required tool_consumption is a native Bill for batch completion and a rational rate array for
   continuous work. Empty {} or [] explicitly means fully reusable tools. No old-shape reader.
2. Continuous wear resources must be authored held tools. Native resource units, periods, duplicates,
   port compatibility and packing are validated before Update.
3. Startup admits full original charge plus first feed, utilities and wear. Running draws replenish
   original holds and acquire fresh feed/utilities together, then consume only that actual draw.
4. Tool holds retain their original token, amount and endpoint across target changes, pauses,
   shortages, output blockage and changed current port selections. Cancellation releases charge
   without refunding already consumed resources.
5. Wear derives from earned progress using existing ResourceRates cumulative ceiling. No separate
   exposure/remainder counter, copied stock, implicit depletion or teleport.
6. Private immutable authored-rate and flow facts, save/load, accounting and replay share execution.

## 2. Design

Add ToolConsumptionRates to ITransformationDefinition/TransformationDef. ResourceRateSpec is the
sole parser/resolver. Batch retains ToolConsumption; continuous retains rate rows. Require the rate
resource in Tools. Validate against named tool port policies using existing port validation.

Revise IResourcePort.ResolveStore(World, ResourceEndpointId? original=null): null selects the
current store; a supplied original must resolve that exact supported store or return null, never
fall back to current. Existing InventoryPort/OperatingInputs and independent proof ports implement
the revised contract directly. ResourcePortContracts.Bind validates exact original identity and
the same authored policy/ownership. Port providers own resolution; no copied endpoint registry.

ToolBindings optionally uses existing durable ToolSources. Active continuous wear/rate facts use
original bindings; startup uses current bindings. The shared PortRates calculator quantizes wear
against those actual endpoints. FlowSnapshot adds original ToolPorts, ToolConsumed and NextTools;
these are derived facts, not persisted consumption authority.

TransformerFlow uses the existing execution root owner. PortClaims groups feed/utilities/wear;
matching actual original ToolClaims produce explicit increase intents, other rows fresh intents.
The whole acquisition returns current full records. ConversionInput consumes each requested draw,
not the full original charge, with remainder retained. One actual conversion includes all outputs.
Startup admission includes the original charge and first wear before StartWip can hold any tool.

## 3. API surface

Revised engine-side IResourcePort and ITransformationDefinition contracts. Existing private
TransformationManagementView gains ReadToolConsumptionRates and extended flow facts; view schema15.
No platform/Draw/host gameplay. Build identity changes; native save/state layouts remain unchanged.

## 4. Files to change

| Action | File | Approximate lines | Risk |
| --- | --- | --- | --- |
| Modify | src/Beep.SimEngine/Standard/Transformations.cs | 30 | medium |
| Modify | src/Beep.SimEngine/Standard/TransformationContracts.cs | 1 | low |
| Modify | src/Beep.SimEngine/Standard/ResourcePorts.cs | 35 | high |
| Modify | src/Beep.SimEngine/Standard/TransformerFlow.cs | 90 | high |
| Modify | src/Beep.SimEngine/Standard/TransformerOperatingInputs.cs | 5 | medium |
| Modify | src/Beep.SimEngine/Standard/OperatingInputs.cs | 3 | low |
| Modify | src/Beep.SimEngine/Views/TransformationManagementView.cs | 12 | medium |
| Modify | src/Beep.SimEngine/Standard/TransformationModule.cs | 1 | low |
| Modify | tests/ExtensionProof existing port providers | 12 | medium |
| Modify | docs/migration/AuthorTransformationPorts.ps1 | 15 | low |
| Modify | existing master-listed continuous recipe JSON files | mechanical | medium |
| New | tests/Beep.SimEngine.Tests/ContinuousCatalystTests.cs | 180 | medium |

## 5. Error handling

Missing/old-shaped/duplicate/unheld/negative/discrete-invalid rates or periods: ContentException
before Update. Full output or missing aggregate feed/utility/wear: native blocker, no earned work,
wear or claim prefix. Original source mismatch/provider mutation: existing deterministic fault,
never substitute the newly selected store. Representation overflow: existing flow-range blocker.

## 6. Backward compatibility

Direct development migration only. Revise the sole one-time authoring script for {} to [] on
continuous recipes; invoke once. It is not a loader. No old contract signature overload/reader.

## 7. Verification

Compile all affected sources, then one NEW exact bounded public-player case for actual original
charge/rational wear, startup shortages, fractional target, feed/utility contention, changed selected
ports, full-output backpressure, pause/stop, saved working/wait/control commands, private immutable
facts, native accounting/replay and invalid authoring. No older/family/full/Oil/platform runtime.

## 8. Risks

Consuming a full augmented receipt would destroy the original charge: conversion uses request delta.
Changed selection must not alter original packing/source authority. Existing tool validation checks
exact base quantity after every completed Update. Complete startup includes charge and wear when
feed and catalyst share a resource/endpoint. Provider checks guard original/current participants.

## 9. Out of scope

Entity tools, scoped physical automatic supply/export, dynamic endpoint removal/recovery, global
provider/accounting/stress and full P8/P1–P12 gates remain required later work.

## 10. Execution order

1. Strict rate/port contracts and source-only provider migration.
2. Native startup and running full-set draw/conversion; derived immutable facts.
3. Mechanical JSON migration and one existing-master-listed native continuous catalyst recipe.
4. New thin player proof, build, exact bounded runtime, all-attempt evidence and tracker/audit update.
