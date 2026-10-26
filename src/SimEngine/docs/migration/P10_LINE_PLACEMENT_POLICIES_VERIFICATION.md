# P10-A11 file-authored corridor placement policies — scoped verification

Engine2.458.0/save154/replay1; existing line/graph/job/resource payloads unchanged. Original221/184checked/37open unchanged; additional184. Goal, entire plan and commit/push condition remain OPEN.

## Actual implementation and ownership

ILinePlacementPolicy over SpatialPlacement replaces closed Medium and mutable LineService.TileRules. Every line now links an explicit actual catalog placement_policy. TileLinePlacementDeclaration is the sole native authoring path, delegating existing TileFootprintConditionDeclaration/TileConditionContracts; native and independent Def registrations share it. No terrain enum, default policy, legacy reader, callback registry, condition catalog or duplicate scheduler was added.

Original LineService invokes LinePlacementContracts in activation checks, path search and earned segment checks. Actual spatial/content aliases validate before Initialize, loaded unused policies included; native binding checks run for the selected space. Pure current map/clock observations preserve first exceptions. LineDefinitionContracts.Read now delegates the original whole-target guard without finally verification masking callback failures. Placement policy joins the existing line disclosure dependencies. Path search clears output and rejects faulted sessions before missing-content shortcuts, observes actual line metadata and respects existing content visibility.

Eight existing JSON files migrated in place: authored-segment-infrastructure, line-definition-contracts, colony, line-segments, network-topology, network-transfers, resource-recovery and work-start. Thirteen existing authored lines now reference explicit policy. Seven actual native empty-all policies make previously unconditional fixture intent explicit; the independent line document references the same original segment policy instead of creating another owner. Existing old proof medium-invalid becomes placement-null; historical cases were not rerun. Engine-side generic policy interface is geometry-neutral; built-in tile policy and native corridor compiler remain explicitly tile-specific.

## Current public-player proof

ONE NEW exact case: Line placement policies: physical underlay controls authored corridors and route admission.

Four native/direct policy × minute/half quantum variants use actual master-listed JSON and existing AuthoredSegmentWorkflow factory. Conditional road placement requires the public pipe layer band through the existing predicate evaluator. Commands submit intent even before prerequisites: that is normal existing semantics, not instant building. Its pending engine view reports line.requires-underlay, owns no work marker and consumes nothing. The same router returns Unreachable with no output prefix and no hash mutation.

Ordinary underlay construction uses real machine travel, held tools, authored parallel preparation/inspection and material-consuming installation. Once all underlay cells are earned, the original pending road activates through the same WorkBoard/LineService, and the engine returns the full route. Partial original graph and whole-session saves continue with twin hashes; paused sole clock freezes execution. All three underlay and three road cells are physically earned. Opening40 material units finish at28 after six two-unit segments; tools retained. Pending cancellation without underlay spends/refunds nothing. Own work/plan facts remain private, retained node/pending facts detached, completed save roundtrip/replay/independent session agree. No business/module dependency or host stock/map/progress mutation.

## Admission and faults

Six invalid probes: unused unknown condition, empty-any footprint, unknown space, direct null spatial alias, missing line policy, unknown policy. Actual content/composition refuses before Initialize. Four direct actual callback modes after two game minutes: throw, clock change, clock-then-throw and map change. Public route invocation withholds output, original callback exception remains first, cached own/foreign missing/actual routes clear seeded lists and reject terminal session, Save/Hash/Update refuse, original fault remains authoritative, independent session continues. Native/direct normal purity verified; arbitrary private CLR fields, unrelated map/provider/metadata callbacks and composition fault matrices remain open.

## Retained attempts

| Attempt | Result |
|---|---|
| Solution compile | Clean9.59s, zero warnings/errors |
| FIRST same exact case |164passed/5failed3816ms; PID41848 Exited1/process3973ms; four wrong intent-preview expectations and an attempted post-fault feature acquisition |
| Necessary focused fixture compile | Clean5.61s, zero warnings/errors; driver preserves existing intent semantics/caches both views before fault |
| FINAL same exact case |224passed/0failed4117ms; PID18816 Exited0/process4274ms |

Every runtime followed terminal clean compile. Timeout60000ms/termination5000ms/output30000chars, stage loops bounded900 Updates; no truncation/observer errors. No production repair after initial compile, no executable edit after FINAL or old/family/full runtime.

Runtime: `dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Line placement policies: physical underlay controls authored corridors and route admission" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`.

## Remaining work

Native construction cursor/path/compiler/topology is still tile-specific. Non-tile corridor execution, physical predicate invalidation between earned segments, arbitrary policy dependency disclosure and provider fault matrices, utility/corridor combinations, network reservation/accounting/full/stress acceptance remain OPEN. The broad P10.003/P3.004/P4.014 requirements are not checked from this case. No Oil/platform/Draw/advanced Tycoon/protected clock/G19 edits; nothing staged/committed/pushed before whole goal and plan finish.

## Metadata closure

Current metadata closure:1586 source anchors,13 quickstart anchors,851 selected executable inputs; zero hash mismatches/post-pass executable drift.2778 paths/976 Markdown files/8601 local links, zero broken links;18 valid master rows; all221 original literal gate/checkbox anchors exact and unchanged (184checked/37open), additional184. Thin driver boundary scan empty, executable Medium/TileRules/medium-invalid scan empty. git diff --check exit0 (34 existing line-ending-warning output lines),2766 dirty paths/zero staged. Sole clock/protected G19 hashes unchanged. Seventeen previously frozen executable inputs changed (four existing production); four new executable files including one core declaration and two stage/evidence docs. Six new source files admitted; consuming dependencies already frozen. No executable edit after FINAL. Metadata integrity is not broad gameplay/provider acceptance.
