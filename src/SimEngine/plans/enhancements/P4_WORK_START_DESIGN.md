# P4-G2c2 — One authored supply and physical work-start protocol

P10-A3 now generates one segment node/multiplier one per corridor tile. The existing work-start
owner consumes the bill and charges only when that stage's machine actually reaches work. It no
longer consumes/charges a whole corridor at first arrival.2.125.0/save68/line_work2;386 checks/two
exact P10 suites, [evidence](../../docs/migration/P10_SEGMENT_CONSTRUCTION_VERIFICATION.md).
This historical G2c2 suite is source-revised/compiled only, not rerun; broader gates remain open.

Status: focused-verified, 2026-10-05: **245 checks / two exact suites**, zero failures/aborts,
build2.50.0/save25/work10/view10/flow4. See
[evidence](../../docs/migration/P4_WORK_START_VERIFICATION.md).
Parent: P4_RESOURCE_REQUIREMENTS_DESIGN. Approved framework migration;
no user decision blocks implementation. Preserve the full P1-P12 roadmap and all remaining gates.

## Problem and acceptance

ActivityNodeDef.Needs/Cash/OnCredit and WorkSystem.TrySupply formerly spent before a worker was acquired
or arrived. Those fields and the early consumption path are removed. All authored input/cost intent belongs
to the existing named requirement family; real commitments/progress remain on existing nodes/jobs/books.

1. Old needs/cash/on_credit node fields and bill_per_tile/cash_per_tile line authoring refuse, not translate.
2. Automatic logistics derives target-input intent from the same evaluated requirement branches, combines
   eligible parallel-node bills once per order, and never spends stock. Other source/tool providers do
   not become remote deliveries implicitly. Existing LogisticsService owns demands/hauls.
3. Costs are named IWorkCostRequirement leaves with explicit amount/credit policy. Readiness checks the
   entire selected ordinary cost, not each leaf independently. Optional IEconomy remains the sole ledger.
   It provides current-state admission, not a guaranteed money reservation: funds can change during travel.
4. Before positive physical/timed work, preflight every unspent native receipt and aggregate cost. An
   ordinary material/money refusal spends neither. After successful preflight, charge aggregate ordinary
   and authorized-credit costs and consume exact receipts once; a provider violating preflight faults,
   with no compensation/partial-success history/resumable checkpoint. No second economy/transaction service.
5. Save/hash/private views retain actual cost obligation/spent facts on the node, alongside native
   receipts; interruptions preserve spent branches/progress and discard unspent obligations/claims.
6. Generated infrastructure uses a declared requirement plus exact authored multiplier for path length,
   not generated unregistered requirement definitions. Construction salvage is an explicit native yield
   rather than inferred from potentially unchosen input branches.

## Contracts, state and strict JSON

Extend WorkRequirementResult with immutable selected cost facts; WorkBoard's iterative evaluator owns
branch selection, joint stock checks, aggregate cost admission and target supply projection. A node's
positive RequirementMultiplier is definition data, not parallel runtime state. Resource/cost leaves
resolve native intent against it; receipt restore uses the same authored scaling and rejects illegal
native quantum. IWorkSupplyRequirement marks explicit target-local stock intent for existing logistics.

Master sections: target_resource_requirements, worker_resource_requirements, work_cost_requirements,
work_requirement_groups. Node uses requirement and optional requirement_multiplier; line uses
requirement for its per-tile recipe. No legacy parser, anonymous/inline consuming requirement or wrapper.
Remove unused Supplying node state and renumber/break work/view/save layouts together.

Implemented: WorkBoard's existing partial owner now has StartWork, selected cost facts and target-supply
projection. It uses the same iterative occurrence walker for saved resource/cost branch validation.
Definition capacity envelopes take per-resource maxima across any branches, not summed alternatives;
these are pure admission metadata, never live supply or consumed balances. Exact fixed-point scaling
rejects dropped raw quantum/overflow as well as illegal native units. Logistics gap uses unpromised
stock plus existing inbound receipts, so paused claimed inputs cannot satisfy another pending job.
Colony's former embedded definition string is one manifest-listed colony.json plus colony-work.json;
nine existing authored proof documents and generated/public callers migrate without an old reader.

## Files and order

ActivityGraph/Work/WorkRequirements/WorkResources/WorkModule/WorkView, ResourceWorkRequirements,
new cost facts and WorkBoard work-start partial, existing Construction/Networks, current standalone
proof JSON/consumers, two directly affected suites, evidence/README/AGENTS/master/parent.
Read callers → strict declarations → sole evaluation/supply/start → codec/views → migrate consumers
→ build → exactly two named admission/physical suites → docs. Preserve dirty unrelated changes.

## Verification and remaining scope

Cover strict old-field refusal, fractional native scaling, pure private previews, aggregate material/
cost refusal without partial spend, no pre-travel consumption, automatic combined physical supply,
pause/funds loss/reselection/cancel, credit policy, scaled infrastructure, explicit salvage and saved
twins with bounded stages/diagnostics. No broad/full tests unless the user requests them.
Monetary budget reservations are not supplied by IEconomy's current contract and are not invented here;
they remain a separate optional provider capability if required by policy. Work-normalized continuous
inputs are subsequently focused-verified in [G2c3](P4_CONTINUOUS_WORK_DESIGN.md), build2.51.0/save26/
work11/view11, **364 / two exact suites**; see
[evidence](../../docs/migration/P4_CONTINUOUS_WORK_VERIFICATION.md). AdvanceWork replaces StartWork and
the separate WorkSystem advancement, using cumulative native consumption against real progress. This
stage's directly affected methods run in the current two suites, not a broad-family rerun. Areas/
target policies, generic executors/templates, full replay/stress/performance, oil and final acceptance
remain open. No Tycoon/host/demo/Draw/Unity/Godot implementation.
