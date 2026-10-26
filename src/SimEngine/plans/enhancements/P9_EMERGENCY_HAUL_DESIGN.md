# P9-C3b — Emergency supply provenance and physical eligibility

Status: C3b focused-verified; broader P9 open. Parent: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Current [C3c](P9_SHARED_MACHINE_DISPATCH_DESIGN.md) moves actual standing-haul arbitration into
WorkSystem's shared pass without changing C3b source/job/receipt ownership. Build2.59.0/save32/
work14/work-view15/logistics4/logistics-view8 has 130/two exact-suite scoped checks. The C3b counts
below are historical and were not rerun in C3c.
Build2.58.0/save32/work14/work-view14/logistics-view8: **82 checks / two exact suites** (45 admission;
37 physical), zero warnings/errors/failures/aborts. Logistics4, policy components1 and flow4 unchanged.
No broad/full run; preceding gates were not rerun. See
[evidence](../../docs/migration/P9_EMERGENCY_HAUL_VERIFICATION.md).

## Problem and outcomes

The ordinary logistics queue cannot distinguish incident supply from a high-priority stock request.
Haul acquisition always passes emergency=false and assigned haul jobs lose their requester identity.
Extend the existing source-owner contract, queue and physical job rather than add an emergency queue.

1. LogisticsDemandSourceCheck includes an explicit Emergency fact only for Active source checks.
   WorkBoard derives it from the current valid bound response episode, never priority or plan name.
   Ordinary stock/production sources explicitly report false; engine-side mod owners may report
   their own current JSON-authored classification through the same declared contract.
2. ILogisticsDemandSources aliases LogisticsService's current declared-owner inspection. The same
   source check serves requests, private views, ranking, acquisition and assigned physical eligibility.
3. Ready emergency offers precede ready ordinary demand/routes; contribution selection likewise uses
   emergency before priority/age/identity. Disabled/quiet/expired source intent cannot promote a group.
   Actual carriers must satisfy emergency_eligible in shared CheckAssignment before any promises.
4. Job.HaulDemand persists optional original requester identity through the existing work codec only.
   No copied emergency bool, incident episode, owner registry or intent table is persisted on a haul.
   Current emergency status derives from that owner each physical update/private view. Source retirement
   or episode replacement removes urgency, but never cancels cargo, claims or physical delivery.
5. Restrictive machine eligibility edits pause current incident supply with actual cargo/receipts and
   a typed blocker. Re-enable, requester retirement and save/load use the same physical job path.

## Files, contracts and errors

Extend LogisticsDemandContracts/LogisticsDemands, WorkBoard.CheckSource/Job/ValidateHaul/Sync,
WorkSystem.CheckJob, LogisticsScheduling/Preview/Acquire, module service aliases and private views.
Migrate work/save/view layouts together with no old reader. Logistics intent codec remains unchanged.
Malformed classification, undeclared/wrong owner or cross-company provenance refuses explicitly;
expired provenance remains lawful physical history. Non-haul, route and cargo-return jobs cannot carry
requester provenance. Validate after all services restore, not against partially restored owner state.

## Proof and sequence

1. Author this stage and extend the actual contracts/owners/sole codecs.
2. Add two exact suites: pure/private emergency ranking and shared eligibility; then JSON mod and
   real incident supply, loaded pause/resume/retirement/episode changes, saved continuation/isolation.
3. Bounded physical waits provide actual job/state/blocker snapshots, never host matching/progression.
4. Record real build/test results and update master/README/AGENTS/P9/P4 together.

- [x] Current owner classification drives queue/acquisition without implicit emergency priority.
- [x] Real incidents and mod requests use actual eligible physical workers and native receipts.
- [x] Loaded pause/retirement/replaced episode/save-load preserve truthful physical commitments.
- [x] Focused build/two suites recorded; final scoped source checks accompany evidence. No broad/full run.

Company alert profiles, starvation promotion, preemption/global work-versus-haul arbitration,
evacuation/return policy ranking, fleet/service/fuel, timed docks, persistent plans/templates,
provider fault matrices/replay/stress/performance and full framework acceptance remain open.
No Oil/Tycoon/host/demo/platform/Draw or legacy compatibility work.
