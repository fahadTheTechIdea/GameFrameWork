# P4-G4f — Contract-owned transformation cancellation

## Specification

Extend SAME ITransformationQueue/TransformerManager with CheckCancel and Cancel. Remove the static
native cancellation path. Commands and private management previews use the same current admission.
No second queue, scheduler, stock ledger, clock, compatibility path or host rule.

## Protocol

Admission reads the actual named provider snapshot, validates current index and returning-head
refusal, then guards provider CheckCancel with existing EntityObservation.CaptureProvider/Sync.
Commit rechecks admission, captures detached actual queue/material/tool/control facts, invokes the
provider once, and verifies controls, sibling component codecs, revision and exact queue transition.

- Tail cancellation removes exactly its selected entry, retaining head progress/phase/blocker/WIP.
- Head without an active WIP execution removes exactly the head and resets to idle/zero/no blocker.
- Head with active WIP enters Returning with SAME queue, progress, execution and actual input/tool
  commitments. No instant refund/release. Existing provider Update performs physical return, waits
  for actual inlet capacity, releases actual tools and retires the head only after return succeeds.

Material/tool endpoints and commitments cannot change during the cancellation request. Legitimate
Entity.Touch advances enclosing inventory revisions; compare actual commitments, not a stale tool
revision. Automatic supply withdrawal remains existing engine-service behavior. Scope is provider
and sibling codecs, not a mod sandbox/world-global purity proof. Commit/pure violations latch the
existing World fault; no rollback/compensation or partial save/hash/continued time.

## Implementation and verification

Change TransformationContracts, Transformations, TransformationManagementView, SaveGame, existing
engine-side TransformerProof negative probes, one NEW bounded cancellation case and its registration.
Build2.247.0/save139; no persistent layout change. Older build-identity assertions source/compile ONLY.
Reuse existing JSON providers; use ordinary commands/views/Update and native transaction/receipt
APIs to create real return-capacity pressure from an authored donor, not fabricated balances.

ONE exact NEW case: native tail cancellation, blocked physical head return, returning save/load,
independent provider exact cancellation, private/refusal parity, hidden admission mutation and
malformed/throwing commits with fault cutoff. Build, run ONLY that case; repair observed failures
only. Freeze source/public proof/tests/JSON after final green, then docs-only evidence/tracker.
No older/family/full/platform/stale gate; no Oil/host/demo/Draw/platform/business change.

## Remaining scope

Automatic-supply integration, continuous/utility/entity-output cancellation breadth, cross-entity/
unrelated-service corruption, resource-provider fault matrices, effects/generated/team/haul/waits/
templates/ETA/replay/stress/CPU/full G3-G5/P1-P12 remain open. Full goal stays active; no publication
until full completion and verification.

## Verified scoped checkpoint,2026-10-08

Implemented2.247.0/save139/persistent layout unchanged. ONE clean build5.87s/0 warnings/errors;
ONE exact NEW bounded case117 checks0 failures/aborts266ms FIRST attempt.113 older identity
assertion files/lines compile/source ONLY. Source/public proof/tests/JSON frozen after green.
No older/family/full/platform/stale gate. Native actual tools/tail/blocked physical return/saved
twins/conservation/private refusal and healthy independent provider proved. Hidden/no-op/extra/
rewritten/control/throw/tail-progress/sibling violations fault; direct coordinator fault boundary
also proved. [Evidence](../../docs/migration/P4_TRANSFORMATION_CANCELLATION_VERIFICATION.md).
