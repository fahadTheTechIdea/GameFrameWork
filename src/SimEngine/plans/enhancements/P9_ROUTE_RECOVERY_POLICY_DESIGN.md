# P9-C13: physical route and recovery policy acceptance

Current M108/native2.742/save161/replay2 requalifies only the existing NETWORKED native cargo
literal137/0/seven actual verifiers, not the twelve-variant C13 case below. Ordinary Desired=0
public recovery still observes worker reserves, area, export permissions/reserve and receiving
maxima; two physical deliveries settle actual claims and conserve20supplies. The separate
emergency case now retains actual demand provenance/private receiving/urgency after loaded
hold and save. [Current native correction and all execution evidence](../../docs/migration/P10_COMPANY_ALERT_AUTOMATION_VERIFICATION.md#current-m108-held-emergency-cargo-serves-its-original-private-demand--2026-10-23).
Original broad requirement status is owned by MASTER_TRACKER, not this historical proposal.

## 0. Problem statement

Original P9.acceptance.043 requires every dispatch type to respect areas, reserves, maximum stock and permissions. G64 proves automatic stock-demand hauling, not the whole requirement. Scheduled routes, evacuation and cargo return need actual physical evidence; inspection of shared LogisticsService.Preview, PortSpare, ExportPermission, ReceivingCeiling and IWorkerDispatch.CheckAssignment is not sufficient.

## 1. Goals

One new exact bounded case covers route, evacuation and cargo-return dispatch with native/direct JSON profiles and original policy owners. Block acquisition with operating areas, ready-worker reserves, protected cargo/material reserves, destination filters and disabled outlets. Release player settings and prove finite, timed loading/travel/unloading with receiving ceilings, saves, pause, twin hashes, private views, replay and isolation.

## 2. Design

Reuse G64 pure scenario authoring and thin input/view/save helpers; replace the singular additional-module authoring hook with explicit collections. Recovery registration/settings are authored through the same master document path. Native storage evacuation and original transporter cargo are initialized by explicit content, not test mutation. Minimum/desired zero with a positive receiving maximum deliberately suppresses automatic stock demand, isolating the selected dispatch path. No second scheduler, ledger, recovery model or policy decision in the driver.

## 3. API surface

No new public runtime API. Existing AddRoute, ConfigureWorkPolicy, ConfigureCompanyAlert, ConfigureOutletPolicy and ConfigureStockPolicy commands, immutable views, sole-clock Update and saves/replay.

## 4. Files to change

- Add tests/Beep.SimEngine.Tests/RouteRecoveryPolicyAcceptanceTests.cs: one new player-driver case.
- Modify DispatchAcceptanceTests and LogisticsPolicyContentTests: shared pure scenario authoring and player-driver utilities only; no historical runtime invocation.
- Modify Program: one new exact registration.
- Stage/evidence, master and audit: current scoped facts, original gate unchanged unless all dispatch paths are proven.

## 5. Error handling

512 Updates per physical phase; public clock/job/blocker diagnostics on a stall. Outer 60000ms with 5000ms owned termination observation and 30000-character output. Preserve all same-case attempts and actual compilation outcomes. Typed policy refusals never create resources or instant delivery.

## 6. Backward compatibility

Dev only; no compatibility layer. No protocol/payload change unless a real production defect requires it.

## 7. Verification

One solution compile for this stage, targeted test-project rebuilds only for same-case repairs. Only the new exact runtime. Historical/family/full, Oil, Tycoon, platform, lockstep/stress and broad provider gates remain open. This stage alone cannot close .043: remaining native work/automatic work supply/independent-provider category-wide acceptance stays explicit.

## 8. Risks

Point geometry does not allow crossing/overtaking. Use one real carrier and reachable points; no teleport/default/fallback. Cargo reserves must apply to the actual original inventory. A changed maximum cannot retract acquired receipts; new dispatch must obey the new ceiling.

## 9. Out of scope

Oil/Tycoon/protected catalogs, rendering/platforms, parallel subsystems, old runtime suites and staging/commit/push before full goal and plan completion.

## 10. Execution order

1. Inspect shared admission and actual recovery/cargo owners.
2. Author three isolated dispatch categories and one bounded physical case.
3. Compile/run that exact case and repair only observed defects.
4. Record actual evidence and refresh tracker/anchors without broader claims.

Status: scoped physical proof verified: FINAL3024/0/0 in4189ms, actual owned PID8228 exited0/4343ms. Three clean compiles/final5.15s and three same-case attempts retained. Twelve variants/two actual deliveries each, original2.424.0/save154/replay1 unchanged; .043 and full P1-P12 remain OPEN. See [evidence](../../docs/migration/P9_ROUTE_RECOVERY_POLICY_VERIFICATION.md).
