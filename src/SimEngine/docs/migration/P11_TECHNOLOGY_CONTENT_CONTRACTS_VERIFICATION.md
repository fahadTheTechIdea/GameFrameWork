# P11-A2 — Technology content contract verification

Date:2026-10-09. [Stage](../../plans/enhancements/P11_TECHNOLOGY_CONTENT_CONTRACTS_DESIGN.md).

## Outcome and existing owners

TechnologyService now uses ITechnologyDefinition for prerequisite topology, catalog ownership, research/exclusivity/completion, unlock access, private views, learned observation validation and saved chronology. Native TechnologyDef resolves prerequisite and unlock contracts. TechnologyRequirementDef, TechnologyObjectivePredicateDef and the existing external Planet-X safety signal metadata reference actual ITechnologyDefinition aliases.

ITechnologyUnlock is content-owned through the existing indexes. DefinitionTechnologyUnlockDef<T> accepts content contracts. The existing TechnologyCompletionDef implements ITechnologyCompletionEffect; the sole TechnologyService composition validator admits linked semantics for both native and independent definitions: actual aliases, positive elapsed graph, technology final capability, no node replacement, unique owned unlocks and prerequisite DAG.

IndependentTechnologyDef uses the existing ActivityDef/work DAG without TechnologyDef inheritance; IndependentActivityUnlockDef uses ordinary Def metadata without TechnologyUnlockDef inheritance. Provider intent admission delegates to ITechnologyKnowledge. Master-listed technology-contracts.json owns new content; existing technology.json is reused with explicit native prerequisite binding, not copied.

No new research command family, research scheduler/timer/progress queue, learned store, completion ledger, loader or catalog. WorkBoard owns all physical work/material/tool/progress/cancellation; CompanyKnowledgeService alone owns learned state. Native ObjectiveSystem reads the same learned facts.

Core identity:beep-simengine-2.332.0/save154/replay1. No state layouts/provider schemas or sole GameClock changes. Strict native prerequisite/unlock arrays no longer accept null as fabricated empty declarations.

## Build and exact runtime evidence

FOUR clean builds of the framework/test/extension/quickstart graph:5.93s/5.35s/3.93s/3.70s, zero warnings/errors.

Only ONE NEW bounded exact case was executed:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Technology content contracts: independent research and unlocks use physical work, private objectives and saved replay" --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

First:0 passed/1 failed,57ms; PID2948 exit1/observed211ms. New fixture authoring assumed a storage component, but the actual source is point_stock. Corrected the JSON initial field on that existing provider; no runtime resource grant or alternate engine path.

Second:65 passed/0 failed/0 aborted,740ms; PID10944 exit0/observed896ms. Strengthened the same case with two planned replacement/final-effect refusal branches.

FINAL:69 passed/0 failed/0 aborted,675ms; PID22620 exit0/observed816ms. All outputs untruncated; no observer error, timeout or forced termination.

## What is proved

- Independent technology/unlock actual immutable aliases connect native prerequisites, technology requirements and objective predicate.
- Private technology view includes three mixed definitions; authored content never grants starting knowledge.
- Explicit source stock40, machine position4; player enables work through ordinary policy command. Actual research moves the machine, consumes8 inputs and retains reusable tools/progress in native owners.
- Ordinary persistent plan pause retains saved progress and no premature learned fact. Cancellation keeps spent inputs, releases work through native cancellation and grants nothing.
- Retried research earns actual completion once; private detached facts and another company remain unlearned. Active and known research are refused through normal previews.
- Independent unlock opens exactly the actual activity after learning. Native objective observes that same earned knowledge for company0; company1 remains unearned.
- Native foundation and advanced research complete through mixed contract prerequisite/requirement links. Four consumed bills (cancelled attempt, retry, foundation, advanced) leave8 of40 actual units.
- Saved twins remain hash-identical during physical work and pauses; learned chronology validates after save/load. Replay and a new interleaved session retain isolation.
- Fifteen missing/wrong/cyclic/duplicate prerequisite/unlock/target/objective/array/time/final/replacement branches reject before Initialize.
- Source search finds no remaining TechnologyDef catalog lookup in the framework. This is not proof that every unrelated content caller has migrated.
- Public player driver contains no World escape or raw resource/progress mutation; extension code owns only content behavior/metadata instrumentation.

Executable freeze:2026-10-09 01:49:29 UTC. Subsequent changes documentation/audit only.

## Limits and publication

Historical technology/Planet-X/startup/point/tile/optional-money cases compile only, not runtime rerun on2.332.0. No full/family/Oil/Tycoon/platform runtime. No business/default income changes, mandatory money, instant knowledge, compatibility reader, Draw/adapter or second clock.

Marker semantics do not sandbox malicious mod code. Broader effects/utilities/transformation-unlock/parameters/corrupt-state/lockstep/stress/global purity and full P1–P12 remain OPEN. Original acceptance requirements/checkboxes stay unchanged. No staging/commit/push until FULL goal AND plan genuinely finish and are verified.

