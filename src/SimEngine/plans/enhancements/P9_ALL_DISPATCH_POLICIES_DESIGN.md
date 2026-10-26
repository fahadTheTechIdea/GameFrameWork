# P9-C14: all physical dispatch policies

## 0. Problem statement

Original P9.acceptance.043 requires all dispatch types to respect areas, reserves, maximum stock and permissions. C13 proves routes and recovery but leaves native work, automatic work supply, independent sources and explicit hauling unproven. WorkSystem and LogisticsService already own those decisions; the acceptance case must exercise them, not introduce another coordinator.

## 1. Goals

One new bounded exact case must cover all four LogisticsDispatchKind values, ordinary and emergency native work and work-supply demand, independent IWorkerDispatchSource and explicit HaulResourcesCommand. Each relevant policy blocks actual acquisition; ordinary releases execute physical finite transfers/consumption. Save/pause/hash twins/replay/private observations remain consistent.

## 2. Design

Extract C13's pure scenario construction into DispatchPolicyScenario, extending file-authored activities and provider selection in the existing master construction helper. No duplicate authoring owner or compatibility overload. A new generic thin driver repeats category-wide physical policy enforcement using current fixtures; no old runtime driver is invoked. Native local work respects work permissions/areas/ordinary worker floors, but export reserves constrain exports, not lawful local input consumption. Emergency override only bypasses ordinary worker floors or explicitly EmergencyOnly export reserve; areas/roles/disabled outlets/maxima remain enforced.

## 3. API surface

Existing commands, SimGame.Update, immutable WorkView/LogisticsView, original saves/replay. No new gameplay API/state/protocol or platform logic.

## 4. Files to change

- Shared pure scenario authoring and file-authored dispatch-policy activities.
- New AllDispatchPolicyAcceptanceTests case and Program registration.
- C13 pure scenario method removed in favor of sole shared authoring.
- Stage/evidence/master/audit only after actual results.

## 5. Error handling

Typed preview/command refusals, no acquisition prefix or resource duplication. Physical stage512Updates with clock/job/blocker diagnostics. Outer60000ms, owned termination5000ms, output30000chars. Preserve unsuccessful attempts and repair only this same new case.

## 6. Backward compatibility

Dev only; no compatibility model. Production repair only if an actual defect is observed.

## 7. Verification

One solution compile, targeted same-case repair compiles, one new exact runtime only. Only .043 may be checked after every declared category and policy passes current physical evidence. Other recovery/durability/provider-purity/stress/lockstep/Oil/full-plan gates remain open.

## 8. Risks

Use one actual carrier, reachable point geometry and no opposing traffic. View diagnostics can report the first rejected recovery destination rather than the later full permitted receiver. Local work consumes real inputs over time; export bands are not invented local-consumption prohibitions.

## 9. Out of scope

Business/Tycoon/Oil/platforms, new scheduler/clock/stock/reservation owner, old/full runtimes, protected catalog migration and publishing before full goal/plan completion.

## 10. Execution order

1. Inspect actual category contracts and policy admission.
2. Extract pure scenario authoring; author activity JSON and all-category thin driver.
3. Compile/run one new bounded case; repair actual defects in place.
4. Record current requirement-wide evidence and original tracker status.

Status: original .043 verified with unchanged requirement text; FINAL10764/0/0 in13177ms, owned PID15920 exited0/13325ms. Forty physical variants, four compiles/final4.45s and three same-case attempts retained. Core2.424.0/save154/replay1 unchanged;221/161checked/60open, full P1-P12 OPEN. See [evidence](../../docs/migration/P9_ALL_DISPATCH_POLICIES_VERIFICATION.md).
