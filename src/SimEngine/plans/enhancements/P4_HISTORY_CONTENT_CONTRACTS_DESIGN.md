# P4-G14 — Open automatic plan-history policy content

Status: scoped verified,189 checks; full P1–P12 remains open. No publication before full goal/plan completion and verification.

[Current evidence](../../docs/migration/P4_HISTORY_CONTENT_CONTRACTS_VERIFICATION.md).

## Problem and goals

Before this slice, PlanHistoryModule selected PlanHistoryPolicyDef and WorkPlanHistoryPolicy validated that base identity despite storing IPlanHistoryPolicy. Direct JSON providers could not use the existing contract alone; a prior external reader duplicated the same retention fields under alternative names.

1. IPlanHistoryPolicy extends IContentDefinition; all selection/identity consumers use actual owned aliases.
2. PlanHistoryPolicyDeclaration owns one explicit minimum_age/minimum_retained_cohorts/cohorts_per_update reader. Native and external content delegate to it; migrate old proof authoring in place, no alternative field reader.
3. One existing provider observation guard captures detached settings and checks age against actual scenario time. Validate every profile before Initialize; WorkBoard alone stores cursor/selected policy and owns derived cohorts/retirement.
4. One NEW exact bounded thin-player case proves private grouped/dependent retention, budgeted fair scans, physical game-time work, saved pauses/continuation, replay and isolation. No host retirement decisions or stock mutations.

## Design and API

Modify Standard/PlanHistoryPolicies.cs, PlanHistoryModule.cs and WorkPlanHistoryPolicy.cs. Add one shared immutable declaration and internal guarded facts reader; views remain detached. Keep original chronology, dependencies, retention callbacks, whole-cohort retirement and native IDs. No copied history or pin ledger.

Existing public PlanHistoryPolicyProof delegates to the same declaration. Migrate its JSON fields and the one historical authoring edit referencing age. Add direct public-only Def content, explicitly master-listed JSON, diagnostic Initialize and one thin-player case. Build identity changes; save154/replay1/current view and state schemas remain unchanged unless evidence demands otherwise. Sole GameClock unchanged.

## Errors and compatibility

Missing duration/retention/budget, malformed or negative values, nonpositive budget, unrepresentable scenario age, unused invalid policy, unknown/wrong-kind/null selection fail explicitly before Initialize. Unselected module cannot enable default pruning. Dependency/reference/live/minimum/age blockers remain original typed diagnostics. Development only; no legacy reader or compatibility fields.

## Verification and execution order

1. Shared declarations, owned aliases and guarded facts; source review.
2. Independent JSON proof and ONE NEW bounded public player case. Build as needed; repair/rerun only that same new case.
3. Actual final build/runtime, freeze, tracker and source/hash/link reconciliation. Original acceptance text/checkboxes preserved.
4. Broad/family/historical/Oil/Tycoon/platform and full-phase gates remain open without renewed authorization.

## Risks and limits

Arbitrary provider mutation/fault/reentry, all external retention graphs/cycles, recurring-template closure breadth, complete productive/resource/accounting gates, lockstep/stress/performance/soak/full acceptance need separate evidence. Focused success does not close full P4/P9/P1–P12.

## Out of scope

OilFieldDays, optional business/Tycoon, mandatory income/budget, Draw/adapters/platforms, host gameplay, parallel scheduler/clock/history, instant completion or resource refunds.

