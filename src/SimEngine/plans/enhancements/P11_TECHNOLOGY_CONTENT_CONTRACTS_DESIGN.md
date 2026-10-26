# P11-A2 — Contract-based technology content and validation

Status: scoped implementation verified on2.332.0/save154/replay1; ONE NEW exact bounded case69/0/0 in675ms. Uses [existing technology work](P11_TECHNOLOGY_WORK_DESIGN.md) and [contract catalog](P4_STARTUP_CONTENT_CONTRACTS_DESIGN.md). Full P1–P12 remains open.

## Problem and goals

TechnologyService still enumerates/resolves TechnologyDef for prerequisites, research, completion, access, private views, learned observations and restored chronology despite ITechnologyDefinition. Native unlock resolution similarly requires TechnologyUnlockDef. A separate ActivityDef implementing the technology contract is omitted/rejected.

Use the existing content indexes, work DAG/board/effects and company knowledge ledger. Independent technology and unlock content must be usable from explicit master-listed JSON without coordinator edits or native technology inheritance. Native prerequisites, work requirements and objective predicates must refer to that exact contract alias. Same-company physical work/material consumption and once-only learned knowledge must remain saved/private/replayable.

## Ownership and contracts

ITechnologyUnlock extends IContentDefinition; native and external unlock definitions alias the existing catalog. Broaden DefinitionTechnologyUnlockDef<T> to class,IContentDefinition without a new resolver.

ITechnologyDefinition remains the ordinary player activity contract. All TechnologyService lookups/topology/enumeration use it. Native TechnologyDef resolves prerequisite/unlock contracts. No native learned list, new research queue, research timer, completion table or technology command family.

Move linked semantic validation from native TechnologyDef into the sole TechnologyService composition validator so every implementation obeys it: actual owned aliases, positive elapsed graph, final technology completion capability, no entity replacement, owned/unique unlocks and valid prerequisite DAG. Add ITechnologyCompletionEffect marker to the existing authored work-effect contracts, implemented by TechnologyCompletionDef; no alternative execution or instant grant.

TechnologyRequirementDef, TechnologyObjectivePredicateDef and external safety-signal metadata resolve ITechnologyDefinition. Native JSON grammar still explicitly requires prerequisite/unlock arrays. Providers own intent parsing and delegate research admission to ITechnologyKnowledge; the native service owns exclusivity/prerequisites/known refusal.

Core build2.332.0/save154/replay1; saved layout/native schemas/sole GameClock unchanged.

## Files and implementation sequence

Standard/Technology.cs, ObjectiveContracts.cs; existing external PlanetXControlProof metadata; exact protocol identity. Add explicit independent technology/unlock content and extension declarations using the existing shared point/construction factory, not copied catalogs.

Plan → contract lookup/central admission revision → independent definitions/JSON → one public player driver → build → ONE NEW bounded exact case (repair/rerun only it) → evidence/tracker/audit.

## Focused acceptance

Prove independent technology/unlock actual aliases, native prerequisite/requirement/objective links, physical movement/research/material consumption, paused/cancelled saved progress without premature learning, private detached facts, exclusivity/no repeated grants, mixed native/external prerequisite completion, saved twins/replay, missing/wrong/cyclic references and no-time/no-final/replacement failure before Initialize.

All older cases compile only, not rerun. This does not close full P11 or the whole plan.

## Risks and exclusions

Marker contracts declare semantics, not a sandbox for arbitrary malicious provider code. Broader mod/effect/utility/transformation-unlock/parameter/stress/corrupt/lockstep matrices stay open. No full/family/Oil/Tycoon/platform runtime, business expansion, default starting technology, compatibility reader, renderer/adapter/Draw, second clock or publication. Full goal AND plan must finish and be verified before staging/commit/push.

