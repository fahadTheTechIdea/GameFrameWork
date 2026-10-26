# P9-A13 — Reference-safe plan history lifecycle

M104 current qualification on the unchanged2.739image: [existing networked physical/history
and public-budget cases216/0 with eight actual replays](../../docs/migration/P6_NETWORKED_MOD_DURABILITY_VERIFICATION.md#current-m104-networked-plan-history-continuation-after-retention-correction--2026-10-23).
No source/test/provider rewrite or rebuild; M103 behavior below remains unchanged. This
qualifies only the selected history interaction, not every optional-module or closure matrix.

Current M103/native2.739/save161/replay2 uses original ReadProvider with full component-owner
observation and preserves first callback causes; recursive/swallowed faults are terminal.
Three focused helpers extracted from broad template bundles pass295/0 with complete physical
0→168replay. [Current evidence and limits](../../docs/migration/P9_PLAN_REFERENCE_OBSERVATION_VERIFICATION.md#current-m103-first-failures-and-full-component-owner-observation--2026-10-23).
Earlier A13.1/A13.2/A13.3/A13.4 scoped evidence remains its actual2026-10-07image, including
2.184/save100 and1,558assertions in two broad suites; those bundles are not rerun now.
[Earlier closure evidence](../../docs/migration/P9_PLAN_HISTORY_CLOSURE_VERIFICATION.md).
The [A13.2 checkpoint](../../docs/migration/P9_AUTOMATIC_PLAN_HISTORY_VERIFICATION.md) remains historical.
The [A13.1 checkpoint](../../docs/migration/P9_PLAN_HISTORY_VERIFICATION.md) remains historical;
its selected reference/physical callbacks are now independently registered; unselected broad
helpers compile only. Do not infer their current execution from the earlier two-gate counts.
Full framework roadmap remains partial.

## 0. Problem

Before A13, WorkBoard pruned ended native jobs/orders but retained every terminal PlanEntry forever.
Deletion cannot simply follow age: actual prerequisites, frozen recurring cycle heads,
source LastPlan, unresolved native disposition, and player-controller cursors still refer
to these records. No platform/world-size limit may dictate engine history lifetime.

## 1. Goals

1. Explicit player retirement is atomic, private, previewable and reference-safe.
2. Whole recurring cycles remain intact; latest source history remains while referenced.
3. Modules declare additional history references through an optional public contract.
4. The sole WorkBoard remains history owner; IDs never recycle and saves permit honest gaps.
5. Authored automatic retention uses the same retirement admission, not another ledger.

## 2. Design

### A13.1 Explicit lifecycle and references

PlanHistorySelection is an immutable canonical nonempty set of actual positive plan IDs.
It reuses PlanIntent's existing canonical identity capture and wire codec.
IPlanBoard.PreviewRetirePlanHistory returns PlanGroupCheck with every selected entry.
RetirePlanHistoryCommand commits only when the whole selection passes; the board removes
actual terminal records from its existing list and lookup, leaving its next-ID untouched.
No archived shadow records or synthetic completion tokens replace removed history.

Admission requires owned terminal records, no surviving prerequisite references, all
members of each selected recurring cycle, no live recurring source LastPlan reference,
no target-retained native order, and no module-declared reference. Dependencies wholly
inside the retired set are allowed. Group labels alone are presentation, not references.
Existing queued commands remain sequenced authority: retirement first makes later old-ID
commands refuse NotFound, never rebind to a reused group label or newly admitted ID.

Optional IPlanHistoryRetention.RetainsPlan(world, company, plan) is discovered through existing
service, system and component registries, with aliases observed once. It owns no extra
pin store; providers read their existing authoritative state. EntityObservation guards
clock/identity and component state. Recurring references are read through the existing
source contracts; native order retention remains IWorkOrderRetention.

PlayerControllersSystem forwards optional controller retention even while human-selected;
the existing controller Sync hashes its state before/after the observation.
Unconsumed accepted template replies retain their actual returned contiguous IDs, including
the interval before awaiting replies are collected. PlanPlaybook retains its actual cursor
range until its existing controller advances. No extra controller cursor or clock.

### A13.2 Authored automatic policy — focused verified

The optional sim.plan-history module selects explicitly master-listed policy content, using
single-clock terminal age, minimum retained terminal cohorts per company, and a per-update cohort budget.
No implicit pruning policy, fixed history size cap, independent cleanup timer, partial-cycle
deletion, or substitute ledger. Actual EndedTick timestamps and a saved native-ID fairness cursor
remain in the existing board codec. Automatic candidates reuse A13.1 references and a safe-set
fixed point. Private immutable diagnostics explain live/young/minimum-retained/reference-held
cohorts. Protected references may retain history indefinitely; no maximum engine size is imposed.
Public-only JSON policy and native/public physical recurrence proof use the same owner.
See the [A13.2 stage](P9_AUTOMATIC_PLAN_HISTORY_DESIGN.md) for exact implementation and limits.

### A13.3 Persisted provider observation — scoped focused verified

The existing EntityObservation owner now captures a provider's actual component/system/persistent
service Sync state and scoped entity/clock facts. WorkBoard uses it around successful or throwing
reference callbacks; controller forwarding uses the same helper with its own existing state codec.
Detected mutation latches the existing World fault even for external previews. A fault cannot be
swallowed/restored away into accepted retirement, further game time or a resumable checkpoint.
No new public state contract, ledger, serializer, rollback or second current-time owner.
Native saves/board/view payloads remain unchanged; public proof component schema directly becomes2.
[Stage](P9_PLAN_REFERENCE_OBSERVATION_DESIGN.md) distinguishes own-codec/scalar scope from unproven
arbitrary cross-provider/session/static-state isolation and large-history performance.

### A13.4 Single-admission dependency closure — focused verified

SafeHistoryCohorts admits the candidate set once per company, preserving direct blockers.
It follows actual rejected whole cohorts' native prerequisite IDs through a transient queue;
each cohort enters once, including grouped contraction cycles. Retained prerequisites do not
retain otherwise eligible descendants. Final mutation still uses ordinary reference admission.
No second graph/index, saved state, contract, clock or size cap. Counted128-plan proof uses128
service reads; this is not total CPU/allocation certification.
[Stage](P9_PLAN_HISTORY_CLOSURE_DESIGN.md) records exact scope and limits.

## 3. API surface

PlanHistorySelection; IPlanHistoryRetention; IPlanBoard.PreviewRetirePlanHistory/
RetirePlanHistory; WorkView.PreviewRetirePlanHistory; RetirePlanHistoryCommand.
Only engine commands/views are public player controls; hosts never decide retirement rules.

## 4. Files

Standard/PlanHistoryContracts.cs and WorkPlanHistory.cs; existing PlanContracts, WorkModule,
Work, PlanPlaybooks, PlayerControllers; Commands/PlanCommands; Views/WorkView; focused
template callbacks and independent public proof provider; current identity/docs.

## 5. Error handling

Invalid or empty selection: InvalidArgument. Missing/foreign ID: NotFound/NotOwned.
Live selected record: NotEnded. Surviving dependency: HistoryDependency.
Incomplete recurring membership: HistoryCycle. Actual source reference: HistorySource.
Unresolved native disposition: HistoryExecution. Module/controller reference: HistoryProvider.
Provider mutation: explicit contract fault, never silently successful cleanup.

## 6. Compatibility

Direct current build/save/board/view contract revision. No legacy reader, adapter or fallback.

## 7. Verification

Build and only two exact existing recurring-template suites. Add canonical/private/atomic
retirement, dependency/cycle/native-source/provider references, physical completed work,
unchanged stock/structures, queued stale commands, monotonic IDs, save/load/twin and pause.
Controller retention callback is included in these gates rather than a third suite.
All separate suites compile only; no full or family regression.

## 8. Risks

Dangling references can corrupt saves or strand work; retain real records until every actual
owner releases them. Preview must not mutate state. Source removal does not fake completion.
No clock changes: all existing time continues from the single GameClock.

## 9. Out of scope

Oil/Tycoon/host/demo/platform/Draw work. Active graph replacement, global purity,
performance/replay/stress/full acceptance remain separate open roadmap work.

## 10. Stages

- [x] A13.1 Explicit reference-safe lifecycle and focused physical/save proof.
- [x] A13.2 Master JSON automatic retention policy and bounded cohort lifecycle diagnostics.
- [x] A13.3 Scoped persisted reference-provider observation and fail-stop checkpoint proof.
- [x] A13.4 Single-admission native dependency closure and counted/saved physical proof.
- [ ] Wider reference/provider purity, performance and full framework acceptance.
