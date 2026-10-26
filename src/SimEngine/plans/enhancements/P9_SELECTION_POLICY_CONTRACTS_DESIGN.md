# P9-B14: Direct fleet and facility policy contracts — Design Document

## 0. Problem Statement

FleetPolicyDef and ActivityFacilityPolicyDef remain mandatory abstract authoring bases despite existing IFleetPolicy/IActivityFacilityPolicy contracts and sole JSON declarations. Fleet callbacks use finally verification that can mask original errors and observe only scalar member facts; facility Node metadata is not consistently observed.

## 1. Goals

1. Delete both obsolete bases; native and existing external definitions implement contracts directly on Def.
2. Retain sole standard JSON declarations, fleet membership owner and WorkBoard physical admission/execution.
3. Observe actual frozen catalog aliases, selected whole entity codecs, clock/map facts and fleet state before publishing successful decisions; preserve first typed errors.
4. Prove native/direct physical service/hauling, saved continuation, replay, private views and targeted invalid callbacks through ONE new bounded exact case.

## 2. Design

MachineFleetService.Pure uses EntityObservation.ReadProvider and actual member.Sync plus existing fleet.Sync. Only reentry flag cleanup occurs in finally. Navigation metadata is observed inside Pure. ActivityFacilityPolicyContracts owns selected observed reads. Recurring compatibility captures Node once outside ordinary incompatibility handling; runtime reads include actual worker/target codecs.

Existing master-listed selection fixture gains native/direct section selection and reusable internal player helpers. Existing proof content delegates sole declarations. Fault observers are deliberately invalid callbacks, not normal gameplay in drivers. No new membership, stock, work, clock or preference state.

## 3. API surface

Breaking deletion of FleetPolicyDef/ActivityFacilityPolicyDef without shims. Existing contracts, commands and immutable views remain. Selected worker context is internal. No Draw/platform API.

## 4. Files to change

| Action | Files | Risk |
|---|---|---|
| Modify | Standard/MachineFleets.cs, ActivityFacilities.cs, WorkFacilities.cs, RecurringPlans.cs | medium |
| Modify | Existing proof definitions, shared selection fixture, NamedFleetTests compile-only | low |
| New | SelectionPolicyContractTests.cs | medium |
| Modify | Engine identity, protocol expectation, Program registration | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Wrong facility node/binding | Existing incompatibility, no fabricated node |
| Callback throws | Original typed error, fault, no facts/checkpoint |
| Selected private persisted state changes without Touch | Actual entity codec faults |
| Clock/map/identity changes | Scoped observation faults |
| Healthy native/direct decisions | Original admission, movement, resources and work |

## 6. Backward compatibility

Development only; no compatibility base/reader. Build2.398.0/save154/replay1; persistent layouts unchanged.

## 7. Verification

Build completed slice; ONE NEW exact bounded case: Selection policies: direct contracts guard metadata and physical fleet facility work. Runtime60000ms/owned termination5000ms/output30000chars; physical stages independently capped at128/96 updates. Initial30000ms attempts are preserved, not reported green. Repairs may rerun this same case only. No old/family/full/Oil/Tycoon/platform runtime; full regression open.

## 8. Risks

Selected persisted codec observation is not arbitrary nonparticipating CLR state purity. Broad provider/reentry/global/performance/lockstep/fuzz/soak/full acceptance remain open. Callback ContentException must not become ordinary incompatibility.

## 9. Out of scope

Protected clock and excluded Tycoon files, Oil/platform/hosts/Draw. Original P1-P12 compound gates unchanged. Commit/push deferred until full goal and plan complete and verified.

## 10. Sub-task execution order

1. Remove bases and migrate existing production/proof references.
2. Strengthen original selected callbacks/metadata.
3. Reuse fixture for physical players and invalid observation injection.
4. Build and run only new exact bounded case; preserve attempts.
5. Reconcile evidence/tracker/audit without broad claims.

## 11. Delivery status

Scoped verified, not full-plan completion. FINAL ONE NEW exact case159/0/0 in34660ms, PID20468 Exited0/observed34823ms, outer60000ms. Nine clean completed-slice builds, FINAL4.68s. Preserve seven preceding attempts including six30000ms timeouts and one44/1 aborted-stage result; attempt7 also logged four assertion failures from observing unused metadata in the wrong test query. Actual consuming-query correction, per-stage limits and explicit JSON speed120 leave all production timing/work owners intact. Broader performance/provider/reentry/all default-speed routes/full P1-P12 remain OPEN.

Selected422 inputs frozen2026-10-09 20:38:26 UTC, no executable changes after final green. [All evidence and repairs](../../docs/migration/P9_SELECTION_POLICY_CONTRACTS_VERIFICATION.md). Commit/push deferred until full goal and plan verified.
