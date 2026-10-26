# P4-C1b2 optional work — 2026-10-04

Working-tree source: **beep-simengine-2.26.0 / save 11**. This report is focused evidence, not full
regression or P4/DAG completion. Existing dirty changes were preserved; no commit/HEAD-only claim.

## Implemented single-owner migration

Deleted PhysicalWorkModule (no facade). WorkModule owns board/system/order/worker commands and
WorkView schema 4. LogisticsModule owns the existing logistics service/system, transporter/supplier
content, stock/route commands and LogisticsView. ConstructionModule owns existing site/construction/
salvage generation, service/resolvers/effects/commands/events and ConstructionView. InfrastructureModule
owns the existing line/network service/system, marker/content/effects/commands and InfrastructureView.
IncidentResponseModule owns response effects and the existing incident projection. InventoryModule
owns the transferred InventoryView. Query methods were moved directly, with no forwarding duplicates.

Required automatic_supply extends IWorkPlan and order/construction/line JSON. Local input waits
need no hauling service. Automatic input requests and haul execution bind contracts to the same
LogisticsService that owns existing demand/reservation state. Positive step/supplier prices require
IEconomy during composition; no implicit free ledger. PointModule's unnecessary ledger binding is gone.
WorkBoard validates component plans through IWorkPlanSource; ProductionSystem validates recipe time,
LineService validates line plans, and Site owns cancellation cleanup through IWorkCancellationTarget.
All affected standalone module manifests, authored flags and view consumers compile.

The physical proof initially failed at tick 504 because the generated line cursor was declared
immobile despite advancing along its corridor. InfrastructureModule now declares its nonblocking
presence movable, retaining the existing position owner and machine travel/reach rules. No guard
bypass, instant construction fallback or additional movement system was added.

## Executed verification

`dotnet build Beep.SimEngine.slnx`: success, **zero warnings/errors**, final build 3.14 s.
ExtensionProof compiles as an ordinary public-contract provider; tests alone instrument internals.

Exactly these two named suites were run (physical suite rerun after its diagnosed defect):

| Exact suite | Passed | Failed | Recorded time |
| --- | ---: | ---: | ---: |
| Optional work: local timing, explicit dependencies and saved sessions | 36 | 0 | 87 ms |
| Optional work: physical owners, hauling, building and saved continuation | 25 | 0 | 220 ms |

**61 checks / two suites**, zero failures/aborted suites. No full/broad family run.
Local suite was rerun against the final line-cursor declaration; scoped tracked-file diff checks pass.
Commands: `dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "<exact suite above>" -v`.

Local proof: explicit master JSON, no space/navigation/money/supply/haul/construction/infrastructure,
authored inlet/outlet, actual input consumption, six-update duration, nonmutating preview, active and
blocked saved twins, cancellation without fabricated refund, required flag/automatic supplier/positive
price refusal before initialization. One initialization-count probe provides diagnostics only; all
execution uses ordinary public player commands and the engine's existing work system.

Physical proof: explicit selected module owners; supply aliases share the same actual ledger; placement
projection; incident projection registration (not incident propagation); production plus real hauling
and machine construction; active saved twins; completed structure/vehicle/material accounting; saved
road-plan resolution and physical road materials; site-owned cancellation and saved cleanup. Stages
are bounded to 4,320 and 2,880 ticks, with fault and order blocker/progress diagnostics on timeout.

## Still open / not executed

Older standalone suites compiled but were not rerun. Prior 2.25.0 motion, 2.24.0 process and earlier
counts remain source-specific historical evidence. No current oil integration or full regression claim.
No oil, Tycoon, hosts, demos, platform adapters, rendering or external repository changes.

Generic authored construction (currently tile-only/generated), hidden generated storage capacities,
P7 atomic resource acquisition, P8 contract process migration, arbitrary DAG/executors, policies/docks,
generalized utilities and persistent incident progression remain open. Save field layout is unchanged;
build/content/module ownership identities change, and no old-save compatibility shim is introduced.
The lifecycle skill informed the staged migration/evidence; explicit repository and user focused-test
rules supersede generic full-regression recommendations.
