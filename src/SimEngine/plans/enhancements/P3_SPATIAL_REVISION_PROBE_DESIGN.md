# P3-S28.C — Exercise the original same-payload revision guard

Status: implemented/scoped-verified776/0. Engine2.658.0/save160/replay1 unchanged.
All attempts/identities/limits: [verification](../../docs/migration/P3_SPATIAL_REVISION_PROBE_VERIFICATION.md).
Extends P3_UNIFORM_BYTE_HASH_DESIGN.md; no second observation owner.

## 0. Problem and specification

The existing exact SharedSpatialObservationTests case reproduces728 passed/48
failed on completed2.658 binaries, matching the older scalar/optimized A/B.
All failures are point-space access callbacks attempting Revision/RevisionThrow.
SpatialMutationProbe.Change performs two PointSpace.WriteLayer calls. The first
mutates the sample and original revision. The second enters
SpatialLayerAccess.CanSeeDefinition -> EntityObservation.ReadProvider while the
outer callback still owns its original baseline. The nested guard correctly
rejects the still-mutated codec before restoration or the intended original
error. The probe never reaches the test's stated same-payload condition.

## 1. Goals

1. Reach identical actual persisted payload with a changed original revision.
2. Retain all existing776 assertions, native/point cases, both quanta, original
   errors, terminal refusals and physical saved/replay checks.
3. No engine guard/hash/visibility/resource/clock/JSON or normal-write changes.

## 2. Design

Only ExtensionProof/ContentAccessContractProof.cs changes. Capture PointLayer
once. First mutation still uses original public WriteLayer with authorization,
preflight, CommitWrites and the genuine revision increment. Only the negative
restore mode calls that layer's existing internal Write to restore its sample.
It deliberately retains the already-incremented original revision; no new
counter, copied commit, raw field/reflection write or public bypass is added.
guard.sample is the actual master-listed public fixed non-resource layer in
shared-spatial-observation.json. Its existing sample writer retains validation,
storage and resource-cell notification. The probe then independently hashes the
complete space and reads its real revision. Existing assertions remain intact.
This corrects fault injection, not a production observation defect.

## 3. API and state

None. Engine build/save/replay/JSON and ordinary PointSpace.WriteLayer unchanged.
ExtensionProof remains a public-contract-only non-friend engine extension.

## 4. Files

- Existing tests/ExtensionProof/ContentAccessContractProof.cs negative helper,
  approximately8 lines, low production risk.
- This design and docs/migration/P3_SPATIAL_REVISION_PROBE_VERIFICATION.md.
- Existing master/parent/AGENTS: current evidence and remaining scope.

## 5. Errors

First-write refusals still throw proof.spatial-mutation-refused. Unexpected
no-op restoration throws proof.spatial-restoration-refused. Revision mode must
reach the original revision fault; RevisionThrow must preserve the intentional
proof.spatial-access-first-error. Faulted sessions cannot save/hash/query healthy
facts. Ordinary authorization is unchanged.

## 6. Compatibility

Development only. No legacy reader, shim or old save-header rewrite.

## 7. Verification

Baseline and final: one exact original shared-spatial case, timeout30000,
termination5000/output18000. One targeted Tests build must reach terminal
success before final execution. No full/profile/family/default run. Record
source/copied-core identities. Separate native inspection cannot prove an
unobserved prior worker passed its wall-time gate.

## 8. Risks

Restrict low-level restoration to the existing negative helper; never use it
for ordinary commands. Existing assertions prevent an earlier different fault
from being mistaken for successful revision-only refusal.

## 9. Out of scope

Production runtime rules, Oil/business/platform/Draw changes, full regression,
original broad gate closure and Git publication.

## 10. Execution order

1. [x] Reproduce and inspect original mutation/guard ownership.
2. [x] Correct existing negative restoration, retaining all assertions.
3. [x] Await targeted build; run the exact affected case.
4. [x] Record outcomes/current native artifact/remaining original gates.
