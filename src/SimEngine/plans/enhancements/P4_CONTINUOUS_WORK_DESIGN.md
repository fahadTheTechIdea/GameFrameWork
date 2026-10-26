# P4-G2c3 — Native continuous inputs over actual work

Status: focused-verified, 2026-10-05: **364 checks / two exact suites**, zero failures/aborts;
build2.51.0/save26/work11/view11/flow4. See
[evidence](../../docs/migration/P4_CONTINUOUS_WORK_VERIFICATION.md).
Parent: P4_WORK_START_DESIGN. Approved G2c continuous-use scope;
no separate scheduler, fuel subsystem, rate accumulator, resource catalog or reservation authority.

Follow-on [G2c4 productive elapsed inputs](P4_ELAPSED_WORK_INPUTS_DESIGN.md) runs these directly nested
resource/work-start/continuous methods in its422-check/two-suite gate on2.148.0/save77/board24/view40.
It extends the same owner with an explicit elapsed mode and replaces bounded-only views in place;
historical364 count is not additive or a new full gate. Follow-on
[G2c5 independent rate periods](P4_ACTIVITY_RATE_PERIODS_DESIGN.md) now authors elapsed rates through
the existing shared ResourceRateSpec/IResourceRate/QuantizedGameRates contracts, with485 checks/two
exact suites on2.149.0/save77/board24/view41 including these methods. Work-normalized budgets remain
unchanged; standby/wider-provider/full acceptance remains open.

## Problem and measurable acceptance

ResourceWorkRequirementDef currently supports consume-at-start and held tools only. WorkSystem advances
the existing WorkProgress after startup; no requirement can consume native fluids/fuel during actual
work. Extend that same authored leaf/source/evaluator/node/transaction path, not a host callback.

1. Master-listed target/worker/custom resource requirements author `use: "continuous"`. Their native
   quantities are the total budget for one authored node, scaled by its requirement multiplier. The
   node's duration determines the work-normalized rate; a faster machine uses it faster, not more.
2. For each line, cumulative consumption is the ceiling of total native amount times actual progress
   fraction, at that resource's native quantum (one unit for discrete, one raw fixed-point quantum for
   continuous). It is bounded by the exact authored total. The next charge is the difference between
   cumulative values before/after the prospective increment. Native rounding happens on the cumulative
   value, never independently on every tick. Positive work cannot proceed on an unfunded first quantum.
3. One WorkBoard advancement method preflights startup receipts, the complete next continuous set and
   ordinary money before mutation; native promises are acquired/consumed through ResourceTransactions,
   then existing node progress advances. A normal shortage spends nothing and advances nothing.
4. Travel/zero rate/pause use no work inputs. Existing target-input logistics supplies the actual next
   demand even for an already assigned/paused node. Worker/custom sources do not imply remote delivery.
5. Existing source selection and worker eligibility use the same effective rate/prospective progress
   as physical execution. Whole-set contention includes startup reservations and every continuous leaf.
   Lost sources reselect through existing reopen/release, preserving progress and committed recipes.
6. A continuous binding retains only actual selected source identity; no long-lived promise or cached
   remainder is invented. WorkProgress alone determines cumulative charges. Codec/view/save identities
   break together; restore rejects continuous bindings with fabricated receipts/spent flags and invalid
   branch/lifetime/ownership. Private detached views report engine-calculated consumed/remaining budgets.

## Contracts, state and owners

Extend WorkResourceUse with Continuous. Existing IResourceWorkRequirement.Quantities/Use and the
normal target/worker JSON sections remain the only resource input authoring path. WorkRequirementContext
aliases its immutable node definition and exposes actual/prospective WorkProgress; IWorkerDispatch
adds the sole pure effective-rate calculation already owned by WorkSystem. No second work-rate formula.

ResourceTransactions.EvaluateClaims accepts an empty native bill only for a pure endpoint access probe;
AcquireClaims still rejects empty bills. This reuses the sole endpoint resolver/ownership guard, enabling
zero next charge without hiding a removed/foreign source or requiring the whole budget in stock.

WorkBoard owns one cumulative/delta calculator using BigInteger intermediate arithmetic and the native
unit catalog. Its evaluator selects sources against actual next deltas, its assignment stores source
bindings without continuous promises, and its advancement acquires exact just-in-time claims. One
combined consume publishes existing actual flow facts. Node progress is the sole fractional authority;
views derive budget accounting from it, rather than saving a parallel consumed balance/remainder.

## Files, errors and migration order

Core/WorkResourceSelection, Standard/WorkRequirements/ResourceWorkRequirements/WorkContracts,
WorkResources/WorkStart/Work/WorkModule, ResourceTransactions, Views/WorkView, SaveGame; one authored
proof JSON and public non-friend module configuration; directly affected exact resource suites; docs.
No backwards reader, legacy use synonym, compatibility shim or gameplay in proof/host code.

Read current contracts/callers → strict authoring/native maths → shared preview/selection → sole
advance/supply → codec/private views → JSON/public-command proof → build/two exact suites → evidence.
Missing stock is pending; incompatible amounts are blocked/invalid; corrupt source/receipt/lifetime
refuses save/load. Provider preflight/mutation violations fault normal Update without compensation or
a resumable checkpoint. No arbitrary world, workflow, quantity or graph cap is introduced.

## Focused verification and remaining scope

Run only the existing exact authored-resource admission and physical-resource lifetime suites, extending
them with native continuous admission/quantization/rate/contending leaves, actual travel/zero-rate/shortage/
replenishment/cancel/source loss, parallel supply, saved fractional twins and pure private budget views.
Bound each physical stage and report diagnostics. Previous counts stay historical; no full/broad run.
Area/target policy, per-elapsed-wall-time utility rates (distinct from work-normalized inputs), monetary
reserved budgets, broader provider safeguards, generic executors/templates, replay/stress/performance,
oil integration and full P1-P12 acceptance remain open. No Tycoon/platform/Draw/host gameplay.
