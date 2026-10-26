# P12-D11 — Reserved self-service verification

Date: 2026-10-06. Working-tree evidence, not clean-HEAD/full-regression proof.
Scope: Beep.SimEngine only. [Stage](../../plans/enhancements/P12_RESERVED_SELF_SERVICE_DESIGN.md).

## Current identity and owners

Build `beep-simengine-2.163.0`, save90, WorkSystem24, work view51, native work-policy registration4,
public work-policy registrations2. WorkBoard27/logistics10/logistics view18/company policy2/view2/
native recurring6/public recurring4 remain unchanged. No old binding constructor or missing-field
compatibility reader survives. Existing valid all/emergency_only modes remain real current policies.

Existing WorkPolicySettings owns EmergencyAndSelfService through strict assignment_mode JSON,
commands/private previews/shared Sync/exact acknowledgement. Idle state uses the same entity state
resolver, not a maintenance state machine. Unknown enums reject; saved wide values cannot wrap to
a valid byte enum. The accepted protocol changes explicitly with build/save/module metadata.

Existing RecurringPlanBinding now requires ReserveUse and reserve_use:none/self_service JSON.
Composition validates nonempty positive-time machine node bindings and actual worker capability.
This is declared game content, not scientific certification of every authored node's purpose.

WorkBoard's stateless IsSelfService checker reads its own actual active plan origin, node execution,
exact selected worker, graph/definition identity, live named recurring source and current last-plan
history. Prospective facility selection uses the same checker with the actual source/node. Normal
dispatch rechecks the admitted origin; a retained preview or caller boolean never grants service
mutation authority. CopyCandidates has one explicit nullable recurring source context, no legacy
overload. All ordinary calls supply null; the engine facility query supplies its actual source.

WorkSystem's existing assignment calculation passes only engine-derived self-service authority to
individual standby and skips the company floor only for an explicitly opted-in self-servicing worker.
Manual/unbound jobs, maximum player priority, unrelated workers and ordinary hauling cannot qualify.
Maintenance remains ordinary, not emergency-classified; materials retain ordinary stock protection.
Existing enabled/roles/area/execution/route/bay/resource gates remain on the shared path. Current
readiness remains derived: a worker with a maintenance job is not counted ready.

Existing jobs, node progress, docks, worker condition effects, resource claims and transactions alone
perform travel, tool holding, spare consumption, positive work, physical departure and repair.
Cancellation/mode edits use the same job ownership and continuation rules. No copied fleet, service
queue, reserve balance, claim store, scheduler or current-time owner is introduced.

Exactly one unchanged GameClock is constructed by World and reused by SimGame and entities. Update
numbers project its StepIndex; work/deadlines are measurements from it, not additional clocks.

## Content and public extension proof

reserved-self-service.json is explicitly embedded, then merged into the existing master-listed points
document by the fixture author. It is not an independently listed manifest filename. The existing
master-listed service-facilities/worker-service/company-alerts documents supply actual workflows,
requirements, effects, facilities and company policies. Native/public policy × worker × recurring
source combinations run through the same engine service selection and jobs (eight combinations).

The public proof module registers the existing independent ProofWorkPolicy under its owned component
name, without wrapping the native policy or adding gameplay. Existing test factories only pass
explicit authored module content; tests submit commands, advance Game.Update and observe engine facts.
79 existing files received mechanical identity/schema/binding migrations. Those older suites compile
only; neither compilation nor this focused proof claims their runtime gates pass.

## Final current gate

