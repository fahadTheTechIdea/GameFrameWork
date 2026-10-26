# P4-G2b2b / P9-A1 — Target-region verification

2026-10-05 working-tree evidence, not committed HEAD or full regression. Engine2.52.0/save26;
existing work/view11/resource-flow4 layouts unchanged. Only standalone SimEngine/proof/tests/docs.

## Final-source gate

`dotnet build Beep.SimEngine.slnx --no-restore`: **zero warnings/errors, 1.58s**.
Runtime, public-only ExtensionProof, testing library and tests compile.

Exactly these two directly affected suites ran throughout development:

```powershell
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Work regions: authored shapes, provider contracts and pure private requirements'
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Work regions: physical target guards, paused ownership and saved continuation'
```

| Exact suite | Passed | Failed | Final runtime |
|---|---:|---:|---:|
| Authored shapes, provider contracts and pure private requirements | 42 | 0 | 236ms |
| Physical target guards, paused ownership and saved continuation | 31 | 0 | 264ms |
| Total final-source checks | 73 | 0 | 500ms |

Zero aborted suites; counts do not accumulate development reruns. No previous resource/worker suite,
broad/full gate, oil, replay/lockstep, stress/soak/allocation or whole-framework acceptance ran.
Earlier 364-check continuous-input evidence remains historical, not rerun at this source checkpoint.

Targeted tracked-file `git diff --check` passed; explicit trailing-whitespace scans of all newly
authored/untracked runtime/proof/test/stage/evidence files found no matches. These are source checks,
not additional test suites. Existing unrelated dirty changes were preserved.

## Verified behavior

- Existing content catalog aliases one SpatialRegionDef / ISpatialRegion per stable identity.
  Explicit master-listed rectangle/painted JSON resolves space references and validates exact geometry.
  No parallel region/requirement registry, scheduler, live state store or world representation.
- Full target footprint must fit: origin-only admission, missing painted tiles, duplicate/out-of-space
  tiles, negative/empty rectangle and oversized integer bounds refuse explicitly. Sparse painted input
  canonicalizes immutable coordinates; shape checks cannot mutate jobs/resources/hash.
- TargetRegionRequirementDef is an ordinary leaf under existing all/any evaluation. Private views
  clear stale facts on unauthorized access. Absent geometry and incompatible space/representation
  produce typed Invalid, not guessed origin/default readiness.
- Ordinary post commands queue intent while outside; existing work stays blocked without claiming a
  worker, consuming startup materials or advancing. Actual target entry assigns through the current
  selector and still requires physical travel. Departure pauses the same exclusive job/progress;
  re-entry resumes without consuming startup twice. Cancellation releases the current job, retains
  real prior material consumption and restores without phantom region state.
- Timed guarded work and moving/paused/completed saved twins are deterministic. Physical waits are
  bounded at 200 updates with node/state/progress/blocker diagnostics.
- Public-only PointIntervalRegionDef proves provider-owned non-tile geometry, strict provider
  validation, pure checks and paused physical saved continuation without TileMap or scheduler edits.
- Build identity changes to2.52.0. Regions are immutable fingerprinted content; existing presence,
  node/job/branch/progress codecs remain the sole saved state. No layout change or legacy reader.

Development fixture errors were corrected before the final gate: authored durations use existing
`hours`, not an invented `minutes` key; foreign proof targets no longer overlap; re-entry uses a free
valid tile instead of the worker's current occupied tile. No movement collision rule was relaxed.

## Remaining scope

This is target membership, not path containment, current-worker-position policy, configurable
player area edits, all fleet/service/extraction/source dispatch policies or full P4/P9. Broader replay,
provider fault injection, vertical-layer region implementations, emergency interactions and stress
proof remain open. No oil/Tycoon/Draw/platform/host/demo behavior changed.
