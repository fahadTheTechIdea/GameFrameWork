# P9-L1 current atomic-haul baseline — focused evidence

## Current M66: compound logistics operation admission — 2026-10-22

Native **2.721.0/save161/replay2**, master HEAD `cb61483ab81404c11944931aedb4703ba694edb5`
plus preserved existing work. [Existing design](../../plans/enhancements/P9_ATOMIC_HAUL_BASELINE_DESIGN.md),
[C3 obligation matrix](../../plans/enhancements/P7_OWNED_RESERVATIONS_DESIGN.md#current-c3-audit-m62).
The original221 requirements remain210 source-recorded checked/11open. No original closure or
full-goal completion claim. This completes the selected compound-haul boundary, not all C3 accounting.

### Observed failure and one-owner repair

M65's compiled census identified three direct purchase claim calls and one supply release in the
original LogisticsService. They straddle job creation/assignment and surviving-claim release; resource
transaction scopes alone cannot protect that enclosing job operation. Corrected old2.720 probe1/2
shows a callback hash reaches the unfinished job validator (`invalid physical haul itinerary`),
not an early clean operation refusal. **The hash did not escape** in that reproduction. An earlier
probe0/3 additionally had an inverted pre-delta stock oracle; that failure is retained, not an extra
engine defect. Other old-image checkpoint/reentry/release combinations were not executed before repair.

IHaulExecution now declares the existing ISimulationOperationBoundary contract. LogisticsService owns
one transient scope instance around original Acquire, Load, Unload, EndHaul and TryReplanDestination
coordination. Reentrant logistics mutation refuses before acquiring another scope or changing state.
Unexpected provider/commit failures latch at existing World first-fault phases logistics.acquire,
load, unload, release or replan; earlier provider/transaction causes retain precedence. Known normal
preview/capacity/worker refusals remain unchanged-state results, not manufactured failures.

Kernel Services now owns one internal reusable SimulationOperation nesting primitive. The original
ResourceTransactions' ten mutations use that same implementation on their own instance; its old
private depth/scope implementation is removed. Resource and logistics do not share live permission
state. No registry, stock/book, clock, persistence field, public native-inheritance requirement,
second coordinator or compatibility path. World already observes frozen operation-owner contracts;
no new World dispatch branch or permission-value cache was needed. Save161/replay2 unchanged.

Assignment calls existing WorkBoard.ValidateHaul before returning success, validating the actual
job/carrier/cargo/endpoint/receipt relation through the authoritative job validator. Cancellation
checks the original supplier's actual book for any retained claim of that job owner, then uses
existing whole surviving-claim release. Malformed returned claim and supplier release/recreation
probes both fault with truthful terminal prefixes rather than returning a valid resumable job.
They neither consume source stock nor roll back to fabricated prior state. Prices, currency,
accounting/settlement amounts and all Tycoon business rules are unchanged.

Eight native sources change: Kernel Services/SaveGame, Standard WorkContracts/Logistics/
LogisticsRecovery/ResourceTransactions/ResourceConversions/ResourceTransferBatches. All450 native
files remain present; no new native source or subsystem. Proof FiniteSupplier adds only per-instance
before/after claim/release hooks around its original inventory book. Those diagnostics are not saved
or authored gameplay. AtomicHaulTests reuses its sole PaidSupplyFixture; direct setup/provider mutation
is explicitly a protocol probe, not public player acceptance. Paid delivery is selected separately
from the existing hauling literal; the unrelated OptionalWorkTests.Physical call was already independently
registered and is removed from that aggregate. No duplicate workflow owner or family run is added.

### Actual focused results

[All attempts/images](../../artifacts/optional-module-durability/841e573d0b5849bc918717c687a6090c/attempts.json),
[compiled census](../../artifacts/optional-module-durability/841e573d0b5849bc918717c687a6090c/call-audit.log),
[caller dispositions](../../artifacts/optional-module-durability/841e573d0b5849bc918717c687a6090c/caller-dispositions.json),
[frozen inputs](../../artifacts/optional-module-durability/841e573d0b5849bc918717c687a6090c/final-inputs.json),
[structural checks](../../artifacts/optional-module-durability/841e573d0b5849bc918717c687a6090c/structural-checks.json),
[original gates](../../artifacts/optional-module-durability/841e573d0b5849bc918717c687a6090c/original-gates-current.json).

| Latest selected literal | Passed/failed | Suite/observer ms | PID/exit | Image |
| --- | --- | --- | --- | --- |
| Haul operation admission: compound supplier claims and cancellation cannot checkpoint or advance time | 1008/0 | 3864/4051 | 2124/0 | build6 |
| Atomic hauling: common quantities, all-participant refusal and exclusive carrier acquisition | 714/0 | 1309/1509 | 19116/0 | build6 |
| Atomic hauling: finite paid physical delivery and saved native settlement | 7/0 | 1734/1903 | 26412/0 | build6 |
| Atomic hauling: protected cargo, route interruption and saved physical workflows | 16/0 | 415/578 | 14300/0 | build6 |
| Spill cargo interruption: native and independent saved load returns through authored recovery | 856/0 | 8688/8862 | 15600/0 | build6; two actual public replays |
| Resource mutation admission: unfinished original commits cannot checkpoint or advance the sole clock | 2104/0 | 2194/2355 | 22164/0 | build5; shared primitive unchanged afterward |

Latest **4705/0/two actual public spill replays**, spanning captured build5/build6 native/proof/tests
images—not one final-image regression. The resource-admission case was not rerun after logistics-only
postcondition and test changes. Final five scopes2601/0 use build6. The80 new contexts cover acquisition/
release × before/after actual book mutation × new/restored sessions × handled/propagated exception ×
hash/save/direct World.Step/ordinary Update/reentrant Acquire. They check actual stock/promise prefix,
independent resource/logistics boundaries, a real nested resource refusal, no clock budget/publication,
exact once-only job/claim state, saved continuation, cash unchanged during claims and session isolation.
Two additional malformed-provider contexts add8 checks. This is not every-provider or full concurrency proof.

Six targeted builds: five clean16.40/5.51/7.52/7.27/7.48seconds, one failed7.83seconds because the
new test initially treated IWorkerControl.SetHeld's void result as Code. Thirteen literal runtimes:
two retained old-image failures; intermediate new800/0 and1000/0; earlier focused714/0,16/0,7/0;
then final results above. Formatting-patch composition initially failed atomically and changed no
files; it was corrected before building. A later review narrowed overbroad indentation back to the
actual method body; no branch/algorithm changed for formatting. All image captures retained, no
repeated unrelated green tests after final logistics-only changes. No full/profile/family/soak gate.

Final native DLL `E8FCF449BCFB573239DF9696306F399F28B6F25148407DDE6FE649ED8569A574`;
proof `49ED7DF0550D0E01537E5EA6D5ADE246705C38E2193A4AD479390E20427B36DB`;
tests `41BCBA7B4334ECDB86E37D618303C332F9988CAE27ACA80AB47C429AB62ECEFD`.
Build5's distinct native/proof/tests hashes are retained with its actual resource-admission result.

Build: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`.
Each runtime uses the tests DLL with `--bounded-exact` plus its literal and
`--timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`.
Read-only audit: `docs/migration/AuditResourceMutationCalls.ps1 -BuildInputs artifacts/optional-module-durability/841e573d0b5849bc918717c687a6090c/build-6-inputs.json`.
Matching native/PDB/source audit:450/450,2925types,12891methods,344237instructions,
111typed calls/14primitive-owned writes,0unknown/unmapped/indirect. Remaining direct purchase calls
now sit inside their original logistics boundary; they are not supplier-as-inlet adapters or balances.
That census remains typed structural evidence, not whole-session semantic accounting.

### Self-review and continuation

Local self-review, not independent review. Existing World first-fault, typed refusals, original flow
facts/private views and notifications remain the operational owners. [Wider operational/assurance
gaps](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage) remain
open. Foreign claims, current identities, no premature time/checkpoint publication, terminal prefix,
normal exact-quantity/worker refusal, saved physical timing and once-only existing payment checked
narrowly. No new logger, balance, event journal, package, platform or business policy.

Next continue the existing spatial sample/layer semantic accounting and typed reference-owner matrix,
then relevant bounded stage stress. P7.017/P7.019 and the other original open requirements remain
open; no current full-provider/full-regression certificate. No consumer/Oil/marine execution or edits,
Tycoon business-rule edits, platforms/Draw, staging/commit/push. Full goal remains active.

## Historical 2.601 fixture baseline — not current runtime certification

Date2026-10-15. Engine2.601.0/save159/replay1 unchanged. This stage repairs and
strengthens an existing regression/authoring fixture; no production engine repair
was needed. It advances the original industrial baseline and logistics verification.

## Original owners and authoring

IHaulExecution/LogisticsService still selects carriers and obtains all source,
destination and cargo commitments. IWorkerControl owns holds/releases. Original
WorkBoard, PointDock, motion, inventory/books, homes and clock execute all work.
The test owns no scheduler, timer, route, transfer or reservation implementation.

haul-competition.json is explicitly embedded and master-listed under the original
points module. Original machine/resource/time/space/capacity definitions are reused.
Two endpoint content variants add service/wait/departure geometry to the same
original inventory/dock providers. One parking prototype supplies distinct forward
homes. Startup and queue geometry are authored before loading; no runtime spawn or
direct Held write remains in Acquisition. Shared haul-quantum.json is unchanged.

The scenario permits two seats and the session explicitly enrolls two human seats.
Privacy now uses a real foreign seat, not a fabricated viewer. Save format uses the
independent current protocol expectation instead of obsolete literal15.

The no-dock fixture stalls legitimately under current physical occupancy: its first
delivered carrier parks at the exact destination and blocks the loaded follower.
The snapshot showed destination30/source0/follower cargo30, moving, no fault. No
timeout increase, overlap or teleport was introduced. Authored dock slots and
separate forward homes clear the lane without requiring an impossible pass in1D.

## Commands and all attempts

Each build: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`.
Every build completed exit0 before runtime. Only this literal ran:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Atomic hauling: common quantities, all-participant refusal and exclusive carrier acquisition" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 7500
```

The Physical case and its nested paid/Tycoon/optional-work cases were never run.
The internal physical limit remains200 original game updates throughout.

| Attempt | Actual result |
|---|---|
| Original unchanged regression | 18passed/1failed,395ms; PID31200 exit1/566ms. Invalid company1 in one-seat world; rest of suite not reached. |
| Authored scenario/controls/protocol build | Clean12.24s; session97189 terminal |
| New document not yet embedded | 0/1,65ms; PID29824 exit1/235ms. Missing proof.haul-competition.json. |
| Explicit resource registration build | Clean10.57s; session82356 terminal |
| Two-seat capacity, one requested human | 18/1,382ms; PID8628 exit1/561ms. Seat1 absent. |
| Explicit actual human seats build | Clean6.85s |
| Physical continuation reached | 37/1,796ms; PID20700 exit1/1049ms. No-dock occupancy stall at200, no fault; atomic checks passed. |
| Bounded diagnostic build | Clean7.56s |
| Diagnostic stall | 37/1,1172ms; PID13544 exit1/1533ms. First carrier delivered30/idle; follower loaded30/blocked. |
| Real docks/distinct initial positions build | Clean8.92s |
| Delivery-only first GREEN | 39/0,1150ms; PID17120 exit0/1317ms. Further strengthened because delivery alone did not prove both departures/home clearance. |
| Distinct homes/full continuation build | Clean7.46s |
| Stronger proof first GREEN | 587/0,1298ms; PID25508 exit0/1468ms |
| Resource-neutral failure diagnostic build | Clean6.11s |
| FINAL | **587/0**,1325ms; PID12872 exit0/1503ms |

Seven builds, all zero warnings/errors; every handle terminal. No external timeout,
output truncation, restart or executable change after FINAL. All early failures are
retained, not counted as passing baseline evidence.

## Proven scope

- Independent source/cargo/destination quanta3/2/5 give common30 by LCM. Below-
  quantum, overflow and unknown-resource refusal fabricate no quantity.
- Held carrier, repeated competing acquisition, insufficient quantum and exhausted
  source refusals preserve hashes, promises and job/receipt sequences; IDs1/2 remain
  contiguous. One carrier and three actual endpoint promises per complete job.
- Total two-job promises never exceed actual60 stock or destination room. Real
  foreign seat receives no private carrier selection. Retained preview is detached.
- Acquired jobs/carriers/receipts restore exactly. Every physical Update checks
  total stock60 across source/destination/both cargo owners, strict carrier position
  ordering (no overlap/crossing) and saved-twin hash equality.
- Both jobs deliver, leave the docks and reach distinct engine-owned homes within
 200 updates. All source/destination/cargo books empty, both workers idle, source0.
- Bounded diagnostics show stage/time, original faults, detached jobs and actual
  resource-neutral inventory vectors, without new live reads after terminal fault.

## Remaining

This is direct-owner contract coverage, not ordinary-command/replay proof. It does
not prove manufacture, every provider/geometry, mixed resources, road/fuel/breakdown
recovery, active interleaved sessions or stress/performance. Parking is explicit1D
authoring, not a universal traffic solution. The shared Until helper compiles for
historical Physical callers; those paid/nested cases were not executed.

Original26 recorded open gates/full goal remain open. The compound P7 tool/dock/
graph/inbound/physical-batch gate is not closed by one scenario. No full/profile/
family runtime, Oil/Tycoon/host/platform/renderer change, frozen-audit rewrite,
staging, commit or push.
