# P12-D13 / P4-G2: feasible distinct worker teams

Current source-focused checkpoint: P4-G2g complete productive commitments,2026-10-08.
Original WorkBoard now resolves actual productive refs and commits whole running-input/startup/
optional-budget sets before binding any node progress. Timed Working/Paused frontiers share this
owner; scarce inputs retain canonical sequential progress. Native productive machines use the same
owner with actual job/phase/worker checks. Per-node causes/history and original saved state remain.
Build2.287.0/save141/replay1/WorkView82. ONE clean build13.77s0/0; SAME ONE new bounded case
FIRST attempt106 passed0 failed/aborted2145ms. No observed build/runtime repairs;153 old identity
paths compile-only. Two new explicit JSON documents; source/proof/tests/JSON frozen after green.
Cross-order/mixed machine-timer productive scheduling, external distributed obligations/global purity/
stress/Oil/authorized P6/full P1-P12 remain OPEN. Full goal ACTIVE; no Git publication.
Every preceding checkpoint below is historical and NOT rerun on2.287.0.

[Productive commitments](P4_PRODUCTIVE_FRONTIER_COMMIT_DESIGN.md), [evidence](../../docs/migration/P4_PRODUCTIVE_FRONTIER_COMMIT_VERIFICATION.md).

Historical checkpoint: P4-G2f unified native activity admission,2026-10-08; not rerun on2.287.0.
Timed and staffed intents now share original WorkSystem ranking/matching and final admission.
WorkBoard owns one fresh mixed native resource/optional-budget set; existing jobs/nodes own state.
The early timed-start path and transient started-ID guard are removed. No new clock/store/scheduler.
Build2.286.0/save141/replay1/WorkView81. THREE clean builds16.58s/8.49s/5.97s,0 warnings/errors.
THREE attempts of SAME ONE new bounded case:107/2 1836ms,108/1 1980ms, final109/0/0 1672ms.
Both observed assertions confused posted machine-node state with real worker acquisition.
An unnecessary additional matching fallback was tried then removed after reading the original matcher;
final engine source needs no runtime repair.152 identities and older explicit ctor migrations compile-only.
Two new explicit JSON documents; source/proof/tests/JSON frozen after final green. Prior checkpoints
are historical, not rerun on2.286.0. Paused-frontier aggregation/external joint obligations/global purity/
stress/Oil/authorized P6/full P1-P12 remain OPEN. Full goal ACTIVE; no Git publication until full completion.

[Unified admission](P4_UNIFIED_NATIVE_ADMISSION_DESIGN.md), [evidence](../../docs/migration/P4_UNIFIED_NATIVE_ADMISSION_VERIFICATION.md).

Historical checkpoint: P4-G2e complete timed frontier acquisition,2026-10-08; not rerun on2.286.0.
Original WorkBoard now admits fresh complete actual positive-time workerless node sets through
CheckWorkSet and the same resource/optional-budget acquisition. Existing graph/node state only.
AdvanceOrder starts feasible timed sets together without extra same-Update work; scarce held tools
retain canonical sequential physical execution. No second scheduler/ledger/clock or business rule.
Build2.285.0/save141/replay1. FIVE clean builds13.80s/6.90s/4.06s/4.30s/3.67s0/0.
FIVE attempts of SAME ONE new bounded case:2/1 abort60ms (resource registration),35/1 abort360ms
(driver key),53/1 abort346ms (authored executor),80/3 failures1731ms (absent account), final83/0/0
1342ms. Fixture/driver repairs only, no runtime engine repair.151 identities compile-only.
Two new explicit master-listed JSON documents; existing data and sole GameClock unchanged.
Source/proof/tests/JSON frozen. No prior/family/full/platform/stale runtime;2000 Updates per physical
phase are failure bounds, not watchdog/CPU/soak proof. Combined staffed/timed/paused frontiers,
external joint obligations/global purity/stress/Oil/authorized P6/P1-P12 remain OPEN.
Full goal ACTIVE; no Git publication until FULL goal AND plan finished and verified.
Every preceding checkpoint below is historical and NOT rerun on2.285.0.

[Stage](P4_WORKERLESS_FRONTIER_ACQUISITION_DESIGN.md), [evidence](../../docs/migration/P4_WORKERLESS_FRONTIER_ACQUISITION_VERIFICATION.md).


Historical preceding checkpoint: P4-G2d / P12-D14 native team acquisition,2026-10-08; not rerun on2.285.0.
Original WorkBoard now admits a fresh complete same-company native job/machine/role/source/dock set;
one original resource transaction and optional authored account reserve complete commitments before
binding existing jobs/workers/nodes/service slots. Single and workerless work share resource acquisition;
BindCosts only binds actual receipts. Feasible native dispatch teams acquire together and rebuild.
Emergency teams use original interruption and fresh whole-set revalidation. No second ledger/scheduler/
state codec/clock or business policy. Nonfeasible teams retain original physical sequential fallback.
Build2.284.0/save141/replay1; sole GameClock and authored JSON unchanged. FIVE builds:first2.54s
zero warnings/two CS0023 on struct null operators; clean5.69s/6.69s/3.71s/3.33s thereafter.
THREE attempts of SAME ONE exact NEW bounded case:53 passed1 failed/aborted638ms (driver read
retired live order);84 passed2 failed/one abort739ms (fixture reserve blocked ordinary actors and
driver confused another active order); corrected existing completion-history/response views and
explicit public reserve policy, final97 passed0 failed/aborted1623ms. No engine repair from runtime.
Refusal purity/no prefix, duplicate/missing jobs/actors/bays, aggregate material shortage, reversed
arrival/save hashes, real parallel timed work/materials/pause/saved twins/replay, optional authored
budget holds/payment, second real acquisition fault/session isolation, emergency team preemption and
public loaded hauling/staged construction/50-30=20 steel proved.150 identities (EngineInfo+149 older
fixtures) compile-only; no old/family/full/platform/stale runtime. Source/proof/tests/JSON frozen.
2000 Updates per physical phase with failure snapshot, not watchdog/CPU/allocation/soak proof.
Earlier per-job/atomic-native-team limitations are superseded ONLY for this native contract path.
External-source/native-haul joint obligations, aggregate workerless frontier acquisition, global
provider purity, broader interruption/fault/stress/Oil/authorized regression/full P1-P12 remain OPEN.
Full goal ACTIVE; commit/push only after FULL goal AND plan finished and verified. No Git publication.
All preceding checkpoints below are historical and NOT rerun on2.284.0.

