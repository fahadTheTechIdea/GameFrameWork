# P9-B4c — Distinct physical dispatch verification

Date: 2026-10-06. Current working-tree evidence, not clean-HEAD or full-regression proof.
Scope: Beep.SimEngine only. [Stage](../../plans/enhancements/P9_DISTINCT_DISPATCH_DESIGN.md).

## Identity and authoritative ownership

Build `beep-simengine-2.166.0`, save93, WorkSystem27, LogisticsService11. WorkBoard27/work view51/
incident view4/native incident2/public incident3/company policy3/view3/worker-policy4-public2 unchanged.
No matching payload, second planner, roster, dispatcher, reservation owner, state codec or clock.
Existing services retain their sole Sync; build/save/behavior identities change directly, no old reader.

WorkSystem.Assign still owns the one mixed-source arbitration pass. Existing CopyAssignments ranks
intent using emergency/starvation/priority/original age/cost/stable identity. Ready unexhausted intents
contribute ordered alternatives through native ReadJobCandidates or IWorkerDispatchSource.CopyCandidates.
The one DistinctWorkerMatching now preserves supplied candidate edge preferences and is shared by
company floors, response readiness and actual physical dispatch. It maximizes distinct capacity while
preserving earlier intent coverage. Actual success invalidates all temporary edges and rebuilds.

Native candidate admission shares target/episode/retry/node/requirement checks. Native TryAssign and
source TryAcquire require chosen identity and recheck current eligibility. A selected missing/busy/
disabled/zero-rate actor never silently switches to another actor. Necessary actual emergency
interruption reacquires that same actor. If any idle candidate exists for an intent, the sole pass
excludes interrupting alternatives: no interruption merely to fit lower-ranked ordinary work.

Logistics uses its same Preview/PreviewSource/BuildQueue/Acquire, constrained to the chosen carrier.
Explicit-carrier preview reads that actual IWorker directly instead of repeatedly scanning every worker.
Native resources, capacity, docks, operating areas, company floors and progress retain final admission.
An absent selector on the general haul API intentionally means automatic selection for manual engine
commands; an explicit selected actor does not fall back. Actual dispatch requires an explicit identity.
Public-only DispatchProofSource participates through the same contracts and native hauling owner.

Candidate alternatives must match original intent metadata, ownership, age, native worker state and
interruption authority; duplicate workers, missing advertised edge or altered metadata refuse. This
is scoped contract validation, not proof of global provider purity or all malicious provider behaviors.

Exactly one GameClock: World constructs it once, Game exposes that same instance. StepIndex/Tick
remain read-only update coordinates of its time, not independent clocks. GameClock.cs is unchanged:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31` (SHA256).

## Exact final gate

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Distinct dispatch: candidate contracts, ranked matching and refusal"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Distinct dispatch: parallel work, logistics and saved continuation"
```

- Build: zero warnings/errors, MSBuild elapsed4.45 seconds.
- Candidate contracts/ranked matching/refusal:1076 passed,0 failed,353ms.
- Parallel work/logistics/saved continuation:158 passed,0 failed,1042ms.
- Total:1234 assertions in two exact suites, zero final failures/aborted suites.
- The exhaustive oracle covers512 three-by-three bipartite graphs, checking both maximum cardinality
  and higher-ranked intent coverage. These repeated assertions are not1234 independent game scenarios.
- Only these two suite names ran, including same-name development repair reruns. No third/family/full
  gate.81 older test files mechanically migrate current build/save/system literals; compile only.
  SharedMachineDispatchTests also migrates the required chosen-worker argument; not rerun. Existing
  fixture visibility and shipment-priority authoring helper extend content composition, not gameplay.

## Behavioral evidence

Eight native/public incident/policy/worker compositions physically acquire distinct machines for the
parallel flexible and specialist frontier in the same game update. Neither node completes instantly.
Real travel, work, resource consumption and restart proceed through existing DAG/node/job owners.
Saved twins match; paused sole clock freezes both; another composed session remains unchanged.

Mixed native logistics and independent public shipment-source cases assign the higher-ranked haul to
the haul-only alternative while the uniquely compatible machine executes lower-ranked inspection.
One destination reservation is real, delivery takes time, source stock and saved continuation agree.
Pure explicit-carrier and provider candidate reads do not mutate hash/time. Private views hide foreign
intent. Unknown/malformed/disabled/busy selections refuse without replacing the actor or duplicating
claims. Candidate corruption injections exercise duplicates, missing advertised edges and wrong
priority. Selected source/cargo capacity claims each equal the actual eight-unit load.

Shared-bill contention proves worker capacity cannot duplicate stock. Four available units satisfy
one four-unit node; the other waits. Player commands enable ordinary replenishment, dispatch a real
courier and stop repeat requests after pickup; the acquired load still travels and enables completion.
One-worker shortage sequences parallel intent without duplication/deadlock. Necessary emergency
preemption preserves selected specialist identity, returns old work to its original queue and later
resumes it. A separate idle-preference case proves matching does not commandeer a busy machine merely
to serve lower-ranked work. Live zero-rate operation stalls its incumbent while the other node finishes;
restoring the operation resumes real progress. Interrupted saves preserve all these current owners.

