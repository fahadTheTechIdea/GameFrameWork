# P4-G2i: Named activity resources and actual-endpoint supply

Status: native implementation/focused verification complete; full P4/P8/P9/P1-P12 remain OPEN.
Core2.321.0/save153/WorkBoard48/WorkSystem42/work-view91. Final101 checks in3147ms;
six builds, final clean6.09s. [Evidence](../../docs/migration/P4_SCOPED_ACTIVITY_RESOURCES_VERIFICATION.md).

## 0. Problem

ResourceWorkRequirements.CopySources/CheckSource assume entity-local outlets. WorkStart.CopySupply
and WorkSystem.RefreshSupply flatten chosen DAG inputs into one local Bill despite native logistics
already accepting exact port/endpoint envelopes. Scoped stores cannot run generic activities.

## 1. Goals

1. Native target/worker requirements author explicit port:null|string in normal master-listed JSON.
2. Retain actual selected source identities for startup, held and running bindings after selection edits.
3. Aggregate target supply by actual endpoint; aliases combine and distinct stores remain independent.
4. Execute physical supply and work through existing WorkBoard/jobs/transactions/logistics owners.
5. Preserve ordinary previews/private facts/cancellation/save/hash/replay through that same graph.

## 2. Design

ResourceWorkRequirementDef owns strict native bill/rate authoring and abstract source contracts only.
EntityResourceRequirementDef owns the single native entity/named-port source algorithm. Target/Worker
and independent role-aware providers reuse it. Custom connected/dual sources retain their explicit
source algorithms, without unused local Source overrides or compatibility defaults.

WorkRequirementContext.CommittedSource is a detached current-occurrence fact from the existing node
resource choice, not saved state. Its internal copy preserves progress/rate/role facts; callbacks are
not recomputed. EvaluateResource passes it to native source offer/check calls. Named reads reuse
ResourcePortContracts; explicit originals never select another store.

IWorkSupplyRequirement exposes the declared target Port. CopySupply uses existing resource choices
and native running input quantities, resolves that actual target store and aggregates one immutable
LogisticsSupplyRequest per actual endpoint. IWorkSupplyRequests replaces local Bill with the existing
typed envelope. Generated local capacity projections include only explicit local requirements.

## 3. API

One new intermediate authoring base, one explicit port property and one current-context source fact.
No additional registry, scheduler, graph, promise ledger, inventory, demand owner or clock. Strict native authoring and
WorkBoard/WorkSystem/view protocol identities advance; no old reader or implicit local default.

## 4. Files

Standard/ResourceWorkRequirements.cs, WorkRequirements.cs, WorkResources.cs, WorkStart.cs, Work.cs,
WorkContracts.cs, Logistics.cs, WorkModule.cs; existing custom proof providers and native JSON/inline
declarations migrate directly. One normal master-listed scoped activity scenario and thin player case.

## 5. Errors

Missing explicit JSON port rejects authoring. Unknown named references reject selected composition.
Unavailable actual candidate ports report typed missing-source; changed selection cannot redirect live
holds/running bindings. Foreign sources remain filtered before private stock observations. Provider
purity/commit violations fault without compensation. No remote provider silently becomes a delivery.

## 6. Compatibility

None. Migrate existing content and provider subclasses directly. Save153 remains unchanged because
the existing resource/demand layouts already contain actual identities; core2.321.0 and WorkBoard48,
WorkSystem42/work-view91 identify changed contracts/meaning. Section ownership remains unchanged.

## 7. Verification

Build for compilation; only ONE NEW exact bounded case. Normal JSON parallel timed consume/running
nodes and dependent staffed held-tool work use real supply. Test aliases, independent stores, original
sources after selection changes, complete requirements, physical work, saved twins, cancellation,
private facts and replay. Wait bounds1000 Updates; process bound15000ms. Existing cases compile only.

## 8. Risks

Duplicate aliases must aggregate once per occurrence, not duplicate a physical endpoint owner.
Running requests must use actual next-input native quantization, not total budget or invented rate.
Context source pinning cannot rerun rate callbacks or mutate previous evaluation snapshots.

## 9. Open scope

Broad external/provider/removed-store/purity-fault/corruption/team/dock/area/stress/global-accounting
matrices, scoped recovery/same-site automatic matching, full regression and P1-P12 completion remain
open. No Oil/Tycoon/platform/Draw work. Publication waits for FULL goal AND plan completed/verified.

## 10. Execution

1. Extend existing source contracts/contexts and direct native content authoring.
2. Replace local supply payload/aggregation with the existing typed endpoint envelope.
3. Migrate normal JSON and external proof providers; add one normal scoped workflow/player case.
4. Build and run only the new bounded case, repair it as required, then record actual tracker evidence.
