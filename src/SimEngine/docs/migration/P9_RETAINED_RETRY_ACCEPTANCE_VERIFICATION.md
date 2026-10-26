# P9-C15 retained physical retry acceptance — 2026-10-10

## Outcome and sole responsibility

ONE NEW exact case passes7008checks on Core2.424.0/save154/replay1, unchanged. Original221 requirements retain their exact wording and checkbox states:161checked/60open. Additional scoped deliverables136. P9.acceptance.051, .052 and the full P1-P12 goal remain OPEN. No staging, commit or push.

Original WorkBoard/WorkSystem and IWorkRetryPolicy own the existing job, failures and retry deadline. Original IHaulExecution, handling, receipts, storage, logistics diagnostics, operation gates, spatial provider, entity Update and GameClock own physical gameplay. The driver submits public player commands, controls the sole clock and asserts detached public facts; no direct world/resource/job/progress mutation or scheduler/reservation/clock fallback.

## Current physical matrix

Twelve combinations: native capped two/four/eight-minute retry and independent constant three-minute contract policy; authored minute and half-minute quantum; actual partial loading, loaded travel and partial unloading.

All begin with20 finite material units and one explicit eight-unit public hauling command. At interruption, original diagnostics observe actual source/cargo/destination stock and inbound/outbound promises. Operation availability is disabled during partial handling; selected point-space access is closed during loaded travel. Native unreachable or worker-unavailable blockers retain one job, one carrier, original endpoint identities, cargo, all remaining claims, earned handling and previously committed delivery prefix. No unavailable update moves, loads or unloads material.

Repeated actual retries reach third failure: native maximum8minutes, independent3minutes, represented by the actual single configured quantum. Work policy disabling retains the original expired deadline and suppresses new failures for20updates. Saved partials and clock pause preserve hashes and real claims. Restoring route/operation while work policy remains disabled grants no work; enabling it resumes the same physical job and ultimately delivers exactly8 once. Source12/cargo0/destination8, all promises released; twelve later updates do not repeat delivery. Captured earlier WorkJobView remains detached.

Every variant verifies normal preview purity, foreign haul refusal/private job and cargo withholding, pending command inbox saves, hash-identical Update twins, partial loading/travel/delivery/backoff/policy-disabled saves, pause, terminal restore, actual command replay and untouched interleaved session. Physical phases bounded at512Updates with original clock/job/blocker diagnostics.

## Pure authoring and contract extension

HaulRetryTests.Factory is the existing sole pure scenario authoring path, exposed internally instead of copied. Historical runtime methods are not called. First attempt exposed that its selected haul-retry document discarded the executor declarations in work-retry.json; composition now retains those explicit master-listed definitions and replaces the selected authored sections. There is no injected default runtime/content or engine validation weakening.

Independent ConstantRetryProfileDef implements IWorkRetryPolicy directly with ordinary Def identity; it no longer derives from the native retry metadata base. Its existing JSON delay and algorithm are unchanged. Public-only extension assembly still declares the module and document; engine scheduler remains unchanged. Legacy historical drivers compile but are not executed or claimed current.

## Actual verification

One initial solution compile, then one same-case targeted repair compile, both clean:

1. `dotnet build Beep.SimEngine.slnx --no-restore`:26.74s, exit0, zero warnings/errors. Live handle81516 observed terminal; no process restart.
2. `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore`:12.14s, exit0, zero warnings/errors. Live handle59305 observed terminal. Final dependency compile covers all five projects; not a runtime suite.

ONE NEW exact case only:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Retained haul retry: public partial loading travel and delivery survive authored interruptions' --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
```

Two attempts of that same case:

1. 0passed/1failed/1aborted243ms, owned PID26356 exited1/475ms. ContentException for unknown IActivityExecutor work.staffed before gameplay: original fixture document replacement was repaired by retaining actual authored executor sections.
2. FINAL7008passed/0failed/0aborted4031ms, owned PID33120 exited0/4235ms.

Both outputs untruncated, observer errors empty; outer60000ms, owned termination5000ms, captured output30000chars. No historical/family/full, Oil, Tycoon or platform runtime. No executable edit after FINAL.

## Metadata and continuation boundary

Actual post-FINAL freeze2026-10-10 05:30:24 UTC covers639selected inputs; four actual executable files new/changed. Seven newly selected inputs include one new driver and six existing dependencies. Actual metadata validation:1306whole-source SHA anchors plus13quickstart anchors, zero mismatches;639selected executable inputs, zero post-FINAL drift;2562indexed paths/886Markdown documents/7591local links, zero broken;221original gates, zero wording or checkbox changes and204line anchors refreshed;18five-column master rows, zero malformed; diff-check exit0 with34existing line-ending warning lines; new driver boundary matches0. Git dirty inventory2552paths via git status --porcelain=v1 --untracked-files=all, staged0. All four protected G19/sole-clock SHA values unchanged. These read-only metadata checks are not additional runtime tests.

Literal endpoint or road destruction, physical refueling, actual equipment breakdown/response, blocked output recovery, dock queues, paused plans, arbitrary-provider faults, lockstep/fuzz/stress/allocation and full regression remain open; operation disable is not a breakdown and point access closure is not road removal. No narrower proof closes the original compound .051/.052 requirements. Protected G19 decision and sole clock remain untouched. No OilFieldDays, business, Tycoon, platforms or Draw work.

The lifecycle skill kept source review, bounded public acceptance and evidence in one staged document/master update; its broad test suggestions did not override the user's focused-test limit.
