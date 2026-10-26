# P9-B11.3c2b1 — Authored service-first machine dispatch

Status: B11.3c2b1 focused verified, 2026-10-07. Wider B11.3c2b and the roadmap remain open.
Build2.193.0/save104/board36/work35/work view68/logistics view22/native recurring8.
[Verification](../../docs/migration/P9_SERVICE_DISPATCH_VERIFICATION.md).

## 0. Problem

An admitted self-service plan can wait for spares while its selected machine accepts unrelated
ordinary work. Priority alone is not an authored machine reservation policy. Fleet service must
share the existing board and dispatch contracts, never create a second reservation ledger.

## 1. Goals

1. Every source explicitly selects dispatch_mode: compete or service_first in master JSON.
2. Service-first requires real worker-bound nodes on a single actual source-owned activity.
3. Existing board provenance and live plans alone reserve that machine against fresh ordinary work.
4. Its own authentic service work, emergency work and actual incumbent continuation remain allowed
   through existing physical/safety/policy gates; matching manual work cannot impersonate service.
5. Pause/cancel/completion release eligibility; disabling future recurrence does not cancel admitted work.
6. Private previews, physical dispatch and saved continuation use one rule and unchanged GameClock.

## 2. Design

Add RecurringWorkerDispatchMode to IRecurringPlanDefinition, parsed explicitly by native/public
readers. RecurringPlanView exposes the authored mode. WorkBoard factors its existing source/order/
selected-worker provenance check so dispatch reservation and reserve-use permission share it.
CheckRecurringDispatch reads actual source LastPlan and the existing admitted cycle members, never
calls recursive plan admission or stores another reservation. A live unpaused service_first cycle
blocks fresh ordinary assignments through IWorkerDispatch idle and full candidate paths. Own
authentic recurring nodes are exempt; manual clones are not. Existing continuation validation
protects loaded jobs; normal emergency policy remains authoritative.

## 3. API

RecurringWorkerDispatchMode; IRecurringPlanDefinition.WorkerDispatch; RecurringPlanView.WorkerDispatch;
typed work.recurring-service-pending refusal through existing dispatch/candidate views. No new command,
scheduler, fleet store, worker flag, reservation, deadline or Sync payload.

## 4. Files

RecurringPlans/WorkRecurringPlans/WorkSystem and view semantic identities; public source definitions;
explicit JSON/inline fixture migration; focused CompletionServiceTests; stage evidence and tracker.

## 5. Errors

Missing/unknown mode fails composition. service_first without real selected worker nodes or on
template/nonworker sources fails before gameplay. Invalid source history/provenance fails through
existing checks. Physical supplies, worker controls, areas, emergency eligibility and safety stay enforced.

## 6. Compatibility

Direct JSON/API/build migration, existing owner payloads unchanged. No default, legacy facade or fallback.

## 7. Verification

Build and only the two exact completion-service suites. Verify idle/candidate/private purity, manual
clone refusal, own service eligibility, pause/resume/cancel, actual loaded continuation, shortage/refill/
service completion and saved twins. Other suites compile only; no full/family/third gate.

## 8. Risks

This policy covers admitted source-owned service, not speculative future demand. Deliberately paused
plans release fresh work eligibility; source disable only controls future admission. Stranded propulsion,
multi-source/queue/provider stress and broader trip/fleet/replay/performance acceptance remain open.

## 9. Out of scope

Oil/Tycoon/hosts/demos/platforms/Draw; teleportation, forced job interruption or independent time owners.

## 10. Stages

- [x] Explicit dispatch mode, canonical board provenance and shared fresh-assignment gate.
- [x] Focused real machine/logistics/service/private/save proof:408 assertions in two exact suites
  (188 admission;220 physical), zero failures/aborts. Other suites compile only.
- [x] Current build, scoped evidence, tracker and fingerprints. One unchanged GameClock;
  existing owner payloads unchanged, no legacy reader or new reservation store.
- [ ] Wider B11.3c2b/B11.4 fleet/trip/queue/provider/performance/replay acceptance.