## Development repairs

Initial build caught foreach on non-enumerable ComponentList; corrected to its native indexed API.
Another build caught an invented reserve-access enum member in proof content; corrected to actual
Protected policy and explicit player permission for ordinary replenishment, not counterfeit emergency.
One physical assertion expected source16 but observed12 because the still-enabled stock policy
correctly posted another load after consumption. The player sequence now disables repeat replenishment
after first physical pickup. No timeout increase, engine fallback or weakened conservation assertion.

## Remaining scope

Matching optimizes currently eligible distinct actors, not globally optimal transport routing, shared
material/dock teams, whole workflow success or deadlines. Each fresh round models one acquisition per
intent and retains its existing attempts-per-pass budget; repeat loads re-enter normal ranked dispatch.
Company reserve protection and resource admission may truthfully reduce simultaneous actual capacity.
Evacuation/return cargo stay existing native behaviors, not fabricated merged-intent identities.

Provider corruption/global purity, larger allocation/performance/soak/fuzz/lockstep/replay gates, fuel,
congestion/traffic/preferred routes/deadlines, broader incident science/preparedness/team-supply
reservations/crew-material exemptions, full accounting and final framework acceptance remain open.
P9/P12 and the full goal are not complete. No Oil/Tycoon/host/demo/platform/Draw changes.

## Source fingerprints (SHA256)

Recorded below after final source and focused gate; documentation-only edits do not alter them.
All17 source/content fingerprints and14 current stage/evidence links verified after documentation
updates. Scoped git diff whitespace check passed; existing LF-to-CRLF notices are informational.

| File relative to repository | SHA256 |
|---|---|
| src/Beep.SimEngine/Standard/WorkContracts.cs | E8CFF272BA33FB0FBDB9B156CC2F8C74AF6B7C42B888B91791A4E7C32B47C189 |
| src/Beep.SimEngine/Standard/DistinctWorkerMatching.cs | B6F5A7285AEDD5D0DA0489977BD23902DA0379DDF702C758F066FFBC3634B927 |
| src/Beep.SimEngine/Standard/WorkScheduling.cs | 935210112DA91C4F7273420616F2B4BD1019B2BFFF3A346ECE7E0140B87F5987 |
| src/Beep.SimEngine/Standard/Work.cs | 3970DFFE894426D9163C86D5F9605638D6EB0BF2C0280BD54335B652B3B08246 |
| src/Beep.SimEngine/Standard/Logistics.cs | BA98CBF2ABD5CD96DDD440D0BB85DC39F622AEDC8A6070D2DBE5AD58388C0619 |
| src/Beep.SimEngine/Standard/LogisticsScheduling.cs | 63CD588613B014B648FAE80CE77E9B96CF70FF7950A6C249A9F96D65F8CD85F9 |
| src/Beep.SimEngine/Kernel/SaveGame.cs | E66EFE2447E277FDFAB0B9825EC45EA4615618D51D4AFA5EC2C786B9324EA54D |
| src/Beep.SimEngine/Hosting/GameClock.cs | 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31 |
| tests/ExtensionProof/DispatchSourceProof.cs | 0475D1BB3F2F28928FDCE6123C2A5E2D93E395921C416F4CA5A25A68C37B11FE |
| tests/Beep.SimEngine.Tests/DistinctDispatchTests.cs | C5863D2FC627607D29F8B87B73298194A0543F16E9F7FE4446297DDAF181CE17 |
| tests/Beep.SimEngine.Tests/Program.cs | 6F63E6856E6680263FB19F116579CF11B41EC64C25FC1024F9C32459229C04FC |
| tests/Beep.SimEngine.Tests/DispatchAgingTests.cs | 64BBED90ABE230080F68A56F586594CA4EB7E6034DE4DEA468207D221036A5F0 |
| tests/Beep.SimEngine.Tests/IncidentPreparednessTests.cs | D0A648B9210AF48DFAA494FC74127A91635DAF949DB2452E6748A06727D33FD1 |
| tests/Beep.SimEngine.Tests/SharedMachineDispatchTests.cs | B09605060930E173B58DD275D7F71EDE5CF3041412A66A3CD6F84CFB752DE3E1 |
| tests/ExtensionProof/content/incident-preparedness.json | 7D4B5170673E50258F30A00973166B0FFCBBF174D6AD35CD4EAA85D931BB9481 |
| tests/ExtensionProof/content/shared-dispatch.json | C53FBD1216709F6310484679966BD931D93393C721AAD18180C1E4B1111F75B4 |
| tests/ExtensionProof/content/dispatch-source.json | 02511EB7142AAD598E52A6D6215A8740ABABCC4037289177C181A18AF53EBB95 |
