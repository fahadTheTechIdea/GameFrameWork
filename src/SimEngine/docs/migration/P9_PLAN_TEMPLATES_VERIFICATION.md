# P9-A3b — Portable plan template evidence

Date: 2026-10-05. Current working-source evidence, not committed HEAD or full regression.
Standalone framework only; no Oil/Tycoon/platform/Draw/host gameplay implementation.

## Identity and ownership

Build2.86.0/save56/work-view29. Work22, transformer6/manager2/transformation-view7, logistics5/
logistics-view10/resource-flow5 and other native layouts unchanged. New command/view semantics
advance current identity directly, with no old reader/default/compatibility wrapper.

WorkPlanTemplates.cs is a partial WorkBoard file. It stores no template execution/group/geometry/
stock/progress state. IPlanTemplateDefinition extends existing plan providers; ConstructibleDef uses
ISpatialPlacementTemplates on the selected authority. Native tiles and public-only PointSpace own
their geometry, and the existing IPlanBoard/private WorkView own capture/placement decisions.
PlanTemplate/PlanTemplateEntry are immutable portable values. PlanTemplate.Read/ToJson are the sole
JSON schema, reused by master PlanTemplateDef, captured export and command Sync. Master definitions
are explicitly document-listed and validate through composed plan resolvers before Initialize.
Explicit space selection permits unrelated spatial content without implicitly composing its providers.

Capture checks actual company group records, retains ordered prototype/relative arguments/settings
and generates local stable entry labels, not live plan/entity links. Active/terminal capture uses original
intent, not actual site state. Unsupported entity-ID activities refuse rather than copying target IDs.
PlacePlanTemplateCommand snapshots value/anchor through ordinary command serialization. Entire
expansion and existing group admission precede native submission; group/site/order/claims/jobs/time/
progress/material/tool/cargo/cancel ownership remains unchanged. Whole intent admission does not
guarantee simultaneous physical placement or jointly reserve overlapping footprints.

Tile relative offsets use long arithmetic; final coordinates must fit the actual TileCoord representation.
Point geometry uses fixed point. Fix.ToExactString round-trips every native bit, including negative
and extreme raw values; debugging ToString is unchanged. No hidden geometry/template count cap.
Pure template callbacks guard entity/plan/order/job allocation even on refusal. This is not global
detection of every provider mutation of unrelated state; broad provider/fault matrices remain open.

## Exact final gate

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Plan templates: portable JSON, private capture and whole admission"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Plan templates: physical placement and saved continuation"
```

Final build: zero warnings/errors,5.24s.
Contracts:59 passed,0 failed,502ms.
Physical:21 passed,0 failed,493ms.
Total:80 checks/exactly two suites, zero failures/aborts. Only these names were repeated after the
explicit spatial-selection refinement. 26 prior-suite identity assertions were mechanically migrated
and compiled only. No third/family/full suite ran; previous stage evidence was not rerun.
Physical waits are bounded1200 updates with tick/plan/status/blocker diagnostics.

## Proven

Explicit master document provenance; no missing-template fallback; required immutable JSON fields,
stable unique entry IDs, frozen nested exports/captured lists and exact round-trip; pure previews and
capture; per-entry late unknown-definition refusal/no prefix/no allocated IDs; native joint duplicate,
fresh label, selected-space, geometry and bounds refusal; private foreign capture with no IDs/value;
relative point/tile translation; active/terminal capture without live IDs/site/progress/stock; unsupported
entity-ID workflow; selected invalid master definitions refusing before Initialize; unselected provider
content coexisting; exact native scalar/fractional point/full tile-difference representation and overflow.

Saved pending template command and caller anchor mutation isolation; normal native sites/orders;
physical machine travel, supply and timed work; group pause and isolated saved continuation; actual two
structures complete once and consume16 of20 supplied inputs, with no material fabrication; terminal
capture after retired sites; fresh placement without copied completion/progress; native cancellation.
Tile placement/capture is contract-tested; the new physical construction scenario uses point geometry.

## Still required

Network/corridor/survey/upgrade/removal template providers, cross-entry links, rotation, editable
template libraries, automatic repeat; wider provider purity/fault/replay/stress/allocation matrices;
tile template physical stress; remaining P9 and full P1–P12 acceptance. Full goal remains active.
