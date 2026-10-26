# P3-S1: Spatial publication privacy revalidation

Status: implemented/focused-verified. S1.4–S1.5 add seven literal finals1305/0;
unchanged S1 direct/layer evidence1637/0 supplies the remaining matrix rows.
Original P3.acceptance.008 now checked; other23tracked requirements/full goal open.
See the verification report for explicit scope, all failures and coverage.

## 0. Problem

The original P3 snapshot/spatial privacy gate remains open. Current source already
guards visibility/provider callbacks and stages collections before publication.
Two relevant cases still assert historical save154 instead of the current authored
protocol expectation: `DirectSpatialViewTests.Run` and
`SpatialLayerAccessGuardTests.Run`. Baselines fail only those assertions (817/1
and786/1 respectively). Do not mistake historical protocol pinning for an engine
privacy defect or replace the original spatial/knowledge authorities.

## 1. Goals

Revalidate these actual projection paths on the current protocol. Explicitly prove
that retained non-tile layer facts cannot mutate the engine and survive real
ownership changes without becoming current authorization. Bound physical waits by
both updates and elapsed time. Keep the broad original gate open pending its full
coverage audit.

## 2. Design

Extend the existing two cases, not a second fixture/model. Use
`StateCodecTests.ExpectedSaveFormat`, whose value comes from the authored current
protocol document. Existing public commands change actual layer ownership. Hold
the original detached projection, check immutable nested JSON and copied unit/
resource interfaces, lose access, check retained historical facts, restore access,
and continue the original save/replay proof. A20-second diagnostic deadline augments
the existing1000-update work bounds; no extra work allowance.

## 3. API

No production API, content, codec, engine identity or gameplay changes.

## 4. Files

Modify the two named tests; add this stage and a verification report; update the
master's current entry. All changes stay in Beep.SimEngine.

## 5. Errors

Retain original callback failures, fault cutoffs and private snapshot preservation.
Timeout reports clock, fault and the actual published entity states; never grants completion.

## 6. Compatibility

No legacy reader, shim or compatibility requirement.

## 7. Verification

Clean targeted project build, then only the two literal bounded test cases named
above. Record baseline and final results, including any failures. No full/profile/
family run. No gate closure inferred from these two cases alone.

## 8. Risks

Historical snapshots legitimately retain previously authorized facts; loss of
current access must not mutate history or grant future sample access. Definition
copies must not expose live content capabilities.

## 9. Out of scope

Oil/Tycoon, rendering/platforms/Draw, arbitrary-provider CLR security sandbox
claims, privacy gates beyond original P3.008, full regression, staging/commit/push.

## 10. Stages

- [x] S1.1 Inspect actual projection and query owners; run two current baselines.
- [x] S1.2 Extend retained-layer assertions and bound physical waits in place.
- [x] S1.3 Build, run exact cases, record all attempts and update current tracker.

Final1637/0 in two literals; [all attempts and limits](../../docs/migration/P3_SPATIAL_PRIVACY_REVALIDATION_VERIFICATION.md).

### S1.4: Native query publication boundary

Extend `CoreViewObservationTests`, the existing owner of core view privacy tests,
with real hidden prototype geometry in its healthy JSON composition. Prove hidden
footprints stay absent while the explicit observer sees authored geometry; invalid
and empty areas clear caller output; returned entity structs cannot alter retained
snapshots. Inject a late spatial-area visibility failure after an earlier entity
has matched, and inject occupant/content callback failures. These remain explicitly
privileged negative instrumentation, not player/host gameplay. Original guards
must withhold all output and preserve first-fault publication. Bound its existing
physical construction stages by2000updates/20seconds. Only this exact case runs.

- [x] S1.4 — Native area/occupant/footprint refusal and detached entity proofs.

### S1.5: Complete original snapshot/spatial-query gate audit

Reconcile the actual original P3.acceptance.008 against selected current cases:
native query/core fault publication, shared non-tile layer access, direct space
capture, malformed snapshot headers, symmetric/stable comparison, native/non-tile
visibility deltas, and native sparse-layer privacy/cursor invalidation. Fix only
stale protocol assertions in the existing header/comparison cases; add elapsed
deadlines to their original loops and the delta case. Run the named literal cases
only; reuse unchanged S1 direct/layer evidence after verifying core source identity.
Close the original checkbox only if the resulting explicit requirements matrix
covers that whole requirement. Broader hidden menus/knowledge/optional-module
interleaving gates are separate and must not be closed here.

- [x] S1.5 — Current snapshot/header/comparison/native-layer evidence and original requirement mapping.
