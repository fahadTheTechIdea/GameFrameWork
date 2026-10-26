# P5-A5 — Native incidents and fleet manufacturing evidence

Date: 2026-10-13. Core remains 2.531.0, save schema 154, replay schema 1.

Three existing Oil source files changed. Wear no longer implements IIncidentResponse or owns Failure, FailedTick, episode state, response selection, repair completion or repair posting. Native Incident owns episode facts and codec; IncidentResponseSystem/WorkBoard own response work. Oil retains authored hourly probability decisions and inspection of actual native facts.

Five existing JSON files changed and one workflow file was added to the existing master manifest. There are 26 entities, 30 kind/source/probability bindings and four workflows referencing the existing repair node IDs. Response completion uses bound_response; storyteller uses named wear.breakdown/wear.blowout. Source-level JSON inspection reports no missing source/workflow or kind mismatch. Engine content loading has NOT run.

Autopilot fleet manufacturing uses original private OperationsView and TransformationManagementView, ordinal capability selection and EnqueueTransformationCommand. No direct producer part lookup, fabricated queue view or compatibility command remains in this method. Other autopilot features still require current API migration.

PowerSource accepts IAutomationSignalContent; supported metrics and typed unavailable results are unchanged.

## Observed validation

One completed scoped build:

```text
dotnet build OilCo.Game/OilCo.Game.csproj -c Release --no-restore
```

Exit 1, zero warnings, one reported error, 1.28 seconds: Fog lacks IVisibility.CanSeeContent. The preceding seven declaration errors reduced to one; this does not prove body compilation or runtime correctness.

Changed Oil paths diff-check exit 0 with one line-ending warning. The first generated JSON edit was refused because a trailing comma was missing from its match context; no source was deleted, and the corrected edit preserved the comma. No test executor, broad filter or full regression.

## Remaining requirements

Replace duplicate Fog with the native visibility/sensor/knowledge composition, migrate the master modules and activity/content schemas, then resolve body/API errors. Verify actual timed repair, cancellation, response episode history, concurrent incident behavior, private fleet commands, save/load/replay and complete oil acceptance.

Original 221 criteria remain 193 recorded checked / 28 open. No original gate closed. Framework/clock/Tycoon/hosts/platform source unchanged; no staging, commit or push. One final full regression remains authorized only after implementation finishes.

Metadata validation: 1,978 source anchors, 13 quickstart anchors and 1,136 selected inputs match recorded hashes. All 221 original criterion text/checkbox/line anchors validate unchanged. Eighteen master rows retain their table shape and 8,858 local Markdown links resolve. Framework diff-check exit 0 with 34 existing line-ending warning lines. All 12 frozen Tycoon sources and the game clock are unchanged; staged files 0. These are source/document checks, not gameplay acceptance. A metadata edit was also refused on trailing-comma context and corrected without a source mutation; two refused edits are retained.
