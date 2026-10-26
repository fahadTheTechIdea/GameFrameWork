# P9-A5 — persistent infrastructure intents and portable corridors

Historical checkpoint, not rerun by the current [A6 removal-plan slice](P9_REMOVAL_PLANS_DESIGN.md).
Its source manifest records the A5 checkpoint, not current post-A6 source hashes.

Status: focused verified, 2026-10-07; build2.173.0/save95/board28/work30/work-view55.
Clean build;457 assertions/two exact suites (144 contracts;313 physical), zero final failures/aborts.
[Evidence](../../docs/migration/P9_INFRASTRUCTURE_PLANS_VERIFICATION.md).
Parent: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Prerequisites: [templates](P9_PLAN_TEMPLATES_DESIGN.md),
[recurring template groups](P9_RECURRING_TEMPLATES_DESIGN.md), existing physical line segment DAG.

## 0. Problem

Networks.cs LineService.Begin posts marker-specific lay:entity activity keys directly. LineDef is
not an IPlanDefinition, so corridors cannot be pending plans, grouped templates or recurring
authored intents. WorkPlans requires one static definition instance as the actual graph; varying
path-sized graphs need binding to the real execution target, not a duplicate plan/scheduler store.

## 1. Goals

1. Authored line:<LineDef ID> keys resolve through the existing board to actual master content.
2. Strict intent arguments contain explicit space and ordered native tile placements, no live IDs.
3. Existing submission/activation/controls/Sync own pending, active and terminal corridor plans.
4. The real LineWork cursor owns its immutable derived segment graph alias. No second saved graph.
5. LineDef supports relative corridor capture/translation through IPlanTemplateDefinition and the
   selected ISpatialPlacementTemplates, including mixed structure/line groups and recurring templates.
6. Existing per-segment travel, supply, money, earned effects, recovery and sole-clock time remain.

## 2. Design

IPlanDefinition.BindExecution(World, EntityId) binds the actual native graph. Fixed ActivityDef and
ConstructibleDef return themselves; LineDef returns the graph derived from its actual LineWork.
WorkBoard uses the same contract on activation verification, load reconstruction and checkpoints.
LineWork retains only a transient immutable alias derived from its existing Line/Tiles state; reset
on decode. Stable keys replace the numeric marker-key resolver directly, with no old-key reader.
One authored representative segment validates role/requirements/time/effects at composition; the
actual positive-length path expands only inside the existing LinePlan graph owner on activation.

Mixed physical proof exposed stale automatic supply after worker material acquisition. WorkSystem
now owns one RefreshSupply publication from actual eligible nodes/bindings, before dispatch and
after each successful shared-pass acquisition. The same CopySupply/Bill/demand identity remain;
no cargo cancellation, synthetic reservation release, new ledger or altered retry policy.

LineDef owns strict argument reading, stable whole-path identity, submission/current activation,
execution identity and portable template methods. Relative path entries are delegated to the
selected spatial provider, not arithmetic in hosts. Bounds, contiguity, uniqueness and duration
remain LineService checks. Submission may record structurally valid currently obstructed intent;
activation alone commits a cursor/order. No partial corridor is instantly stamped.

LayLineCommand becomes a normal board submission through LineDef.CreateIntent; its result Arg0 is
the accepted plan ID. LineService.Begin remains the single physical execution primitive used only
after actual admission. Generic plan/template/group/source commands need no infrastructure branch.

## 3. API

IPlanDefinition.BindExecution(World, EntityId); line:<ID>; arguments:
{"space":"colony.tiles","tiles":[{"space":"colony.tiles","x":20,"y":20}]}.
Template arguments use the same outer space/tiles keys with provider-relative dx/dy entries. Required
explicit fields and reader completion reject extra target/order/progress payloads. Existing command
inbox freezes ordinary intent/template arguments.

## 4. Files

PlanContracts.cs, Work.cs/WorkPlans.cs, Construction.cs, Networks.cs and InfrastructurePlans.cs,
WorkModule.cs, LayLine command registration, master-listed proof JSON and exact line tests. Source
identity migrations compile only outside those tests. Stage/evidence/master/README/AGENTS/help.

## 5. Errors

Malformed/empty/foreign-space/extra fields -> explicit schema refusal. Noncontiguous/repeated path,
actual bounds or checked duration overflow -> existing typed refusal. Foreign ownership/blocked
geometry/already-laid paths remain activation blockers. Duplicate whole-path intents use existing
board identity admission. Mismatched cursor/graph/order/path must reject checkpoint/activation.

## 6. Compatibility

Direct stable-key and graph-binding migration; no lay:marker reader or parallel command path.
Build/board/view/command semantics identify the revision; unchanged serialized owner layouts.

## 7. Verification

Build; promote only two existing LineSegmentTests registrations to:
Infrastructure plans: authored corridors, templates and admission;
Infrastructure plans: physical segments, controls and saved continuation.
Include prior earned DAG/supply/ownership/recovery cases plus new pending/mixed template/recurring/
private/strict/cancellation/checkpoint proofs. Bounded waits with diagnostic snapshots. No third,
family or full regression run; wider migrated callers compile only.

## 8. Risks

Runtime graph shape must derive actual cursor data, never today's mutable preview or a fabricated
default. Pending plan identity cannot depend on entity allocation. Group admission does not reserve
all materials, guarantee simultaneous placement or invent network geometry for other spatial types.

## 9. Out of scope

New point/volume line execution providers, survey/upgrade/removal templates, cross-entry dependencies,
runtime libraries/history/pruning/global purity/replay/stress/full acceptance. No Oil/Tycoon/host/
demo/platform/Draw or second clock.

## 10. Execution

- [x] Inspect existing owners and document the direct revision.
- [x] Bind execution graphs through existing plan contract; replace marker-specific keys.
- [x] Extend line intent/template provider and route normal command through existing board.
- [x] Add strict master-listed and bounded physical proof; exactly two focused gates.
- [x] Ownership review and source/evidence/master/help reconciliation; full objective remains open.
