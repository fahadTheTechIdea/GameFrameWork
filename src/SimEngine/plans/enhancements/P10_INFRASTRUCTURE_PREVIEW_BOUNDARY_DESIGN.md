# P10-PV1: Infrastructure preview boundary — Design Document

## 0. Problem Statement

InfrastructureView.PreviewRemoval calls topology without first rejecting a faulted session or undisclosed line content. SuggestLinkPath computes candidates and calls routing directly, with no complete provider/space/both-target observation. SuggestLinePath passes the caller's destination directly to a provider. A failed callback can expose a partial route; unsupported non-tile targets can dereference absent tile presence.

## 1. Goals

1. All infrastructure routing/removal previews reject first fault before missing/private/unsupported early returns.
2. Use actual line-content and entity visibility, without inventing a second authorization model.
3. Observe selected provider, line, space and actual link targets through existing observation owners.
4. Publish a route only after a complete healthy Found result; refusal/fault leaves caller output empty.
5. Preserve native route/topology/work/resource/time ownership, physical execution and saved continuation.

## 2. Design

Revise existing InfrastructureView route/removal methods. A private route observation helper stages a local list, wraps the actual routing contract and selected space, validates result shape, then copies only Found output. Link candidates remain engine-owned and use placed tile presences in the actual selected map; missing/foreign/hidden/non-tile targets return InvalidRequest. Observe both real target entities and the authored line around the whole candidate search. Removal selects visible actual content and observes the existing topology owner and line.

NetworkService.ReadDemand wraps its remaining Carries selector with the existing NetworkDefinitionContracts read guard. No new router, evaluator, state book, clock or command path is introduced.

## 3. API surface

Existing signatures unchanged. Unknown/hidden removal definition returns UnknownDef. Existing path refusal statuses remain. No schema change; build identity advances.

## 4. Files to change

Existing Views/InfrastructureView.cs and Standard/NetworkDiagnostics.cs, build identity/protocol; one thin public-view/physical/fault proof driver and registration/profile entry; existing proof factory made internal for reuse; this stage, evidence, master and audit.

## 5. Error handling matrix

Faulted session: throw before early return. Hidden/missing definition or target: typed refusal, no provider access or stale output. Unsupported spatial presence: InvalidRequest. Provider failure/mutation/malformed route: original first fault, no healthy route publication, save/hash refused.

## 6. Backward compatibility

Development only. No fallback, alias, compatibility model or parallel implementation.

## 7. Verification

Completed focused build, one new bounded exact case: native route/removal purity, point unsupported targets, physical tile/point construction/split/reconnect saved twins and replay, retained foreign/missing previews after real provider fault. No family/full runtime; one final full regression remains deferred until implementation finishes.

## 8. Risks

Output must remain empty even if the provider wrote local values before throwing. Both link target states and the actual map need observation. Presence/visibility decisions must stay in engine contracts; a host must not reproduce routing arithmetic.

## 9. Out of scope

Full original provider/privacy/topology/lockstep/stress/performance acceptance, Tycoon business rules, Oil/platform/Draw/clock changes and Git writes.

## 10. Sub-task execution order

1. Protect original route/removal previews and actual demand selector.
2. Add public-player/view proofs without stock/map/progress writers.
3. Focused compilation and exact-case runtime; retain attempts.
4. Refresh tracker/evidence/source freeze; leave unproven original gates open.

## Scoped stage outcome

FINAL 543 passed, zero failures/aborts in 7542 ms. Four actual tile/point minute/half physical workflows, route/removal purity and eight authored callback-fault scenarios verified. First driver compilation and first runtime fixture failure retained; two subsequent clean compiles. Full original provider/privacy/topology/durability/stress/global acceptance remains open. See [verification evidence](../../docs/migration/P10_INFRASTRUCTURE_PREVIEW_BOUNDARY_VERIFICATION.md).
