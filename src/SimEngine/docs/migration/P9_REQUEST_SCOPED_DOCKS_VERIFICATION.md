# P9-L2 request-scoped dock slots — focused evidence

Follow-up: the then-open full Planet X control scenario subsequently passed128/0
in [P6-I2](P6_INDEPENDENT_CONTROL_INTEGRATION_VERIFICATION.md). Remaining-scope
statements below describe the P9-L2 checkpoint, not that later result.

Date: 2026-10-15. Engine2.605.0/save159/replay1. This is an incremental engine
capability, not completion of the full industrial-colony plan or original P6 gate.

## Implementation and ownership

`Standard/DockContracts.cs` extends the original immutable WorkDockSlot with one
optional resolved work-role or resource restriction. `Matches(WorkDockRequest)` is
the sole additional slot predicate. Unrestricted slots still obey the endpoint's
authored global admission. A worker able to haul and perform work does not make
those two requests interchangeable.

The shared WorkDockSlotDeclaration reads identity/kind/work_role/resource once.
Native TileWorkDockDef and external PointDockDef retain only their own geometry
reading. WorkDockRules rejects unknown, blank, mutually incompatible or foreign
policy references and missing compatible service/departure before gameplay.
The old constructor was replaced, not retained as a compatibility path.

Original WorkBoard selection, journey continuation, queue eligibility/promotion,
approach, reach, departure and assigned-policy checks use the same predicate.
Review found that original checkpoint validation only checked slot identity,
phase and occupancy. It now also checks the actual work/resource request,
including a retained recovery bay. Recovery arrival cannot clear an incompatible
exit. Existing jobs, claims, stock receipts, geometry, motion and clock remain
the sole owners; no new dispatcher, reservation book or state model.

Static metadata participates in the original content manifest. No live payload,
native save or replay schema was added; original saved slot IDs remain authoritative.

## Focused proof

`haul-competition.json` remains explicitly master-listed. Its closer work-only
source bay must not receive loading, even though the carrier has that same role.
After both physical loads, actual departures, distinct home arrivals and released
books, an ordinary staffed order must use that work-only bay rather than the
closer resource bay. Existing update bounds, every-update finite-stock conservation,
no crossing and saved-twin checks remain; no teleport or larger timeout.

DockRequestFilterTests covers exact resolved identities, request-kind distinction,
default invalid bindings, seven malformed/unsupported authored variants and valid
checkpoints. Explicitly labelled test-only corruption changes original job slot
fields, proving live checkpoint refusal and the restore-mode validator's refusal
in both directions (haul to work-only; work to resource-only). It does not claim
an independently corrupted serialized-byte loading/fuzz campaign.

Native construction remains a normal player-command physical supply/build test.
It is not a native-filter permutation/queue/recovery stress proof. Both native and
external readers reuse the same inspected declaration and validation.

## All attempts

Earlier continuation notes retain these terminal observations (not rerun to
reconstruct timing):

- Original Planet X control literal on2.604: 35 passed/1 failed,18,409ms;
  PID26564 exit1/18,610ms. At update2005, detection still assigned, finite deposit12
  and reserve alloy2 unchanged. One-dimensional carrier occupancy blocked the
  extractor. This full scenario remains failed/open, not replaced by smaller tests.
- First slot implementation build clean16.96s; preliminary acquisition589/0,
  PID29084 exit0/1784ms (case1590ms), before inverse staffed continuation.
- Next build failed27.79s: CS1503 for integer WorkerRoleId probe. Corrected to a
  stable string role identity. Next build clean16.25s.
- Four-case observation output was lost at context truncation. Their outcomes are
  not counted. On resumption a live process inventory found no matching engine
  process. Only the four exact cases were rerun to recover observable evidence.

Recovered terminal observations before final checkpoint hardening:

| Literal case | Passed/failed | Case ms | PID / exit / bounded elapsed ms |
|---|---:|---:|---|
| Dock request filters: single authored predicate and early invalid binding refusal |13/0|353|25824 /0/582|
| Atomic hauling: common quantities, all-participant refusal and exclusive carrier acquisition |714/0|1760|14460 /0/2073|
| Independent tile construction: physical material supply and native project completion |121/0|1186|32140 /0/1412|
| Framework independence: compiled external modules use public engine contracts only |10/0|131|14756 /0/295|

Compiled audit observed ExtensionProof6001 method bodies/30114 member references,
QuickGame.Module5/15, zero violations. This audit precedes the final core validator
change; external module source did not change afterward. It is not a mod sandbox
or a full-engine safety audit.

After checkpoint review:

- Targeted build clean11.67s, terminal exit0/session77332 before runtime.
- Expanded contract literal21/2,479ms; PID22296 exit1/659ms. Both failures expected
  CorruptStateException for saving invalid *live* state. Existing save validation
  correctly uses InvalidOperationException; restoring uses CorruptStateException.
  Corrected test expectations only; no production exception-policy change.
- Targeted build clean6.14s, exit0, zero warnings/errors before final runtime.

Final changed-runtime evidence:

| Literal case | Passed/failed | Case ms | PID / exit / bounded elapsed ms |
|---|---:|---:|---|
| Dock request filters: single authored predicate and early invalid binding refusal |23/0|448|31960 /0/606|
| Atomic hauling: common quantities, all-participant refusal and exclusive carrier acquisition |714/0|1330|27372 /0/1479|
| Independent tile construction: physical material supply and native project completion |121/0|940|18492 /0/1090|

Three final literals858 successful checks, no truncation/abort/timeout. All commands
used the built test DLL with `--bounded-exact`,60000ms timeout and5000ms termination
timeout. No full/profile/family test run. No executable changes after these finals.

## Remaining requirements

Full Planet X research/control/incident/production/construction integration remains
required. Its one-dimensional topology must be authored feasibly without merging
workers, permitting crossing/overlap, enlarging interaction reach or bypassing
physical work. Dedicated role/resource bays now exist as an engine capability;
that alone does not prove the larger scenario.

Native filtered mixed-queue/recovery/fault permutations, serialized corruption,
broader spatial/congestion/replanning/mod/global/stress requirements remain open.
The original221/195 recorded checked/26open consolidated metadata is historical,
not freshly verified. No original broad acceptance checkbox closed in this slice.
No Oil/Tycoon, host gameplay, platform/rendering/adapters, arbitrary map-size limit,
staging, commit or push. The full goal remains active.
