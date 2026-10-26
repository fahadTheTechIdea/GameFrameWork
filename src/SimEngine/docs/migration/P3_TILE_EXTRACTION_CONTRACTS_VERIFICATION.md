# P3-S31 — Tile extraction and reading verification

Historical2.582.0 evidence below. Subsequent [P3-S32](P3_SPATIAL_WITHDRAWAL_PREPARATION_VERIFICATION.md) corrects the point withdrawal/schema failures and proves native visibility-enabled extraction; earlier failed attempts remain recorded here.

Current engine: beep-simengine-2.582.0/save157/replay1. Independent proof space schema2; native payloads unchanged. Scope: capability-based tile layer binding, native timed extraction and optional surveying on an actual independent tile provider. No Oil/Tycoon/platform/host logic changes, full/profile/family execution or Git publication.

## Implementation evidence

- TileSpatialGeometry.RequireLayer<TLayer> is the shared registered-layer capability/metadata selector. RequireSampleLayer delegates and admits the existing revision. Quantities do not have to implement sampling.
- TileResourceCellDeclaration and TileReadingDef no longer require TileMap or MapLayer. Existing work, resource transaction, endpoint reservations and knowledge owners execute the authored rules.
- The independent provider reuses its sole sparse dictionary, SpatialResourceValues and original SpatialResourceCellBindings. Candidate sample/owner/revision/books restore before publication; all layers contribute to its observation revision.
- Explicit master-listed extraction/readings documents author a private12-half-unit deposit, two-unit extraction, six-minute staffed work and a separate three-minute survey. Normal drivers neither inject stock nor complete work.

## Every build and run

1. Targeted test-project build: exit0, zero warnings/errors, MSBuild7.07s (observed7.61s).
2. New exact physical extraction/survey:88/0,2359ms; bounded PID25236 exit0, observed2518ms.
3. Existing exact `Spatial extraction: authored ownership, receipts and actual transfer provenance`:83/2,836ms; bounded PID5972 exit1,989ms. One stale point-space schema expectation4 versus actual5; then point reserved withdrawal called public WriteLayer/CanObserve/EntityObservation while endpoint `_committing` was positive. Original StageSync correctly rejected unsettled commit. No claim of a green native/point admission gate.
4. Existing exact sensor boundary: worker PID6688 terminal exit-1073741571,8751ms, stderr truncated. New proof SetSample called instrumented GetSample, whose negative mutation callback recursively called SetSample. Repair reads the existing dictionary directly for mutation preflight; it does not alter optical rules or suppress callbacks on reads.
5. Targeted repair build: exit0, zero warnings/errors, MSBuild5.58s (observed6.14s).
6. FINAL affected exact sensor boundary:73/0,157ms; bounded PID39324 exit0,298ms, outputs not truncated.
7. FINAL new exact extraction/survey:88/0,2258ms; bounded PID19096 exit0,2401ms, outputs not truncated.

Final focused successes161 checks across two exact cases. The separate existing admission failure remains unresolved. No executable edits after the final two cases; all owned test/build processes reported terminal results.

## Proven physical behavior

Machine travel precedes extraction; partial productive work and dock location are observed through immutable views. Saving unfinished extraction restores actual work/layer/endpoint state and twin hashes match every update. Pausing the sole clock freezes work and stock. Exact two-unit finite extraction completes without survey knowledge. Separately ordered survey takes authored time and reads the actual depleted stored value8 (four native units), preserving conservation with two units in destination inventory. Company-private knowledge is withheld from the other seat, survives saves, and ordinary command replay reproduces the final hash. Fresh interleaved sessions receive no changed stock, knowledge or state.

## Not proven / next work

Fix the point-provider internal withdrawal path without passing unsettled transactions through public observation or weakening save guards; update the actual schema assertion. Then verify its exact affected case. Concurrent/receipt-reserved independent extraction, capacity/resource conversion breadth, lifecycle/removal/recovery/utility/privacy/provider-fault matrices, global replay/lockstep/stress/performance and all original compound gates remain open. This is not full-engine acceptance.
