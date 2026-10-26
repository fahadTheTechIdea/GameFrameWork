# P12-D12 — Distinct-worker reserve coverage verification

Date: 2026-10-06. Working-tree evidence, not clean-HEAD/full-regression proof.
Scope: Beep.SimEngine only. [Stage](../../plans/enhancements/P12_DISTINCT_WORKER_RESERVES_DESIGN.md).

## Current identity and responsibility

Build `beep-simengine-2.164.0`, save91, WorkSystem25, company policy3/company view3. WorkBoard27,
work view51, native work-policy4/public policy2, logistics10/view18 and recurring codecs unchanged.
Existing company settings/JSON/commands/shared Sync remain the only reserve-policy owner. Persistent
layouts are not duplicated; semantic/projection changes have explicit current composition identities.
The five-argument snapshot constructor replaces the old constructor; no compatibility overload or reader.

CompanyWorkerReserveRules.ReadyRoles reads actual live company workers through existing IWorker,
execution and individual policy contracts. Busy/held/disabled/not-emergency-eligible/filtered/zero-rate/
execution-blocked actors do not provide capacity. Scoped existing pure-read guards remain active.

One transient Capture sorts actual worker IDs and authored role IDs, then uses iterative augmenting
paths with one worker capacity and authored role capacities. It maximizes total covered slots while
preserving earlier ordinal roles when capacity is scarce. It does not greedily consume flexible actors,
create a second worker roster, retain a cache, reserve workers or allocate an object per requested slot.
Aggregate requests/shortage use checked Int64. Billion-slot requests operate over only actual fleet
and authored role lists; this is not a measured large-fleet performance claim.

CompanyWorkerReserveSnapshot adds CoveredWorkers; Shortfall now uses distinct coverage. ReadyWorkers
and Headroom remain raw role compatibility facts, NOT simultaneous coverage or ordinary assignment
permission. CompanyWorkerReserveCoverage copies/sorts immutable role/assignment facts and validates
identity, uniqueness and row-count agreement. Illustrative assignments are not acquired jobs/claims.
ReadCoverage uses the existing company policy; CompanyAlertView applies the existing private boundary.
Unknown company/role errors remain typed. Foreign private reads refuse before company lookup.

Ordinary assignment compares the same fresh computation before/after excluding the actual candidate:
maximum covered capacity must not fall, and any decreased raw protected role count must retain its
floor. Existing WorkSystem derives genuine emergency/opted-in self-service/incumbent exemptions. No
caller priority or coverage projection creates exemption authority. Policy edits do not claw back an
already acquired worker or loaded delivery. Existing physical jobs/resources/docks/effects do the work.

Exactly one unchanged GameClock is constructed in World and reused by SimGame/entities. World.Tick
and entity update coordinates project its StepIndex. Matching has no timer/current time or update loop.

## Content and callers

distinct-worker-reserves.json is explicitly embedded and merged into the existing master-listed points
document by fixture authoring, not independently advertised as a manifest filename. Existing activity,
resource, role, policy and incident files remain authoritative; fleets are authored entity clones.
Public single-role workers/native multi-role workers and independent public/native work policies
participate through current contracts. One public worker/policy/recurring-source self-service scenario
uses the existing engine-side provider and physical maintenance workflow.

Tests submit existing commands/public actions, call Game.Update and observe engine facts. The small
exhaustive oracle verifies maximum cardinality independently; it never simulates/assigns engine actors.
78 older test files received mechanical current identity/schema/projection migrations; they compile
only. ReservedSelfServiceTests.Fixture visibility changes only to reuse authored setup. No new test-side
gameplay, native wrapper, host/demo logic or duplicate engine system.

## Final gate

