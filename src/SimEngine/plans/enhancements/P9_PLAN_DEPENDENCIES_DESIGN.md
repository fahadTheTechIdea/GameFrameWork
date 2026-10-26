# P9-A8 — prerequisites between persistent plans

Status: focused verified, 2026-10-07; build2.176.0/save96/board29/work30/work-view56/construction-view4.
Clean build;446 assertions/two exact suites (231 contracts;215 physical), zero failures/aborts.
[Evidence](../../docs/migration/P9_PLAN_DEPENDENCIES_VERIFICATION.md).
Parent: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Prerequisite: [portable templates](P9_REMOVAL_TEMPLATES_DESIGN.md).

Historical checkpoint: A9 added [entity activities/upgrades](P9_ENTITY_ACTIVITY_UPGRADES_DESIGN.md);
A10 now owns [current pending dependency editing](P9_PLAN_DEPENDENCY_EDITING_DESIGN.md) on2.178.0.
Original A8 callbacks are included in A10's two exact current template gates. This A8 manifest and
its count remain historical, not current whole-framework regression proof.

## 0. Problem

WorkPlans.PromotePlans admits each pending entry independently. Activity nodes already form a DAG,
but a group of separate projects cannot declare that one actual project must finish before another.
Priority and list order are not prerequisites and cannot guarantee physical sequencing.

## 1. Goals

1. A PlanIntent declares immutable prerequisite plan IDs; the existing PlanEntry alone persists them.
2. Whole-group authoring can link member positions before IDs exist. Admission resolves these links
   once to actual stable plan IDs, with no second graph execution store.
3. Master JSON templates explicitly declare `after` arrays of stable entry IDs. Missing references,
   self-links, duplicates and cycles use the existing DependencyGraph compiler and fail before play.
4. Promotion waits for every prerequisite to be Done. Cancelled/failed prerequisites remain typed
   blockers; no free completion, retry fallback or automatic descendant cancellation occurs.
5. Capture/repeat/reorder/regroup/recurrence/save/hash/commands/views preserve actual graph identity.
   Repeated whole groups remap internal links; portable capture refuses external live bindings.

## 2. Design

Extend PlanIntent with Dependencies (actual positive long IDs). Extend PlanGroupIntent with immutable
PlanGroupDependency values (zero-based dependent/prerequisite member positions). These are authoring
links only; WorkBoard resolves them at whole admission and discards the positional representation.
PlanTemplateEntry implements IDependencyNode. Its `after` IDs compile via the sole DependencyGraph;
provider JSON geometry remains unchanged. Expansion converts stable authored IDs to group positions.

WorkBoard retains CheckSubmission for admission, then checks prerequisite disposition before native
CheckActivation. No resources/order/target are created for blocked successors. PlanView carries the
same frozen dependency IDs and computes blockers from actual predecessor records. Mutating display
membership/order never mutates links. All runtime validation also uses DependencyGraph, not another
cycle algorithm. Existing plan/group/template command codecs include the extended immutable intent.

Whole-group repeat remaps edges among current members to new IDs; references outside the group remain
explicit actual plan prerequisites. Capture is portable only if every prerequisite is captured too.
Recurring priority transformation preserves group links. Existing source provenance/time/contiguous
IDs remain the cycle owner; no scheduler/current time/history ledger is added.

## 3. API

PlanIntent(..., IEnumerable<long> dependencies = null) captures an empty or explicit prerequisite set.
PlanGroupIntent(..., IEnumerable<PlanGroupDependency> dependencies = null) captures optional member
links. Empty sets are genuinely independent plans, not fabricated prerequisite providers.
PlanTemplateEntry.Dependencies and required JSON `after` use stable authored IDs.
PlanCodes.DependencyPending/DependencyCancelled/DependencyFailed/ExternalDependencies are typed facts.

## 4. Files

PlanContracts, PlanTemplates, WorkPlans, WorkPlanGroups, WorkPlanTemplates, WorkRecurringPlans,
RecurringPlans, Work/WorkModule, SaveGame; directly authored template JSON and selected proof fixtures.
Focused PlanTemplateTests callbacks and a PlanDependencyTests partial file. No harness gameplay.

## 5. Errors

Nonpositive/duplicate prerequisite IDs refuse construction. Missing/foreign IDs refuse admission.
Missing/out-of-range/duplicate/cyclic group links refuse immutable authoring before allocation.
Cancelled/failed prerequisites do not satisfy completion. Invalid saved links/topology/disposition
throw CorruptStateException. Capturing external dependencies returns a typed whole-group refusal.

## 6. Compatibility

Direct JSON/command/board codec revision. No old reader, migration shim or inferred `after` field.

## 7. Verification

Clean build and exactly the two existing PlanTemplateTests suite names. Add strict graph authoring,
forward refs/cycles, private missing/foreign IDs, pure preview, save inbox, physical sequential and
parallel branches, cancellation/failure, repeat/capture remapping, recurrence and paused twins.
Bounded waits. Other suites compile only; no full regression.

## 8. Risks

Bindings are actual current IDs, not future target promises. Group repeat after regroup retains
external references honestly. Full graph validation is a checkpoint cost, not a new runtime index.

## 9. Out of scope

Editing admitted topology, arbitrary plan libraries/history retirement, survey/upgrade providers,
global provider purity, large-graph performance, replay/lockstep/full acceptance. No platform/Draw.

## 10. Execution

- [x] Inspect the current board/template/compiler/source owners and specify one-owner links.
- [x] Extend immutable authoring and existing board promotion/codec/validation.
- [x] Integrate portable capture/repeat and both recurring sources without copied state.
- [x] Build, run two exact suites and publish focused evidence/tracker. Full P9 remains partial.
