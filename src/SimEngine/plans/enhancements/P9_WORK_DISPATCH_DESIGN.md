# P9-B2 — Deterministic physical work dispatch

Follow-on [B4d authored ordering](P9_DISPATCH_ORDERING_DESIGN.md) now makes age-versus-priority an
explicit required policy setting through the same rank owner. Industrial profiles use priority-first;
fair profiles explicitly use starvation-first. This B2 report remains historical, not rerun on2.297.0.

Status: historical focused gate, 2026-10-05: **116 checks / two exact named suites**, zero failures/aborts.
Build2.54.0/save28/work13/work-view13; logistics-view4 unchanged.
[Evidence](../../docs/migration/P9_WORK_DISPATCH_VERIFICATION.md).
Parent: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Follow-on [B4a starvation dispatch](P9_STARVATION_DISPATCH_DESIGN.md) now adds explicitly selected
JSON thresholds and oldest eligible starvation cohorts to the common pass. B2 remains historical
native-queue evidence, not a fresh gate for current source; broader preemption/full gates remain open.

## Problem / acceptance

WorkSystem.Assign sorts Job.Priority then Job.Id, with no emergency or explicit age/cost facts.
Order priority is also copied onto node jobs, creating two settings for one responsibility.
Replace that path in the existing WorkSystem, not another queue store or scheduler.

1. IWorkDispatchQueue exposes immutable current WorkDispatchOffer facts through the existing owner.
   WorkView privately projects the same queue. Offers never authorize retained assignment.
2. Ready work sorts by actual valid incident binding, current parent order priority, original job
   creation tick (oldest first), selected eligible machine's provider-calculated travel cost, then
   stable target/node/job IDs. Blocked/pending/invalid work remains visible with typed reasons but
   does not outrank ready work or claim machines. Retry deadlines are explicit pending facts.
3. Each successful assignment rebuilds the remaining queue at the current checkpoint, so contention
   and changed best-machine routes are not decided from stale pre-assignment costs or input facts.
   Each job is attempted at most once per pass; existing fallback scheduling/Dirty remains sole timing.
4. JSON ActivityDef priority and player commands allow exact nonnegative integer priorities without
   an arbitrary 100 ceiling. Order.Priority is sole node-job priority; Job.Priority is standalone only.
   Existing authored role/requirement/incident contracts supply all other scheduling intent. There is
   no invented scheduling profile, default emergency, age threshold or overflow-prone combined score.
5. Original job age survives physical interruption/retry, save/load/hash. Future/negative creation
   timestamps and redundant node-job priorities refuse checkpoints. Save/work/view identities migrate
   together; no compatibility reader. Machine preference remains a separate candidate ranking rule.
6. Prove priority changes, incident eligibility, age/cost/stable ties, live contention, pure private
   diagnostics, bounded physical progress, cancellation/retry and saved twins in two exact suites.

## API / files / errors

Add Standard/WorkScheduling.cs as another source file of the same partial WorkSystem class, owning
pure queue construction/comparison only. Extend WorkContracts with IWorkDispatchQueue and detached
WorkDispatchOffer; WorkModule binds the existing WorkSystem alias and advances WorkView schema.
Work.cs removes the old priority/ID sort and duplicate order-job priority codec; StandardCommands
removes copied priority updates and arbitrary ceilings. WorkView adds CopyDispatchQueue.
Missing targets/invalid response bindings yield invalid offers; deferred retry yields pending.
No current worker yields the existing selector's exact reason. Existing native acquisition remains
authoritative; a preview is re-evaluated immediately before the actual claim.

Sequence: contracts/queue → assignment migration → sole priority/codec validation → private view →
explicit master-listed public proof content/tests → build and two exact suites → evidence/docs/tracker.
No Draw/platform/host/demo/Oil/Tycoon changes. No old JSON or save translation.

- [x] Shared pure private queue, exact deterministic ordering and contention recomputation.
- [x] Sole priority/age authority and command/JSON/checkpoint admission.
- [x] Real execution/interruption/retry/incident/cancel and saved continuation proof.
- [ ] Company alert overrides/preemption, cross-demand haul scheduling, starvation promotion and
  complete fairness/replay/lockstep/stress/allocation gates. These remain separate P9 work.

Age ordering prevents newer equal-priority work overtaking older work; it does not guarantee an
unbounded low-priority job beats an endless higher-priority stream. No starvation guarantee is claimed.
Already acquired hauls have actual carrier commitments and are not reopened as unreserved offers.

Follow-up C1 [open demand ownership](P9_DEMAND_OWNERS_DESIGN.md) is focused-verified at
build2.55.0/save29/logistics2/view5, **108 / two exact suites**. It opens requester provenance and
retains age in the same table without changing this work queue or globally sorting haul intent.
B2's 116-check report above remains historical, not rerun in C1. The unified-haul gate stays open.
