# P9-B4c — Distinct physical worker dispatch

Status: focused verified, 2026-10-06. Build2.166.0/save93/work27/logistics11.
Clean build;1234 assertions/two exact suites (1076 contracts;158 physical), zero final failures/aborts.
[Evidence](../../docs/migration/P9_DISTINCT_DISPATCH_VERIFICATION.md). Parent [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).

## 0. Problem

WorkSystem.Assign merges work and logistics priorities, but TryAssign and provider TryAcquire
independently choose their first worker. A flexible machine can take a flexible job while the only
remaining job needs that machine's specialist role. Preparedness already detects distinct capacity;
physical dispatch must realize it using the existing owner, not a second scheduler.

## 1. Goals

1. Match current ready intents to distinct actual workers across native jobs and registered sources.
2. Preserve emergency, starvation, priority, age and stable intent ordering. Earlier covered intents
   cannot lose coverage to later ones. Worker preference/cost orders alternative edges deterministically.
3. Re-resolve the selected worker before mutation, after every acquisition. Never silently substitute
   another worker, bypass company floors, borrow a stale material claim or interrupt another incumbent.
4. Extend IWorkerDispatchSource and IHaulExecution directly; ordinary public-only providers participate.
5. Keep one GameClock, existing WorkBoard/LogisticsService jobs/claims, and no saved matching state.

## 2. Design

WorkScheduling remains a partial file of the sole WorkSystem. CopyAssignments retains ranked intent
diagnostics. A pure candidate query supplies alternatives for each ready intent. DistinctWorkerMatching
serves all three consumers (company floors, preparedness, physical arbitration), traversing supplied
ordered edges instead of discarding preferences. Assign matches unexhausted intents, then attempts
them in existing rank order. Actual success invalidates the whole temporary allocation and rebuilds.

Native candidate gathering shares existing target/episode/retry/node/requirement validation. TryAssign
accepts selected identity, checks it against fresh candidates and preserves that identity across actual
emergency interruption. A missing selected edge refuses without acquiring another machine.

IWorkerDispatchSource gains CopyCandidates(identity, entry, mode, destination), returning ready
WorkerDispatchOffer alternatives with identical intent facts and unique worker identities. TryAcquire
requires selected worker identity. WorkSystem validates candidate scope/ownership/age/ranking/worker/
interruption facts and rejects changed intent metadata, duplicates and missing advertised best edges.
Candidate reads are observations, never reservations or promises of shared material/dock feasibility.
The sole pass excludes interrupting alternatives while that intent has an idle compatible actor.
It never interrupts incumbents merely to improve lower-ranked work coverage; after actual idle
acquisition a fresh pass can authorize genuinely necessary emergency interruption.

LogisticsService uses the same BuildQueue/Preview/PreviewSource and Acquire paths constrained to an
explicit carrier. IHaulExecution exposes optional worker selection on its existing preview/acquire
methods: absent selection means intentional automatic selection for ordinary manual commands; an
explicit identity never falls back to another actor. Registered shipment proof uses these same paths.
Return-cargo and evacuation retain existing ownership, not falsely invented dispatch intent identities.

## 3. API surface

- IWorkerDispatchSource.CopyCandidates(long identity, string entry, WorkCandidateMode mode,
  IList<WorkerDispatchOffer> destination).
- IWorkerDispatchSource.TryAcquire(long identity, string entry, EntityId worker, WorkCandidateMode mode).
- IHaulExecution.PreviewHaul / AcquireHaul add optional EntityId worker selector.
- No new command, JSON subsystem, policy default, reservation owner or clock.

## 4. Files

Existing WorkContracts, WorkScheduling, WorkSystem, DistinctWorkerMatching, LogisticsScheduling,
LogisticsService, public DispatchSourceProof; one focused test file and registration. Build/save/system
identity advances for deterministic behavior changes. Previous metadata assertions migrate only where
needed to compile current contract; they are not rerun or represented as current behavioral evidence.

## 5. Errors

Foreign/duplicate/mismatched candidate facts are contract violations. Removed, busy, disabled, zero-rate,
out-of-area, unreachable or materially blocked selected actors refuse before claims. Valid shortages
remain pending. Existing owner atomic resource/dock admission, actual emergency and floors stay final.

## 6. Compatibility

Development only: direct contract replacement, no overload, alias, old reader or legacy dispatcher.

## 7. Verification

Clean build and only these two exact suites (repair reruns of the same names allowed):

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Distinct dispatch: candidate contracts, ranked matching and refusal"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Distinct dispatch: parallel work, logistics and saved continuation"
```

Bound all physical waits and report game time, jobs, worker IDs and blockers. Prior suites compile only.

## 8. Risks

Matching optimizes distinct current worker capacity, not global routes or whole-workflow completion.
Fresh sequential admission must still preserve shared stock, docks and company floors. Provider
candidate enumeration cost and larger stress/allocation gates remain open; do not imply they passed.

## 9. Out of scope

Oil/Tycoon/hosts/demos/platforms/Draw; new clock; global purity, fuels/traffic/congestion, complete
team/material reservations, full replay/fuzz/soak/allocation/roadmap acceptance.

## 10. Execution

- [x] Inspect current authoritative ranking, native and source acquisition.
- [x] Extend existing contracts and single matching/dispatch owner.
- [x] Prove actual parallel DAG and mixed-provider acquisition through two exact suites.
- [x] Record current evidence and update the sole master; keep full roadmap open.
