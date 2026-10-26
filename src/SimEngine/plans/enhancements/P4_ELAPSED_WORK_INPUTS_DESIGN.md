# P4-G2c4: Productive game-time resource inputs

Historical G2c4 stage. Follow-on [G2c5](P4_ACTIVITY_RATE_PERIODS_DESIGN.md) replaces its node-duration
rate denominator with explicit independent rational rates, without a compatibility reader. Existing
productive exposure/whole-set transaction/save owners remain; G2c5 includes these nested execution
proofs in its485-check/two-suite gate. The old authoring example below is historical, not current JSON.

## 0. Problem

The existing continuous requirements consume a fixed total proportional to work progress. A slower
machine consumes the same total as a faster one, so content cannot express input costs proportional
to productive operating duration. This is distinct from movement input consumption and from clocks.

## 1. Goals

1. Existing target/worker/public resource requirement contracts accept explicit JSON use elapsed.
2. Quantities specify native consumption per authored node duration, scaled by requirement multiplier.
3. Each successfully funded positive work update adds one productive game-time quantum. Travel,
   pause, shortage, zero rate and completed work add none. Even a final overshooting update uses one
   whole quantum: activity completion occurs at update boundaries, never an invented fractional time.
4. Cumulative native rounding determines next deltas. Elapsed usage is not capped to reference quantity:
   slow machines use more elapsed fuel; faster machines use less. Work-normalized continuous is unchanged.
5. Existing node/selection/transaction/logistics/view/Sync owners remain the sole execution authority.

## 2. Design

WorkResourceUse.Elapsed extends the existing interface and strict JSON reader. Same quantities/source
contracts express startup, held, work-normalized and elapsed input intent; no parallel rate catalog.
ActivityNodeExecution.ProductiveSteps counts funded productive intervals from the one game clock,
not current time or a separately advanced clock. WorkRequirementContext projects current/prospective
work and exposure using the existing effective worker rate. AdvanceWork preflights the whole resource
set and payment, commits normal receipts, then updates work/exposure together. A normal wait changes
neither; provider violations remain terminal failures. WorkBoard's one cumulative calculator supports
bounded work or unbounded productive duration with checked BigInteger intermediates.

WorkResourceUsage replaces the bounded-only WorkResourceBudget. Existing node RunningInputs replaces
ContinuousInputs; it exposes Use, ReferenceQuantity, Consumed and nullable RemainingBudget (elapsed
has no fixed remaining budget). ProductiveSteps and ProductiveGameTime are detached in ActivityNodeView. No consumed stock
or second remaining balance is saved. Existing node Sync embeds one full-width productive count and
rejects negative/future/zero-exposure positive-work state. Board24/view40/save77/build2.148.0 break in
place; no old constructor/property/reader. Native endpoints and normal flows retain resource ownership.

## 3. API / JSON

`{"id":"fuel","use":"elapsed","quantities":{"water":6}}` in the existing resource requirement
sections means six native units per selected node's authored duration. A12-minute node requires0.5
per funded minute update regardless of work rate. Half-rate work takes24 productive minutes/12 units;
double-rate work takes6 productive minutes/3 units. Alternate quantum definitions use the same authored
duration conversion; no wall time, minute constant or new clock.

## 4. Files

Core/WorkResourceSelection; Standard/ResourceWorkRequirements, WorkRequirements, WorkContinuous,
WorkResources, WorkStart, Work, WorkModule; Views/WorkView; build/save assertions; ContinuousWorkTests
within the existing two exact activity-resource suites; embedded/master-listed elapsed-work.json;
stage/evidence/master/parent/README/help/AGENTS.

## 5. Errors

Unknown use aliases and invalid native bills fail content admission. Stock shortages remain typed
pending; no partial money/resource/progress/exposure commit. Checked representational overflow is
explicit, not wrapping. Corrupt exposure refuses restore; faulted partial resource commits cannot save.

## 6. Compatibility

Development-only breaking APIs and save identity; no legacy model, synonymous property or old reader.

## 7. Verification

Build solution, then only Activity resources: authored scoped offers, native sets and private selection
and Activity resources: physical consumption, held tools, interruption and saved ownership. Extend
nested continuous proofs for elapsed JSON/native cumulative maths/private pure views, actual travel/
work rates/zero rate/shortage/automatic physical delivery/pause/save/restore/corruption. Bounded waits
reuse diagnostic500-update stages. Older nested scenarios remain execution gates, not full regression.

## 8. Risks

Elapsed quantities describe one authored duration, not a known total execution budget. Variable rates
change total operating time. Whole-quantum final admission is deliberate; previews share the rule.
Broader endpoint-specific quantum providers, remote tools, monetary budgets, stress/replay remain open.

## 9. Out of scope

Oil/Tycoon/host/demo/platform/Draw, additional clocks, idle standby fuel, new scheduler, full regression.

## 10. Execution

- [x] Extend existing requirement mode, cumulative calculation and whole-set resource admission.
- [x] Embed actual productive exposure in node codec and replace bounded-only view in place.
- [x] Extend existing nested JSON/normal command/physical fuel/continuation probes.
- [x] Build and two exact gates; scoped evidence and tracker/docs reconciliation.

Focused result: build2.148.0/save77/WorkBoard24/WorkView40; clean build,422 checks/two exact suites
(110 admission;312 physical), zero final failures/aborts.
[Evidence](../../docs/migration/P4_ELAPSED_WORK_INPUTS_VERIFICATION.md). This closes G2c4 productive
elapsed input scope only; full P4/P1–P12, independent rate periods/standby and provider breadth remain open.
