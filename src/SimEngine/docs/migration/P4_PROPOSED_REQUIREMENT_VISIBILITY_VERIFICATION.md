# P4-R1: Proposed requirement target disclosure verification

2026-10-15. Engine2.606.0, save159, replay1, work-view105. Two literal finals855/0.

## Reproduced engine defect

`WorkView.PreviewOrderRequirements` previously checked company ownership but not
the actual selected visibility provider. By contrast, its existing-order path
`TryCopyOrderRequirements` uses `Discloses(order)`, including `SeesTarget`.

The existing PointGame JSON fixture now supplies one actual `visibility.stock`
target-resource requirement. A test-only configured instance of the existing
ViewObservationVisibility hides the OWNED destination. The ordinary entity view
correctly refuses that target, while the original requirement preview returned
success and a real stock-shortage result, including after save/load. A second
negative configuration throws an original visibility exception during a paused
read: the old preview skipped that callback and returned the shortage anyway.

The corrected pre-fix reproduction had seven failing assertions and598passing
ones. This is a demonstrated query-authority bypass, not an inferred naming issue.

## Repair and ownership

The existing query now requires `SeesTarget(target)` at its existing target
admission, before invoking intent/requirement providers. It reuses the original
PlayerView.CanSee and EntityObservation guard. Hidden target returns NotFound with
cleared output; callback failure latches the original World fault and publishes no
prefix. No new evaluator, visibility cache, state owner, serializer, scheduler or
compatibility method. Existing company/activity disclosure checks remain.

Engine identity2.605.0→2.606.0 and work-view104→105 are explicit. The independently
authored current protocol expectation changes build identity only. Save159/replay1
and all state codec layouts remain unchanged. Existing exact identity validation
continues to reject other builds; no old-build shim was added.

ExtensionProof's existing visibility provider gains an optional entity predicate;
the existing negative WorkObservationProbeModule can configure a fresh instance
per composed world. This is explicitly selected negative instrumentation, not
positive host/demo gameplay or a new visibility system. Its default behavior and
persisted Value codec remain unchanged. Authored stock shortage remains the
original resource/work owners' decision.

## Focused verification and every attempt

Build command each time:
`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`.
Three successful builds:5.49s,7.28s,7.44s reported; each zero warnings/errors, exit0.
Every runtime began only after a successful terminal build result.

Only these exact literals ran:

1. `Work observations: physical execution and terminal candidate plan query boundaries`
2. `Activity graph disclosure: nested requirement and node final effects stay private while earned work continues`

Each invocation used `--bounded-exact`, `--timeout-ms 60000`,
`--termination-timeout-ms 5000`, and bounded captured output. All children terminal,
no abandoned process, timeout or aborted suite.

- First reproduction596/9,799ms, PID22168 exit1/937ms. Seven actual bypass failures;
  two test assertions incorrectly expected generic Quantity facts for resource
  requirements. Corrected those two assertions to the actual typed
  WorkResourceCodes.Stock result, not fabricated quantity data.
- Corrected reproduction598/7,862ms, PID32684 exit1/1007ms. Remaining seven failures
  demonstrate hidden stock facts and skipped callback before production fix.
- FINAL work-observation609/0,812ms, PID33084 exit0/952ms. Same original physical
  execution/candidate/plan/haul/save/replay/fault boundaries remain. New assertions
  prove hidden and restored hidden targets refuse, explicit observer and visible
  target retain actual shortage facts, reads remain pure, original callback failure
  withholds output/healthy save and retains own previous spatial epoch. Independent
  session state unchanged.
- FINAL activity-graph disclosure246/0,945ms, PID10852 exit0/1082ms. Existing
  six30/60-second compositions cover nested requirement/node/final-effect metadata
  disclosure, real timed discovery and work, forgetting while accepted work
  continues, saved twins, rediscovery of the SAME completion, private history and
  canonical replay. No change to these assertions or scenario.

Final sum855passed/0failed. Earlier failures are preserved, not added to final totals.
Core441 C# combined SHA256 after repair:
`6bbfacf46e6f2a69793d70bedf425f027313559832f5ce819853fb48f15324e6`.

## Limits and next work

No full/profile/family tests. No Oil, Tycoon, host, platform or rendering changes.
No staging/commit/push. Original P4.acceptance.014 remains OPEN: this repairs one
concrete proposed-query disclosure gap, not the complete evaluator/command/blocker
matrix. Original bookkeeping remains221/198recorded checked/23tracked open. Earlier
P3 evidence belongs to2.605.0 and is historical on this new build; not silently
represented as rerun855checks. Full goal remains active.
