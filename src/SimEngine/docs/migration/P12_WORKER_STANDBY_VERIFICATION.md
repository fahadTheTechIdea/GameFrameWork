# P12-D7 — Emergency-only worker standby evidence

Date: 2026-10-06. Scope: Beep.SimEngine only.
[Design](../../plans/enhancements/P12_WORKER_STANDBY_DESIGN.md).

## Protocol and ownership

Build beep-simengine-2.159.0; save86; WorkBoard27 unchanged; WorkSystem21; private WorkView50;
native work_policy codec3; independent public proof codec1. Stock/cargo/dock/incident/worker/clock
payload layouts remain unchanged. Existing recurring native6/public4 codecs remain unchanged.

WorkPolicySettings alone owns enabled/priority/roles/area/interruptibility/emergency eligibility and
the new explicit All/EmergencyOnly assignment mode. The required seven-argument constructor, strict
assignment_mode JSON and shared Sync replace the prior protocol directly. No old overload, reader,
implicit authored mode or policy mirror survives. Required states.standby resolves through the existing
EntityStateBindings and shared state resolver; Entity.Update alone commits actual state.

WorkSystem's existing IWorkerDispatch.CheckAssignment applies standby before a new ordinary worker
claim. Actual bound response and logistics demand supply emergency classification through the existing
WorkBoard/logistics owners. Numeric player priority and company Emergency alerts do not substitute
that identity. Disabled, role, area, route, resource and normal worker gates remain authoritative.

An ordinary incumbent continues only when the actual live job, company, role, both Job.Worker and
IWorker.Job links and requested remaining destinations agree. Invented IDs, unrelated stops or
one-sided relations refuse. Cancelled or preempted ordinary work must reacquire under current policy.
Loaded ordinary hauls retain their actual cargo, promises and worker until physical delivery finishes.
Standby is availability policy, not an additional reservation or minimum fleet guarantee.

Existing ConfigureWorkPolicyCommand and private preview share pure admission and exact settings
acknowledgement. A public provider ignoring the requested mode or severing the actual worker link
faults normal Update; no resumable partial checkpoint or repair fallback is introduced. Read/check
purity uses existing scoped guards, not a claim of global rollback. IWorkerDispatch.RefreshPolicy
exposes the already existing refresh owner for native/public policies; no second scheduler is added.

ExtensionProof.ProofWorkPolicy owns its own settings/state Sync, implements IWorkPolicy and
IEntityStateSource, and shares engine validation/state/refresh contracts without wrapping a native
component or using internals. Existing ContractWorker proves substitutability. Embedded
worker-standby.json contributes policy/state/area authoring to the test factory's existing
master-listed documents; the engine still uses normal master-manifest loading and validation.
Tests submit player intents/controls and drive Update; selection, claims, movement, spending and
response effects all execute inside the engine or its public-contract extension providers.

## Final focused gates

Working directory: C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine.

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Worker standby: authored settings, private controls and shared admission"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Worker standby: physical response, emergency supply and retained work"
```

Final build: zero warnings/errors; MSBuild1.86s (2.45s command). Contracts168 passed/0 failed/546ms.
Physical326 passed/0 failed/1016ms. Total494 checks/two exact suites; zero thrown/aborted suites.
Only these names ran; same-name repair reruns were used. No third, family or full runtime gate.
Following changes are documentation only; verified source/content/test fingerprints appear below.

Contracts prove explicit native/public settings, detached private views, foreign refusal, ordinary
max-priority refusal, alert non-authority, no refusal-side job/stock/identity allocation, required
mode/state authoring, invalid enum, unknown/truncated settings preserving state, pending command save/
load, actual entity standby state, disabled/emergency eligibility/role/area/closed-route protection,
ordinary haul refusal, and dishonest public mode/read/check/worker-link rejection.

Physical scenarios cover all four native/public policy × worker combinations for dry response,
incumbent ordinary work, loaded hauling, cancellation and emergency preemption. Public workers have
one explicit role per authored fixture, not an unproved multi-role implementation. Actual automatic
emergency input hauling followed by response work is tested with native multi-role workers under both
policy providers. Loading, delivery and work take actual game updates; stock moves and is consumed
once. Cancellation releases held tools without refunding spent inputs. Preempted ordinary work retains
progress in its existing queue but cannot reacquire a standby worker. Same-world saved twins, edited/
pending/ended saves, isolated sessions and pause of the sole GameClock remain exact. Physical waits
are bounded at 300 game updates with job/state/blocker/toil/load diagnostics.

The first build found a test comparing fixed-point zero with uint WorkProgress.Fraction; the assertion
now uses the actual uint representation. No engine progress/time semantics changed for this repair.

Mechanical migration: 73 older test files update69 build/66 save/5 WorkSystem assertion lines;
23 test files explicitly supply All in44 old constructor calls;12 JSON files migrate15 profiles and
three inline JSON profiles supply assignment_mode and standby bindings. A read-only inventory found
all16 standalone authored work-policy rows include both required fields (including the new profile).
Older profiles explicitly bind standby to their existing authored blocked/stopped state and explicitly
select All; this is authored intent, not an engine fallback. Older suites compile only. D6d and all
earlier runtime gates are historical, not rerun or added to494. Focused green is not full regression.

## Remaining scope

One GameClock remains unchanged, constructed once by World and reused by SimGame. Update index is
that clock's coordinate, never another timer. WorkBoard retains all jobs/claims; WorkSystem dispatches;
workers retain job/home; ports own real stock; incidents own response identity. No separate fleet
roster, scheduler, queue, clock, reserve cache or instantaneous response/material grant was added.
No OilFieldDays/Tycoon/host/demo/platform/Draw implementation was changed.

Full goal remains incomplete: automatic company minimum-ready quotas, reserve stocks/maintenance
exemptions, fuel/traffic/congestion/route budgets, incident science/contamination breadth, global
callback purity/rollback, wider provider/corrupted-save matrices, replay/fuzz/soak/stress/performance
and authorized full acceptance gates. The rest of P2–P12 remains governed by the master tracker.

## SHA256 of verified source

| File | SHA256 |
|---|---|
| Standard/WorkPolicies.cs | 4152324CF398BD5CCFCD9CCC39FC7C9CC81F1C7BB80F728A494475CA7E14193C |
| Standard/Work.cs | 6ACA6B74C98985E0148393808D416DD7FB14871EA99A11D427E297A63C4D9A85 |
| Standard/WorkContracts.cs | 01E6D55AA4893A82411732C6921C1D03F363D1D07D09AA35D1AB31801738BC80 |
| Commands/WorkPolicyCommands.cs | 83047C3EB443E50D417994EFBEC0B24D276DEF154EE72AB69B9E4A346A5C12AA |
| Views/WorkView.cs | A6FF66A011BC9340941C354BF18DC2062D003F8BE4720E33E09378CE4D810A16 |
| Hosting/GameClock.cs (unchanged) | 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31 |
| proof/WorkPolicyProof.cs | 4FEEA8FBBA282E16A1A8E3A0809B86E59563E77C71915212465448528F8F0D0F |
| proof/content/worker-standby.json | B799E5308625AE23FD5A8B60FD069CFFEF19C111E17B4C959A201EC57AD8D783 |
| tests/WorkerStandbyTests.cs | B7C59EB697C986763ACA65ADE6627F4CD7FFC2581FF3C8F7962BA3AEBD74AAA9 |
