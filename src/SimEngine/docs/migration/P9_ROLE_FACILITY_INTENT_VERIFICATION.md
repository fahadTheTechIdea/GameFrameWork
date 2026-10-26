# P9-B4i/P12-D14 — Role-aware facility intent: focused evidence

[Stage](../../plans/enhancements/P9_ROLE_FACILITY_INTENT_DESIGN.md).
[Master](../../plans/enhancements/MASTER_TRACKER.md).
[Self-excluded manifest](P9_ROLE_FACILITY_INTENT_SOURCE_MANIFEST.json).

## Outcome and authoritative owners
Original WorkBoard.ReadFacilities evaluates every ready actual source-worker role before
applying the public facility policy. Same-facility offers sort by rank/travel/ordinal role;
actual facilities retain rank/travel/stable entity order. Exactly one detached row per dock.

Original ActivityWorkerSelection now owns exact role or deliberate unrestricted intent along
with selected worker. Its canonical parser/identity/admission/embedded Sync are the sole codec.
Explicit worker JSON requires role:null or a valid declared node role; missing, undeclared or
malformed roles cannot silently become unbound. None is deliberate unrestricted intent, not
a compatibility shim. Only the facility probe node gets a role; other bound nodes stay unrestricted.
The target-intent provider must preserve the exact worker/role/target identity.

Original native candidates filter exact roles. Fresh acquisition rechecks the selection and
commits Job.Role WITH the worker assignment. Continuation and checkpoint validation retain it;
Logistics passes its already selected haul role to this original commit. Immutable RequestedRole
distinguishes intent from actual SelectedRole. No second selector, state owner, claim ledger,
clock, rendering, host gameplay or business logic.

Build2.302.0/save144/replay1/board44/work-view86/incidents-view8. Breaking current codec only.
Pre-slice actual board schema was43; older historical tests expecting28 were already stale.

## Authored extension proof and thin driver
RoleFacilityPolicyDef extends public ActivityFacilityPolicyDef, resolves explicit unique role/rank
rows and excludes undeclared offers with null. It owns immutable preferences only.
Embedded master-listed role-facilities.json authors profiles, dual-role physical service, signal
and DAG through the existing module declaration/factory seams. Document-text fixture injection
exercises malformed normal authoring, not duplicate module definitions or runtime mutation.
ONE new player driver uses snapshots/views/commands/Game.Update/save/replay only, no Game.World,
stock mutations, scheduling, forced progress or repairs.

167 older source/test build-identity paths migrated compile-only. Two authored binding callers add
explicit role:null; one old internal diagnostic Assign caller supplies its original actual role.
Older save/service numeric schema assertions are NOT revalidated by compilation or this case.
They remain a pending P6 baseline-source audit; no broad runtime was authorized.

## All executable verification attempts
All FOUR builds used `dotnet build Beep.SimEngine.slnx --no-restore`.

1. Failed2.88s: CS0103 missing ContractIdentity import in NEW proof.
2. Failed1.24s: CS0122 ContractIdentity is internal; remove private dependency.
3. Failed3.65s: OLD diagnostic Assign caller needed its actual role.
4. Clean3.74s: **0 warnings,0 errors**.

Exactly ONE attempt of ONE distinct NEW bounded case:
`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Role-aware facilities: selected intent, physical repair and saved policy changes"`
**135 passed /0 failed /0 aborted**, **1537 ms**, first runtime attempt.
No runtime repair/rerun/older/family/full/Oil/Tycoon/platform/stale runtime.
Executable source/proof/tests/JSON frozen after green at **2026-10-08 13:46:23 UTC**.
Only docs/manifests/read-only inspection afterward.

## Scope actually proven
- Ordinal first haul role is excluded or ranked lower; inspect at the SAME real facility is selected.
  Haul-preferred and equal-rank profiles retain deterministic haul selection. Roles do not fabricate
  workers, docks or extra endpoint rows. Private immutable previews acquire nothing.
- Explicit missing/unknown/malformed/wrong-type role refuses preview. Missing/unknown/negative/
  duplicate public role-rank authoring refuses composition before Initialize.
- Near stock2 physically cannot supply tools; far stock32 supplies real service. Acquisition leaves
  stock intact; physical approach/work spends three spares32→29. Partial work leaves condition4.
- Pending policy command, acquired/working/paused/completed intent and progress save exactly.
  Future profile edits never duplicate or relabel active inspect work. Removing inspect from the
  worker policy pauses the SAME job, never substitutes available haul. Sole clock pause earns no work.
- Normal cancellation preserves spent stock and damage. New future posting alone selects haul,
  consumes another three spares and physically repairs. Far final stock26(cancel) or29(normal);
  near stock2 untouched; condition10 only after actual work/departure.
- Retained node facts remain detached; private saved twins, interleaved untouched session and
  canonical player replay agree. Waits<=1200 normal updates with actual clock/job/blocker diagnostics.
  Explicit8MiB/5000 replay bounds are test limits, not an engine world-size cap.

## Review, source audit and open gates
Development-lifecycle/SOLID review keeps single selection/facility/native admission/stock/effect/
clock owners. No compatibility overload, old reader or duplicate model.
Self-excluded normalized SHA manifest compares source ownership/JSON/clock and local docs links.
Those source checks and this narrow runtime are NOT full regression or broad completion proof.
Wider provider purity, multi-node/dock/interruption matrices, full resource/environment accounting,
CPU/fairness/stress/fuzz/soak/lockstep, industrial acceptance and full P1-P12 remain OPEN.
Full goal ACTIVE. No staging/commit/push until FULL goal AND plan completion and verification.

