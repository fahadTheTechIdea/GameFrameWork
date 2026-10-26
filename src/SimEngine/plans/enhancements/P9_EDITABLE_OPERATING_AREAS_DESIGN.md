# P9-A1b — Company-owned editable operating areas

Status: focused implementation verified,2026-10-06. Parent: [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Clean build2.151.0/save79;429 checks/two exact suites, zero final failures/aborts.
[Evidence](../../docs/migration/P9_EDITABLE_OPERATING_AREAS_VERIFICATION.md).

## 0. Problem statement

`WorkPolicyRules` currently resolves runtime areas from shared immutable content. Players cannot
create or paint geometry, and two companies cannot independently edit the same authored starting area.

## 1. Goals

1. One session-owned `IOperatingAreas` authority stores company-private, stable named areas.
2. Rectangles, painted tiles and public mod shapes reuse their existing readers, validation and
   containment implementations, not duplicate geometry algorithms.
3. Commands and immutable WorkView previews share validation, optimistic revisions and typed blockers.
4. Edits retain actual work/cargo/receipts and re-evaluate existing physical routes and dock intentions.
5. Area state, pending commands and policy references restore/hash deterministically through one codec.

## 2. Design

JSON `SpatialRegionDef` definitions are explicit initial geometry and geometry prototypes. Each company
receives its own initial records for the selected spatial space. Runtime work-policy IDs resolve only
through `IOperatingAreas`, never through a content fallback. Fixed authored activity site requirements
remain authored prerequisites, not player operating policies.

`ISpatialRegionGeometryFactory.CreateGeometry(id, JsonReader)` returns immutable version geometry and
canonical shape JSON. Native and public providers construct the same definition class using the base
protected builder, reusing its reader, resolved space validation, Check and CheckTravel. Painted shapes
normalize tile ordering. No engine switch on coordinate representation is introduced.

`OperatingAreas` owns sorted `(company, name)` records, per-company monotonically increasing revision
sequences, and one embedded persistence responsibility. Revision is an edit identity, not game time.
Create uses expected revision -1; edits/removal require the current revision. Removal refuses an area
referenced by a current work policy or required by future authored policy initialization.
Recreating a name cannot validate an older edit. Geometry is a
decoded projection of canonical shape intent, never a second editable authority.

After committing an edit, existing WorkSystem.RefreshPolicy rechecks affected jobs/home dispatch and
marks WorkBoard dirty. Native movement already validates its actual path against current geometry
before propulsion, and reopens its existing A* owner when a permitted detour is available. There is
no additional system, game loop, clock, route store, teleportation or instant repair.

## 3. API surface

- `SetOperatingAreaCommand`: Name, Template, ShapeJson, ExpectedRevision.
- `RemoveOperatingAreaCommand`: Name, ExpectedRevision.
- `IOperatingAreas`: Resolve, Read, Copy, PreviewSet/Set, PreviewRemove/Remove.
- WorkView: immutable area listing/read and matching previews, scoped to its viewer.
- Runtime WorkPolicyRules.CheckSettings accepts actual World/Entity; authoring validation remains
  content-based. Saved policy existence is checked after all services decode, not during entity decode.

## 4. Files to change

Content/SpatialRegionDef.cs, Kernel/SpatialRegions.cs, Map/TileRegions.cs, Standard/OperatingAreas.cs,
Standard/WorkPolicies.cs, Standard/WorkModule.cs, Standard/Work.cs, Commands/OperatingAreaCommands.cs,
Views/WorkView.cs, public PointRegionProof, RouteOperatingAreaTests, identity declarations and docs.

## 5. Errors

Invalid company/name/shape, unknown or incompatible template, stale revision, unsupported factory,
and referenced removal return typed refusals before commit. Contradictory provider output throws.
Invalid/noncanonical/out-of-range saved geometry or missing policy references reject restore.

## 6. Compatibility

No legacy API, content fallback or old save reader. New persistent service changes the save manifest.

## 7. Verification

Clean solution build, then only these two complete named suites; same-name repairs allowed:

- Route areas: provider contracts, admission and private diagnostics
- Route areas: physical constrained travel, cargo and saved continuation

Extend with creation/edit/removal, private revision checks, rectangle/paint/public provider geometry,
invalid shape purity, loaded cargo interruption/reopening, pending commands, saved twins and sole-clock
pause/session isolation. Full regression/replay/stress/provider rollback remains pending.

## 8. Risks

Entity decode precedes service decode: defer runtime reference validation to existing OnLoaded.
Provider geometry must be immutable and pure; arbitrary malicious provider global rollback is not proven.

## 9. Out of scope

Oil/Tycoon/hosts/demo/rendering/platforms/Draw, preferred routes, congestion, fleet service/fuel budgets,
world sizing, extra clock, full roadmap completion.

## 10. Execution order

- [x] Shared geometry factory and area authority.
- [x] Runtime policy, command, preview and save integration.
- [x] Extend and run the two gates after a clean build.
- [x] Record evidence and align master/parent/guidance.
- [ ] Broader providers/layers/deltas/replay/stress/full P9 acceptance; not inferred from focused gates.
