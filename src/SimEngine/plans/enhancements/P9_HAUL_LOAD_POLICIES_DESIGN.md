# P9-C6 — Authored carrier load admission and wait deadlines

Historical C6 stage. Follow-on [C6b editable settings](P9_EDITABLE_HAUL_LOAD_POLICIES_DESIGN.md)
replaces profile-only runtime state/commands with one current settings value; JSON now supplies Initial
and the selected rule receives actual runtime settings. No compatibility overload or reader. Existing
request/history/physical jobs remain; C6b includes these methods in its207-check/two-suite gate on
2.150.0/save78/load policy2/logistics view16. Earlier API descriptions below are historical.

Status: focused-verified, 2026-10-06. Parent: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Build2.107.0/save65/load policy1/logistics service7/logistics view12. WorkBoard22/work system6/
movement4/existing other owner layouts unchanged.122 checks/two exact suites, clean build.
[Evidence](../../docs/migration/P9_HAUL_LOAD_POLICIES_VERIFICATION.md). Full P9/framework remains partial.

## 0. Problem statement

LogisticsService.Preview currently sends any positive native amount; existing standing route/demand
records have original fairness age but no last successful dispatch time. A minimum-load rule using
that original age alone would permanently expire after the first trip. Extend those existing owners,
not a fleet scheduler, per-host timer or parallel request queue.

## 1. Goals

1. A carrier's optional contract selects an actual master-listed load profile with native resource
   minimums, maximum wait duration, enabled state and explicit emergency bypass.
2. Existing automatic matching chooses another ready carrier when a preferred carrier is waiting.
3. Positive smaller loads become eligible after the request's maximum wait; source/cargo/destination
   capacity, fuel, handling, roles, regions and routes remain mandatory. No fabricated minimum quantity.
4. Existing route/demand records preserve original priority age and separately retain last actual
   dispatch tick. Failed previews/acquisitions do not advance it; successful admission resets it.
5. Selection commands, private views, saves/hashes and native/public providers use one protocol.

## 2. Design

IHaulLoadProfile declares immutable content and a pure CheckLoad(HaulLoadContext) extension hook.
HaulLoadProfileDef is the registered content base; AuthoredHaulLoadProfileDef owns native explicit
enabled/minimum_load/maximum_wait/emergency_bypass parsing and the standard rule. Minimums are per-resource
alternatives, not a simultaneous cargo bill. Validate each against actual cargo filter/native quantum/
empty capacity. Maximum wait uses the selected session's rational duration converter (rounded up to
the session quantum through existing Time.Ticks), with no
platform limit. Zero wait explicitly admits positive partial loads immediately.

IHaulLoadPolicyDefinition supplies initial actual profile metadata for composition validation.
IHaulLoadPolicy selects a real profile on its actual transporter entity. Native haul_load_policy owns
one profile ID and its codec; no copied bands, enabled state, clocks or inventory. Normal command
selects another authored profile through shared check/commit. No invented default when a profile is
missing. Explicit absence of this optional capability imposes no automatic load gate.

LogisticsService.Preview applies the selected policy to each automatic candidate after actual amount
packing. Existing BuildQueue/TryAcquire re-evaluate the same context before native acquisition. Direct
explicit engine-side IHaulExecution hauls and physical return/evacuation are not automatic standing-load batching; no hidden
override applies to acquired jobs, which retain their original commitments through profile changes.

LastDispatchTick is initialized to CreatedTick and stays on existing route/demand rows. Surviving demand
updates preserve it. Successful standing admission advances the actual route or all matching active
destination/resource demand rows; no copied clock on workers/jobs. Request wait age is tick minus last
dispatch, independent of carrier identity/readiness and original fairness age. Time without supply or
while disabled counts toward the request deadline; it is not a ready-only loading timer. This semantic
is explicit, not a promise to wait a full interval after every new positive gap.

## 3. API surface

Public IHaulLoadProfile/IHaulLoadPolicyDefinition/IHaulLoadPolicy/HaulLoadContext, pure profile read/check/commit protocol,
ConfigureHaulLoadPolicyCommand and private LogisticsView read/preview. LogisticsDispatchOffer exposes
LastDispatchTick/WaitAgeTicks; existing demand/route views expose history. No renderer or platform work.

## 4. Files

New Standard/HaulLoadPolicies.cs and Commands/HaulLoadPolicyCommands.cs. Extend existing Logistics,
Scheduling, Module, View, build/save identity. Add public-only provider/profile proofs, master-listed
content and two exact suites. Update stage/master/parent/README/AGENTS/help and scoped evidence.

## 5. Errors

| Case | Result |
|---|---|
| Unknown profile/foreign company/non-transporter | Typed refusal before mutation |
| Invalid catalog/filter/quantum/capacity/time | Fail content or command admission |
| Below minimum before deadline | Typed policy wait; another ready candidate remains eligible |
| Deadline reached | Admit only actual positive packed amount |
| Invalid/mutating public read/check or no-op commit | Explicit provider fault |
| Invalid saved chronology/profile | Reject current save, never legacy conversion |

## 6. Compatibility

None. Revise current owner codecs and callers directly. No old reader, shim, policy fallback or grants.

## 7. Verification

Clean build and only these exact full suites (same-name repair reruns allowed):

- Haul load policies: authored contracts, deadlines and canonical admission
- Haul load policies: physical batches, dispatch history and saved continuation

Prior suites migrate/compile only; no third/family/full run. Every physical stage has a bounded timeout.
63 admission checks and59 physical checks pass, zero failures/aborts. Four public provider faults,
strict/null/native-capacity/range validation, staged profile decode, future history rejection, pending
commands, native/private facts, independent authored odd-load rule/standby selection and wide time pass.
Native/public policies execute actual two-batch loading/travel/unloading, profile changes with loaded
cargo, fresh per-dispatch deadlines, updated stock-demand history and interleaved saved twins. Entity.Update
settles the authored disabled native state; no new lifecycle, movement or host scheduler. Broader
emergency/fuel/dock/preemption/provider/ready-only timer matrices were not run.

## 8. Risks

Wait age is request-level history, not carrier-local stopwatch or ready-only duration. Shared demands
advance together after actual admission. A mod profile may define another pure rule through the same
contract. Scoped provider guards are not process sandboxing/global rollback. Deadline expiry cannot
fix absent supply, insufficient native capacity, fuel, roles or unreachable endpoints.

## 9. Remaining scope

Home depots/preferred routes/service intervals/named holds, travel fuel budgets, congestion, ready-only
wait episodes and wider provider/replay/lockstep/stress/full P9/P1–P12 acceptance remain open.

## 10. Order

- [x] C6.1 profile/policy contracts, strict master JSON and native owner
- [x] C6.2 sole dispatch history, matching/acquisition and private commands/views
- [x] C6.3 two exact gates, public extension and physical saved continuation
- [x] C6.4 scoped evidence and tracker/help; full framework remains partial
