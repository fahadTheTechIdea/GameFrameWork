# P9-C3e — Shared recovery and cargo-return arbitration

Status: historical focused implementation verified, 2026-10-07, before C3f.
Current source/evidence: [C3f guarded cargo binding](P9_CARGO_BINDING_OBSERVATION_DESIGN.md).
The full framework plan remains open; the C3e manifest below records the earlier source only.
[Current evidence](../../docs/migration/P9_CARGO_RETURN_DISPATCH_VERIFICATION.md) and
[current source manifest](../../docs/migration/P9_CARGO_RETURN_DISPATCH_SOURCE_MANIFEST.json).

## 0. Problem and outcome

The former IdleBehaviour posted residual cargo after shared scheduling, bypassing competition
with ordinary work, demands, routes and evacuation. That path, API and constant are deleted.
Both recovery kinds now compete in the existing WorkSystem assignment pass with one explicit
authored policy and one LogisticsService chronology. No replacement dispatcher or stock store.

The earlier handoff's CS1579 failure was repaired using actual Bill.Lines. Current content,
public proof, callers and direct build/save/schema migration compile. Historical C3d reports
remain historical; they are not current source-hash evidence.

## 1. Goals and focused completion

The completed slice provides explicit both-kind policy, shared deterministic ranking, actual
returning-worker selection, protected operating/committed stock, original age, private pure
views, atomic acquisition, physical timed delivery and saved continuation. Native and independent
public-only authored providers use the same scheduler. Broader acceptance is not claimed.

## 2. Engine design

### 2.1 Contract and master-listed content

ILogisticsRecoveryPolicy.Read(LogisticsRecoveryKind) returns immutable LogisticsRecoverySettings:
Enabled, Priority and AttemptsPerPass. LogisticsRecoveryProfileDef uses the existing Def catalog.
The native definition requires both strict settings objects:

```json
{
  "logistics_recovery_profiles": [{
    "id": "recovery.standard",
    "evacuation": {"enabled": true, "priority": 100, "attempts_per_pass": 2},
    "cargo_return": {"enabled": true, "priority": 60, "attempts_per_pass": 1}
  }]
}
```

Numbers above are examples, never engine defaults. Explicit module sim.logistics-recovery1.0.0
requires sim.logistics1.0.0. The master lists the JSON document; scenario module settings select
the actual profile. Absent capability means no automatic recovery, not fallback behavior.
Disabled kinds report typed private blockers. Missing fields, unknown keys and invalid settings
fail explicitly. The public-only proof implements the same profile contract and strict settings
reader without internal access, a scheduler edit or duplicated policy store.

### 2.2 Sole recovery chronology and real stock owners

Standard/LogisticsRecovery.cs is a partial of existing LogisticsService. Its single RecoveryIntent
collection stores kind, actual origin, resource, company, creation and last successful dispatch.
No quantity, destination, carrier assignment, receipt or inventory is copied into intent.

Evacuation origin is the actual inventory entity; return origin is the actual worker. A provider
may expose that worker's cargo on another actual inventory entity: identities are validated,
not conflated. ReadCargoDisposition remains the authoritative stock classification.

Existing PrepareDispatch observes current unassigned cargo and evacuation stock. Existing return
intent remains active while actual unassigned or committed cargo exists, preserving original age.
Retained operating stock alone neither creates nor retains return demand. Reservations belong to
actual inventory endpoints; another owner's promises are never released by recovery.

SyncRecoveries is embedded once in LogisticsService.Sync. Pure views never create intent, prune
state or invent age. Before preparation observes a new intent, enabled return previews report
current dispatch unavailability; selected absence/disabled state is explicit.

### 2.3 Shared acquisition and physical execution

LogisticsDispatchKind includes Demand, Route, Evacuation and CargoReturn. RecoveryOrigin replaces
the evacuation-only property. Kind-specific stable entry IDs separate both recovery kinds from
ordinary routes/demands. CopyOffers, CopyCandidates and TryAcquire use the existing BuildQueue.

Actual return acquisition must select the returning worker, not another compatible worker.
Existing Acquire creates actual cargo-out/destination-in claims and a Loaded job, with no invented
pickup claim or load phase. Existing movement, timed unloading and departure execute over the
same GameClock. RecordRecovery changes only the admitted kind/origin/resource chronology.

PostReturnCargo is removed from IHaulExecution and LogisticsService; IdleBehaviour no longer posts
cargo jobs. Existing idle/home rules remain their current owner and are not replaced in this slice.
Cancellation/interruption releases only the actual job's claims. Delivered prefixes remain delivered,
residual cargo remains real and later shared admission preserves active intent age.

### 2.4 Sole clock and lifecycle

GameClock owns current time; World constructs it once and SimGame reuses it. Creation/last-dispatch
coordinates are historical facts from that same clock, not new clocks. StepIndex/Tick are read-only
coordinates. Actual engine Game.Update executes the lifecycle and physical work.
No clock source edit, rendering phase, platform timing or host gameplay was introduced.

## 3. API and responsibility changes

New: LogisticsRecoveryKind, LogisticsRecoverySettings, ILogisticsRecoveryPolicy,
LogisticsRecoveryProfileDef, AuthoredLogisticsRecoveryProfileDef and LogisticsRecoveryPolicyModule.
Existing immutable logistics views project RecoveryOrigin and current offers, not executable handles.
IHaulExecution retains PreviewReturnCargo and physical haul protocol but no direct return posting.
One responsibility owner each: LogisticsService matching/chronology, WorkSystem assignment,
WorkBoard jobs, actual ports reservations, existing motion handling and GameClock time.

