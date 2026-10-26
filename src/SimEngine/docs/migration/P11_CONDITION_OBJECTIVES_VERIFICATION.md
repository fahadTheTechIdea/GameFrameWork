# P11-B2 — Shared private conditions and operational objectives verification

2026-10-07. Build2.226.0/save130; controller view4. Objective view2/system1 and native payloads
unchanged. GameConditionsModule installs pure content only, no view/system/service/Sync/clock.

## Every invocation

ONE build, first attempt, exit0/0 warnings/0 errors,9.82s:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
```

ONE distinct exact NEW bounded scenario, ONE runtime invocation, first attempt:

```powershell
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Objectives: shared private conditions and material-consuming incident recovery"
```

82 passed/0 failed/0 aborted483ms,exit0. No repair/rerun needed.
98 older assertion files/161 identity lines compile-migrated ONLY, NOT run. Existing playbook branch
fixture directly migrated to actual separately master-listed playbook-conditions.json. Old names/
sections/ObserveStrategyCondition and PlayerStrategyConditions.cs removed, no compatibility shim.
No older/family/full/platform/stale-binary gate, no gameplay/test edit after final green.

## Responsibilities and actual authored content

IGameCondition/GameConditionDef/SignalGameConditionDef replace existing controller-specific names.
SAME condition implementation, not a parallel signal interpreter. Optional sim.conditions declares
signal_conditions; GameConditionContracts applies actual content ownership/private seat/content
visibility and existing EntityObservation provider guard. OperationsView and SignalGateCondition
retain exact typed source/schema/kind/unit/unavailability evaluation. all/any modes quantify matching
owned entities; every local condition must hold on SAME entity. No-target is typed wait, never vacuous
success. A provider has no condition progress/reservation/timer/controller lifecycle state.

PlanPlaybookDef, PlayerControllersSystem, PlayerDecisionContext.ObserveCondition and detached playbook
diagnostics use same condition contracts. Generic condition declarations removed from controller
module; pure DTO/contracts moved to PlaybookDiagnostics.cs. Public GameConditionProofModule owns the
existing elapsed provider/dial declaration; controller proof module no longer owns/duplicates them.
Affected playbook call sites/fixtures compiled only, NOT runtime-rerun in this gate.

ConditionObjectivePredicateDef resolves exact GameConditionDef content alias and observes actual
company-private PlayerView. It publishes binary0/1 readiness, not fabricated partial signal progress.
LogicalObjectivePredicateDef and sole ObjectiveSystem DAG/enrollment/state/deadline/once-only effect
owner remain unchanged. Existing predicate Observe/effect Check now receive scoped observation
guards. CopyObjectives clears/refuses invalid negative/nonseat viewers; exact None observer explicit.

Actual master has objective-conditions.json under sim.conditions, condition-objectives.json under
sim.objectives, objective-response.json under selected points content. The existing public point
provider and native incident/inventory/response effects/target requirements/activity graph implement
all movement/rules/work. No harness objective/workflow algorithm, live progress/stock/incident injection,
Tycoon default/income/purchase/instant repair, host/demo/platform or OilFieldDays changes.

## Focused physical/private/durable proof

No autopilot or business module is selected. Own plant starts20 actual supplies at point20, foreign
distinct prototype at50 starts20. Shared condition aliases catalog identity; separate declaring module
documents own content once. Public elapsed condition uses actual GameClock, no scheduler change.
Repeated native observations leave hash/identity/materials unchanged; immutable prerequisite snapshots
stay detached. Malformed negative/nonseat snapshots empty; exact observer sees6 enrolled entries.

Both companies use ordinary RaiseIncidentCommand. Foreign incident is real but cannot complete own
condition or lend its prototype/resources. Player enables existing machine policy and previews/submits
native own incident. Pending command save/load agrees; next Update creates actual incident and response,
completes detection objective but keeps dependent repair Locked for whole-pass capture. Stock still20.
Machine travels to point20, begins authored inspect repair over game time, consumes8 actual supplies
through existing target-resource requirement. While stock12 satisfies reserve band, active incident
still blocks objective. Partial repair save/load twins agree. Actual response resolves incident; native
repair objective completes, victory remains Locked until next objective pass. Public elapsed + same
native recovered condition agree. Native outcome effect ends at field.recovery-complete exactly once.
Own supplies12, foreign20/unresolved incident unchanged. Foreign chain never earns own objective facts.
Terminal save/load/hash/loop halt and fresh interleaved session isolation pass. Waits bound400 Updates
per stage with actual game time/fault/outcome/objective/order diagnostic snapshot on timeout.

Boolean readiness with explicitly null inventory threshold yields exact InventorySignal.NoThreshold;
objective stays Active, no substitution/failure/material consumption/outcome, saved twins agree.
Missing own target under all and any modes waits condition.no-target. Missing condition/mode/empty
condition array/source/signal/kind/unit and actual objective dependency cycle refuse before Update.

Isolated public predicate/effect providers deliberately Pause the actual clock during Observe/Check.
Existing scope tracks clock and entity identity; faults prevent terminal success, effect Apply is not
reached, faulted save/hash refuse. These are labeled provider fault instrumentation, NOT player
gameplay. Guard does NOT prove arbitrary Entity.Touch/component/service mutation detection, CPU
sandbox/global privacy. Effect entry may already be Completing when Check faults; no rollback claim.

## Remaining and publication

Shared playbook/pressure/network/transformation older runtime gates NOT rerun. Broader condition
composition/all entity combinations/hidden knowledge/other metrics, provider validation faults,
arbitrary mutation/privacy/corruption/overflow/replay/lockstep/soak/allocation and full framework
acceptance remain open, as do industrial rival maintenance/risk/recovery/generated-resource strategy.
This is a scoped B2 proof, NOT completed P11 or full P1–P12 goal/plan.

Development-lifecycle review preserved single responsibility and actual engine-side contracts/JSON
ownership. Full goal active; user requested commit/push ONLY after FULL completion/verification.
No commit/push performed. Preserve unrelated dirty work; no adapter/Draw/platform/business changes.

[Design](../../plans/enhancements/P11_CONDITION_OBJECTIVES_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md),
[current scoped source inventory](P11_CONDITION_OBJECTIVES_SOURCE_MANIFEST.json), excludes itself.
Inventory excludes removed PlayerStrategyConditions.cs and records its replacement ownership;
previous manifests remain historical and unchanged. Read-only hashes/local links/diff/sole clock checks
are source evidence, NOT additional runtime gates.

Final scoped inventory:312 unique source/document/content paths,51 Markdown documents,1,301 local
links,0 broken links. Current SHA256 records do not claim older-suite runtime success.
git diff --check exits0; sole GameClock construction remains Kernel/World.cs:96, unchanged SHA256
339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31.
Manifest excludes itself; final read-only recheck verifies hashes/links without another runtime gate.