[Stage](P4_NATIVE_TEAM_ACQUISITION_DESIGN.md), [evidence](../../docs/migration/P4_NATIVE_TEAM_ACQUISITION_VERIFICATION.md).


## 0. Problem

`Standard/DistinctWorkerMatching.cs` maximizes priority-ordered distinct coverage, but accepts the
first assignment without testing its combined native obligations. Preparedness already checks
joint materials, productive docks and optional costs; dispatch does not reconsider an incompatible
combination when other actual eligible machines can satisfy those same obligations.

## 1. Goals

- One matcher preserves the attained worker count of every priority-ordered demand while searching
  actual compatible alternatives. No dropped higher-priority work to manufacture readiness.
- Preparedness and native dispatch use the same WorkBoard joint admission owners.
- Successful reads remain private, detached and unreserved. At the historical D13 checkpoint actual
  jobs acquired individually; D14 above supersedes this with complete feasible native team acquisition.
  Existing jobs still move, consume and finish over the sole game clock.
- No feasible simultaneous set means ordinary deterministic physical sequencing remains available;
  it does not turn a shortage into a ready preview or fabricate an assignment.

## 2. Design

Extend `DistinctWorkerMatching.Solve` with an optional pure complete-set predicate. The original
augmenting-path result is retained when feasible. Otherwise iterative backtracking enumerates
distinct identities over actual attained capacity (not requested capacity), canonical edge order,
and no redundant permutations inside a demand. No recursion, combination cap or saved search state.
If all alternatives refuse, return the original result so callers expose actual blockers and jobs
can still queue physically. Predicates evaluate complete sets, not misleading incomplete frontiers.

WorkBoard owns one `CheckWorkSet` composition of existing CheckDockSet, CheckResourceSet and
CheckCostSet. CapturePreparedness projects the actual selected candidate/role/choices through
that evaluator. WorkSystem groups selected native activity jobs by actual company and invokes it
before actual acquisition. External dispatch candidates still participate in distinct matching,
but their unrepresented resource/dock obligations remain their own acquisition responsibility.
Fixed incumbents remain fixed. Existing role/area/interruption filters remain authoritative.

## 3. API

No new public gameplay authority, command, JSON schema or saved owner. Existing immutable
preparedness fields now describe the feasible selected team when one exists. Build identity changes;
save payload does not. No compatibility path.

## 4. Files

Modify the existing matcher, WorkStart, WorkIncidentPreparedness, WorkScheduling and Assign.
Add one exact bounded engine proof using existing master-loaded point/workflow/dock JSON.
Update current build assertions as compile-only migration, registration and tracker/evidence.

## 5. Errors

Invalid matching identities/alignment still refuse. Provider faults are not caught/substituted.
Joint refusal preserves typed existing blockers. Missing workers leave combined facts unevaluated.

## 6. Compatibility

Development-only replacement; no legacy reader or shim.

## 7. Verification

One completed-slice build, one distinct NEW bounded case:
`Response frontier teams: feasible alternatives, dispatch parity and saved physical work`.
Repair/rerun only observed failures. No old/family/full/platform suite. Freeze source/tests/JSON
after final green, then docs-only. Record every attempt and scoped evidence.

## 8. Risks

Complete-set search is combinatorial in the worst case; broader CPU/stress gates remain open.
It is current native feasibility, not atomic team acquisition, global recipe optimization,
external-provider joint admission or a future departure/funding guarantee.

## 9. Out of scope

Oil, hosts, demos, rendering/platforms, Tycoon source/policy, extra clocks, ledgers or provider models.

## 10. Execution

1. Extend the same matcher and joint WorkBoard evaluator.
2. Wire existing preparedness and dispatch, preserving all actual acquisition owners.
3. Author/register one focused case; build/run and record observed repairs.
4. Freeze source and update stage, master and scoped evidence without claiming P1-P12 complete.

## Current scoped outcome

Implemented in the existing owners on build2.253.0/save139; persistent payloads unchanged.
THREE clean builds6.20s/3.92s/3.72s. THREE runs of ONE exact new case: initial49 passed1 failed622ms;
diagnostic49 passed1 failed719ms; final53 passed0 failures/aborts517ms. The observed fixture issue
was a completed un-homed worker genuinely occupying the sole permitted departure; proof now waits
for that blocker and uses ordinary player SetHomeCommand with physical engine movement.
119 older identity files/lines compile-only. No older/family/full/platform/stale gate.
Source/tests/JSON frozen after green. Docs-only afterward. No publication/full completion claim.
See [evidence](../../docs/migration/P12_FRONTIER_TEAM_FEASIBILITY_VERIFICATION.md).
