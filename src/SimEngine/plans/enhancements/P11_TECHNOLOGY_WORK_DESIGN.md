# P11-A1 — Optional technology progression through the existing work DAG

Status: focused native technology slice verified on 2026-10-05; full P11/roadmap active/partial.
Build2.87.0/save57; technology-state1/view1, Work22 and all other native layouts unchanged.
76 checks/two exact suites, zero failures/aborts; [evidence](../../docs/migration/P11_TECHNOLOGY_WORK_VERIFICATION.md).

Current ownership is revised by [P11-D1](P11_COMPANY_KNOWLEDGE_DESIGN.md),2.92.0/save61:
CompanyKnowledgeService/Sync alone owns learned records. TechnologyService no longer has a learned
table or IPersistentService; its existing research/access rules and chronology validate the same ledger.
146 checks/two exact extended technology suites pass; [current evidence](../../docs/migration/P11_COMPANY_KNOWLEDGE_VERIFICATION.md).
The original A1 identity/count and ownership description below are historical, not a second active path.

Current technology content ownership is revised by [P11-A2](P11_TECHNOLOGY_CONTENT_CONTRACTS_DESIGN.md) on2.332.0/save154/replay1: service/linked requirements/objectives use actual contract aliases, native and independent definitions share composition validation, and explicit master-listed independent research/unlocks use ordinary work and private knowledge. FINAL69 focused checks, no old runtime rerun. [Evidence](../../docs/migration/P11_TECHNOLOGY_CONTENT_CONTRACTS_VERIFICATION.md). Full P11 remains OPEN.

## 0. Problem

The framework has no technology prerequisites/knowledge, while ContentAccessPolicy exposes a mutable
delegate list and untyped keys. Research must not add another worker/material/timer/progress scheduler.

## 1. Goals

Optional master-listed technology DAG, typed unlock providers, company-private learned knowledge;
real research through existing activities/requirements/material supply; once-only completion, private
views, cancellation/pause and saved continuation. No lab, money, population, rendering or economy default.

## 2. Design

TechnologyDef extends ActivityDef and implements ITechnologyDefinition/IDependencyNode. It authors
explicit prerequisite and unlock arrays and the actual existing work graph/final completion effect.
DependencyGraph remains sole topology/cycle validator. Ordinary PostOrder/plan commands and WorkBoard
alone own active research, machines, claims, materials, supply, progress, pause and cancellation.
One optional TechnologyService/ITechnologyKnowledge owns completed knowledge only through one Sync;
active exclusivity/progress derives native orders. No research queue, progress counter or loop.
Native PostOrderCommand uses the bare activity ID; persistent PlanIntent uses its existing activity:
provider key. Both converge on the same native admission/execution. No technology command family.

TechnologyUnlockDef is an open provider contract; typed native entity/activity/transformation unlock
subclasses resolve existing catalog aliases. IContentDefinition on Def gives the existing catalog a
common identity contract; IContentAccess now accepts actual resolved aliases, not ambiguous strings.
Declared IContentAccessRule services replace mutable delegates; one ContentAccessPolicy composes the
selected base policy and stable rules. TechnologyRequirementDef plugs into the existing all/any evaluator.
Technology completion is the authored final WorkEffect and must see a real Completing order/all nodes
Done. Nothing grants instant initial/admin research. Knowledge is monotonic and company-private.

## 3. API

ITechnologyKnowledge.CheckKnown/CheckResearch/Complete and detached CopyTechnologies; TechnologyView
exposes private facts and previews through ordinary activity admission. No new player grant command.

## 4. Files

Technology contracts/definitions/service/module/view; ContentAccess/Def/catalog/callers; public JSON
proof and two exact suites; engine identity, stage/master/evidence/README/AGENTS/help.

## 5. Errors

Missing/cyclic/duplicate references and invalid completion/unlock schemas refuse composition;
unknown/foreign content aliases refuse access; missing prerequisites/known/active research return typed
blockers. Completion without actual physical graph completion refuses with no learned record.

## 6. Compatibility

Direct typed API/identity migration; no string overload, Rules list, old reader or compatibility path.
Oil remains untouched under the current user scope; P5 must replace its generic research owner during
adoption. This optional standalone owner is not registered in any oil world this turn.

## 7. Verification

Build; only `Technology: authored contracts, access identities and private knowledge` and
`Technology: physical research, unlocks and saved continuation`. Previous suites compile/migrate only.

## 8. Risks

Research shares ordinary work admission, not separate authored placement. Provider effect breadth,
utility/pricing/modifier/replay/stress/corrupt cross-owner state matrices remain later gates.

## 9. Out of scope

Technology parameter effects, alternative research providers, starting technologies, repeat research,
objective/knowledge/fog/controller/pressure modules, Oil/P5/platform/Draw, full roadmap acceptance.

## 10. Stages

- [x] A1.1 Typed content access/rule composition and immutable technology/unlock DAG contracts.
- [x] A1.2 Native research/once-only knowledge/requirements/private views and sole Sync.
- [x] A1.3 Two exact suites, focused evidence and docs.
- [ ] Wider independent unlock/provider and transformation-unlock execution proofs.
- [ ] Parameter effects, objectives, general private knowledge, controllers and pressure composition.
- [ ] Cross-owner corrupt-state/fault/replay/stress/full P11 acceptance and P5 adoption.
