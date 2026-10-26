# P10-A6 — authored network demand and physical utility allocation

Status: native focused slice complete on2.142.0/save76/LineService4/infrastructure view5.
Clean build;182 checks/two exact network-solver suites (92 contracts;90 execution), zero final
failures/aborts. [Evidence](../../docs/migration/P10_NETWORK_DEMAND_VERIFICATION.md).
Uses the sole LineService flow/receipt/history owner; this does not complete full P10.

## 0. Problem and acceptance

Networks currently assume a receiving endpoint wants all available capacity. Utilities need actual
consumer demand, while other games may author different policies. Do not hard-wire utility cases
into the scheduler or introduce another request/allocation balance.

1. Every NetworkDef explicitly selects a registered INetworkDemandPolicy/demand_policy JSON reference.
   CapacityNetworkDemandPolicyDef retains explicitly selected capacity filling. UtilityNetworkDemandPolicyDef
   binds a required named consumer and limits pulls to real next demand minus available and inbound
   commitments. Sources still push through normal timed capacity admission. No default/legacy loader.
2. One bounded pure NetworkDemandContext exposes detached leg identity/company/resource/direction,
   actual ready ceiling/quantum and guarded named utility observations. It expires in finally and
   contains no mutable World/entity/endpoint handles. External demand content needs no coordinator case.
3. Demand is evaluated before eligibility/ranking and rechecked immediately before physical transfer.
   Zero/blocked demand earns no credit. Multiple networks sharing a consumer cannot deliver against
   stale demand; inbound physical/logistics reservations count. No virtual balance or free delivery.
4. Existing arbitration, explicit solver and native common-quantum receipts retain ownership. Invalid
   quantities/foreign or mutating observations fault. Known missing/inactive/incompatible consumers
   refuse visibly; no fallback to filling storage. Partial/incompatible quanta wait rather than overfill.
5. Native timed consumers and independent public-contract consumers receive actual finite stock.
   Saved/interleaved/pause/disabled/end-of-work continuation retain the sole game clock and codecs.

## 1. Files and execution order

Standard/NetworkDemands.cs owns the open contract/content policies and temporary pure observation
context. Networks.cs requires/links the explicit policy. InfrastructureModule registers native JSON
sections. NetworkFlow.cs delegates admission twice through the same policy and existing owner; no
utility-specific coordinator switch. All actual current network JSON migrates explicitly in place.

1. Add contracts/native policies and mandatory authored binding.
2. Guard/evaluate demand before existing time/rank capture and again inside ExecuteFlow.
3. Migrate current explicit fixtures; prove independent policies/lifetime/faults and native/public
   utility pulls through exactly the two existing network-solver suites.
4. Clean build then those two exact suites; other suites compile only. Update evidence/tracker/docs.

All four native tasks are complete. Existing INetworkDiagnostics/InfrastructureView.ReadDemand
reuses the same capture/admission helper; existing quantity diagnostics validate private connection,
content and topology first. No parallel diagnostic model or allocation store. Compatible empty
endpoints remain in the participating-owner guards, but earn no time without positive readiness/demand.

## 2. API, errors and compatibility

capacity_network_demands:[{id}], utility_network_demands:[{id,consumer}], networks[].demand_policy
are explicit master-owned definitions. NetworkDemandSnapshot carries nonnegative Maximum and typed
Blocker; blocked quantities are zero. Context ReadUtility(Code,out UtilityDemandSnapshot,out
ResourceQuantitySnapshot) selects the actual named receiving provider and exact same inlet owner.
Returned limits must fit actual ready quantity/common quantum. No rounding-up or implicit consumer.

Known absent/inactive/representation/incompatible demand returns a typed zero allowance. Malformed
or mutation/expired observations throw and fault normal Update, never checkpoint healthy. No persistent
layout is added; save76/LineService4 unchanged, exact build/content identity changes.

## 3. Remaining scope

Follow-on [A9 utility demand groups](P10_UTILITY_DEMAND_GROUPS_DESIGN.md) replaces the singular
binding with mandatory explicit consumers arrays and subtracts shared stock/promises once. Its current
242/two exact network gate includes this stage's older checks; the historical182 count is not additive.

Follow-on [A7 utility activity requirements](P10_UTILITY_WORK_REQUIREMENTS_DESIGN.md) now adds
target/worker readiness leaves through the existing evaluator. Its291/two requirement-expression
gate does not rerun this stage's182/two network-solver gate.

This completes demand-aware resource-network delivery, not electrical/thermal/pressure physics,
aggregate network bandwidth, global utility priority editing, graph-independent provider breadth,
loss/disposal policies or global rollback/replay/stress/allocations/full P10/P1-P12 acceptance.
No host/demo/Oil/Tycoon/platform/Draw changes, additional clock, legacy or parallel authority.
