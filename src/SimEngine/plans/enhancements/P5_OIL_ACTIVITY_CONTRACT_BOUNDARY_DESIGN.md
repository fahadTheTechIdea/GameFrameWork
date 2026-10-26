# P5-A47 — Oil activity contracts and external runtime boundary

## Incremental P5-A49 — native scoped individual technology facts

The original TechnologyService now implements `ITechnologyKnowledge.ReadTechnology(viewer, company, technology, out snapshot)`. A single private fact projection serves both that contract and CopyTechnologies, using original knowledge, active work, prerequisite disclosure and callback observation. Public TechnologyView offers own-company and explicit-company reads; only the observer can read another company's actual enrollment. Missing, undisclosed, foreign and invalid owners return NotFound with a cleared snapshot. No separate learned-state authority or fabricated observer company.

Oil ResearchView.Has uses this scoped fact contract, not the engine-internal CheckKnown query. Authored starting knowledge remains in the original ledger even when content is hidden. Native catalog output also clears before terminal refusal. Existing physical effects and internal knowledge requirements continue to use actual learned state; visibility does not delete knowledge or change gameplay completion.

Focused proof: native single/catalog parity and saved isolation31 checks; public physical prerequisite discovery, timed research, forgetting/re-discovery and replay71 checks; hidden authored Oil knowledge, observer facts, saved state and terminal output36 checks. The initial Oil34/2 failure correctly found stale output at the native view's pre-service guard and is retained in evidence. Broader private query/provider/fault/durability gates remain open. Build2.571.0/save157/replay1; no state payload or compatibility path added.

## Incremental P5-A48 — domain read-view boundary

Problem: some Oil read views bypassed the original fault guard on missing/private/empty branches, published partial collection prefixes, or enumerated undisclosed technology metadata directly.

Implementation: existing CompanyView, ExplorationView, ResearchView and PlanningView guard through World.RequireHealthyUpdate; existing collection methods clear outputs and assemble local detached facts before final publication. Invalid company IDs and observer-only planning never index company books. Scoreboard entity disclosure delegates to PlayerView.TryGetEntity, not a raw visibility callback. Technology menus and unlock lookup use ITechnologyKnowledge.CopyTechnologies, retaining the native complete-disclosure rules and one menu entry per technology. No parallel visibility policy, state store, work coordinator or clock is introduced.

Files: OilCo.Game/Views.cs and Research.cs; OilCo.PublicProof/ViewBoundaryTests.cs and Program.cs; framework build identity and independent protocol JSON. Save157/replay1 and all state codecs remain unchanged; build2.570.0 refuses earlier-build saves as already designed.

Verification: one public focused case checks seat/observer terminal reads, cleared outputs, invalid IDs, authored hidden-content menu, retained detached facts, saved continuation and interleaved session isolation. The affected existing research case checks physical research and saved knowledge. A thin-client case timed out at30s and remains incomplete; no retries/full regression. Callback failure midway through a collection, arbitrary provider purity, complete privacy closure of every domain query, all workflow timing and broad P5/P6 gates remain open. The original acceptance checklist is unchanged.

## 0. Problem

Oil Research.cs queried concrete WorkBoard state and SurveyHandler posted through that concrete coordinator. The public extension boundary must use engine contracts and immutable facts while retaining the original execution owner.

## 1. Goals

Remove remaining Oil WorkBoard dependencies; preserve study pause/resume and direct-command behavior; post survey work through the existing activity-order contract; prove the solution has one external framework runtime and one domain extension assembly.

## 2. Design

Research uses IPlanBoard for intents and IActivityCatalog for actual plan metadata. Its active study is a detached ActivityProgressView obtained through the company's WorkView, including direct generic technology orders and excluding paused linked studies. ResearchView exposes those original facts without returning or inspecting a mutable Order.

IActivityExecutionOrders gains PostActivity, returning the existing ActivityServiceOrder identity snapshot. WorkBoard implements it by calling its sole existing Post owner. No parallel order store, scheduler or activation mechanism. SurveyHandler retains the same admission, stake creation, priority, command result and native graph/effect.

All movement, supply, productive work, consumption, departure and completion remain in the existing engine owners. The API is for engine-side providers after admission; it is not a host mutation surface.

## 3. API and state

Required PostActivity contract; existing implementations migrate in place. Core2.569.0/save157/replay1. No new saved payload or schema. Independent current-engine-protocol JSON declares the actual build.

## 4. Files

Framework ActivityExecutionLifetime.cs, current build/protocol; Oil Research.cs, Views.cs and Commands.cs; public ActivityContractTests.cs and explicit runner registration; existing affected worker-effect assertions and primary P5 evidence.

## 5. Errors

Existing Post validates the actual declared/bound graph, selection and activity start rules. No alternate default, free material, instant workflow or silent refusal substitution. Research's invalid/nonseated viewers return no active study, and scoped views retain privacy.

## 6. Migration

No compatibility wrapper or legacy execution path. Original WorkBoard remains responsible for actual orders; contracts expose only the operations needed by providers and existing detached facts.

## 7. Verification

Focused native research covers real facility gates, switching/resuming earned progress, shutdown/restart, optional fees, direct generic command parity, saved knowledge and invalid authoring. Compiled composition checks verify one OilCo.Game extension, one referenced/loaded Beep.SimEngine runtime, no embedded framework types, no Oil friend access and no public mutable SimGame.World.

Whole-solution source inventory resolves all project references and shared build imports to the canonical external framework. The specific P5.acceptance.001 structural gate may close on that complete evidence; no broader behavior gate is inferred.

Survey proof uses only commands, Update and immutable views/history. Its fixture explicitly authors two seats, a24-tile map, the original seismic/haul prototypes and stock, a six-minute survey duration and one native clock step per Update. It requires physical original charges/supply, positive work, saved continuation and original completion. Longer-distance runtime/performance is not inferred from a nearby survey fixture.

## 8. Risks

Full canonical production/construction/research/survey/rival timing, private projections, faults and durability remain broad gates. Native minute-based travel can require long bounded runs; diagnostics must retain actual phase, cargo and position instead of hiding delays or accelerating a machine.

## 9. Out of scope

Business rules/amounts, host gameplay, draw/platform adapters, alternate schedulers, canonical content speed changes, full regression and Git publication.

## 10. Execution

Implemented and scoped verified: survey116/0, research95/0, composition8/0, compiled clients38/0 and worker effects155/0;412/0 across five literal bounded cases. Survey consumed original charges through real pickup/travel/unload, then six-minute work and saved completion. P5.acceptance.001 closes on complete project/assembly evidence only. Original221 requirements/195 recorded checked/26 open; additional324. Full goal remains open. [Evidence](../../docs/migration/P5_OIL_ACTIVITY_CONTRACT_BOUNDARY_VERIFICATION.md).