After all source/test/content edits:

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Reserved self-service: authored provenance, private controls and safe admission"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Reserved self-service: physical maintenance, reserve floors and saved continuation"
```

| Gate | Result |
|---|---|
| Build | 0 warnings, 0 errors; MSBuild 2.47s |
| Exact contracts suite | 258 passed, 0 failed;512ms;1 suite |
| Exact physical suite | 251 passed, 0 failed;873ms;1 suite |
| Total focused evidence | 509 checks/2 suites/0 final failures or aborted suites |

Counts are assertions, not 509 independent scenarios. Only these two exact suite names ran, including
repair reruns. No third/family/full, Oil/Tycoon, host/demo, rendering or platform gate.

## Coverage and development failures

Contracts prove strict native/public binding JSON; unknown/mistyped/missing purpose; empty/timer node
refusal; unknown constructor enum/wide codec rejection; exact mode round-trip and pending command
save/load; truthful prospective source-worker qualification; honest ready counts; pure/private views;
disabled and emergency-only refusal; manual exact-worker maximum-priority plans remain blocked and
consume no stock; reserve_use:none remains ordinary despite recurring provenance.

Physical proofs show eight actual reserved machines admitted from real source origins, no emergency
classification, ready count4 to3 while busy and back4 after completion, no instant condition repair,
exact three-spare consumption and held-tool release, untouched unselected facility, partial active
save/twin continuation, sole-clock pause/resume and independent-session isolation. Cancellation plus
standby in the same update releases unspent claims, creates no repair and prevents fresh service
posting. Explicit re-enable reposts a new real plan; an acquired incumbent finishes after the opt-in
is removed. Interrupted saves spend inputs once and twins match. Missing spares/tools earn no posting.

Every physical wait is bounded at 300 game updates with actual job/blocker/worker/toil diagnostics.
Development compile errors and the missing resource embedding were corrected before successful gates.
An intermediate cancellation test wrongly issued standby one update after cancelling an enabled
recurring source; the engine legitimately reposted first. The final test submits standby and cancel
together before the next gameplay phase; no engine behavior was weakened and no timeout increased.
Earlier failed attempts are not counted as final successes.

## Source fingerprints (SHA256)

```text
src/Beep.SimEngine/Hosting/GameClock.cs 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31
src/Beep.SimEngine/Kernel/SaveGame.cs 1ABE70CF4C26DFC846400AEAB12D6865EBA3E05A18A6CF00EEFA6E799B5E5669
src/Beep.SimEngine/Standard/WorkPolicies.cs C3E9210E314C58C998A6F49D2E281A867368CA14E2AB195F37F5468DAEF35DC3
src/Beep.SimEngine/Standard/RecurringPlans.cs 408A841F232FD2F53E94CD86D2B727BA797B1591EB9239084C0D7708E9B833CD
src/Beep.SimEngine/Standard/WorkRecurringPlans.cs EA36A9A7FB9CD69DC92AEF65B1661637EE41AA9B70DDFAD5108AC1A2B2D0E814
src/Beep.SimEngine/Standard/Work.cs B7611124A03B53B04B59D8FDF0A552834FEE945671EE738ADE48FD1B1E76AA97
src/Beep.SimEngine/Standard/WorkContracts.cs F62BEB32FE50F25868D07055807FC6A794057E85B8001AA490EA6F4632868EE9
src/Beep.SimEngine/Standard/WorkFacilities.cs 6378E1A9C4175D54C6DDB81F385E18F388EEFD29761BA4DCCD26D481B51BBAE9
src/Beep.SimEngine/Standard/WorkScheduling.cs 6421E8FACDDBC4444F2658A4BD20FDDEF624DEB0F135C4FCA7C7A31CFC508FCA
src/Beep.SimEngine/Standard/WorkModule.cs 0382887EED96145CC5186121534D6CBF4DC09AB08611C86F0A4D439C0873C4C1
src/Beep.SimEngine/Views/WorkView.cs 753808638776A9275CA4CA99D771F8F89913331F095D8CA5D9A3B26C00963FC2
tests/ExtensionProof/WorkerServiceProof.cs 5A7F41ED27E312659378A5D20407C123A847F0163FA25268F5AD1D78F5DBCB05
tests/ExtensionProof/DispatchSourceProof.cs 2B72E9D65E32467CF1912574C020F3FDE1EB836C38E90BC08E18BE9F90E82E50
tests/ExtensionProof/ExtensionProof.csproj F236CD3B31ED5FC1F48A175EBADF21DDCEA47AB7B9BF31C876F44DE46C12B234
tests/ExtensionProof/content/reserved-self-service.json 63332E00703EFB8E60C2F26FD80D7408725F64D86F0E34F237FC0A237DC5933D
tests/Beep.SimEngine.Tests/ReservedSelfServiceTests.cs 0C31A1C9F24F3ED9A4D5F17DD1765731ABC2C3B48FEC42F116571D28D3C34660
tests/Beep.SimEngine.Tests/Program.cs A3A74663934E457792EBD744A6E614BA63794395C1257478007D4CB7E252E355
tests/Beep.SimEngine.Tests/ServiceDockTests.cs 81CCC6B3EB4C4B119AF9A3F11B8D2C174B1DB4EC62ABA68CFC1E577BCD2E39FF
tests/Beep.SimEngine.Tests/WorkerServiceTests.cs 382785C96F332F8BE4E5FFAF43625B6E640BE58AA96DBC4B5A5D502F136622A2
tests/Beep.SimEngine.Tests/FacilitySelectionTests.cs D5EBED07B05B68DDA36EFD86FB327DC75542D2EB446F7EC41359997F7F035007
```

## Remaining scope

P9/P12/full framework remain partial. This proves self-maintenance opt-in, not arbitrary repair crews,
supply-worker/material exemptions, guaranteed target/distinct-role response preparedness, full provider
replacement/purity/corrupt-state matrices, all area/route/dock combinations, fuel/traffic/science/
contamination/native incident formulas, full resource accounting, replay/fuzz/soak/stress/allocation
or whole industrial acceptance. D10/D9 and earlier runtime gates are historical/not rerun.

