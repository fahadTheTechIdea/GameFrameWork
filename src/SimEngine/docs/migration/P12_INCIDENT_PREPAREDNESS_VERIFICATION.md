# P12-D13 — Incident preparedness verification

Date: 2026-10-06. Working-tree evidence, not clean-HEAD/full-regression proof.
Scope: Beep.SimEngine only. [Stage](../../plans/enhancements/P12_INCIDENT_PREPAREDNESS_DESIGN.md).

## Current identity and ownership

Build `beep-simengine-2.165.0`, save92, WorkSystem26, incident view4, native incident registration2/
public incident registrations3. WorkBoard27/work view51/company policy3/view3/worker-policy4-public2/
logistics10/view18/recurring codecs unchanged. Existing persistent layouts remain sole state owners.
Native/public IIncidentResponse.ResponsePlan always exposes its actual authored definition, independent
of inactive/active episode facts. Conditional inactive-null getters are replaced, not wrapped/fallback.

IIncidentPreparedness aliases the existing WorkBoard. Its target query resolves actual named response,
composed definition and episode/order binding. It reads only the declared DAG and actual node records;
never creates a temporary incident, order, execution record, scheduler or copied plan. Shared native
DependenciesDone determines the current frontier. Future nodes show criteria under current facts but
are not current work; completed/completing stages do not demand replacement workers.

IWorkerDispatch.CopyResponseCandidates resolves actual target/source/node and delegates to the same
private candidate core as existing ordinary/active work. Prospective context permits emergency-eligible
reserved actors in a pure observation, never posts/preempts/acquires. Actual candidates use the real
response job/episode/execution. Existing roles, individual eligibility, area, route, dock, materials,
tools, cost and utility providers remain authoritative. Incumbent criteria/coverage are tied to their
actual worker; another idle offer cannot hide a blocked acquired actor.

The checks exposed a pre-existing zero-rate admission gap: CheckWorker inspected execution blockers
but not effective positive rate. The shared owner now rejects both; ordinary work cannot acquire a
zero-rate actor or consume its bill even with standby and company floors removed. This is not an
extra preparedness eligibility rule or detached view-only filter. Execution retains physical claims
and incumbent pause/resume through existing job behavior.

DistinctWorkerMatching is the one internal stateless iterative maximum-capacity algorithm, extracted
from D12 and reused by both company floors and parallel response frontiers. Callers supply real
eligible worker IDs and canonical demands; one actor covers at most one current machine node. No
slot-per-billion graph allocation, roster/cache, acquired reservation, new stock ledger or clock.

IncidentPreparednessSnapshot/Node contain copied immutable IDs/dependencies/candidates/criteria,
source/plan/episode/native order and state, current-frontier capacity and sole-clock observation.
IncidentsView applies existing private target visibility; refused reads return no partial snapshot.
ReadyForFrontier means current individual criteria and distinct worker capacity ONLY. The same source
stock or dock can appear individually feasible to several nodes; this is not atomic shared feasibility,
future workflow success, actual team reservations, supply scheduling or guaranteed arrival/deadlines.
One actor may eventually sequence parallel intent even when the current frontier has a worker shortage.

Exactly one unchanged GameClock remains constructed once in World. World.Tick/entity update coordinates
project StepIndex; preparedness cannot move time. No platform Draw/lifecycle implementation was added.

## Content and test boundary

incident-preparedness.json is explicitly embedded, then merged into the existing master-listed work/
points documents by fixture authoring; it is not an independently listed manifest filename. It authors
alternative roles, parallel stabilization/repair, dependent restart, actual input requirements and
native phase mappings. Native/public incident × policy × worker gives eight combinations through the
same engine logic. Tests submit commands/actions, call Game.Update and inspect current engine facts.
Renew is an explicit stale-episode fault injection through the existing public provider, not gameplay
inside a host/test. Small matching inputs are algorithm verification, not another gameplay coordinator.
79 older test files mechanically migrate current build/save/work metadata; they compile only.

## Final scoped gate

