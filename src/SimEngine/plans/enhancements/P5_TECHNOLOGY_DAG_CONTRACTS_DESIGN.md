# P5-A29 — Existing technology DAG routes and iterative metadata visibility

## 0. Problem statement

Oil ResearchView recursively traverses prerequisites with a hard64-depth cutoff and no technology
cycle validation. The framework already owns ITechnologyDefinition/TechnologyDef/TechnologyService
and DependencyGraph; duplicating them is prohibited.

## 1. Goals

Reuse the existing topology compiler for uncapped deterministic closure in native and current Oil
routes. Validate missing/cyclic/duplicate prerequisite declarations before play. Preserve known
targets, private metadata and command readiness. Native Oil research consolidation remains required.

## 2. Design

DependencyGraph.PrerequisiteOrder uses an iterative stack and captured canonical topological IDs;
shared prerequisites appear once. Native TechnologyService retains one derived graph and implements
ITechnologyKnowledge.CopyRoute; TechnologyView delegates with typed results and cleared outputs.
Current Oil TechDef implements existing IDependencyNode/IContentDependencyDefinition with read-only
prerequisite/unlock/effect metadata. Original Research owns its existing point/known state and one
derived graph, refusing duplicate unlock owners. ResearchView delegates; recursion/depth64 removed.

PlayerView remains the sole metadata-visibility owner; its recursive dependency walker is replaced
by a transient iterative stack, retaining original purity/actual-reference/alias checks and depth-first
decision order. Oil checks the root closure once rather than repeatedly traversing every ancestor.
No new graph compiler/catalog/permission owner, reader facade, ledger, scheduler or persistent graph.

A duplicate shared technology reader was briefly drafted without inspecting Technology.cs, failed
compilation and was removed completely. This stage does NOT pretend Oil point-based research was
migrated to native activity-based technology. Existing Oil TechDef/Research/Lab remain explicit
migration debt, not a compatibility facade; no new duplicate native contract/reader survives.

Core2.548.0/save156/replay1; native and Oil research views2, existing state codecs unchanged.
Canonical Oil tech JSON/points/fees/effects remain unchanged. Era is now required authored metadata.

## 3. API surface

Existing ITechnologyKnowledge gains CopyRoute; both native TechnologyView.RouteTo and Oil
ResearchView.RouteTo return Code and detached identities. DependencyGraph.PrerequisiteOrder includes
target in its closure; service filters actual known state and honors already-known target completion.

## 4. Files

Existing dependency graph/TechnologyService/TechnologyView/Module/PlayerView/build marker.
Oil Research/Views and mechanical readonly collection callers; new exact public route case.
Stage/evidence/master/audit. No new TechnologyDefinitions.cs survives.

## 5. Errors

Cycles/missing identities/duplicate edges reject through original graph/reference machinery.
Oil duplicate unlock owners refuse, never last-wins. Unknown/unseated/private route clears output.
Already-known target returns successful empty route; no unknown prerequisites are invented.

## 6. Compatibility

No new compatibility shim or second technology definition. Original Oil progression still needs
migration to the existing native owner; do not label that unfinished architectural work complete.

## 7. Verification

FINAL1513/0/0 in7587ms/observer7734ms. One new exact case exercises257-depth JSON graphs, reordered
declarations, diamonds/shared ancestors, known prefix/target, saved active Oil continuation, native
graph saves, pure routes and invalid/cyclic/duplicate ownership authoring.
A native minimal mapless technology game and actual24x24 Oil game use public commands/views only.
Older/native physical research/private-provider/fault/stress/full-size/whole-game suites not rerun.
[Evidence](../../docs/migration/P5_TECHNOLOGY_DAG_CONTRACTS_VERIFICATION.md).

## 8. Risks

A finite proof is not an infinite-scale performance guarantee. Graph memory remains input-dependent,
without an authored depth limit. Private visibility/provider/fault behavior needs broader acceptance.
Derived graphs rebuild from selected content; company state remains in its original current owner.

## 9. Out of scope

Completing Oil lab/point progression/effect migration into native activities/knowledge, changed
business rules, platform/Draw/host gameplay, old/full/family suites or publication.

## 10. Execution order and remaining architecture

1. Reuse existing graph closure, native route contracts and visibility owner.
2. Replace current Oil recursion/unsafe metadata with captured dependency contracts and validation.
3. Run only the affected public proof; retain failures and update tracker without broad gate closure.
4. Still required: author Oil technology work graphs/facility policies through existing native
   TechnologyDeclaration/ActivityDeclaration; move known/progress/paused studies into CompanyKnowledge/
   WorkBoard; bind Oil modifiers/lab operations through contracts; remove Oil TechDef/Research/Lab
   duplicate generic progression; preserve optional Tycoon fees and prove physical research.
