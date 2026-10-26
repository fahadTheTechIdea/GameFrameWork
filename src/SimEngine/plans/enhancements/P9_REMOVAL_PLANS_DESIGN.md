# P9-A6 — persistent target removal plans

Historical A6 checkpoint, not rerun by [A7 portable removal templates](P9_REMOVAL_TEMPLATES_DESIGN.md).
Its scoped manifest records that checkpoint, not current post-A7 source.

Status: focused verified, 2026-10-07; build2.174.0/save95/board28/work30/work-view55/construction-view4.
Clean build;409 assertions/two exact suites (174 contracts;235 physical), zero final failures/aborts.
[Evidence](../../docs/migration/P9_REMOVAL_PLANS_VERIFICATION.md).
Parent: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Prerequisite: [bound plan execution](P9_INFRASTRUCTURE_PLANS_DESIGN.md).

## 0. Problem

ConstructibleDef's master-authored demolition graph is only IActivityDefinition. DemolishHandler
posts an order directly, so removal is missing from the persistent board despite sharing work,
physical travel, salvage, recovery and the single game clock.

## 1. Goals

1. The existing demolition graph implements IEntityTargetPlanDefinition/IPlayerActivityDefinition.
2. The construction contract exposes that actual provider, not an alternate catalog or graph.
3. Strict ordinary target/worker JSON and existing selection ownership govern intent and execution.
4. Generic pending removal may wait for real evacuation/packing constraints; immediate demolition
   command and its engine preview retain the same current physical preflight, then submit that plan.
5. Pause, priority, grouping, cancellation and saves use the existing board/order owners. Removal
   preserves stock/promises until normal timed completion and releases authored salvage exactly once.

## 2. Design

Extend the existing nested DemolishPlan, with fixed BindExecution returning itself. The existing
single-role/duration demolition shorthand is removed directly: the demolition object requires
explicit priority/automatic_supply/salvage and a nonempty nodes DAG using the same ActivityNodeDef reader/resolver,
requirements, dependency and duration contracts as ordinary work. Salvage stays its authored bill.
Each removal node explicitly declares duration and roles (empty for deliberate nonmachine gates).
The whole DAG must contain physical machine work. Absent demolition authoring exposes no orderable
work. No generated default node or engine-owned priority survives.
The existing
ActivityWorkerSelection owns parsing, identity, canonical binding and machine checks. Structural
submission validates company, exact prototype/content access, graph and target; CheckActivation adds
ConstructionService.CheckDemolition. Missing demolition authoring is non-orderable, never instant work.

IConstructionProjects.PreviewDemolition resolves the actual target provider, creates ordinary intent
and calls board admission/current activation. ConstructionView and DemolishHandler share that method.
The command submits to the board and returns its stable plan ID in Arg0; Entity remains its actual
target. WorkSystem, completion effect and recovery alone earn and commit removal. No new current time,
entity state machine, reserve store, demolition executor or compatibility result is introduced.

## 3. API

IConstructionDefinition.Demolition becomes IRemovalPlanDefinition, extending IEntityTargetPlanDefinition
and IPlayerActivityDefinition with immutable authored Priority. Existing demolish:<prototype>
keys remain. Strict arguments use existing {"target":positiveID,"workers":[{"node":"demolish",
"worker":positiveID}]} selection schema; omit workers for deliberately unbound selection.
ConstructionView.CheckDemolition(EntityId) is the pure matching immediate-command preview.

## 4. Files

Construction.cs, ConstructionContracts.cs, ConstructionModule.cs, ConstructionView.cs; existing
ConstructionPlanTests partial and removal proof callbacks. Mechanical build identity expectations
migrate directly; their runtime suites are not implied by compilation.

## 5. Errors

Missing/foreign/wrong-prototype targets and bad machine selections return existing typed refusals.
Malformed JSON is InvalidArgument; missing authoring is NotConstructible/NotOrderable. A pending
plan blocked by actual promises creates no execution; release permits normal promotion. Missing
targets remain visible blockers, never substitute another entity. Cancellation produces no salvage.

## 6. Compatibility

Direct contract and command-result migration. No old order-ID result reader, alternate command or
executor. Existing plan/work payload layouts stay unchanged; build and construction view identify
the capability revision.

## 7. Verification

Build and exactly the existing two directly affected ConstructionPlanTests suite names:
`Plans: authored intent and construction admission contracts`;
`Plans: physical sites, pause and saved continuation`.
Include original callbacks plus removal strict/private/preview, target/selection, blocked promises,
physical earned progress, pause/cancel/salvage/conservation and saved native/point continuation.
All waits are bounded with actual plan/order diagnostics. No third/family/full test run.

## 8. Risks

Active graph identity must be checked without depending on a mutable preview; terminal target removal
must remain saveable through existing history. Immediate preflight is not a resource reservation.

## 9. Out of scope

Portable spatial removal templates, survey/upgrade providers, cross-entry dependencies, library/history
lifecycle, broader provider/replay/lockstep/stress/performance/full acceptance. No Oil/Tycoon/host/demo/
platform/Draw changes. One unchanged GameClock.

## 10. Execution

- [x] Inspect existing graph, physical removal, admission and checkpoint owners.
- [x] Extend that provider and shared preview/command path, with no duplicate responsibilities.
- [x] Add bounded native/point policy, promise, selection, cancellation and durability proof.
- [x] Run build and two exact gates; reconcile tracker and evidence. Full roadmap remains partial.