After all source/test/content changes:

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Incident preparedness: authored response, private DAG facts and distinct frontier"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Incident preparedness: physical response, live blockers and saved continuation"
```

| Gate | Result |
|---|---|
| Build | 0 warnings, 0 errors; MSBuild 3.38s |
| Exact contracts suite | 265 passed, 0 failed;959ms;1 suite |
| Exact physical suite | 172 passed, 0 failed;1317ms;1 suite |
| Total focused evidence | 437 assertions/2 suites/0 final failures or aborted suites |

Counts are assertions, not 437 independent scenarios. Only these two exact names ran, including
same-name repair reruns. No third/family/full, Oil/Tycoon/host/demo/platform/rendering gate. D12 and
earlier are not rerun; embedded overlap/matching checks are scoped coverage, not the entire D12 suite.

## Covered behavior and development failures

Contracts prove always-authored inactive native/public plans without invented incident/order state;
actual IDs/dependencies/parallel/sequential/timed frontier semantics; alternative-compatible machine
reassignment; immutable retained nodes/dependencies/candidates; private/missing/named-source refusals;
same-board ownership and no extra codec; reads preserve hash/time; changed area/route/eligibility/rate/
material facts use existing typed blockers and resource availability. Current input shortage is actual
available0/required4, not a fabricated top-level quantity. Explicit zero-rate normal-work refusal has
no acquired worker or consumed material. Cycle admission uses the existing DAG compiler; impure policy
reads fail existing scoped guards. A forced three-demand augmenting chain verifies the shared matching;
large capacities remain bounded by actual identities; D12 overlapping company floor refusal stays intact.

Physical proofs cover all eight provider combinations: actual incident creates the native response
order, uncompleted ancestors keep restart out of the frontier, real work completes both predecessor
nodes, repair consumes four target inputs once, then the released inspector works on restart. Actual
completion effect resolves the current episode. Active snapshots point to real native node/order state,
not a parallel simulation. Partial saved twins remain exact; a paused sole clock freezes everything;
interleaved untouched sessions stay isolated. A retained preview cannot override a later area edit;
dispatch waits for the repaired policy. Zero-rate incumbent work pauses honestly, restores and continues
physically. A single multi-role worker eventually sequences the parallel intent without becoming two
simultaneous machines. Cancellation resumes the existing episode order; a renewed actual episode never
reports the older execution, and the next actual update alone performs posting/cleanup. Saved renewals
match. All waits are bounded at300 updates with current job/blocker/worker/toil diagnostics.

An initial contract run had four failures: the real zero-rate admission gap, an incorrect assumption
that resource shortage uses the top-level Quantity instead of existing per-source availability, and
two mistaken assignment expectations for a non-forcing matching graph. Fixed the shared zero-rate
engine check, read the real typed availability, and strengthened the matching case to force a genuine
three-demand augmenting chain. No timeout grew; no failed attempts are counted as final success.

## Source fingerprints (SHA256)

| File relative to repository | SHA256 |
|---|---|
| src/Beep.SimEngine/Standard/WorkIncidentPreparedness.cs | E0BEC7B69750FA517513B4773B951EFD3CF71EBDB72E89A5D9910C66FCCACC01 |
| src/Beep.SimEngine/Standard/DistinctWorkerMatching.cs | 8B82B12993E91AFAFDAE805A63FC8A0D087C9F0FF5132ADAA8E16D21DA29E8C4 |
| src/Beep.SimEngine/Standard/CompanyWorkerReserves.cs | 4104320750694BF5E510412AB7D1AB5981E0E537EB8D0DF3FB036D935C4B6E49 |
| src/Beep.SimEngine/Standard/Work.cs | 2246AE65DD869F8E25420E5AD5A01D82539FC7B81A20ED22F4313BCF346B4F1B |
| src/Beep.SimEngine/Standard/WorkContracts.cs | CA8570496EACD01B198DD3E111B94733FBF7F4632CECEEF1612861CD0E8C962C |
| src/Beep.SimEngine/Standard/WorkModule.cs | 3216501C778D271F0786DE3E4683EBAB27627F1873DD2DCB012DBFA244C6AF98 |
| src/Beep.SimEngine/Standard/Incidents.cs | 55010A625D5C39A553245CB150300FBF44A903FBF144997A999AD0D654CDEEB8 |
| src/Beep.SimEngine/Standard/IncidentWorkflows.cs | B2C5F047B6D5EF4A585DB42DB666B992D436BBF1E066DDC287A767E08036A8FC |
| src/Beep.SimEngine/Standard/IncidentResponseModule.cs | 831D137CAE049B75D1DB9870880CD9EE27678E9A12CFE375F8AEC905115985BA |
| src/Beep.SimEngine/Views/IncidentsView.cs | D104E8618FDC1D928B8E70D2A75FD39D7AD298C354F26B6CF31BA0F5B8B9338D |
| src/Beep.SimEngine/Kernel/SaveGame.cs | 52066A16CBCCAC2A1968B835C0B970AD59FA7CD1A8164B35B38AAF36BF21BB8E |
| src/Beep.SimEngine/Hosting/GameClock.cs | 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31 |
| tests/Beep.SimEngine.Tests/IncidentPreparednessTests.cs | 9B9DEC679B110B1923212859897AFF02FA1E9DE178F09D030784D4B210A26286 |
| tests/ExtensionProof/ContractIncidents.cs | F89779FECAB15148EA439F51653CD182F6C07D34367BBD33FBF7963D80353564 |
| tests/ExtensionProof/ColonyGame.cs | 37CC7203E8E18BD95669284FFF3AE3D353FF474717C516F87FDA0B88A7A5E2DB |
| tests/ExtensionProof/DispatchSourceProof.cs | 78ABD3EF84880DC5C5D5134C185E0CD2A14E0A2B3FE8F30899C1034F52E23FDA |
| tests/ExtensionProof/LogisticsDemandProof.cs | F1FDBFFD0AB5274181EB64353BD8E26B54F7E152A6244ED210F4A8BE982059E7 |
| tests/ExtensionProof/content/incident-preparedness.json | 7D4B5170673E50258F30A00973166B0FFCBBF174D6AD35CD4EAA85D931BB9481 |
| tests/ExtensionProof/ExtensionProof.csproj | 28A103ACB5037BDEBECB565C2716B4693FE790EA803123E62FCDAB22CEBF6EF3 |
| tests/Beep.SimEngine.Tests/Program.cs | 90AF4379D7ED7F3851E201BF428B2B5D9A70AE10C2299E626E03B6D6758E90B9 |

## Remaining requirements

Final documentation check verified all20 recorded source fingerprints and9 current stage/evidence
links. Scoped tracked-file diff whitespace check passed; only existing Git line-ending warnings.

Target-aware policy/reservation decisions, atomic team/material/dock feasibility, general crew/supply/
material exemptions and every role/worker/provider/purity/corruption matrix remain open. Current costs/
tools/utilities use the shared evaluator; their complete cross-provider acceptance is not established
by these focused scenarios. Larger fleets/graphs/deltas/allocation budgets, fuel/congestion/service
envelopes/deadlines, incident science/weather/network/contamination/accounting breadth and full
scenario/rival pacing/replay/lockstep/fault/fuzz/soak/stress gates remain required. P2–P12/full objective
remain active and incomplete. No complete-plan claim or full regression authorization inferred.
