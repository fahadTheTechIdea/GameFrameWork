# P9-C3g — One haul-candidate cost authority

Status: partial; source-focused verified,2026-10-07. Build2.204.0/save109; owner schemas unchanged.
[Evidence](../../docs/migration/P9_HAUL_CANDIDATE_COST_VERIFICATION.md) /
[source manifest](../../docs/migration/P9_HAUL_CANDIDATE_COST_SOURCE_MANIFEST.json).

## 0. Problem and specification

Scope clarification,2026-10-07: default resource values, budgets and production income belong to
the shared engine-owned business/Tycoon module. Advanced Tycoon rules require explicit activation.
Supplier proofs must use the actual module/contracts, not a new test-owned economy implementation.
See [P4-H1](P4_BUSINESS_DEFAULTS_DESIGN.md). Physical travel cost remains distinct from money value.

Standard/Logistics.cs PreviewSource obtains a ready haul with an actual selected carrier and
TravelCost, but compares sources using another requester-to-source distance. It omits pickup
travel and can choose the farther complete trip. PreviewDestination similarly recomputes an
endpoint-only metric except during dock recovery. Selection must use the existing selected
preview cost; no second cost authority or fabricated ETA belongs in dispatch.

## 1. Goals

- All ready source/supplier/destination candidates compare their own nonnegative TravelCost.
- No successful preview substitutes requester proximity or a default zero when cost is absent.
- Existing readiness, non-interruption preference, export priority and stable endpoint IDs remain.
- Actual acquisition rebuilds current candidates and uses the same selection as immutable diagnostics.
- Normal physical travel, loading, unloading, stock/reserve gates and saved twins remain engine-side.

## 2. Design

LogisticsService remains the sole matching owner. A private selected-candidate cost validator reads
the existing HaulPreview.TravelCost and rejects an impossible successful preview with missing or
negative cost. PreviewSource uses it in both owned-stock and supplier fallback branches;
PreviewDestination uses it for ordinary and continuing/egress recovery. Existing Preview computes
and publishes the cost; no new clock conversion, route search, model, field or cache is created.

## 3. API

No new public API, command, content kind or schema. Existing IHaulExecution/dispatch queue views
and actual source acquisition share the same selected preview. TravelCost is an engine/provider
estimate, not guaranteed arrival time or a promise of congestion-free travel. Conditional duration
continues through the separate existing MotionTravelEstimate contract.

## 4. Files

Existing Standard/Logistics.cs; Kernel/SaveGame.cs current build identity;
existing LogisticsPolicyTests.cs fixture/scenario. Current payloads/schema layouts stay unchanged.
Current caller identity assertions migrate directly; no old loader or compatibility facade.

## 5. Errors

| Condition | Behavior |
|---|---|
| Candidate not ready | Existing typed refusal retained |
| Ready candidate lacks nonnegative selected cost | Explicit internal protocol error; no guessed metric |
| Source stock changes after preview | Actual normal acquisition rebuilds selection |
| Export priority differs | Existing explicit export priority precedes cost |
| Equal priority/cost | Stable actual endpoint identity breaks tie |
| No owned supply | Existing supplier fallback, using its selected haul cost |

## 6. Compatibility

Development-only direct current build change. No payload/format/schema changes or compatibility path.

## 7. Verification

One newly named scenario at the completed slice; compilation build only as needed. Counterexample:
carrier40, customer10, source9 versus25. Requester proximity prefers9; selected complete costs32
versus30 prefer25. Native/public outlet policy, priority, stable ties, changed supply, physical
conservation and saved continuation use existing engine commands/views/update. No previous/full run.

## 8. Risks

Do not redefine TravelCost as duration or silently promise exact arbitrary-provider routing.
This removes a duplicate comparison metric, not all geometric/provider approximation. More precise
prospective-origin/multi-dock cost contracts, congestion and arbitrary spatial providers remain open.

## 9. Exclusions

New spatial/media/congestion solvers, independent cost/time ledgers, Oil/Tycoon/host/demo,
Unity/Godot/Draw, broader supplier/fleet/replay/stress/full acceptance.

## 10. Tasks

- [x] Same selected preview cost wired into owned source, supplier and recovery destination matching.
- [x] Clean build;52 assertions/one new source scenario,0 failures/aborts. Older suites compile only.
- [ ] Separate physical recovery-destination cost counterexample; not covered by source proof.
- [ ] Align supplier proofs with P4-H's one default/Tycoon business module; no duplicate test economy.
- [x] Current evidence and tracker reconciliation; whole plan remains incomplete.
