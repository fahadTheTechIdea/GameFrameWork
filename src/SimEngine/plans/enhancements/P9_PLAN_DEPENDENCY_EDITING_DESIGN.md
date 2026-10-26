# P9-A10 — Atomic editing of pending plan prerequisites

Historical pending-edit checkpoint. [A14 active manual control](P9_ACTIVE_PLAN_DEPENDENCIES_DESIGN.md)
now supersedes its active-only refusal semantics; terminal/source-owned protections remain.
Original callbacks (with current invalid-reference checks) are included in the current A14 gates.
The numbers below remain the original stage proof, not an extra run.

## 0. Problem

A8 persists actual prerequisite IDs in WorkBoard.PlanEntry, but there is no public command or
preview for changing an admitted pending graph. Display reorder and priority are deliberately
not dependencies. Player editing must not rewrite active work or recurring cycle provenance.

## 1. Goals

1. Players can atomically replace the prerequisite sets of multiple owned pending manual plans.
2. Whole proposed topology uses the existing DependencyGraph compiler, including unchanged and
   terminal records. Cycles, self-links, missing/foreign IDs and invalid edit membership refuse.
3. Per-plan immutable diagnostics and command refusal agree; a refused set changes no prefix.
4. Active/terminal plans and recurring source-owned plans refuse edits explicitly. Finished
   requirements/claims and authored recurring history are never erased or silently rebased.
5. Existing promotion, template capture/repeat, saves/hashes and the sole GameClock read the
   edited actual records. Editing itself allocates no native work, materials or entities.

## 2. Design

PlanDependencyChange contains one actual plan ID and a canonical immutable prerequisite set.
PlanDependencyChanges captures a nonempty canonical set of those changes, not another graph.
PlanIntent remains the sole prerequisite capture/wire-schema owner; the new command embeds it.

IPlanBoard gains PreviewPlanDependencies(company, changes) and ConfigurePlanDependencies.
WorkBoard preflights every change, then compiles one proposed company topology through existing
PlanGraphNode/DependencyGraph. It commits the immutable sets to those same PlanEntry records only
after complete validation. There is no alternate graph store/index, compensation or executor.

ConfigurePlanDependenciesCommand and WorkView delegate to that existing board contract.
Current plan projection and normal PromotePlans act on the new sets at the normal game update.
Atomic multi-plan edits permit reversing a pending relationship without an invalid intermediate
cycle or briefly promoting work after a separate clear/add operation.

## 3. API

PlanDependencyChange(long plan, IEnumerable<long> dependencies) requires explicit positive IDs
or an explicit empty set. PlanDependencyChanges(IEnumerable<PlanDependencyChange>) requires
nonempty unique changes, canonicalized by actual plan ID. Commands/saved inboxes decode only
canonical positive unique rows and links. Preview returns the existing PlanGroupCheck with
actual edited plan IDs. Cyclic proposed topology returns PlanCodes.DependencyCycle for the set;
source-owned topology returns PlanCodes.SourceOwnedDependencies.

## 4. Files

PlanContracts.cs, PlanDependencyChanges.cs, Commands/PlanCommands.cs, WorkPlans.cs, WorkModule.cs,
Views/WorkView.cs and engine identity. Extend the two existing PlanTemplateTests callbacks with
PlanDependencyEditingTests; templates remain master-listed JSON, not scheduler code in tests.

## 5. Errors

Malformed authoring throws ArgumentException; noncanonical command decode throws
CorruptStateException. Missing/foreign plans/dependencies return existing typed owner refusals.
Nonpending plans return NotPending. Source-owned cycles return SourceOwnedDependencies, never
a silently mutated recurring manifest. Proposed cycles refuse the whole set, without mutations.

## 6. Compatibility

Direct new command/contract semantics and engine identity; no old reader, duplicate subsystem,
compatibility facade, legacy fallback or saved payload copy. Existing prerequisite codec remains.

## 7. Verification

Build and only these two existing exact suites:

- Recurring templates: authored groups, private admission and cycle ownership
- Recurring templates: physical projects, cancellation and saved continuation

Add immutable/canonical wire, missing/foreign/active/source-owned/cyclic whole refusal,
atomic reversal, per-entry diagnostics, saved inbox/twins, physically changed sequencing,
cancelled dependency removal, template capture/repeat, current views and sole-clock pause.
Other suites compile-only, with full regression open.

## 8. Risks

Topology checks are a transient compiler cost, not a new persistent index. Edits are pending
intent only; there is no interrupt/rebind permission over an active native order. Recurring
sources remain governed by authored content and their existing source configuration contracts.

## 9. Out of scope

Active work topology rewriting, recurring authored-template changes, runtime libraries/history
retirement, survey semantics, reference rebinding, global purity/performance/replay/full acceptance.
No Oil/Tycoon, host/demo logic, Draw, platform, adapter, clock or independent update loop.

## 10. Execution

1. Share canonical immutable prerequisite capture and define one atomic command value.
2. Extend the existing board, compiler projection, command and view contracts.
3. Add focused authoring/player/physical/save proofs to the existing two suite callbacks.
4. Build, run those exact gates, reconcile stage/evidence/master. Full plan stays active.

Status: implemented and focused-verified,544 assertions/two exact suites (294 contracts;250
physical), clean build, zero final failures/aborts. Broader regression/full plan remain open.
[Evidence](../../docs/migration/P9_PLAN_DEPENDENCY_EDITING_VERIFICATION.md).
