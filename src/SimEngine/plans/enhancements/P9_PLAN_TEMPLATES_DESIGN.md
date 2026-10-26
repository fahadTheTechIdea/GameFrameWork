# P9-A3b — Portable engine-owned plan templates

Historical stage evidence. Its existing contract/physical callbacks are now included in the two
focused [A4b recurring template gates](P9_RECURRING_TEMPLATES_DESIGN.md); current source expansion
is shared by manual placement and recurring groups. This report is not an additional regression run.

Status: focused construction-template slice verified on 2026-10-05. Build 2.86.0/save56/work-view29;
Work22 and other native layouts unchanged. 80 checks/two exact suites (59 contracts;21 physical),
clean build. [Evidence](../../docs/migration/P9_PLAN_TEMPLATES_VERIFICATION.md).
Full P1–P12 goal remains active/partial.

## 0. Problem

WorkPlanGroups controls native intent groups, but hosts cannot capture/translate reusable layouts
without reconstructing provider-owned geometry. Templates must not copy entity IDs, jobs or stock.

## 1. Goals

Capture an owned group as immutable prototype/relative intent; export/read one JSON schema; author
templates through the master catalog; place through one normal command and existing whole-group
admission. Prove tile and independent point providers, private refusal, physical construction and saves.

## 2. Design

IPlanTemplateDefinition is optional on the existing IPlanDefinition. It validates portable arguments,
captures relative intent and expands at an explicit provider-owned anchor. ConstructibleDef implements
it through optional ISpatialPlacementTemplates on the selected spatial authority. Neither generic
board nor host interprets geometry. Activity entity-ID intentions without this capability refuse.

PlanTemplate is an immutable ordered value with stable entry IDs, definition, provider arguments,
priority and pause only. An explicit space identity (or null for nonspatial intent) selects applicability;
unselected spatial content can coexist without forcing unrelated providers into the current world.
One JSON reader/writer serves master PlanTemplateDef, captured export and
the command codec. No template service, execution store, scheduler, stock/progress copies or group
ledger. Master definitions validate against composed plan resolvers before Initialize. Captured values
are portable player-authored content; placing freezes them in the ordinary command inbox and expands
to existing PlanGroupIntent. Entire expansion/admission precedes native group submission.

Tile geometry authors long relative dx/dy but places only representable TileCoord; point geometry
uses native fixed-point offsets. No cross-space conversion, default anchor, cap, rotation or live IDs.
Fix.ToExactString provides exact native-bit decimal authoring; log-format ToString is not geometry.
Capture-generated entry labels are local ordinal identities, never copied plan/entity references.

## 3. API

IPlanBoard.CapturePlanTemplate(company, group, origin, out template) returns private typed diagnostics;
PreviewPlanTemplate/PlacePlanTemplate take explicit value, anchor and fresh group. WorkView exposes
those pure operations plus immutable master template lookup. PlacePlanTemplateCommand alone mutates.

## 4. Files

PlanTemplates.cs, WorkPlanTemplates.cs, PlanTemplateCommands.cs; extend PlanContracts, ConstructibleDef,
spatial contracts/TileMap/PointSpace, WorkBoard composition, WorkModule/WorkView/engine identity;
two exact suites, master-listed proof JSON, docs/master/README/AGENTS/help.

## 5. Errors

Malformed schema/overflow/space mismatch -> InvalidArgument; missing definition -> UnknownDef;
unsupported workflow -> work.plan-template-unsupported; foreign capture -> NotFound without entries;
all per-entry expansion refusals prevent any prefix. Native duplicate/fresh group/range/placement
checks remain unchanged. Provider violations fault Update; no fictional rollback.

## 6. Compatibility

Direct command/view identity migration. No old reader/shim. Existing Work22 codec unchanged.

## 7. Verification

Build; only `Plan templates: portable JSON, private capture and whole admission` and
`Plan templates: physical placement and saved continuation`. Other suites compile only.

## 8. Risks

Custom providers must uphold portable schema/no-live-ID semantics; native construction is proven,
not arbitrary external providers. Group admission is not simultaneous physical placement.

## 9. Out of scope

Network/survey/upgrade/removal template providers, cross-entry links, rotation, automatic repeat,
template library editing, global purity/fault/replay/stress/full roadmap gates. No Oil/platform/Draw.

## 10. Stages

- [x] A3b.1 Immutable JSON/template contracts and selected provider geometry.
- [x] A3b.2 Same-board capture/whole placement/private command boundary.
- [x] A3b.3 Exactly two focused suites and evidence/documentation.
- [ ] Remaining template providers/cross-links/rotation/automatic repeat/provider/fault/replay/stress/full gates.
