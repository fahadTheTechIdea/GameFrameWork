# P11-C8 — Private condition-gated plan sites

Status: native condition-gated site slice implemented and focused verified,2026-10-08.
Build2.294.0/save143/controller-view7, persistent layouts unchanged. ONE clean build13.03s0/0;
ONE exact NEW bounded case FIRST64 passed0 failed0 aborted1658ms. Executable inputs frozen.
[Every build/run and scoped evidence](../../docs/migration/P11_CONDITIONAL_PLAN_SITES_VERIFICATION.md).
Full goal/plan active; broader strategy and acceptance gates remain open.

## 0. Problem

Standard/PlanSites.cs AuthoredPlanSitesDef only offers static priority/anchors. PlanPlaybooks.cs
chooses the first physically admissible site regardless of candidate-specific risk/reserve/service
preferences. Step conditions cannot distinguish alternative sites.

## 1. Goals

1. Each authored candidate explicitly selects a canonical distinct conditions array; empty is deliberate.
2. Existing IGameCondition providers evaluate only the actual company's private view through the original
   purity/content/visibility guard, for both decisions and diagnostics.
3. Preserve every candidate and its typed policy blocker in immutable facts; choose only policy-ready
   and physically-ready candidates using original stable priority/ID order.
4. No new scheduler, strategy store, condition authority, clock, money dependency or placement privilege.
5. Existing normal commands, saves/hash/replay and physical projects remain authoritative.

## 2. Design

Revise IPlanSiteSource.Observe(PlayerView,Func<IGameCondition,Code>) in place. Original
PlanSiteContracts supplies guarded GameConditionContracts.Read for the same actual view.
AuthoredPlanSitesDef alone stores authored candidate condition references, resolves/validates them,
and derives candidate Blocker at observation; no executable providers leak through candidate facts.
PlanSiteCandidate requires explicit blocker and validates it. PlaybookSiteSnapshot combines policy
blocker with existing PlanGroupCheck, and Playbook chooses its Ready rather than raw admission.
Validation checks every condition under original EntityObservation scope. Existing provider wrappers
forward the same observer callback. No callback substitutes a condition's result.
Policies affect site intent selection, not active work cancellation or ordinary human placement.

## 3. API

IPlanSiteSource.Observe(PlayerView view,Func<IGameCondition,Code> observe).
PlanSiteCandidate(id,priority,anchor,blocker); Blocker read-only.
PlaybookSiteSnapshot.Blocker policy-first; Ready requires both policy and actual placement readiness.
Build2.294.0/save143/replay1/playbook5/controller-view7. Persistent layout unchanged.

## 4. Files

Modify Standard/PlanSites.cs, PlanPlaybooks.cs, PlayerControllersModule.cs and Kernel/SaveGame.cs.
Migrate prior public wrapper/constructor/build callers compile-only.
Five existing authored site JSON documents gain explicit empty conditions arrays.
One new master-selected policy/site/entity document and thin bounded player case; original proof
module contracts provide signal and clock conditions, not test-owned gameplay.

## 5. Errors

Missing/wrong/duplicate condition IDs, absent arrays and malformed blocker IDs fail explicitly.
Foreign/no-seat/hidden providers retain existing typed visibility semantics.
No qualifying site exposes real blocker, no fabricated fallback site. Provider fault propagates
through existing Update/read-only boundary; no successful checkpoint after partial mutation.

## 6. Compatibility

Development-only breaking API/content redesign. No old overload, loader or implicit empty default.

## 7. Verification

ONE exact NEW bounded case: Player controllers: private conditional sites and physical strategic selection.
Prove differing own private signals, blocked high-priority alternatives, stable equal-priority IDs,
retained immutable blockers, all-blocked wait, ordinary signal edit, normal queued placement,
physical material-consuming saves/replay, explicit no-money mode and malformed content.
Bound waits <=2000 updates with actual clock/plans/site blockers. Record every build/run;
repair only observed same-case failures. Freeze executable inputs after final green.

## 8. Risks

This is conditional site selection, not the full industrial strategy/maintenance/survey profile or
active-project recovery. Global provider purity/CPU, larger lockstep/corruption/performance/Oil/full
P6/P1-P12 gates remain open.

## 9. Out of scope

Tycoon/Oil/platform/renderer/adapters, budgets, instant workers/materials, new clocks/stores.

## 10. Execution

- [x] Inspect native site/condition/playbook/private observation owners.
- [x] Extend existing source/candidate/selection owner and cleanly migrate source/JSON.
- [x] Author one bounded physical/private proof, build/run and freeze.
- [x] Update tracker/guides/evidence/provenance.
Open decisions: none; conditions use existing contract providers and normal player rules.
