# P11-C4 — Private expansion-site selection

Status: focused slice verified,2026-10-07.49 checks/ONE exact NEW bounded case0 failures/aborts664ms.
TWO builds include test-helper ambiguity repair; final clean build2.225.0/save129. Controller view3/
work view77; native codec3/payloads unchanged. Earlier gates historical; full goal remains active.

## 0. Specification

Native playbook steps bind one fixed anchor. Ordinary template submission records intent even when
physical placement is occupied: WorkBoard.CheckActivation, not submission, owns current readiness.
Replace fixed-anchor authoring with public JSON/content site-source contracts on the SAME playbook.
Select viable alternatives using existing normal submission AND actual provider activation previews.
No new AI/placement/geometry/plan/resource authority, no business or platform behavior.

## 1. Goals

1. Explicit sites reference per step, no anchor/default/legacy reader. Native master-authored sets
   contain stable IDs/priorities/frozen provider anchors; public pure sources can expose private sites.
2. Existing WorkBoard exposes pure per-entry current template activation checks using IPlanDefinition.
   No concrete construction lookup, consumption, reservation or execution during observation.
3. Read/decision share candidate evaluation, explicit priority then stable ID, skip currently blocked
   site and report each actual blocker. Retained facts are not placement grants.
4. Native pending/accepted links remain sole persistent playbook state/codec3. No selected-site ledger:
   actual queued command and accepted plan arguments own placement. Reconsider only unissued/refused work.
5. Ordinary companies compete for finite authored viable positions with physically supplied/timed work;
   exhausted alternatives wait, never free-grant/teleport/auto-cancel/rewrite accepted native plans.

## 2. Design and API

New Standard/PlanSites.cs: IPlanSiteSource/PlanSiteSourceDef, AuthoredPlanSitesDef,
immutable PlanSiteCandidate/PlanSiteObservation/PlaybookSiteSnapshot, guarded pure observation.
Validate actual content binding, selected space/template structure, distinct IDs and explicit priority.
No global provider registry or persistent source state.

Existing public PointSpace.CheckPlacement now checks same-position structural occupancy in that
SAME geometry provider (excluding actual replacement); it formerly validated bounds only.
This is engine-side extension geometry, not a harness/controller collision rule. Mobile entities
retain existing point movement; no hardcoded engine map size or new universal geometry assumption.
Public DelayedPlanSitesDef exposes authored sites after explicit GameClock duration; no second clock
or copied site table. Two real companies/machines/sources start through actual master content.

Revise existing PlanPlaybooks.Step to sites reference. Read evaluates sources via the same private
callback guard as conditions. Current activation preview expands the SAME template/provider intent
and invokes existing IPlanDefinition.CheckActivation under the existing pure template guard.
WorkView delegates actual company/private template selection. Candidate facts carry exact normal
blockers/anchors, selected currently ready ID; pending/executing nodes report no fresh alternatives.
Controller owner delegates diagnostics; PlayerDecisionContext guards new observation method.

Controller view3/work view77; native payloads unchanged. Current engine identity advances; no shim.
Migrate all old authored step JSON and directly affected older authoring fixtures, compile only.

## 3. Files/sequence

1. This document/master task; PlanSites/contracts/shared activation preview.
2. Same native playbook/context/controller/module/view; native JSON fixtures/public source proof.
3. One NEW exact bounded player/rival physical scenario, metadata/build.
4. Record every invocation, final green freezes source/tests, then tracker/evidence/current inventory.

## 4. Errors and risks

Missing sites/reference/IDs/negative priority/invalid geometry: ContentException before first Update.
Public source invalid facts or pure mutation fault, no fallback. No eligible sites: typed wait.
Normal next-Update race rechecks intent/activation; no candidate reservation/hidden bonus.
An already accepted plan can wait for physical placement; it is NOT silently moved to another site.
This is viable-site choice, not path-cost optimization, generated geology, full risk/maintenance planner,
automatic cancellation/recovery, resource competition or arbitrary provider/privacy/CPU sandbox.

## 5. Verification

ONE distinct exact NEW bounded case after build:
```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Player controllers: private site selection, ordinary rival competition and physical expansion"
```
Repairs/reruns only actual new failure; all invocations recorded. Older/family/full/platform gates
remain unrun. Native codec3 unchanged, no test/source edit after final green. Full plan remains open.

[Parent](P11_PROGRESSION_KNOWLEDGE_CONTROLLERS_DESIGN.md), [master](MASTER_TRACKER.md).

## 6. Execution

- [x] Public site contracts/native sets, guarded private observations and same board activation preview.
- [x] Same native playbook/diagnostics/context; public source/geometry and JSON migration.
- [x] ONE new exact physical player/rival gate; all invocations recorded, source/test freeze after green.
- [x] Tracker/documentation/current source inventory; full P11/framework goal still open.

[Verification](../../docs/migration/P11_PLAYBOOK_SITES_VERIFICATION.md). No full/regression/platform
claim. Source-only older fixture migration does not prove those runtime suites green.

