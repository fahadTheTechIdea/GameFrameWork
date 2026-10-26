# P9-A14 — Active manual plan prerequisite control

Status: focused verified2026-10-07. Extends A10 on the same board and native jobs.
Build2.185.0/save101/board35/work32/view63/native7/public5/public-retention2/construction-view4.
Clean build;1,665 assertions/two exact suites (1,101 contracts;564 physical), zero failures/aborts.
[Evidence](../../docs/migration/P9_ACTIVE_PLAN_DEPENDENCIES_VERIFICATION.md). Full roadmap remains partial.

## 0. Problem

PreviewPlanDependencies refuses every nonpending plan. Players cannot change the prerequisites
of an ongoing physical project, even though the actual board already owns its graph, progress,
claims and controls. Treating an active plan as a new pending plan would duplicate execution
and spent resources; retaining its machine while awaiting new machine work risks deadlock.

## 1. Goals

1. Existing atomic prerequisite command admits owned live manual plans, including active ones.
2. The sole existing compiler validates the complete proposed topology before any mutation.
3. Active dependencies gate travel, assignment, work, completion and automatic supply.
4. Earned progress, spent inputs, completed effects, native target/order/job identities persist.
5. Newly blocked assigned jobs release machines through existing interruption/resource owners.
   Noninterruptible assigned workers refuse the whole edit with matching per-entry preview.
6. Private immutable views, queued commands, cancellation, save/load and twin continuation
   read the same actual graph, not copied state or a second clock.

## 2. Design

Extend PreviewPlanDependencies/ConfigurePlanDependencies, not add another command/model.
Pending or active manual records accept canonical replacement sets. Terminal history remains
immutable; recurring-source admitted topology remains authored and refuses manual editing.

The preview checks actual owned prerequisites and normal interruption permission for every
assigned native job when a proposed prerequisite is not Done. Proposed complete company DAG
uses the existing DependencyGraph compiler. Commit all sets only after complete admission.

Replace IsPlanPaused with one board PlanOrderBlocker that resolves explicit pause and actual
prerequisite state. Existing execution, assignment, supply and views use that same gate.
Generalize existing PausePlannedOrder to accept the actual blocker. Dependency waits reopen
assigned jobs through ordinary Interrupt/Reopen, retaining native progress/spent bindings and
returning unspent claims; explicit player pause retains its established behavior. No refunds,
instant restart, job duplication, resource copies or completion replay.

Saved graph validation accepts actual unresolved prerequisites on manual active or cancelled/
failed records, but still rejects missing/foreign IDs, cycles and successful completion with
unfulfilled prerequisites. Recurring-source executions retain their admission invariant.
Current schemas revise directly; no legacy readers or compatibility path.

## 3. API

Same PlanDependencyChange/Changes, IPlanBoard methods, command and WorkView previews.
Terminal edits retain NotPending; source-owned edits retain SourceOwnedDependencies;
noninterruptible assigned work uses existing WorkPolicyCodes.NotInterruptible. Dependency
pending/cancelled/failed diagnostics already exist. No new JSON schema: workflows still use
explicitly master-listed definitions/templates and existing public contracts.

## 4. Files

WorkPlans.cs, Work.cs, PlanDependencyChanges.cs, WorkModule.cs and current engine identity.
Same two template suites gain active control contract/physical callbacks; obsolete active-only
refusal assertions are replaced, not retained as a compatibility test path. Docs/master evidence.

## 5. Errors

Missing/foreign links, source ownership, noninterruptibility, terminal edits and cycles refuse
the complete set without any graph/progress/claim prefix. Runtime provider contract faults use
existing fail-stop boundaries, never partial successful compensation.

## 6. Compatibility

Direct current contract/save semantics. No legacy or duplicate execution authority.

## 7. Verification

Build and only the two existing exact recurring-template suites. Actual command-authored active
projects: cycle/owner/terminal/noninterruptible/source-owned refusal, atomic edits, stable target/
order/job identity, same native progress/spent inputs, stopped motion, released worker, no supply
or effect while waiting, physical prerequisite completion and resumed work, failed/cancelled
prerequisites and explicit recovery, queued saves, private views and deterministic saved twins.

## 8. Risks

Do not overwrite a pending completion-effect retry blocker or replay completed effects.
Machine interruption is a real existing policy permission, never forced teleport/free work.
Dependency control is between admitted plans; changing authored node graphs is separate work.

## 9. Out of scope

Authored active node-graph replacement, recurring admitted shape rewriting, fleets/fuel/teams,
global sandbox/performance/replay/full acceptance. No Oil/Tycoon/host/demo/platform/Draw.
No clock, game loop, size cap or parallel graph store.

## 10. Stages

- [x] Extend existing atomic live-manual admission and shared native execution gate.
- [x] Add command-driven physical/progress/material/interruption/save proofs.
- [x] Build, run exactly two relevant suites, reconcile evidence and master.
- [ ] Broader authored active topology and full framework acceptance.

