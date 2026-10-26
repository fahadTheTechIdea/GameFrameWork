# P6-I4: Independent spatial fleet acceptance

Status: implemented/focused-verified on unchanged engine2.605.0/save159/replay1,
final81/0 plus separate compiled external audit10/0. [All attempts and limits](../../docs/migration/P6_SPATIAL_FLEET_ACCEPTANCE_VERIFICATION.md).
Continues original P6 Game B, not a replacement for the
combined A/B/C requirement or full industrial-colony acceptance.

## Problem and completion criteria

Existing independent tile proofs cover navigation and material-supplied building
separately. Complete a single authored spatial-game flow with competing stock
priorities, cancellation, an in-flight changed route and alternative role metadata.
Use the existing external tile provider, native navigation/motion, stock/haul/work
and construction owners. All simulation decisions remain in those engine modules.

## Design

New explicit spatial-colony-master.json references existing independent sensor,
navigation, work and construction documents plus a fleet scenario document.
The fleet uses a30-second quantum and two gaps in an authored impassable layer.
Player commands move an ordinary blocker into the shorter crossing after physical
hauling starts. Original navigation must route loaded cargo through the other gap;
there is no test-side route setter, layer mutation or recovery implementation.

One content-only external SpatialColonyProofModule registers existing
IndependentWorkerDef (the original shared WorkerDeclaration/Worker runtime), not
another worker or scheduler. The original build graph gains an authored alternative
fabricator role. Native hauler and independent-definition builder use the same
engine dispatch/capability contracts. A native builder can be held through normal
commands so work must select the alternative role/provider.

Starting material stays the existing depot's12. Two ordinary stock policies compete
for8 each with different priorities, proving scarce material goes high8/low4.
Then release the high reserve for an8-material physical project; low remains
protected. Final total4 must account for all actual stock/cargo and consumption.
Early cancellation of a separate unstarted project must create no free result or
lost material. Preserve saved partial travel/handling/work, twins, replay and views.

## Files and verification

New external module, master/fleet JSON, focused public-input test and explicit
resource embedding/registration. Extend existing construction role metadata only;
no production owner, Oil/Tycoon, host/platform, compatibility or clock changes.

- [x] I4.1 — Author explicit content composition, fleet and two-crossing topology.
- [x] I4.2 — Public-input scenario proves cancellation/priority/in-flight detour and
  actual alternative-role timed construction with exact material accounting.
- [x] I4.3 — Saved twins/partial checkpoints/replay and compiled external boundary.
- [x] I4.4 — Record every attempt, limits and master status; no broad closure inferred.

Build targeted test project, then only the new exact case and relevant compiled
module boundary audit. No full/profile/family execution. Waits bounded by updates
and wall-clock diagnostic timeout. Failure must retain actual positions/jobs/stock;
never lengthen a bound or bypass movement to fabricate completion.