## 4. Source and authored content inventory

Runtime changes: Standard/LogisticsRecoveryPolicies.cs and LogisticsRecovery.cs; existing
Logistics.cs, LogisticsScheduling.cs, Work.cs, WorkContracts.cs, Tuning.cs, LogisticsModule.cs
and Kernel/SaveGame.cs. EvacuationPolicies.cs and LogisticsEvacuations.cs are deleted.

Public proof: tests/ExtensionProof/LogisticsRecoveryPolicyProof.cs, explicit embedded
content/logistics-recovery.json and logistics-recovery-proof.json, ExtensionProof.csproj.
Old evacuation-only proof/content files are deleted.

Affected two-suite proof: tests/Beep.SimEngine.Tests/RecoveryDispatchTests.cs and Program.cs;
old EvacuationDispatchTests.cs is deleted. Other direct callers in CargoRecoveryTests,
ResourceDispositionTests, TimedHandlingTests, HaulTripPolicyTests and ItineraryDurationTests
migrate to existing preparation/shared acquisition and explicit content. They compile but their
separate suites were not executed. Exact current build/save/schema expectations also migrated.
Runtime paths above are relative to src/Beep.SimEngine.

## 5. Refusals and state safety

| Condition | Engine behavior |
|-----------|-----------------|
| Absent capability | No automatic recovery; no default profile |
| Invalid selected content | Explicit composition/content refusal |
| Disabled kind | Typed private blocker; no job or promises |
| Full/removed/foreign/incompatible endpoint | Current refusal; actual cargo retained |
| Wrong returning worker, busy or stale eligibility | Fresh refusal; no substitute transporter |
| Retained operating stock/other owner's promises | Protected; not fresh return quantity |
| Partial unload interrupted | Delivered prefix preserved; own claims released; residual re-admitted normally |
| Invalid kind/duplicate intent/future time/profile mismatch | Direct current save refusal |
| Build failure | Never run stale no-build binaries as verification |

## 6. Direct version migration

Current identity: build2.198.0/save106/WorkBoard36/WorkSystem36/LogisticsService16/
logistics maintenance2/work view69/logistics view27. Native recurring8 and public recurring5,5,6
unchanged. Existing resource/job payloads unchanged. Recovery rows now include a kind byte and
selected current profile identity in the same embedded codec. No old reader, facade or shim.

## 7. Actual focused verification

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Recovery dispatch: authored policy, private chronology and shared ranking"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Recovery dispatch: physical conservation, cleanup and saved contention"
```

Clean build, zero warnings/errors. Two exact suites:85 admission +95 physical =180 assertions,
zero final failures/aborts. Same two suites were rerun during development; no third/family/full gate.

Admission covers strict both-kind JSON/native/public policy, absent/disabled capability, private
pure views, pending observation, original saved age, current priorities/attempt budgets, wrong
worker refusal, stale/no duplicate acquisition, protected stock bands and full-output saved waits.

Physical covers ordinary-work priority contention, actual return claims, real movement/unload/exit,
retained stock, simultaneous evacuation/return, other providers' cargo promises, partial unloading
interruption/re-admission, disabled eligibility, pause and saved twins. Previous evacuation
regressions remain inside these same suites. Workflows are bounded with diagnostics.
Explicit foreign-claim and interruption calls are protocol instrumentation, not test-owned gameplay.

## 8. Risks and remaining acceptance

Wider mixed-resource fleets, alternate cargo-inventory providers, home/service interaction,
emergency/route interplay, congestion, endpoint-removal combinations, global provider purity,
replay/performance/allocation/soak and full acceptance remain unverified here. Migrated sibling
test callers compile only. Focused saved twins are not a full replay gate.
Do not promote these180 assertions to full framework completion.

## 9. Out of scope

OilFieldDays/Tycoon, Unity/Godot, Draw/adapters, demos/harness gameplay, new transport media,
multiple clocks, parallel stores/dispatchers and compatibility loading. No platform size limits.

## 10. Task and continuation tracker

- [x] Remove idle return bypass/API/constant directly.
- [x] Extend sole policy/chronology and shared ranking for both recovery kinds.
- [x] Migrate strict explicit JSON and public-only content proof.
- [x] Migrate actual API/content callers and direct current metadata.
- [x] Verify protected stock, foreign claims, private chronology and current acquisition.
- [x] Verify timed delivery, interruption, mixed-kind contention and saved continuation.
- [x] Clean build and only the two exact affected suites,180 assertions.
- [x] Record current evidence/source hashes and reconcile master/parent/help.
- [ ] Broader provider/mixed-resource/home/fleet interactions and consumer acceptance.
- [ ] Congestion/global purity/replay/performance/full framework acceptance.

## 11. Resume instruction

Later focused2.217 checkpoint extends this SAME recovery owner with required JSON destination
policies and public-only detached decisions. Native demand/no-demand and partial paid-cargo physical
completion now have [scoped evidence](../../docs/migration/P9_RECOVERY_DESTINATION_POLICY_VERIFICATION.md)
and [design](P9_RECOVERY_DESTINATION_POLICY_DESIGN.md). Historical2.198 tests here were not rerun.
Wider evacuation/reroute/endpoint/fleet/provider/replay/acceptance remains open.

Use C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine, not OilFieldDays cwd.
Preserve the dirty tree. Read AGENTS.md and the master tracker. C3e's focused migration is complete;
continue the remaining explicit P9/provider/fleet work instead of restarting or creating a second
recovery scheduler, policy store or clock. Full-plan acceptance remains open.
