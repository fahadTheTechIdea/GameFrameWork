# P8-C1 — Continuous native transformation evidence

Current physically hauled catalyst extension: [M75 exact sources/images and scoped
accounting/replay/allocation evidence](P8_CONTINUOUS_CATALYSTS_VERIFICATION.md#current-m75-physically-hauled-continuous-catalyst-accounting--2026-10-22).
Native2.723 unchanged, two new literals9472/0/two complete replays; not all-provider
qualification. No rerun of the historical C1 cases below.

## Historical C1 evidence

Date: 2026-10-05. Historical working source, not a current HEAD/full-regression report.
Scope: standalone Beep.SimEngine only; no Oil/Tycoon, renderer, platform or host gameplay changes.

## Source identity and breaking migration

2.84.0 / save54 / transformer6 / transformation-view7. Manager2, work22, logistics5,
work-view27, logistics-view10, resource-flow5 and actual inventory layouts unchanged.
Explicit mode:batch|continuous is required. Existing batch proof/inline JSON migrated directly.
ResourceRates.Consumed/Next are removed; callers use Accumulated/Between with WorkProgress and
explicit rounding. TransformationSnapshot requires Flow facts; absent providers report explicit None.
No old reader, default mode, compatibility constructor or parallel coordinator.

## Implementation ownership

- TransformationDef owns authored mode, native Bills/rates, period and cancellation metadata.
  ResourceRateSpec is the single rate-array authoring/resolution path for feed/products/operating inputs.
- ResourceRates uses exact wide rational cumulative calculations with fractional game-time ticks.
  Ceiling feed and floor product respect actual native endpoint quantum; no per-update rounding.
- Transformer retains its existing queue, effective WorkProgress, elapsed running counter, execution
  identity/tools and sole Sync. Continuous streams stay Working; Blocked is finite batch completion only.
- TransformerFlow.cs is a partial file of that executor, not another system. InputStore/OutputStore
  alias its actual local inventory. Tools remain owned by WIP execution; transient flow claims use
  a distinct derived owner and are completely consumed in the same Update.
- Existing ResourceTransactions whole conversion reserves every product before feed/fuel consumption,
  verifies actual stock/receipt mutations and publishes only complete success facts. Space fits now,
  not after hypothetical input consumption. No rollback/refund/duplicated output ledger.
- Existing named LogisticsService/WorkBoard/jobs/receipts carry real startup/running input deliveries.
  No resource teleport, alternate supply queue or demo/harness scheduler.
- TransformationContracts owns shared FlowFacts and guards private current facts on Update/save/load.
  Flow input totals/next deltas are feed only; Operating is actual elapsed fuel. Admission combines both.
  ResourceFlowJournal remains the only historical feed, never a balance authority.

## Exact final commands and outcomes

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Continuous transformations: authored modes, quantization and private contracts"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Continuous transformations: physical flow, waits, cancellation and saved throughput"
```

Final build: zero warnings/errors, 2.39s.
Contracts: 46 passed, zero failed, 322ms.
Physical: 66 passed, zero failed, 1354ms.
Total: 112 checks in exactly two named suites, zero failures/aborts. Repair repeats used only these
same names. No third/family/full-suite run. Other suites migrated and compiled only.
Every physical wait is bounded to800 updates and reports tick/progress/blocker/current jobs on timeout.

## Proven in these suites

Strict modes/shapes/periods/native quantities/cancellation/repeat/output authoring; actual catalog aliases
and stores; exact fractional discrete ceiling/floor and wide arithmetic/refused overflow; immutable private
snapshots; stream queue/phase and actual tool holds; finite batches through the same executor; mixed
feed/fuel/coproduction; real inventory/journal accounting of the short local scenario; disabled and signal
gates; startup/running backpressure before spending; shortages without consuming tools; changed throughput
independent of elapsed fuel; generators with empty-delta updates and no dummy resource claims; rational
fluid output; ordinary stop without fuel/feed refunds or prospective product creation; loaded physical
startup and repeated running deliveries; saved controls/shortages/cargo/gates/generators/streams and
isolated interleaved twins; fake continuous completion refusing checkpoint and faulting normal Update.

## Remaining gates

Native local continuous flow is complete for this slice, not full P8 or P1–P12. Distinct/remote ports,
independent continuous-provider conformance/fault matrices, catalysts/remote/entity tools, paid/shared
graphs, idle/travel fuel, utility-network solving, output-dispatch/fleet/congestion integration, full
provider purity/reentrancy, whole-session accounting, replay/lockstep/fuzz/soak/allocation and roadmap
acceptance remain unproven. Focused short history is not an unlimited accounting retention guarantee.
Stopped quantized streams do not refund prepaid input fractions; stop explicitly preserves only actual
earned products. Author content accordingly; no hidden discard/fractional stock or completion fallback.
Full roadmap remains active/partial. Predecessor reports, including98 checks, were not rerun as suites.