After all source/test/content edits:

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Distinct reserves: maximum coverage, private contracts and deterministic matching"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Distinct reserves: physical dispatch, emergencies and saved continuation"
```

| Gate | Result |
|---|---|
| Build | 0 warnings, 0 errors; MSBuild 3.08s |
| Exact contracts suite | 366 passed, 0 failed;474ms;1 suite |
| Exact physical suite | 128 passed, 0 failed;528ms;1 suite |
| Total focused evidence | 494 assertions/2 suites/0 final failures or aborted suites |

Counts are assertions, not 494 independent scenarios. Only these exact two names ran, including repair
reruns. No third/family/full, Oil/Tycoon/host/demo/rendering/platform gate. D11 and earlier not rerun.

## What passed and what failed during development

Contracts cover overlapping multi-role capacity, iterative reassignment, existing shortages, multiple
slots per role, stable identity/ordinal allocation and reordered authored rows across 27 small fleet
combinations. An independent enumerative oracle checks maximum cardinality at capacities1/1 and2/1.
Wide requests4294967294/actual coverage2/shortfall4294967292 do not invent capacity. Native/public
policy controls, public single-role workers, actual role/rate/eligibility exclusions, private/foreign/
unknown queries, explicit empty floors/unprotected known roles, detached read-only projections and
invalid/duplicate/mismatched facts pass. Hash reads do not change jobs, claims or game time.

Physical tests run both native/public policies: ordinary maximum-priority work cannot take either of
two distinct reserved actors; a genuine incident uses one, travels/repairs and returns it. Busy coverage
falls2 to1 then returns2, without permitting the unrelated backlog. Lowering explicit floors admits
paid work; raising them preserves incumbents; completion consumes eight materials exactly once and
releases held tools. Cancellation restores coverage but does not refund spent materials or allow fresh
ordinary acquisition under restored floors. A spare haul-only actor delivers four physical units while
the sole inspector remains protected; saved partial cargo survives increased floors and returns three
ready slots. Public worker/policy/source self-service consumes three spares, repairs actual condition
and restores coverage4 after actual bay/work/departure, without classifying service as an emergency.
Saved twins, independent session isolation and sole-clock pause/resume pass. Physical waits are bounded
at300 game updates with job/blocker/worker/toil/cargo diagnostics.

One initial compile failure used a nonexistent JSON Items property; current index-based content
authoring fixes it. Initial tests expected lookup refusal before the private boundary and dereferenced
a completed job's intentionally released worker. Corrected tests query the policy contract for unknown
companies and retain the actual carrier identity before completion. No engine behavior was weakened,
no timeout expanded and no failed attempt counted as final success.

## Source fingerprints (SHA256)

| File relative to repository | SHA256 |
|---|---|
| src/Beep.SimEngine/Standard/CompanyWorkerReserves.cs | 6E85DF6DF5FA97A3227D6947D84C0A4D8DB9D07E463178B0CDB3A96055BDBC83 |
| src/Beep.SimEngine/Standard/CompanyAlerts.cs | B5CC21089A8BCD187CA6800E5EB6DE0428CB64A66B92E8807F6C4FC0F55BAAC2 |
| src/Beep.SimEngine/Standard/CompanyAlertModule.cs | 26BF042383E0FC5A83C44B42E91254F886972E6C602539B4E86051B082BF9933 |
| src/Beep.SimEngine/Views/CompanyAlertView.cs | 299A05A62FEFEEBAF38BAC5ECF7C3086D1ACFD5A20C1C7A7B83AA09385B96985 |
| src/Beep.SimEngine/Standard/Work.cs | EFCA655FCB38521A479CD24E80D937D4CFA6521CF8ED497CE2364C8A5DC476C6 |
| src/Beep.SimEngine/Kernel/SaveGame.cs | 1E8A68E244DC64B6D8A807458EB8E3AC98B4231B110039D8BC7732AA5D04DBBA |
| src/Beep.SimEngine/Hosting/GameClock.cs | 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31 |
| tests/Beep.SimEngine.Tests/DistinctWorkerReserveTests.cs | 59833C62AA4B2C0D00C20D44DF744E630560FFB7795394FE2D3F13EC1D8FD5C6 |
| tests/ExtensionProof/content/distinct-worker-reserves.json | 18658B003D9BF618E79345DDDE7408ADFC6A6340C6CAB062F647FCF5D6398766 |
| tests/ExtensionProof/ExtensionProof.csproj | 1225D6E475637CFBC0002B90D8653F8FFAEAE4000AF995384DD6AB1C41F07F39 |
| tests/Beep.SimEngine.Tests/Program.cs | C24FBBEBD84E627E9BD4A7AE15D31AB24477DF65222F57B36406554F1D8111EC |
| tests/Beep.SimEngine.Tests/ReservedSelfServiceTests.cs | 0FAB594C830B876165FA628D4615134A6BB90A9F198A3FA3FAC5D3A13283FC14 |

## Remaining requirements

Final documentation check verified all12 recorded source fingerprints and9 current stage/evidence
links. Scoped tracked-file diff whitespace check passed; only existing Git line-ending warnings.

This proves simultaneous role capacity, not target-specific route/material/fuel/tool/bay readiness,
multi-stage response teams, actual worker reservations or every provider/operating-area combination.
Target-aware preparedness, crew/supply/material exemptions, larger-role/fleet performance budgets,
general incident science/contamination/propagation breadth, global purity/corruption matrices,
logistics congestion/fuel/dock breadth and complete environmental accounting remain open. Replay,
lockstep/fault/fuzz/soak/stress/allocation/full acceptance gates are not established by this stage.
P2–P12 and the full objective remain active and incomplete. No full regression authorization inferred.
