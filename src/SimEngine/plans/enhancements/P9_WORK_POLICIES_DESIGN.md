# P9-B1 — Authored worker work policy and player controls

Status: focused-verified, 2026-10-05: **147 checks / two exact named suites**, zero failures/aborts.
Build2.53.0/save27/work12/work-view12/logistics-view4; work_policy component codec1.
[Evidence](../../docs/migration/P9_WORK_POLICIES_VERIFICATION.md).
Parents: P9_PLANS_POLICIES_LOGISTICS_DESIGN and P4_TARGET_REGIONS_DESIGN.
Approved policy scope; extend WorkSystem/LogisticsService, never another scheduler or reservation owner.

Follow-up [B2 dispatch](P9_WORK_DISPATCH_DESIGN.md) now orders ready physical work through the same
WorkSystem by bound response/current priority/original age/provider cost/stable identity, with private
offers and recalculation after actual claims. Build2.54.0/save28/work13/work-view13 passes **116 / two
exact suites**; this document's 147-check B1 gate is historical, not rerun there. Company alerts,
starvation promotion/preemption, cross-demand haul ordering and remaining full P9 gates stay open.

## Problem and acceptance

Worker eligibility currently reads role/held/job/operation availability only. Immutable target-region
requirements do not express a player's per-machine policy. Add an optional explicit policy capability;
absence means no authored policy constraint, not a fabricated default profile or compatibility path.

1. Master-listed `work_policies` profiles explicitly author enabled, priority, allowed_roles, area
   (explicit null means no region constraint), interruptible and emergency_eligible. A `work_policy`
   capability references a profile. Roles/regions use the existing catalogs; no live region copy.
   Profiles explicitly bind `states.disabled`; Entity.Update alone commits that authored state.
   An explicit empty allowed_roles list permits no work; absence of the capability adds no constraint.
2. IWorkPolicy owns immutable current settings, pure setting admission and configuration. Its one
   component Sync saves/hashes those settings; worker.Job, WorkBoard jobs and actual receipts/cargo
   stay with current owners. Commands and private views use the same contract checks.
3. The existing IWorkerDispatch gains typed destination-aware assignment admission, replacing the
   destination-blind CanAssign path. Work and hauling share enabled/role/area gates. Check all proposed
   physical stops before acquiring any receipt; ongoing work checks only remaining stops.
4. Ready worker candidates compare authored worker preference before travel cost and stable ties;
   logistics uses the same preference semantics. This is machine preference, not a replacement for
   existing player order priorities or a completed company emergency/starvation scheduler.
5. Bound incident-response orders use emergency_eligible, never infer emergencies from a high priority
   or a manual ordinary order. No hidden emergency bypass of enabled/role/area/physical input rules.
6. Restrictive policy edits pause existing jobs, preserve actual ownership/cargo and stop motion.
   Resuming uses current reach/load/work/unload rules. No cancellation/refund/teleport/phantom load.
   Manual commandeering checks interruptible through the same engine dispatch boundary.
7. Existing job records persist a typed blocker and expose immutable private facts, including hauls.
   Save/work/view identities migrate together; no old reader. Corrupt settings refuse restore.
8. Home return obeys enabled/area intent; no automatic home travel bypass. This is destination
   membership, not constrained intermediate-path geometry or editable spatial regions.

## Files / sequence

Standard/WorkPolicies.cs and Commands/WorkPolicyCommands.cs own settings/JSON/codec and player intent;
WorkModule declares/registers them. WorkContracts/Work.cs extend the existing selector/execution;
Logistics.cs extends its existing complete carrier acquisition. WorkView projects settings/jobs.
Existing Tile/Point region providers remain geometry authority. Public-only extension proof reuses
contracts without scheduler edits. No mandatory policy module or platform/host gameplay.

JSON/content admission → immutable settings/contract/codec → destination-aware shared dispatch →
ongoing pause and manual interruption → command/private views → bounded physical/save proof →
build and exactly two directly affected named suites → evidence/tracker/README/AGENTS.

- [x] Strict profile/reference/role/region admission and immutable pure settings/views.
- [x] Command/preview parity, privacy and per-machine priority/role/destination selection.
- [x] Work/haul pause with preserved ownership/cargo, real resume and cancellation/interrupt behavior.
- [x] Bound-response eligibility, home return, save/load/hash and non-tile provider proof.
- [ ] Editable/versioned areas, company alert overrides, starvation and all fleet/process policies.
- [ ] Wider replay/lockstep/stress/performance/full-stage gates, only when explicitly authorized.

## Authored profile

The file is explicitly listed in the game's existing master manifest. IDs reference existing role,
region and state definitions; executable behavior stays in registered engine contracts.

```json
{
  "work_policies": [{
    "id": "fleet.field",
    "enabled": true,
    "priority": 4,
    "allowed_roles": ["haul", "inspect"],
    "area": "field.allowed",
    "interruptible": true,
    "emergency_eligible": true,
    "states": {"disabled": "blocked"}
  }]
}
```

The worker's `work_policy` component references `profile: "fleet.field"`. Player commands change
immutable current settings, not the content profile or region geometry. Priority is an exact
nonnegative integer machine preference, not a capped/combined score. B2 now orders physical jobs by
response/current priority/original age/cost/identity. Company alert overrides, starvation promotion
and automatic preemption remain unimplemented.
Region membership checks destinations, not every intermediate route point. Assigned role identity
does not silently change when a policy changes. Only an actual bound incident response requires
emergency eligibility; ordinary work never becomes an emergency merely because its priority is high.
