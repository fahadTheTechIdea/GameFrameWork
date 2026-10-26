# P9-B5 — Contract-owned worker execution readiness and rates

Status: historically focused verified, 2026-10-06; not rerun in B6. Build2.103.0/save64/native execution codec1/work view32/
work system3/movement system2; WorkBoard22 and existing owner payloads unchanged. **134 checks/two
exact suites** (75 contracts;59 physical), zero failures/aborted suites, clean build.
[Evidence](../../docs/migration/P9_WORKER_EXECUTION_VERIFICATION.md). P9/P1–P12 remain partial.

## 0. Problem

Before this slice, WorkSystem exposed CanWork/WorkRate mutable Func callbacks. WorkRetryProofModule
installed them with implicit enabled/rate fallbacks. That was not authored contract-based composition.
Replace these paths directly before adding physical fleet fuel/service policies. No compatibility API.

## 1. Goals

1. Remove both mutable callbacks and migrate all source callers.
2. Named IWorkerExecution providers return one immutable gate/multiplier observation.
3. Reuse ExecutionGateContracts/SignalGateCondition, ordinary operation commands and native work owners.
4. Author native operation controls, typed conditions and disabled state through explicit master JSON.
5. Prove private/pure views, guarded providers, saved controls and physical work/haul gating in two suites.

## 2. Design

IWorkerExecutionDefinition declares named metadata. IWorkerExecution.ReadExecution(World) returns
WorkerExecutionSnapshot containing an ExecutionGateSnapshot and nonnegative dimensionless multiplier.
WorkerExecutionContracts reads providers in stable identity order through the same gate protocol,
validates actual metadata/multiplier/purity and derives effective work rate from actual IWorker.WorkRate.
No providers means no additional constraint/mathematical multiplier identity, not a fabricated machine.
Non-applicable providers must contribute identity. Representational multiplication overflow faults
explicitly; combined-rate overflow is not currently a typed ordinary control-preview refusal.

Native worker_execution is itself an IControllableOperation: it owns enabled/multiplier once in Sync,
using existing OperationSettings and ordinary controls, never an enabled or rate mirror. JSON requires
operation controls (unit.work-rate), conditions[] and states.disabled. Typed gate conditions read
actual existing fuel/condition/control signals; they do not grant fuel/repair or implement consumption.
Entity.Update alone commits state. Native/public providers use the same coordinator without branches.
Readiness gates actual movement without changing the motion owner's coordinates/path/goal. Multiplier
scales work/handling, not travel speed: zero work rate is not an invented fuel or locomotion rule.

WorkSystem alone retains selection/acquisition/jobs/progress/cancellation/retry. Candidate admission,
actual job continuation, handling and home return inspect execution readiness. Hauls retain current
physical retry/receipts/cargo; ordinary work retains its existing reopen semantics. Zero rate earns no
work/resources. Remove WorkRetryProofModule callbacks and its redundant retry operation registration;
its JSON controls become native worker_execution. Prior tests migrate to authored controls or explicit
public hostile-provider content rather than engine callback injection.

## 3. API / files

New Standard/WorkerExecution.cs; update Work.cs/WorkScheduling.cs/WorkModule.cs, shared gate purity,
WorkView, source callers/fixtures and public proof. No host/platform/renderer implementation.

## 4. Errors / durability

Malformed/missing metadata, unknown named signal, negative/unrepresentable rate or pure-read mutation
faults/refuses explicitly. Disabled/unready workers use existing work.worker-unavailable classification;
private diagnostics retain original gate blockers. Native controls stage decode; no fallback/old reader.
Every saved/hash checkpoint validates actual execution providers; current gate facts are derived only.

## 5. Verification

Build; exactly two named suites:

- Worker execution: authored contracts, controls and immutable admission
- Worker execution: physical work, cargo waits and saved continuation

Prior suites compile/migrate only. No third/family/full run, including phase completion.

## 6. Risks / remaining scope

Readiness is not fuel consumption, motion-cost prediction, fleet minimum load/maximum wait/home/route
preferences/reserves/service intervals, preparedness or full acceptance. All future work uses these
contracts and current physical owners. Existing base WorkerDef authoring/rate defaults remain a separate
explicit-authoring audit item; no new defaults are introduced here. Capture purity is scoped, not global
transactional rollback. Full P9/P1–P12 remain partial.
MovementSystem.SpeedModifier remains a separate callback-to-authored-contract migration prerequisite.
The native scalar unit.work-rate convention is not a catalog-unit rate conversion proof. Condition
tests use the existing native unit.condition requirement; arbitrary native unit aliases are not claimed.

## 7. Sequence

- [x] B5.1 contracts/native controls/shared gates and dispatch integration
- [x] B5.2 migrate callbacks/content/providers without a compatibility path
- [x] B5.3 private facts, two focused gates and checkpoint guards
- [x] B5.4 current evidence/master/parent/README/AGENTS/help

## 8. Scoped results

Tests prove native authoring/control/private immutable facts, deterministic named composition, five
provider faults, overflow, staged malformed/truncated decode, pending-command saves and real condition
signal readiness. The public proof compiles independently without friend access. Physical tests cover
timed ordinary work, zero/rate-two/disabled waits, retained progress, partial pickup, loaded travel,
unloading, native reservations/conservation and saved/interleaved continuation. Unassigned movement
retains its actual goal through disabled waits. All waits are bounded to160 ticks with job diagnostics.
Condition damage/repair is submitted through existing public engine contracts as fixture input; this
slice does not claim a new physical repair scenario, fuel burn or service dispatch.
37 previous identity assertion files and callback caller migrations compile only; no previous/third/
family/full gate ran. Source callback API and redundant retry operation registration were deleted,
not retained as legacy facades. Hosts, Oil/Tycoon, demos, platform SDKs and Draw remain untouched.
