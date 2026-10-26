# P9-C1 — Open logistics demand verification

Date: 2026-10-05. Current working tree: **2.55.0 / save29 / logistics2 / logistics-view5**.
Work/work-view13, work_policy component1 and resource-flow4 are unchanged.
[Stage](../../plans/enhancements/P9_DEMAND_OWNERS_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Final scoped gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Logistics demands: declared sources, atomic native intent and private saved facts"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Logistics demands: JSON mod, physical delivery and retired saved commitments"
```

Build: **0 warnings / 0 errors**, 1.61 seconds. Only these two exact directly affected suites ran
during development. Final gate: **108 checks / 2 suites / 0 failures / 0 aborted suites**.

| Exact suite | Passed | Runtime |
| --- | ---: | ---: |
| Logistics demands: declared sources, atomic native intent and private saved facts | 67 | 267 ms |
| Logistics demands: JSON mod, physical delivery and retired saved commitments | 41 | 179 ms |

Total reported suite runtime: 446 ms. No broad/full regression, Oil/Tycoon, renderer/platform/host/demo,
lockstep/replay, fuzz/stress/soak or allocation gate ran. B2/B1 and earlier reports are historical, not
rerun. StandardTests/ActivityGraphTests references were migrated and compiled, not executed here.
An early proof called a nonexistent cancellation API; it now uses the normal CancelOrderCommand.
The initial duplicate-scope fixture first hit the existing duplicate codec-ID guard; distinct fixture
codec IDs now exercise the actual demand-scope guard. The final gate above has no failures.

Scoped git diff --check on edited tracked source/test/tracker files passes, with repository LF-to-CRLF
notices only. An explicit trailing-whitespace scan of new/edited demand, proof, view and stage files
passes. Runtime/test source checks confirm the old DemandSource/SourceKind/SetDemand/WithdrawAll paths
are absent and build/save/service/view identities agree. These are source checks, not another test
gate. Documentation-only handoff edits did not trigger a further build or test run.

## Authoritative implementation

LogisticsDemandId is a canonical scope plus positive signed 64-bit source identity. There is no
closed source-kind enum or fallback owner. Default identity/source-check values do not mean ready.
ILogisticsDemandOwner is implemented by declared session-owned services and discovered using the
existing ServiceContainer.Providers path; aliases appear once, duplicate scopes/foreign worlds refuse
before tick one. There is no second registry or mirrored workflow store.

ILogisticsDemandRequests aliases the existing LogisticsService. Whole Bills preflight company/source/
destination, known resources, native acceptance/quantum, nonnegative priority and representable active
aggregate quantity before mutation. A changed complete bill withdraws absent lines and retains original
CreatedTick for surviving destination/resource/source lines. Single stock edits use the same table.
Stock, WorkBoard and ProductionSystem own their own source liveness; generic matching does not inspect
concrete Producer or Order state. Existing supply interfaces are typed workflow entry points on that
same coordinator, not old-format adapters or separate stores.

Desired intent can exceed physical capacity; it cannot create an inbound promise or instant stock.
The normal stock command still enforces existing public-store/ownership/capacity permissions.
Native fixed-point range is a numeric representation constraint, not a world-size or gameplay cap.
Original source age is exposed/saved, but this slice does not order all haul demands by age/priority.

Expired sources immediately stop contributing to demand/spare calculations; periodic normal planning
prunes their intent. Withdrawal and expiry never cancel already-acquired hauls or release their real
worker/source/destination/cargo commitments. Existing WorkBoard and physical haul execution remain sole
job/claim/progression owners. LogisticsView reports detached company-private source/quantity/priority/
age/status facts; foreign queries clear their output before returning.

One LogisticsService codec saves source scope/long identity and creation ticks. It rejects unknown,
duplicate, fractional, future-age and mismatched source records. Full native binding and aggregate
range checks occur after all services restore. Explicit expired intent may save until the next planning
phase; restoring its provider state never revives it. Save29/logistics2/view5 replace previous layouts,
with no old reader. ProductionSystem's service alias does not add a second system-state codec.

## Physical public-contract mod proof

ExtensionProof/content/logistics-demands.json is explicitly master-listed as demands.json by the
selected proof.demands module. It authors source scope, identity 4294967297, company, destination
prototype, whole native bill, priority and lifecycle state. The public-only mod owns source lifecycle
and engine commands; it implements no matcher, worker dispatcher, stock table or hauling loop.
Tests act as players by submitting commands and advancing Update.

Proof covers actual pickup, movement, protected loaded cargo and later unload; posted and loaded
checkpoints; retirement with remaining claims; explicit withdrawal; unpruned expiry; exact saved
continuation hashes; and an interleaved independent session with no requests or borrowed deliveries.
Native work/player sources with the same numeric ID coexist by scope. Work refresh retains age,
cancellation withdraws only its contribution, and replacing a producer bill removes obsolete lines.
Two bounded named suites fail after 120 updates per physical stage with job/state/blocker diagnostics.

## Remaining gates

Stock bands/outlet reserves; global demand/route ordering and company alerts/starvation/preemption;
editable/all-dispatch areas; persistent plans/templates; timed loading/docks/congestion; fleet/service/
fuel/recovery policies; provider mutation/fault matrices; replay/lockstep, stress/performance and full
framework acceptance remain open. This scoped proof is not completion of P9 or the overall roadmap.
