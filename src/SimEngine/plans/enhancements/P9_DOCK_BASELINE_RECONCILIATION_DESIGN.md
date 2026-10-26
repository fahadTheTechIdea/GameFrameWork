# P9-DB1 — Reconcile the original dock acceptance cases

Status: implemented/scoped-verified2.660.0/save160/replay1: original admission100/0,
physical66/0, affected native checkpoint377/0. [All attempts/identities/limits](../../docs/migration/P9_DOCK_BASELINE_RECONCILIATION_VERIFICATION.md).

Implementation finding: first fixture reconciliation gives admission71/9 and
physical60/1. Source World.Sync confirms most native codec invariant failures
throw without latching World.Fault; a later Update can replan corrupted live
bindings. HashBreakdown also omits the normal pending-event checkpoint gate.
The implementation extends the ORIGINAL World checkpoint owner: normal readiness
refusals remain nonterminal, but failures AFTER codec encoding starts latch the
first fault and return no healthy save/hash/breakdown. Gate faulted hashes before
Settle, reuse original write preflight, preserve decode behavior/first exceptions.
No faulted-state repair. Build2.660, no schema/compatibility change.
The existing MotionCheckpointTests negative geometry injection must use its own
saved clone rather than restore a world after a failed whole checkpoint. Run
that one affected literal separately. External file IO still occurs after the
engine returns save bytes and is outside this change.

## 0. Problem

Both original exact cases reproduce: admission40/3, physical15/3. Admission
assumes ordinal destination wait slots instead of the existing cost comparator,
then tries to save a terminal fault after restoring injected fields. Physical
expects atomic loading/unloading despite the current earned incremental transfer
contract. Its1D layout places waiting machines across the departing carrier's
route, and completed idle carriers have no homes to clear subsequent exits.
These are actual impossible/stale proof inputs, not permission to remove collision
checks, refund loaded cargo, recover terminal sessions or change engine timing.

## 1. Goals

1. Original admission case verifies real distance selection, atomic refusal and
   four independent terminal corruption cases, plus undocked continuation.
2. Original physical case verifies three8-unit hauls with bounded physical
   waiting/promotion, elapsed partial transfers, pause/resume, saved twins,
   actual home return, interruption, endpoint loss, isolation and blocked exits.
3. Check complete source/cargo/destination conservation and bay occupancy after
   every Update in both twins, rather than replacing strict totals with waivers.

## 2. Design

Retain both existing registered case names and sole HaulDockTests owner. Admission
uses original shared fixture unchanged; expect the nearer unload.wait.b before a.
Each corrupt job gets a fresh session; after its first fault verify terminal
save/hash/update/clock behavior, never restore fields or resume that world.

Physical-only authored layout is a new small JSON overlay, not a copied provider:
source10/bay11/waits9,8 and machines10,9,8,7; destination30/bay31. Three explicit
static home targets40,39,38 allow finished machines to physically clear the
shared exit and park without crossing. Existing WorldSetup/IWorker homes and
PointMotion/MobileVacancy own all decisions/movement. No lane invention,
teleport, controller steering or new scheduler. Shared point-docks.json remains
unchanged for its other callers. Factory merges explicit authored fixture
definitions/startup before the sole ordinary content loader, never runtime edits.

Assertions derive actual slot positions via original dock geometry. Partial
loading has source32 minus actual cargo; partial unloading has actual delivered
amount equal8 minus remaining cargo. Full pickup waits for the actual8 units,
not merely a positive partial chunk. Interrupted partial cargo remains real on
its original carrier; player home command uses its authored clear home, not a
destination anchor blocking the bay. Every bounded Reach checks conservation,
no service overlap, no twin divergence and health after each Update.

## 3. API/state

No public API/schema/gameplay-content changes. Original World.Sync now latches
encoding failures after original preflight. HashBreakdown reuses that preflight
and faults on codec failures without returning a prefix; hashes require health
before Settle. Read decode behavior is unchanged. Build2.660 explicitly breaks
old saves; no compatibility path. New layout JSON is test authoring only.

## 4. Files

Existing World.cs/SaveGame.cs/current protocol, HaulDockTests.cs,
MotionCheckpointTests.cs, ExtensionProof.csproj and new
content/point-dock-queue-layout.json; stage/tracker/evidence. No Oil edits.

## 5. Errors

Keep existing typed capacity/route refusal, duplicate/missing binding/progress
corruption checks, no successful prefix and original300-Update bound. Faulted
worlds remain terminal. A stalled valid layout stays a failed test, never a
larger timeout or overlap bypass.

## 6. Compatibility

Development only. No old reader or faulted-world restoration fiction.

## 7. Verification

Original exact baselines40/3 and15/3 retained. All four targeted Tests builds
awaited to terminal. Same two original exact cases and one affected native motion
checkpoint case separately, timeout30000/termination5000/output12000. All
intermediate failures retained. No family/profile/full test.

## 8. Risks

Same1D geometry must have ordered incoming wait positions and real outbound
clearance. Homes are ordinary authored targets, not test-side movement. Partial
transfer assertions must account all actual buffers and never invent refunds.

## 9. Out of scope

Engine performance fix, universal deadlock proof, Oil/business/platform/Draw,
full regression, original broad P9.051 closure and publication.

## 10. Execution

1. [x] Reproduce original failures and audit actual owners.
2. [x] Correct authoring/assertions, preserving all behavioral obligations.
3. [x] Build/run original cases; repair actual encoding failure boundary at World.
4. [x] Update scoped evidence and remaining original requirements.
