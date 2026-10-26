# P11-A3 — Shared progression declarations

Status: scoped verified on2.411.0/save154/replay1, FINAL133/0/0 in3022ms. Original P11.acceptance.001 is verified; other compound/full gates remain OPEN. [Evidence](../../docs/migration/P11_PROGRESSION_DECLARATIONS_VERIFICATION.md).

## 0. Problem

TechnologyDef and IndependentTechnologyDef duplicate prerequisite/unlock parsing and resolution. ObjectiveDef and IndependentObjectiveDef duplicate dependency/predicate/deadline/effect parsing and resolution, with weaker independent validation. File-authored extensibility must not require a second parser or permissive mod path.

## 1. Goals

One TechnologyDeclaration and one ObjectiveDeclaration serve native and independent contract providers. Both preserve strict keys, explicit arrays/references, stable ordering and typed early errors. A new explicitly master-listed progression document uses original work, stock, knowledge, objectives and clock; no coordinator edits for the new content.

## 2. Design

Move existing metadata readers/resolvers into declaration classes in their existing owner files. Native/independent definitions delegate them; existing ActivityDeclaration still owns research DAG authoring. TechnologyService, WorkBoard, CompanyKnowledgeService and ObjectiveSystem retain authoritative behavior. Shared declaration validates ordinary player-orderable final-effect intent, explicit unique unlocks and actual typed references; original graph compiler continues dependency-cycle validation. ObjectiveDeclaration keeps positive deadline and explicit optional effect identity validation.

## 3. API

Add public TechnologyDeclaration and ObjectiveDeclaration; existing contract types and primitive saves remain unchanged. Breaking independent/native admission parity changes engine identity2.411.0, save154/replay1 unchanged. No compatibility parser or duplicate metadata catalog.

## 4. Files

Existing Technology.cs/ObjectiveContracts.cs and two independent provider files; one new module registration proof and exact test driver; two new explicit JSON files; registry/project/protocol identity; stage/evidence/master/audit documents. No business/Tycoon, Oil, platform, renderer or sole-clock edits.

## 5. Errors

Native/independent missing/null/duplicate prerequisites/unlocks, unknown/wrong references, cyclic prerequisites, missing final effect, zero research time, invalid effect identity and nonpositive deadline refuse before Initialize. Negative authored mutations use the same actual admission path, not a second verifier or fabricated fallback.

## 6. Compatibility

No legacy concerns, shim or weaker independent-reader path.

## 7. Verification

One new exact bounded case only. Actual file composition registers independent types without native technology/objective inheritance; physical machine travel/tool supply/time earns research, pause/save/retry keeps ownership, native prerequisite unlock enables original physical production, objective dependencies settle in the sole original pass. Private detached facts, retained snapshots, saved twins/replay and early native/independent refusals are checked. Compile completed slice; no historical/family/full runtime.

## 8. Risks

This proves authored registration/reader/coordinator extensibility, not arbitrary mod sandboxing, all effect parameter bounds, utilities, all optional-module matrices or full regression. Original P11.acceptance.002 remains open unless its complete effect-bound scope is proven.

## 9. Non-goals

New progression/lifecycle schedulers, starting knowledge grants, instantaneous physical work, budget/business changes, rendering/Draw/platforms, protected G19 bindings and broad runtime suites. No stage/commit/push until full goal AND plan finish.

## 10. Order

Inspect actual declarations/coordinators → shared metadata owners → migrate independent/native providers → explicit authored module/master/document → new public physical driver → build/exact case and same-case repairs → evidence/master/audit update.
