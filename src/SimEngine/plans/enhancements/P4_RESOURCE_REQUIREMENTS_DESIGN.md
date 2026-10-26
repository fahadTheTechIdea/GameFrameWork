# P4-G2b2a/G2c1 — Authored resource sources, held tools and physical consumption

Status: focused-verified, 2026-10-05: **159 checks / two exact suites**, zero failures/aborts,
build2.49.0/save24/work9/view9/flow4. See
[evidence](../../docs/migration/P4_RESOURCE_REQUIREMENTS_VERIFICATION.md).
Parent: P4_REQUIREMENT_EXPRESSIONS_DESIGN. Depends on P7-C4a, not a replacement
for the remaining operating-area/target/provider/continuous-input and complete joint-acquisition scope.

## Problem and measurable goals

WorkBoard already evaluates named all/any expressions and WorkSystem selects actual workers. The
existing node stores branch choices but has no selected resource-source links or physical tool lifetime.
P7 now owns complete-set native promises. Connect those contracts inside the same evaluator/node/job
owners: pure scoped source offers, actual all-or-none resource/worker assignment, owned held tools,
first-physical-work consumption, interruption/cancellation release and saved exact continuation.
No second selector, scheduler, inventory, reservation ledger or authoring registry.

## Contracts and JSON

IResourceWorkRequirement extends the existing leaf family with immutable Bill/Use, pure CopySources
and CheckSource. Sources are scoped ResourceEndpointId plus deterministic cost; source checks declare
physical availability at the target/worker, not permission to teleport remote stock. WorkBoard remains
the single source selector, uses ResourceTransactions for stock/claim checks, and composes selected
leaf occurrences using the same all/any traversal. Every alternative still validates; invalid dominates.
Individually ready leaves can compete for one scarce source. The same WorkBoard evaluator performs
one iterative, prefix-pruned joint source search in canonical occurrence/cost/scope/identity order,
preflighting native sets through ResourceTransactions. It chooses the first feasible combination,
not a false stock blocker when another declared source fits. Bound/spent occurrences remain fixed;
no new selector, persistent search state, mutation during search or hard combination cap.

TargetResourceRequirementDef and WorkerResourceRequirementDef share ResourceWorkRequirementDef's
native Bill/strict consume|hold authoring and local-source checks. Worker sources use the existing
IWorkerWorkRequirement marker. Ordinary master-listed target_resource_requirements and
worker_resource_requirements sections register these definitions; modules may register another concrete
IResourceWorkRequirement provider in their own section. No hard-coded resource or provider type lookup.

```json
{
  "target_resource_requirements":[{"id":"work.materials","use":"consume","quantities":{"steel":2}}],
  "worker_resource_requirements":[{"id":"work.tools","use":"hold","quantities":{"wrench":1}}],
  "work_requirement_groups":[{"id":"work.ready","mode":"all","entries":[
    {"id":"materials","requirement":"work.materials"},{"id":"tools","requirement":"work.tools"}
  ]}],
  "activities":[{"id":"work.prepare","automatic_supply":false,"nodes":[
    {"id":"prepare","roles":["builder"],"hours":1,"requirement":"work.ready"}
  ]}]
}
```

## Execution and sole state ownership

- Immutable selected source facts include occurrence paths, authored leaf identity, endpoint, native
  Bill and usage. Offers expose typed actual native availability, cost and refusals through detached
  requirement/worker views. Queries never acquire or spend; retained offers never authorize mutation.
- ActivityNodeExecution owns actual resource binding/receipt facts through its existing Sync. Endpoint
  ResourceReservationBook alone owns live promises; Job.Worker/Role and IWorker.Job remain the machine
  claim. ReservationOwner derives unambiguously from order, named node and requirement occurrence.
- Assignment rechecks the complete selected resource set and acquires it before committing the current
  worker relation; timed positive work uses the same path. Zero-time resource work refuses authoring.
  No source/tool bypasses normal machine travel. At actual Work reach (or timed work start), consume
  consume-mode receipts once through ConsumeClaims. Hold-mode receipts protect tools until completion.
- Pause retains owned tools/selection. Reopen/loss/cancel releases only unspent promises and retains
  spent occurrence facts and physical progress. Once materials are spent, retain the committed workflow
  branch; do not silently switch to another recipe and charge a second material bill. Sources inside
  the same leaf may reselect after release. Spent inputs never require their old source to remain alive.
- Restore validates authored paths, usage/native bills, owner/token links, active/terminal state and
  actual endpoint promises. Entity books and scoped providers' pure InspectReservations callbacks
  expose their existing records for orphan validation only, not a new discovery/endpoint registry.
  Immutable views contain facts only. Work/view/save layouts break together;
  no old reader. This slice added no automatic supply path; the subsequent G2c2 migration below
  removes old Needs/cash/supply authoring through these same owners. Continuous use remains separate.

## Failure, files and order

Missing/incompatible/unavailable sources and insufficient native stock are typed ordinary blockers;
malformed provider identities/cost/results or illegal authored quantities fail explicitly. Whole-set
conflicts refuse before any promise/machine mutation. Provider protocol violations fault normal Update
without compensation. Lost receipts release surviving unspent claims through the existing transaction.

Extend WorkRequirementResult/WorkRequirements/WorkBoard/WorkSystem/WorkView and existing resource
check facts; declare JSON in WorkModule. Extend the normal non-friend proof with an unrelated scoped
source leaf, actual JSON resources/tools and command-driven physical execution. Read contracts → strict
authoring → pure selection → actual node/endpoint lifetime → sole codec/views → two exact suites → docs.

Verification: one authored scoped-source/whole-set/private-offer suite and one physical held-tool/
consumption/pause/reselection/cancel/save suite, with bounded stages and diagnostic failure. Build the
solution. Do not run broad/full suites. Remaining target/area policies, generic executors/templates,
elapsed-time input breadth, optional reserved-budget semantics, oil integration and full replay/stress/acceptance
gates remain open. No Tycoon, host/demo, Draw, Unity/Godot or platform work.

## Subsequent migration — P4-G2c2 (focused-verified)

The [work-start stage](P4_WORK_START_DESIGN.md) deletes ActivityNode Needs/Cash/OnCredit, TrySupply
and early spending. Its final build2.50.0/save25/work10/view10/flow4 passes **245 / two exact suites**,
including this stage's directly affected methods, not a broad-family rerun. See
[evidence](../../docs/migration/P4_WORK_START_VERIFICATION.md). Named target-input/cost requirements
share this evaluator/node codec; the existing LogisticsService supplies combined pending intent.
Optional IEconomy checks aggregate ordinary costs before assignment and actual affordability before
positive physical work. StartWork alone pays/consumes exact receipts once; obligations are not reserved
money. Infrastructure uses exact authored multipliers; salvage is explicit native yield. No legacy
translation/wrapper or duplicate material spending remains. Work-normalized continuous inputs are now
focused-verified in [G2c3](P4_CONTINUOUS_WORK_DESIGN.md), build2.51.0/save26/work11/view11,
**364 / two exact suites**, including this slice's directly affected methods. See
[evidence](../../docs/migration/P4_CONTINUOUS_WORK_VERIFICATION.md). One cumulative/delta calculator and
AdvanceWork share actual progress/native transactions; no rate/remainder ledger. Elapsed-time input
breadth, operating-area/target
policies, optional reserved-budget semantics, broader economic guards, generic executors/templates,
replay/stress/performance and full acceptance remain separate.
