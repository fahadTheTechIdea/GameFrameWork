# P11-D11 engine-owned sensor occlusion — verification

Date2026-10-08. Canonical engine only. Full goal and P1–P12 plan ACTIVE; publication waits for FULL
verified completion. This is focused evidence, not complete-engine acceptance.

## Ownership and identities

Build2.257.0/save139; tile_sensor codec1 and every persistent payload unchanged. Content bindings
are new authoring, not saved obstacle/fog state. No compatibility overload or fabricated default.

[ISpatialSensor](../../src/Beep.SimEngine/Standard/SpatialSensors.cs) owns pure current CanObserve.
The SAME [SensorVisibilityDef](../../src/Beep.SimEngine/Standard/SensorVisibility.cs) delegates
layer and actual-entity admission to the actual owned provider. Detached range geometry is not
authorization to observe a changed world. Existing scoped provider guard surrounds the query.

The SAME [TileSensor/TileSensorDef](../../src/Beep.SimEngine/Standard/TileSensorOcclusion.cs) owns
JSON layer selection, threshold validation, native geometry traversal and current reads. No second
obstacle/value owner, visibility map, scheduler, raycast service or clock. Public native tile layers
only: private foreign layers cannot affect another company's sight or keys. Explicit [] denotes
unobstructed sensors. Missing/duplicate/unknown/wrong-space/private references, zero/fractional-byte/
out-of-byte thresholds fail at game initialization. Fixed/int and multiple-layer breadth compile only.

Actual source top-left tile center is the declared native ray origin. Whole target footprint cells
must pass. Source footprint and destination cell do not self-block; the obstruction itself can be
seen, not cells behind it. Exact iterative supercover checks both adjacent cells at a corner tie.
Long arithmetic uses bounds of this native coordinate representation; no engine/world/ray-size cap,
recursion or copied grid. Cost scales with target cells and crossed ray cells, NOT a proved budget.
Optical obstruction does not invent navigation rules: density can block sight without blocking travel.

Existing native layer versions extend detached scope keys. Existing end-Update/restore visibility
refresh owns map cursors. Source operation/state, map values and sole clock persist through their
original codecs; there is no saved fog, obstruction version or extra Update loop.

## Every build/run

Canonical working directory:

```
dotnet build Beep.SimEngine.slnx
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Sensor occlusion: authored screens, physical sight and saved continuation"
```

1. Build exit0,12.45s,0 warnings/errors.
2. Exact NEW bounded case: exit1,51 passed9 assertion failures410ms,0 aborted suites.
   Hidden page correctly returned Unavailable before cursor disclosure; test incorrectly expected
   Invalidated there. Still-visible sample now separately checks old-cursor invalidation. Invalid
   content tests created factories but did not initialize games; validation correctly happens at
   initialization. Only those observed test assumptions repaired; engine source unchanged.
3. Build exit0,5.11s,0 warnings/errors.
4. SAME exact case: exit0,62 checks0 failures/0 aborted suites342ms.

121 older strict build expectation files/lines migrated and compiled only. Three existing authored
native sensor instances migrated to required explicit empty occlusion. No older/family/full/platform/
stale suite. No green full-regression or old-runtime claim.

## Proven behavior

[One new case](../../tests/Beep.SimEngine.Tests/SensorOcclusionTests.cs) composes existing native
master/startup/work/time/operation contracts. Master explicitly selects the new
[density document](../../tests/ExtensionProof/content/native-sensor-occlusion.json) and
[visibility document](../../tests/ExtensionProof/content/native-sensor-occlusion-visibility.json).
No test provider/sensor rule, selection algorithm, movement implementation or game logic module.

- Actual authored density/threshold and master ownership; no IEconomy required.
- Detached range contains hidden field; actual provider/map/entity/scoped snapshot refuses it.
- Clear sensor of another company reads actual public terrain but cannot read private geology.
- Opaque endpoint itself, source footprint and clear diagonal semantics; blocked corner neighbour,
  mixed-footprint refusal, out-of-bounds refusal and current enabled/range gates.
- Pure reads/hash; unchanged retained geometry/key; actual threshold-crossing density edit changes
  sight and invalidates stationary sensor scope. Hidden stale page refuses without output; visible
  sample rejects the stale cursor. Entity appearance/removal follows same contract.
- Native density/controls restore derives current blocked sight with no saved fog model.
- Normal player hold/release, operation controls and physical move past screen. Bounded400 updates
  with step/position/motion diagnostic on timeout. Sole-clock pause and moving save/load twins.
- Actual relocated field/entity sight, same terrain value, no free remembered survey and isolated
  interleaved session. Eight invalid authoring cases fail at initialization.

Direct native layer Set calls are scoped provider protocol checks, NOT a normal player editing or
construction workflow. This case does not claim player-owned optical-screen manufacturing/removal.

## Freeze and scoped source inspection

Source/tests/content frozen immediately after final green; remaining edits documentation-only.
Scoped609 paths/122 Markdown documents/1943 local links/0 broken; actual git diff --check exit0
with existing LF/CRLF warnings. All manifest hashes verified after final documentation edits.
Own [manifest](P11_SENSOR_OCCLUSION_SOURCE_MANIFEST.json) excluded from its own hashes. Historical
evidence/manifests preserved; accumulated inventory does not prove whole-repository/runtime health.
Sole GameClock unchanged SHA256:
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.

## Open gates and next work

Non-tile sensors, hostile provider/global purity, typed/multiple selected layers, tall/large/multicell
source/target and long-ray performance, structural/entity occlusion, weather attenuation, persistent
exploration/sharing/forgetting, other operation signal gates, rival strategy, replay/fuzz/stress/full
P11/P1–P12. Prior logistics/other stages compile only, not rerun at2.257.0. No Oil/Tycoon/business/
host/demo/Draw/platform implementation. Continue engine gameplay slices; do not publish this slice.
