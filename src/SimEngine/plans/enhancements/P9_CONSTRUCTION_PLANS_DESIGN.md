# P9-A2b — provider-owned intents and authored physical construction

Historical build2.70.0 focused proof. [P7-D2/P3-S11](P7_PROVIDER_RECOVERY_DESIGN.md) subsequently
removes generated salvage and the tile-only recovery/demolition paths described as remaining here.
The generic plan and authored site API remains; recovery selection is now explicit scenario JSON.

## Problem and goals

A2a stores only an activity key plus an existing target. It cannot represent a future site without
another plan model. ConstructionService/commands/generation also require tile geometry and generate
sites instead of allowing ordinary explicitly authored site prototypes. Replace those narrow paths,
not wrap them or add a building-plan board.

1. One PlanIntent, IPlanBoard, WorkBoard codec and native execution link handle provider-specific
   immutable JSON arguments. IPlanDefinition extends existing IActivityDefinition resolution; no
   new catalog or scheduler. Submission and current activation have distinct contract checks.
2. Existing ActivityDef uses native player-target admission. ConstructibleDef uses ConstructionService
   and the selected placement provider. Identity derives from validated provider arguments, not JSON
   key order or a host-calculated position. Invalid argument/definition refuses explicitly.
3. Construction and site prototypes are explicitly authored in master-listed JSON. SiteDef resolves
   its target and authored state; no generated-site translation, old reader or implicit prototype.
4. Spatial placement arguments decode through the selected provider contract. Construction creates
   an actual authored site, posts its ordinary graph and later creates the target only after real work.
   Tile and independent point geometry must both use this owner. Occupied placement waits and rechecks;
   cancelled pending intent allocates no entity/order/resource. No ghost occupancy or resource store.
5. Existing pause/priority/supply/cancel/repeat/history/private views apply to all intents. History
   records actual target identity after native completion, not duplicated active geometry/progress.

## Responsibilities and files

- PlanContracts.cs/WorkPlans.cs/PlanCommands.cs/WorkView: replace activity-only payload with generic
  frozen JSON intent, provider identity/submission/activation contracts, shared controls and sole Sync.
- Work.cs: ActivityDef implements plan admission/activation; direct activity admission remains shared.
- SpatialPlacement.cs/TileMap.cs: selected provider owns JSON placement decoding and native geometry.
- Construction.cs/ConstructionModule.cs/ConstructionView/StandardCommands.cs: authored site links,
  generic placement and shared native creation/completion; existing resource recovery stays its owner.
- ExtensionProof JSON/providers: explicitly authored tile sites and point build/site, ordinary engine
  content and command usage only. Existing callers migrate directly, no compatibility overloads.

## Failure and timing rules

Submit records intent only after provider submission validation. Current activation rechecks resources
handled by ordinary graph execution, content permission and actual placement; no retained preview
authorizes entity creation. A valid but occupied construction intent waits with the current blocker.
Activation must create exactly one actual native order with matching graph/company/priority. A broken
provider faults Update rather than returning invented success or compensating mutations. Native work,
inbound supply, paid inputs, node progress and site retention remain existing authorities.

Completion captures selected placement before retiring the empty site and creates the real target
through that same selected spatial provider. Stocked recovery in unsupported geometry refuses with
the existing typed blocker and retains the site/order; never discards inventory. General recovery/
salvage geometry is a separate remaining P3/P7 slice, not a reason to impose tiles on empty/fully
consumed site construction.

## Verification and remaining scope

Build and exactly two directly affected suites; no broad/full gate:

- `Plans: authored intent and construction admission contracts`
- `Plans: physical sites, pause and saved continuation`

Retain A2a policy/physical coverage within these two suites, add generic JSON purity/identity/private
commands, strict authored site references, real tile/point construction, occupied pending placement,
physical input/travel/pause/cancellation, native completion and saved twins. Every wait is bounded.
Construction/network templates, bulk groups/automatic repeats/history controls, all geometry recovery,
broader provider fault/replay/stress and full framework acceptance remain required later work.

## Status

Focused-verified **310 / two exact suites** (135 admission; 175 physical), zero failures/aborts,
zero build warnings/errors. Current build2.70.0/save42/work20/work-view25/logistics4/logistics-view9/
construction-view2. [Evidence](../../docs/migration/P9_CONSTRUCTION_PLANS_VERIFICATION.md).
The point-construction document is separately master-listed, not merged into another prototype file.
Site input filters/quantum/capacity and empty creation stock validate before Initialize. Direct and
persistent construction share admission and physical sites; queued command and actual site/graph
save stages pass. Wrong-target and extra-order provider probes fault actual Update and cannot
checkpoint/hash partial activation. The exact two suites include A2a policy/physical proof; old
API callers migrated directly. Other suites compiled only. This slice is complete, not P9/P3/P4/P7
or full framework completion; the full goal remains active. Lifecycle skill guides staged ownership
and focused evidence; repository/user restrictions override its general full-regression advice.

## Current authoring and command contract

The master module declaration explicitly lists additional content documents, for example:

```json
{"id":"points","version":"1.0.0","requires":[{"id":"sim.work","version":"1.0.0"}],
 "documents":["points.json","point-construction.json"]}
```

The actual proof master retains its existing production/logistics dependencies; this excerpt shows
the document declaration, not a replacement manifest. `point-construction.json` declares ordinary
target/site entity prototypes, construction effects and typed material requirements. The target
authors `"construction":{"site":"point.tower-site","nodes":[...]}`; the site authors
`"site":{"target":"point.tower"}`, empty inventory with a registered capacity policy and the
selected provider's presence component. Nodes use the existing stable IDs/after/roles/durations/
requirements/effects. The site state aliases its target's authored construction state, not another
state machine. See [actual proof JSON](../../tests/ExtensionProof/content/point-construction.json).

```csharp
// Engine-owned immutable intent, not a host project/stock/placement-rule implementation.
var intent = new PlanIntent("build:point.tower",
    Json.Parse("{\"placement\":{\"space\":\"field.points\",\"position\":12}}"),
    "field.construction", 70, true);
seat.Submit(new SubmitPlanCommand { Intent = intent });
// Direct construction uses the same owner but does not fabricate persistent intent history.
seat.Submit(new ConstructCommand { Def = "point.tower", Priority = 70,
    Placement = Json.Parse("{\"space\":\"field.points\",\"position\":12}") });
```

These are alternative submissions, not instructions to submit both to the same occupied placement.
Tile geometry supplies `{space,x,y}` instead; the selected provider owns decoding/bounds/identity.
Activity intents supply `{target}` through ActivityDef's existing shared player admission. Plan
commands retain their stable registration IDs; breaking payload/API/save identities have no old reader.
