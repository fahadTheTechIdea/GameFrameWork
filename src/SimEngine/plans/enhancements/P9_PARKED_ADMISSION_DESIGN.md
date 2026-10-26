# P9-PBH1.P2 — Arrived-slot admission before unrelated candidates

Subsequent [P3-S28.B](P3_UNIFORM_BYTE_HASH_DESIGN.md) preserves this original owner
and all dock validation while making exact hashing faster. On2.658 ONE canonical
day now passes the original bound; [current1451 native checkpoint and limits](../../docs/migration/P3_UNIFORM_BYTE_HASH_VERIFICATION.md).
This stage's2.653 failures/results below are retained historical evidence.

Status: implemented/scoped-verified on2.653.0/save160/replay1; canonical day
performance gate remains NON-GREEN. [All attempts/current native evidence](../../docs/migration/P9_PARKED_ADMISSION_VERIFICATION.md).
Parent P9_PARKING_ROUTE_SELECTION_DESIGN.md; original
P9.acceptance.051/full goal OPEN,221/209 recorded checked/12 open unchanged.

## 0. Problem and specification

Actual2.652 native minute1092/0c700607... loads normally. Public home reads of
already-parked workers take29.214/11.406/50.379/56.634/73.580ms; one update412ms.
Working workers take1.336–2.146ms and bypass parking. ReadHomeParking still
evaluates role/claims/vacancy/policy/cost of every earlier candidate before it
discovers an actual arrived later slot. Those candidates cannot beat arrival.

## 1. Goals

1. An actual eligible arrived slot checks only its own admission, not unrelated
   cost/vacancy/search callbacks. Never infer arrival from zero cost.
2. A role/claim/vacancy/area-refused arrived slot does not become eligible merely
   because the worker occupies it; ordinary ranked fallback remains authoritative.
3. Retain original stable-ID blocker precedence, provider guards, motion/home/
   resource/time state, saves/replay and native/independent geometry.
4. Prove callback counts and actual behavior, then measure canonical current
   state without raising original limits or decreasing layout capacity.

## 2. Design

Only original WorkBoard.ReadHomeParking changes. Take its existing stable slot
list, call required guarded IsAtSlot and admit actual arrived candidates first.
Move its existing role/claim/vacancy/policy code into ONE private helper reused
for arrived and non-arrived candidates. Locally remember arrived IDs/refusals
inside this single pure query so they are not re-admitted or route candidates.
If none is eligible, existing cost-ranked route evaluation handles the rest.
Original stable list indices, not pass order, select final typed blockers.
No persistent cache, job/parking owner, path table, native cast or second clock.

## 3. API

No new API or JSON. Existing required IWorkDock.IsAtSlot owns geometric arrival
for every actual carrier/slot, independent of service eligibility. Existing
contracts still reject invoked provider mutations. Engine build advances;
payload/save schema and component versions unchanged, no legacy reader.

## 4. Files

Standard/WorkHomeParking.cs; engine identity and current protocol fixture;
existing PointDockProof test counters/ProducerHomeBerthTests. Existing Oil
OffshoreTests only times public observations and calls normal Update/save.
Tracker/docs record all source/build/runtime identities and limits.

## 5. Errors

Role/claim/vacancy/policy failures of arrived slots retain original precedence.
Only irrelevant candidate callbacks are skipped, not guards around invoked
providers. No grant, teleport, lost cargo, guard/version/hash bypass or capacity
reduction. Faulted sessions remain non-resumable.

## 6. Compatibility

Development only, exact build native saves. Preserve older artifacts unchanged.
Fresh canonical acceptance must be earned after code changes; no header rewrite.

## 7. Verification

NEW exact count repro on original2.652, then same literal after correction.
Focused ranking/refusal/guard and actual physical native replenishment/saved
continuation checks only as affected; no full/profile/family runs. Fresh Gulf
prepare and bounded current one-day observation retain original limits. Actual
measurements are scoped, never proof of general CPU/soak/deadlock freedom.

## 8. Risks

First-pass refusal must not beat an earlier-ID second-pass refusal. Incompatible
arrival must not win. Pure geometry provider callbacks must handle actual slots
without assuming a service role. Local query results cannot escape into live
state or become stale across updates. Test counters are diagnostic, not state.

## 9. Out of scope

All map/hash/provider guard changes, host gameplay, JSON capacity/terrain/recipe/
stock edits, business/platform work, second subsystem or publication.

## 10. Order

1. [x] Measure actual retained1092 state and public home query costs.
2. [x] Add failing generic callback-count proof and retain baseline.
3. [x] Original-owner two-pass admission, unchanged callback guard boundaries.
4. [ ] Focused physical/durable/fault/native measurements, record every outcome.
5. [x] Update authoritative tracker and retain all unproved original gates.

Step4 has six latest focused passing selections222/0 but one canonical day20/1
wall-time failure. Current real native checkpoint is consumer
artifacts/marine/f3a757ddee54466aaf0c89f21c4abd7b/checkpoint.json,1152/hashABBD700CFC27F993.
First-arrival admission is verified; whole-day throughput is not. A live stack
now samples WorkSystem.RunJob -> ReplanJobDocks -> ReplanDockBindings -> joint
SelectRemainingJourney/SelectDock/ClaimBlocker. Audit actual retained-journey
validation, route/fuel/claim/policy/recovery owners before choosing the next
generic correction; no blind days, new cache owner or increased limits.
