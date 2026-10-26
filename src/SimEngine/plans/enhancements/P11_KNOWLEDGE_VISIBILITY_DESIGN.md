# P11-D2 — Authored knowledge visibility on the existing projection/admission owners

Status: focused D2 slice verified,2026-10-06. Full P11/P1–P12 remains partial.
105 checks/two exact visibility suites, clean build;
[evidence](../../docs/migration/P11_KNOWLEDGE_VISIBILITY_VERIFICATION.md).

## 0. Problem

Entity counts/deltas and private layer cursors are already scoped. PlayerView.CopyCatalogue and other
definition queries still expose authored definitions independently of learned knowledge. IVisibility
only admits entities, and normal access admission cannot enforce discovery visibility consistently.

## 1. Goals

1. Extend the existing IVisibility with typed content visibility, not a parallel fog/knowledge service.
2. Explicit master-listed rules/policy select entity/content discovery and optional own-entity visibility.
3. Native rules derive from the sole learned ledger; no saved visibility/unlock table or new game loop.
4. Catalogues, known-definition queries and native admission return no hidden definition/recipe details.
5. Real physical discovery reveals native snapshots/counts/inspect/catalogues and saves consistently.

## 2. Design

IVisibility retains CanSee(World,viewer,Entity) and adds CanSeeContent(World,viewer,IContentDefinition).
PlayerView reuses that single selected policy for its definition reads. ContentAccessPolicy checks
the same discovery gate before other access rules, returning UnknownDef for hidden content. Existing
technology access remains distinct: known content may still be locked. Observer is exactly PlayerId.None.

Optional sim.knowledge-visibility requires sim.knowledge. Explicit scenario settings select one
knowledge_visibility_policies profile. Required own_entities/unlisted_entities/unlisted_content/rules
have no inferred gameplay values. Open IVisibilityTarget/VisibilityTargetDef contracts match exact
resolved content and validate binding. Native entity/activity/transformation targets resolve authored
knowledge_facts, all/any modes and stable IDs; they read actual company facts from ICompanyKnowledge.
Rules use the existing ContentTypes/ResolvedContent aliases; no provider registry or copied learned state.

One KnowledgeVisibility policy implements IVisibility/ICompositionValidator and evaluates matching
targets deterministically. Multiple matching rules are all required; unlisted behavior is explicit.
Own-entity visibility is an authored entity observation allowance, not an automatic catalogue unlock.
Entity snapshots/spawn/removal diffs reuse the existing GameSnapshot/seat known-ID list.

## 3. API

Breaking IVisibility content method; IVisibilityTarget, VisibilityTargetDef, KnowledgeVisibilityPolicyDef,
typed native targets and KnowledgeVisibilityModule. Build2.93.0/save61, no new state codec.

## 4. Files

Kernel/Visibility.cs; Standard/KnowledgeVisibility.cs/module and ContentAccess.cs; Hosting/PlayerView.cs;
native technology/transformation/work/tile definition queries; public proof fixtures/JSON and two
visibility suites; stage/master/evidence/parents/README/help/AGENTS. Existing current identity assertions
migrate/compile only.

## 5. Errors

Missing policy/settings/rules/facts, duplicate rule IDs, invalid modes and foreign content/session refuse.
Hidden content and unknown content have the same UnknownDef admission/no-details response. Missing
knowledge capability refuses before Initialize. Refusals/read queries do not alter state or unlocks.

## 6. Compatibility

Clean IVisibility contract revision; migrate all actual providers. No overload/shim/legacy policy or table.

## 7. Verification

Standalone build; exactly two named suites:

- Visibility: authored knowledge contracts and hidden definition admission
- Visibility: physical discovery, scoped snapshots and saved continuation

Bounded native physical stages include diagnostics. No third/family/full run.

## 8. Risks

Provider callbacks are engine-side trusted contracts; broad mutation/CPU/fault matrices remain open.
Admission order must not reveal hidden prototype capability/bounds/recipe details. Existing execution
already admitted before a visibility change is not silently cancelled. Knowledge is first-learned;
dynamic forgetting/refresh requires a later explicit model.

## 9. Out of scope

Spatial cell-level discovery/partial layers, route/fleet fog, private arbitrary custom feature auditing,
dynamic surveys/refresh/sharing, pressure/recovery, Oil/Tycoon/platform/Draw and full regression.

## 10. Execution

1. [x] Revise the one visibility contract and add authored native rule/policy composition.
2. [x] Scope definition queries and normal content admission through that owner.
3. [x] Prove typed identity/privacy and real saved physical discovery using public extension contracts.
4. [x] Run two exact gates, record evidence and update linked tracker/docs.

65 contracts/40 physical pass, zero failures/aborts. Public resource predicates use actual historical
stock observations; native entity/activity/recipe rules use authored facts. Real discovery spends8
materials and reveals exact existing IDs through native snapshots/counts/entered-sight deltas. All/any,
independent technology locks, private menus/inspect, saved twins and no-work/no-space composition pass.
An identity test initially reused one factory's legitimately shared immutable definitions; corrected
to an independent catalogue, not changed engine identity rules. Wider information-flow auditing is
still open: definition relationship payloads, previously executing recipes, arbitrary custom features,
event/ID side channels, location/cell discovery and provider purity/fault matrices are not proved here.
